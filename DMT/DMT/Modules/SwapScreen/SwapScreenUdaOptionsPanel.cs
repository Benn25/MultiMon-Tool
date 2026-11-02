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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DMT.Library.Environment;
using DMT.Library.Ui;

namespace DMT.Modules.SwapScreen
{
	/// <summary>
	/// Custom zones (User Defined Areas): positions the active window can be moved to with a hotkey.
	/// Only the zones the user has set up are listed.
	/// </summary>
	class SwapScreenUdaOptionsPanel : SettingsPage
	{
		SwapScreenModule _swapScreenModule;
		MonitorMap _map;
		FluentButton _addButton;
		TextBlock _emptyText;
		List<SettingsCard> _cards = new List<SettingsCard>();

		public SwapScreenUdaOptionsPanel(SwapScreenModule swapScreenModule)
		{
			_swapScreenModule = swapScreenModule;

			AddText("Give any position on your screens its own shortcut. Pressing it moves the active window there. " +
				"Point at a zone below to see where it is.");

			_map = Add(new MonitorMap(), 16);

			_addButton = new FluentButton("Add a zone", Theme.GlyphAdd, ButtonKind.Accent);
			_addButton.Click += AddButton_Click;
			Add(new HStack(_addButton), 16);

			_emptyText = AddText("You haven't set up any custom zones yet.", TextTone.Tertiary);
			BuildCards();
		}

		void BuildCards()
		{
			SuspendLayout();
			foreach (SettingsCard card in _cards)
			{
				Controls.Remove(card);
				card.Dispose();
			}

			_cards.Clear();

			foreach (UdaController uda in _swapScreenModule.UdaControllers.Where(u => u.IsDefined))
			{
				AddZoneCard(uda);
			}

			_emptyText.Visible = _cards.Count == 0;
			_addButton.Enabled = _swapScreenModule.UdaControllers.Any(u => !u.IsDefined);
			ResumeLayout();
		}

		void AddZoneCard(UdaController uda)
		{
			KeyChips chips = new KeyChips();
			chips.SetKeyCombo(uda.HotKey.HotKeyCombo);
			FluentButton edit = new FluentButton("Change zone", Theme.GlyphEdit, ButtonKind.Subtle) { ShowIconOnly = true };
			edit.Click += (s, e) =>
			{
				if (uda.Edit(FindForm()))
				{
					BuildCards();
				}
			};
			FluentButton remove = new FluentButton("Remove zone", Theme.GlyphDelete, ButtonKind.Subtle) { ShowIconOnly = true };
			remove.Click += (s, e) =>
			{
				uda.Clear();
				BuildCards();
			};

			string title = string.IsNullOrEmpty(uda.Description) ? "Unnamed zone" : uda.Description;
			SettingsCard card = new SettingsCard(title, DescribePosition(uda.Position), Theme.GlyphLayout, new HStack(chips, edit, remove) { Spacing = 4 });

			// preview the zone on the map while the mouse is over its card
			EventHandler show = (s, e) => _map.SetZone(r => uda.Position, true);
			EventHandler hide = (s, e) =>
			{
				if (!card.ClientRectangle.Contains(card.PointToClient(System.Windows.Forms.Cursor.Position)))
				{
					_map.SetZone(null);
				}
			};
			HookHover(card, show, hide);

			Add(card, _cards.Count == 0 ? 12 : 4);
			_cards.Add(card);
		}

		static void HookHover(Control control, EventHandler enter, EventHandler leave)
		{
			control.MouseEnter += enter;
			control.MouseLeave += leave;
			foreach (Control child in control.Controls)
			{
				HookHover(child, enter, leave);
			}
		}

		static string DescribePosition(Rectangle position)
		{
			if (position.IsEmpty)
			{
				return "No position set";
			}

			Monitors monitors = Monitor.AllMonitors;
			int screen = monitors.MonitorIndexFromRectangle(position) + 1;
			return string.Format("{0} × {1} at {2}, {3} · screen {4}", position.Width, position.Height, position.X, position.Y, screen);
		}

		void AddButton_Click(object sender, EventArgs e)
		{
			UdaController free = _swapScreenModule.UdaControllers.FirstOrDefault(u => !u.IsDefined);
			if (free != null && free.Edit(FindForm()))
			{
				BuildCards();
			}
		}
	}
}
