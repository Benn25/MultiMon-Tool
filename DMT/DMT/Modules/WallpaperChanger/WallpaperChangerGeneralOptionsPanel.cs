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
using DMT.Library.Ui;
using DMT.Library.Wallpaper;

namespace DMT.Modules.WallpaperChanger
{
	/// <summary>
	/// When and how the wallpaper is changed
	/// </summary>
	class WallpaperChangerGeneralOptionsPanel : SettingsPage
	{
		WallpaperChangerModule _wallpaperChangerModule;
		SettingsCard _changeNowCard;
		ToggleSwitch _periodic;
		NumericUpDown _hours;
		NumericUpDown _minutes;
		SettingsCard _busyCard;
		NumericUpDown _quality;
		ToggleSwitch _aspect;
		ToggleSwitch _clip;
		ToggleSwitch _enlarge;
		ToggleSwitch _shrink;
		bool _loading = true;

		public WallpaperChangerGeneralOptionsPanel(WallpaperChangerModule wallpaperChangerModule)
		{
			_wallpaperChangerModule = wallpaperChangerModule;

			FluentButton changeNow = new FluentButton("Change now", Theme.GlyphRefresh, ButtonKind.Accent);
			changeNow.Click += (s, e) => _wallpaperChangerModule.UpdateWallpaper();
			_changeNowCard = Add(new SettingsCard("New wallpaper", string.Empty, Theme.GlyphPhoto, changeNow), 0);
			this.AddHotKey(_wallpaperChangerModule.ChangeWallpaperHotKeyController, "Shortcut for a new wallpaper", null, Theme.GlyphKeyboard);

			AddHeader("When to change");
			AddToggle("When DMT starts", null, "", _wallpaperChangerModule.ChangeOnStartup, v => _wallpaperChangerModule.ChangeOnStartup = v);
			AddToggle("When monitors or resolutions change", null, Theme.GlyphMonitor, _wallpaperChangerModule.ChangeOnResolutionChange, v => _wallpaperChangerModule.ChangeOnResolutionChange = v);

			_hours = MakeNumber(0, 24, _wallpaperChangerModule.IntervalHours, v => _wallpaperChangerModule.IntervalHours = v, 70);
			_minutes = MakeNumber(0, 59, _wallpaperChangerModule.IntervalMinutes, v => _wallpaperChangerModule.IntervalMinutes = v, 70);
			_periodic = new ToggleSwitch { Checked = _wallpaperChangerModule.ChangePeriodically };
			_periodic.CheckedChanged += (s, e) =>
			{
				_wallpaperChangerModule.ChangePeriodically = _periodic.Checked;
				UpdateEnabledState();
			};
			HStack every = new HStack(
				new TextBlock("Every", TextRole.Body, TextTone.Secondary) { Wrap = false }, _hours,
				new TextBlock("h", TextRole.Body, TextTone.Secondary) { Wrap = false }, _minutes,
				new TextBlock("min", TextRole.Body, TextTone.Secondary) { Wrap = false }, _periodic) { Spacing = 8 };
			AddCard("Regularly", null, "", every);

			ToggleSwitch busy = new ToggleSwitch { Checked = _wallpaperChangerModule.DontChangeIfBusy };
			busy.CheckedChanged += (s, e) => _wallpaperChangerModule.DontChangeIfBusy = busy.Checked;
			_busyCard = AddCard("Not while gaming or presenting", "Skip regular changes while a full screen game or presentation is running", "", busy);

			AddToggle("Fade smoothly to the new wallpaper", null, "", _wallpaperChangerModule.SmoothFade, v => _wallpaperChangerModule.SmoothFade = v);

			AddHeader("Fitting images to screens");
			StretchType.Fit fit = _wallpaperChangerModule.Fit;
			_aspect = AddToggle("Keep the aspect ratio", null, Theme.GlyphFrame, fit.HasFlag(StretchType.Fit.MaintainAspectRatio), v => UpdateFit());
			_clip = AddToggle("Crop to avoid bars", "Fill the screen, cutting off the edges of the image if needed", "", fit.HasFlag(StretchType.Fit.ClipImage), v => UpdateFit());
			_enlarge = AddToggle("Enlarge small images", null, Theme.GlyphFullScreen, fit.HasFlag(StretchType.Fit.AllowEnlarge), v => UpdateFit());
			_shrink = AddToggle("Shrink large images", null, "", fit.HasFlag(StretchType.Fit.AllowShrink), v => UpdateFit());

			List<SwitchType> switchTypes = SwitchType.AllTypes();
			SwitchType selectedSwitch = switchTypes.FirstOrDefault(t => t.Mapping == _wallpaperChangerModule.MonitorMapping);
			ComboBox mapping = MakeCombo(switchTypes.Cast<object>(), selectedSwitch, item => _wallpaperChangerModule.MonitorMapping = ((SwitchType)item).Mapping, 260);
			AddCard("With more than one screen", null, Theme.GlyphLayout, mapping);

			ColorSwatch swatch = new ColorSwatch(_wallpaperChangerModule.BackgroundColour);
			FluentButton pickColour = new FluentButton("Choose");
			pickColour.Click += (s, e) =>
			{
				using (ColorDialog colorDialog = new ColorDialog { Color = _wallpaperChangerModule.BackgroundColour, FullOpen = true })
				{
					if (colorDialog.ShowDialog(FindForm()) == DialogResult.OK)
					{
						swatch.SwatchColor = colorDialog.Color;
						_wallpaperChangerModule.BackgroundColour = colorDialog.Color;
					}
				}
			};
			AddCard("Background colour", "Shown around images that don't fill the screen", Theme.GlyphPalette, new HStack(swatch, pickColour));

			AddHeader("Saved wallpaper");
			List<WallpaperImageFormat> formats = WallpaperImageFormat.AllImageFormats();
			WallpaperImageFormat selectedFormat = formats.FirstOrDefault(f => f.Format == _wallpaperChangerModule.ImageFormat);
			_quality = MakeNumber(0, 100, _wallpaperChangerModule.ImageQuality, v => _wallpaperChangerModule.ImageQuality = v, 70);
			ComboBox format = MakeCombo(formats.Cast<object>(), selectedFormat, item =>
			{
				_wallpaperChangerModule.ImageFormat = ((WallpaperImageFormat)item).Format;
				UpdateEnabledState();
			}, 140);
			AddCard("File format", "The image Windows is given as the wallpaper", "", format);
			AddCard("JPEG quality", "Higher looks better but makes a bigger file", "", _quality);

			_loading = false;
			UpdateEnabledState();
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			// the module only knows about this page once it has been created, so ask now
			_wallpaperChangerModule.UpdateTimeToChange();
		}

