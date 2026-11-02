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
	using System.Drawing;
	using System.Runtime.InteropServices;
	using System.Windows.Forms;

	/// <summary>
	/// Applies the theme across the application:
	/// menus are drawn by our renderer, and every window gets a title bar that matches the theme.
	/// </summary>
	static class ThemeManager
	{
		static readonly HashSet<IntPtr> _themedWindows = new HashSet<IntPtr>();

		/// <summary>
		/// Call once at startup (before any windows are created)
		/// </summary>
		public static void Install()
		{
			ToolStripManager.Renderer = new MenuRenderer();
			Application.Idle += Application_Idle;
			Theme.Changed += Theme_Changed;
		}

		/// <summary>
		/// Gives a context menu (and its sub menus) the modern look
		/// </summary>
		public static void StyleMenu(ToolStripDropDown menu)
		{
			menu.Opening += (s, e) => PrepareMenu(menu);
			menu.Opened += (s, e) => RoundCorners(menu);
		}

		static void PrepareMenu(ToolStripDropDown menu)
		{
			menu.Font = Theme.DefaultFont;
			menu.Padding = new Padding(0, menu.LogicalToDeviceUnits(4), 0, menu.LogicalToDeviceUnits(4));
			foreach (ToolStripItem item in menu.Items)
			{
				if (!(item is ToolStripSeparator))
				{
					item.Padding = new Padding(menu.LogicalToDeviceUnits(4), menu.LogicalToDeviceUnits(5), menu.LogicalToDeviceUnits(12), menu.LogicalToDeviceUnits(5));
				}

				ToolStripMenuItem menuItem = item as ToolStripMenuItem;
				if (menuItem != null && menuItem.HasDropDownItems && menuItem.Tag as string != "styled")
				{
					menuItem.Tag = "styled";
					StyleMenu(menuItem.DropDown);
				}
			}
		}

		static void RoundCorners(Control control)
		{
			if (control.IsHandleCreated)
			{
				int preference = DWMWCP_ROUND;
				DwmSetWindowAttribute(control.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
			}
		}

		static void Application_Idle(object sender, EventArgs e)
		{
			// give any new windows a title bar to match the theme
			foreach (Form form in Application.OpenForms)
			{
				if (form.IsHandleCreated && _themedWindows.Add(form.Handle))
				{
					Theme.ApplyWindowChrome(form);
					IntPtr handle = form.Handle;
					form.HandleDestroyed += (s, a) => _themedWindows.Remove(handle);
				}
			}
		}

		static void Theme_Changed(object sender, EventArgs e)
		{
			ToolStripManager.Renderer = new MenuRenderer();
			_themedWindows.Clear();
		}

		const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
		const int DWMWCP_ROUND = 2;

		[DllImport("dwmapi.dll")]
		static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

		/// <summary>
		/// Draws menus in the style of Windows 11
		/// </summary>
		class MenuRenderer : ToolStripProfessionalRenderer
		{
			public MenuRenderer()
			{
				RoundedEdges = false;
			}

			static Color MenuBackground
			{
				get { return Theme.IsDark ? Color.FromArgb(0x2C, 0x2C, 0x2C) : Color.FromArgb(0xF9, 0xF9, 0xF9); }
			}

			protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
			{
				if (e.ToolStrip is ToolStripDropDown)
				{
					using (SolidBrush brush = new SolidBrush(MenuBackground))
					{
						e.Graphics.FillRectangle(brush, e.AffectedBounds);
					}
				}
				else
				{
					base.OnRenderToolStripBackground(e);
				}
			}

			protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
			{
				if (e.ToolStrip is ToolStripDropDown)
				{
					Rectangle rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
					using (Pen pen = new Pen(Theme.IsDark ? Color.FromArgb(0x3C, 0x3C, 0x3C) : Color.FromArgb(0xE0, 0xE0, 0xE0)))
					{
						e.Graphics.DrawRectangle(pen, rect);
					}
				}
				else
				{
					base.OnRenderToolStripBorder(e);
				}
			}

			protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
			{
				// the margin is the same colour as the rest of the menu
			}

			protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
			{
				if (!e.Item.Selected || !e.Item.Enabled)
				{
					return;
				}

				int inset = e.ToolStrip.LogicalToDeviceUnits(4);
				Rectangle rect = new Rectangle(inset, 1, e.Item.Width - inset * 2, e.Item.Height - 2);
				Theme.FillRounded(e.Graphics, rect, e.ToolStrip.LogicalToDeviceUnits(4), Theme.IsDark ? Color.FromArgb(0x38, 0x38, 0x38) : Color.FromArgb(0xEA, 0xEA, 0xEA));
			}

			protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
			{
				e.TextColor = e.Item.Enabled ? Theme.TextPrimary : Theme.TextDisabled;
				base.OnRenderItemText(e);
			}

			protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
			{
				e.ArrowColor = e.Item != null && e.Item.Enabled ? Theme.TextSecondary : Theme.TextDisabled;
				base.OnRenderArrow(e);
			}

			protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
			{
				int inset = e.ToolStrip.LogicalToDeviceUnits(4);
				int y = e.Item.Height / 2;
				using (Pen pen = new Pen(Theme.IsDark ? Color.FromArgb(0x3D, 0x3D, 0x3D) : Color.FromArgb(0xE0, 0xE0, 0xE0)))
				{
					e.Graphics.DrawLine(pen, inset, y, e.Item.Width - inset, y);
				}
			}

			protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
			{
				Rectangle rect = e.ImageRectangle;
				TextRenderer.DrawText(e.Graphics, Theme.GlyphSuccess, Theme.GetFont(TextRole.Icon, e.ToolStrip.DeviceDpi), rect, Theme.TextPrimary,
					TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
			}
		}
	}
}
