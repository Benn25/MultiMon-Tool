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

	/// <summary>
	/// A rounded card holding a single setting: an icon, a title, an optional description
	/// and the control used to change the setting.
	/// When there isn't enough room, the control moves underneath the text.
	/// An optional content control can be placed underneath, spanning the card.
	/// </summary>
	class SettingsCard : Panel, IHeightForWidth, IThemeAware
	{
		TextBlock _title;
		TextBlock _description;
		Control _action;
		Control _content;
		string _glyph;
		bool _inLayout;

		public SettingsCard(string title, string description = null, string glyph = null, Control action = null)
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			BackColor = Theme.Surface;
			_glyph = glyph;
			SuspendLayout();

			_title = new TextBlock(title, TextRole.Body, TextTone.Primary);
			Controls.Add(_title);

			_description = new TextBlock(description ?? string.Empty, TextRole.Caption, TextTone.Secondary);
			_description.Visible = !string.IsNullOrEmpty(description);
			Controls.Add(_description);

			AccessibleName = title;
			Action = action;
			ResumeLayout(false);
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string Title
		{
			get { return _title.Text; }
			set { _title.Text = value; AccessibleName = value; }
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string Description
		{
			get { return _description.Text; }
			set
			{
				_description.Text = value ?? string.Empty;
				_description.Visible = !string.IsNullOrEmpty(value);
				PerformLayout();
			}
		}

		/// <summary>
		/// The control shown on the right of the card (or below the text when narrow)
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Control Action
		{
			get { return _action; }
			set
			{
				if (_action != null)
				{
					Controls.Remove(_action);
				}

				_action = value;
				if (_action != null)
				{
					Controls.Add(_action);
				}

				PerformLayout();
			}
		}

		/// <summary>
		/// A control shown underneath, using the full width of the card
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Control Content
		{
			get { return _content; }
			set
			{
				if (_content != null)
				{
					Controls.Remove(_content);
				}

				_content = value;
				if (_content != null)
				{
					Controls.Add(_content);
				}

				PerformLayout();
			}
		}

		public void ApplyTheme()
		{
			BackColor = Theme.Surface;
			Invalidate();
		}

		public int GetHeightForWidth(int width)
		{
			return Arrange(width, false);
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (_inLayout || _description == null)
			{
				return;
			}

			_inLayout = true;
			Arrange(Width, true);
			_inLayout = false;
		}

		protected override void OnControlAdded(ControlEventArgs e)
		{
			base.OnControlAdded(e);
			e.Control.VisibleChanged += Child_VisibleChanged;
		}

		protected override void OnControlRemoved(ControlEventArgs e)
		{
			e.Control.VisibleChanged -= Child_VisibleChanged;
			base.OnControlRemoved(e);
		}

		void Child_VisibleChanged(object sender, EventArgs e)
		{
			if (Parent != null)
			{
				Parent.PerformLayout();
			}

			PerformLayout();
		}

		int S(int logical)
		{
			return LogicalToDeviceUnits(logical);
		}

		int TextLeft
		{
			get { return string.IsNullOrEmpty(_glyph) ? S(16) : S(16 + 20 + 16); }
		}

		/// <summary>
		/// Works out where everything goes for the given width, returning the height needed.
		/// If apply is true, the children are moved into place.
		/// </summary>
		int Arrange(int width, bool apply)
		{
			int padH = S(16);
			int padV = S(8);
			int textLeft = TextLeft;
			int right = width - padH;

			Size actionSize = Size.Empty;
			bool hasAction = _action != null && _action.Visible;
			if (hasAction)
			{
				actionSize = UiHelper.MeasureNatural(_action);
			}

			// put the action underneath the text if the text would get too squashed
			int sideTextWidth = right - textLeft - (hasAction ? actionSize.Width + S(24) : 0);
			bool stacked = hasAction && sideTextWidth < S(200);
			int textWidth = Math.Max(stacked ? right - textLeft : sideTextWidth, S(40));

			int titleHeight = _title.GetHeightForWidth(textWidth);
			int descriptionHeight = _description.Visible ? _description.GetHeightForWidth(textWidth) : 0;
			int textHeight = titleHeight + (_description.Visible ? S(1) + descriptionHeight : 0);

			int rowHeight;
			int y = padV;
			if (stacked)
			{
				rowHeight = textHeight + S(8) + actionSize.Height;
				if (apply)
				{
					_title.SetBounds(textLeft, y, textWidth, titleHeight);
					_description.SetBounds(textLeft, y + titleHeight + S(1), textWidth, descriptionHeight);
					_action.SetBounds(textLeft, y + textHeight + S(8), Math.Min(actionSize.Width, right - textLeft), actionSize.Height);
				}
			}
			else
			{
				rowHeight = Math.Max(Math.Max(textHeight, actionSize.Height), S(34));
				if (apply)
				{
					int textTop = y + (rowHeight - textHeight) / 2;
					_title.SetBounds(textLeft, textTop, textWidth, titleHeight);
					_description.SetBounds(textLeft, textTop + titleHeight + S(1), textWidth, descriptionHeight);
					if (hasAction)
					{
						_action.SetBounds(right - actionSize.Width, y + (rowHeight - actionSize.Height) / 2, actionSize.Width, actionSize.Height);
					}
				}
			}

			y += rowHeight;

			if (_content != null && _content.Visible)
			{
				y += S(10);
				int contentWidth = width - padH * 2;
				int contentHeight = UiHelper.MeasureHeight(_content, contentWidth);
				if (apply)
				{
					_content.SetBounds(padH, y, contentWidth, contentHeight);
				}

				y += contentHeight;
			}

			return y + padV;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(Parent != null ? Parent.BackColor : Theme.Background);
			Theme.FillRounded(g, ClientRectangle, S(6), Theme.Surface, Theme.Stroke);

			if (!string.IsNullOrEmpty(_glyph))
			{
				int iconSize = S(20);
				int top = _title.Top + (_title.Height + (_description.Visible ? _description.Height + S(1) : 0) - iconSize) / 2;
				if (_content != null && _content.Visible && _action == null)
				{
					top = _title.Top;
				}

				TextRenderer.DrawText(g, _glyph, Theme.GetFont(TextRole.IconLarge, DeviceDpi), new Rectangle(S(16), top, iconSize, iconSize),
					Theme.TextPrimary, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
			}
		}
	}

	/// <summary>
	/// A rounded card that holds a single control filling it (e.g. a list)
	/// </summary>
	class CardFrame : Panel, IThemeAware
	{
		Control _child;

		public CardFrame(Control child)
		{
			SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			BackColor = Theme.Surface;
			_child = child;
			Controls.Add(child);
		}

		public void ApplyTheme()
		{
			BackColor = Theme.Surface;
			Invalidate();
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			int inset = LogicalToDeviceUnits(6);
			_child.SetBounds(inset, inset, Math.Max(0, Width - inset * 2), Math.Max(0, Height - inset * 2));
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.Clear(Parent != null ? Parent.BackColor : Theme.Background);
			Theme.FillRounded(e.Graphics, ClientRectangle, LogicalToDeviceUnits(6), Theme.Surface, Theme.Stroke);
		}
	}

	/// <summary>
	/// A scrolling page of settings.
	/// Children are stacked vertically and stretched to the width of the page (up to a maximum).
	/// </summary>
	class SettingsPage : UserControl, IThemeAware
	{
		Dictionary<Control, int> _spacing = new Dictionary<Control, int>();
		bool _inLayout;

		public SettingsPage()
		{
			SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
			AutoScaleMode = AutoScaleMode.None;
			AutoScroll = true;
			BackColor = Theme.Background;
			MaxContentWidth = 1000;
		}

		/// <summary>
		/// The widest that the content will be stretched to (in logical pixels)
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int MaxContentWidth { get; set; }

		/// <summary>
		/// A control (e.g. a list) that is given whatever height is left over in the page
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Control FillControl { get; set; }

		/// <summary>
		/// The smallest height (logical pixels) for the FillControl
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int FillMinHeight { get; set; } = 240;

		public virtual void ApplyTheme()
		{
			BackColor = Theme.Background;
		}

		/// <summary>
		/// Adds a control to the bottom of the page
		/// </summary>
		/// <param name="control">Control to add</param>
		/// <param name="spaceBefore">Gap above the control (logical pixels)</param>
		public T Add<T>(T control, int spaceBefore = 4) where T : Control
		{
			_spacing[control] = spaceBefore;
			Controls.Add(control);
			control.VisibleChanged += (s, e) => PerformLayout();
			return control;
		}

		/// <summary>
		/// Adds a section heading
		/// </summary>
		public TextBlock AddHeader(string text)
		{
			bool first = _spacing.Count == 0;
			return Add(new TextBlock(text, TextRole.BodyStrong), first ? 0 : 20);
		}

		/// <summary>
		/// Adds a paragraph of explanatory text
		/// </summary>
		public TextBlock AddText(string text, TextTone tone = TextTone.Secondary)
		{
			return Add(new TextBlock(text, TextRole.Body, tone), _spacing.Count == 0 ? 0 : 8);
		}

		public SettingsCard AddCard(string title, string description = null, string glyph = null, Control action = null)
		{
			bool afterHeader = Controls.Count > 0 && Controls[Controls.Count - 1] is TextBlock;
			return Add(new SettingsCard(title, description, glyph, action), afterHeader ? 6 : 3);
		}

		/// <summary>
		/// Adds a card with an on/off switch
		/// </summary>
		public ToggleSwitch AddToggle(string title, string description, string glyph, bool value, Action<bool> setter)
		{
			ToggleSwitch toggle = new ToggleSwitch { Checked = value };
			toggle.CheckedChanged += (s, e) => setter(toggle.Checked);
			AddCard(title, description, glyph, toggle);
			return toggle;
		}

		/// <summary>
		/// Creates a number entry box
		/// </summary>
		public NumericUpDown MakeNumber(int minimum, int maximum, int value, Action<int> setter, int width = 90)
		{
			NumericUpDown number = new NumericUpDown { Minimum = minimum, Maximum = maximum }.WithLogicalWidth(width);
			number.Value = Math.Max(minimum, Math.Min(maximum, value));
			number.ValueChanged += (s, e) => setter((int)number.Value);
			return number;
		}

		/// <summary>
		/// Creates a drop down list
		/// </summary>
		public ComboBox MakeCombo(IEnumerable<object> items, object selected, Action<object> setter, int width = 220)
		{
			ComboBox combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat }.WithLogicalWidth(width);
			foreach (object item in items)
			{
				combo.Items.Add(item);
			}

			if (selected != null)
			{
				combo.SelectedItem = selected;
			}

			if (combo.SelectedIndex < 0 && combo.Items.Count > 0)
			{
				combo.SelectedIndex = 0;
			}

			combo.SelectedIndexChanged += (s, e) =>
			{
				if (combo.SelectedItem != null)
				{
					setter(combo.SelectedItem);
				}
			};
			return combo;
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			if (_inLayout)
			{
				return;
			}

			_inLayout = true;
			try
			{
				// laying out can show/hide the scroll bar, changing the width, so allow a second pass
				for (int pass = 0; pass < 2; pass++)
				{
					int clientWidth = ClientSize.Width;
					Arrange(clientWidth);

					// the base layout is what shows/updates the scroll bar for AutoScrollMinSize
					base.OnLayout(e);
					if (ClientSize.Width == clientWidth)
					{
						break;
					}
				}
			}
			finally
			{
				_inLayout = false;
			}
		}

		void Arrange(int clientWidth)
		{
			int pad = LogicalToDeviceUnits(36);
			int contentWidth = Math.Max(Math.Min(clientWidth - pad * 2, LogicalToDeviceUnits(MaxContentWidth)), LogicalToDeviceUnits(160));
			int top = LogicalToDeviceUnits(4);
			int bottom = LogicalToDeviceUnits(36);
			int scroll = AutoScrollPosition.Y;

			// work out how much height is left for the fill control
			int fillHeight = 0;
			if (FillControl != null && FillControl.Visible)
			{
				int used = top + bottom;
				foreach (Control child in Controls)
				{
					if (child.Visible)
					{
						int space;
						_spacing.TryGetValue(child, out space);
						used += LogicalToDeviceUnits(space) + (child == FillControl ? 0 : UiHelper.MeasureHeight(child, contentWidth));
					}
				}

				fillHeight = Math.Max(LogicalToDeviceUnits(FillMinHeight), ClientSize.Height - used);
			}

			int y = top;
			foreach (Control child in Controls)
			{
				if (!child.Visible)
				{
					continue;
				}

				int space;
				_spacing.TryGetValue(child, out space);
				y += LogicalToDeviceUnits(space);
				int height = child == FillControl ? fillHeight : UiHelper.MeasureHeight(child, contentWidth);
				child.SetBounds(pad, y + scroll, contentWidth, height);
				y += height;
			}

			AutoScrollMinSize = new Size(0, y + bottom);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			PerformLayout();
		}
	}
}