		/// <summary>
		/// Shows when the wallpaper will next change (called by the module)
		/// </summary>
		public void ShowNextChange(string nextChangeMsg, Color foreColor)
		{
			if (IsDisposed || _changeNowCard == null)
			{
				return;
			}

			_changeNowCard.Description = nextChangeMsg;
		}

		void UpdateEnabledState()
		{
			bool periodic = _periodic.Checked;
			_hours.Enabled = periodic;
			_minutes.Enabled = periodic;
			_busyCard.Enabled = periodic;

			WallpaperImageFormat format = WallpaperImageFormat.AllImageFormats().FirstOrDefault(f => f.Format == _wallpaperChangerModule.ImageFormat);
			_quality.Enabled = format != null && format.Format == WallpaperImageFormat.ImageFormat.JPG;
		}

		void UpdateFit()
		{
			if (_loading)
			{
				return;
			}

			StretchType.Fit newFit = StretchType.Fit.NewFit;
			if (_aspect.Checked)
			{
				newFit |= StretchType.Fit.MaintainAspectRatio;
			}

			if (_clip.Checked)
			{
				newFit |= StretchType.Fit.ClipImage;
			}

			if (_enlarge.Checked)
			{
				newFit |= StretchType.Fit.AllowEnlarge;
			}

			if (_shrink.Checked)
			{
				newFit |= StretchType.Fit.AllowShrink;
			}

			_wallpaperChangerModule.Fit = newFit;
		}
	}
}
