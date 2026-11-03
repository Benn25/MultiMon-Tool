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
	using System.Drawing;
	using System.Globalization;
	using System.Windows.Forms;

	using DMT.Library.Ui;

	/// <summary>
	/// Lets the user arrange the screens as they really are on the desk,
	/// so the cursor crosses between them at the matching physical place
	/// </summary>
	class CursorDeskOptionsPanel : SettingsPage
	{
		CursorModule _cursorModule;
		DeskLayout _layout;
		DeskLayoutEditor _editor;
		VStack _sizeCards;
		bool _loading;

		public CursorDeskOptionsPanel(CursorModule cursorModule)
		{
			_cursorModule = cursorModule;

			AddText("Windows moves the cursor to the same row of pixels on the next screen, so when your screens are different sizes " +
				"or resolutions it appears somewhere else entirely. Show where your screens really are, and the cursor will cross " +
				"between them at the matching place.");

			ToggleSwitch enabled = new ToggleSwitch { Checked = _cursorModule.DeskLayoutEnabled };
			enabled.CheckedChanged += (s, e) => _cursorModule.DeskLayoutEnabled = enabled.Checked;
			Add(new SettingsCard("Cross screens where they meet on your desk", "Keeps the cursor at the same real-world height (or position) as it moves to another screen", Theme.GlyphMouse, enabled), 16);

			AddHeader("Your desk");
			AddText("Drag the screens to where they are in front of you. They're drawn to scale, and the highlighted edges are where the cursor crosses. " +
				"Edges snap together (hold Alt to place freely); the arrow keys nudge the selected screen (Shift for 1 cm).");

			_editor = Add(new DeskLayoutEditor(), 12);
			_editor.LayoutChanged += (s, e) => Save();

			FluentButton reset = new FluentButton("Arrange like Windows", Theme.GlyphRefresh);
			reset.Click += (s, e) =>
			{
				_layout.ArrangeLikeWindows();
				_editor.LayoutUpdated();
				PerformLayout();
				Save();
			};
			Add(new HStack(reset), 8);

			AddHeader("Screen sizes");
			AddText("Sizes are read from the screens themselves. If one is wrong (some portable and TV screens don't say), enter its diagonal.");
			_sizeCards = Add(new VStack(), 8);

			Reload();
		}

		/// <summary>
		/// Re-reads the layout (call when the screens have changed)
		/// </summary>
		public void Reload()
		{
			_layout = _cursorModule.GetDeskLayout();
			_editor.Names = MonitorDescriptions.GetNames();
			_editor.Layout = _layout;
			BuildSizeCards();
			PerformLayout();
		}

		void BuildSizeCards()
		{
			_loading = true;
			while (_sizeCards.Controls.Count > 0)
			{
				Control old = _sizeCards.Controls[0];
				_sizeCards.Controls.RemoveAt(0);
				old.Dispose();
			}

			string[] names = _editor.Names;
			for (int i = 0; i < _layout.Screens.Count; i++)
			{
				DeskScreen screen = _layout.Screens[i];
				NumericUpDown diagonal = new NumericUpDown
				{
					Minimum = 5,
					Maximum = 150,
					DecimalPlaces = 1,
					Increment = 0.5m,
					AccessibleName = string.Format("Diagonal of screen {0} in inches", i + 1),
				}.WithLogicalWidth(80);
				diagonal.Value = Math.Max(diagonal.Minimum, Math.Min(diagonal.Maximum, (decimal)Math.Round(screen.DiagonalInches, 1)));
				diagonal.ValueChanged += (s, e) => SizeChanged(screen, (float)diagonal.Value);

				TextBlock inches = new TextBlock("inches", TextRole.Body, TextTone.Secondary) { Wrap = false };

				string title = string.IsNullOrEmpty(names[i]) ? string.Format("Screen {0}", i + 1) : string.Format("Screen {0} · {1}", i + 1, names[i]);
				string description = screen.ReportedMm.IsEmpty
					? string.Format("Doesn't report its size · {0} × {1} pixels", screen.Pixels.Width, screen.Pixels.Height)
					: string.Format(CultureInfo.CurrentCulture, "Reports {0:0.#}″ ({1:0} × {2:0} mm) · {3} × {4} pixels",
						screen.ReportedDiagonalInches, screen.ReportedMm.Width, screen.ReportedMm.Height, screen.Pixels.Width, screen.Pixels.Height);

				_sizeCards.Add(new SettingsCard(title, description, Theme.GlyphMonitor, new HStack(diagonal, inches) { Spacing = 8 }), 3);
			}

			_loading = false;
		}

		void SizeChanged(DeskScreen screen, float diagonalInches)
		{
			if (_loading)
			{
				return;
			}

			// keep the screen centred where it was
			RectangleF before = screen.DeskRect;
			bool isReported = !screen.ReportedMm.IsEmpty && Math.Abs(diagonalInches - screen.ReportedDiagonalInches) < 0.05f;
			screen.DiagonalOverride = isReported ? 0 : diagonalInches;
			SizeF after = screen.SizeMm;
			screen.PositionMm = new PointF(before.Left + (before.Width - after.Width) / 2, before.Top + (before.Height - after.Height) / 2);

			_layout.Normalise();
			_editor.LayoutUpdated();
			PerformLayout();
			Save();
		}

		void Save()
		{
			_cursorModule.SaveDeskLayout(_layout);
		}
	}
}
