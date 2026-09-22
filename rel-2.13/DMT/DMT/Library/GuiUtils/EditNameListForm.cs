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
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DMT.Library.GuiUtils
{
	public partial class EditNameListForm : Form
	{
		List<string> _allNames;

		public EditNameListForm(string caption, string listText, List<string> allNames, List<string> selectedNames)
		{
			_allNames = allNames;

			InitializeComponent();

			this.Text = caption;
			labelSelected.Text = listText;

			List<string> availableNames = GetAvailableNames(allNames, selectedNames);

			FillListBox(listBoxAvailable, availableNames);
			FillListBox(listBoxSelected, selectedNames);

			UpdateButtonStates();
		}

		public List<string> GetSelectedNames()
		{
			List<string> selectedNames = new List<string>();

			foreach (object o in listBoxSelected.Items)
			{
				selectedNames.Add((string)o);
			}

			return selectedNames;
		}

		/// <summary>
		/// Gets the names that are available to be added to selected.
		/// This is the names in allNames that are not currently in selectedNames
		/// </summary>
		/// <param name="allNames"></param>
		/// <param name="selectedNames"></param>
		/// <returns></returns>
		List<string> GetAvailableNames(List<string> allNames, List<string> selectedNames)
		{
			List<string> availableNames = new List<string>();

			foreach (string name in allNames)
			{
				if (!selectedNames.Contains(name))
				{
					availableNames.Add(name);
				}
			}

			return availableNames;
		}

		void FillListBox(ListBox listBox, List<string> names)
		{
			listBox.BeginUpdate();
			listBox.Items.Clear();	// in case we need to re-populate - shouldn't have to?
			foreach (string name in names)
			{
				listBox.Items.Add(name);
			}
			if (listBox.Items.Count > 0)
			{
				listBox.SelectedIndex = 0;
			}
			listBox.EndUpdate();
		}

		private void listBoxAvailable_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateButtonStates();
		}

		private void listBoxSelected_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateButtonStates();
		}

		private void buttonAdd_Click(object sender, EventArgs e)
		{
			AddName();
		}

		private void listBoxAvailable_DoubleClick(object sender, EventArgs e)
		{
			AddName();
		}

		void AddName()
		{
			int index = listBoxAvailable.SelectedIndex;
			if (index >= 0)
			{
				listBoxSelected.Items.Add(listBoxAvailable.Items[index]);
				listBoxAvailable.Items.RemoveAt(index);

				// make sure something sensible is selected in each list
				if (index < listBoxAvailable.Items.Count)
				{
					listBoxAvailable.SelectedIndex = index;
				}
				else if (listBoxAvailable.Items.Count > 0)
				{
					listBoxAvailable.SelectedIndex = listBoxAvailable.Items.Count - 1;
				}

				listBoxSelected.SelectedIndex = listBoxSelected.Items.Count - 1;

				UpdateButtonStates();
			}
		}

		private void buttonRemove_Click(object sender, EventArgs e)
		{
			RemoveName();
		}

		private void listBoxSelected_DoubleClick(object sender, EventArgs e)
		{
			RemoveName();
		}

		void RemoveName()
		{
			int index = listBoxSelected.SelectedIndex;
			if (index >= 0)
			{
				// we only add the name back into the available list
				// if it is in the current list of all names
				// (This could be an old monitor name that the user has now changed
				//  or removed from the computer)
				string name = (string)listBoxSelected.Items[index];
				int availableIndex = -1;
				if (_allNames.Contains(name))
				{
					availableIndex = listBoxAvailable.Items.Add(name);
					listBoxAvailable.SelectedIndex = availableIndex;
				}
				listBoxSelected.Items.RemoveAt(index);

				// make sure something sensible is selected in each list
				if (index < listBoxSelected.Items.Count)
				{
					listBoxSelected.SelectedIndex = index;
				}
				else if (listBoxSelected.Items.Count > 0)
				{
					listBoxSelected.SelectedIndex = listBoxSelected.Items.Count - 1;
				}

				UpdateButtonStates();
			}
		}

		private void buttonUp_Click(object sender, EventArgs e)
		{
			int index = listBoxSelected.SelectedIndex;
			if (index > 0)
			{
				listBoxSelected.BeginUpdate();
				string name = (string)listBoxSelected.Items[index];
				listBoxSelected.Items.RemoveAt(index);
				listBoxSelected.Items.Insert(index - 1, name);
				listBoxSelected.SelectedIndex = index - 1;
				listBoxSelected.EndUpdate();
			}
		}

		private void buttonDown_Click(object sender, EventArgs e)
		{
			int index = listBoxSelected.SelectedIndex;
			if (index >= 0 && index < listBoxSelected.Items.Count - 1)
			{
				listBoxSelected.BeginUpdate();
				string name = (string)listBoxSelected.Items[index];
				listBoxSelected.Items.RemoveAt(index);
				listBoxSelected.Items.Insert(index + 1, name);
				listBoxSelected.SelectedIndex = index + 1;
				listBoxSelected.EndUpdate();
			}
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		void UpdateButtonStates()
		{
			buttonAdd.Enabled = (listBoxAvailable.SelectedIndex >= 0);
			buttonRemove.Enabled = (listBoxSelected.SelectedIndex >= 0);

			buttonUp.Enabled = (listBoxSelected.SelectedIndex > 0);
			buttonDown.Enabled = (listBoxSelected.SelectedIndex >= 0 && listBoxSelected.SelectedIndex < listBoxSelected.Items.Count - 1);
		}
	}
}
