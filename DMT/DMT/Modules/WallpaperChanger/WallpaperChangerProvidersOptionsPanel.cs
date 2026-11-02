#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2015  Gerald Evans
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using DMT.Library.Ui;
using DMT.Library.WallpaperPlugin;
using DMT.Resources;

namespace DMT.Modules.WallpaperChanger
{
	/// <summary>
	/// The sources (providers) that wallpaper images are taken from
	/// </summary>
	class WallpaperChangerProvidersOptionsPanel : SettingsPage
	{
		WallpaperChangerModule _wallpaperChangerModule;
		DataGridView _grid;
		FluentButton _editButton;
		FluentButton _deleteButton;

		public WallpaperChangerProvidersOptionsPanel(WallpaperChangerModule wallpaperChangerModule)
		{
			_wallpaperChangerModule = wallpaperChangerModule;

			AddText("Wallpaper images are picked at random from these sources. A source with a higher weight is picked more often.");

			FluentButton addButton = new FluentButton("Add", Theme.GlyphAdd, ButtonKind.Accent);
			addButton.Click += AddButton_Click;
			_editButton = new FluentButton("Edit", Theme.GlyphEdit);
			_editButton.Click += (s, e) => EditSelected();
			_deleteButton = new FluentButton("Delete", Theme.GlyphDelete);
			_deleteButton.Click += DeleteButton_Click;
			Add(new HStack(addButton, _editButton, _deleteButton), 16);

			_grid = new DataGridView
			{
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				AutoGenerateColumns = false,
				EditMode = DataGridViewEditMode.EditOnEnter,
			};
			_grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = "Enabled", HeaderText = "On", Width = LogicalToDeviceUnits(56) });
			_grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Weight", HeaderText = "Weight", Width = LogicalToDeviceUnits(80) });
			_grid.Columns.Add(new DataGridViewImageColumn { DataPropertyName = "ProviderImage", HeaderText = string.Empty, Width = LogicalToDeviceUnits(48), ReadOnly = true });
			_grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProviderName", HeaderText = "Type", Width = LogicalToDeviceUnits(140), ReadOnly = true });
			_grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Description", HeaderText = "Description", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
			GridStyle.Apply(_grid);

			_grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && e.ColumnIndex > 1) { EditProvider(e.RowIndex); } };
			_grid.SelectionChanged += (s, e) => UpdateButtons();
			_grid.CellMouseUp += (s, e) => { if (e.ColumnIndex == 0 && e.RowIndex >= 0) { _grid.EndEdit(); } };
			_grid.CellValueChanged += (s, e) => { if (e.RowIndex >= 0 && e.ColumnIndex <= 1) { _wallpaperChangerModule.SaveProvidersConfiguration(); _grid.InvalidateRow(e.RowIndex); } };
			_grid.RowPrePaint += Grid_RowPrePaint;

			FillControl = Add(new CardFrame(_grid), 12);
			FillMinHeight = 200;

			_grid.DataSource = _wallpaperChangerModule.GetProvidersDataSource();
			UpdateButtons();
		}

		public override void ApplyTheme()
		{
			base.ApplyTheme();
			GridStyle.Apply(_grid);
		}

		void UpdateButtons()
		{
			_editButton.Enabled = _grid.SelectedRows.Count == 1;
			_deleteButton.Enabled = _grid.SelectedRows.Count > 0;
		}

		void AddButton_Click(object sender, EventArgs e)
		{
			ProviderForm dlg = new ProviderForm(_wallpaperChangerModule);
			if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
			{
				_wallpaperChangerModule.AddProvider(dlg.SelectedProviderName);
			}
		}

		void EditSelected()
		{
			if (_grid.SelectedRows.Count == 1)
			{
				EditProvider(_grid.SelectedRows[0].Index);
			}
		}

		void EditProvider(int rowIndex)
		{
			if (_wallpaperChangerModule.EditProvider(rowIndex))
			{
				_grid.Refresh();
			}
		}

		void DeleteButton_Click(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection selectedRows = _grid.SelectedRows;
			if (selectedRows.Count == 0)
			{
				return;
			}

			string msg;
			if (selectedRows.Count == 1)
			{
				IImageProvider provider = selectedRows[0].DataBoundItem as IImageProvider;
				msg = string.Format(WallpaperStrings.ConfirmDelProvider, provider != null ? provider.Description : "?");
			}
			else
			{
				msg = string.Format(WallpaperStrings.ConfirmDelProviders, selectedRows.Count);
			}

			if (MessageBox.Show(FindForm(), msg, CommonStrings.MyTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
			{
				List<int> rowIndexesToDelete = new List<int>();
				foreach (DataGridViewRow row in selectedRows)
				{
					rowIndexesToDelete.Add(row.Index);
				}

				_wallpaperChangerModule.DeleteProviders(rowIndexesToDelete);
				_grid.Refresh();
			}
		}

		void Grid_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
		{
			// sources that won't be used are greyed out
			BindingList<IImageProvider> providers = _wallpaperChangerModule.GetProvidersDataSource();
			if (e.RowIndex >= 0 && e.RowIndex < providers.Count)
			{
				IImageProvider provider = providers[e.RowIndex];
				bool used = provider.Enabled && provider.Weight > 0;
				_grid.Rows[e.RowIndex].DefaultCellStyle.ForeColor = used ? Theme.TextPrimary : Theme.TextDisabled;
			}
		}
	}
}
