#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2026
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
	using System.Drawing;
	using System.Runtime.InteropServices;
	using System.Windows.Forms;

	using DMT.Library.Ui;

	/// <summary>
	/// Lets the user pick a key combination, either by pressing it
	/// or by choosing the modifiers and key (for keys that Windows grabs first)
	/// </summary>
	class ShortcutEditor : VStack
	{
		KeyCaptureBox _capture;
		CheckBox _winCheck;
		CheckBox _ctrlCheck;
		CheckBox _altCheck;
		CheckBox _shiftCheck;
		ComboBox _keyCombo;
		bool _updating;

		public ShortcutEditor()
		{
			_capture = Add(new KeyCaptureBox());
			_capture.KeyComboCaptured += Capture_KeyComboCaptured;

			Add(new TextBlock("Or choose the keys", TextRole.Caption, TextTone.Secondary), 16);
			_winCheck = MakeModifierCheck("Win");
			_ctrlCheck = MakeModifierCheck("Ctrl");
			_altCheck = MakeModifierCheck("Alt");
			_shiftCheck = MakeModifierCheck("Shift");
			_keyCombo = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				MaxDropDownItems = 16,
				AccessibleName = "Key",
			}.WithLogicalWidth(150);
			foreach (VirtualKey virtualKey in VirtualKey.AllVirtualKeys)
			{
				_keyCombo.Items.Add(virtualKey.Name);
			}

			_keyCombo.SelectedIndexChanged += (s, e) => ManualChanged();
			Add(new HStack(_winCheck, _ctrlCheck, _altCheck, _shiftCheck, _keyCombo) { Spacing = 12 }, 6);
		}

		/// <summary>
		/// Raised with a message when the user presses a key that can't be used
		/// </summary>
		public event EventHandler<string> InvalidKey;

		/// <summary>
		/// Raised when the user changes the key combination
		/// </summary>
		public event EventHandler KeyComboChanged;

		/// <summary>
		/// The key combination chosen (the Enabled flag isn't set)
		/// </summary>
		[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
		public KeyCombo KeyCombo
		{
			get
			{
				KeyCombo keyCombo = new KeyCombo();
				if (_keyCombo.SelectedItem != null)
				{
					keyCombo.KeyCode = VirtualKey.NameToCode(_keyCombo.SelectedItem.ToString());
				}

				keyCombo.WinMod = _winCheck.Checked;
				keyCombo.ControlMod = _ctrlCheck.Checked;
				keyCombo.AltMod = _altCheck.Checked;
				keyCombo.ShiftMod = _shiftCheck.Checked;
				return keyCombo;
			}

			set
			{
				_updating = true;
				_winCheck.Checked = value.WinMod;
				_ctrlCheck.Checked = value.ControlMod;
				_altCheck.Checked = value.AltMod;
				_shiftCheck.Checked = value.ShiftMod;
				_keyCombo.SelectedItem = value.KeyCode == Keys.None ? null : VirtualKey.CodeToName(value.KeyCode);
				_updating = false;

				_capture.KeyCombo = value;
			}
		}

		/// <summary>
		/// Puts the focus in the box where the keys can be pressed
		/// </summary>
		public void FocusCapture()
		{
			_capture.Focus();
		}

		CheckBox MakeModifierCheck(string text)
		{
			CheckBox check = new CheckBox { Text = text, AutoSize = true };
			check.CheckedChanged += (s, e) => ManualChanged();
			return check;
		}

		void Capture_KeyComboCaptured(object sender, EventArgs e)
		{
			KeyCombo keyCombo = _capture.KeyCombo;
			if (VirtualKey.CodeToName(keyCombo.KeyCode) == "?")
			{
				if (InvalidKey != null)
				{
					InvalidKey(this, "That key can't be used for a shortcut. Pick one from the list instead.");
				}

				return;
			}

			KeyCombo = keyCombo;
			if (KeyComboChanged != null)
			{
				KeyComboChanged(this, EventArgs.Empty);
			}
		}

		void ManualChanged()
		{
			if (_updating)
			{
				return;
			}

			_capture.KeyCombo = KeyCombo;
			if (KeyComboChanged != null)
			{
				KeyComboChanged(this, EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// A box that records the key combination pressed while it has the focus
	/// </summary>
	class KeyCaptureBox : ThemedControl, IHeightForWidth
	{
		const int VK_LWIN = 0x5B;
		const int VK_RWIN = 0x5C;

		KeyChips _chips;
		KeyCombo _keyCombo;
		bool _hover;

		public KeyCaptureBox()
		{
			SetStyle(ControlStyles.Selectable, true);
			TabStop = true;
			AccessibleRole = AccessibleRole.Text;
			AccessibleName = "Press the new shortcut";
			_chips = new KeyChips { EmptyText = string.Empty };
			Controls.Add(_chips);
		}

		public event EventHandler KeyComboCaptured;

		[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
		public KeyCombo KeyCombo
		{
			get { return _keyCombo; }
			set
			{
				_keyCombo = value;
				_chips.SetKeys(UiHelper.KeyParts(value));
				AccessibleDescription = _chips.AccessibleName;
				PerformLayout();
				Invalidate();
			}
		}

		public int GetHeightForWidth(int width)
		{
			return S(96);
		}

		[DllImport("user32.dll")]
		static extern short GetKeyState(int nVirtKey);

		static bool IsWinDown()
		{
			return ((GetKeyState(VK_LWIN) | GetKeyState(VK_RWIN)) & 0x8000) != 0;
		}

		protected override bool IsInputKey(Keys keyData)
		{
			Keys key = keyData & Keys.KeyCode;
			if (keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift) || keyData == Keys.Escape || keyData == Keys.Enter)
			{
				// leave these for moving around and closing the dialog
				return false;
			}

			return true;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			e.Handled = true;
			e.SuppressKeyPress = true;

			KeyCombo keyCombo = new KeyCombo();
			keyCombo.ControlMod = e.Control;
			keyCombo.AltMod = e.Alt;
			keyCombo.ShiftMod = e.Shift;
			keyCombo.WinMod = IsWinDown();

			Keys key = e.KeyCode;
			bool modifierOnly = key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu || key == Keys.LWin || key == Keys.RWin
				|| key == Keys.LControlKey || key == Keys.RControlKey || key == Keys.LShiftKey || key == Keys.RShiftKey;

			if (modifierOnly)
			{
				// show the modifiers as they're pressed, but wait for the real key
				_chips.SetKeys(UiHelper.KeyParts(keyCombo, false));
				Invalidate();
				return;
			}

			keyCombo.KeyCode = key;
			_keyCombo = keyCombo;
			_chips.SetKeys(UiHelper.KeyParts(keyCombo));
			PerformLayout();
			Invalidate();
			if (KeyComboCaptured != null)
			{
				KeyComboCaptured(this, EventArgs.Empty);
			}
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			// let Alt+key combinations reach OnKeyDown rather than being treated as menu mnemonics
			const int WM_SYSKEYDOWN = 0x0104;
			if (msg.Msg == WM_SYSKEYDOWN && (keyData & Keys.KeyCode) != Keys.F4)
			{
				OnKeyDown(new KeyEventArgs(keyData));
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);
			// if only modifiers were pressed, go back to showing the current shortcut
			_chips.SetKeys(UiHelper.KeyParts(_keyCombo));
			PerformLayout();
			Invalidate();
		}

		protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
		protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
		protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
		protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; Invalidate(); }

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Focus();
		}

		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);
			_chips.Visible = Enabled;
			Invalidate();
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			Size size = _chips.GetPreferredSize(Size.Empty);
			int width = Math.Min(size.Width, Width - S(24));
			_chips.SetBounds((Width - width) / 2, S(22), width, size.Height);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(ParentBackColor);

			Color fill = !Enabled ? Theme.ControlFillPressed : (_hover && !Focused ? Theme.ControlFillHover : Theme.ControlFill);
			Theme.FillRounded(g, ClientRectangle, S(6), fill, Focused ? Theme.Accent : Theme.ControlStroke);
			if (Focused)
			{
				// a thicker accent line along the bottom, like a focused text box
				Rectangle bottom = new Rectangle(S(1), Height - S(2), Width - S(2), S(2));
				using (SolidBrush brush = new SolidBrush(Theme.Accent))
				{
					g.FillRectangle(brush, bottom);
				}
			}

			string hint;
			if (!Enabled)
			{
				hint = "Shortcut is turned off";
			}
			else if (Focused)
			{
				hint = "Press the keys you want to use";
			}
			else
			{
				hint = "Click here, then press the keys you want to use";
			}

			TextRenderer.DrawText(g, hint, Theme.GetFont(TextRole.Caption, DeviceDpi), new Rectangle(0, Height - S(30), Width, S(20)),
				Enabled ? Theme.TextSecondary : Theme.TextDisabled, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
		}
	}
}
