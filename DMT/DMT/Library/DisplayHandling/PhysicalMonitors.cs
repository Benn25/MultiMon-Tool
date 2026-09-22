#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2025  Gerald Evans
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
using System.Linq;
using System.Text;

namespace DMT.Library.DisplayHandling
{
	/// <summary>
	/// Wraps NativeMethods.PHYSICAL_MONITOR[] into a disposable object
	/// 
	/// Note: these are the physical monitors for a single virtual monitor
	/// </summary>
	public class PhysicalMonitors : IDisposable
	{
		IntPtr _hMonitor;
		uint _numPhysicalMonitors;
		NativeMethods.PHYSICAL_MONITOR[] _physicalMonitors;

		public PhysicalMonitors(IntPtr hMonitor)
		{
			_hMonitor = hMonitor;
			_numPhysicalMonitors = 0;
			_physicalMonitors = null;
		}

		~PhysicalMonitors()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (disposing)
			{
				// no managed objects to dispose
			}

			if (_physicalMonitors != null)
			{
				NativeMethods.DestroyPhysicalMonitors(_numPhysicalMonitors, _physicalMonitors);
				_physicalMonitors = null;
			}
		}


		public bool GetFirstPhysicalMonitorHandle(out IntPtr hPhysicalMonitor)
		{
			GetPhysicalMonitors();

			if (_physicalMonitors != null)
			{
				hPhysicalMonitor = _physicalMonitors[0].hPhysicalMonitor;
				return true;
			}

			hPhysicalMonitor = System.IntPtr.Zero;
			return false;
		}

		void GetPhysicalMonitors()
		{
			// make sure we have loaded the physical monitors
			if (_physicalMonitors == null)
			{
				NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(_hMonitor, ref _numPhysicalMonitors);

				if (_numPhysicalMonitors >= 1)
				{
					_physicalMonitors = new NativeMethods.PHYSICAL_MONITOR[_numPhysicalMonitors];
					if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(_hMonitor, _numPhysicalMonitors, _physicalMonitors))
					{
						_physicalMonitors = null;
					}
				}
			}
		}
	}
}
