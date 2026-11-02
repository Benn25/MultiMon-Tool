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

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DMT.Library.Environment;
using DMT.Library.GuiUtils;
using DMT.Library.Ui;
using DMT.Library.Utils;

namespace DMT.Modules.Cursor
{
	/// <summary>
	/// Lets the user pick the monitors that make up the primary working area
	/// </summary>
	class CursorPrimaryOptionsPanel : SettingsPage
	{
		const string ByClicking = "Clicking them below";
		const string ByName = "Their names";

		CursorModule _cursorModule;
		MonitorMap _map;
		ComboBox _mode;
		SettingsCard _namesCard;
		SettingsCard _mapCard;

		public CursorPrimaryOptionsPanel(CursorModule cursorModule)
		{
			_cursorModule = cursorModule;

			AddText("The primary working area is the screen (or group of screens) where you do most of your work. " +
				"Sticky and locked edges treat it as one big screen, and the cursor can be allowed to return to it freely.");

			_mode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat }.WithLogicalWidth(200);
			_mode.Items.AddRange(new object[] { ByClicking, ByName });
			_mode.SelectedItem = _cursorModule.PrimaryGroupType == CursorModule.EPrimaryGroupType.Names ? ByName : ByClicking;
			_mode.SelectedIndexChanged += Mode_SelectedIndexChanged;
			Add(new SettingsCard("Choose the monitors by", "Using names keeps the same monitors when Windows renumbers them", Theme.GlyphSettings, _mode), 16);

			FluentButton editNames = new FluentButton("Edit names", Theme.GlyphEdit);
			editNames.Click += EditNames_Click;
			_namesCard = AddCard("Named monitors", string.Empty, Theme.GlyphRename, editNames);

			_map = new MonitorMap();
			_map.MonitorClicked += Map_MonitorClicked;
			_mapCard = AddCard("Primary working area", null, null, null);
			_mapCard.Content = _map;

			AddText("If no monitors are selected, just the main display is used.", TextTone.Tertiary);

			ShowMonitorPreview();
		}

		/// <summary>
		/// Redraws the monitors (called by the module when the monitors change)
		/// </summary>
		public void ShowMonitorPreview()
		{
			if (IsDisposed)
			{
				return;
			}

			bool byName = _cursorModule.PrimaryGroupType == CursorModule.EPrimaryGroupType.Names;
			_namesCard.Visible = byName;
			_namesCard.Description = string.IsNullOrEmpty(_cursorModule.PrimaryGroupNames) ? "None chosen yet" : _cursorModule.PrimaryGroupNames;
			_map.Clickable = !byName;
			_mapCard.Description = byName ? "Highlighted monitors match the chosen names" : "Click a monitor to add it to, or remove it from, the primary working area";
			_map.Selected = new List<int>(_cursorModule.PrimaryGroup);
			_map.Refresh();
		}

		void Mode_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if ((string)_mode.SelectedItem == ByName)
			{
				_cursorModule.PrimaryGroupType = CursorModule.EPrimaryGroupType.Names;

				// update the primary monitors using the named primary monitors
				_cursorModule.PrimaryGroup = _cursorModule.PrimaryGroupFromNames(_cursorModule.PrimaryGroupNames);
			}
			else
			{
				// leave the monitor selection alone
				_cursorModule.PrimaryGroupType = CursorModule.EPrimaryGroupType.Nums;
			}

			ShowMonitorPreview();
		}

		void Map_MonitorClicked(object sender, int monitorIndex)
		{
			List<int> primaryGroup = _cursorModule.PrimaryGroup;
			if (primaryGroup.Contains(monitorIndex))
			{
				primaryGroup.Remove(monitorIndex);
			}
			else
			{
				primaryGroup.Add(monitorIndex);
			}

			_cursorModule.PrimaryGroup = primaryGroup;
			ShowMonitorPreview();
		}

		void EditNames_Click(object sender, System.EventArgs e)
		{
			List<string> allNames = Monitor.MonitorNames.GetAllNames();
			allNames.Sort();
			List<string> selectedNames = MonitorNamesParser.StringToList(_cursorModule.PrimaryGroupNames).ToList();
			EditNameListForm dlg = new EditNameListForm("Change Primary Monitors by Name", "Selected Primary Monitors", allNames, selectedNames);
			if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
			{
				_cursorModule.PrimaryGroupNames = MonitorNamesParser.ListToString(dlg.GetSelectedNames());
				_cursorModule.PrimaryGroup = _cursorModule.PrimaryGroupFromNames(_cursorModule.PrimaryGroupNames);
				ShowMonitorPreview();
			}
		}
	}
}
