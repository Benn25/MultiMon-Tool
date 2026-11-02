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
using DMT.Library.HotKeys;
using DMT.Library.Ui;

namespace DMT.Modules.SwapScreen
{
	/// <summary>
	/// Settings for the numpad zones (System Defined Areas):
	/// a modifier key per screen used together with the numeric keypad
	/// to move the active window to a full, half or quarter screen.
	/// </summary>
	class SwapScreenSdaOptionsPanel : SettingsPage
	{
		/// <summary>
		/// The modifiers that can be assigned to screens, in the order they are given out
		/// </summary>
		static readonly uint[] DefaultSdaModifiers =
		{
			// Shift doesn't tend to work with the numpad (Num Lock turns Shift+Num7 into Home etc.)
			KeyCombo.FlagControl,
			KeyCombo.FlagWin,
			KeyCombo.FlagAlt,
			KeyCombo.FlagWin | KeyCombo.FlagControl,
			KeyCombo.FlagWin | KeyCombo.FlagAlt,
			KeyCombo.FlagControl | KeyCombo.FlagAlt,
			// tricky to press, but available for setups with lots of screens
			KeyCombo.FlagControl | KeyCombo.FlagWin | KeyCombo.FlagAlt
		};

		SwapScreenModule _swapScreenModule;
		bool _loading;

		InfoBar _numLockBar;
		InfoBar _errorBar;
		ToggleSwitch _enableSwitch;
		ToggleSwitch _borderSwitch;
		MonitorMap _monitorMap;
		NumpadView _numpad;
		TextBlock _zoneCaption;
		TextBlock _screensHeader;
		List<SettingsCard> _screenCards = new List<SettingsCard>();
		Timer _numLockTimer;

		/// <summary>
		/// Initialises a new instance of the <see cref="SwapScreenSdaOptionsPanel" /> class.
		/// </summary>
		/// <param name="swapScreenModule">Swap screen module</param>
		public SwapScreenSdaOptionsPanel(SwapScreenModule swapScreenModule)
		{
			_swapScreenModule = swapScreenModule;
			_loading = true;

			EnsureModifierList();

			AddText("Hold a modifier key and press a number on the numeric keypad to move the active window " +
				"to a full, half or quarter of a screen. Each screen has its own modifier, so the same keys work on every screen.");

			_numLockBar = Add(new InfoBar(Severity.Warning, "Num Lock is off", "Numpad zones only work while Num Lock is on."), 16);
			_numLockBar.Visible = false;

			_enableSwitch = new ToggleSwitch { Checked = _swapScreenModule.EnableSdas };
			_enableSwitch.CheckedChanged += EnableSwitch_CheckedChanged;
			Add(new SettingsCard("Numpad zones", "Move windows with a modifier + numeric keypad", Theme.GlyphTiles, _enableSwitch), 16);

			_borderSwitch = new ToggleSwitch { Checked = _swapScreenModule.BorderOutsideSda };
			_borderSwitch.CheckedChanged += BorderSwitch_CheckedChanged;
			AddCard("Line windows up edge to edge",
				"Windows has invisible borders around most windows. Push them outside the zone so neighbouring windows don't leave gaps.",
				Theme.GlyphFrame, _borderSwitch);

			_errorBar = Add(new InfoBar(Severity.Error, "Some shortcuts are already in use", string.Empty), 8);
			_errorBar.Visible = false;

			AddHeader("Preview");
			_numpad = new NumpadView();
			_numpad.HotKeyChanged += Numpad_HotKeyChanged;
			_monitorMap = new MonitorMap();
			_zoneCaption = new TextBlock(string.Empty, TextRole.Body, TextTone.Secondary);
			SettingsCard previewCard = AddCard("Hover over a key to see where it puts the window", null, null, null);
			previewCard.Content = new PreviewLayout(_numpad, _monitorMap, _zoneCaption);

			_screensHeader = AddHeader("Modifier for each screen");
			BuildScreenCards();

			ShowErrors();
			ShowZoneCaption();
			UpdateEnabledState();

			_numLockTimer = new Timer { Interval = 500 };
			_numLockTimer.Tick += (s, e) => UpdateNumLock();
			_loading = false;
		}

		/// <summary>
		/// Shows any hot keys that couldn't be registered (called by the module after regenerating the SDAs)
		/// </summary>
		public void ShowErrors()
		{
			if (IsDisposed)
			{
				return;
			}

			List<string> errors = _swapScreenModule.SdaHotKeyErrors;
			_errorBar.Visible = errors.Count > 0;
			_errorBar.Text = "Another program has already taken these, so they won't work: " + string.Join(", ", errors) + ".";

			// the monitors may have changed too
			if (_screenCards.Count != Monitor.AllMonitors.Count)
			{
				BuildScreenCards();
			}

			_monitorMap.Refresh();
			UpdateMapLabels();
		}

		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			if (_numLockTimer != null)
			{
				_numLockTimer.Enabled = Visible;
				UpdateNumLock();
			}
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			if (_numLockTimer != null)
			{
				_numLockTimer.Stop();
			}

