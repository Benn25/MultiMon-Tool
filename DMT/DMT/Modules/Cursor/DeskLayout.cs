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

namespace DMT.Modules.Cursor
{
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.Globalization;
	using System.Linq;
	using System.Text;

	using DMT.Library.DisplayHandling;
	using DMT.Library.Environment;
	using Microsoft.Win32;

	/// <summary>
	/// A screen as it physically sits on the desk
	/// </summary>
	class DeskScreen
	{
		public const float MmPerInch = 25.4f;

		/// <summary>
		/// Windows' id for the monitor (so the layout survives the screens being renumbered)
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// The screen's bounds in desktop pixels
		/// </summary>
		public Rectangle Pixels { get; set; }

		/// <summary>
		/// Size the monitor reports for itself (in mm, already turned to match the screen's orientation), or empty
		/// </summary>
		public SizeF ReportedMm { get; set; }

		/// <summary>
		/// The diagonal the user has entered (inches), or 0 to use the reported size
		/// </summary>
		public float DiagonalOverride { get; set; }

		/// <summary>
		/// Top left corner on the desk (mm)
		/// </summary>
		public PointF PositionMm { get; set; }

		/// <summary>
		/// Width and height of the picture (mm)
		/// </summary>
		public SizeF SizeMm
		{
			get
			{
				float diagonalPixels = (float)Math.Sqrt((double)Pixels.Width * Pixels.Width + (double)Pixels.Height * Pixels.Height);
				float mmPerPixel;
				if (DiagonalOverride > 0)
				{
					mmPerPixel = DiagonalOverride * MmPerInch / diagonalPixels;
				}
				else if (ReportedMm.Width > 0 && ReportedMm.Height > 0)
				{
					// use the longer side, as pixels are square even if the reported sizes are a little off
					mmPerPixel = Math.Max(ReportedMm.Width, ReportedMm.Height) / Math.Max(Pixels.Width, Pixels.Height);
				}
				else
				{
					// a typical desktop monitor
					mmPerPixel = 0.25f;
				}

				return new SizeF(Pixels.Width * mmPerPixel, Pixels.Height * mmPerPixel);
			}
		}

		public float DiagonalInches
		{
			get
			{
				SizeF size = SizeMm;
				return (float)Math.Sqrt(size.Width * size.Width + size.Height * size.Height) / MmPerInch;
			}
		}

		public float ReportedDiagonalInches
		{
			get { return (float)Math.Sqrt(ReportedMm.Width * ReportedMm.Width + ReportedMm.Height * ReportedMm.Height) / MmPerInch; }
		}

		public RectangleF DeskRect
		{
			get { return new RectangleF(PositionMm, SizeMm); }
		}
	}

	/// <summary>
	/// The physical arrangement of the screens on the user's desk
	/// </summary>
	class DeskLayout
	{
		public DeskLayout(List<DeskScreen> screens)
		{
			Screens = screens;
		}

		/// <summary>
		/// The screens, in Monitor.AllMonitors order
		/// </summary>
		public List<DeskScreen> Screens { get; private set; }

		/// <summary>
		/// Builds the layout for the current monitors, using any saved positions and sizes
		/// </summary>
		public static DeskLayout ForCurrentMonitors(string setting)
		{
			Dictionary<string, string[]> saved = ParseSetting(setting);
			List<DeskScreen> screens = new List<DeskScreen>();
			Monitors monitors = Monitor.AllMonitors;

			DisplayDevices displayDevices = null;
			try
			{
				displayDevices = new DisplayDevices(false);
			}
			catch (Exception)
			{
				// we can still work without the ids, just not remember the layout so well
			}

			for (int i = 0; i < monitors.Count; i++)
			{
				Rectangle bounds = monitors[i].Bounds;
				DisplayDevice device = displayDevices != null ? displayDevices.FindMonitor(bounds) : null;
				string id = device != null ? device.GetMonitorId() : string.Empty;
				if (string.IsNullOrEmpty(id))
				{
					id = "#" + (i + 1);
				}

				DeskScreen screen = new DeskScreen { Id = id, Pixels = bounds, ReportedMm = ReadReportedSize(id, bounds) };
				string[] fields;
				if (saved.TryGetValue(id, out fields))
				{
					screen.DiagonalOverride = ParseFloat(fields, 1);
					screen.PositionMm = new PointF(ParseFloat(fields, 2), ParseFloat(fields, 3));
				}

				screens.Add(screen);
			}

			DeskLayout layout = new DeskLayout(screens);
			if (screens.Any(s => !saved.ContainsKey(s.Id)))
			{
				// a screen we haven't seen before, so start from how Windows has them arranged
				layout.ArrangeLikeWindows(screens.Where(s => saved.ContainsKey(s.Id)).ToList());
			}

			return layout;
		}

