#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2026
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

namespace DMT.Library.Ui
{
	using System;
	using System.Windows.Forms;

	using DMT.Library.HotKeys;

	/// <summary>
	/// A card showing a hotkey as key caps, with buttons to change or remove it
	/// </summary>
	class HotKeyCard : SettingsCard
	{
		HotKeyController _controller;
		KeyChips _chips;
		FluentButton _editButton;
		FluentButton _clearButton;

		public HotKeyCard(HotKeyController controller, string title = null, string description = null, string glyph = null)
			: base(title ?? SentenceCase(controller.Description), description, glyph)
		{
			_controller = controller;

			_chips = new KeyChips();
			_editButton = new FluentButton("Change shortcut", Theme.GlyphEdit, ButtonKind.Subtle) { ShowIconOnly = true };
			_clearButton = new FluentButton("Remove shortcut", Theme.GlyphClear, ButtonKind.Subtle) { ShowIconOnly = true };
			_editButton.Click += (s, e) => Edit();
			_clearButton.Click += (s, e) => Clear();

			ToolTip toolTip = new ToolTip();
			toolTip.SetToolTip(_editButton, "Change shortcut");
			toolTip.SetToolTip(_clearButton, "Remove shortcut");
			Disposed += (s, e) => toolTip.Dispose();

			Action = new HStack(_chips, _editButton, _clearButton) { Spacing = 4 };
			ShowKeys();
		}

		void Edit()
		{
			if (_controller.Edit(FindForm()))
			{
				ShowKeys();
			}
		}

		void Clear()
		{
			_controller.Clear();
			ShowKeys();
			_editButton.Focus();
		}

		void ShowKeys()
		{
			_chips.SetKeyCombo(_controller.KeyCombo);
			_clearButton.Visible = _controller.IsEnabled();
			if (Action != null)
			{
				Action.PerformLayout();
			}

			PerformLayout();
		}

		/// <summary>
		/// Hotkey descriptions tend to be in Title Case, this gives them the more modern sentence case
		/// (only lower-casing words that are entirely Title Case so acronyms are left alone)
		/// </summary>
		static string SentenceCase(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return text;
			}

			string[] words = text.Split(' ');
			for (int i = 1; i < words.Length; i++)
			{
				string word = words[i];
				if (word.Length > 1 && char.IsUpper(word[0]) && word.Substring(1).ToLowerInvariant() == word.Substring(1))
				{
					words[i] = char.ToLowerInvariant(word[0]) + word.Substring(1);
				}
			}

			return string.Join(" ", words);
		}
	}

	static class SettingsPageExtensions
	{
		/// <summary>
		/// Adds a hotkey card to the page
		/// </summary>
		public static HotKeyCard AddHotKey(this SettingsPage page, HotKeyController controller, string title = null, string description = null, string glyph = null)
		{
			if (controller == null)
			{
				return null;
			}

			bool afterHeader = page.Controls.Count > 0 && page.Controls[page.Controls.Count - 1] is TextBlock;
			return page.Add(new HotKeyCard(controller, title, description, glyph), afterHeader ? 8 : 4);
		}
	}
}
