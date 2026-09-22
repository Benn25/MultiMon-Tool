#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2026  Gerald Evans
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

using DMT.Library.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DMT.Library.DisplayHandling
{
	/// <summary>
	/// Maps from a monitorId to the monitor's name
	/// </summary>
	public class MonitorNameIdMap
	{
		const char SerialisedEscapeChar = '\\';
		const char SerialisedSeparatorChar = '|';

		Dictionary<string, string> _nameMap;

		public MonitorNameIdMap()
		{
			_nameMap = new Dictionary<string, string>();
		}

		public void LoadNames(string serialisedNames)
		{
			FieldDeserialiser deserialiser = new FieldDeserialiser(serialisedNames, SerialisedEscapeChar, SerialisedSeparatorChar);

			_nameMap.Clear();

			string monitorId;
			string monitorName;

			while (deserialiser.GetNextField(out monitorId))
			{
				if (deserialiser.GetNextField(out monitorName))
				{
					_nameMap[monitorId] = monitorName;
				}
			}
		}

		public override string ToString()
		{
			FieldSerialiser serialiser = new FieldSerialiser(SerialisedEscapeChar, SerialisedSeparatorChar);

			foreach (KeyValuePair<string, string> monitor in _nameMap)
			{
				serialiser.AddField(monitor.Key);	// the monitorId
				serialiser.AddField(monitor.Value);	// the monitorName
			}

			return serialiser.ToString();
		}

		public string GetName(string monitorId)
		{
			if (_nameMap.ContainsKey(monitorId))
			{
				return _nameMap[monitorId];
			}

			return "";
		}

		public void SetName(string monitorId, string monitorName)
		{
			if (monitorName.Length == 0)
			{
				// we use this as a signal that the entry for this monitor is to be deleted
				if (_nameMap.ContainsKey(monitorId))
				{
					_nameMap.Remove(monitorId);
				}
			}
			else
			{
				_nameMap[monitorId] = monitorName;
			}
		}

		public List<string> GetAllNames()
		{
			List<string> allNames = new List<string>();

			foreach (string name in _nameMap.Values)
			{
				// don't expect allNames to be big
				if (!allNames.Contains(name))
				{
					allNames.Add(name);
				}
			}

			return allNames;
		}
	}
}
