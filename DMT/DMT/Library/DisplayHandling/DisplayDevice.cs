#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2016-2026  Gerald Evans
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

using DMT.Library.PInvoke;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace DMT.Library.DisplayHandling
{
	public class DisplayDevice
	{
		// links to where this information came from
		public int PathIndex { get; private set; }
		public IntPtr MonitorHandle { get; set; }

		public NativeDisplayMethods.LUID AdapterId { get; private set; }
		public uint OutletId { get; private set; }	// connector/outlet on above adapter

		public bool IsActive { get; set; }
		public bool IsPrimary { get; set; }

		//public string AdapterName { get; set; }
		public string SourceName { get; set; }
		public string FriendlyName { get; set; }
		public string DeviceName { get; set; }	// from virtual monitor
		public string Description { get; set; }	// from physical monitor

		public string GPU { get; set; }

		public WindowsDevicePath DevicePath { get; set; }

		//public int OutputTechnology { get; set; }
		//public int Rotation { get; set; }
		public string OutputTechnology { get; set; }
		public int RotationDegrees { get; set; }

		public Rectangle Bounds { get; set; }
		public Rectangle WorkingArea { get; set; }

		public int BitsPerPixel { get; set; }

		//public uint MinBrightness { get; set; }
		//public uint MaxBrightness { get; set; }
		//public uint CurBrightness { get; set; }
		//public NumericRange Brightness { get; private set; }

		//public uint MinContrast { get; set; }
		//public uint MaxContrast { get; set; }
		//public uint CurContrast { get; set; }
		//public NumericRange Contrast { get; private set; }

		public int DeviceIndex { get; set; }
		public int ActiveDeviceIndex { get; set; }



		public DisplayDevice(int pathIndex, NativeDisplayMethods.LUID adapterId, uint outletId)
		{
			PathIndex = pathIndex;
			AdapterId = adapterId;
			OutletId = outletId;

			//Brightness = new NumericRange();
			//Contrast = new NumericRange();
		}

		public string GetMonitorId()
		{
			if (DevicePath != null)
			{
				return DevicePath.ToString();
			}

			return "";
		}

		public string GetMonitorName()
		{
			string monitorName = "";
			string monitorId = GetMonitorId();
			if (!string.IsNullOrEmpty(monitorId))
			{
				MonitorNameIdMap monitorNames = DMT.Library.Environment.Monitor.MonitorNames;
				if (monitorNames != null)
				{
					monitorName = monitorNames.GetName(monitorId);
				}
			}

			return monitorName;
		}

		public NumericRange GetBrightness()
		{
			NumericRange brightness = new NumericRange();

			using (PhysicalMonitors physicalMonitors = new PhysicalMonitors(MonitorHandle))
			{
				IntPtr hPhysicalMonitor;
				if (physicalMonitors.GetFirstPhysicalMonitorHandle(out hPhysicalMonitor))
				{
					uint minBrightness;
					uint maxBrightness;
					uint curBrightness;
					NativeMethods.GetMonitorBrightness(hPhysicalMonitor, out minBrightness, out curBrightness, out maxBrightness);
					brightness.SetValues((int)minBrightness, (int)curBrightness, (int)maxBrightness);
				}
			}

			return brightness;
		}

		public void SetBrightness(int value, bool isDelta)
		{
			NumericRange brightness = new NumericRange();

			using (PhysicalMonitors physicalMonitors = new PhysicalMonitors(MonitorHandle))
			{
				IntPtr hPhysicalMonitor;
				if (physicalMonitors.GetFirstPhysicalMonitorHandle(out hPhysicalMonitor))
				{
					uint minBrightness;
					uint maxBrightness;
					uint curBrightness;
					NativeMethods.GetMonitorBrightness(hPhysicalMonitor, out minBrightness, out curBrightness, out maxBrightness);
					brightness.SetValues((int)minBrightness, (int)curBrightness, (int)maxBrightness);

					if (brightness.Editable)
					{
						int absoluteBrightness = brightness.GetAbsoluteValue(value, isDelta);
						NativeDisplayMethods.SetMonitorBrightness(hPhysicalMonitor, (uint)absoluteBrightness);
					}
				}
			}
		}

		public NumericRange GetContrast()
		{
			NumericRange contrast = new NumericRange();

			using (PhysicalMonitors physicalMonitors = new PhysicalMonitors(MonitorHandle))
			{
				IntPtr hPhysicalMonitor;
				if (physicalMonitors.GetFirstPhysicalMonitorHandle(out hPhysicalMonitor))
				{
					uint minContrast;
					uint maxContrast;
					uint curContrast;
					NativeMethods.GetMonitorContrast(hPhysicalMonitor, out minContrast, out curContrast, out maxContrast);
					contrast.SetValues((int)minContrast, (int)curContrast, (int)maxContrast);
				}
			}

			return contrast;
		}

		public void SetContrast(int value, bool isDelta)
		{
			NumericRange contrast = new NumericRange();

			using (PhysicalMonitors physicalMonitors = new PhysicalMonitors(MonitorHandle))
			{
				IntPtr hPhysicalMonitor;
				if (physicalMonitors.GetFirstPhysicalMonitorHandle(out hPhysicalMonitor))
				{
					uint minContrast;
					uint maxContrast;
					uint curContrast;
					NativeMethods.GetMonitorContrast(hPhysicalMonitor, out minContrast, out curContrast, out maxContrast);
					contrast.SetValues((int)minContrast, (int)curContrast, (int)maxContrast);

					if (contrast.Editable)
					{
						int absoluteContrast = contrast.GetAbsoluteValue(value, isDelta);
						NativeDisplayMethods.SetMonitorContrast(hPhysicalMonitor, (uint)absoluteContrast);
					}
				}
			}
		}
	}
}
