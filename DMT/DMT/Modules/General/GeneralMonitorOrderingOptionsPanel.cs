#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2020-2026  Gerald Evans
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
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Drawing;
	using System.Data;
	using System.Linq;
	using System.Text;
	using System.Windows.Forms;
	using DMT.Library.Environment;
	using DMT.Library.Wallpaper;
	using DMT.Library.GuiUtils;
	using DMT.Library.DisplayHandling;
	using DMT.Library.Utils;

	partial class GeneralMonitorOrderingOptionsPanel : UserControl
	{
		GeneralModule _generalModule;

		bool _ignoreOrderingEvent = false;

		public GeneralMonitorOrderingOptionsPanel(GeneralModule generalModule, DisplayDevices displayDevices)
		{
			_generalModule = generalModule;

			InitializeComponent();

			UpdateOrderingButtons();
			UpdateEditNamesButton();

			ShowCurrentLayout(displayDevices);

			ShowNameOrder();
		}

		public void ShowCurrentLayout(DisplayDevices displayDevices)
		{
			ShowMonitorLayout(displayDevices);
		}


		void UpdateOrderingButtons()
		{
			// we don't want setting the radio buttons to fire the event
			// as that could result in the wallpaper changing
			_ignoreOrderingEvent = true;

			try
			{
				switch (_generalModule.MonitorOrder) //(Monitor.MonitorOrder)
				{
					case Monitor.EMonitorOrder.DotNet:
						radioButtonDotNet.Checked = true;
						break;

					case Monitor.EMonitorOrder.TopDown:
						radioButtonTopDown.Checked = true;
						break;

					case Monitor.EMonitorOrder.DisplayName:
						radioButtonNames.Checked = true;
						break;

					case Monitor.EMonitorOrder.LeftRight: // default case
					default:
						radioButtonLeftRight.Checked = true;
						break;
				}
			}
			finally
			{
				_ignoreOrderingEvent = false;
			}
		}

		void ShowMonitorLayout(DisplayDevices displayDevices)
		{
			//DisplayDevices displayDevices = _generalModule.GetDisplayDevices();
			MonitorPreviewHelper.ShowLayout(picPreview, displayDevices.Items);
		}

		private void radioButtonLeftRight_CheckedChanged(object sender, EventArgs e)
		{
			UpdateOrdering(sender);
		}

		private void radioButtonTopDown_CheckedChanged(object sender, EventArgs e)
		{
			UpdateOrdering(sender);
		}

		private void radioButtonDotNet_CheckedChanged(object sender, EventArgs e)
		{
			UpdateOrdering(sender);
		}

		private void radioButtonNames_CheckedChanged(object sender, EventArgs e)
		{
			UpdateOrdering(sender);
		}

		private void buttonNames_Click(object sender, EventArgs e)
		{
			string caption = "Edit Monitor Order by Name";
			string listText = "Selected Monitor Order";
			List<string> allNames;
			List<string> selectedNames;

			allNames = Monitor.MonitorNames.GetAllNames();
			allNames.Sort();

			selectedNames = _generalModule.MonitorNameOrdering.ToList();

			EditNameListForm dlg = new EditNameListForm(caption, listText, allNames, selectedNames);
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				selectedNames = dlg.GetSelectedNames();

				_generalModule.MonitorNameOrdering = selectedNames.ToArray();

				ShowNameOrder();
			}
		}

		void ShowNameOrder()
		{
			//List<string> nameOrder = _generalModule.MonitorNameOrdering.ToList();
			//textBoxNames.Text = ListParser.ListToString(nameOrder, GeneralModule.MonitorNameSeparatorChars, ", ");
			textBoxNames.Text = _generalModule.MonitorNameOrderToString();
		}

		void UpdateEditNamesButton()
		{
			bool enable = (_generalModule.MonitorOrder == Monitor.EMonitorOrder.DisplayName);
			buttonNames.Enabled = enable;
		}

		void UpdateOrdering(object sender)
		{
			if (!_ignoreOrderingEvent)
			{
				RadioButton rb = sender as RadioButton;
				if (rb != null)
				{
					if (rb.Checked)
					{
						UpdateOrdering();
					}
				}
			}
		}

		// call to update monitor ordering
		void UpdateOrdering()
		{
			if (radioButtonDotNet.Checked)
			{
				_generalModule.MonitorOrder = Monitor.EMonitorOrder.DotNet;
			}
			else if (radioButtonTopDown.Checked)
			{
				_generalModule.MonitorOrder = Monitor.EMonitorOrder.TopDown;
			}
			else if (radioButtonNames.Checked)
			{
				_generalModule.MonitorOrder = Monitor.EMonitorOrder.DisplayName;
			}
			else //if (radioButtonLeftRight.Checked)
			{
				// anything else
				_generalModule.MonitorOrder = Monitor.EMonitorOrder.LeftRight;
			}

			UpdateEditNamesButton();
		}
	}
}