		/// <summary>
		/// Places the screens (other than those to keep) next to each other in the same way that Windows has them arranged
		/// </summary>
		public void ArrangeLikeWindows(List<DeskScreen> keep = null)
		{
			List<DeskScreen> placed = new List<DeskScreen>();
			if (keep != null && keep.Count > 0)
			{
				placed.AddRange(keep);
			}
			else
			{
				DeskScreen first = Screens.FirstOrDefault(s => s.Pixels.Contains(0, 0)) ?? Screens.FirstOrDefault();
				if (first == null)
				{
					return;
				}

				first.PositionMm = PointF.Empty;
				placed.Add(first);
			}

			// keep placing whichever screen is nearest to one that's already placed
			while (placed.Count < Screens.Count)
			{
				DeskScreen next = null;
				DeskScreen neighbour = null;
				long nearest = long.MaxValue;
				foreach (DeskScreen screen in Screens.Where(s => !placed.Contains(s)))
				{
					foreach (DeskScreen other in placed)
					{
						long distance = GapBetween(screen.Pixels, other.Pixels);
						if (distance < nearest)
						{
							nearest = distance;
							next = screen;
							neighbour = other;
						}
					}
				}

				PlaceNextTo(next, neighbour);
				placed.Add(next);
			}

			Normalise();
		}

		/// <summary>
		/// Puts a screen against the side of its neighbour that Windows has it on,
		/// offset along that side in proportion to how Windows has it offset
		/// </summary>
		static void PlaceNextTo(DeskScreen screen, DeskScreen neighbour)
		{
			Rectangle p = screen.Pixels;
			Rectangle n = neighbour.Pixels;
			RectangleF nDesk = neighbour.DeskRect;
			SizeF size = screen.SizeMm;
			float mmPerPixel = nDesk.Width / Math.Max(1, n.Width);

			float centreOffsetX = ((p.Left + p.Right) / 2f - (n.Left + n.Right) / 2f) * mmPerPixel;
			float centreOffsetY = ((p.Top + p.Bottom) / 2f - (n.Top + n.Bottom) / 2f) * mmPerPixel;
			float nCentreX = nDesk.Left + nDesk.Width / 2;
			float nCentreY = nDesk.Top + nDesk.Height / 2;

			int overlapX = Math.Min(p.Right, n.Right) - Math.Max(p.Left, n.Left);
			int overlapY = Math.Min(p.Bottom, n.Bottom) - Math.Max(p.Top, n.Top);
			bool sideBySide = overlapY > 0 || (overlapX <= 0 && Math.Abs(centreOffsetX) >= Math.Abs(centreOffsetY));

			if (sideBySide)
			{
				float x = p.Left >= n.Left ? nDesk.Right : nDesk.Left - size.Width;
				screen.PositionMm = new PointF(x, nCentreY + centreOffsetY - size.Height / 2);
			}
			else
			{
				float y = p.Top >= n.Top ? nDesk.Bottom : nDesk.Top - size.Height;
				screen.PositionMm = new PointF(nCentreX + centreOffsetX - size.Width / 2, y);
			}
		}

		static long GapBetween(Rectangle a, Rectangle b)
		{
			long dx = Math.Max(0, Math.Max(a.Left - b.Right, b.Left - a.Right));
			long dy = Math.Max(0, Math.Max(a.Top - b.Bottom, b.Top - a.Bottom));
			return dx * dx + dy * dy;
		}

