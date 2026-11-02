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

namespace DMT.Modules.Cursor
{
	/// <summary>
	/// Hotkeys for moving the cursor between screens
	/// </summary>
	class CursorGeneralOptionsPanel : SettingsPage
	{
		CursorModule _cursorModule;

		public CursorGeneralOptionsPanel(CursorModule cursorModule)
		{
			_cursorModule = cursorModule;

			AddText("Jump the mouse cursor between screens. The cursor keeps its relative position on the new screen.");

			AddHeader("Move the cursor");
			this.AddHotKey(_cursorModule.CursorNextScreenHotKeyController, null, null, Theme.GlyphChevronRight);
			this.AddHotKey(_cursorModule.CursorPrevScreenHotKeyController, null, null, "");
			this.AddHotKey(_cursorModule.CursorToPrimaryScreenHotKeyController, null, null, Theme.GlyphHome);

			AddHeader("Lock");
			this.AddHotKey(_cursorModule.LockCursorToMonitorHotKeyController, null,
				"Press again to release. Free movement keys and buttons don't work while the cursor is locked this way.", "");
		}
	}
}
