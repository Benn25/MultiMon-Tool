#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2026  Gerald Evans
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
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DMT.Library.DisplayHandling;
using DMT.Library.GuiUtils;

namespace DMT.Modules.General
{
	partial class GeneralMonitorNamesOptionsPanel : UserControl
	{
		GeneralModule _generalModule;
		int _selectedMonitorIndex;

		public GeneralMonitorNamesOptionsPanel(GeneralModule generalModule, DisplayDevices displayDevices)
		{
			_generalModule = generalModule;
			_selectedMonitorIndex = 0;

			InitializeComponent();

			ShowCurrentLayout(displayDevices);
		}

		public void ShowCurrentLayout(DisplayDevices displayDevices)
		{
			ShowMonitorLayout(displayDevices);
		}

		void ShowMonitorLayout(DisplayDevices displayDevices)
		{
			//DisplayDevices displayDevices = _generalModule.GetDisplayDevices();

			List<int> selectedMonitors = new List<int>();
			selectedMonitors.Add(_selectedMonitorIndex);

			MonitorPreviewHelper.ShowLayout(picPreview, displayDevices.Items, selectedMonitors);

			ShowCurrentMonitorInfo();
		}

		void ShowCurrentMonitorInfo()
		{
			DisplayDevice monitorProperties = GetSelectedDisplayDevice();

			string monitorId = monitorProperties.GetMonitorId();
			textBoxName.Text = _generalModule.GetMonitorName(monitorId);

			labelBrand.Text = monitorProperties.FriendlyName; 
			labelGpu.Text = monitorProperties.GPU;
			string size = string.Format("{0} * {1}", monitorProperties.Bounds.Width, monitorProperties.Bounds.Height);
			labelSize.Text = size;
		}

		DisplayDevice GetSelectedDisplayDevice()
		{
			// TODO

			DisplayDevices displayDevices = _generalModule.GetDisplayDevices();
			int index = _selectedMonitorIndex;
			if (index < 0 || index >= displayDevices.Count())
			{
				index = 0;
			}

			return displayDevices.Items[index];
		}

		private void picPreview_MouseClick(object sender, MouseEventArgs e)
		{
			// check if the area clicked belongs to one of the monitors
			int monitorIndex = MonitorPreviewHelper.PosnToMonitor(picPreview, e.X, e.Y);
			if (monitorIndex >= 0)
			{
				_selectedMonitorIndex = monitorIndex;

				DisplayDevices displayDevices = _generalModule.GetDisplayDevices();
				ShowMonitorLayout(displayDevices);
			}
		}

		private void buttonChangeName_Click(object sender, EventArgs e)
		{
			int index = _selectedMonitorIndex;

			if (index >= 0)
			{
				_generalModule.ShowEditNameDlg(index, this);
			}
		}
	}
}
