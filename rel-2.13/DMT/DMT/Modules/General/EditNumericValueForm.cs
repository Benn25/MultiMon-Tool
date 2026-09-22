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
// along with this program.  If not, see <http://www.gnu.org/licenses/>
#endregion

using DMT.Library.DisplayHandling;
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
	/// <summary>
	/// Form to change the numeric value (within a given range) for one of the monitor properties
	/// </summary>
	public partial class EditNumericValueForm : Form
	{
		// this is what we will need to call to reflect the change in the numeric value
		public delegate void ReflectChange(int monitorIndex, int numericValue);

		int _monitorIndex;
		int _originalValue;
		NumericRange _numericRange;
		ReflectChange _reflectChange;

		public EditNumericValueForm(string title, int monitorIndex, NumericRange numericRange, ReflectChange reflectChange)
		{
			_monitorIndex = monitorIndex;
			_numericRange = numericRange;
			_reflectChange = reflectChange;

			// remember original value, so we can restore it if the user cancels
			_originalValue = _numericRange.CurValue;

			InitializeComponent();

			this.Text = title;

			trackBarBrightness.Minimum = _numericRange.MinValue;
			trackBarBrightness.Maximum = _numericRange.MaxValue;
			trackBarBrightness.Value = _numericRange.CurValue;

			labelMinBrightness.Text = _numericRange.MinValue.ToString();
			labelMaxBrightness.Text = _numericRange.MaxValue.ToString();
			ShowCurBrightnessValue();
		}

		private void EditNumericValueForm_Load(object sender, EventArgs e)
		{
			// center the dlg on the cursor
			Point pt = System.Windows.Forms.Cursor.Position;
			Left = pt.X - Width / 2;
			Top = pt.Y - Height / 2;
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			// restore value to original value
			_reflectChange(_monitorIndex, _originalValue);

			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void trackBarBrightness_Scroll(object sender, EventArgs e)
		{
			_reflectChange(_monitorIndex, trackBarBrightness.Value);
			ShowCurBrightnessValue();
		}

		void ShowCurBrightnessValue()
		{
			labelCurBrightness.Text = trackBarBrightness.Value.ToString();
		}
	}
}
