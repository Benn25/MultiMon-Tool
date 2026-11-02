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

namespace DMT.Library.Ui
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Drawing;
	using System.Windows.Forms;

	using DMT.Library.DisplayHandling;
	using DMT.Library.Environment;
	using DMT.Library.Extensions;

	/// <summary>
	/// Helpers for describing the monitors to the user
	/// </summary>
	static class MonitorDescriptions
	{
		/// <summary>
		/// Gets a name for each monitor (in Monitor.AllMonitors order):
		/// the name the user has given it, otherwise its brand name
		/// </summary>
		public static string[] GetNames()
		{
			Monitors monitors = Monitor.AllMonitors;
			string[] names = new string[monitors.Count];
			try
			{
				DisplayDevices displayDevices = new DisplayDevices(false);
				for (int i = 0; i < monitors.Count; i++)
				{
					DisplayDevice device = displayDevices.FindMonitor(monitors[i].Bounds);
					if (device != null)
					{
						string userName = Monitor.MonitorNames != null ? Monitor.MonitorNames.GetName(device.GetMonitorId()) : null;
						names[i] = !string.IsNullOrEmpty(userName) ? userName : device.FriendlyName;
					}
				}
			}
			catch (Exception)
			{
				// names are just a nicety
			}

			for (int i = 0; i < names.Length; i++)
			{
				if (string.IsNullOrEmpty(names[i]) || names[i] == "Unknown")
				{
					names[i] = string.Empty;
				}
			}

			return names;
		}
	}

	/// <summary>
	/// Draws the monitors in their real arrangement.
	/// Can label each monitor and highlight an area on each of them.
	/// </summary>
	class MonitorMap : ThemedControl, IHeightForWidth
	{
		string[] _names = new string[0];
		string[] _labels;
		Func<Rectangle, Rectangle> _zone;
		bool _zoneSpansAll;
		List<int> _selected = new List<int>();
		List<Rectangle> _screenRects = new List<Rectangle>();
		int _hoverIndex = -1;
		bool _clickable;

		public MonitorMap()
		{
			SetStyle(ControlStyles.Selectable, false);
			TabStop = false;
			Refresh();
		}

		/// <summary>
		/// Raised when a monitor is clicked (if Clickable)
		/// </summary>
		public event EventHandler<int> MonitorClicked;

		/// <summary>
		/// The tallest the map will be (logical pixels)
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int MaxHeight { get; set; } = 300;

		/// <summary>
		/// Gets or sets whether the monitors can be clicked
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool Clickable
		{
			get { return _clickable; }
			set { _clickable = value; Cursor = value ? Cursors.Hand : Cursors.Default; _hoverIndex = -1; Invalidate(); }
		}

		/// <summary>
		/// Indexes of the monitors shown as selected
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<int> Selected
		{
			get { return _selected; }
			set { _selected = value ?? new List<int>(); Invalidate(); }
		}

		int MonitorAt(Point point)
		{
			for (int i = 0; i < _screenRects.Count; i++)
			{
				if (_screenRects[i].Contains(point))
				{
					return i;
				}
			}

			return -1;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			int index = _clickable ? MonitorAt(e.Location) : -1;
			if (index != _hoverIndex)
			{
				_hoverIndex = index;
				Invalidate();
			}
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			_hoverIndex = -1;
			Invalidate();
		}

		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			if (_clickable && e.Button == MouseButtons.Left)
			{
				int index = MonitorAt(e.Location);
				if (index >= 0 && MonitorClicked != null)
				{
					MonitorClicked(this, index);
				}
			}
		}

		/// <summary>
		/// Re-reads the monitor names (call after the monitors change)
		/// </summary>
		public new void Refresh()
		{
			_names = MonitorDescriptions.GetNames();
			if (Parent != null)
			{
				Parent.PerformLayout();
			}

			Invalidate();
		}

		/// <summary>
		/// Text shown as key caps in the corner of each monitor (or null for none)
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string[] Labels
		{
			get { return _labels; }
			set { _labels = value; Invalidate(); }
		}

		/// <summary>
		/// Highlights the area produced by the function on each monitor's working area
		/// </summary>
		public void SetZone(Func<Rectangle, Rectangle> zone, bool spansAllMonitors = false)
		{
			_zone = zone;
			_zoneSpansAll = spansAllMonitors;
			Invalidate();
		}

		public int GetHeightForWidth(int width)
		{
			Rectangle desktop = Monitor.AllMonitors.Bounds;
			if (desktop.Width <= 0)
			{
				return S(160);
			}

			int height = (int)((long)width * desktop.Height / desktop.Width);
			return Math.Max(Math.Min(S(150), S(MaxHeight)), Math.Min(height, S(MaxHeight)));
		}

		/// <summary>
		/// Maps a rectangle in desktop co-ordinates onto the control
		/// </summary>
		Rectangle ToClient(Rectangle desktopRect, Rectangle desktop, Rectangle area, float scale)
		{
			int x = area.Left + (int)Math.Round((desktopRect.Left - desktop.Left) * scale);
			int y = area.Top + (int)Math.Round((desktopRect.Top - desktop.Top) * scale);
			int right = area.Left + (int)Math.Round((desktopRect.Right - desktop.Left) * scale);
			int bottom = area.Top + (int)Math.Round((desktopRect.Bottom - desktop.Top) * scale);
			return new Rectangle(x, y, right - x, bottom - y);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(ParentBackColor);

			Monitors monitors = Monitor.AllMonitors;
			Rectangle desktop = monitors.Bounds;
			if (desktop.Width <= 0 || desktop.Height <= 0)
			{
				return;
			}

			Rectangle area = Rectangle.Inflate(ClientRectangle, -S(4), -S(4));
			float scale = Math.Min((float)area.Width / desktop.Width, (float)area.Height / desktop.Height);
			Size used = new Size((int)(desktop.Width * scale), (int)(desktop.Height * scale));
			area = new Rectangle(area.Left + (area.Width - used.Width) / 2, area.Top + (area.Height - used.Height) / 2, used.Width, used.Height);

			int gap = S(3);
			Font numberFont = Theme.GetFont(TextRole.Subtitle, DeviceDpi);
			Font nameFont = Theme.GetFont(TextRole.Caption, DeviceDpi);
			TextFormatFlags center = TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

			_screenRects.Clear();
			for (int i = 0; i < monitors.Count; i++)
			{
				Rectangle screen = Rectangle.Inflate(ToClient(monitors[i].Bounds, desktop, area, scale), -gap, -gap);
				_screenRects.Add(screen);
				bool selected = _selected.Contains(i);
				Color fill = selected ? Color.FromArgb(Theme.IsDark ? 70 : 45, Theme.Accent) : (i == _hoverIndex ? Theme.ControlFillHover : Theme.ControlFill);
				Theme.FillRounded(g, screen, S(6), Theme.ControlFill);
				Theme.FillRounded(g, screen, S(6), fill, selected ? Theme.Accent : (i == _hoverIndex ? Theme.TextSecondary : Theme.ControlStroke));
				if (selected)
				{
					Theme.FillRounded(g, Rectangle.Inflate(screen, -S(1), -S(1)), S(5), Color.Transparent, Theme.Accent);
				}

				if (_zone != null && !_zoneSpansAll)
				{
					Rectangle zone = ToClient(_zone(monitors[i].WorkingArea), desktop, area, scale);
					zone.Intersect(screen);
					zone = Rectangle.Inflate(zone, -S(3), -S(3));
					Theme.FillRounded(g, zone, S(4), Color.FromArgb(Theme.IsDark ? 150 : 110, Theme.Accent), Theme.Accent);
				}

				// number and name in the middle
				string number = (i + 1).ToString();
				Size numberSize = TextRenderer.MeasureText(number, numberFont, Size.Empty, TextFormatFlags.NoPadding);
				string name = i < _names.Length ? _names[i] : string.Empty;
				int nameHeight = string.IsNullOrEmpty(name) ? 0 : TextRenderer.MeasureText(name, nameFont, Size.Empty, TextFormatFlags.NoPadding).Height;
				bool showName = nameHeight > 0 && screen.Height > numberSize.Height + nameHeight + S(48);
				int blockHeight = numberSize.Height + (showName ? nameHeight + S(2) : 0);
				int top = screen.Top + (screen.Height - blockHeight) / 2;
				TextRenderer.DrawText(g, number, numberFont, new Rectangle(screen.Left, top, screen.Width, numberSize.Height), Theme.TextPrimary, center);
				if (showName)
				{
					TextRenderer.DrawText(g, name, nameFont, new Rectangle(screen.Left + S(6), top + numberSize.Height + S(2), screen.Width - S(12), nameHeight), Theme.TextSecondary, center);
				}

				// key cap label in the top left corner
				if (_labels != null && i < _labels.Length && !string.IsNullOrEmpty(_labels[i]))
				{
					DrawKeyCap(g, _labels[i], new Point(screen.Left + S(8), screen.Top + S(8)), screen.Right - S(8));
				}
			}

			if (_zone != null && _zoneSpansAll)
			{
				Rectangle zone = Rectangle.Inflate(ToClient(_zone(monitors.WorkingArea), desktop, area, scale), -S(3), -S(3));
				Theme.FillRounded(g, zone, S(6), Color.FromArgb(Theme.IsDark ? 120 : 90, Theme.Accent), Theme.Accent);
			}
		}

		void DrawKeyCap(Graphics g, string text, Point location, int maxRight)
		{
			Font font = Theme.GetFont(TextRole.Caption, DeviceDpi);
			Size size = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding);
			Rectangle cap = new Rectangle(location.X, location.Y, Math.Min(size.Width + S(12), maxRight - location.X), size.Height + S(8));
			if (cap.Width < S(16))
			{
				return;
			}

			Theme.FillRounded(g, cap, S(4), Theme.KeyCapFill, Theme.ControlStroke);
			TextRenderer.DrawText(g, text, font, cap, Theme.TextPrimary,
				TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
		}
	}

	/// <summary>
	/// A numeric keypad where each key shows the zone it moves the window to.
	/// Hovering (or focusing) a key raises HotKeyChanged so the zone can be previewed.
	/// </summary>
	class NumpadView : ThemedControl
	{
		int _hotKey = -1;

		public NumpadView()
		{
			SetStyle(ControlStyles.Selectable, true);
			TabStop = true;
			AccessibleRole = AccessibleRole.Grouping;
			AccessibleName = "Numeric keypad zones";
		}

		/// <summary>
		/// Raised when the key being previewed changes (-1 for none)
		/// </summary>
		public event EventHandler HotKeyChanged;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int HotKey
		{
			get { return _hotKey; }
			set
			{
				if (value != _hotKey)
				{
					_hotKey = value;
					Invalidate();
					if (HotKeyChanged != null)
					{
						HotKeyChanged(this, EventArgs.Empty);
					}
				}
			}
		}

		/// <summary>
		/// The area of a working area that each numpad key moves the window to
		/// </summary>
		public static Rectangle ZoneFor(int key, Rectangle rect)
		{
			switch (key)
			{
				case 1: return rect.BottomLeftQuadrant();
				case 2: return rect.BottomHalf();
				case 3: return rect.BottomRightQuadrant();
				case 4: return rect.LeftHalf();
				case 6: return rect.RightHalf();
				case 7: return rect.TopLeftQuadrant();
				case 8: return rect.TopHalf();
				case 9: return rect.TopRightQuadrant();
				default: return rect;
			}
		}

		public static string ZoneName(int key)
		{
			switch (key)
			{
				case 0: return "Stretch across all screens";
				case 1: return "Bottom left quarter";
				case 2: return "Bottom half";
				case 3: return "Bottom right quarter";
				case 4: return "Left half";
				case 5: return "Full screen";
				case 6: return "Right half";
				case 7: return "Top left quarter";
				case 8: return "Top half";
				case 9: return "Top right quarter";
				default: return string.Empty;
			}
		}

		int KeySize { get { return S(48); } }
		int Gap { get { return S(6); } }

		public override Size GetPreferredSize(Size proposedSize)
		{
			return new Size(KeySize * 3 + Gap * 2, KeySize * 4 + Gap * 3);
		}

		Rectangle KeyRect(int key)
		{
			if (key == 0)
			{
				return new Rectangle(0, (KeySize + Gap) * 3, KeySize * 2 + Gap, KeySize);
			}

			int col = (key - 1) % 3;
			int row = 2 - (key - 1) / 3;
			return new Rectangle(col * (KeySize + Gap), row * (KeySize + Gap), KeySize, KeySize);
		}

		int KeyAt(Point point)
		{
			for (int key = 0; key <= 9; key++)
			{
				if (KeyRect(key).Contains(point))
				{
					return key;
				}
			}

			return -1;
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			HotKey = KeyAt(e.Location);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			if (!Focused)
			{
				HotKey = -1;
			}
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			if (_hotKey < 0)
			{
				HotKey = 5;
			}

			Invalidate();
		}

		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			HotKey = -1;
		}

		protected override bool IsInputKey(Keys keyData)
		{
			Keys key = keyData & Keys.KeyCode;
			return (key >= Keys.NumPad0 && key <= Keys.NumPad9) || (key >= Keys.D0 && key <= Keys.D9)
				|| key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down || base.IsInputKey(keyData);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
			{
				HotKey = e.KeyCode - Keys.NumPad0;
			}
			else if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)
			{
				HotKey = e.KeyCode - Keys.D0;
			}
			else if (_hotKey > 0)
			{
				int col = (_hotKey - 1) % 3;
				int row = (_hotKey - 1) / 3;
				switch (e.KeyCode)
				{
					case Keys.Left: col = Math.Max(0, col - 1); break;
					case Keys.Right: col = Math.Min(2, col + 1); break;
					case Keys.Up: row = Math.Min(2, row + 1); break;
					case Keys.Down: row = Math.Max(0, row - 1); break;
				}

				HotKey = row * 3 + col + 1;
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(ParentBackColor);
			Font font = Theme.GetFont(TextRole.Caption, DeviceDpi);

			for (int key = 0; key <= 9; key++)
			{
				Rectangle rect = KeyRect(key);
				bool hot = key == _hotKey;

				Rectangle shadow = rect;
				shadow.Offset(0, S(2));
				Theme.FillRounded(g, shadow, S(5), Theme.ControlStroke);
				Theme.FillRounded(g, rect, S(5), hot ? Theme.Accent : Theme.KeyCapFill, hot ? Theme.Accent : Theme.ControlStroke);

				Color textColor = hot ? Theme.OnAccent : Theme.TextPrimary;
				TextRenderer.DrawText(g, key.ToString(), font, new Rectangle(rect.Left + S(6), rect.Top + S(4), S(16), S(16)), textColor,
					TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

				// mini screen showing the zone
				Rectangle mini = key == 0
					? new Rectangle(rect.Left + S(28), rect.Top + S(18), rect.Width - S(36), rect.Height - S(26))
					: new Rectangle(rect.Left + S(14), rect.Top + S(20), rect.Width - S(22), rect.Height - S(28));
				Color line = hot ? Theme.OnAccent : Theme.TextSecondary;
				using (Pen pen = new Pen(line))
				{
					g.DrawRectangle(pen, mini.Left, mini.Top, mini.Width - 1, mini.Height - 1);
				}

				Rectangle zone = ZoneFor(key, new Rectangle(mini.Left + S(2), mini.Top + S(2), mini.Width - S(4), mini.Height - S(4)));
				using (SolidBrush brush = new SolidBrush(line))
				{
					g.FillRectangle(brush, zone);
				}

				if (key == 0)
				{
					// a divider shows that the zone spans the screens
					using (Pen pen = new Pen(hot ? Theme.Accent : Theme.KeyCapFill, S(2)))
					{
						int mid = mini.Left + mini.Width / 2;
						g.DrawLine(pen, mid, mini.Top + S(2), mid, mini.Bottom - S(3));
					}
				}
			}

			if (Focused && ShowFocusCues && _hotKey >= 0)
			{
				Rectangle rect = Rectangle.Inflate(KeyRect(_hotKey), S(2), S(2));
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				using (Pen pen = new Pen(Theme.TextPrimary, S(1)))
				using (System.Drawing.Drawing2D.GraphicsPath path = Theme.RoundedRect(rect, S(6)))
				{
					g.DrawPath(pen, path);
				}
			}
		}
	}
}
