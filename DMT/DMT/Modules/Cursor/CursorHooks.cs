#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2010-2026  Gerald Evans
// 
// Dual Monitor Tools is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using DMT.Library.Environment;
using DMT.Library.GuiUtils;
using DMT.Library.PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DMT.Modules.Cursor
{
	class CursorHooks
	{
		CursorModule _cursorModule;

		System.Threading.Thread _hooksThread = null;
		Control _hooksThreadControl = null;
		// wait handle event for when the threads control is ready
		System.Threading.EventWaitHandle _hooksThreadControlEwh = null;

		bool _freeMovementKeyPressed;
		bool _freeMovementButtonPressed;

		// Win32 low level mouse hook
		NativeMethods.HookProc llMouseProc;
		IntPtr llMouseHook = IntPtr.Zero;

		// Win32 low level keyboard hook
		NativeMethods.HookProc llKeyboardProc;
		IntPtr llKeyboardHook = IntPtr.Zero;

		// Windows silently removes low level hooks that take too long to respond,
		// so we periodically check that the mouse hook is still being called.
		// These are only accessed on the hooks thread.
		const int HookWatchdogInterval = 2000;	// in ms
		Timer _hookWatchdogTimer;
		int _mouseCallbackCount;
		int _lastWatchdogCallbackCount;
		Point _lastWatchdogCursorPos;

		// Barriers which constrain the cursor movement
		CursorBarrierLower _leftBarrier = new CursorBarrierLower(false, 0, 0);
		CursorBarrierUpper _rightBarrier = new CursorBarrierUpper(false, 0, 0);
		CursorBarrierLower _topBarrier = new CursorBarrierLower(false, 0, 0);
		CursorBarrierUpper _bottomBarrier = new CursorBarrierUpper(false, 0, 0);


		#region Interface used by CursorModule
		public CursorHooks(CursorModule cursorModule)
		{
			_cursorModule = cursorModule;

			llMouseProc = llMouseHookCallback;
			llKeyboardProc = llKeyboardHookCallback;
		}

		/// <summary>
		/// Gets a value indicating whether cursor movement is restricted (sticky or locked)
		/// </summary>
		public bool CursorLocked
		{
			get { return llMouseHook != IntPtr.Zero; }
		}

		/// <summary>
		/// Starts the hooks thread up
		/// </summary>
		public void Start()
		{
			_hooksThreadControlEwh = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.ManualReset);
			_hooksThread = new System.Threading.Thread(CursorHooksThread);
			_hooksThread.IsBackground = true;
			_hooksThread.SetApartmentState(System.Threading.ApartmentState.STA); // TODO: is this needed?
			_hooksThread.Start();

			// now wait until the thread has created it's control
			_hooksThreadControlEwh.WaitOne();
		}

		/// <summary>
		/// Terminates the hooks
		/// </summary>
		public void Terminate()
		{
			if (_hooksThread != null)
			{
				if (_hooksThreadControl != null)
				{
					// this should cause the hooks thread message loop to exit
					// and it should then proceed to unhook
					_hooksThreadControl.BeginInvoke(((Action)(() => Application.ExitThread())));

					// wait until the thread has died which should be after the unhook is complete
					// shouldn't need a timeout, but JIC
					_hooksThread.Join(2000);
				}
			}
		}

		// The cursor should be locked (possibly just sticky) to the screen it is currently on.
		public void LockCursorToScreen()
		{
			ReBuildBarriers();

			UpdateHooks();
		}

		// The cursor's movement should not be hindered by screen edges
		public void UnLockCursor()
		{
			UpdateHooks();
		}

		public bool AreBarriersNeeded()
		{
			return IsMouseHookRequired();
		}

		/// <summary>
		/// Called when cursor type or dead space jump flag changed
		/// </summary>
		public void ReflectNewCursorOptions()
		{
			ReBuildBarriers();

			UpdateHooks();
		}

		// rebuild the barriers to restrict movement of the cursor
		// to the screen that it is currently on.
		// This can be called by the low level mouse hook callback,
		// so needs to be reasonably efficient.
		//
		// Note: no locking is currently employed so need to be carefull of the order in which things are done
		public void ReBuildBarriers(Point pt)
		{
			//Screen curScreen = Screen.FromPoint(pt);
			Monitor curScreen = Monitor.AllMonitors.FromPoint(pt);
			int minForce = _cursorModule.MinForce;
			if (_cursorModule.CursorLockedToMonitor)
			{
				// override any current force to be fully locked on this monitor 
				minForce = int.MaxValue;
			}

			// We use the virtualDesktopRect to determine if it is
			// possible for the mouse to move over each of the borders
			// of the current screen.
			Rectangle vitrualDesktopRect = ScreenHelper.GetVitrualDesktopRect();

			// left of current screen
			if (curScreen.Bounds.Left > vitrualDesktopRect.Left)
			{
				_leftBarrier.ChangeBarrier(true, curScreen.Bounds.Left, minForce);
			}
			else
			{
				// not possible for mouse to move here, so fully disable barrier to improve efficiency
				_leftBarrier.ChangeBarrier(false, 0, 0);
			}

			// right of current screen
			if (curScreen.Bounds.Right < vitrualDesktopRect.Right)
			{
				_rightBarrier.ChangeBarrier(true, curScreen.Bounds.Right - 1, minForce);
			}
			else
			{
				// not possible for mouse to move here, so fully disable barrier to improve efficiency
				_rightBarrier.ChangeBarrier(false, 0, 0);
			}

			// top of current screen
			if (curScreen.Bounds.Top > vitrualDesktopRect.Top)
			{
				_topBarrier.ChangeBarrier(true, curScreen.Bounds.Top, minForce);
			}
			else
			{
				// not possible for mouse to move here, so fully disable barrier to improve efficiency
				_topBarrier.ChangeBarrier(false, 0, 0);
			}

			// bottom of current screen
			if (curScreen.Bounds.Bottom < vitrualDesktopRect.Bottom)
			{
				_bottomBarrier.ChangeBarrier(true, curScreen.Bounds.Bottom - 1, minForce);
			}
			else
			{
				// not possible for mouse to move here, so fully disable barrier to improve efficiency
				_bottomBarrier.ChangeBarrier(false, 0, 0);
			}
		}

		#endregion

		void CursorHooksThread()
		{
			_hooksThreadControl = new Control();
			// need to create the handle
			IntPtr handle = _hooksThreadControl.Handle;

			// Let calling thread know that the control has been created
			_hooksThreadControlEwh.Set();

			_hookWatchdogTimer = new Timer();
			_hookWatchdogTimer.Interval = HookWatchdogInterval;
			_hookWatchdogTimer.Tick += HookWatchdogTimer_Tick;
			_hookWatchdogTimer.Start();

			Application.Run();

			_hookWatchdogTimer.Stop();
			_hookWatchdogTimer.Dispose();

			UnHookKeyboard();
			UnHookMouse();
		}

		// Runs on the hooks thread.
		// If the cursor has moved but our mouse hook hasn't been called,
		// then Windows has removed the hook, so we need to re-install it.
		void HookWatchdogTimer_Tick(object sender, EventArgs e)
		{
			Point cursorPos = System.Windows.Forms.Cursor.Position;

			if (llMouseHook != IntPtr.Zero)
			{
				if (cursorPos != _lastWatchdogCursorPos && _mouseCallbackCount == _lastWatchdogCallbackCount)
				{
					// The cursor can also be moved programmatically without calling the hook,
					// in which case the re-hook is unnecessary, but harmless.
					// The keyboard hook will have suffered the same fate, so re-install both.
					UnHookMouse();
					UnHookKeyboard();
					UpdateHooks();
					ReBuildBarriers();
				}
			}

			_lastWatchdogCursorPos = cursorPos;
			_lastWatchdogCallbackCount = _mouseCallbackCount;
		}

		// rebuild the barriers based on the current cursor position
		void ReBuildBarriers()
		{
			// TODO: is it worth performing this test?
			if (AreBarriersNeeded())
			{
				ReBuildBarriers(_cursorModule.GetCursorPosition());
			}
		}

		void JumpToNearestValidY(int x, ref int y)
		{
			int nearestY = y;
			int nearestDistance = int.MaxValue;

			foreach (Monitor monitor in Monitor.AllMonitors)
			{
				// check this monitor covers this x value
				if (x >= monitor.Bounds.Left && x < monitor.Bounds.Right)
				{
					// check if this point is above, on or below this monitor
					if (y < monitor.Bounds.Top)
					{
						int distance = monitor.Bounds.Top - y;
						if (distance < nearestDistance)
						{
							nearestDistance = distance;
							nearestY = monitor.Bounds.Top;
						}
					}
					else if (y >= monitor.Bounds.Bottom)
					{
						int distance = y + 1 - monitor.Bounds.Bottom;
						if (distance < nearestDistance)
						{
							nearestDistance = distance;
							nearestY = monitor.Bounds.Bottom - 1;
						}
					}
					else
					{
						// point is actually on this monitor, so no more to do
						return;
					}
				}
			}

			y = nearestY;
		}

		void JumpToNearestValidX(ref int x, int y)
		{
			int nearestX = x;
			int nearestDistance = int.MaxValue;

			foreach (Monitor monitor in Monitor.AllMonitors)
			{
				// check this monitor covers this y value
				if (y >= monitor.Bounds.Top && y < monitor.Bounds.Bottom)
				{
					// check if this point is to the left, on or to the right of this monitor
					if (x < monitor.Bounds.Left)
					{
						int distance = monitor.Bounds.Left - x;
						if (distance < nearestDistance)
						{
							nearestDistance = distance;
							nearestX = monitor.Bounds.Left;
						}
					}
					else if (x >= monitor.Bounds.Right)
					{
						int distance = x + 1 - monitor.Bounds.Right;
						if (distance < nearestDistance)
						{
							nearestDistance = distance;
							nearestX = monitor.Bounds.Right - 1;
						}
					}
					else
					{
						// point is actually on this monitor, so no more to do
						return;
					}
				}
			}

			x = nearestX;
		}

		MouseButtons GetXButton(NativeMethods.MSLLHOOKSTRUCT msllHookStruct)
		{
			uint mouseDataHiWord = msllHookStruct.mouseData >> 16;

			if (mouseDataHiWord == NativeMethods.XBUTTON1)
			{
				return MouseButtons.XButton1;
			}
			else if (mouseDataHiWord == NativeMethods.XBUTTON2)
			{
				return MouseButtons.XButton2;
			}
			else
			{
				return MouseButtons.None;
			}
		}


		void UpdateHooks()
		{
			if (IsKeyboardHookRequired())
			{
				HookKeyboard();
			}
			else
			{
				UnHookKeyboard();
			}

			if (IsMouseHookRequired())
			{
				HookMouse();
			}
			else
			{
				UnHookMouse();
			}
		}

		bool IsKeyboardHookRequired()
		{
			if (_cursorModule.CurCursorType != CursorModule.CursorType.Free)
			{
				return true;
			}

			return false;
		}

		bool IsMouseHookRequired()
		{
			if (_cursorModule.CursorLockedToMonitor)
			{
				return true;
			}

			if (_cursorModule.CurCursorType != CursorModule.CursorType.Free)
			{
				// need mouse hook to control sticky/locked movement
				return true;
			}

			if (_cursorModule.AllowDeadSpaceXJump || _cursorModule.AllowDeadSpaceYJump)
			{
				// need mouse hook to allow dead space jumping
				return true;
			}

			if (_cursorModule.DeskMapper != null)
			{
				// need mouse hook to move the cursor to the matching place on the next screen
				return true;
			}

			return false;
		}

		/// <summary>
		/// If the cursor is leaving the screen it's on, moves it to the same physical place on the next screen
		/// (from how the screens are arranged on the desk)
		/// </summary>
		/// <returns>true if the cursor position was changed</returns>
		bool MapToDesk(ref int x, ref int y)
		{
			DeskCursorMapper mapper = _cursorModule.DeskMapper;
			if (mapper == null)
			{
				return false;
			}

			// where the cursor is now (before this movement)
			NativeMethods.POINT current;
			if (!NativeMethods.GetCursorPos(out current))
			{
				return false;
			}

			Point result;
			if (mapper.TryMap(mapper.ScreenAt(new Point(current.x, current.y)), new Point(x, y), out result))
			{
				x = result.X;
				y = result.Y;
				return true;
			}

			return false;
		}

		#region hook/unhook must be done on the hooks thread

		// TODO: some of below (esp. the tests) could be run on the calling thread.
		// This would mean in some cases we can avoid the Invoke,
		// but are there any dangers in doing this?

		void HookKeyboard()
		{
			if (_hooksThreadControl.InvokeRequired)
			{
				_hooksThreadControl.BeginInvoke(new Action(HookKeyboard));
				return;
			}

			if (llKeyboardHook == IntPtr.Zero)
			{
				using (Process curProcess = Process.GetCurrentProcess())
				{
					using (ProcessModule curModule = curProcess.MainModule)
					{
						IntPtr hModule = NativeMethods.GetModuleHandle(curModule.ModuleName);
						llKeyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, llKeyboardProc, hModule, 0);
					}
				}
			}
		}

		void UnHookKeyboard()
		{
			if (_hooksThreadControl.InvokeRequired)
			{
				_hooksThreadControl.BeginInvoke(new Action(UnHookKeyboard));
				return;
			}

			// make sure the low level keyboard hook is unhooked
			if (llKeyboardHook != IntPtr.Zero)
			{
				// unhook our callback to make sure there is no performance degredation
				if (!NativeMethods.UnhookWindowsHookEx(llKeyboardHook))
				{
					//_logger.LogError("UnHookKeyboard", "unable to unhook keyboard");
				}
				llKeyboardHook = IntPtr.Zero;
			}
		}

		void HookMouse()
		{
			if (_hooksThreadControl.InvokeRequired)
			{
				_hooksThreadControl.BeginInvoke(new Action(HookMouse));
				return;
			}

			if (llMouseHook == IntPtr.Zero)
			{
				using (Process curProcess = Process.GetCurrentProcess())
				{
					using (ProcessModule curModule = curProcess.MainModule)
					{
						IntPtr hModule = NativeMethods.GetModuleHandle(curModule.ModuleName);
						llMouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, llMouseProc, hModule, 0);
					}
				}
			}
		}

		void UnHookMouse()
		{
			if (_hooksThreadControl.InvokeRequired)
			{
				_hooksThreadControl.BeginInvoke(new Action(UnHookMouse));
				return;
			}

			// make sure the low level mouse hook is unhooked
			if (llMouseHook != IntPtr.Zero)
			{
				// unhook our callback to make sure there is no performance degredation
				if (!NativeMethods.UnhookWindowsHookEx(llMouseHook))
				{
					// looks like Windows has already unhooked us 
					//_logger.LogError("UnHookMouse", "unable to unhook mouse");
				}
				llMouseHook = IntPtr.Zero;
			}
		}
		#endregion


		#region hooks
		// This is the low level Mouse hook callback
		// Processing in here should be efficient as possible
		// as it can be called very frequently.
		public int llMouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			// let the watchdog know that we are still hooked
			unchecked
			{
				_mouseCallbackCount++;
			}

			if (nCode >= 0)
			{
				// lParam is a pointer to a MSLLHOOKSTRUCT, 
				NativeMethods.MSLLHOOKSTRUCT msllHookStruct = (NativeMethods.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.MSLLHOOKSTRUCT));
				int originalX = msllHookStruct.pt.x;
				int originalY = msllHookStruct.pt.y;
				int x = originalX;
				int y = originalY;

				bool touchEvent = false;

				// we don't allow free movement options when the CursorLockedToMonitor option is used
				// or allow touch events to move the cursor out of the locked monitor
				if (!_cursorModule.CursorLockedToMonitor)
				{
					if (_cursorModule.AllowFreeMovementButton)
					{
						MouseButtons mouseButton = MouseButtons.None;
						bool buttonDown = false;
						int msg = (int)wParam;
						switch (msg)
						{
							case NativeMethods.WM_LBUTTONDOWN:
								mouseButton = MouseButtons.Left;
								buttonDown = true;
								break;

							case NativeMethods.WM_LBUTTONUP:
								mouseButton = MouseButtons.Left;
								break;

							case NativeMethods.WM_MBUTTONDOWN:
								mouseButton = MouseButtons.Middle;
								buttonDown = true;
								break;

							case NativeMethods.WM_MBUTTONUP:
								mouseButton = MouseButtons.Middle;
								break;

							case NativeMethods.WM_RBUTTONDOWN:
								mouseButton = MouseButtons.Right;
								buttonDown = true;
								break;

							case NativeMethods.WM_RBUTTONUP:
								mouseButton = MouseButtons.Right;
								break;

							case NativeMethods.WM_XBUTTONDOWN:
								mouseButton = GetXButton(msllHookStruct);
								buttonDown = true;
								break;

							case NativeMethods.WM_XBUTTONUP:
								mouseButton = GetXButton(msllHookStruct);
								break;
						}

						if (mouseButton != MouseButtons.None && mouseButton == _cursorModule.FreeMovementButton)
						{
							if (buttonDown)
							{
								_freeMovementButtonPressed = true;
							}
							else
							{
								_freeMovementButtonPressed = false;

								// must also rebuild the barriers as the cursor may now be on a different screen
								ReBuildBarriers();
							}
						}
					}

					if (_freeMovementKeyPressed)
					{
						// If the free movement key is Control (left or right)
						// and that key is pressed when Ctrl-Alt-Del (and some other combinations)
						// we don't see the control key being released, so we think it is still pressed.
						// So we double check here that the key is still really pressed.
						//
						// Note: we could remove the keyboard hook and just always call GetAsyncKeyState() here in the mouse hook
						// to determine the state of the key, but this would add extra load to every mouse movement message
						// so we only call this if we think the free movement key is already pressed.
						ushort keyFlags = (ushort)NativeMethods.GetAsyncKeyState((int)_cursorModule.FreeMovementKey);

						// most sig bit of the 16 bit value will be set if key is currently pressed
						if ((keyFlags & 0x8000) == 0)
						{
							// key is no longer pressed
							_freeMovementKeyPressed = false;
						}
					}

					if (IntPtr.Size == 4)
					{
						uint dwExtraInfo = (uint)msllHookStruct.dwExtraInfo.ToInt32();
						if ((dwExtraInfo & NativeMethods.MOUSEEVENTF_FROMTOUCH) == NativeMethods.MOUSEEVENTF_FROMTOUCH)
						{
							touchEvent = true;
						}
					}
					else if (IntPtr.Size == 8)
					{
						ulong qwExtraInfo = (ulong)msllHookStruct.dwExtraInfo.ToInt64();
						if ((qwExtraInfo & NativeMethods.MOUSEEVENTF_FROMTOUCH) == NativeMethods.MOUSEEVENTF_FROMTOUCH)
						{
							touchEvent = true;
						}
					}
				}

				bool useBarrier = true;
				bool mappedToDesk = false;

				if (_cursorModule.CursorLockedToMonitor)
				{
					// We always use the barrier in this case
					// The only way to move the cursor out of the monitor is by toggling this mode
				}
				else if (touchEvent)
				{
					// If this message was generated by a touch event
					// then we want to allow the cursor to move un-hindered
					useBarrier = false;
				}
				else
				{
					// these can all allow the mouse to be moved unhindered
					if (_freeMovementKeyPressed || _freeMovementButtonPressed || _cursorModule.PrimaryReturnUnhindered)
					{
						int preJumpX = x;
						int preJumpY = y;

						if (_cursorModule.AllowDeadSpaceXJump || _cursorModule.AllowDeadSpaceYJump)
						{
							bool outsideX = _leftBarrier.Outside(x) || _rightBarrier.Outside(x);
							bool outsideY = _topBarrier.Outside(y) || _bottomBarrier.Outside(y);

							if (_cursorModule.AllowDeadSpaceXJump && outsideX)
							{
								JumpToNearestValidY(x, ref y);
							}
							if (_cursorModule.AllowDeadSpaceYJump && outsideY)
							{
								JumpToNearestValidX(ref x, y);
							}
						}

						if (_freeMovementKeyPressed || _freeMovementButtonPressed)
						{
							useBarrier = false;
						}
						else // if PrimaryReturnUnhindered
						{
							if (Monitor.AllMonitors.IsOnPrimary(x, y, _cursorModule.PrimaryGroup))
							{
								useBarrier = false;
							}
							else
							{
								// don't allow the barries to be bypassed
								// x/y may have been updated due to a dead space jump - need to undo this
								x = preJumpX;
								y = preJumpY;
							}
						}
					}
				}

				if (useBarrier)
				{
					// check if the cursor has moved from one screen to another
					// and if so add the required amount of stickiness to the cursor
					// restraining it to the current screen if necessary

					bool brokenThroughX = false;
					bool brokenThroughY = false;

					if (_leftBarrier.BrokenThrough(ref x) || _rightBarrier.BrokenThrough(ref x))
					{
						brokenThroughX = true;
					}

					if (_topBarrier.BrokenThrough(ref y) || _bottomBarrier.BrokenThrough(ref y))
					{
						brokenThroughY = true;
					}

					// the desk layout takes precedence over jumping dead space
					// (checked on every move, as the barriers are out of date if something else has moved the cursor)
					mappedToDesk = MapToDesk(ref x, ref y);
					if (!mappedToDesk && _cursorModule.AllowDeadSpaceXJump && brokenThroughX)
					{
						JumpToNearestValidY(x, ref y);
					}
					if (!mappedToDesk && _cursorModule.AllowDeadSpaceYJump && brokenThroughY)
					{
						JumpToNearestValidX(ref x, y);
					}

					if (brokenThroughX || brokenThroughY || mappedToDesk)
					{
						ReBuildBarriers(new Point(x, y));
					}
				}
				else
				{
					// cursor is free to move between screens, 
					// but still need to check if the screen that it is on has changed
					mappedToDesk = MapToDesk(ref x, ref y);
					bool outsideX = _leftBarrier.Outside(x) || _rightBarrier.Outside(x);
					bool outsideY = _topBarrier.Outside(y) || _bottomBarrier.Outside(y);
					if (outsideX || outsideY || mappedToDesk)
					{
						ReBuildBarriers(new Point(x, y));
					}
				}

				if (mappedToDesk)
				{
					// move it straight away (rather than via the UI thread), otherwise the next few mouse movements
					// would still be from the old screen and would be mapped straight back again
					NativeMethods.SetCursorPos(x, y);
					return 1;
				}

				if (x != originalX || y != originalY)
				{
					// override the position that Windows wants to place the cursor
					_cursorModule.SetCursorPoition(new Point(x, y));
					return 1;
				}
			}

			return NativeMethods.CallNextHookEx(llMouseHook, nCode, wParam, lParam);
		}

		// This is the low level Keyboard hook callback
		// Processing in here should be efficient as possible
		// as it can be called very frequently.
		private int llKeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0)
			{
				// lParam is a pointer to a KBDLLHOOKSTRUCT, but we only want the virtual key code
				// from this which is the first int, so instead of marshalling the entire structure
				// we just marshal the first int to minimise any performance hit 
				uint vkCode = (uint)Marshal.ReadInt32(lParam);
				Keys key = (Keys)vkCode;

				if (key == _cursorModule.FreeMovementKey)
				{
					if (_cursorModule.AllowFreeMovementKey)
					{
						int msg = (int)wParam;
						if (msg == NativeMethods.WM_KEYDOWN)
						{
							_freeMovementKeyPressed = true;
						}
						else if (msg == NativeMethods.WM_KEYUP)
						{
							_freeMovementKeyPressed = false;

							// must also rebuild the barriers as the cursor may now be on a different screen
							ReBuildBarriers();
						}
					}
				}
			}

			return NativeMethods.CallNextHookEx(llKeyboardHook, nCode, wParam, lParam);
		}

		#endregion
	}
}
