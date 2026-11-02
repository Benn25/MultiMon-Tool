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
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Windows.Forms;

	using DMT.Library.Settings;
	using DMT.Library.Ui;
	using DMT.Resources;

	/// <summary>
	/// Options panel for the general options for DMT
	/// </summary>
	class GeneralOptionsPanel : SettingsPage
	{
		GeneralModule _generalModule;

		/// <summary>
		/// Initialises a new instance of the <see cref="GeneralOptionsPanel" /> class.
		/// </summary>
		/// <param name="generalModule">The general module</param>
		public GeneralOptionsPanel(GeneralModule generalModule)
		{
			_generalModule = generalModule;

			AddHeader("Startup");
			AddToggle("Start with Windows", null, "", _generalModule.StartWhenWindowsStarts, v => _generalModule.StartWhenWindowsStarts = v);
			AddToggle("Add to the desktop right-click menu", "Quick access to the most used commands", "", _generalModule.InDesktopContextMenu, v => _generalModule.InDesktopContextMenu = v);

			AddHeader("About");
			string installType = _generalModule.IsMsiInstall ? GeneralStrings.MsiInstall : GeneralStrings.PortableInstall;
			FluentButton website = new FluentButton("Website", Theme.GlyphOpen);
			website.Click += (s, e) => Open("https://dualmonitortool.sourceforge.net");
			SettingsCard about = AddCard("Dual Monitor Tools " + _generalModule.Version.ToString(), installType, Theme.GlyphMonitor, website);
			TextBlock details = new TextBlock(
				CommonStrings.MyDescription + Environment.NewLine + Environment.NewLine +
				AboutForm.AssemblyCopyright + Environment.NewLine +
				CommonStrings.License, TextRole.Caption, TextTone.Secondary);
			about.Content = details;

			AddHeader("Files");
			AddFile("Program", FileLocations.Instance.ExecutableFilename, "");
			AddFile("Settings", FileLocations.Instance.SettingsFilename, Theme.GlyphSettings);
			AddFile("Magic words", FileLocations.Instance.MagicWordsFilename, Theme.GlyphCommand);
			AddFile("Wallpaper sources", FileLocations.Instance.WallpaperProvidersFilename, Theme.GlyphPhoto);
			AddFile("Log", FileLocations.Instance.LogFilename, Theme.GlyphList);
		}

		void AddFile(string title, string path, string glyph)
		{
			if (string.IsNullOrEmpty(path))
			{
				AddCard(title, GeneralStrings.FileNotUsed, glyph);
				return;
			}

			FluentButton show = new FluentButton("Show in folder", Theme.GlyphFolder) { ShowIconOnly = true };
			show.Click += (s, e) => ShowInFolder(path);
			new ToolTip().SetToolTip(show, "Show in folder");
			AddCard(title, path, glyph, show);
		}

		static void ShowInFolder(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"") { UseShellExecute = true });
				}
				else
				{
					string folder = Path.GetDirectoryName(path);
					if (Directory.Exists(folder))
					{
						Open(folder);
					}
				}
			}
			catch (Exception)
			{
				// nothing useful we can do
			}
		}

		static void Open(string target)
		{
			try
			{
				Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
			}
			catch (Exception)
			{
				// nothing useful we can do
			}
		}
	}
}
