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

	/// <summary>
	/// Works out where the cursor should appear when it leaves one screen for another,
	/// so that it crosses at the same physical place as on the user's desk
	/// (rather than at the same pixel row or column, which is all Windows knows about).
	/// Immutable, so it can be swapped in for the mouse hook to use without locking.
	/// </summary>
	class DeskCursorMapper
	{
		readonly Rectangle[] _pixels;
		readonly RectangleF[] _desk;

		/// <param name="pixels">Each screen's bounds in desktop pixels</param>
		/// <param name="desk">Where each screen is on the desk (any unit, e.g. mm), in the same order</param>
		public DeskCursorMapper(IList<Rectangle> pixels, IList<RectangleF> desk)
		{
			if (pixels.Count != desk.Count)
			{
				throw new ArgumentException("Need a desk position for every screen");
			}

			_pixels = new Rectangle[pixels.Count];
			_desk = new RectangleF[desk.Count];
			pixels.CopyTo(_pixels, 0);
			desk.CopyTo(_desk, 0);
		}

		public int ScreenCount
		{
			get { return _pixels.Length; }
		}

		/// <summary>
		/// Index of the screen containing the point, or -1
		/// </summary>
		public int ScreenAt(Point point)
		{
			for (int i = 0; i < _pixels.Length; i++)
			{
				if (_pixels[i].Contains(point))
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// The cursor is on screen 'from' and Windows wants to move it to 'to', which is off that screen.
		/// Works out where on the screen physically beyond that edge it should go instead.
		/// </summary>
		/// <returns>False if the cursor isn't leaving the screen, or there's no screen on the desk beyond that edge</returns>
		public bool TryMap(int from, Point to, out Point result)
		{
			result = to;
			if (from < 0 || from >= _pixels.Length)
			{
				return false;
			}

			Rectangle src = _pixels[from];
			if (src.Contains(to))
			{
				return false;
			}

			// how far past each edge the cursor has gone
			int overX = to.X >= src.Right ? to.X - src.Right + 1 : (to.X < src.Left ? to.X - src.Left : 0);
			int overY = to.Y >= src.Bottom ? to.Y - src.Bottom + 1 : (to.Y < src.Top ? to.Y - src.Top : 0);
			bool horizontal = Math.Abs(overX) * src.Height >= Math.Abs(overY) * src.Width && overX != 0;

			// where the cursor leaves the screen, on the desk
			RectangleF srcDesk = _desk[from];
			PointF exit;
			if (horizontal)
			{
				float y = srcDesk.Top + Fraction(to.Y, src.Top, src.Height) * srcDesk.Height;
				exit = new PointF(overX > 0 ? srcDesk.Right : srcDesk.Left, y);
			}
			else
			{
				float x = srcDesk.Left + Fraction(to.X, src.Left, src.Width) * srcDesk.Width;
				exit = new PointF(x, overY > 0 ? srcDesk.Bottom : srcDesk.Top);
			}

			int target = FindTarget(from, horizontal, horizontal ? overX > 0 : overY > 0, exit);
			if (target < 0)
			{
				return false;
			}

			// the same physical spot on the target screen (or as near as it gets)
			Rectangle dest = _pixels[target];
			RectangleF destDesk = _desk[target];
			if (horizontal)
			{
				int depth = Math.Min(Math.Abs(overX) - 1, dest.Width - 1);
				int x = overX > 0 ? dest.Left + depth : dest.Right - 1 - depth;
				int y = dest.Top + ToPixel((exit.Y - destDesk.Top) / destDesk.Height, dest.Height);
				result = new Point(x, y);
			}
			else
			{
				int depth = Math.Min(Math.Abs(overY) - 1, dest.Height - 1);
				int y = overY > 0 ? dest.Top + depth : dest.Bottom - 1 - depth;
				int x = dest.Left + ToPixel((exit.X - destDesk.Left) / destDesk.Width, dest.Width);
				result = new Point(x, y);
			}

			return true;
		}

		/// <summary>
		/// The screen on the desk beyond the given edge of screen 'from' that is nearest to the exit point
		/// </summary>
		int FindTarget(int from, bool horizontal, bool forwards, PointF exit)
		{
			RectangleF srcDesk = _desk[from];
			int best = -1;
			float bestDistance = float.MaxValue;
			for (int i = 0; i < _desk.Length; i++)
			{
				if (i == from)
				{
					continue;
				}

				RectangleF d = _desk[i];

				// must be beyond that edge (allowing a little overlap from rough placement)
				bool beyond;
				if (horizontal)
				{
					float slack = Math.Min(d.Width, srcDesk.Width) / 2;
					beyond = forwards ? d.Left >= srcDesk.Right - slack : d.Right <= srcDesk.Left + slack;
				}
				else
				{
					float slack = Math.Min(d.Height, srcDesk.Height) / 2;
					beyond = forwards ? d.Top >= srcDesk.Bottom - slack : d.Bottom <= srcDesk.Top + slack;
				}

				if (!beyond)
				{
					continue;
				}

				float dx = Math.Max(0, Math.Max(d.Left - exit.X, exit.X - d.Right));
				float dy = Math.Max(0, Math.Max(d.Top - exit.Y, exit.Y - d.Bottom));
				float distance = dx * dx + dy * dy;
				if (distance < bestDistance)
				{
					bestDistance = distance;
					best = i;
				}
			}

			return best;
		}

		/// <summary>
		/// How far (0 to 1) a pixel is along a span, using the middle of the pixel
		/// </summary>
		static float Fraction(int value, int start, int length)
		{
			int clamped = Math.Max(start, Math.Min(start + length - 1, value));
			return (clamped - start + 0.5f) / length;
		}

		static int ToPixel(float fraction, int length)
		{
			int pixel = (int)Math.Floor(fraction * length);
			return Math.Max(0, Math.Min(length - 1, pixel));
		}
	}
}
