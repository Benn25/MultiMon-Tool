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

namespace DMT.Modules.General
{
	public partial class EditMonitorNameForm : Form
	{
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string MonitorName {
			get
			{
				return textBoxName.Text;
			}
			set
			{
				textBoxName.Text = value;
			}
		}

		public EditMonitorNameForm()
		{
			InitializeComponent();
		}

		private void textBoxName_TextAlignChanged(object sender, EventArgs e)
		{
			// TODO?
			// we could check to make sure that this name is unique amongst all physical monitors
			// and display a warning it it isn't.
			//
			// But the user could have replaced an old monitor with a new one and want to reuse the same name.
			// As we would have the names for both monitors, we could end up putting a warning when one isn't
			// really needed.

			// so don't do this check (for now!)
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			// we don't do any furter validation as we have already told the user
			// if the name is not unique
			DialogResult = DialogResult.OK;
			Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
