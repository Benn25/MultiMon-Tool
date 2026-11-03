#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2026
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

namespace DMT.Modules.WallpaperChanger.Plugins.List
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;

	/// <summary>
	/// A picture in a picture list
	/// </summary>
	class PictureListEntry
	{
		public PictureListEntry(string path, float? focus = null)
		{
			Path = path;
			Focus = focus;
		}

		public string Path { get; private set; }

		/// <summary>
		/// Where along the image's longer side (0 to 1) should stay central when it's cropped to fill a screen.
		/// Null for the middle.
		/// </summary>
		public float? Focus { get; set; }
	}

	/// <summary>
	/// Reads and writes picture list files.
	/// Each line holds a file path or web address, optionally followed by a tab and settings such as "focus=0.35".
	/// Lines starting with # are comments.
	/// </summary>
	static class PictureListFile
	{
		const string FocusKey = "focus=";

		public static bool TryParse(string line, out PictureListEntry entry)
		{
			entry = null;
			string trimmed = line.Trim();
			if (trimmed.Length == 0 || trimmed[0] == '#')
			{
				return false;
			}

			string[] parts = trimmed.Split('\t');
			string path = parts[0].Trim();
			if (path.Length == 0)
			{
				return false;
			}

			float? focus = null;
			for (int i = 1; i < parts.Length; i++)
			{
				string part = parts[i].Trim();
				float value;
				if (part.StartsWith(FocusKey, StringComparison.OrdinalIgnoreCase)
					&& float.TryParse(part.Substring(FocusKey.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
				{
					focus = Math.Max(0f, Math.Min(1f, value));
				}
			}

			entry = new PictureListEntry(path, focus);
			return true;
		}

		public static string Format(PictureListEntry entry)
		{
			if (entry.Focus.HasValue)
			{
				return entry.Path + "\t" + FocusKey + entry.Focus.Value.ToString("0.###", CultureInfo.InvariantCulture);
			}

			return entry.Path;
		}

		public static List<PictureListEntry> Read(string filename)
		{
			List<PictureListEntry> entries = new List<PictureListEntry>();
			foreach (string line in File.ReadAllLines(filename))
			{
				PictureListEntry entry;
				if (TryParse(line, out entry))
				{
					entries.Add(entry);
				}
			}

			return entries;
		}

		public static void Write(string filename, IEnumerable<PictureListEntry> entries)
		{
			List<string> lines = new List<string> { "# Picture list for Dual Monitor Tools (one picture per line)" };
			foreach (PictureListEntry entry in entries)
			{
				lines.Add(Format(entry));
			}

			File.WriteAllLines(filename, lines);
		}
	}
}
