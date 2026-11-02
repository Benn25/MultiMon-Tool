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

using System;
using System.ComponentModel;
using System.Windows.Forms;
using DMT.Library.Ui;
using DMT.Resources;

namespace DMT.Modules.Launcher
{
	/// <summary>
	/// The list of magic words, with buttons to add, change and delete them
	/// </summary>
	class LauncherMagicWordsOptionsPanel : SettingsPage
	{
		LauncherModule _launcherModule;
		BindingSource _bindingSource;
		DataGridView _grid;
		FluentButton _editButton;
		FluentButton _deleteButton;
		bool _formHooked;

		public LauncherMagicWordsOptionsPanel(LauncherModule launcherModule)
		{
			_launcherModule = launcherModule;

			AddText("Type a magic word into the launcher to start a program, open a file or web page, or run a DMT command. " +
				"A magic word can also put the program's window in a chosen position.");

			FluentButton addButton = new FluentButton("Add", Theme.GlyphAdd, ButtonKind.Accent);
			addButton.Click += AddButton_Click;
			_editButton = new FluentButton("Edit", Theme.GlyphEdit);
			_editButton.Click += (s, e) => EditSelected();
			_deleteButton = new FluentButton("Delete", Theme.GlyphDelete);
			_deleteButton.Click += DeleteButton_Click;
			FluentButton resetButton = new FluentButton("Reset usage counts", Theme.GlyphRefresh, ButtonKind.Subtle);
			resetButton.Click += ResetButton_Click;
			Add(new HStack(addButton, _editButton, _deleteButton, resetButton), 16);

			_grid = new DataGridView
			{
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				ReadOnly = true,
				SelectionMode = DataGridViewSelectionMode.FullRowSelect,
				AutoGenerateColumns = false,
				AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
			};
			AddColumn("Alias", "Magic word", 18);
			AddColumn("Filename", "Opens", 34);
			AddColumn("Parameters", "Parameters", 14);
			AddColumn("Comment", "Comment", 22);
			AddColumn("UseCount", "Uses", 6);
			AddColumn("LastUsed", "Last used", 12);
			GridStyle.Apply(_grid);
			_grid.CellDoubleClick += Grid_CellDoubleClick;
			_grid.KeyPress += Grid_KeyPress;
			_grid.KeyDown += Grid_KeyDown;
			_grid.SelectionChanged += (s, e) => UpdateButtons();

			FillControl = Add(new CardFrame(_grid), 12);
			InitGrid();
		}

		public override void ApplyTheme()
		{
			base.ApplyTheme();
			GridStyle.Apply(_grid);
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			HookForm();
		}

		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			HookForm();
		}

		void HookForm()
		{
			// save any changes when the options window closes
			Form form = FindForm();
			if (form != null && !_formHooked)
			{
				_formHooked = true;
				form.FormClosing += (s, e) => _launcherModule.SaveMagicWords();
			}
		}

		void AddColumn(string property, string header, float weight)
		{
			DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn
			{
				DataPropertyName = property,
				HeaderText = header,
				ReadOnly = true,
				FillWeight = weight,
				SortMode = DataGridViewColumnSortMode.NotSortable,
			};
			_grid.Columns.Add(column);
		}

		void InitGrid()
		{
			PropertyDescriptor property = TypeDescriptor.GetProperties(typeof(MagicWord)).Find("Alias", false);
			_launcherModule.MagicWords.Sort(property, ListSortDirection.Ascending);

			_bindingSource = new BindingSource();
			_bindingSource.DataSource = _launcherModule.MagicWords.DataSource;
			_grid.DataSource = _bindingSource;
			UpdateButtons();
		}

		void UpdateButtons()
		{
			_editButton.Enabled = _grid.SelectedRows.Count == 1;
			_deleteButton.Enabled = _grid.SelectedRows.Count > 0;
		}

		void AddButton_Click(object sender, EventArgs e)
		{
			MagicWord newMagicWord = new MagicWord();
			MagicWordForm dlg = new MagicWordForm(_launcherModule, newMagicWord);
			if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
			{
				_launcherModule.MagicWords.Insert(newMagicWord);
			}
		}

		void EditSelected()
		{
			if (_grid.SelectedRows.Count == 1)
			{
				EditMagicWord(_grid.SelectedRows[0].Index);
			}
		}

		void EditMagicWord(int rowIndex)
		{
			MagicWord editMagicWord = _launcherModule.MagicWords[rowIndex];
			MagicWordForm dlg = new MagicWordForm(_launcherModule, editMagicWord);
			if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
			{
				_launcherModule.SaveMagicWords();
			}
		}

		void DeleteButton_Click(object sender, EventArgs e)
		{
			DataGridViewSelectedRowCollection selectedRows = _grid.SelectedRows;
			if (selectedRows.Count == 0)
			{
				return;
			}

			string msg;
			if (selectedRows.Count == 1)
			{
				MagicWord curMagicWord = selectedRows[0].DataBoundItem as MagicWord;
				msg = string.Format(LauncherStrings.ConfirmDel1MW, curMagicWord != null ? curMagicWord.Alias : "?");
			}
			else
			{
				msg = string.Format(LauncherStrings.ConfirmDel2MW, selectedRows.Count);
			}

			if (MessageBox.Show(FindForm(), msg, CommonStrings.MyTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
			{
				foreach (DataGridViewRow row in selectedRows)
				{
					_grid.Rows.Remove(row);
				}

				_grid.Refresh();
				_launcherModule.SaveMagicWords();
			}
		}

		void ResetButton_Click(object sender, EventArgs e)
		{
			foreach (MagicWord mw in _launcherModule.MagicWords)
			{
				mw.UseCount = 0;
			}

			_grid.Refresh();
		}

		void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex >= 0)
			{
				EditMagicWord(e.RowIndex);
			}
		}

		void Grid_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				EditSelected();
				e.Handled = true;
			}
			else if (e.KeyCode == Keys.Delete)
			{
				DeleteButton_Click(sender, e);
				e.Handled = true;
			}
		}

		/// <summary>
		/// Typing a letter jumps to the next magic word starting with it
		/// </summary>
		void Grid_KeyPress(object sender, KeyPressEventArgs e)
		{
			string keyChar = e.KeyChar.ToString();
			int totalRows = _grid.Rows.Count;
			if (totalRows == 0 || char.IsControl(e.KeyChar))
			{
				return;
			}

			int curRowIndex = 0;
			DataGridViewSelectedRowCollection selectedRows = _grid.SelectedRows;
			if (selectedRows.Count >= 1)
			{
				curRowIndex = (selectedRows[0].Index + 1) % totalRows;
			}

			int newRowIndex = curRowIndex;
			do
			{
				MagicWord magicWord = _grid.Rows[newRowIndex].DataBoundItem as MagicWord;
				if (magicWord != null && magicWord.Alias.StartsWith(keyChar, true, null))
				{
					_grid.ClearSelection();
					_grid.Rows[newRowIndex].Selected = true;
					_grid.CurrentCell = _grid.Rows[newRowIndex].Cells[0];
					break;
				}

				newRowIndex = (newRowIndex + 1) % totalRows;
			}
			while (newRowIndex != curRowIndex);
		}
	}
}
