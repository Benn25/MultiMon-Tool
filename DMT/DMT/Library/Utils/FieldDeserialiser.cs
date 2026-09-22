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
	/// Allows extraction of individual fields from a serialised string created
	/// by FieldSerialiser.
	/// </summary>
	public class FieldDeserialiser
	{
		string _serialisedString;
		int _idx;

		char _escapeChar;
		char _separatorChar;
		StringBuilder _sb;

		public FieldDeserialiser(string serialisedString, char escapeChar, char separatorChar)
		{
			_serialisedString = serialisedString;
			_idx = 0;

			_escapeChar = escapeChar;
			_separatorChar = separatorChar;
		}

		public bool GetNextField(out string field)
		{
			if (_idx >= _serialisedString.Length)
			{
				// no more fields to return
				field = "";
				return false;
			}

			StringBuilder sb = new StringBuilder();

			while (_idx < _serialisedString.Length)
			{
				if (_serialisedString[_idx] == _escapeChar)
				{
					// skip over the escape char
					_idx++;
					if (_idx < _serialisedString.Length)
					{
						sb.Append(_serialisedString[_idx]);
						_idx++;
					}
				}
				else if (_serialisedString[_idx] == _separatorChar)
				{
					// end of field
					_idx++;
					break;
				}
				else
				{
					// field character
					sb.Append(_serialisedString[_idx]);
					_idx++;
				}
			}

			field = sb.ToString();
			return true;
		}
	}
}
