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
using System.Collections.ObjectModel;
using System.Windows.Forms;
using DMT.Library.Ui;
using DMT.Resources;

namespace DMT.Modules.Launcher
{
	/// <summary>
	/// Import, export and delete magic words
	/// </summary>
	class LauncherImportOptionsPanel : SettingsPage
	{
		LauncherModule _launcherModule;

		public LauncherImportOptionsPanel(LauncherModule launcherModule)
		{
			_launcherModule = launcherModule;

			AddHeader("Back up and restore");
			FluentButton importXml = new FluentButton("Import", "");
			importXml.Click += ImportXml_Click;
			FluentButton exportXml = new FluentButton("Export", "");
			exportXml.Click += ExportXml_Click;
			AddCard("Magic words file", "DMT's own XML format (also used by DualLauncher)", Theme.GlyphCommand, new HStack(importXml, exportXml));

			AddHeader("Bring words from other launchers");
			FluentButton importQrs = new FluentButton("Import", "");
			importQrs.Click += ImportQrs_Click;
			AddCard("SlickRun magic words", "QRS files", Theme.GlyphSwap, importQrs);

			AddHeader("Start again");
			FluentButton deleteAll = new FluentButton("Delete all", Theme.GlyphDelete);
			deleteAll.Click += DeleteAll_Click;
			AddCard("Delete every magic word", "You'll be asked to confirm. Export them first if you might want them back.", Theme.GlyphDelete, deleteAll);
		}

		void DeleteAll_Click(object sender, EventArgs e)
		{
			if (_launcherModule.MagicWords.Count > 0)
			{
				if (MessageBox.Show(
					FindForm(),
					LauncherStrings.ConfirmDelAllMW,
					CommonStrings.MyTitle,
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question,
					MessageBoxDefaultButton.Button2) == DialogResult.Yes)
				{
					_launcherModule.MagicWords.Clear();
				}
			}
		}

		void ImportXml_Click(object sender, EventArgs e)
		{
			Import(LauncherStrings.XmlFilter, XmlImporter.Import);
		}

		void ImportQrs_Click(object sender, EventArgs e)
		{
			Import(LauncherStrings.QrsFilter, QrsImporter.Import);
		}

		void Import(string filter, Func<string, Collection<MagicWord>> importer)
		{
			using (OpenFileDialog dlg = new OpenFileDialog { Filter = filter })
			{
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
				{
					try
					{
						_launcherModule.MagicWords.Merge(importer(dlg.FileName));
					}
					catch (Exception ex)
					{
						MessageBox.Show(FindForm(), ex.Message, CommonStrings.MyTitle);
					}
				}
			}
		}

		void ExportXml_Click(object sender, EventArgs e)
		{
			using (SaveFileDialog dlg = new SaveFileDialog { Filter = LauncherStrings.XmlFilter })
			{
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
				{
					try
					{
						XmlImporter.Export(_launcherModule.MagicWords, dlg.FileName);
					}
					catch (Exception ex)
					{
						MessageBox.Show(FindForm(), ex.Message, CommonStrings.MyTitle);
					}
				}
			}
		}
	}
}
