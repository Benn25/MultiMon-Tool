#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2026 Gerald Evans
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
using System.Linq;
using System.Text;

namespace DMT.Library.DisplayHandling
{
	/// <summary>
	/// A representation of the device instance path that Windows uses to
	/// uniquely identify a monitor.
	/// </summary>
	public class WindowsDevicePath
	{
		string _deviceId;

		public WindowsDevicePath(string devicePath)
		{
			_deviceId = FromDevivePath(devicePath);
		}

		public override string ToString()
		{
			return _deviceId;
		}

		public string ToWmiString()
		{
			// TODO: need a better solution for the instance index
			return @"DISPLAY\" + _deviceId + "_0";
		}

		static string FromDevivePath(string devicePath)
		{
			if (devicePath.IndexOf('#') >= 0)
			{
				return FromWin32DevicePath(devicePath);
			}

			return FromWmiDevicePath(devicePath);
		}

		static string FromWin32DevicePath(string devicePath)
		{
			// EnumDisplayDevices() returns a DeviceID of the form:
			// \\?\DISPLAY#ACR0523#4&8b3f87c&0&UID54926080#{GUID}

			string[] fields = devicePath.Split(new char[] { '#' });
			if (fields.Length >= 3)
			{
				// TODO: can these have an instance index?
				// could just add "_0" to this?
				return fields[1] + @"\" + fields[2];
			}

			// to help with debugging
			return devicePath;
		}

		static string FromWmiDevicePath(string devicePath)
		{
			// WMI returns an InstanceName of the form:
			// DISPLAY\ACR0523\4&8b3f87c&0&UID54926080_0

			string[] fields = devicePath.Split(new char[] { '\\', '_' });
			if (fields.Length >= 3)
			{
				// TODO: we may need the 3rd field at some point as this represents an instance index
				// which may not be zero in certian circumstances
				// (eg. ultra wide monitors being treated as dual displays)
				return fields[1] + @"\" + fields[2];
			}

			// to help with debugging
			return devicePath;
		}
	}
}
