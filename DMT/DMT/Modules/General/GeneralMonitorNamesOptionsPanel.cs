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

	using DMT.Library.DisplayHandling;
	using DMT.Library.Environment;
	using DMT.Library.Ui;

	/// <summary>
	/// Lets the user give each physical monitor their own name
	/// </summary>
	class GeneralMonitorNamesOptionsPanel : SettingsPage
	{
		GeneralModule _generalModule;
		MonitorMap _map;
		List<SettingsCard> _cards = new List<SettingsCard>();

		public GeneralMonitorNamesOptionsPanel(GeneralModule generalModule, DisplayDevices displayDevices)
		{
			_generalModule = generalModule;

			AddText("Optionally give each monitor a name of your own. Names stay with the monitor even if Windows renumbers your screens, " +
				"and can be used to choose the monitor order and the primary working area.");
			_map = Add(new MonitorMap(), 16);

			ShowCurrentLayout(displayDevices);
		}

		/// <summary>
		/// Refreshes the page (also called by the module when the monitors or names change)
		/// </summary>
		public void ShowCurrentLayout(DisplayDevices displayDevices)
		{
			if (IsDisposed)
			{
				return;
			}

			SuspendLayout();
			foreach (SettingsCard card in _cards)
			{
				Controls.Remove(card);
				card.Dispose();
			}

			_cards.Clear();
			_map.Refresh();

			IList<DisplayDevice> devices = displayDevices.Items;
			for (int i = 0; i < devices.Count; i++)
			{
				DisplayDevice device = devices[i];
				if (!device.IsActive)
				{
					continue;
				}

				int deviceIndex = i;
				int screenNumber = Monitor.AllMonitors.FindIndex(m => m.Bounds == device.Bounds) + 1;
				string name = _generalModule.GetMonitorName(device.GetMonitorId());
				string brand = string.IsNullOrEmpty(device.FriendlyName) ? device.SourceName : device.FriendlyName;

				FluentButton rename = new FluentButton("Rename", Theme.GlyphRename);
				rename.Enabled = !string.IsNullOrEmpty(device.GetMonitorId());
				rename.Click += (s, e) => _generalModule.ShowEditNameDlg(deviceIndex, FindForm());

				string title = string.IsNullOrEmpty(name) ? "No name yet" : name;
				string description = string.Format("Screen {0} · {1} · {2} × {3}", screenNumber, brand, device.Bounds.Width, device.Bounds.Height);
				if (!string.IsNullOrEmpty(device.GPU))
				{
					description += " · " + device.GPU;
				}

				SettingsCard card = new SettingsCard(title, description, Theme.GlyphMonitor, rename);
				Add(card, _cards.Count == 0 ? 16 : 4);
				_cards.Add(card);
			}

			ResumeLayout();
		}
	}
}
