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

	using DMT.Library.HotKeys;

	/// <summary>
	/// Implemented by controls whose height depends on the width they are given (e.g. wrapping text)
	/// </summary>
	interface IHeightForWidth
	{
		int GetHeightForWidth(int width);
	}

	/// <summary>
	/// Implemented by controls that need to refresh themselves when the theme changes
	/// </summary>
	interface IThemeAware
	{
		void ApplyTheme();
	}

	enum TextTone
	{
		Primary,
		Secondary,
		Tertiary,
		Accent,
		Disabled
	}

	static class UiHelper
	{
		public static Color ToneColor(TextTone tone)
		{
			switch (tone)
			{
				case TextTone.Secondary: return Theme.TextSecondary;
				case TextTone.Tertiary: return Theme.TextTertiary;
				case TextTone.Accent: return Theme.AccentText;
				case TextTone.Disabled: return Theme.TextDisabled;
				default: return Theme.TextPrimary;
			}
		}

		/// <summary>
		/// Height a control wants when given the specified width
		/// </summary>
		public static int MeasureHeight(Control control, int width)
		{
			IHeightForWidth flexible = control as IHeightForWidth;
			if (flexible != null)
			{
				return flexible.GetHeightForWidth(width);
			}

			if (control.AutoSize)
			{
				return control.GetPreferredSize(new Size(width, 0)).Height;
			}

			return control.Height;
		}

		/// <summary>
		/// Width a control wants (for controls placed at their natural size)
		/// </summary>
		public static Size MeasureNatural(Control control)
		{
			ComboBox combo = control as ComboBox;
			if (combo != null)
			{
				return MeasureCombo(combo);
			}

			int logicalWidth;
			if (_logicalWidths.TryGetValue(control, out logicalWidth))
			{
				return new Size(control.LogicalToDeviceUnits(logicalWidth), control.Height);
			}

			// the standard input controls report AutoSize, but their preferred width isn't useful,
			// so they keep the width they were given
			bool fixedWidth = control is ComboBox || control is UpDownBase || control is TextBoxBase;
			if (control.AutoSize && !fixedWidth)
			{
				return control.GetPreferredSize(Size.Empty);
			}

			return control.Size;
		}

		/// <summary>
		/// A drop down is made wide enough for its longest item (up to a limit),
		/// and its list is always wide enough to show every item in full
		/// </summary>
		static Size MeasureCombo(ComboBox combo)
		{
			int widest = 0;
			foreach (object item in combo.Items)
			{
				string text = combo.GetItemText(item);
				widest = Math.Max(widest, TextRenderer.MeasureText(text, combo.Font).Width);
			}

			// room for the arrow and the padding either side of the text
			int needed = widest + combo.LogicalToDeviceUnits(36);

			int logicalWidth;
			int width = _logicalWidths.TryGetValue(combo, out logicalWidth) ? combo.LogicalToDeviceUnits(logicalWidth) : combo.Width;
			width = Math.Max(width, Math.Min(needed, combo.LogicalToDeviceUnits(440)));

			int dropDownWidth = Math.Max(width, needed);
			if (combo.DropDownWidth != dropDownWidth)
			{
				combo.DropDownWidth = dropDownWidth;
			}

			return new Size(width, combo.Height);
		}

		static readonly Dictionary<Control, int> _logicalWidths = new Dictionary<Control, int>();

		/// <summary>
		/// Gives a control a fixed width in logical pixels.
		/// The width is converted to device pixels whenever it is laid out, so it is right on any monitor.
		/// </summary>
		public static T WithLogicalWidth<T>(this T control, int logicalWidth) where T : Control
		{
			_logicalWidths[control] = logicalWidth;
			control.Disposed += (s, e) => _logicalWidths.Remove(control);
			control.Width = control.LogicalToDeviceUnits(logicalWidth);
			return control;
		}

		/// <summary>
		/// Walks the control tree letting theme aware controls refresh themselves
		/// </summary>
		public static void RefreshTheme(Control root)
		{
			IThemeAware aware = root as IThemeAware;
			if (aware != null)
			{
				aware.ApplyTheme();
			}

			foreach (Control child in root.Controls)
			{
				RefreshTheme(child);
			}

			root.Invalidate();
		}

		/// <summary>
		/// Makes a pre-existing (designer laid out) panel blend in with the theme
		/// </summary>
		public static void ApplyLegacyTheme(Control root)
		{
			foreach (Control child in root.Controls)
			{
				if (child is GroupBox || child is Label || child is CheckBox || child is RadioButton)
				{
					child.ForeColor = Theme.TextPrimary;
				}

				if (child is Panel || child is GroupBox || child is UserControl || child is TabPage || child is SplitContainer)
				{
					child.BackColor = Theme.Background;
				}

				ApplyLegacyTheme(child);
			}
		}

		/// <summary>
		/// Converts a key name such as "&lt;Num7&gt;" into a friendlier form such as "Num 7"
		/// </summary>
		public static string FriendlyKeyName(Keys key)
		{
			string name = VirtualKey.CodeToName(key);
			if (name.Length > 2 && name.StartsWith("<") && name.EndsWith(">"))
			{
				name = name.Substring(1, name.Length - 2);
			}

			if (name.StartsWith("Num") && name.Length == 4)
			{
				name = "Num " + name.Substring(3);
			}

			return name;
		}

		/// <summary>
		/// The keys that make up a key combination, in the order they would be displayed
		/// </summary>
		public static List<string> KeyParts(KeyCombo keyCombo, bool includeKey = true)
		{
			List<string> parts = new List<string>();
			if (keyCombo.WinMod)
			{
				parts.Add("Win");
			}

			if (keyCombo.ControlMod)
			{
				parts.Add("Ctrl");
			}

			if (keyCombo.AltMod)
			{
				parts.Add("Alt");
			}

			if (keyCombo.ShiftMod)
			{
				parts.Add("Shift");
			}

			if (includeKey && keyCombo.KeyCode != Keys.None)
			{
				parts.Add(FriendlyKeyName(keyCombo.KeyCode));
			}

			return parts;
		}
	}

	/// <summary>
	/// Base for all of our custom painted controls
	/// </summary>
	class ThemedControl : Control, IThemeAware
	{
		public ThemedControl()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
				| ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
		}

		public virtual void ApplyTheme()
		{
			Invalidate();
		}

		protected int S(int logical)
		{
			return LogicalToDeviceUnits(logical);
		}

		protected Color ParentBackColor
		{
			get { return Parent != null ? Parent.BackColor : Theme.Background; }
		}
	}

	/// <summary>
	/// A block of (optionally wrapping) text in one of the theme's text roles
	/// </summary>
	class TextBlock : ThemedControl, IHeightForWidth
	{
		TextRole _role;
		TextTone _tone;
		bool _wrap = true;

		public TextBlock(string text = "", TextRole role = TextRole.Body, TextTone tone = TextTone.Primary)
		{
			SetStyle(ControlStyles.Selectable, false);
			TabStop = false;
			_role = role;
			_tone = tone;
			Text = text;
			AutoSize = true;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TextRole Role
		{
			get { return _role; }
			set { _role = value; RequestLayout(); }
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TextTone Tone
		{
			get { return _tone; }
			set { _tone = value; Invalidate(); }
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool Wrap
		{
			get { return _wrap; }
			set { _wrap = value; RequestLayout(); }
		}

		/// <summary>
		/// Overrides the tone with an explicit colour
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Color? ColorOverride { get; set; }

		Font TextFont
		{
			get { return Theme.GetFont(_role, DeviceDpi); }
		}

		TextFormatFlags Flags
		{
			get
			{
				TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
				flags |= _wrap ? TextFormatFlags.WordBreak : (TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
				return flags;
			}
		}

		public int GetHeightForWidth(int width)
		{
			return GetPreferredSize(new Size(width, 0)).Height;
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			string text = string.IsNullOrEmpty(Text) ? " " : Text;
			int width = (_wrap && proposedSize.Width > 0) ? proposedSize.Width : int.MaxValue;
			Size size = TextRenderer.MeasureText(text, TextFont, new Size(width, int.MaxValue), Flags);
			if (!_wrap && proposedSize.Width > 0)
			{
				size.Width = Math.Min(size.Width, proposedSize.Width);
			}

			return size;
		}

		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);
			RequestLayout();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Color color = ColorOverride.HasValue ? ColorOverride.Value : UiHelper.ToneColor(Enabled ? _tone : TextTone.Disabled);
			TextRenderer.DrawText(e.Graphics, Text, TextFont, ClientRectangle, color, Flags);
		}

		void RequestLayout()
		{
			Invalidate();
			if (Parent != null)
			{
				Parent.PerformLayout();
			}
		}
	}

	/// <summary>
	/// A single icon from the Segoe Fluent Icons font
	/// </summary>
	class Glyph : ThemedControl
	{
		public Glyph(string glyph, TextRole role = TextRole.Icon, TextTone tone = TextTone.Primary)
		{
			SetStyle(ControlStyles.Selectable, false);
			TabStop = false;
			Text = glyph;
			Role = role;
			Tone = tone;
			AutoSize = true;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TextRole Role { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TextTone Tone { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Color? ColorOverride { get; set; }

		public override Size GetPreferredSize(Size proposedSize)
		{
			Font font = Theme.GetFont(Role, DeviceDpi);
			int size = (int)Math.Ceiling(font.Size * 1.25f);
			return new Size(size, size);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Color color = ColorOverride.HasValue ? ColorOverride.Value : UiHelper.ToneColor(Tone);
			TextRenderer.DrawText(e.Graphics, Text, Theme.GetFont(Role, DeviceDpi), ClientRectangle, color,
				TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
		}
	}

	/// <summary>
	/// On/off switch.  This is a CheckBox, so existing code using Checked/CheckedChanged keeps working.
	/// </summary>
	class ToggleSwitch : CheckBox, IThemeAware
	{
		bool _hover;

		public ToggleSwitch()
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			AutoSize = true;
			Text = string.Empty;
			OnText = "On";
			OffText = "Off";
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string OnText { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string OffText { get; set; }

		public void ApplyTheme()
		{
			Invalidate();
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			Font font = Theme.GetFont(TextRole.Body, DeviceDpi);
			int textWidth = Math.Max(TextRenderer.MeasureText(OnText, font).Width, TextRenderer.MeasureText(OffText, font).Width);
			return new Size(textWidth + LogicalToDeviceUnits(12 + 40 + 4), LogicalToDeviceUnits(32));
		}

		protected override void OnMouseEnter(EventArgs eventargs)
		{
			_hover = true;
			Invalidate();
			base.OnMouseEnter(eventargs);
		}

		protected override void OnMouseLeave(EventArgs eventargs)
		{
			_hover = false;
			Invalidate();
			base.OnMouseLeave(eventargs);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Parent != null ? Parent.BackColor : Theme.Background);

			int trackWidth = LogicalToDeviceUnits(40);
			int trackHeight = LogicalToDeviceUnits(20);
			Rectangle track = new Rectangle(Width - trackWidth - LogicalToDeviceUnits(2), (Height - trackHeight) / 2, trackWidth, trackHeight);

			// state text to the left of the switch
			Font font = Theme.GetFont(TextRole.Body, DeviceDpi);
			Rectangle textRect = new Rectangle(0, 0, track.Left - LogicalToDeviceUnits(12), Height);
			TextRenderer.DrawText(g, Checked ? OnText : OffText, font, textRect, Enabled ? Theme.TextPrimary : Theme.TextDisabled,
				TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

			int knob = LogicalToDeviceUnits(_hover && Enabled ? 14 : 12);
			int knobInset = (trackHeight - knob) / 2;
			Rectangle knobRect;

			if (Checked)
			{
				Color fill = !Enabled ? Theme.TextDisabled : (_hover ? Theme.AccentHover : Theme.Accent);
				Theme.FillRounded(g, track, trackHeight / 2, fill);
				knobRect = new Rectangle(track.Right - knobInset - knob - LogicalToDeviceUnits(1), track.Top + knobInset, knob, knob);
				Theme.FillRounded(g, knobRect, knob / 2, Theme.OnAccent == Color.Black ? Theme.Background : Color.White);
			}
			else
			{
				Color stroke = Enabled ? Theme.ToggleOffStroke : Theme.TextDisabled;
				Theme.FillRounded(g, track, trackHeight / 2, _hover && Enabled ? Theme.SubtleFillHover : Theme.ControlFill, stroke);
				knobRect = new Rectangle(track.Left + knobInset + LogicalToDeviceUnits(1), track.Top + knobInset, knob, knob);
				Theme.FillRounded(g, knobRect, knob / 2, stroke);
			}

			if (Focused && ShowFocusCues)
			{
				Rectangle focus = Rectangle.Inflate(track, LogicalToDeviceUnits(2), LogicalToDeviceUnits(2));
				focus.Intersect(new Rectangle(0, 0, Width - 1, Height - 1));
				using (Pen pen = new Pen(Theme.TextPrimary, LogicalToDeviceUnits(1)))
				{
					g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
					using (System.Drawing.Drawing2D.GraphicsPath path = Theme.RoundedRect(focus, focus.Height / 2f))
					{
						g.DrawPath(pen, path);
					}
				}
			}
		}
	}

	enum ButtonKind
	{
		Standard,
		Accent,
		Subtle
	}

	/// <summary>
	/// A flat button with rounded corners and an optional icon
	/// </summary>
	/// <remarks>
	/// FlatStyle is Flat because in dark mode WinForms draws Standard buttons natively,
	/// which would bypass our painting.
	/// </remarks>
	class FluentButton : Button, IThemeAware
	{
		bool _hover;
		bool _pressed;

		public FluentButton(string text = "", string glyph = null, ButtonKind kind = ButtonKind.Standard)
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			FlatStyle = FlatStyle.Flat;
			UseVisualStyleBackColor = false;
			Text = text;
			GlyphText = glyph;
			Kind = kind;
			AutoSize = true;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string GlyphText { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ButtonKind Kind { get; set; }

		public void ApplyTheme()
		{
			Invalidate();
		}

		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);
			Invalidate();
		}

		protected override void OnKeyDown(KeyEventArgs kevent) { if (kevent.KeyCode == Keys.Space) { _pressed = true; Invalidate(); } base.OnKeyDown(kevent); }
		protected override void OnKeyUp(KeyEventArgs kevent) { if (kevent.KeyCode == Keys.Space) { _pressed = false; Invalidate(); } base.OnKeyUp(kevent); }
		protected override void OnLostFocus(EventArgs e) { _pressed = false; base.OnLostFocus(e); Invalidate(); }

		/// <summary>
		/// Only show the icon.  The text is still used as the button's name for screen readers.
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool ShowIconOnly { get; set; }

		bool IconOnly
		{
			get { return !string.IsNullOrEmpty(GlyphText) && (ShowIconOnly || string.IsNullOrEmpty(Text)); }
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			int height = LogicalToDeviceUnits(32);
			if (IconOnly)
			{
				return new Size(height, height);
			}

			int width = TextRenderer.MeasureText(Text, Theme.GetFont(TextRole.Body, DeviceDpi), Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
			width += LogicalToDeviceUnits(12) * 2;
			if (!string.IsNullOrEmpty(GlyphText))
			{
				width += LogicalToDeviceUnits(16 + 8);
			}

			return new Size(Math.Max(width, LogicalToDeviceUnits(96)), height);
		}

		protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
		protected override void OnMouseLeave(EventArgs e) { _hover = false; _pressed = false; Invalidate(); base.OnMouseLeave(e); }
		protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
		protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Parent != null ? Parent.BackColor : Theme.Background);

			Color fill;
			Color? stroke = null;
			Color text;
			switch (Kind)
			{
				case ButtonKind.Accent:
					fill = !Enabled ? Theme.ControlFillPressed : (_pressed ? Theme.AccentPressed : (_hover ? Theme.AccentHover : Theme.Accent));
					text = Enabled ? Theme.OnAccent : Theme.TextDisabled;
					break;

				case ButtonKind.Subtle:
					fill = !Enabled ? Color.Transparent : (_pressed ? Theme.SubtleFillSelected : (_hover ? Theme.SubtleFillHover : Color.Transparent));
					text = Enabled ? Theme.TextPrimary : Theme.TextDisabled;
					break;

				default:
					fill = !Enabled ? Theme.ControlFillPressed : (_pressed ? Theme.ControlFillPressed : (_hover ? Theme.ControlFillHover : Theme.ControlFill));
					stroke = IsDefault && Enabled ? Theme.Accent : Theme.ControlStroke;
					text = Enabled ? (_pressed ? Theme.TextSecondary : Theme.TextPrimary) : Theme.TextDisabled;
					break;
			}

			if (fill.A > 0 || stroke.HasValue)
			{
				Theme.FillRounded(g, ClientRectangle, LogicalToDeviceUnits(4), fill.A > 0 ? fill : (Parent != null ? Parent.BackColor : Theme.Background), stroke);
			}

			Font font = Theme.GetFont(TextRole.Body, DeviceDpi);
			Font iconFont = Theme.GetFont(TextRole.Icon, DeviceDpi);
			TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter;

			if (IconOnly)
			{
				TextRenderer.DrawText(g, GlyphText, iconFont, ClientRectangle, text, flags | TextFormatFlags.HorizontalCenter);
			}
			else
			{
				int textWidth = TextRenderer.MeasureText(Text, font, Size.Empty, flags).Width;
				int glyphWidth = string.IsNullOrEmpty(GlyphText) ? 0 : LogicalToDeviceUnits(16 + 8);
				int x = (Width - textWidth - glyphWidth) / 2;
				if (glyphWidth > 0)
				{
					TextRenderer.DrawText(g, GlyphText, iconFont, new Rectangle(x, 0, LogicalToDeviceUnits(16), Height), text, flags | TextFormatFlags.HorizontalCenter);
					x += glyphWidth;
				}

				TextRenderer.DrawText(g, Text, font, new Rectangle(x, 0, textWidth + 1, Height), text, flags);
			}

			if (Focused && ShowFocusCues)
			{
				Rectangle focus = new Rectangle(1, 1, Width - 3, Height - 3);
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				using (Pen pen = new Pen(Theme.TextPrimary, LogicalToDeviceUnits(1)))
				using (System.Drawing.Drawing2D.GraphicsPath path = Theme.RoundedRect(focus, LogicalToDeviceUnits(4)))
				{
					g.DrawPath(pen, path);
				}
			}
		}
	}

	/// <summary>
	/// Shows a key combination as a row of key caps
	/// </summary>
	class KeyChips : ThemedControl
	{
		List<string> _keys = new List<string>();

		public KeyChips()
		{
			SetStyle(ControlStyles.Selectable, false);
			TabStop = false;
			AutoSize = true;
			EmptyText = "Not set";
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string EmptyText { get; set; }

		/// <summary>
		/// Sets the keys to display (empty for none)
		/// </summary>
		public void SetKeys(IEnumerable<string> keys)
		{
			_keys = new List<string>(keys);
			AccessibleName = _keys.Count > 0 ? string.Join(" + ", _keys) : EmptyText;
			Invalidate();
			if (Parent != null)
			{
				Parent.PerformLayout();
			}
		}

		public void SetKeyCombo(KeyCombo keyCombo)
		{
			SetKeys(keyCombo.Enabled ? UiHelper.KeyParts(keyCombo) : new List<string>());
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			Font font = Theme.GetFont(TextRole.Body, DeviceDpi);
			if (_keys.Count == 0)
			{
				Size size = TextRenderer.MeasureText(EmptyText, font, Size.Empty, TextFormatFlags.NoPadding);
				return new Size(size.Width, S(28));
			}

			int width = 0;
			foreach (string key in _keys)
			{
				width += ChipWidth(key, font) + S(4);
			}

			return new Size(width - S(4), S(28));
		}

		int ChipWidth(string key, Font font)
		{
			int textWidth = TextRenderer.MeasureText(key, font, Size.Empty, TextFormatFlags.NoPadding).Width;
			return Math.Max(textWidth + S(10) * 2, S(32));
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			Font font = Theme.GetFont(TextRole.Body, DeviceDpi);
			TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix;

			if (_keys.Count == 0)
			{
				TextRenderer.DrawText(g, EmptyText, font, ClientRectangle, Theme.TextTertiary,
					TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
				return;
			}

			int x = Width - GetPreferredSize(Size.Empty).Width;
			foreach (string key in _keys)
			{
				int chipWidth = ChipWidth(key, font);
				Rectangle chip = new Rectangle(x, 0, chipWidth, Height - S(2));

				// a slightly darker edge at the bottom gives the key cap some depth
				Rectangle shadow = chip;
				shadow.Offset(0, S(2));
				Theme.FillRounded(g, shadow, S(4), Theme.ControlStroke);
				Theme.FillRounded(g, chip, S(4), Theme.KeyCapFill, Theme.ControlStroke);
				TextRenderer.DrawText(g, key, font, chip, Theme.TextPrimary, flags);
				x += chipWidth + S(4);
			}
		}
	}

	/// <summary>
	/// A coloured bar that draws attention to something (like a WinUI InfoBar)
	/// </summary>
	class InfoBar : ThemedControl, IHeightForWidth
	{
		Severity _severity;
		string _title;

		public InfoBar(Severity severity, string title, string message)
		{
			SetStyle(ControlStyles.Selectable, false);
			TabStop = false;
			_severity = severity;
			_title = title;
			Text = message;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Severity Severity
		{
			get { return _severity; }
			set { _severity = value; Invalidate(); }
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string Title
		{
			get { return _title; }
			set { _title = value; Invalidate(); if (Parent != null) { Parent.PerformLayout(); } }
		}

		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);
			if (Parent != null)
			{
				Parent.PerformLayout();
			}
		}

		const TextFormatFlags Flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

		int TextLeft { get { return S(16 + 16 + 12); } }

		public int GetHeightForWidth(int width)
		{
			int textWidth = Math.Max(width - TextLeft - S(16), S(40));
			int height = S(14);
			if (!string.IsNullOrEmpty(_title))
			{
				height += TextRenderer.MeasureText(_title, Theme.GetFont(TextRole.BodyStrong, DeviceDpi), new Size(textWidth, int.MaxValue), Flags).Height;
			}

			if (!string.IsNullOrEmpty(Text))
			{
				height += S(2) + TextRenderer.MeasureText(Text, Theme.GetFont(TextRole.Body, DeviceDpi), new Size(textWidth, int.MaxValue), Flags).Height;
			}

			return Math.Max(height + S(14), S(48));
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(ParentBackColor);
			Theme.FillRounded(g, ClientRectangle, S(4), Theme.SeverityBackground(_severity), Theme.Stroke);

			// round icon badge
			Rectangle badge = new Rectangle(S(16), S(15), S(16), S(16));
			Theme.FillRounded(g, badge, S(8), Theme.SeverityForeground(_severity));
			string glyph = _severity == Severity.Info ? "i" : (_severity == Severity.Success ? Theme.GlyphSuccess : "!");
			Font badgeFont = _severity == Severity.Success ? Theme.GetFont(TextRole.IconSmall, DeviceDpi) : Theme.GetFont(TextRole.BodyStrong, DeviceDpi);
			TextRenderer.DrawText(g, glyph, badgeFont, badge, Theme.IsDark ? Color.Black : Color.White,
				TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

			int textWidth = Math.Max(Width - TextLeft - S(16), S(40));
			int y = S(14);
			if (!string.IsNullOrEmpty(_title))
			{
				Font titleFont = Theme.GetFont(TextRole.BodyStrong, DeviceDpi);
				Size size = TextRenderer.MeasureText(_title, titleFont, new Size(textWidth, int.MaxValue), Flags);
				TextRenderer.DrawText(g, _title, titleFont, new Rectangle(TextLeft, y, textWidth, size.Height), Theme.TextPrimary, Flags);
				y += size.Height + S(2);
			}

			if (!string.IsNullOrEmpty(Text))
			{
				Font font = Theme.GetFont(TextRole.Body, DeviceDpi);
				Size size = TextRenderer.MeasureText(Text, font, new Size(textWidth, int.MaxValue), Flags);
				TextRenderer.DrawText(g, Text, font, new Rectangle(TextLeft, y, textWidth, size.Height), Theme.TextPrimary, Flags);
			}
		}
	}

	/// <summary>
	/// A small rounded square showing a colour
	/// </summary>
	class ColorSwatch : ThemedControl
	{
		Color _color;

		public ColorSwatch(Color color)
		{
			SetStyle(ControlStyles.Selectable, false);
			TabStop = false;
			_color = color;
			AutoSize = true;
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Color SwatchColor
		{
			get { return _color; }
			set { _color = value; Invalidate(); }
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			return new Size(S(32), S(32));
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.Clear(ParentBackColor);
			Theme.FillRounded(e.Graphics, ClientRectangle, S(4), _color, Theme.ControlStroke);
		}
	}

	/// <summary>
	/// A horizontal slider (like the WinUI Slider)
	/// </summary>
	class Slider : ThemedControl
	{
		int _value;
		bool _dragging;
		bool _hover;

		public Slider()
		{
			SetStyle(ControlStyles.Selectable, true);
			TabStop = true;
			AccessibleRole = AccessibleRole.Slider;
			AutoSize = true;
			Minimum = 0;
			Maximum = 100;
			SmallChange = 1;
			LargeChange = 10;
		}

		public event EventHandler ValueChanged;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int Minimum { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int Maximum { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int SmallChange { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int LargeChange { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int Value
		{
			get { return _value; }
			set
			{
				int newValue = Math.Max(Minimum, Math.Min(Maximum, value));
				if (newValue != _value)
				{
					_value = newValue;
					AccessibleDescription = _value.ToString();
					Invalidate();
					if (ValueChanged != null)
					{
						ValueChanged(this, EventArgs.Empty);
					}
				}
			}
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			return new Size(S(200), S(32));
		}

		int ThumbSize { get { return S(20); } }

		Rectangle TrackRect
		{
			get { return new Rectangle(ThumbSize / 2, Height / 2 - S(2), Math.Max(1, Width - ThumbSize), S(4)); }
		}

		int ValueToX(int value)
		{
			Rectangle track = TrackRect;
			if (Maximum <= Minimum)
			{
				return track.Left;
			}

			return track.Left + (int)((long)(value - Minimum) * track.Width / (Maximum - Minimum));
		}

		int XToValue(int x)
		{
			Rectangle track = TrackRect;
			int value = Minimum + (int)((long)(x - track.Left) * (Maximum - Minimum) / Math.Max(1, track.Width));
			if (SmallChange > 1)
			{
				value = (int)Math.Round(value / (double)SmallChange) * SmallChange;
			}

			return value;
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if (e.Button == MouseButtons.Left)
			{
				Focus();
				_dragging = true;
				Capture = true;
				Value = XToValue(e.X);
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (_dragging)
			{
				Value = XToValue(e.X);
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			_dragging = false;
			Capture = false;
			Invalidate();
		}

		protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
		protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; Invalidate(); }
		protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
		protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

		protected override bool IsInputKey(Keys keyData)
		{
			switch (keyData)
			{
				case Keys.Left:
				case Keys.Right:
				case Keys.Up:
				case Keys.Down:
				case Keys.PageUp:
				case Keys.PageDown:
				case Keys.Home:
				case Keys.End:
					return true;
			}

			return base.IsInputKey(keyData);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			switch (e.KeyCode)
			{
				case Keys.Left:
				case Keys.Down: Value -= SmallChange; break;
				case Keys.Right:
				case Keys.Up: Value += SmallChange; break;
				case Keys.PageDown: Value -= LargeChange; break;
				case Keys.PageUp: Value += LargeChange; break;
				case Keys.Home: Value = Minimum; break;
				case Keys.End: Value = Maximum; break;
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(ParentBackColor);
			Rectangle track = TrackRect;
			int x = ValueToX(_value);

			Theme.FillRounded(g, track, S(2), Enabled ? Theme.ToggleOffStroke : Theme.TextDisabled);
			Rectangle filled = new Rectangle(track.Left, track.Top, Math.Max(0, x - track.Left), track.Height);
			if (filled.Width > 0)
			{
				Theme.FillRounded(g, filled, S(2), Enabled ? Theme.Accent : Theme.TextDisabled);
			}

			// thumb: a ring with an accent dot that grows when hovered
			Rectangle thumb = new Rectangle(x - ThumbSize / 2, (Height - ThumbSize) / 2, ThumbSize, ThumbSize);
			Theme.FillRounded(g, thumb, ThumbSize / 2, Theme.KeyCapFill, Theme.ControlStroke);
			int dot = S(_dragging ? 10 : (_hover ? 14 : 12));
			Rectangle dotRect = new Rectangle(x - dot / 2, (Height - dot) / 2, dot, dot);
			Theme.FillRounded(g, dotRect, dot / 2, Enabled ? Theme.Accent : Theme.TextDisabled);

			if (Focused && ShowFocusCues)
			{
				Rectangle focus = Rectangle.Inflate(thumb, S(2), S(2));
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				using (Pen pen = new Pen(Theme.TextPrimary, S(1)))
				using (System.Drawing.Drawing2D.GraphicsPath path = Theme.RoundedRect(focus, focus.Width / 2f))
				{
					g.DrawPath(pen, path);
				}
			}
		}
	}

	/// <summary>
	/// Lays out its children left to right at their natural size, vertically centred
	/// (used for groups of controls on the right hand side of a card)
	/// </summary>
	class HStack : Panel, IThemeAware
	{
		public HStack(params Control[] controls)
		{
			Spacing = 8;
			AutoSize = true;
			foreach (Control control in controls)
			{
				Controls.Add(control);
			}
		}

		/// <summary>
		/// Gap between children in logical pixels
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int Spacing { get; set; }

		public void ApplyTheme()
		{
			Invalidate();
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			int width = 0;
			int height = 0;
			foreach (Control child in Controls)
			{
				if (!child.Visible)
				{
					continue;
				}

				Size size = UiHelper.MeasureNatural(child);
				width += size.Width + LogicalToDeviceUnits(Spacing);
				height = Math.Max(height, size.Height);
			}

			return new Size(Math.Max(0, width - LogicalToDeviceUnits(Spacing)), height);
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			int x = 0;
			foreach (Control child in Controls)
			{
				if (!child.Visible)
				{
					continue;
				}

				Size size = UiHelper.MeasureNatural(child);
				child.SetBounds(x, (Height - size.Height) / 2, size.Width, size.Height);
				x += size.Width + LogicalToDeviceUnits(Spacing);
			}
		}

		protected override void OnControlAdded(ControlEventArgs e)
		{
			base.OnControlAdded(e);
			e.Control.VisibleChanged += (s, a) => { if (Parent != null) { Parent.PerformLayout(); } PerformLayout(); };
		}
	}
}
