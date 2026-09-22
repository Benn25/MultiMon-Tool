#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2023  Gerald Evans
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

namespace DMT.Modules.Cursor
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Data;
	using System.Drawing;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows.Forms;

	using DMT.Library.HotKeys;
	using DMT.Resources;
	using DMT.Library.GuiUtils;
	using DMT.Library.DisplayHandling;
	using DMT.Library.Utils;
	using DMT.Library.Environment;

	/// <summary>
	/// General options panel for the cursor module
	/// </summary>
	partial class CursorPrimaryOptionsPanel : UserControl
	{
		CursorModule _cursorModule;

		/// <summary>
		/// Initialises a new instance of the <see cref="CursorGeneralOptionsPanel" /> class.
		/// </summary>
		/// <param name="cursorModule">The cursor module</param>
		public CursorPrimaryOptionsPanel(CursorModule cursorModule)
		{
			_cursorModule = cursorModule;

			InitializeComponent();

			InitOptions();
		}

		public void ShowMonitorPreview()
		{
			//IList<DisplayDevice> displayDevices = null;
			IList<DisplayDevice> displayDevices = new DisplayDevices().Items;
			MonitorPreviewHelper.ShowLayout(picPreview, displayDevices, _cursorModule.PrimaryGroup);
		}

		void InitOptions()
		{
			UpdateRadioButtons();
			UpdateEditNamesButton();

			ShowPrimaryNames();

			ShowMonitorPreview();
		}

		void UpdateRadioButtons()
		{
			switch (_cursorModule.PrimaryGroupType)
			{
				case CursorModule.EPrimaryGroupType.Names:
					radioButtonNames.Checked = true;
					break;

				case CursorModule.EPrimaryGroupType.Nums: // default case
				default:
					radioButtonClick.Checked = true;
					break;
			}
		}

		void UpdateEditNamesButton()
		{
			bool enable = (_cursorModule.PrimaryGroupType == CursorModule.EPrimaryGroupType.Names);
			buttonNames.Enabled = enable;
		}

		//private void radioButtonPrimaryScreen_CheckedChanged(object sender, EventArgs e)
		//{
		//	ShowMonitorPreview();
		//}

		//private void radioButtonPrimaryArea_CheckedChanged(object sender, EventArgs e)
		//{
		//	ShowMonitorPreview();
		//}

		private void picPreview_MouseClick(object sender, MouseEventArgs e)
		{
			// don't update if we are specifying the primary area by names 
			if (_cursorModule.PrimaryGroupType != CursorModule.EPrimaryGroupType.Names)
			{
				// check if the area clicked belongs to one of the screens
				int monitorIndex = MonitorPreviewHelper.PosnToMonitor(picPreview, e.X, e.Y);
				if (monitorIndex >= 0)
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
			}
		}

		private void radioButtonClick_CheckedChanged(object sender, EventArgs e)
		{
			_cursorModule.PrimaryGroupType = CursorModule.EPrimaryGroupType.Nums;

			// leave monitor selection alone
			// could try and remember monitors the user manually selected the last
			// time this radio button was used?

			UpdateEditNamesButton();
			ShowMonitorPreview();
		}

		private void radioButtonNames_CheckedChanged(object sender, EventArgs e)
		{
			_cursorModule.PrimaryGroupType = CursorModule.EPrimaryGroupType.Names;

			// update the primary monitors using the named primary monitors
			string primaryNames = _cursorModule.PrimaryGroupNames;
			List<int> primaryGroup = _cursorModule.PrimaryGroupFromNames(primaryNames);
			_cursorModule.PrimaryGroup = primaryGroup;

			UpdateEditNamesButton();
			ShowMonitorPreview();
		}

		private void buttonNames_Click(object sender, EventArgs e)
		{
			string caption = "Change Primary Monitors by Name";
			string listText = "Selected Primary Monitors";
			List<string> allNames;
			List<string> selectedNames;

			allNames = Monitor.MonitorNames.GetAllNames();
			allNames.Sort();

			selectedNames = MonitorNamesParser.StringToList(_cursorModule.PrimaryGroupNames).ToList();

			EditNameListForm dlg = new EditNameListForm(caption, listText, allNames, selectedNames);
			if (dlg.ShowDialog() == DialogResult.OK)
			{
				selectedNames = dlg.GetSelectedNames();

				_cursorModule.PrimaryGroupNames = MonitorNamesParser.ListToString(selectedNames);

				// this will always be true at this point
				if (_cursorModule.PrimaryGroupType == CursorModule.EPrimaryGroupType.Names)
				{
					SetPrimaryFromNames(_cursorModule.PrimaryGroupNames);

					ShowMonitorPreview();
				}
			}
		}

		void SetPrimaryFromNames(string primaryNames)
		{
			textBoxNames.Text = primaryNames;

			_cursorModule.PrimaryGroupNames = primaryNames;

			List<int> primaryGroup = _cursorModule.PrimaryGroupFromNames(primaryNames);

			_cursorModule.PrimaryGroup = primaryGroup;
		}

		void ShowPrimaryNames()
		{
			textBoxNames.Text = _cursorModule.PrimaryGroupNames;
		}

		//void UpdateArea()
		//{
		//	ShowMonitorPreview();
		//}

	}
}
