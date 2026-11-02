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

using DMT.Library.Ui;

namespace DMT.Modules.Launcher
{
	/// <summary>
	/// Hotkeys for the launcher
	/// </summary>
	class LauncherHotKeysOptionsPanel : SettingsPage
	{
		LauncherModule _launcherModule;

		public LauncherHotKeysOptionsPanel(LauncherModule launcherModule)
		{
			_launcherModule = launcherModule;

			AddText("Open the launcher and type a magic word to start a program, open a file or run a DMT command.");
			this.AddHotKey(_launcherModule.ActivateHotKeyController, null, null, Theme.GlyphSearch);
			this.AddHotKey(_launcherModule.AddMagicWordHotKeyController, null, "Create a magic word for the program you're using", Theme.GlyphAdd);
		}
	}
}
