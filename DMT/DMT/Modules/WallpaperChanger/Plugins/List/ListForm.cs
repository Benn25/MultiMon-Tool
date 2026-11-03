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

namespace DMT.Modules.WallpaperChanger.Plugins.List
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Windows.Forms;

	using DMT.Library.Settings;
	using DMT.Library.Ui;

	/// <summary>
	/// Editor for a wallpaper source that picks from a list of pictures.
	/// Pictures can be added from any number of folders and are shown as thumbnails.
	/// The list is saved as a text file (one picture per line) which DMT manages.
	/// </summary>
	class ListForm : ContentDialog
	{
		const string OrderRandom = "Random";
		const string OrderList = "In list order";

		ListConfig _config;
		string _listFnm;

		TextBox _name;
		ThumbnailGrid _grid;
		TextBlock _count;
		FluentButton _removeButton;
		ToggleSwitch _subfolders;
		ToggleSwitch _enabled;
		NumericUpDown _weight;
		ComboBox _order;
		ToggleSwitch _persist;
		SettingsCard _persistCard;
		SettingsCard _fileCard;
		InfoBar _errorBar;

		public ListForm(ListConfig config)
			: base("Picture list", 760)
		{
			_config = config;
			_listFnm = config.ListFnm;
			StartPosition = FormStartPosition.CenterScreen;

			Body.Add(new TextBlock("Name", TextRole.Caption, TextTone.Secondary));
			_name = Body.Add(new TextBox { Text = config.Description, AccessibleName = "Name" }, 4);

			FluentButton addPictures = new FluentButton("Add pictures", Theme.GlyphAdd, ButtonKind.Accent);
			addPictures.Click += AddPictures_Click;
			FluentButton addFolder = new FluentButton("Add a folder", Theme.GlyphFolder);
			addFolder.Click += AddFolder_Click;
			_removeButton = new FluentButton("Remove", Theme.GlyphDelete);
			_removeButton.Click += (s, e) => _grid.RemoveSelected();
			FluentButton removeMissing = new FluentButton("Remove missing", Theme.GlyphClear, ButtonKind.Subtle);
			removeMissing.Click += (s, e) => _grid.RemoveMissing();
			Body.Add(new HStack(addPictures, addFolder, _removeButton, removeMissing), 16);

			_grid = new ThumbnailGrid();
			_grid.ItemsChanged += (s, e) => UpdateCount();
			_grid.SelectionChanged += (s, e) => _removeButton.Enabled = _grid.SelectedCount > 0;
			CardFrame frame = new CardFrame(_grid) { Height = LogicalToDeviceUnits(330) };
			Body.Add(frame, 8);

			_count = Body.Add(new TextBlock(string.Empty, TextRole.Caption, TextTone.Secondary), 6);

			_subfolders = new ToggleSwitch { Checked = true };
			_subfolders.CheckedChanged += (s, e) => _grid.IncludeSubfolders = _subfolders.Checked;
			Body.Add(new SettingsCard("Include subfolders when adding a folder", null, Theme.GlyphFolder, _subfolders), 12);

			_order = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat }.WithLogicalWidth(160);
			_order.Items.AddRange(new object[] { OrderRandom, OrderList });
			_order.SelectedItem = config.Type == ListConfig.OrderType.List ? OrderList : OrderRandom;
			_order.SelectedIndexChanged += (s, e) => UpdatePersistCard();
			Body.Add(new SettingsCard("Order", "Show the pictures in a random order, or one after the other", Theme.GlyphSort, _order), 4);

			_persist = new ToggleSwitch { Checked = config.Persist };
			_persistCard = Body.Add(new SettingsCard("Carry on where it left off", "Remember the position in the list when DMT restarts", Theme.GlyphPin, _persist), 4);

			_enabled = new ToggleSwitch { Checked = config.Enabled };
			_weight = new NumericUpDown { Minimum = 0, Maximum = 100, Value = Math.Max(0, Math.Min(100, config.Weight)) }.WithLogicalWidth(70);
			Body.Add(new SettingsCard("Use this list", "Weight: how often it's picked compared with your other sources", Theme.GlyphPhoto, new HStack(_weight, _enabled) { Spacing = 16 }), 4);

			FluentButton useFile = new FluentButton("Use another file", "", ButtonKind.Subtle);
			useFile.Click += UseFile_Click;
			_fileCard = Body.Add(new SettingsCard("List file", string.Empty, "", useFile), 4);

			_errorBar = Body.Add(new InfoBar(Severity.Error, null, string.Empty), 8);
			_errorBar.Visible = false;

			PrimaryButton.Text = "Save";
			PrimaryButton.Click += Save_Click;

			LoadList(_listFnm);
			UpdatePersistCard();
			UpdateFileCard();
			_removeButton.Enabled = false;
		}

		/// <summary>
		/// Gets the configuration as edited by the user (call after the dialog returns OK)
		/// </summary>
		public ListConfig GetConfig()
		{
			ListConfig config = new ListConfig();
			config.Enabled = _enabled.Checked;
			config.Weight = (int)_weight.Value;
			config.Description = _name.Text;
			config.ListFnm = _listFnm;
			config.Type = (string)_order.SelectedItem == OrderList ? ListConfig.OrderType.List : ListConfig.OrderType.Random;
			config.Persist = _persist.Checked;
			return config;
		}

		void LoadList(string listFnm)
		{
			List<string> pictures = new List<string>();
			if (!string.IsNullOrEmpty(listFnm) && File.Exists(listFnm))
			{
				try
				{
					foreach (string line in File.ReadAllLines(listFnm))
					{
						string cleaned = line.Trim();
						if (cleaned.Length > 0 && cleaned[0] != '#')
						{
							pictures.Add(cleaned);
						}
					}
				}
				catch (Exception ex)
				{
					ShowError("Couldn't read the list: " + ex.Message);
				}
			}

			_grid.SetItems(pictures);
			UpdateCount();
		}

		void UpdateCount()
		{
			int count = _grid.Items.Count;
			_count.Text = count == 1 ? "1 picture" : count + " pictures";
		}

		void UpdatePersistCard()
		{
			_persistCard.Visible = (string)_order.SelectedItem == OrderList;
			if (IsHandleCreated)
			{
				FitToContent();
			}
		}

		void UpdateFileCard()
		{
			_fileCard.Description = string.IsNullOrEmpty(_listFnm) ? "Created for you when you save" : _listFnm;
		}

		void AddPictures_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog
			{
				Multiselect = true,
				Title = "Add pictures",
				Filter = "Pictures|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All files|*.*",
			})
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					_grid.AddItems(dlg.FileNames);
				}
			}
		}

		void AddFolder_Click(object sender, EventArgs e)
		{
			using (FolderBrowserDialog dlg = new FolderBrowserDialog { Description = "Add every picture in a folder", UseDescriptionForTitle = true, Multiselect = true })
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					Cursor = Cursors.WaitCursor;
					try
					{
						int added = _grid.AddItems(ThumbnailGrid.ExpandPaths(dlg.SelectedPaths, _subfolders.Checked));
						if (added == 0)
						{
							ShowError("No new pictures were found there.");
						}
						else
						{
							HideError();
						}
					}
					finally
					{
						Cursor = Cursors.Default;
					}
				}
			}
		}

		void UseFile_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog { Title = "Use a list file", Filter = "Text files|*.txt|All files|*.*", CheckFileExists = true })
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					_listFnm = dlg.FileName;
					LoadList(_listFnm);
					UpdateFileCard();
				}
			}
		}

		void Save_Click(object sender, EventArgs e)
		{
			try
			{
				if (string.IsNullOrEmpty(_listFnm))
				{
					_listFnm = NewListFilename(_name.Text);
				}

				Directory.CreateDirectory(Path.GetDirectoryName(_listFnm));
				List<string> lines = new List<string> { "# Picture list for Dual Monitor Tools (one picture per line)" };
				lines.AddRange(_grid.Items);
				File.WriteAllLines(_listFnm, lines);
			}
			catch (Exception ex)
			{
				ShowError("Couldn't save the list: " + ex.Message);
				return;
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		/// <summary>
		/// A new file in the settings folder, named after the list
		/// </summary>
		static string NewListFilename(string name)
		{
			string folder = Path.Combine(FileLocations.Instance.DataDirectory, "Picture lists");
			string safeName = new string((string.IsNullOrWhiteSpace(name) ? "Pictures" : name).Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray()).Trim();
			string path = Path.Combine(folder, safeName + ".txt");
			for (int n = 2; File.Exists(path); n++)
			{
				path = Path.Combine(folder, string.Format("{0} ({1}).txt", safeName, n));
			}

			return path;
		}

		void ShowError(string message)
		{
			_errorBar.Text = message;
			if (!_errorBar.Visible)
			{
				_errorBar.Visible = true;
				if (IsHandleCreated)
				{
					FitToContent();
				}
			}
		}

		void HideError()
		{
			if (_errorBar.Visible)
			{
				_errorBar.Visible = false;
				FitToContent();
			}
		}
	}
}
