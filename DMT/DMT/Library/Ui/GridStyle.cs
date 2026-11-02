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
	using System.Drawing;
	using System.Windows.Forms;

	/// <summary>
	/// Gives a DataGridView the look of the rest of the UI
	/// </summary>
	static class GridStyle
	{
		/// <summary>
		/// Mixes two colours (amount 0 = first colour, 1 = second colour)
		/// </summary>
		public static Color Blend(Color from, Color to, float amount)
		{
			return Color.FromArgb(
				(int)(from.R + (to.R - from.R) * amount),
				(int)(from.G + (to.G - from.G) * amount),
				(int)(from.B + (to.B - from.B) * amount));
		}

		public static void Apply(DataGridView grid)
		{
			Color selection = Blend(Theme.Surface, Theme.Accent, Theme.IsDark ? 0.35f : 0.22f);

			grid.BorderStyle = BorderStyle.None;
			grid.BackgroundColor = Theme.Surface;
			grid.GridColor = Theme.Divider;
			grid.EnableHeadersVisualStyles = false;
			grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
			grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
			grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
			grid.RowHeadersVisible = false;
			grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
			grid.ColumnHeadersHeight = grid.LogicalToDeviceUnits(36);
			grid.RowTemplate.Height = grid.LogicalToDeviceUnits(34);
			foreach (DataGridViewRow row in grid.Rows)
			{
				row.Height = grid.LogicalToDeviceUnits(34);
			}

			DataGridViewCellStyle header = grid.ColumnHeadersDefaultCellStyle;
			header.BackColor = Theme.Surface;
			header.ForeColor = Theme.TextSecondary;
			header.SelectionBackColor = Theme.Surface;
			header.SelectionForeColor = Theme.TextSecondary;
			header.Font = new Font(Theme.TextStrongFamily, 9.75f);
			header.Padding = new Padding(grid.LogicalToDeviceUnits(8), 0, 0, 0);
			header.WrapMode = DataGridViewTriState.False;

			DataGridViewCellStyle cell = grid.DefaultCellStyle;
			cell.BackColor = Theme.Surface;
			cell.ForeColor = Theme.TextPrimary;
			cell.SelectionBackColor = selection;
			cell.SelectionForeColor = Theme.TextPrimary;
			cell.Padding = new Padding(grid.LogicalToDeviceUnits(8), 0, grid.LogicalToDeviceUnits(4), 0);

			// no zebra stripes: the row lines are enough
			DataGridViewCellStyle alternate = grid.AlternatingRowsDefaultCellStyle;
			alternate.BackColor = Theme.Surface;
			alternate.ForeColor = Theme.TextPrimary;
			alternate.SelectionBackColor = selection;
			alternate.SelectionForeColor = Theme.TextPrimary;

			grid.RowsDefaultCellStyle.BackColor = Theme.Surface;
			grid.RowsDefaultCellStyle.ForeColor = Theme.TextPrimary;
			grid.RowsDefaultCellStyle.SelectionBackColor = selection;
			grid.RowsDefaultCellStyle.SelectionForeColor = Theme.TextPrimary;
		}
	}
}
