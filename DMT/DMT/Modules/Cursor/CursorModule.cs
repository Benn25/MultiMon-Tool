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

namespace DMT.Modules.Cursor
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Drawing;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows.Forms;

	using DMT.Library.GuiUtils;
	using DMT.Library.HotKeys;
	using DMT.Library.Logging;
	using DMT.Library.PInvoke;
	using DMT.Library.Settings;
	using DMT.Library.Transform;
	using DMT.Resources;
	using DMT.Library.Environment;
	using DMT.Library.Utils;
	using DMT.Library.DisplayHandling;

	/// <summary>
	/// Module for handling mouse/cursor related features
	/// </summary>
	class CursorModule : Module
	{
		/// <summary>
		/// Indicates if we use the index numbers or the names of the
		/// monitors to indicate which are in the primary group
		/// </summary>
		public enum EPrimaryGroupType { Nums = 0, Names = 1 };

		/// <summary>
		/// Delegate to set cursor
		/// </summary>
		/// <param name="pt">Position to set cursor</param>
		public delegate void SetCursorPoitionDelegate(Point pt);

		ISettingsService _settingsService;
		ILogger _logger;
		AppForm _appForm;

		/// <summary>
		/// Indicates if using free/sticky/locked cursor for the primary working area
		/// </summary>
		public CursorType CurCursorType { get; private set; }

		/// <summary>
		/// Indicates if cursor is locked to the current monitor.
		/// If it is, this overrides CurCursorType
		/// </summary>
		public bool CursorLockedToMonitor { get; private set; }

		CursorHooks _cursorHooks;

		CursorPrimaryOptionsPanel _cursorPrimaryOptionsPanel;

		public int MinForce { get; private set; }

		/// <summary>
		/// The primary group of monitors, used to allow free movement into any of these monitors.
		/// These are the monitor numbers, but zero based so can be used as indexes into Monitor.AllMonitors
		/// NOTE: this is no longer null if not in use - it will be empty instead
		/// </summary>
		List<int> _primaryGroup { get; set; }

		/// <summary>
		/// Initialises a new instance of the <see cref="CursorModule" /> class.
		/// </summary>
		/// <param name="settingsService">Settings repository</param>
		/// <param name="hotKeyService">Service for registering hot keys</param>
		/// <param name="logger">Application logger</param>
		public CursorModule(ISettingsService settingsService, IHotKeyService hotKeyService, ILogger logger, AppForm appForm)
			: base(hotKeyService)
		{
			_settingsService = settingsService;
			_logger = logger;
			_appForm = appForm;

			CurCursorType = CursorType.Free;
			CursorLockedToMonitor = false;

			ModuleName = "Cursor";

			_cursorHooks = new CursorHooks(this);
		}

		/// <summary>
		/// Type of cursor handling
		/// </summary>
		public enum CursorType 
		{ 
			/// <summary>
			/// Cursor free to move between screens
			/// </summary>
			Free = 0, 

			/// <summary>
			/// Cursor sticky when it comes to moving between screens
			/// </summary>
			Sticky = 1, 

			/// <summary>
			/// Cursor locked onto current screen
			/// </summary>
			Lock = 2 
		}

		/// <summary>
		/// Gets the controller for 'Free Cursor' hot key
		/// </summary>
		public HotKeyController FreeCursorHotKeyController { get; private set; }

		/// <summary>
		/// Gets the controller for 'Sticky Cursor' hot key
		/// </summary>
		public HotKeyController StickyCursorHotKeyController { get; private set; }

		/// <summary>
		/// Gets the controller for 'Lock Cursor' hot key
		/// </summary>
		public HotKeyController LockCursorHotKeyController { get; private set; }

		/// <summary>
		/// Gets the controller for 'Move cursor to next screen' hot key
		/// </summary>
		public HotKeyController CursorNextScreenHotKeyController { get; private set; }

		/// <summary>
		/// Gets the controller for 'Move cursor to previous screen' hot key
		/// </summary>
		public HotKeyController CursorPrevScreenHotKeyController { get; private set; }

		/// <summary>
		/// Gets the controller for 'Move cursor to primary screen' hot key
		/// </summary>
		public HotKeyController CursorToPrimaryScreenHotKeyController { get; private set; }

		/// <summary>
		/// Gets the controller for 'Lock cursor to monitor' hot key
		/// </summary>
		public HotKeyController LockCursorToMonitorHotKeyController { get; private set; }

		/// <summary>
		/// Gets or sets the minimum sticky value
		/// </summary>
		public int MinStickyForce
		{
			get 
			{ 
				return MinStickyForceSetting.Value; 
			}

			set 
			{ 
				MinStickyForceSetting.Value = value;
				if (CurCursorType == CursorType.Sticky)
				{
					// also need to update the min force that is currently in use
					MinForce = value;

					// minForce is used within the barriers, so must update these as well
					ReBuildBarriers();
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the free movement key is allowed
		/// </summary>
		public bool AllowFreeMovementKey
		{
			get 
			{ 
				return AllowFreeMovementKeySetting.Value; 
			}

			set
			{ 
				AllowFreeMovementKeySetting.Value = value; 
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the free movement mouse button is allowed
		/// </summary>
		public bool AllowFreeMovementButton
		{
			get 
			{ 
				return AllowFreeMovementButtonSetting.Value; 
			}

			set 
			{ 
				AllowFreeMovementButtonSetting.Value = value; 
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the cursor may return freely to the primary monitor
		/// </summary>
		public bool PrimaryReturnUnhindered
		{
			get 
			{ 
				return PrimaryReturnUnhinderedSetting.Value; 
			}

			set 
			{ 
				PrimaryReturnUnhinderedSetting.Value = value; 
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the cursor may jump dead space 
		/// when moving horizontally to find a monitor
		/// </summary>
		public bool AllowDeadSpaceXJump
		{
			get
			{
				return AllowDeadSpaceXJumpSetting.Value;
			}

			set
			{
				AllowDeadSpaceXJumpSetting.Value = value;
				ReflectNewCursorOptions();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the cursor may jump dead space 
		/// when moving vertically to find a monitor
		/// </summary>
		public bool AllowDeadSpaceYJump
		{
			get
			{
				return AllowDeadSpaceYJumpSetting.Value;
			}

			set
			{
				AllowDeadSpaceYJumpSetting.Value = value;
				ReflectNewCursorOptions();
			}
		}

		/// <summary>
		/// Gets or sets the free movement key
		/// </summary>
		public Keys FreeMovementKey
		{
			get 
			{ 
				return (Keys)FreeMovementKeySetting.Value; 
			}

			set 
			{ 
				FreeMovementKeySetting.Value = (int)value; 
			}
		}

		/// <summary>
		/// Gets or sets the free movement button
		/// </summary>
		public MouseButtons FreeMovementButton
		{
			get 
			{ 
				return (MouseButtons)FreeMovementButtonSetting.Value; 
			}

			set 
			{ 
				FreeMovementButtonSetting.Value = (uint)value; 
			}
		}

		/// <summary>
		/// Gets or sets the default cursor mode
		/// </summary>
		public CursorType DefaultCursorMode
		{
			get 
			{ 
				return (CursorType)DefaultCursorModeSetting.Value; 
			}

			set 
			{ 
				DefaultCursorModeSetting.Value = (int)value;
			}
		}

		/// <summary>
		/// Gets or sets the list of monitors (zero based) in the primary group
		/// </summary>
		public List<int> PrimaryGroup
		{
			get
			{
				return _primaryGroup;
			}

			set
			{
				_primaryGroup = value;
				SetPrimaryGroupSetting();
			}
		}

		public EPrimaryGroupType PrimaryGroupType
		{
			get
			{
				return (EPrimaryGroupType)PrimaryGroupTypeSetting.Value;
			}

			set
			{
				PrimaryGroupTypeSetting.Value = (int)value;
			}
		}

		public string PrimaryGroupNames
		{
			get
			{
				return PrimaryGroupNamesSetting.Value;
			}

			set
			{
				PrimaryGroupNamesSetting.Value = value;
				// Note: this does not trigger changing the primary group
				// PrimaryGroup must be set explicitly if needed
			}
		}

		void SetPrimaryGroupSetting()
		{
			StringBuilder sb = new StringBuilder();
			foreach (int monIdx in _primaryGroup)
			{
				// we save monitor numbers (1 based)
				int monNum = monIdx + 1;
				if (sb.Length > 0)
				{
					sb.Append(",");
				}
				sb.Append(monNum);
			}

			PrimaryGroupNumsSetting.Value = sb.ToString();
		}

		/// <summary>
		/// Rebuilds PrimaryGroup from PrimaryGroupSetting
		/// </summary>
		void UpdatePrimaryGroupFromSetting()
		{
			List<int> primaryGroup;

			//if (PrimaryGroupTypeSetting.Value == (int)EPrimaryGroupType.Names)
			if (PrimaryGroupType == EPrimaryGroupType.Names)
			{
				primaryGroup = PrimaryGroupFromNames(PrimaryGroupNamesSetting.Value);
			}
			else
			{
				// default case
				primaryGroup = PrimaryGroupFromNums(PrimaryGroupNumsSetting.Value);
			}

			_primaryGroup = primaryGroup;
		}

		List<int> PrimaryGroupFromNums(string nums)
		{
			List<int> primaryGroup = new List<int>();

			string[] fields = nums.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			for (int n = 0; n < fields.Count(); n++)
			{
				int monNum = StringUtils.GetFieldAsInt(fields, n, 0);
				// monNum should be >= 1
				if (monNum >= 1)
				{
					// but we store zero based
					primaryGroup.Add(monNum - 1);
				}
			}

			return primaryGroup;
		}

		public List<int> PrimaryGroupFromNames(string names)
		{
			List<int> primaryGroup = new List<int>();

			// parse primary group names into list of monitor names
			string[] primaryNames = MonitorNamesParser.StringToList(names);

			// check name of each of the current monitors to see if it exists in primaryNames 
			DisplayDevices displayDevices = new DisplayDevices();
			for (int idx = 0; idx < displayDevices.Items.Count; idx++)
			{
				string monitorName = displayDevices.Items[idx].GetMonitorName();
				if (primaryNames.Contains(monitorName))
				{
					// yes, this is a primary monitor
					primaryGroup.Add(idx);
				}
			}

			return primaryGroup;
		}


		/// <summary>
		/// Gets a value indicating whether cursor movement is restricted (sticky or locked)
		/// </summary>
		bool CursorLocked
		{
			get { return _cursorHooks != null && _cursorHooks.CursorLocked; }
		}

		// The settings
		IntSetting MinStickyForceSetting { get; set; }

		BoolSetting AllowFreeMovementKeySetting { get; set; }

		BoolSetting AllowFreeMovementButtonSetting { get; set; }

		BoolSetting PrimaryReturnUnhinderedSetting { get; set; }

		IntSetting FreeMovementKeySetting { get; set; }

		UIntSetting FreeMovementButtonSetting { get; set; }

		IntSetting DefaultCursorModeSetting { get; set; }

		BoolSetting AllowDeadSpaceXJumpSetting { get; set; }
		BoolSetting AllowDeadSpaceYJumpSetting { get; set; }

		IntSetting PrimaryGroupTypeSetting { get; set; }
		StringSetting PrimaryGroupNumsSetting { get; set; }
		StringSetting PrimaryGroupNamesSetting { get; set; }

		/// <summary>
		/// Gets the option nodes for this module
		/// </summary>
		/// <returns>The root node</returns>
		public override ModuleOptionNode GetOptionNodes()
		{
			Image image = new Bitmap(Properties.Resources.cursor_16_16);
			ModuleOptionNodeBranch options = new ModuleOptionNodeBranch("Cursor", image, new CursorRootOptionsPanel());
			options.Nodes.Add(new ModuleOptionNodeLeaf("General", image, new CursorGeneralOptionsPanel(this)));
			options.Nodes.Add(new ModuleOptionNodeLeaf("Sticky", image, new CursorStickyOptionsPanel(this)));

			_cursorPrimaryOptionsPanel = new CursorPrimaryOptionsPanel(this);
			options.Nodes.Add(new ModuleOptionNodeLeaf("Primary Working Area", image, _cursorPrimaryOptionsPanel));

			options.Nodes.Add(new ModuleOptionNodeLeaf("Dead Space", image, new CursorDeadSpaceOptionsPanel(this)));

			return options;
		}

		/// <summary>
		/// Starts the module up
		/// </summary>
		public override void Start()
		{
			// hot keys
			FreeCursorHotKeyController = AddCommand("FreeCursor", CursorStrings.FreeCursorDescription, CursorStrings.FreeCursorWin7, FreeCursor);
			StickyCursorHotKeyController = AddCommand("StickyCursor", CursorStrings.StickyCursorDescription, CursorStrings.StickyCursorWin7, StickyCursor);
			LockCursorHotKeyController = AddCommand("LockCursor", CursorStrings.LockCursorDescription, CursorStrings.LockCursorWin7, LockCursor);
			CursorNextScreenHotKeyController = AddCommand("CursorToNextScreen", CursorStrings.CursorNextScreenDescription, CursorStrings.CursorNextScreenWin7, CursorToNextScreen);
			CursorPrevScreenHotKeyController = AddCommand("CursorToPrevScreen", CursorStrings.CursorPrevScreenDescription, CursorStrings.CursorPrevScreenWin7, CursorToPrevScreen);
			CursorToPrimaryScreenHotKeyController = AddCommand("CursorToPrimaryScreen", CursorStrings.CursorToPrimaryScreenDescription, CursorStrings.CursorToPrimaryScreenWin7, CursorToPrimaryScreen);
			LockCursorToMonitorHotKeyController = AddCommand("LockCursorToMonitor", CursorStrings.LockCursorToMonitorDescription, CursorStrings.LockCursorToMonitorWin7, ToggleLockCursorToMonitor);

			// init the other values from the settings
			MinStickyForceSetting = new IntSetting(_settingsService, ModuleName, "MinStickyForce");

			// The setting value for this should have been "AllowFreeMovement"
			AllowFreeMovementKeySetting = new BoolSetting(_settingsService, ModuleName, "ControlUnhindersCursor");
			FreeMovementKeySetting = new IntSetting(_settingsService, ModuleName, "FreeMovementKey");

			AllowFreeMovementButtonSetting = new BoolSetting(_settingsService, ModuleName, "AllowFreeMovementButton");
			FreeMovementButtonSetting = new UIntSetting(_settingsService, ModuleName, "FreeMovementButton");

			PrimaryReturnUnhinderedSetting = new BoolSetting(_settingsService, ModuleName, "PrimaryReturnUnhindered");

			DefaultCursorModeSetting = new IntSetting(_settingsService, ModuleName, "DefaultCursorMode");

			AllowDeadSpaceXJumpSetting = new BoolSetting(_settingsService, ModuleName, "AllowDeadSpaceXJump");
			AllowDeadSpaceYJumpSetting = new BoolSetting(_settingsService, ModuleName, "AllowDeadSpaceYJump");

			PrimaryGroupTypeSetting = new IntSetting(_settingsService, ModuleName, "PrimaryGroupType", (int)EPrimaryGroupType.Nums);
			PrimaryGroupNumsSetting = new StringSetting(_settingsService, ModuleName, "PrimaryGroup"); // Name used for backwards compatibility
			PrimaryGroupNamesSetting = new StringSetting(_settingsService, ModuleName, "PrimaryGroupNames");
			UpdatePrimaryGroupFromSetting();

			_cursorHooks.Start();
			InitCursorMode(DefaultCursorMode);
		}

		/// <summary>
		/// Terminates the module
		/// </summary>
		public override void Terminate()
		{
			// This will release the hooks if they are hooked
			_cursorHooks.Terminate();

			CurCursorType = CursorType.Free;
		}

		/// <summary>
		/// Called when the display resolution changes
		/// </summary>
		public override void DisplayResolutionChanged()
		{
			ReBuildBarriers();

			// also update the primary working area panel
			if (_cursorPrimaryOptionsPanel != null)
			{
				_cursorPrimaryOptionsPanel.ShowMonitorPreview();
			}
		}

		/// <summary>
		/// Callback from CursorHooks to allow it to set a new cursor position
		/// which needs to be performed from the UI thread
		/// </summary>
		/// <param name="pt"></param>
		public void SetCursorPoition(Point pt)
		{
			// make sure we are on the UI thread, as we can get called from the hook thread
			if (_appForm.InvokeRequired)
			{
				_appForm.BeginInvoke(new SetCursorPoitionDelegate(SetCursorPoition), new object[] { pt });
				return;
			}

			System.Windows.Forms.Cursor.Position = pt;
		}

		public Point GetCursorPosition()
		{
			// TODO: Is this thread safe?

			return System.Windows.Forms.Cursor.Position;
		}

		/// <summary>
		/// Set the cursor so that its movement is unhindered by the screen edges
		/// </summary>
		public void FreeCursor()
		{
			MinForce = 0;
			CurCursorType = CursorType.Free;
			ReflectNewCursorOptions();
		}

		/// <summary>
		/// Make the transition between the screens sticky.
		/// <para />
		/// Note: If the current cursor state is already sticky and a hotkey has not been defined to free the cursor
		/// then we toggle the sticky state off.  This is mainly for safety rather than functionality.
		/// </summary>
		public void StickyCursor()
		{
			if (CurCursorType == CursorType.Sticky && !HaveFreeCursorHotKey())
			{
				// force operation to toggle
				FreeCursor();
			}
			else
			{
				MinForce = MinStickyForce;
				CurCursorType = CursorType.Sticky;
				ReflectNewCursorOptions();
			}
		}

		/// <summary>
		/// Lock the cursor to the current screen.
		/// <para />
		/// Note: If the current cursor state is already locked and a hotkey has not been defined to free the cursor
		/// then we toggle the locked state off.  This is mainly for safety rather than functionality.
		/// </summary>
		public void LockCursor()
		{
			if (CurCursorType == CursorType.Lock && !HaveFreeCursorHotKey())
			{
				// force operation to toggle
				FreeCursor();
			}
			else
			{
				MinForce = int.MaxValue;
				CurCursorType = CursorType.Lock;
				ReflectNewCursorOptions();
			}
		}

		/// <summary>
		/// Toggles the locking of the cursor to the current monitor.
		/// </summary>
		public void ToggleLockCursorToMonitor()
		{
			CursorLockedToMonitor = !CursorLockedToMonitor;
			ReflectNewCursorOptions();
		}

		void InitCursorMode(CursorType initCursorType)
		{
			CurCursorType = initCursorType;
			MinForce = GetMinForceValue();

			ReflectNewCursorOptions();
		}

		int GetMinForceValue()
		{
			switch (CurCursorType)
			{
				case CursorType.Lock:
					return int.MaxValue;

				case CursorType.Sticky:
					return MinStickyForce;

				case CursorType.Free:
				default:
					return 0;
			}
		}

		/// <summary>
		/// Called when cursor type or dead space jump flag changed
		/// </summary>
		void ReflectNewCursorOptions()
		{
			_cursorHooks.ReflectNewCursorOptions();
		}

		bool HaveFreeCursorHotKey()
		{
			return FreeCursorHotKeyController.IsEnabled();
		}

		/// <summary>
		/// Move the cursor to the next screen.
		/// The cursors position relative to the edges of the screen it is on
		/// is maintained after it has been moved.
		/// </summary>
		void CursorToNextScreen()
		{
			CursorToDeltaScreen(1);
		}

		/// <summary>
		/// Move the cursor to the previous screen.
		/// The cursors position relative to the edges of the screen it is on
		/// is maintained after it has been moved.
		/// </summary>
		void CursorToPrevScreen()
		{
			CursorToDeltaScreen(-1);
		}

		/// <summary>
		/// Move the cursor to the primary screen and position it in the center.
		/// </summary>
		void CursorToPrimaryScreen()
		{
			bool wasLocked = CursorLocked;

			// find primary monitor
			Monitor primaryScreen = Monitor.AllMonitors.PrimaryMonitor;
			Point newCursorPosition = new Point(primaryScreen.Bounds.Width / 2, primaryScreen.Bounds.Height / 2);
			//if (wasLocked)
			//{
			//	_cursorHooks.UnLockCursor();
			//}

			System.Windows.Forms.Cursor.Position = newCursorPosition;
			if (wasLocked)
			{
				//_cursorHooks.LockCursorToScreen();
				// may be different screen, so rebuild barriers
				_cursorHooks.ReBuildBarriers(Cursor.Position);
			}
		}

		/// <summary>
		/// Called when display settings have changed.
		/// We need to capture this as the screen co-ordinates may have
		/// changed so we must rebuild the barriers.
		/// </summary>
		void DisplaySettingsChanged()
		{
			ReBuildBarriers();
		}

		// Move the cursor to another screen
		private void CursorToDeltaScreen(int deltaScreenIndex)
		{
			// TODO: check we really don't need to unlock the cursor before moving it
			bool wasLocked = CursorLocked;

			Point oldCursorPosition = System.Windows.Forms.Cursor.Position;
			//Screen curScreen = Screen.FromPoint(oldCursorPosition);
			//int curScreenIndex = ScreenHelper.FindScreenIndex(curScreen);
			int curScreenIndex = Monitor.AllMonitors.MonitorIndexFromPoint(oldCursorPosition);
			Monitor curScreen = Monitor.AllMonitors[curScreenIndex];
			int newScreenIndex = ScreenHelper.DeltaScreenIndex(curScreenIndex, deltaScreenIndex);
			if (newScreenIndex != curScreenIndex)
			{
				// want to position the cursor on this new screen in a position
				// that is relative to the position it was on the old screen.
				Debug.Assert(newScreenIndex >= 0 && newScreenIndex < Monitor.AllMonitors.Count, "Invalid screen index");
				Monitor newScreen = Monitor.AllMonitors[newScreenIndex];

				Scaler scaleX = new Scaler(curScreen.Bounds.Left, curScreen.Bounds.Right, newScreen.Bounds.Left, newScreen.Bounds.Right);
				Scaler scaleY = new Scaler(curScreen.Bounds.Top, curScreen.Bounds.Bottom, newScreen.Bounds.Top, newScreen.Bounds.Bottom);
				Point newCursorPosition = new Point(scaleX.DestFromSrc(oldCursorPosition.X), scaleY.DestFromSrc(oldCursorPosition.Y));
				Debug.Assert(newScreen.Bounds.Contains(newCursorPosition), "Screen does not contain position");
				//if (wasLocked)
				//{
				//	_cursorHooks.UnLockCursor();
				//}

				System.Windows.Forms.Cursor.Position = newCursorPosition;
				if (wasLocked)
				{
					//_cursorHooks.LockCursorToScreen();
					// may be different screen, so rebuild barriers
					_cursorHooks.ReBuildBarriers(Cursor.Position);
				}
			}
		}

		// rebuild the barriers based on the current cursor position
		void ReBuildBarriers()
		{
			// TODO: is it worth performing this test?
			if (_cursorHooks.AreBarriersNeeded())
			{
				_cursorHooks.ReBuildBarriers(System.Windows.Forms.Cursor.Position);
			}
		}
	}
}
