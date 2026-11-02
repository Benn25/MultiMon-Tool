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

namespace DMT.Modules.General
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Windows.Forms;

	using DMT.Library.DisplayHandling;
	using DMT.Library.Environment;
	using DMT.Library.Ui;

	/// <summary>
	/// Shows the monitors and lets the user change the main display, names, orientation, brightness and contrast
	/// </summary>
	class GeneralMonitorsOptionsPanel : SettingsPage
	{
		GeneralModule _generalModule;
		DisplayDevices _displayDevices;
		int _selectedDevice = -1;

		MonitorMap _map;
		SettingsCard _monitorCard;
		FluentButton _makePrimary;
		FluentButton _rename;
		FluentButton _rotate;
		SettingsCard _brightnessCard;
		SettingsCard _contrastCard;
		SettingsCard _detailsCard;
		TextBlock _details;
		InfoBar _errorBar;
		TextBlock _inactiveHeader;
		TextBlock _inactive;

		/// <summary>
		/// Initialises a new instance of the <see cref="GeneralMonitorsOptionsPanel" /> class.
		/// </summary>
		/// <param name="generalModule">The general module</param>
		/// <param name="displayDevices">The current display devices (to save getting them again)</param>
		public GeneralMonitorsOptionsPanel(GeneralModule generalModule, DisplayDevices displayDevices)
		{
			_generalModule = generalModule;

			AddText("Choose a monitor to see its details and change its settings.");

			_map = Add(new MonitorMap { Clickable = true }, 16);
			_map.MonitorClicked += Map_MonitorClicked;

			_errorBar = Add(new InfoBar(Severity.Error, "Couldn't read the monitors", string.Empty), 12);
			_errorBar.Visible = false;

			_makePrimary = new FluentButton("Make main display", Theme.GlyphHome);
			_makePrimary.Click += (s, e) => _generalModule.MakePrimary(_selectedDevice);
			_rename = new FluentButton("Rename", Theme.GlyphRename);
			_rename.Click += (s, e) => _generalModule.ShowEditNameDlg(_selectedDevice, FindForm());
			_rotate = new FluentButton("Rotate", "");
			_rotate.Click += (s, e) => { if (_generalModule.ShowOrientationDlg(_selectedDevice, FindForm())) { ShowCurrentInfo(); } };
			_monitorCard = Add(new SettingsCard(string.Empty, string.Empty, Theme.GlyphMonitor, new HStack(_makePrimary, _rename, _rotate)), 16);

			FluentButton brightness = new FluentButton("Change");
			brightness.Click += (s, e) => { if (_generalModule.ShowBrightnessDlg(_selectedDevice, FindForm())) { ShowCurrentInfo(); } };
			_brightnessCard = AddCard("Brightness", string.Empty, "", brightness);

			FluentButton contrast = new FluentButton("Change");
			contrast.Click += (s, e) => { if (_generalModule.ShowContrastDlg(_selectedDevice, FindForm())) { ShowCurrentInfo(); } };
			_contrastCard = AddCard("Contrast", string.Empty, "", contrast);

			_details = new TextBlock(string.Empty, TextRole.Body, TextTone.Secondary);
			_detailsCard = AddCard("Details", null, Theme.GlyphInfo);
			_detailsCard.Content = _details;

			_inactiveHeader = AddHeader("Connected but not in use");
			_inactive = AddText(string.Empty);

			ShowCurrentInfo(displayDevices);
		}

		/// <summary>
		/// Refreshes the information shown (also called by the module when the monitors change)
		/// </summary>
		/// <param name="displayDevices">Current display devices, or null to get them</param>
		public void ShowCurrentInfo(DisplayDevices displayDevices = null)
		{
			if (IsDisposed)
			{
				return;
			}

			try
			{
				_errorBar.Visible = false;
				_displayDevices = displayDevices ?? _generalModule.GetDisplayDevices();
				_map.Refresh();

				IList<DisplayDevice> devices = _displayDevices.Items;
				if (_selectedDevice < 0 || _selectedDevice >= devices.Count || !devices[_selectedDevice].IsActive)
				{
					// start with the main display
					_selectedDevice = Math.Max(0, IndexOf(devices, d => d.IsActive && d.IsPrimary));
				}

				ShowSelected();

				List<string> inactive = devices.Where(d => !d.IsActive).Select(d => string.IsNullOrEmpty(d.FriendlyName) ? d.SourceName : d.FriendlyName).ToList();
				_inactiveHeader.Visible = inactive.Count > 0;
				_inactive.Visible = inactive.Count > 0;
				_inactive.Text = string.Join(Environment.NewLine, inactive);
			}
			catch (Exception ex)
			{
				// Shouldn't get here, but just in case
				_errorBar.Text = ex.Message;
				_errorBar.Visible = true;
			}
		}

		static int IndexOf(IList<DisplayDevice> devices, Func<DisplayDevice, bool> predicate)
		{
			for (int i = 0; i < devices.Count; i++)
			{
				if (predicate(devices[i]))
				{
					return i;
				}
			}

			return -1;
		}

		void Map_MonitorClicked(object sender, int monitorIndex)
		{
			// the map uses DMT's monitor order, the display devices may be in a different order
			System.Drawing.Rectangle bounds = Monitor.AllMonitors[monitorIndex].Bounds;
			int device = IndexOf(_displayDevices.Items, d => d.IsActive && d.Bounds == bounds);
			if (device >= 0)
			{
				_selectedDevice = device;
				ShowSelected();
			}
		}

		void ShowSelected()
		{
			IList<DisplayDevice> devices = _displayDevices.Items;
			if (_selectedDevice < 0 || _selectedDevice >= devices.Count)
			{
				return;
			}

			DisplayDevice device = devices[_selectedDevice];

			// highlight it on the map
			int mapIndex = Monitor.AllMonitors.FindIndex(m => m.Bounds == device.Bounds);
			_map.Selected = mapIndex >= 0 ? new List<int> { mapIndex } : new List<int>();

			string name = device.GetMonitorName();
			string brand = string.IsNullOrEmpty(device.FriendlyName) ? device.SourceName : device.FriendlyName;
			_monitorCard.Title = string.Format("Screen {0} · {1}", mapIndex + 1, string.IsNullOrEmpty(name) ? brand : name);
			_monitorCard.Description = string.Format("{0} × {1}{2}", device.Bounds.Width, device.Bounds.Height, device.IsPrimary ? " · main display" : string.Empty);
			_makePrimary.Enabled = device.IsActive && !device.IsPrimary;
			_rename.Enabled = !string.IsNullOrEmpty(device.GetMonitorId());
			_rotate.Enabled = device.IsActive;

			NumericRange brightness = device.GetBrightness();
			_brightnessCard.Description = brightness.Editable ? brightness.GetCurDisplayableValue() : "This monitor doesn't let Windows change its brightness";
			_brightnessCard.Action.Enabled = device.IsActive && brightness.Editable;

			NumericRange contrast = device.GetContrast();
			_contrastCard.Description = contrast.Editable ? contrast.GetCurDisplayableValue() : "This monitor doesn't let Windows change its contrast";
			_contrastCard.Action.Enabled = device.IsActive && contrast.Editable;

			StringBuilder details = new StringBuilder();
			AppendDetail(details, "Brand", brand);
			AppendDetail(details, "Description", device.Description);
			AppendDetail(details, "Graphics card", device.GPU);
			AppendDetail(details, "Connection", device.OutputTechnology);
			AppendDetail(details, "Orientation", device.RotationDegrees + "°");
			AppendDetail(details, "Colour depth", device.BitsPerPixel + " bits per pixel");
			AppendDetail(details, "Area", string.Format("({0}, {1}) – ({2}, {3})", device.Bounds.Left, device.Bounds.Top, device.Bounds.Right - 1, device.Bounds.Bottom - 1));
			AppendDetail(details, "Working area", string.Format("({0}, {1}) – ({2}, {3})", device.WorkingArea.Left, device.WorkingArea.Top, device.WorkingArea.Right - 1, device.WorkingArea.Bottom - 1));
			AppendDetail(details, "Windows name", device.SourceName);
			_details.Text = details.ToString().TrimEnd();
		}

		static void AppendDetail(StringBuilder details, string label, string value)
		{
			if (!string.IsNullOrEmpty(value))
			{
				details.AppendLine(label + ":  " + value);
			}
		}
	}
}