			base.OnHandleDestroyed(e);
		}

		void EnsureModifierList()
		{
			uint[] saved = _swapScreenModule.SdaModifiers;
			if (saved.Length < DefaultSdaModifiers.Length)
			{
				// keep the user's order, then add any modifiers they don't have yet
				List<uint> modifiers = saved.Where(m => DefaultSdaModifiers.Contains(m)).Distinct().ToList();
				modifiers.AddRange(DefaultSdaModifiers.Where(m => !modifiers.Contains(m)));
				_swapScreenModule.SdaModifiers = modifiers.ToArray();
			}
		}

		void BuildScreenCards()
		{
			SuspendLayout();
			foreach (SettingsCard card in _screenCards)
			{
				Controls.Remove(card);
				card.Dispose();
			}

			_screenCards.Clear();

			Monitors monitors = Monitor.AllMonitors;
			string[] names = MonitorDescriptions.GetNames();
			uint[] modifiers = _swapScreenModule.SdaModifiers;

			for (int i = 0; i < monitors.Count; i++)
			{
				int screenIndex = i;
				ComboBox combo = new ComboBox
				{
					DropDownStyle = ComboBoxStyle.DropDownList,
					FlatStyle = FlatStyle.Flat,
				}.WithLogicalWidth(190);
				foreach (uint modifier in DefaultSdaModifiers)
				{
					combo.Items.Add(new ModifierItem(modifier));
				}

				if (i < modifiers.Length)
				{
					combo.SelectedIndex = Array.IndexOf(DefaultSdaModifiers, modifiers[i]);
				}

				combo.SelectedIndexChanged += (s, e) => ModifierChanged(screenIndex, combo);
				combo.AccessibleName = string.Format("Modifier for screen {0}", i + 1);

				Rectangle bounds = monitors[i].Bounds;
				string description = string.Format("{0} × {1}{2}", bounds.Width, bounds.Height, monitors[i].Primary ? " · main display" : string.Empty);
				string title = string.IsNullOrEmpty(names[i]) ? string.Format("Screen {0}", i + 1) : string.Format("Screen {0} · {1}", i + 1, names[i]);
				SettingsCard card = new SettingsCard(title, description, Theme.GlyphMonitor, combo);
				Add(card, _screenCards.Count == 0 ? 8 : 4);
				_screenCards.Add(card);
			}

			ResumeLayout();
			UpdateMapLabels();
		}

		void ModifierChanged(int screenIndex, ComboBox combo)
		{
			if (_loading)
			{
				return;
			}

			ModifierItem item = combo.SelectedItem as ModifierItem;
			if (item == null)
			{
				return;
			}

			// a modifier can only be used by one screen, so swap with whoever had it
			List<uint> modifiers = _swapScreenModule.SdaModifiers.ToList();
			int previousOwner = modifiers.IndexOf(item.Modifier);
			if (previousOwner == screenIndex)
			{
				return;
			}

			uint oldModifier = modifiers[screenIndex];
			modifiers[screenIndex] = item.Modifier;
			if (previousOwner >= 0)
			{
				modifiers[previousOwner] = oldModifier;
			}

			_swapScreenModule.SdaModifiers = modifiers.ToArray();

			// reflect the swap in the other screen's drop down
			_loading = true;
			if (previousOwner >= 0 && previousOwner < _screenCards.Count)
			{
				ComboBox other = _screenCards[previousOwner].Action as ComboBox;
				if (other != null)
				{
					other.SelectedIndex = Array.IndexOf(DefaultSdaModifiers, oldModifier);
				}
			}

			_loading = false;

			UpdateMapLabels();
			_swapScreenModule.GenerateSDAs();
		}

		void EnableSwitch_CheckedChanged(object sender, EventArgs e)
		{
			if (!_loading)
			{
				_swapScreenModule.EnableSdas = _enableSwitch.Checked;
				_swapScreenModule.GenerateSDAs();
			}

			UpdateEnabledState();
		}

		void BorderSwitch_CheckedChanged(object sender, EventArgs e)
		{
			if (!_loading)
			{
				_swapScreenModule.BorderOutsideSda = _borderSwitch.Checked;
			}
		}

