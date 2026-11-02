#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2015  Gerald Evans
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

using System;
using System.Windows.Forms;
using DMT.Library.Ui;
using DMT.Resources;

namespace DMT.Modules.Cursor
{
	/// <summary>
	/// Settings that make the cursor stick at (or stop at) the edges of the screens
	/// </summary>
	class CursorStickyOptionsPanel : SettingsPage
	{
		CursorModule _cursorModule;

		public CursorStickyOptionsPanel(CursorModule cursorModule)
		{
			_cursorModule = cursorModule;

			AddText("Stop the cursor slipping onto the next screen by accident. With sticky edges you have to push a little harder to cross over; locked edges stop it completely.");

			AddHeader("Cursor mode");
			this.AddHotKey(_cursorModule.FreeCursorHotKeyController, "Move freely between screens", null, "");
			this.AddHotKey(_cursorModule.StickyCursorHotKeyController, "Sticky edges", null, Theme.GlyphPin);
			this.AddHotKey(_cursorModule.LockCursorHotKeyController, "Lock to the current screen", null, "");

			ComboBox mode = MakeCombo(260,
				new[] { CursorStrings.FreeCursorDescription, CursorStrings.StickyCursorDescription, CursorStrings.LockCursorDescription },
				CursorModeToString(_cursorModule.DefaultCursorMode));
			mode.SelectedIndexChanged += (s, e) => _cursorModule.DefaultCursorMode = CursorModeFromString(mode.SelectedItem.ToString());
			AddCard("When DMT starts", "The mode the cursor starts in", Theme.GlyphSettings, mode);

			AddHeader("Stickiness");
			Slider slider = new Slider { Minimum = 0, Maximum = 3000, SmallChange = 10, LargeChange = 100, AccessibleName = "Resistance at screen edges" };
			slider.Value = _cursorModule.MinStickyForce;
			slider.ValueChanged += (s, e) => _cursorModule.MinStickyForce = slider.Value;
			HStack sliderRow = new HStack(new TextBlock("Less", TextRole.Caption, TextTone.Secondary) { Wrap = false }, slider,
				new TextBlock("More", TextRole.Caption, TextTone.Secondary) { Wrap = false });
			AddCard("How hard you push to cross an edge", "Used when sticky edges are on", "", sliderRow);

			AddHeader("Getting past the edges");
			ComboBox key = MakeCombo(170,
				new[] { CommonStrings.LControlKey, CommonStrings.RControlKey, CommonStrings.LShiftKey, CommonStrings.RShiftKey },
				FreeMovementKeyToString(_cursorModule.FreeMovementKey));
			key.SelectedIndexChanged += (s, e) => _cursorModule.FreeMovementKey = FreeMovementKeyFromString(key.SelectedItem.ToString());
			ToggleSwitch keyToggle = new ToggleSwitch { Checked = _cursorModule.AllowFreeMovementKey };
			keyToggle.CheckedChanged += (s, e) => { _cursorModule.AllowFreeMovementKey = keyToggle.Checked; key.Enabled = keyToggle.Checked; };
			key.Enabled = keyToggle.Checked;
			AddCard("Move freely while holding a key", null, Theme.GlyphKeyboard, new HStack(key, keyToggle) { Spacing = 16 });

			ComboBox button = MakeCombo(170,
				new[] { CommonStrings.LeftMouseButton, CommonStrings.MidMouseButton, CommonStrings.RightMouseButton, CommonStrings.X1MouseButton, CommonStrings.X2MouseButton },
				FreeMovementButtonToString(_cursorModule.FreeMovementButton));
			button.SelectedIndexChanged += (s, e) => _cursorModule.FreeMovementButton = FreeMovementButtonFromString(button.SelectedItem.ToString());
			ToggleSwitch buttonToggle = new ToggleSwitch { Checked = _cursorModule.AllowFreeMovementButton };
			buttonToggle.CheckedChanged += (s, e) => { _cursorModule.AllowFreeMovementButton = buttonToggle.Checked; button.Enabled = buttonToggle.Checked; };
			button.Enabled = buttonToggle.Checked;
			AddCard("Move freely while holding a mouse button", "Handy when dragging windows between screens", Theme.GlyphMouse, new HStack(button, buttonToggle) { Spacing = 16 });

			ToggleSwitch primary = new ToggleSwitch { Checked = _cursorModule.PrimaryReturnUnhindered };
			primary.CheckedChanged += (s, e) => _cursorModule.PrimaryReturnUnhindered = primary.Checked;
			AddCard("Return freely to the primary area", "The cursor can always move back onto (and around) the primary working area", Theme.GlyphHome, primary);

			AddText("Sticky and locked edges treat the primary working area as a single screen.", TextTone.Tertiary);
		}

