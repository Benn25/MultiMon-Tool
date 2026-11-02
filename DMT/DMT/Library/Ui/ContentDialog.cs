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
	/// Stacks its children vertically at the full width (doesn't scroll)
	/// </summary>
	class VStack : Panel, IHeightForWidth, IThemeAware
	{
		Dictionary<Control, int> _spacing = new Dictionary<Control, int>();

		public VStack()
		{
			SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
		}

		public void ApplyTheme()
		{
			Invalidate();
		}

		public T Add<T>(T control, int spaceBefore = 8) where T : Control
		{
			_spacing[control] = _spacing.Count == 0 ? 0 : spaceBefore;
			Controls.Add(control);
			control.VisibleChanged += (s, e) =>
			{
				PerformLayout();
				if (Parent != null)
				{
					Parent.PerformLayout();
				}
			};
			return control;
		}

		public int GetHeightForWidth(int width)
		{
			return Arrange(width, false);
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			Arrange(Width, true);
		}

		int Arrange(int width, bool apply)
		{
			int y = 0;
			bool first = true;
			foreach (Control child in Controls)
			{
				if (!child.Visible)
				{
					continue;
				}

				int space;
				_spacing.TryGetValue(child, out space);
				y += first ? 0 : LogicalToDeviceUnits(space);
				first = false;

				int height = UiHelper.MeasureHeight(child, width);
				if (apply)
				{
					child.SetBounds(0, y, width, height);
				}

				y += height;
			}

			return y;
		}
	}

	/// <summary>
	/// A dialog in the style of a WinUI ContentDialog:
	/// a title, a body and a footer with the buttons.
	/// </summary>
	class ContentDialog : Form
	{
		TextBlock _title;
		Panel _footer;
		int _bodyWidth;

		public ContentDialog(string title, int bodyWidth = 440)
		{
			_bodyWidth = bodyWidth;
			Text = title;
			AutoScaleMode = AutoScaleMode.Dpi;
			AutoScaleDimensions = new SizeF(96F, 96F);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			ShowInTaskbar = false;
			ShowIcon = false;
			StartPosition = FormStartPosition.CenterParent;
			BackColor = Theme.Surface;
			ForeColor = Theme.TextPrimary;

			_title = new TextBlock(title, TextRole.Subtitle);
			Body = new VStack();
			_footer = new Panel { BackColor = Theme.Background };

			PrimaryButton = new FluentButton("OK", null, ButtonKind.Accent);
			SecondaryButton = new FluentButton("Cancel");
			SecondaryButton.DialogResult = DialogResult.Cancel;
			_footer.Controls.Add(PrimaryButton);
			_footer.Controls.Add(SecondaryButton);

			Controls.Add(_title);
			Controls.Add(Body);
			Controls.Add(_footer);

			AcceptButton = PrimaryButton;
			CancelButton = SecondaryButton;
		}

		/// <summary>
		/// Holds the content of the dialog
		/// </summary>
		protected VStack Body { get; private set; }

		protected FluentButton PrimaryButton { get; private set; }

		protected FluentButton SecondaryButton { get; private set; }

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			Theme.ApplyWindowChrome(this);
			FitToContent();
		}

		protected override void OnDpiChanged(DpiChangedEventArgs e)
		{
			base.OnDpiChanged(e);
			FitToContent();
		}

		/// <summary>
		/// Resizes the dialog to fit what's in the body (call after showing/hiding content)
		/// </summary>
		protected void FitToContent()
		{
			int pad = LogicalToDeviceUnits(24);
			int width = LogicalToDeviceUnits(_bodyWidth);
			int titleHeight = _title.GetHeightForWidth(width);
			int bodyHeight = Body.GetHeightForWidth(width);
			int footerHeight = LogicalToDeviceUnits(80);
			int bodyTop = pad + titleHeight + LogicalToDeviceUnits(16);

			ClientSize = new Size(width + pad * 2, bodyTop + bodyHeight + pad + footerHeight);
			_title.SetBounds(pad, pad, width, titleHeight);
			Body.SetBounds(pad, bodyTop, width, bodyHeight);
			_footer.SetBounds(0, ClientSize.Height - footerHeight, ClientSize.Width, footerHeight);

			// two equal buttons filling the footer, like WinUI
			int gap = LogicalToDeviceUnits(8);
			int buttonWidth = (width - gap) / 2;
			int buttonHeight = LogicalToDeviceUnits(32);
			int buttonTop = (footerHeight - buttonHeight) / 2;
			PrimaryButton.AutoSize = false;
			SecondaryButton.AutoSize = false;
			PrimaryButton.SetBounds(pad, buttonTop, buttonWidth, buttonHeight);
			SecondaryButton.SetBounds(pad + buttonWidth + gap, buttonTop, buttonWidth, buttonHeight);
		}
	}
}
