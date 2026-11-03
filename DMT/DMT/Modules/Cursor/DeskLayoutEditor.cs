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
	using System.ComponentModel;
	using System.Drawing;
	using System.Drawing.Drawing2D;
	using System.Globalization;
	using System.Windows.Forms;

	using DMT.Library.Ui;

	/// <summary>
	/// Shows the screens to scale as they sit on the desk, and lets the user drag them into place.
	/// Edges snap to the other screens (hold Alt to place freely), and the arrow keys nudge the selected screen.
	/// </summary>
	class DeskLayoutEditor : ThemedControl, IHeightForWidth
	{
		DeskLayout _layout;
		string[] _names = new string[0];

		// how the desk (mm) maps onto the control, kept still while dragging
		RectangleF _view;
		float _scale;
		PointF _origin;

		int _hoverIndex = -1;
		int _selectedIndex = -1;
		int _dragIndex = -1;
		PointF _dragStartMm;
		Point _dragStartMouse;
		bool _dragMoved;

		public DeskLayoutEditor()
		{
			SetStyle(ControlStyles.Selectable, true);
			TabStop = true;
			AccessibleRole = AccessibleRole.Diagram;
			AccessibleName = "Screens on the desk";
		}

		/// <summary>
		/// Raised when a screen has been moved (once dragging has finished)
		/// </summary>
		public event EventHandler LayoutChanged;

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DeskLayout Layout
		{
			get
			{
				return _layout;
			}

			set
			{
				_layout = value;
				_selectedIndex = -1;
				_hoverIndex = -1;
				_dragIndex = -1;
				UpdateView();
				if (Parent != null)
				{
					Parent.PerformLayout();
				}

				Invalidate();
			}
		}

		/// <summary>
		/// Names shown under the screen numbers
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string[] Names
		{
			get { return _names; }
			set { _names = value ?? new string[0]; Invalidate(); }
		}

		/// <summary>
		/// The tallest the editor will be (logical pixels)
		/// </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int MaxHeight { get; set; } = 360;

		/// <summary>
		/// Call after a screen's size has changed
		/// </summary>
		public void LayoutUpdated()
		{
			if (_dragIndex < 0)
			{
				UpdateView();
			}

			Invalidate();
		}

		public int GetHeightForWidth(int width)
		{
			RectangleF view = ViewFor(_layout);
			if (view.Width <= 0)
			{
				return S(200);
			}

			int height = (int)(width * view.Height / view.Width);
			return Math.Max(S(200), Math.Min(height, S(MaxHeight)));
		}

		/// <summary>
		/// The area of the desk to show: the screens with room around them to move them
		/// </summary>
		static RectangleF ViewFor(DeskLayout layout)
		{
			if (layout == null || layout.Screens.Count == 0)
			{
				return RectangleF.Empty;
			}

			RectangleF bounds = layout.Bounds;
			float margin = Math.Max(bounds.Width, bounds.Height) * 0.18f;
			return RectangleF.Inflate(bounds, margin, margin);
		}

		void UpdateView()
		{
			_view = ViewFor(_layout);
			if (_view.Width <= 0 || _view.Height <= 0 || Width <= 0 || Height <= 0)
			{
				_scale = 0;
				return;
			}

			Rectangle area = Rectangle.Inflate(ClientRectangle, -S(8), -S(8));
			_scale = Math.Min(area.Width / _view.Width, area.Height / _view.Height);
			float usedWidth = _view.Width * _scale;
			float usedHeight = _view.Height * _scale;
			_origin = new PointF(area.Left + (area.Width - usedWidth) / 2, area.Top + (area.Height - usedHeight) / 2);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			if (_dragIndex < 0)
			{
				UpdateView();
			}
		}

		RectangleF ToClient(RectangleF desk)
		{
			return new RectangleF(
				_origin.X + (desk.Left - _view.Left) * _scale,
				_origin.Y + (desk.Top - _view.Top) * _scale,
				desk.Width * _scale,
				desk.Height * _scale);
		}

		int ScreenAt(Point point)
		{
			if (_layout == null || _scale <= 0)
			{
				return -1;
			}

			// the selected screen is drawn on top, so check it first
			if (_selectedIndex >= 0 && ToClient(_layout.Screens[_selectedIndex].DeskRect).Contains(point))
			{
				return _selectedIndex;
			}

			for (int i = _layout.Screens.Count - 1; i >= 0; i--)
			{
				if (ToClient(_layout.Screens[i].DeskRect).Contains(point))
				{
					return i;
				}
			}

			return -1;
		}

		#region Mouse and keyboard

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (_dragIndex >= 0 && e.Button == MouseButtons.Left)
			{
				if (!_dragMoved && Math.Abs(e.X - _dragStartMouse.X) <= SystemInformation.DragSize.Width / 2
					&& Math.Abs(e.Y - _dragStartMouse.Y) <= SystemInformation.DragSize.Height / 2)
				{
					return;
				}

				_dragMoved = true;
				DeskScreen screen = _layout.Screens[_dragIndex];
				PointF position = new PointF(
					_dragStartMm.X + (e.X - _dragStartMouse.X) / _scale,
					_dragStartMm.Y + (e.Y - _dragStartMouse.Y) / _scale);
				if ((ModifierKeys & Keys.Alt) == 0)
				{
					position = Snap(_dragIndex, position, S(10) / _scale);
				}

				screen.PositionMm = position;
				Invalidate();
				return;
			}

			int index = ScreenAt(e.Location);
			if (index != _hoverIndex)
			{
				_hoverIndex = index;
				Cursor = index >= 0 ? Cursors.SizeAll : Cursors.Default;
				Invalidate();
			}
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			if (_dragIndex < 0)
			{
				_hoverIndex = -1;
				Invalidate();
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			Focus();
			if (e.Button != MouseButtons.Left)
			{
				return;
			}

			int index = ScreenAt(e.Location);
			_selectedIndex = index;
			if (index >= 0)
			{
				_dragIndex = index;
				_dragStartMm = _layout.Screens[index].PositionMm;
				_dragStartMouse = e.Location;
				_dragMoved = false;
				Capture = true;
			}

			Invalidate();
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (_dragIndex >= 0)
			{
				bool moved = _dragMoved;
				_dragIndex = -1;
				Capture = false;
				if (moved)
				{
					Finish();
				}
			}
		}

		protected override bool IsInputKey(Keys keyData)
		{
			Keys key = keyData & Keys.KeyCode;
			return key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down || base.IsInputKey(keyData);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (_layout == null || _layout.Screens.Count == 0)
			{
				return;
			}

			if (_selectedIndex < 0)
			{
				_selectedIndex = 0;
				Invalidate();
			}

			// 1 mm at a time, or 1 cm with Shift
			float step = e.Shift ? 10 : 1;
			float dx = 0;
			float dy = 0;
			switch (e.KeyCode)
			{
				case Keys.Left: dx = -step; break;
				case Keys.Right: dx = step; break;
				case Keys.Up: dy = -step; break;
				case Keys.Down: dy = step; break;
				default: return;
			}

			DeskScreen screen = _layout.Screens[_selectedIndex];
			screen.PositionMm = new PointF(screen.PositionMm.X + dx, screen.PositionMm.Y + dy);
			e.Handled = true;
			Finish();
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			Invalidate();
		}

		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			Invalidate();
		}

		void Finish()
		{
			_layout.Normalise();
			UpdateView();
			Invalidate();
			if (Parent != null)
			{
				Parent.PerformLayout();
			}

			if (LayoutChanged != null)
			{
				LayoutChanged(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Snaps the screen's edges (and centre) to those of the other screens when they are close
		/// </summary>
		PointF Snap(int index, PointF position, float threshold)
		{
			SizeF size = _layout.Screens[index].SizeMm;
			float bestX = threshold;
			float bestY = threshold;
			float snappedX = position.X;
			float snappedY = position.Y;

			for (int i = 0; i < _layout.Screens.Count; i++)
			{
				if (i == index)
				{
					continue;
				}

				RectangleF other = _layout.Screens[i].DeskRect;

				// side by side, or lined up with the other screen
				foreach (float target in new[] { other.Right, other.Left - size.Width, other.Left, other.Right - size.Width, other.Left + (other.Width - size.Width) / 2 })
				{
					float distance = Math.Abs(position.X - target);
					if (distance < bestX)
					{
						bestX = distance;
						snappedX = target;
					}
				}

				foreach (float target in new[] { other.Bottom, other.Top - size.Height, other.Top, other.Bottom - size.Height, other.Top + (other.Height - size.Height) / 2 })
				{
					float distance = Math.Abs(position.Y - target);
					if (distance < bestY)
					{
						bestY = distance;
						snappedY = target;
					}
				}
			}

			return new PointF(snappedX, snappedY);
		}

		#endregion

		#region Painting

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			g.Clear(ParentBackColor);
			if (_layout == null || _scale <= 0)
			{
				return;
			}

			g.SmoothingMode = SmoothingMode.AntiAlias;

			// the desk, with a 10 cm grid to give a sense of scale
			Rectangle desk = Rectangle.Round(ToClient(_view));
			Theme.FillRounded(g, desk, S(8), Theme.Surface, Theme.Stroke);
			using (Pen gridPen = new Pen(Color.FromArgb(Theme.IsDark ? 22 : 18, Theme.TextPrimary)))
			{
				g.SetClip(desk);
				float start = (float)Math.Ceiling(_view.Left / 100) * 100;
				for (float x = start; x < _view.Right; x += 100)
				{
					float cx = ToClient(new RectangleF(x, 0, 0, 0)).X;
					g.DrawLine(gridPen, cx, desk.Top, cx, desk.Bottom);
				}

				start = (float)Math.Ceiling(_view.Top / 100) * 100;
				for (float y = start; y < _view.Bottom; y += 100)
				{
					float cy = ToClient(new RectangleF(0, y, 0, 0)).Y;
					g.DrawLine(gridPen, desk.Left, cy, desk.Right, cy);
				}

				g.ResetClip();
			}

			for (int i = 0; i < _layout.Screens.Count; i++)
			{
				if (i != _selectedIndex)
				{
					DrawScreen(g, i);
				}
			}

			if (_selectedIndex >= 0 && _selectedIndex < _layout.Screens.Count)
			{
				DrawScreen(g, _selectedIndex);
			}

			DrawCrossings(g);

			Font captionFont = Theme.GetFont(TextRole.Caption, DeviceDpi);
			TextRenderer.DrawText(g, "Grid: 10 cm", captionFont, new Point(desk.Left + S(8), desk.Bottom - S(22)), Theme.TextTertiary, TextFormatFlags.NoPadding);
		}

		void DrawScreen(Graphics g, int index)
		{
			DeskScreen screen = _layout.Screens[index];
			Rectangle rect = Rectangle.Round(ToClient(screen.DeskRect));
			bool selected = index == _selectedIndex;
			bool hover = index == _hoverIndex;

			Color fill = selected ? GridStyle.Blend(Theme.ControlFill, Theme.Accent, Theme.IsDark ? 0.35f : 0.22f) : (hover ? Theme.ControlFillHover : Theme.ControlFill);
			Color stroke = selected ? Theme.Accent : (hover ? Theme.TextSecondary : Theme.ControlStroke);
			Theme.FillRounded(g, rect, S(4), fill, stroke);
			if (selected && Focused && ShowFocusCues)
			{
				Theme.FillRounded(g, Rectangle.Inflate(rect, -S(2), -S(2)), S(3), Color.Transparent, Theme.TextPrimary);
			}

			// number, name and size
			Font numberFont = Theme.GetFont(TextRole.Subtitle, DeviceDpi);
			Font captionFont = Theme.GetFont(TextRole.Caption, DeviceDpi);
			TextFormatFlags center = TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

			string number = (index + 1).ToString();
			string name = index < _names.Length ? _names[index] : string.Empty;
			SizeF sizeMm = screen.SizeMm;
			string size = string.Format(CultureInfo.CurrentCulture, "{0:0.#}″ · {1:0} × {2:0} cm", screen.DiagonalInches, sizeMm.Width / 10, sizeMm.Height / 10);
			if (TextRenderer.MeasureText(size, Theme.GetFont(TextRole.Caption, DeviceDpi), Size.Empty, TextFormatFlags.NoPadding).Width > rect.Width - S(8))
			{
				size = string.Format(CultureInfo.CurrentCulture, "{0:0.#}″", screen.DiagonalInches);
			}

			int numberHeight = TextRenderer.MeasureText(number, numberFont, Size.Empty, TextFormatFlags.NoPadding).Height;
			int captionHeight = TextRenderer.MeasureText("Ag", captionFont, Size.Empty, TextFormatFlags.NoPadding).Height;
			bool showName = !string.IsNullOrEmpty(name) && rect.Height > numberHeight + captionHeight * 2 + S(24);
			bool showSize = rect.Height > numberHeight + captionHeight + S(16);
			int blockHeight = numberHeight + (showName ? captionHeight + S(2) : 0) + (showSize ? captionHeight + S(2) : 0);
			int top = rect.Top + (rect.Height - blockHeight) / 2;
			Rectangle inner = new Rectangle(rect.Left + S(4), top, rect.Width - S(8), numberHeight);

			TextRenderer.DrawText(g, number, numberFont, inner, Theme.TextPrimary, center);
			inner.Y += numberHeight + S(2);
			inner.Height = captionHeight;
			if (showName)
			{
				TextRenderer.DrawText(g, name, captionFont, inner, Theme.TextSecondary, center);
				inner.Y += captionHeight + S(2);
			}

			if (showSize)
			{
				TextRenderer.DrawText(g, size, captionFont, inner, Theme.TextTertiary, center);
			}
		}

		/// <summary>
		/// Highlights where the screens touch, which is where the cursor will cross between them
		/// </summary>
		void DrawCrossings(Graphics g)
		{
			float touch = Math.Max(1f, S(3) / _scale);
			using (Pen pen = new Pen(Theme.Accent, S(3)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
			{
				for (int i = 0; i < _layout.Screens.Count; i++)
				{
					for (int j = i + 1; j < _layout.Screens.Count; j++)
					{
						RectangleF a = _layout.Screens[i].DeskRect;
						RectangleF b = _layout.Screens[j].DeskRect;

						float top = Math.Max(a.Top, b.Top);
						float bottom = Math.Min(a.Bottom, b.Bottom);
						if (bottom > top)
						{
							if (Math.Abs(a.Right - b.Left) <= touch)
							{
								DrawEdge(g, pen, new PointF(a.Right, top), new PointF(a.Right, bottom));
							}
							else if (Math.Abs(b.Right - a.Left) <= touch)
							{
								DrawEdge(g, pen, new PointF(a.Left, top), new PointF(a.Left, bottom));
							}
						}

						float left = Math.Max(a.Left, b.Left);
						float right = Math.Min(a.Right, b.Right);
						if (right > left)
						{
							if (Math.Abs(a.Bottom - b.Top) <= touch)
							{
								DrawEdge(g, pen, new PointF(left, a.Bottom), new PointF(right, a.Bottom));
							}
							else if (Math.Abs(b.Bottom - a.Top) <= touch)
							{
								DrawEdge(g, pen, new PointF(left, a.Top), new PointF(right, a.Top));
							}
						}
					}
				}
			}
		}

		void DrawEdge(Graphics g, Pen pen, PointF fromMm, PointF toMm)
		{
			PointF from = ToClient(new RectangleF(fromMm, SizeF.Empty)).Location;
			PointF to = ToClient(new RectangleF(toMm, SizeF.Empty)).Location;
			g.DrawLine(pen, from, to);
		}

		#endregion
	}
}
