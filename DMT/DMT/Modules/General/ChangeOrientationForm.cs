#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2025  Gerald Evans
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

/*
 * Currently the orientation is only changed on 'OK'
 * Could:
 * 1) relect the change as soon as it is made
 * 2) have a chackbox to indicate we want the change made immediatedly 
 * 3) have an 'Apply' button
 */

namespace DMT.Modules.General
{
	public partial class ChangeOrientationForm : Form
	{
		public int NewOrientation { get; set; }

		int _monitorIndex;
		int _originalOrientationDegrees;

		public ChangeOrientationForm(int monitorIndex, int orientationDegrees)
		{
			NewOrientation = orientationDegrees;

			_monitorIndex = monitorIndex;
			_originalOrientationDegrees = orientationDegrees;

			InitializeComponent();

			SelectRadio(orientationDegrees);
		}

		private void ChangeOrientationForm_Load(object sender, EventArgs e)
		{
			// center the dlg on the cursor
			Point pt = System.Windows.Forms.Cursor.Position;
			Left = pt.X - Width / 2;
			Top = pt.Y - Height / 2;
		}

		void SelectRadio(int orientationDegrees)
		{
			switch (orientationDegrees)
			{
				case 90:
					radioButtonOrientation90.Checked = true;
					break;

				case 180:
					radioButtonOrientation180.Checked = true;
					break;

				case 270:
					radioButtonOrientation270.Checked = true;
					break;

				case 0:
				default:
					radioButtonOrientation0.Checked = true;
					break;
			}
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			SetNewOrientation();
			DialogResult = DialogResult.OK;
			Close();
		}

		void SetNewOrientation()
		{
			if (radioButtonOrientation90.Checked)
			{
				NewOrientation = 90;
			}
			else if (radioButtonOrientation180.Checked)
			{
				NewOrientation = 180;
			}
			else if (radioButtonOrientation270.Checked)
			{
				NewOrientation = 270;
			}
			else
			{
				NewOrientation = 0;
			}
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		// treat the images as part of the radio button

		private void pictureBoxOrientation0_Click(object sender, EventArgs e)
		{
			radioButtonOrientation0.Checked = true;
		}

		private void pictureBoxOrientation90_Click(object sender, EventArgs e)
		{
			radioButtonOrientation90.Checked = true;
		}

		private void pictureBoxOrientation180_Click(object sender, EventArgs e)
		{
			radioButtonOrientation180.Checked = true;
		}

		private void pictureBoxOrientation270_Click(object sender, EventArgs e)
		{
			radioButtonOrientation270.Checked = true;
		}
	}
}
