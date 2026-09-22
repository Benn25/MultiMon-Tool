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
	/// Simple parser to parse a list of fields from a single string 
	/// using given list of field separators
	/// 
	/// If any of the field separator characters are required within the fields
	/// then they can either be escaped with '\', or the field can be enclosed in "..."
	/// 
	/// Note: unlike FieldSerialiser/FieldDeserialiser which are intended
	/// for software generated lists of fields, this is intended for
	/// handling user input.
	/// 
	/// Currently tries to make sense of any syntax errors
	/// but we could easily return an error text/position instead if required
	/// </summary>
	public class ListParser
	{
		char[] _separators;

		public List<string> Fields { get; protected set;  }
		public string LastErrorMsg { get; protected set; }
		public int LastErrorPosn { get; protected set; }

		public static string ListToString(List<string> fields, char[] separatorChars, string separator)
		{
			StringBuilder sb = new StringBuilder();

			foreach (string field in fields)
			{
				// TODO: may be worth having an option to decide what to do with empty fields
				// At the moment, we just ignore empty fields 
				if (field.Length > 0)
				{
					if (sb.Length > 0)
					{
						sb.Append(separator);
					}

					// if the field contins separator chars, then we must quote it
					bool needQuotes = ContainsSeparator(field, separatorChars);
					if (needQuotes)
					{
						sb.Append('"');
					}
					foreach (char ch in field)
					{
						if (ch == '"')
						{
							sb.Append('\\');
						}
						sb.Append(ch);
					}
					if (needQuotes)
					{
						sb.Append('"');
					}
				}
			}
			return sb.ToString();
		}

		static bool ContainsSeparator(string field, char[] separatorChars)
		{
			foreach (char ch in field)
			{
				if (separatorChars.Contains(ch))
				{
					return true;
				}
			}

			return false;
		}

		public ListParser(char[] separators)
		{
			_separators = separators;
		}

		//public bool Parsex(string line)
		//{
		//	Clear();

		//	StringBuilder sb = new StringBuilder();

		//	int idx = 0;
		//	while (idx < line.Length)
		//	{
		//		// skip any leading separators
		//		while (IsSeparator(line[idx]) && idx < line.Length)
		//		{
		//			idx++;
		//		}

		//		if (idx < line.Length)
		//		{
		//			char ch = line[idx];
		//			idx++;
		//			if (ch == '"')
		//			{
		//				// double quoted field
		//				// - take everything up to the next double quote
		//				while (idx < line.Length)
		//				{
		//					ch = line[idx];
		//					idx++;
		//					if (ch == '"')
		//					{
		//						// end of field
		//						Fields.Add(sb.ToString());
		//						sb.Clear();
		//						// leave idx at the finishing "
		//						break;
		//					}
		//					else if (ch == '\\')
		//					{
		//						if (idx < line.Length)
		//						{
		//							ch = line[idx];
		//							idx++;
		//							sb.Append(ch);
		//						}
		//					}
		//					else
		//					{
		//						sb.Append(ch);
		//					}
		//				}
		//				if (sb.Length > 0)
		//				{
		//					// missing end double quote.
		//					// - we can safely ignore this error
		//					Fields.Add(sb.ToString());
		//					sb.Clear();
		//				}
		//			}
		//			else if (IsSeparator(ch))
		//			{
		//				if (sb.Length > 0)
		//				{
		//					Fields.Add(sb.ToString());
		//					sb.Clear();
		//				}
		//			}
		//			else if (ch == '\\')
		//			{
		//				if (idx < line.Length)
		//				{
		//					ch = line[idx];
		//					idx++;
		//					sb.Append(ch);
		//				}
		//			}
		//			else
		//			{
		//				sb.Append(ch);
		//			}
		//		}
		//	}

		//	return true;
		//}

		public bool Parse(string line)
		{
			Clear();

			StringBuilder sb = new StringBuilder();
			bool inDoubleQuotes = false;

			int idx = 0;
			while (idx < line.Length)
			{
				if (sb.Length == 0)
				{
					// skip any leading separators
					while (IsSeparator(line[idx]) && idx < line.Length)
					{
						idx++;
					}
				}

				if (idx < line.Length)
				{
					char ch = line[idx];
					idx++;
					if (ch == '\\')
					{
						// skip this and add the next char
						if (idx < line.Length)
						{
							ch = line[idx];
							idx++;
							sb.Append(ch);
						}
					}
					else if (ch == '"')
					{
						if (inDoubleQuotes)
						{
							// end of a double quoted field
							// always add, even if there is nothing between the double quotes
							Fields.Add(sb.ToString());
							sb.Clear();
							inDoubleQuotes = false;
							// we don't check if this is followed by a separator
						}
						else if (sb.Length == 0)
						{
							// start of a double quoted field
							inDoubleQuotes = true;
						}
						else
						{
							// double quotes within a field
							// strictly an error, but we treat it as part of the field
							sb.Append(ch);
						}
					}
					else if (!inDoubleQuotes && IsSeparator(ch))
					{
						if (sb.Length > 0)
						{
							Fields.Add(sb.ToString());
							sb.Clear();
						}
					}
					else
					{
						sb.Append(ch);
					}
				}
			}

			// add any final field
			if (sb.Length > 0)
			{
				Fields.Add(sb.ToString());
				//sb.Clear();
			}


			return true;
		}

		void Clear()
		{
			Fields = new List<string>();
			LastErrorMsg = "";
			LastErrorPosn = -1;
		}

		bool IsSeparator(char ch)
		{
			return _separators.Contains(ch);
		}
	}
}
