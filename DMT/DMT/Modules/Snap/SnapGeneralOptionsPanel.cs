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
using System.Windows.Forms;
using DMT.Library.Environment;
using DMT.Library.Ui;

namespace DMT.Modules.Snap
{
	/// <summary>
	/// Settings for taking and showing snapshots
	/// </summary>
	class SnapGeneralOptionsPanel : SettingsPage
	{
		SnapModule _snapModule;

		public SnapGeneralOptionsPanel(SnapModule snapModule)
		{
			_snapModule = snapModule;

			AddText("Take a picture of a screen or window and show it on another screen, e.g. to keep a reference in view while you work.");

			AddHeader("Shortcuts");
			this.AddHotKey(_snapModule.TakeScreenSnapHotKeyController, null, "Snaps the main display", Theme.GlyphCamera);
			this.AddHotKey(_snapModule.TakeWindowSnapHotKeyController, null, null, Theme.GlyphTaskView);
			this.AddHotKey(_snapModule.ShowSnapHotKeyController, null, null, Theme.GlyphPhoto);

			AddHeader("Showing snaps");
			NumericUpDown monitor = new NumericUpDown
			{
				Minimum = 1,
				Maximum = Math.Max(1, Monitor.AllMonitors.Count),
				AccessibleName = "Show snap on monitor",
			}.WithLogicalWidth(80);
			monitor.Value = Math.Max(monitor.Minimum, Math.Min(monitor.Maximum, _snapModule.ShowSnapsOn));
			monitor.ValueChanged += (s, e) => _snapModule.ShowSnapsOn = (int)monitor.Value;
			AddCard("Show snaps on screen", null, Theme.GlyphMonitor, monitor);

			AddToggle("Show the snap as soon as it's taken", null, "", _snapModule.AutoShowSnap, v => _snapModule.AutoShowSnap = v);
			AddToggle("Enlarge small snaps to fill the screen", null, Theme.GlyphFullScreen, _snapModule.ExpandSnap, v => _snapModule.ExpandSnap = v);
			AddToggle("Shrink large snaps to fit the screen", null, "", _snapModule.ShrinkSnap, v => _snapModule.ShrinkSnap = v);
			AddToggle("Keep the aspect ratio", "When enlarging or shrinking a snap", Theme.GlyphFrame, _snapModule.MaintainAspectRatio, v => _snapModule.MaintainAspectRatio = v);

			AddHeader("History");
			NumericUpDown snaps = new NumericUpDown
			{
				Minimum = 1,
				Maximum = 100,
				AccessibleName = "Snaps to remember",
			}.WithLogicalWidth(80);
			snaps.Value = Math.Max(snaps.Minimum, Math.Min(snaps.Maximum, _snapModule.MaxSnaps));
			snaps.ValueChanged += (s, e) => _snapModule.MaxSnaps = (int)snaps.Value;
			AddCard("Snaps to remember", "Older snaps can be viewed, saved or copied from the snap window", Theme.GlyphList, snaps);
		}
	}
}
