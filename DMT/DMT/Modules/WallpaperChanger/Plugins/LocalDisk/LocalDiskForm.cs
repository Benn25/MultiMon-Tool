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

namespace DMT.Modules.WallpaperChanger.Plugins.LocalDisk
{
	using System;
	using System.IO;
	using System.Windows.Forms;

	using DMT.Library.Ui;

	/// <summary>
	/// Settings for a wallpaper source that takes pictures from a folder.
	/// (Which screens it's used on is chosen for every source on the Sources page.)
	/// </summary>
	class LocalDiskForm : ContentDialog
	{
		const string OrderRandom = "Random";
		const string OrderAlphabetical = "Alphabetical";

		LocalDiskConfig _config;
		TextBox _name;
		TextBox _folder;
		ToggleSwitch _recursive;
		ComboBox _order;
		ToggleSwitch _cycle;
		SettingsCard _cycleCard;
		ToggleSwitch _rescan;
		ToggleSwitch _enabled;
		NumericUpDown _weight;
		InfoBar _errorBar;

		public LocalDiskForm(LocalDiskConfig config)
			: base("Pictures from a folder", 560)
		{
			_config = config;
			StartPosition = FormStartPosition.CenterScreen;

			Body.Add(new TextBlock("Name", TextRole.Caption, TextTone.Secondary));
			_name = Body.Add(new TextBox { Text = config.Description, AccessibleName = "Name" }, 4);

			Body.Add(new TextBlock("Folder", TextRole.Caption, TextTone.Secondary), 16);
			_folder = new TextBox { Text = config.DefaultDirectory, AccessibleName = "Folder" }.WithLogicalWidth(420);
			FluentButton browse = new FluentButton("Browse", Theme.GlyphFolder);
			browse.Click += Browse_Click;
			Body.Add(new HStack(_folder, browse), 4);

			_recursive = new ToggleSwitch { Checked = config.Recursive };
			Body.Add(new SettingsCard("Include subfolders", null, Theme.GlyphFolder, _recursive), 16);

			_order = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat }.WithLogicalWidth(150);
			_order.Items.AddRange(new object[] { OrderRandom, OrderAlphabetical });
			_order.SelectedItem = config.Type == LocalDiskConfig.OrderType.Alphabetical ? OrderAlphabetical : OrderRandom;
			_order.SelectedIndexChanged += (s, e) => UpdateCycleCard();
			Body.Add(new SettingsCard("Order", null, Theme.GlyphSort, _order), 4);

			_cycle = new ToggleSwitch { Checked = config.Cycle };
			_cycleCard = Body.Add(new SettingsCard("Show every picture before repeating", null, Theme.GlyphRefresh, _cycle), 4);

			_rescan = new ToggleSwitch { Checked = config.Rescan };
			Body.Add(new SettingsCard("Look for new pictures every time", "Slower with big folders, but always up to date", Theme.GlyphSearch, _rescan), 4);

			_enabled = new ToggleSwitch { Checked = config.Enabled };
			_weight = new NumericUpDown { Minimum = 0, Maximum = 100, Value = Math.Max(0, Math.Min(100, config.Weight)) }.WithLogicalWidth(70);
			Body.Add(new SettingsCard("Use this folder", "Weight: how often it's picked compared with your other sources", Theme.GlyphPhoto, new HStack(_weight, _enabled) { Spacing = 16 }), 4);

			Body.Add(new TextBlock("Choose which screens it's used on from the Sources page.", TextRole.Caption, TextTone.Tertiary), 8);

			_errorBar = Body.Add(new InfoBar(Severity.Error, null, string.Empty), 8);
			_errorBar.Visible = false;

			PrimaryButton.Text = "Save";
			PrimaryButton.Click += Save_Click;
			UpdateCycleCard();
		}

		/// <summary>
		/// Gets the configuration as edited by the user (call after the dialog returns OK)
		/// </summary>
		public LocalDiskConfig GetConfig()
		{
			LocalDiskConfig config = new LocalDiskConfig();
			config.Enabled = _enabled.Checked;
			config.Weight = (int)_weight.Value;
			config.Description = _name.Text;
			config.DefaultDirectory = _folder.Text.Trim();
			config.Type = (string)_order.SelectedItem == OrderAlphabetical ? LocalDiskConfig.OrderType.Alphabetical : LocalDiskConfig.OrderType.Random;
			config.Recursive = _recursive.Checked;
			config.Rescan = _rescan.Checked;
			config.Cycle = _cycle.Checked;
			config.RandomStart = _config.RandomStart;
			return config;
		}

		void UpdateCycleCard()
		{
			// cycling only applies to random order
			_cycleCard.Visible = (string)_order.SelectedItem == OrderRandom;
			if (IsHandleCreated)
			{
				FitToContent();
			}
		}

		void Browse_Click(object sender, EventArgs e)
		{
			using (FolderBrowserDialog dlg = new FolderBrowserDialog { SelectedPath = _folder.Text, Description = "Choose the folder with your pictures", UseDescriptionForTitle = true })
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					_folder.Text = dlg.SelectedPath;
				}
			}
		}

		void Save_Click(object sender, EventArgs e)
		{
			if (!Directory.Exists(_folder.Text.Trim()))
			{
				_errorBar.Text = "That folder doesn't exist.";
				if (!_errorBar.Visible)
				{
					_errorBar.Visible = true;
					FitToContent();
				}

				return;
			}

			DialogResult = DialogResult.OK;
			Close();
		}
	}
}
