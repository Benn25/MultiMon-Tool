#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2023-2026  Gerald Evans
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

using DMT.Library.DisplayHandling;
using DMT.Library.Environment;
using DMT.Library.Wallpaper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DMT.Library.GuiUtils
{
	/// <summary>
	/// Helper to display an image of the monitor layout
	/// </summary>
	public static class MonitorPreviewHelper
	{
		public static int PosnToMonitor(PictureBox picPreview, int x, int y)
		{
			Monitors monitors = Monitor.AllMonitors;

			// get rectangle within the picture box where the monitors will be displayed
			Rectangle pictureBoxRect = new Rectangle(new Point(0, 0), picPreview.Size);
			Rectangle previewRect = ImageFitter.UnderStretch(monitors.Bounds.Size, pictureBoxRect);

			for (int monitorIndex = 0; monitorIndex < monitors.Count; monitorIndex++)
			{
				Rectangle screenRect = monitors[monitorIndex].Bounds; // position in desktop
				Rectangle previewScreen = WallpaperCompositor.CalcDestRect(monitors.Bounds, previewRect, screenRect);

				if (previewScreen.Contains(x, y))
				{
					return monitorIndex;
				}
			}

			return -1;	// outside bounds of all monitors
		}

		public static void ShowLayout(PictureBox picPreview, IList<DisplayDevice> displayDevices, List<int> selectedMonitors = null)
		{
			Monitors monitors = Monitor.AllMonitors;

			// get rectangle within the picture box where the monitors will be displayed
			Rectangle pictureBoxRect = new Rectangle(new Point(0, 0), picPreview.Size);
			Rectangle previewRect = ImageFitter.UnderStretch(monitors.Bounds.Size, pictureBoxRect);

			Image preview = new Bitmap(previewRect.Width, previewRect.Height);

			using (Graphics g = Graphics.FromImage(preview))
			{
				// This is needed or the monitor name comes out very blocky and looks bold
				g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

				for (int monitorIndex = 0; monitorIndex < monitors.Count; monitorIndex++)
				{
					string monitorName = ""; // "A long name to cause truncation to be tested";
					bool isUsersMonitorName = true;
					if (displayDevices != null)
					{
						string monitorId = displayDevices[monitorIndex].GetMonitorId();
						// get the users name for the monitor
						monitorName = Monitor.MonitorNames.GetName(monitorId);
						if (string.IsNullOrEmpty(monitorName))
						{
							// user has not set a name for the monitor, so use the brand/name
							monitorName = displayDevices[monitorIndex].FriendlyName;
							isUsersMonitorName = false;
						}
					}
					string monitorNum = string.Format("{0}", monitorIndex + 1);
					if (monitors[monitorIndex].Primary)
					{
						monitorNum += "P";
					}
					bool selected = false;
					if (selectedMonitors != null && selectedMonitors.Contains(monitorIndex))
					{
						selected = true;
					}
					ShowMonitor(monitors, monitorIndex, g, previewRect, monitorNum, monitorName, isUsersMonitorName, selected);
				}
			}

			// display preview
			if (picPreview.Image != null)
			{
				picPreview.Image.Dispose();
			}

			picPreview.Image = preview;
		}

		static void ShowMonitor(Monitors monitors, int monitorIndex, Graphics g, Rectangle previewRect, string monitorNum, string monitorName, bool isUsersMonitorName, bool selected)
		{
			// need to determine position of screen rect in the preview
			Rectangle screenRect = monitors[monitorIndex].Bounds; // position in desktop
			Rectangle picBoxRect = new Rectangle(new Point(0, 0), previewRect.Size); // target rectangle for entire dektop
			Rectangle previewScreen = WallpaperCompositor.CalcDestRect(monitors.Bounds, picBoxRect, screenRect);

			// TODO: look into this!
			previewScreen = new Rectangle(previewScreen.Left, previewScreen.Top, previewScreen.Width - 1, previewScreen.Height - 1);

			// draw border around screen
			Pen borderPen1 = Pens.Black;
			Pen borderPen2 = Pens.Black; // Pens.White;
			Brush textBrush = Brushes.Black; //  Brushes.White;
			//SolidBrush textBrush = new SolidBrush(Color.Black);
			if (selected)
			{
				borderPen1 = Pens.Blue; // Pens.Yellow;
				borderPen2 = Pens.Blue; // Pens.Yellow;
				textBrush = Brushes.Black; //  Brushes.Yellow;
			}

			// leave outermost pixels of image visible
			previewScreen.Inflate(-1, -1);
			g.DrawRectangle(borderPen1, previewScreen);
			previewScreen.Inflate(-1, -1);
			g.DrawRectangle(borderPen2, previewScreen);

			//// display the screen name centered in the screen
			//using (Font font = new Font("Arial", 24, FontStyle.Bold, GraphicsUnit.Point))
			//{
			//	StringFormat stringFormat = new StringFormat();
			//	stringFormat.Alignment = StringAlignment.Center;
			//	stringFormat.LineAlignment = StringAlignment.Center;
			//	g.DrawString(monitorNum, font, textBrush, previewScreen, stringFormat);
			//}

			// display the monitor number and name centered in the monitor
			int borderMargin = 2; // 2 pixel line + pixel either side, but have already deflated by 2
			int availabelWidth = previewScreen.Width - borderMargin * 2;
			string fontName = "Arial"; // "Microsoft Sans Serif";
			FontStyle nameFontStyle = isUsersMonitorName ? FontStyle.Regular : FontStyle.Italic;
			using (Font fontForNum = new Font(fontName, 24, FontStyle.Bold, GraphicsUnit.Point))
			using (Font fontForName = new Font(fontName, 8, nameFontStyle))
			{
				//Font fontForName = System.Drawing.SystemFonts.DialogFont;

				// we don't expect the number to need truncating!
				Size numSize = g.MeasureString(monitorNum, fontForNum, previewScreen.Width).ToSize();
				Size nameSize;
				string truncatedMonitorName = DisplayTextHelper.Truncate(monitorName, g, fontForName, availabelWidth, out nameSize);

				// height centering
				int lineSpacing = nameSize.Height / 2;
				lineSpacing = 0;
				int topMargin = (previewScreen.Height - numSize.Height - lineSpacing - nameSize.Height) / 2;

				// ideally, we want to display:
				// <Num> of height numSize.Height
				// spacing of nameSize.Height / 2
				// <Name> of nameSize.Height
				// 
				// but if the monitors are stacked vertically, this may result in the text overflowing the window
				// if this happens, we do not display the <name> 
				// - this happens on a per monitor basis, so may look inconsistent
				// - but better than the alternatives?

				if (topMargin < borderMargin)
				{
					// don't display the monitor name
					truncatedMonitorName = "";
					// and re-position the Number
					topMargin = (previewScreen.Height - numSize.Height) / 2;
				}

				// display the monitor number centered horizontally
				int numX = borderMargin + (availabelWidth - numSize.Width) / 2;
				g.DrawString(monitorNum, fontForNum, textBrush, previewScreen.X + numX, previewScreen.Y + topMargin);

				if (truncatedMonitorName.Length > 0)
				{
					// display the monitor name (possibly truncated) centered horizontally
					int nameX = borderMargin + (availabelWidth - nameSize.Width) / 2;
					g.DrawString(truncatedMonitorName, fontForName, textBrush, previewScreen.X + nameX, previewScreen.Y + topMargin + numSize.Height + lineSpacing);
				}
			}
		}
	}
}
