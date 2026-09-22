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
	/// Allows the serialisation of multiple fields into a single string.
	/// The separator character used between the fields in the serialised string,
	/// together with an escape character to allow fields to contain both the
	/// separator and escape characters needs to be specified in the ctor.
	/// 
	/// Use FieldDeserialiser to extract the fields back out of the serialised string.
	/// </summary>
	public class FieldSerialiser
	{
		char _escapeChar;
		char _separatorChar;
		StringBuilder _sb;

		public FieldSerialiser(char escapeChar, char separatorChar)
		{
			_escapeChar = escapeChar;
			_separatorChar = separatorChar;
			_sb = new StringBuilder();
		}

		public void AddField(string field)
		{
			foreach (char ch in field)
			{
				if (ch == _escapeChar || ch == _separatorChar)
				{
					// must escape this char before adding it
					_sb.Append(_escapeChar);
				}

				// add the char itself
				_sb.Append(ch);
			}

			// always add the separator after adding the field,
			// so we can distinguish between an empty final field and a missing field
			_sb.Append(_separatorChar);
		}

		public override string ToString()
		{
			return _sb.ToString();
		}
	}
}