		ComboBox MakeCombo(int width, string[] items, string selected)
		{
			ComboBox combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat }.WithLogicalWidth(width);
			combo.Items.AddRange(items);
			combo.SelectedItem = selected;
			return combo;
		}

		static string FreeMovementKeyToString(Keys key)
		{
			if (key == Keys.RControlKey)
			{
				return CommonStrings.RControlKey;
			}
			else if (key == Keys.LShiftKey)
			{
				return CommonStrings.LShiftKey;
			}
			else if (key == Keys.RShiftKey)
			{
				return CommonStrings.RShiftKey;
			}

			// else if (key == Keys.LControlKey) - default case
			return CommonStrings.LControlKey;
		}

		static Keys FreeMovementKeyFromString(string keyName)
		{
			if (keyName == CommonStrings.RControlKey)
			{
				return Keys.RControlKey;
			}
			else if (keyName == CommonStrings.LShiftKey)
			{
				return Keys.LShiftKey;
			}
			else if (keyName == CommonStrings.RShiftKey)
			{
				return Keys.RShiftKey;
			}

			// else -  anything else - leave as left control
			return Keys.LControlKey;
		}

		static string FreeMovementButtonToString(MouseButtons mouseButton)
		{
			if (mouseButton == MouseButtons.Middle)
			{
				return CommonStrings.MidMouseButton;
			}
			else if (mouseButton == MouseButtons.Right)
			{
				return CommonStrings.RightMouseButton;
			}
			else if (mouseButton == MouseButtons.XButton1)
			{
				return CommonStrings.X1MouseButton;
			}
			else if (mouseButton == MouseButtons.XButton2)
			{
				return CommonStrings.X2MouseButton;
			}

			// else if (mouseButton == MouseButtons.Left) - default case
			return CommonStrings.LeftMouseButton;
		}

		static MouseButtons FreeMovementButtonFromString(string buttonName)
		{
			if (buttonName == CommonStrings.MidMouseButton)
			{
				return MouseButtons.Middle;
			}
			else if (buttonName == CommonStrings.RightMouseButton)
			{
				return MouseButtons.Right;
			}
			else if (buttonName == CommonStrings.X1MouseButton)
			{
				return MouseButtons.XButton1;
			}
			else if (buttonName == CommonStrings.X2MouseButton)
			{
				return MouseButtons.XButton2;
			}

			// else -  anything else - leave as left mouse button
			return MouseButtons.Left;
		}

		static string CursorModeToString(CursorModule.CursorType cursorMode)
		{
			if (cursorMode == CursorModule.CursorType.Sticky)
			{
				return CursorStrings.StickyCursorDescription;
			}
			else if (cursorMode == CursorModule.CursorType.Lock)
			{
				return CursorStrings.LockCursorDescription;
			}

			// else if (cursorMode == CursorModule.CursorType.Free) - default case
			return CursorStrings.FreeCursorDescription;
		}

		static CursorModule.CursorType CursorModeFromString(string cursorModeName)
		{
			if (cursorModeName == CursorStrings.StickyCursorDescription)
			{
				return CursorModule.CursorType.Sticky;
			}
			else if (cursorModeName == CursorStrings.LockCursorDescription)
			{
				return CursorModule.CursorType.Lock;
			}

			// else -  anything else - leave as free
			return CursorModule.CursorType.Free;
		}
	}
}
