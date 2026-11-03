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
using DMT.Library.Environment;
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
		DataGridViewTextBoxColumn _screensColumn;
		TextBlock _screensHeader;
		TextBlock _screensText;
		List<Slider> _screenSliders = new List<Slider>();
		List<SettingsCard> _screenCards = new List<SettingsCard>();
		IImageProvider _selectedProvider;
		bool _loadingShares;
		bool _shareChanged;

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
			_screensColumn = new DataGridViewTextBoxColumn { HeaderText = "Screens", AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, ReadOnly = true };
			_grid.Columns.Add(_screensColumn);
			_grid.CellFormatting += Grid_CellFormatting;
			GridStyle.Apply(_grid);

			_grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0 && e.ColumnIndex > 1) { EditProvider(e.RowIndex); } };
			_grid.SelectionChanged += (s, e) => UpdateButtons();
			_grid.CellMouseUp += (s, e) => { if (e.ColumnIndex == 0 && e.RowIndex >= 0) { _grid.EndEdit(); } };
			_grid.CellValueChanged += (s, e) => { if (e.RowIndex >= 0 && e.ColumnIndex <= 1) { _wallpaperChangerModule.SaveProvidersConfiguration(); _grid.InvalidateRow(e.RowIndex); } };
			_grid.RowPrePaint += Grid_RowPrePaint;

			FillControl = Add(new CardFrame(_grid), 12);
			FillMinHeight = 200;

			// how much the selected source is used on each screen
			_screensHeader = AddHeader("Screens");
			_screensText = AddText(string.Empty);
			BuildScreenCards();

			_grid.DataSource = _wallpaperChangerModule.GetProvidersDataSource();
			UpdateButtons();
		}

		void BuildScreenCards()
		{
			string[] names = MonitorDescriptions.GetNames();
			for (int i = 0; i < Monitor.AllMonitors.Count; i++)
			{
				int screenIndex = i;
				Slider slider = new Slider { Minimum = 0, Maximum = 100, SmallChange = 5, LargeChange = 25, AccessibleName = string.Format("Use on screen {0}", i + 1) };
				TextBlock value = new TextBlock("100%", TextRole.Body) { Wrap = false };
				value.WithLogicalWidth(44);
				value.Height = LogicalToDeviceUnits(24);
				slider.ValueChanged += (s, e) =>
				{
					value.Text = slider.Value + "%";
					if (!_loadingShares && _selectedProvider != null)
					{
						ProviderScreenShares.Set(_selectedProvider, screenIndex, slider.Value);
						_shareChanged = true;
						_grid.AutoResizeColumn(_screensColumn.Index);
						_grid.InvalidateColumn(_screensColumn.Index);
					}
				};

				// save once the user has finished with the slider, rather than on every step
				slider.MouseUp += (s, e) => SaveShares();
				slider.KeyUp += (s, e) => SaveShares();
				slider.LostFocus += (s, e) => SaveShares();

				string title = string.IsNullOrEmpty(names[i]) ? string.Format("Screen {0}", i + 1) : string.Format("Screen {0} · {1}", i + 1, names[i]);
				SettingsCard card = AddCard(title, null, Theme.GlyphMonitor, new HStack(slider, value) { Spacing = 12 });
				_screenSliders.Add(slider);
				_screenCards.Add(card);
			}
		}

		void SaveShares()
		{
			if (_shareChanged)
			{
				_shareChanged = false;
				_wallpaperChangerModule.SaveProvidersConfiguration();
			}
		}

		void ShowScreenShares()
		{
			_selectedProvider = _grid.SelectedRows.Count == 1 ? _grid.SelectedRows[0].DataBoundItem as IImageProvider : null;
			bool show = _selectedProvider != null;
			_screensText.Text = show
				? string.Format("How often \"{0}\" is used on each screen, compared with its usual weight. " +
					"A screen that no source is used on picks from all of them.", _selectedProvider.Description)
				: "Select a source to choose which screens it's used on.";

			_loadingShares = true;
			for (int i = 0; i < _screenSliders.Count; i++)
			{
				_screenCards[i].Visible = show;
				_screenSliders[i].Value = show ? ProviderScreenShares.Get(_selectedProvider, i) : 100;
			}

			_loadingShares = false;
		}

		void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.ColumnIndex == _screensColumn.Index && e.RowIndex >= 0)
			{
				IImageProvider provider = _grid.Rows[e.RowIndex].DataBoundItem as IImageProvider;
				if (provider != null)
				{
					e.Value = ProviderScreenShares.Describe(provider, Monitor.AllMonitors.Count);
					e.FormattingApplied = true;
				}
			}
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
			SaveShares();
			ShowScreenShares();
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
