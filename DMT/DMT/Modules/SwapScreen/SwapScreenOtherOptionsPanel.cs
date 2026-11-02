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

namespace DMT.Modules.SwapScreen
{
	/// <summary>
	/// Hotkeys that act on all of the windows
	/// </summary>
	class SwapScreenOtherOptionsPanel : SettingsPage
	{
		SwapScreenModule _swapScreenModule;

		public SwapScreenOtherOptionsPanel(SwapScreenModule swapScreenModule)
		{
			_swapScreenModule = swapScreenModule;

			AddText("Shortcuts that act on every window on the current virtual desktop.");

			AddHeader("Arrange");
			this.AddHotKey(_swapScreenModule.MinimiseAllButHotKeyController, null, null, "");
			this.AddHotKey(_swapScreenModule.RotateNextHotKeyController, null, null, Theme.GlyphRefresh);
			this.AddHotKey(_swapScreenModule.RotatePrevHotKeyController, null, null, "");

			AddHeader("Show the desktop of a screen");
			this.AddHotKey(_swapScreenModule.ShowCursorDesktopHotKeyController, null, null, Theme.GlyphMouse);
			for (int desktop = 0; desktop < _swapScreenModule.MaxConfigurableDesktops; desktop++)
			{
				this.AddHotKey(_swapScreenModule.ShowDesktopHotKeyControllers[desktop], null, null, Theme.GlyphMonitor);
			}

			AddHeader("Gather all windows onto one screen");
			this.AddHotKey(_swapScreenModule.MoveAllWindowsToCursorHotKeyController, null, null, Theme.GlyphMouse);
			for (int desktop = 0; desktop < _swapScreenModule.MaxConfigurableDesktops; desktop++)
			{
				this.AddHotKey(_swapScreenModule.MoveAllWindowsToHotKeyControllers[desktop], null, null, Theme.GlyphMonitor);
			}
		}
	}
}
