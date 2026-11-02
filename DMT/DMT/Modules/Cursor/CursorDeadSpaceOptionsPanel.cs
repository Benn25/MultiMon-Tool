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
	/// Settings for jumping the cursor over "dead space" in non-rectangular monitor layouts
	/// </summary>
	class CursorDeadSpaceOptionsPanel : SettingsPage
	{
		CursorModule _cursorModule;

		public CursorDeadSpaceOptionsPanel(CursorModule cursorModule)
		{
			_cursorModule = cursorModule;

			AddText("When screens of different sizes or offsets sit next to each other, some edges have no screen on the other side, " +
				"so the cursor can't cross them. These options let the cursor jump to the nearest point on the next screen instead.");

			Add(new MonitorMap(), 16);

			ToggleSwitch horizontal = new ToggleSwitch { Checked = _cursorModule.AllowDeadSpaceXJump };
			horizontal.CheckedChanged += (s, e) => _cursorModule.AllowDeadSpaceXJump = horizontal.Checked;
			Add(new SettingsCard("Jump across gaps when moving sideways", null, "", horizontal), 16);

			ToggleSwitch vertical = new ToggleSwitch { Checked = _cursorModule.AllowDeadSpaceYJump };
			vertical.CheckedChanged += (s, e) => _cursorModule.AllowDeadSpaceYJump = vertical.Checked;
			AddCard("Jump across gaps when moving up or down", null, "", vertical);

			AddText("Usually you only need one of these, and only if your screens don't line up.", TextTone.Tertiary);
		}
	}
}
