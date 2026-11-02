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
	using System.Drawing.Text;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Windows.Forms;

	using Microsoft.Win32;

	/// <summary>
	/// The role a piece of text plays, which determines its font
	/// </summary>
	enum TextRole
	{
		Caption,
		Body,
		BodyStrong,
		Subtitle,
		Title,
		Icon,
		IconSmall,
		IconLarge
	}

	/// <summary>
	/// Severity used by info bars
	/// </summary>
	enum Severity
	{
		Info,
		Success,
		Warning,
		Error
	}

	/// <summary>
	/// Colours, fonts and metrics shared by all of the DMT windows.
	/// Follows the Windows light/dark setting and accent colour.
	/// </summary>
	static class Theme
	{
		// Segoe Fluent Icons code points used around the UI
		public const string GlyphSettings = "";
		public const string GlyphMonitor = "";
		public const string GlyphRename = "";
		public const string GlyphSort = "";
		public const string GlyphMouse = "";
		public const string GlyphPin = "";
		public const string GlyphHome = "";
		public const string GlyphFrame = "";
		public const string GlyphCommand = "";
		public const string GlyphList = "";
		public const string GlyphKeyboard = "";
		public const string GlyphSliders = "";
		public const string GlyphSwap = "";
		public const string GlyphCamera = "";
		public const string GlyphGrid = "";
		public const string GlyphTaskView = "";
		public const string GlyphTiles = "";
		public const string GlyphLayout = "";
		public const string GlyphWindows = "";
		public const string GlyphPhoto = "";
		public const string GlyphPalette = "";
		public const string GlyphFolder = "";
		public const string GlyphInfo = "";
		public const string GlyphWarning = "";
		public const string GlyphError = "";
		public const string GlyphSuccess = "";
		public const string GlyphEdit = "";
		public const string GlyphDelete = "";
		public const string GlyphAdd = "";
		public const string GlyphClear = "";
		public const string GlyphChevronRight = "";
		public const string GlyphSearch = "";
		public const string GlyphRefresh = "";
		public const string GlyphOpen = "";
		public const string GlyphFullScreen = "";

		static readonly Dictionary<Tuple<TextRole, int>, Font> _fonts = new Dictionary<Tuple<TextRole, int>, Font>();
		static readonly string[] _installedFamilies;

		static Theme()
		{
			using (InstalledFontCollection installed = new InstalledFontCollection())
			{
				_installedFamilies = installed.Families.Select(f => f.Name).ToArray();
			}

			TextFamily = FirstInstalled("Segoe UI Variable Text", "Segoe UI");
			TextStrongFamily = FirstInstalled("Segoe UI Variable Text Semibold", "Segoe UI Semibold");
			DisplayFamily = FirstInstalled("Segoe UI Variable Display Semib", "Segoe UI Variable Display Semibold", "Segoe UI Semibold");
			IconFamily = FirstInstalled("Segoe Fluent Icons", "Segoe MDL2 Assets");

			Load();
			SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
		}

		/// <summary>
		/// Raised (on the UI thread) when Windows switches between light/dark or changes the accent colour
		/// </summary>
		public static event EventHandler Changed;

		public static bool IsDark { get; private set; }

		public static string TextFamily { get; private set; }
		public static string TextStrongFamily { get; private set; }
		public static string DisplayFamily { get; private set; }
		public static string IconFamily { get; private set; }

		// surfaces
		public static Color Background { get; private set; }
		public static Color NavBackground { get; private set; }
		public static Color Surface { get; private set; }
		public static Color SurfaceHover { get; private set; }
		public static Color Stroke { get; private set; }
		public static Color Divider { get; private set; }

		// text
		public static Color TextPrimary { get; private set; }
		public static Color TextSecondary { get; private set; }
		public static Color TextTertiary { get; private set; }
		public static Color TextDisabled { get; private set; }

		// controls
		public static Color ControlFill { get; private set; }
		public static Color ControlFillHover { get; private set; }
		public static Color ControlFillPressed { get; private set; }
		public static Color ControlStroke { get; private set; }
		public static Color SubtleFillHover { get; private set; }
		public static Color SubtleFillSelected { get; private set; }
		public static Color ToggleOffStroke { get; private set; }
		public static Color KeyCapFill { get; private set; }

		// accent
		public static Color Accent { get; private set; }
		public static Color AccentHover { get; private set; }
		public static Color AccentPressed { get; private set; }
		public static Color OnAccent { get; private set; }
		public static Color AccentText { get; private set; }

		/// <summary>
		/// The font used by all standard controls
		/// </summary>
		public static Font DefaultFont
		{
			get { return new Font(TextFamily, 10.5f, FontStyle.Regular, GraphicsUnit.Point); }
		}

		/// <summary>
		/// Gets the font for the given role, sized for the given DPI
		/// </summary>
		public static Font GetFont(TextRole role, int dpi)
		{
			Tuple<TextRole, int> key = Tuple.Create(role, dpi);
			Font font;
			if (!_fonts.TryGetValue(key, out font))
			{
				float px;
				string family;
				switch (role)
				{
					case TextRole.Caption: px = 12; family = TextFamily; break;
					case TextRole.BodyStrong: px = 14; family = TextStrongFamily; break;
					case TextRole.Subtitle: px = 20; family = DisplayFamily; break;
					case TextRole.Title: px = 28; family = DisplayFamily; break;
					case TextRole.Icon: px = 16; family = IconFamily; break;
					case TextRole.IconSmall: px = 12; family = IconFamily; break;
					case TextRole.IconLarge: px = 20; family = IconFamily; break;
					case TextRole.Body:
					default: px = 14; family = TextFamily; break;
				}

				font = new Font(family, px * dpi / 96f, FontStyle.Regular, GraphicsUnit.Pixel);
				_fonts[key] = font;
			}

			return font;
		}

		public static Color SeverityBackground(Severity severity)
		{
			switch (severity)
			{
				case Severity.Success: return IsDark ? Color.FromArgb(0x39, 0x3D, 0x1B) : Color.FromArgb(0xDF, 0xF6, 0xDD);
				case Severity.Warning: return IsDark ? Color.FromArgb(0x43, 0x35, 0x19) : Color.FromArgb(0xFF, 0xF4, 0xCE);
				case Severity.Error: return IsDark ? Color.FromArgb(0x44, 0x27, 0x26) : Color.FromArgb(0xFD, 0xE7, 0xE9);
				default: return Surface;
			}
		}

		public static Color SeverityForeground(Severity severity)
		{
			switch (severity)
			{
				case Severity.Success: return IsDark ? Color.FromArgb(0x6C, 0xCB, 0x5F) : Color.FromArgb(0x0F, 0x7B, 0x0F);
				case Severity.Warning: return IsDark ? Color.FromArgb(0xFC, 0xE1, 0x00) : Color.FromArgb(0x9D, 0x5D, 0x00);
				case Severity.Error: return IsDark ? Color.FromArgb(0xFF, 0x99, 0xA4) : Color.FromArgb(0xC4, 0x2B, 0x1C);
				default: return AccentText;
			}
		}

		public static string SeverityGlyph(Severity severity)
		{
			switch (severity)
			{
				case Severity.Success: return GlyphSuccess;
				case Severity.Warning: return GlyphWarning;
				case Severity.Error: return GlyphError;
				default: return GlyphInfo;
			}
		}

		/// <summary>
		/// Makes the title bar match the theme (dark title bar, caption colour matching the window)
		/// </summary>
		public static void ApplyWindowChrome(Form form)
		{
			if (!form.IsHandleCreated)
			{
				form.HandleCreated += (s, e) => ApplyWindowChrome(form);
				return;
			}

			int dark = IsDark ? 1 : 0;
			DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

			int caption = ColorTranslator.ToWin32(form.BackColor);
			DwmSetWindowAttribute(form.Handle, DWMWA_CAPTION_COLOR, ref caption, sizeof(int));
		}

		/// <summary>
		/// Creates a path for a rectangle with rounded corners
		/// </summary>
		public static System.Drawing.Drawing2D.GraphicsPath RoundedRect(RectangleF rect, float radius)
		{
			System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
			float d = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
			if (d <= 0)
			{
				path.AddRectangle(rect);
				return path;
			}

			path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
			path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
			path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
			path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
			path.CloseFigure();
			return path;
		}

		/// <summary>
		/// Fills (and optionally strokes) a rounded rectangle with anti-aliasing,
		/// keeping the stroke inside the given bounds
		/// </summary>
		public static void FillRounded(Graphics g, Rectangle bounds, int radius, Color fill, Color? stroke = null)
		{
			System.Drawing.Drawing2D.SmoothingMode oldMode = g.SmoothingMode;
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			RectangleF rect = new RectangleF(bounds.X + 0.5f, bounds.Y + 0.5f, bounds.Width - 1, bounds.Height - 1);
			using (System.Drawing.Drawing2D.GraphicsPath path = RoundedRect(rect, radius))
			{
				using (SolidBrush brush = new SolidBrush(fill))
				{
					g.FillPath(brush, path);
				}

				if (stroke.HasValue)
				{
					using (Pen pen = new Pen(stroke.Value))
					{
						g.DrawPath(pen, path);
					}
				}
			}

			g.SmoothingMode = oldMode;
		}

		static void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
		{
			if (e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.Color || e.Category == UserPreferenceCategory.VisualStyle)
			{
				bool wasDark = IsDark;
				Color oldAccent = Accent;
				Load();
				if (wasDark != IsDark || oldAccent != Accent)
				{
					if (Changed != null)
					{
						Changed(null, EventArgs.Empty);
					}
				}
			}
		}

		static void Load()
		{
			IsDark = ReadIsDark();
			Color[] palette = ReadAccentPalette();

			if (IsDark)
			{
				Background = Color.FromArgb(0x20, 0x20, 0x20);
				NavBackground = Background;
				Surface = Color.FromArgb(0x2B, 0x2B, 0x2B);
				SurfaceHover = Color.FromArgb(0x32, 0x32, 0x32);
				Stroke = Color.FromArgb(0x1C, 0x1C, 0x1C);
				Divider = Color.FromArgb(0x3A, 0x3A, 0x3A);

				TextPrimary = Color.White;
				TextSecondary = Color.FromArgb(0xCF, 0xCF, 0xCF);
				TextTertiary = Color.FromArgb(0x9E, 0x9E, 0x9E);
				TextDisabled = Color.FromArgb(0x78, 0x78, 0x78);

				ControlFill = Color.FromArgb(0x37, 0x37, 0x37);
				ControlFillHover = Color.FromArgb(0x3D, 0x3D, 0x3D);
				ControlFillPressed = Color.FromArgb(0x32, 0x32, 0x32);
				ControlStroke = Color.FromArgb(0x45, 0x45, 0x45);
				SubtleFillHover = Color.FromArgb(0x2D, 0x2D, 0x2D);
				SubtleFillSelected = Color.FromArgb(0x33, 0x33, 0x33);
				ToggleOffStroke = Color.FromArgb(0xCF, 0xCF, 0xCF);
				KeyCapFill = Color.FromArgb(0x3A, 0x3A, 0x3A);

				// Windows uses the lighter shades of the accent in dark mode
				Accent = palette != null ? palette[1] : Color.FromArgb(0x4C, 0xC2, 0xFF);
				AccentHover = palette != null ? palette[2] : Color.FromArgb(0x47, 0xB1, 0xE8);
				AccentPressed = palette != null ? palette[3] : Color.FromArgb(0x42, 0xA1, 0xD2);
				OnAccent = Color.Black;
				AccentText = palette != null ? palette[0] : Color.FromArgb(0x99, 0xEB, 0xFF);
			}
			else
			{
				Background = Color.FromArgb(0xF3, 0xF3, 0xF3);
				NavBackground = Background;
				Surface = Color.FromArgb(0xFB, 0xFB, 0xFB);
				SurfaceHover = Color.FromArgb(0xF6, 0xF6, 0xF6);
				Stroke = Color.FromArgb(0xE5, 0xE5, 0xE5);
				Divider = Color.FromArgb(0xE0, 0xE0, 0xE0);

				TextPrimary = Color.FromArgb(0x1A, 0x1A, 0x1A);
				TextSecondary = Color.FromArgb(0x5F, 0x5F, 0x5F);
				TextTertiary = Color.FromArgb(0x8A, 0x8A, 0x8A);
				TextDisabled = Color.FromArgb(0xA0, 0xA0, 0xA0);

				ControlFill = Color.FromArgb(0xFE, 0xFE, 0xFE);
				ControlFillHover = Color.FromArgb(0xF7, 0xF7, 0xF7);
				ControlFillPressed = Color.FromArgb(0xF2, 0xF2, 0xF2);
				ControlStroke = Color.FromArgb(0xD9, 0xD9, 0xD9);
				SubtleFillHover = Color.FromArgb(0xEA, 0xEA, 0xEA);
				SubtleFillSelected = Color.FromArgb(0xE6, 0xE6, 0xE6);
				ToggleOffStroke = Color.FromArgb(0x5F, 0x5F, 0x5F);
				KeyCapFill = Color.FromArgb(0xFF, 0xFF, 0xFF);

				// Windows uses the darker shades of the accent in light mode
				Accent = palette != null ? palette[4] : Color.FromArgb(0x00, 0x5F, 0xB8);
				AccentHover = palette != null ? palette[5] : Color.FromArgb(0x19, 0x6E, 0xBF);
				AccentPressed = palette != null ? palette[5] : Color.FromArgb(0x31, 0x7D, 0xC6);
				OnAccent = Color.White;
				AccentText = palette != null ? palette[5] : Color.FromArgb(0x00, 0x3E, 0x92);
			}
		}

		/// <summary>
		/// Gets the theme forced by the DMT_THEME environment variable ("light" or "dark"), or null to follow Windows.
		/// (Handy for checking both themes without changing the Windows setting.)
		/// </summary>
		public static bool? ForcedDark
		{
			get
			{
				string forced = System.Environment.GetEnvironmentVariable("DMT_THEME");
				if (string.Equals(forced, "light", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}

				if (string.Equals(forced, "dark", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}

				return null;
			}
		}

		static bool ReadIsDark()
		{
			if (ForcedDark.HasValue)
			{
				return ForcedDark.Value;
			}

			try
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
				{
					object value = key?.GetValue("AppsUseLightTheme");
					if (value is int)
					{
						return (int)value == 0;
					}
				}
			}
			catch (Exception)
			{
			}

			return false;
		}

		// The accent palette holds 8 colours (RGBA), from lightest to darkest:
		// Light3, Light2, Light1, Base, Dark1, Dark2, Dark3, (unused)
		static Color[] ReadAccentPalette()
		{
			try
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent"))
				{
					byte[] bytes = key?.GetValue("AccentPalette") as byte[];
					if (bytes != null && bytes.Length >= 32)
					{
						Color[] palette = new Color[8];
						for (int i = 0; i < 8; i++)
						{
							palette[i] = Color.FromArgb(bytes[i * 4], bytes[i * 4 + 1], bytes[i * 4 + 2]);
						}

						return palette;
					}
				}
			}
			catch (Exception)
			{
			}

			return null;
		}

		static string FirstInstalled(params string[] families)
		{
			foreach (string family in families)
			{
				if (_installedFamilies.Contains(family, StringComparer.OrdinalIgnoreCase))
				{
					return family;
				}
			}

			return families[families.Length - 1];
		}

		const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
		const int DWMWA_CAPTION_COLOR = 35;

		[DllImport("dwmapi.dll")]
		static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
	}
}
