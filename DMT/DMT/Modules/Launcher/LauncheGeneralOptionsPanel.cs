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

using System.Collections.Generic;
using System.Windows.Forms;
using DMT.Library.Ui;

namespace DMT.Modules.Launcher
{
	/// <summary>
	/// General launcher settings
	/// </summary>
	class LauncheGeneralOptionsPanel : SettingsPage
	{
		static readonly Dictionary<View, string> ViewNames = new Dictionary<View, string>
		{
			{ View.LargeIcon, "Grid" },
			{ View.Details, "List" },
			{ View.List, "Multi-column list" },
		};

		LauncherModule _launcherModule;

		public LauncheGeneralOptionsPanel(LauncherModule launcherModule)
		{
			_launcherModule = launcherModule;

			AddHeader("Suggestions");
			AddToggle("Suggest the most recently used words first", "Otherwise the most used words are suggested first", Theme.GlyphList,
				_launcherModule.UseMru, v => _launcherModule.UseMru = v);

			AddHeader("Icons");
			string selected;
			if (!ViewNames.TryGetValue(_launcherModule.IconView, out selected))
			{
				selected = ViewNames[View.LargeIcon];
			}

			ComboBox view = MakeCombo(new List<object>(ViewNames.Values), selected, item =>
			{
				foreach (KeyValuePair<View, string> pair in ViewNames)
				{
					if (pair.Value == (string)item && pair.Key != _launcherModule.IconView)
					{
						_launcherModule.IconView = pair.Key;
					}
				}
			}, 200);
			AddCard("Show matching words as", null, Theme.GlyphTiles, view);
			AddCard("Most icons to show", null, Theme.GlyphGrid, MakeNumber(0, 100, _launcherModule.MaxIcons, v => _launcherModule.MaxIcons = v));

			AddHeader("Starting programs");
			AddToggle("Load magic words when DMT starts", "Otherwise they're loaded the first time the launcher is used", Theme.GlyphCommand,
				_launcherModule.LoadWordsOnStartup, v => _launcherModule.LoadWordsOnStartup = v);
			AddCard("Wait for a program's window for up to", "Used to move new windows to their chosen position (seconds)", "",
				MakeNumber(1, 3600, _launcherModule.StartupTimeout, v => _launcherModule.StartupTimeout = v));
		}
	}
}
