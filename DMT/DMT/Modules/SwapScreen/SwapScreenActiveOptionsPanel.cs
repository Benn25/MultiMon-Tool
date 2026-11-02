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
	/// Hotkeys that act on the active window
	/// </summary>
	class SwapScreenActiveOptionsPanel : SettingsPage
	{
		SwapScreenModule _swapScreenModule;

		public SwapScreenActiveOptionsPanel(SwapScreenModule swapScreenModule)
		{
			_swapScreenModule = swapScreenModule;

			AddText("Shortcuts that act on the window you're using.");

			AddHeader("Move between screens");
			this.AddHotKey(_swapScreenModule.NextScreenHotKeyController, null, null, Theme.GlyphChevronRight);
			this.AddHotKey(_swapScreenModule.PrevScreenHotKeyController, null, null, "");

			AddHeader("Size");
			this.AddHotKey(_swapScreenModule.MinimiseHotKeyController, null, null, "");
			this.AddHotKey(_swapScreenModule.MaximiseHotKeyController, null, null, "");
			this.AddHotKey(_swapScreenModule.SupersizeHotKeyController, null, "Stretch the window across all of your screens", Theme.GlyphFullScreen);

			AddHeader("Arrange");
			this.AddHotKey(_swapScreenModule.SnapLeftHotKeyController, null, "Each press moves to the next half, continuing onto the next screen", "");
			this.AddHotKey(_swapScreenModule.SnapRightHotKeyController, null, "Each press moves to the next half, continuing onto the next screen", "");
			this.AddHotKey(_swapScreenModule.SnapUpHotKeyController, null, null, "");
			this.AddHotKey(_swapScreenModule.SnapDownHotKeyController, null, null, "");
			this.AddHotKey(_swapScreenModule.SwapTop2HotKeyController, null, "Swap the size and position of the two top windows", Theme.GlyphSwap);
		}
	}
}
