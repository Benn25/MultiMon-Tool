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
	using System.ComponentModel;
	using System.Drawing;
	using System.Windows.Forms;

	/// <summary>
	/// An entry in the navigation pane.
	/// It's a RadioButton so only one can be selected and arrow keys move between them.
	/// </summary>
	class NavItem : RadioButton, IThemeAware
	{
		bool _hover;
		bool _compact;
		ToolTip _toolTip;

		public NavItem(string text, string glyph, ToolTip toolTip)
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			// the AutomationId used by UI automation (handy for testing)
			Name = "NavItem";
			Text = text;
			GlyphText = glyph;
			_toolTip = toolTip;
			AutoSize = false;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string GlyphText { get; set; }

		/// <summary>
		/// Gets or sets whether only the icon is shown
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool Compact
		{
			get { return _compact; }
			set
			{
				_compact = value;
				_toolTip.SetToolTip(this, _compact ? Text : null);
				Invalidate();
			}
		}

		public void ApplyTheme()
		{
			Invalidate();
		}

		protected override void OnMouseEnter(EventArgs eventargs) { _hover = true; Invalidate(); base.OnMouseEnter(eventargs); }
		protected override void OnMouseLeave(EventArgs eventargs) { _hover = false; Invalidate(); base.OnMouseLeave(eventargs); }

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Theme.NavBackground);

			int s4 = LogicalToDeviceUnits(4);
			Rectangle pill = new Rectangle(s4, LogicalToDeviceUnits(2), Width - s4 * 2, Height - LogicalToDeviceUnits(4));
			if (Checked || _hover)
			{
				Theme.FillRounded(g, pill, s4, Checked ? Theme.SubtleFillSelected : Theme.SubtleFillHover);
			}

			if (Checked)
			{
				// the accent coloured indicator on the left
				int indicatorHeight = LogicalToDeviceUnits(16);
				Rectangle indicator = new Rectangle(pill.Left, (Height - indicatorHeight) / 2, LogicalToDeviceUnits(3), indicatorHeight);
				Theme.FillRounded(g, indicator, LogicalToDeviceUnits(2), Theme.Accent);
			}

			TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;
			Color color = Enabled ? Theme.TextPrimary : Theme.TextDisabled;
			Font iconFont = Theme.GetFont(TextRole.Icon, DeviceDpi);

			if (_compact)
			{
				TextRenderer.DrawText(g, GlyphText, iconFont, ClientRectangle, color, flags | TextFormatFlags.HorizontalCenter);
			}
			else
			{
				int iconLeft = LogicalToDeviceUnits(16);
				int iconSize = LogicalToDeviceUnits(16);
				TextRenderer.DrawText(g, GlyphText, iconFont, new Rectangle(iconLeft, 0, iconSize, Height), color, flags | TextFormatFlags.HorizontalCenter);

				int textLeft = iconLeft + iconSize + LogicalToDeviceUnits(14);
				TextRenderer.DrawText(g, Text, Theme.GetFont(TextRole.Body, DeviceDpi), new Rectangle(textLeft, 0, Width - textLeft - s4 * 2, Height), color,
					flags | TextFormatFlags.EndEllipsis);
			}

			if (Focused && ShowFocusCues)
			{
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				using (Pen pen = new Pen(Theme.TextPrimary, LogicalToDeviceUnits(1)))
				using (System.Drawing.Drawing2D.GraphicsPath path = Theme.RoundedRect(pill, s4))
				{
					g.DrawPath(pen, path);
				}
			}
		}
	}

	/// <summary>
	/// A heading for a group of navigation items (a thin divider when compact)
	/// </summary>
	class NavHeader : ThemedControl
	{
		public NavHeader(string text)
		{
			SetStyle(ControlStyles.Selectable, false);
			TabStop = false;
			Text = text;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool Compact { get; set; }

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Theme.NavBackground);
			if (Compact)
			{
				using (Pen pen = new Pen(Theme.Divider))
				{
					int y = Height / 2;
					g.DrawLine(pen, S(12), y, Width - S(12), y);
				}
			}
			else
			{
				Rectangle rect = new Rectangle(S(16), 0, Width - S(20), Height - S(4));
				TextRenderer.DrawText(g, Text, Theme.GetFont(TextRole.Caption, DeviceDpi), rect, Theme.TextSecondary,
					TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.Bottom | TextFormatFlags.EndEllipsis);
			}
		}
	}

	/// <summary>
	/// The scrolling column of navigation items
	/// </summary>
	class NavPane : Panel, IThemeAware
	{
		bool _compact;

		public NavPane()
		{
			SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
			AutoScroll = true;
			BackColor = Theme.NavBackground;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool Compact
		{
			get { return _compact; }
			set
			{
				_compact = value;
				foreach (Control child in Controls)
				{
					NavItem item = child as NavItem;
					if (item != null)
					{
						item.Compact = value;
					}

					NavHeader header = child as NavHeader;
					if (header != null)
					{
						header.Compact = value;
						header.Invalidate();
					}
				}

				PerformLayout();
			}
		}

		public void ApplyTheme()
		{
			BackColor = Theme.NavBackground;
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			int y = AutoScrollPosition.Y + LogicalToDeviceUnits(4);
			int width = ClientSize.Width;
			foreach (Control child in Controls)
			{
				int height;
				if (child is NavHeader)
				{
					height = LogicalToDeviceUnits(_compact ? 13 : 36);
				}
				else
				{
					height = LogicalToDeviceUnits(36);
				}

				child.SetBounds(LogicalToDeviceUnits(4), y, width - LogicalToDeviceUnits(8), height);
				y += height + LogicalToDeviceUnits(child is NavItem ? 2 : 2);
			}

			AutoScrollMinSize = new Size(0, y - AutoScrollPosition.Y + LogicalToDeviceUnits(8));
		}
	}
}
