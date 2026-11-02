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

namespace DMT.Library.HotKeys
{
	using System;
	using System.Windows.Forms;

	using DMT.Library.Ui;
	using DMT.Resources;

	/// <summary>
	/// Dialog for editing a hotkey.
	/// The user can just press the key combination they want,
	/// or pick the modifiers and key by hand (for keys that Windows grabs first).
	/// </summary>
	class HotKeyForm : ContentDialog
	{
		// hotkey being edited
		HotKey _hotKey;

		ShortcutEditor _editor;
		ToggleSwitch _enableSwitch;
		InfoBar _errorBar;

		/// <summary>
		/// Initialises a new instance of the <see cref="HotKeyForm" /> class.
		/// </summary>
		/// <param name="hotKey">The hotkey to edit</param>
		/// <param name="decription">A short description of the function of the hotkey.</param>
		/// <param name="note">Further information relating to the hotkey.
		/// If the functionality of this hotkey is provided by Windows 7,
		/// then currently we use this note to advise the user of this fact.</param>
		internal HotKeyForm(HotKey hotKey, string decription, string note)
			: base("Edit shortcut")
		{
			_hotKey = hotKey;

			Body.Add(new TextBlock(decription, TextRole.Body, TextTone.Secondary));

			_editor = Body.Add(new ShortcutEditor(), 16);
			_editor.KeyCombo = hotKey.HotKeyCombo;
			_editor.InvalidKey += (s, message) => ShowError(message);
			_editor.KeyComboChanged += (s, e) =>
			{
				_enableSwitch.Checked = true;
				HideError();
			};

			_enableSwitch = new ToggleSwitch();
			_enableSwitch.Checked = hotKey.HotKeyCombo.Enabled || hotKey.HotKeyCombo.KeyCode == Keys.None;
			_enableSwitch.CheckedChanged += (s, e) => _editor.Enabled = _enableSwitch.Checked;
			_editor.Enabled = _enableSwitch.Checked;
			Body.Add(new SettingsCard("Shortcut enabled", null, null, _enableSwitch), 20);

			if (!string.IsNullOrEmpty(note))
			{
				Body.Add(new InfoBar(Severity.Info, null, note), 12);
			}

			_errorBar = Body.Add(new InfoBar(Severity.Error, null, string.Empty), 12);
			_errorBar.Visible = false;

			PrimaryButton.Text = "Save";
			PrimaryButton.Click += PrimaryButton_Click;
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			_editor.FocusCapture();
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

			if (keyCombo.Enabled && keyCombo.KeyCode == (Keys)0)
			{
				ShowError(CommonStrings.NoKey);
				return;
			}

			// attempt to register this hotkey
			if (!_hotKey.RegisterHotKey(keyCombo))
			{
				ShowError(CommonStrings.RegisterFail);
				return;
			}

			// hotkey is OK
			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