		/// <summary>
		/// Moves everything so the layout starts at (0, 0)
		/// </summary>
		public void Normalise()
		{
			if (Screens.Count == 0)
			{
				return;
			}

			float left = Screens.Min(s => s.PositionMm.X);
			float top = Screens.Min(s => s.PositionMm.Y);
			foreach (DeskScreen screen in Screens)
			{
				screen.PositionMm = new PointF(screen.PositionMm.X - left, screen.PositionMm.Y - top);
			}
		}

		/// <summary>
		/// The area the screens take up on the desk (mm)
		/// </summary>
		public RectangleF Bounds
		{
			get
			{
				if (Screens.Count == 0)
				{
					return RectangleF.Empty;
				}

				RectangleF bounds = Screens[0].DeskRect;
				foreach (DeskScreen screen in Screens)
				{
					bounds = RectangleF.Union(bounds, screen.DeskRect);
				}

				return bounds;
			}
		}

		public DeskCursorMapper CreateMapper()
		{
			return new DeskCursorMapper(Screens.Select(s => s.Pixels).ToList(), Screens.Select(s => s.DeskRect).ToList());
		}

		/// <summary>
		/// Gets the setting to save, merging in any screens that aren't connected at the moment
		/// </summary>
		public string ToSetting(string oldSetting)
		{
			Dictionary<string, string> entries = new Dictionary<string, string>();
			foreach (KeyValuePair<string, string[]> old in ParseSetting(oldSetting))
			{
				entries[old.Key] = string.Join("|", old.Value);
			}

			foreach (DeskScreen screen in Screens)
			{
				entries[screen.Id] = string.Join("|",
					screen.Id.Replace("|", "/").Replace(";", ","),
					Format(screen.DiagonalOverride),
					Format(screen.PositionMm.X),
					Format(screen.PositionMm.Y));
			}

			return string.Join(";", entries.Values);
		}

		static Dictionary<string, string[]> ParseSetting(string setting)
		{
			Dictionary<string, string[]> saved = new Dictionary<string, string[]>();
			if (!string.IsNullOrEmpty(setting))
			{
				foreach (string entry in setting.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
				{
					string[] fields = entry.Split('|');
					if (fields.Length >= 4 && fields[0].Length > 0)
					{
						saved[fields[0]] = fields;
					}
				}
			}

			return saved;
		}

		static float ParseFloat(string[] fields, int index)
		{
			float value;
			if (index < fields.Length && float.TryParse(fields[index], NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value))
			{
				return value;
			}

			return 0;
		}

		static string Format(float value)
		{
			return value.ToString("0.#", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Reads the size the monitor reports in its EDID (the registry keeps a copy of it)
		/// </summary>
		static SizeF ReadReportedSize(string monitorId, Rectangle pixels)
		{
			try
			{
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\DISPLAY\" + monitorId + @"\Device Parameters"))
				{
					byte[] edid = key != null ? key.GetValue("EDID") as byte[] : null;
					SizeF size = ParseEdidSize(edid);
					if (size.IsEmpty)
					{
						return SizeF.Empty;
					}

					// the EDID is for the monitor in its normal (landscape) orientation
					bool portraitPixels = pixels.Height > pixels.Width;
					bool portraitSize = size.Height > size.Width;
					return portraitPixels == portraitSize ? size : new SizeF(size.Height, size.Width);
				}
			}
			catch (Exception)
			{
				return SizeF.Empty;
			}
		}

		/// <summary>
		/// Gets the picture size (mm) from an EDID block, or empty if it doesn't give one
		/// </summary>
		public static SizeF ParseEdidSize(byte[] edid)
		{
			if (edid == null || edid.Length < 128)
			{
				return SizeF.Empty;
			}

			// the first detailed timing descriptor has the size in mm
			int widthMm = edid[66] | ((edid[68] >> 4) << 8);
			int heightMm = edid[67] | ((edid[68] & 0x0F) << 8);
			bool isTiming = edid[54] != 0 || edid[55] != 0;
			if (isTiming && widthMm > 50 && heightMm > 50 && widthMm < 5000 && heightMm < 5000)
			{
				return new SizeF(widthMm, heightMm);
			}

			// otherwise the basic size in cm
			if (edid[21] > 5 && edid[22] > 5)
			{
				return new SizeF(edid[21] * 10, edid[22] * 10);
			}

			return SizeF.Empty;
		}
	}
}
