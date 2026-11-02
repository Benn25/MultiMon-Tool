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

namespace DMT.Modules.General
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Forms;

	using DMT.Library.DisplayHandling;
	using DMT.Library.Environment;
	using DMT.Library.GuiUtils;
	using DMT.Library.Ui;

	/// <summary>
	/// Lets the user choose how DMT numbers the monitors
	/// </summary>
	class GeneralMonitorOrderingOptionsPanel : SettingsPage
	{
		static readonly Dictionary<Monitor.EMonitorOrder, string> OrderNames = new Dictionary<Monitor.EMonitorOrder, string>
		{
			{ Monitor.EMonitorOrder.LeftRight, "Left to right, then top to bottom" },
			{ Monitor.EMonitorOrder.TopDown, "Top to bottom, then left to right" },
			{ Monitor.EMonitorOrder.DisplayName, "By the names I choose" },
			{ Monitor.EMonitorOrder.DotNet, "As DMT 2.7 and earlier" },
		};

		GeneralModule _generalModule;
		MonitorMap _map;
		SettingsCard _namesCard;

		public GeneralMonitorOrderingOptionsPanel(GeneralModule generalModule, DisplayDevices displayDevices)
		{
			_generalModule = generalModule;

			AddText("The numbers DMT gives your monitors. They're used for moving windows between screens, numpad zones, " +
				"the wallpaper and the Monitors page.");

			string selected;
			if (!OrderNames.TryGetValue(_generalModule.MonitorOrder, out selected))
			{
				selected = OrderNames[Monitor.EMonitorOrder.LeftRight];
			}

			ComboBox order = MakeCombo(OrderNames.Values.Cast<object>(), selected, item =>
			{
				Monitor.EMonitorOrder newOrder = OrderNames.First(pair => pair.Value == (string)item).Key;
				if (newOrder != _generalModule.MonitorOrder)
				{
					_generalModule.MonitorOrder = newOrder;
					UpdateNamesCard();
					_map.Refresh();
				}
			}, 280);
			Add(new SettingsCard("Number the monitors", null, Theme.GlyphSort, order), 16);

			FluentButton edit = new FluentButton("Edit order", Theme.GlyphEdit);
			edit.Click += EditNames_Click;
			_namesCard = AddCard("Named monitor order", string.Empty, Theme.GlyphRename, edit);

			AddHeader("Result");
			_map = AddCardContent(new MonitorMap());

			UpdateNamesCard();
		}

		T AddCardContent<T>(T control) where T : Control
		{
			SettingsCard card = AddCard("How DMT numbers your monitors", null, null, null);
			card.Content = control;
			return control;
		}

		/// <summary>
		/// Redraws the monitors (called by the module when the monitors change)
		/// </summary>
		public void ShowCurrentLayout(DisplayDevices displayDevices)
		{
			if (!IsDisposed)
			{
				_map.Refresh();
			}
		}

		void UpdateNamesCard()
		{
			_namesCard.Visible = _generalModule.MonitorOrder == Monitor.EMonitorOrder.DisplayName;
			string names = _generalModule.MonitorNameOrderToString();
			_namesCard.Description = string.IsNullOrEmpty(names) ? "No order chosen yet" : names;
		}

		void EditNames_Click(object sender, System.EventArgs e)
		{
			List<string> allNames = Monitor.MonitorNames.GetAllNames();
			allNames.Sort();
			List<string> selectedNames = _generalModule.MonitorNameOrdering.ToList();
			EditNameListForm dlg = new EditNameListForm("Edit Monitor Order by Name", "Selected Monitor Order", allNames, selectedNames);
			if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
			{
				_generalModule.MonitorNameOrdering = dlg.GetSelectedNames().ToArray();
				UpdateNamesCard();
				_map.Refresh();
			}
		}
	}
}
