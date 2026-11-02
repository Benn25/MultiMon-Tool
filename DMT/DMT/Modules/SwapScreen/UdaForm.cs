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

namespace DMT.Modules.SwapScreen
{
	using System;
	using System.Drawing;
	using System.Windows.Forms;

	using DMT.Library.GuiUtils;
	using DMT.Library.HotKeys;
	using DMT.Library.PInvoke;
	using DMT.Library.Ui;
	using DMT.Resources;

	/// <summary>
	/// Dialog to set up a custom zone (User Defined Area): its name, position and shortcut
	/// </summary>
	class UdaForm : ContentDialog
	{
		UdaController _udaController;

		ToggleSwitch _enableSwitch;
		TextBox _name;
		NumericUpDown _x;
		NumericUpDown _y;
		NumericUpDown _width;
		NumericUpDown _height;
		MonitorMap _map;
		ShortcutEditor _editor;
		InfoBar _errorBar;

		public UdaForm(UdaController udaController)
			: base("Custom zone", 480)
		{
			_udaController = udaController;

			_enableSwitch = new ToggleSwitch();
			_enableSwitch.CheckedChanged += (s, e) => UpdateEnableStatus();
			Body.Add(new SettingsCard("Zone enabled", "Turn off to keep the zone without its shortcut", Theme.GlyphLayout, _enableSwitch));

			Body.Add(new TextBlock("Name", TextRole.Caption, TextTone.Secondary), 16);
			_name = Body.Add(new TextBox { AccessibleName = "Name" }, 4);

			Body.Add(new TextBlock("Position and size (in pixels)", TextRole.Caption, TextTone.Secondary), 16);
			_x = MakeNumber("Left", -32000);
			_y = MakeNumber("Top", -32000);
			_width = MakeNumber("Width", 0);
			_height = MakeNumber("Height", 0);
			Body.Add(new HStack(
				_x, _y,
				new TextBlock("·", TextRole.Body, TextTone.Tertiary) { Wrap = false },
				_width, new TextBlock("×", TextRole.Body, TextTone.Tertiary) { Wrap = false }, _height), 4);

			WindowPicker picker = new WindowPicker();
			picker.InitControl(Properties.Resources.TargetCursor, Properties.Resources.WinNoCrossHairs, Properties.Resources.WinCrossHairs);
			picker.HoveredWindowChanged += Picker_HoveredWindowChanged;
			picker.WithLogicalWidth(48);
			picker.Height = LogicalToDeviceUnits(48);
			Body.Add(new HStack(picker, new TextBlock("Or drag this target onto any window to copy where it is", TextRole.Caption, TextTone.Secondary) { Wrap = false }) { Spacing = 12 }, 8);

			_map = Body.Add(new MonitorMap { MaxHeight = 160 }, 12);

			Body.Add(new TextBlock("Shortcut", TextRole.Caption, TextTone.Secondary), 16);
			_editor = Body.Add(new ShortcutEditor(), 4);
			_editor.InvalidKey += (s, message) => ShowError(message);
			_editor.KeyComboChanged += (s, e) => HideError();

			_errorBar = Body.Add(new InfoBar(Severity.Error, null, string.Empty), 12);
			_errorBar.Visible = false;

			PrimaryButton.Text = "Save";
			PrimaryButton.Click += PrimaryButton_Click;

			ShowCurrentValues();
		}

		NumericUpDown MakeNumber(string name, int minimum)
		{
			NumericUpDown number = new NumericUpDown { Minimum = minimum, Maximum = 32000, AccessibleName = name }.WithLogicalWidth(84);
			number.ValueChanged += (s, e) => _map?.Invalidate();
			return number;
		}

		void ShowCurrentValues()
		{
			_name.Text = _udaController.Description;
			SetRectangle(_udaController.Position);
			_editor.KeyCombo = _udaController.HotKey.HotKeyCombo;

			// a brand new zone starts switched on
			_enableSwitch.Checked = _udaController.HotKey.HotKeyCombo.Enabled || !_udaController.IsDefined;
			_map.SetZone(r => CurrentRectangle(), true);
			UpdateEnableStatus();
		}

		void SetRectangle(Rectangle rectangle)
		{
			_x.Value = Clamp(_x, rectangle.X);
			_y.Value = Clamp(_y, rectangle.Y);
			_width.Value = Clamp(_width, rectangle.Width);
			_height.Value = Clamp(_height, rectangle.Height);
		}

		static decimal Clamp(NumericUpDown number, int value)
		{
			return Math.Max(number.Minimum, Math.Min(number.Maximum, value));
		}

		Rectangle CurrentRectangle()
		{
			return new Rectangle((int)_x.Value, (int)_y.Value, (int)_width.Value, (int)_height.Value);
		}

		void Picker_HoveredWindowChanged(IntPtr hWnd)
		{
			NativeMethods.RECT rect;
			if (NativeMethods.GetWindowRect(hWnd, out rect))
			{
				SetRectangle(ScreenHelper.RectToRectangle(ref rect));
			}
		}

		void UpdateEnableStatus()
		{
			bool enable = _enableSwitch.Checked;
			_name.Enabled = enable;
			_x.Enabled = enable;
			_y.Enabled = enable;
			_width.Enabled = enable;
			_height.Enabled = enable;
			_editor.Enabled = enable;
		}

		void ShowError(string message)
		{
			_errorBar.Text = message;
			if (!_errorBar.Visible)
			{
				_errorBar.Visible = true;
				FitToContent();
			}
		}

		void HideError()
		{
			if (_errorBar.Visible)
			{
				_errorBar.Visible = false;
				FitToContent();
			}
		}

		void PrimaryButton_Click(object sender, EventArgs e)
		{
			KeyCombo keyCombo = _editor.KeyCombo;
			keyCombo.Enabled = _enableSwitch.Checked;

			Rectangle position = CurrentRectangle();
			if (position.Width <= 0 || position.Height <= 0)
			{
				ShowError(SwapScreenStrings.InvalidPosition);
				return;
			}

			if (keyCombo.Enabled && keyCombo.KeyCode == (Keys)0)
			{
				ShowError(CommonStrings.NoKey);
				return;
			}

			if (!_udaController.SetValues(keyCombo.ComboValue, position, _name.Text))
			{
				ShowError(CommonStrings.RegisterFail);
				return;
			}

			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
