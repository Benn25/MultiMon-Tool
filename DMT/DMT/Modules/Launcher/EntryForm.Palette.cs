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

namespace DMT.Modules.Launcher
{
	using System;
	using System.Drawing;
	using System.Runtime.InteropServices;
	using System.Windows.Forms;

	using DMT.Library.Ui;

	/// <summary>
	/// The look of the magic word entry window: a borderless, rounded command palette
	/// (the behaviour is in EntryForm.cs)
	/// </summary>
	partial class EntryForm
	{
		Glyph _searchGlyph;
		FluentButton _settingsButton;

		void ApplyPaletteStyle()
		{
			SuspendLayout();

			FormBorderStyle = FormBorderStyle.None;
			Text = "DMT - Launcher";
			BackColor = Theme.FlyoutBackground;
			ForeColor = Theme.TextPrimary;
			Size = new Size(LogicalToDeviceUnits(640), LogicalToDeviceUnits(380));
			MinimumSize = new Size(LogicalToDeviceUnits(560), LogicalToDeviceUnits(320));

			_searchGlyph = new Glyph(Theme.GlyphSearch, TextRole.IconLarge, TextTone.Secondary);
			Controls.Add(_searchGlyph);

			textBoxInput.BorderStyle = BorderStyle.None;
			textBoxInput.Font = new Font(Theme.TextFamily, 15f);
			textBoxInput.BackColor = Theme.FlyoutBackground;
			textBoxInput.ForeColor = Theme.TextPrimary;
			textBoxInput.PlaceholderText = "Type a magic word";

			pictureBoxIcon.SizeMode = PictureBoxSizeMode.Zoom;
			pictureBoxIcon.BackColor = Theme.FlyoutBackground;

			buttonOptions.Visible = false;
			_settingsButton = new FluentButton("Settings", Theme.GlyphSettings, ButtonKind.Subtle) { ShowIconOnly = true, TabStop = false };
			_settingsButton.Click += buttonOptions_Click;
			Controls.Add(_settingsButton);

			magicWordListBox.BorderStyle = BorderStyle.None;
			magicWordListBox.BackColor = Theme.FlyoutBackground;
			magicWordListBox.ForeColor = Theme.TextPrimary;

			MouseDown += Palette_MouseDown;
			_searchGlyph.MouseDown += Palette_MouseDown;

			ResumeLayout();
		}

		protected override CreateParams CreateParams
		{
			get
			{
				// a shadow, as there's no window frame
				const int CS_DROPSHADOW = 0x00020000;
				CreateParams cp = base.CreateParams;
				cp.ClassStyle |= CS_DROPSHADOW;
				return cp;
			}
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			int preference = DWMWCP_ROUND;
			DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			base.OnLayout(levent);
			if (_searchGlyph == null)
			{
				return;
			}

			int pad = LogicalToDeviceUnits(16);
			int headerHeight = LogicalToDeviceUnits(60);
			int buttonSize = LogicalToDeviceUnits(32);
			int iconSize = LogicalToDeviceUnits(28);

			Size glyphSize = _searchGlyph.GetPreferredSize(Size.Empty);
			_searchGlyph.SetBounds(pad + LogicalToDeviceUnits(4), (headerHeight - glyphSize.Height) / 2, glyphSize.Width, glyphSize.Height);

			_settingsButton.SetBounds(ClientSize.Width - pad - buttonSize, (headerHeight - buttonSize) / 2, buttonSize, buttonSize);
			pictureBoxIcon.SetBounds(_settingsButton.Left - LogicalToDeviceUnits(8) - iconSize, (headerHeight - iconSize) / 2, iconSize, iconSize);

			int textLeft = _searchGlyph.Right + LogicalToDeviceUnits(14);
			int textHeight = textBoxInput.PreferredHeight;
			textBoxInput.SetBounds(textLeft, (headerHeight - textHeight) / 2, Math.Max(0, pictureBoxIcon.Left - LogicalToDeviceUnits(8) - textLeft), textHeight);

			int listTop = headerHeight + LogicalToDeviceUnits(8);
			magicWordListBox.SetBounds(LogicalToDeviceUnits(8), listTop, ClientSize.Width - LogicalToDeviceUnits(16), Math.Max(0, ClientSize.Height - listTop - LogicalToDeviceUnits(8)));
			Invalidate();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// line between the search box and the results
			int y = LogicalToDeviceUnits(60);
			using (Pen pen = new Pen(Theme.Divider))
			{
				e.Graphics.DrawLine(pen, 0, y, ClientSize.Width, y);
			}

			// thin border, as there's no window frame (Windows 11 rounds the corners)
			using (Pen pen = new Pen(Theme.IsDark ? Color.FromArgb(0x45, 0x45, 0x45) : Color.FromArgb(0xD8, 0xD8, 0xD8)))
			{
				e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
			}
		}

		/// <summary>
		/// Without a title bar, the window is moved by dragging its background
		/// </summary>
		void Palette_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
			}
		}

		const int WM_NCLBUTTONDOWN = 0xA1;
		const int HTCAPTION = 2;
		const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
		const int DWMWCP_ROUND = 2;

		[DllImport("user32.dll")]
		static extern bool ReleaseCapture();

		[DllImport("user32.dll")]
		static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

		[DllImport("dwmapi.dll")]
		static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
	}
}