		void UpdateEnabledState()
		{
			bool enabled = _enableSwitch.Checked;
			foreach (SettingsCard card in _screenCards)
			{
				card.Enabled = enabled;
			}

			UpdateNumLock();
		}

		void UpdateNumLock()
		{
			bool show = _enableSwitch.Checked && !Control.IsKeyLocked(Keys.NumLock);
			if (_numLockBar.Visible != show)
			{
				_numLockBar.Visible = show;
			}
		}

		void Numpad_HotKeyChanged(object sender, EventArgs e)
		{
			int key = _numpad.HotKey;
			if (key < 0)
			{
				_monitorMap.SetZone(null);
			}
			else
			{
				_monitorMap.SetZone(rect => NumpadView.ZoneFor(key, rect), key == 0);
			}

			UpdateMapLabels();
			ShowZoneCaption();
		}

		void ShowZoneCaption()
		{
			int key = _numpad.HotKey;
			if (key < 0)
			{
				_zoneCaption.Text = "Each screen's modifier is shown in its corner. Num 0 with the first screen's modifier stretches the window across every screen.";
				return;
			}

			uint[] modifiers = _swapScreenModule.SdaModifiers;
			string keys;
			if (key == 0)
			{
				keys = modifiers.Length > 0 ? ModifierName(modifiers[0]) + " + Num 0" : "Num 0";
			}
			else
			{
				keys = string.Format("Modifier + Num {0}", key);
			}

			_zoneCaption.Text = string.Format("{0}  —  {1}", NumpadView.ZoneName(key), keys);
		}

		void UpdateMapLabels()
		{
			uint[] modifiers = _swapScreenModule.SdaModifiers;
			int key = _numpad == null ? -1 : _numpad.HotKey;
			string[] labels = new string[Monitor.AllMonitors.Count];
			for (int i = 0; i < labels.Length && i < modifiers.Length; i++)
			{
				if (key == 0)
				{
					// stretching across all screens only uses the first screen's modifier
					labels[i] = i == 0 ? ModifierName(modifiers[0]) + " + Num 0" : null;
				}
				else
				{
					labels[i] = ModifierName(modifiers[i]) + (key > 0 ? " + Num " + key : string.Empty);
				}
			}

			_monitorMap.Labels = labels;
		}

		static string ModifierName(uint modifier)
		{
			return string.Join(" + ", UiHelper.KeyParts(new KeyCombo(modifier), false));
		}

		class ModifierItem
		{
			public ModifierItem(uint modifier)
			{
				Modifier = modifier;
			}

			public uint Modifier { get; private set; }

			public override string ToString()
			{
				return ModifierName(Modifier);
			}
		}

		/// <summary>
		/// Numpad on the left, monitors on the right (stacked when narrow), with a caption underneath
		/// </summary>
		class PreviewLayout : Panel, IHeightForWidth
		{
			NumpadView _numpad;
			MonitorMap _map;
			TextBlock _caption;

			public PreviewLayout(NumpadView numpad, MonitorMap map, TextBlock caption)
			{
				_numpad = numpad;
				_map = map;
				_caption = caption;
				Controls.Add(numpad);
				Controls.Add(map);
				Controls.Add(caption);
			}

			public int GetHeightForWidth(int width)
			{
				return Arrange(width, false);
			}

			protected override void OnLayout(LayoutEventArgs levent)
			{
				Arrange(Width, true);
			}

			int Arrange(int width, bool apply)
			{
				Size pad = _numpad.GetPreferredSize(Size.Empty);
				int gap = LogicalToDeviceUnits(24);
				bool stacked = width - pad.Width - gap < LogicalToDeviceUnits(240);
				int y;

				if (stacked)
				{
					int mapHeight = _map.GetHeightForWidth(width);
					if (apply)
					{
						_numpad.SetBounds(0, 0, pad.Width, pad.Height);
						_map.SetBounds(0, pad.Height + gap, width, mapHeight);
					}

					y = pad.Height + gap + mapHeight;
				}
				else
				{
					int mapWidth = width - pad.Width - gap;
					int mapHeight = Math.Max(_map.GetHeightForWidth(mapWidth), pad.Height);
					if (apply)
					{
						_numpad.SetBounds(0, (mapHeight - pad.Height) / 2, pad.Width, pad.Height);
						_map.SetBounds(pad.Width + gap, 0, mapWidth, mapHeight);
					}

					y = mapHeight;
				}

				y += LogicalToDeviceUnits(12);
				int captionHeight = _caption.GetHeightForWidth(width);
				if (apply)
				{
					_caption.SetBounds(0, y, width, captionHeight);
				}

				return y + captionHeight;
			}
		}
	}
}
