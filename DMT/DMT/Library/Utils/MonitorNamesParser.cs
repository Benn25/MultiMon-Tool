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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DMT.Library.Utils
{
	/// <summary>
	/// Converts between a single string listing the monitor names (as specified by the user)
	/// and a list of individual monitor names.
	/// </summary>
	public static class MonitorNamesParser
	{
		// allowed input separators
		static readonly char[] MonitorNameSeparatorChars = { ' ', ',' };

		// separator used on output
		const string MonitorNameSeparator = ", ";

		public static string ListToString(List<string> fields)
		{
			return ListParser.ListToString(fields, MonitorNameSeparatorChars, MonitorNameSeparator);
		}

		public static string[] StringToList(string setting)
		{
			ListParser parser = new ListParser(MonitorNameSeparatorChars);

			if (parser.Parse(setting))
			{
				return parser.Fields.ToArray();
			}

			return new string[0];
		}
	}
}
