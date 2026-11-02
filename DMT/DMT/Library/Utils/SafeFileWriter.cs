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

namespace DMT.Library.Utils
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	/// <summary>
	/// Helper class to write files safely with a backup
	/// </summary>
	class SafeFileWriter
	{
		string _filename;

		/// <summary>
		/// Initialises a new instance of the <see cref="SafeFileWriter" /> class.
		/// </summary>
		/// <param name="filename">Filename to write to</param>
		public SafeFileWriter(string filename)
		{
			_filename = filename;
		}

		/// <summary>
		/// Opens the file for writing
		/// </summary>
		/// <returns>The stream we can write to</returns>
		public Stream OpenForWriting()
		{
			// write through so the data is on disk before we swap the files over
			return new FileStream(GetTempFilename(), FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
		}

		/// <summary>
		/// Finishes the process of writing to the file
		/// </summary>
		public void CompleteWrite()
		{
			// assumes stream returned by OpenForWriting() has been closed
			string tempFilename = GetTempFilename();

			if (File.Exists(_filename))
			{
				// swap the new file in and keep the old one as the backup in a single operation,
				// so there is never a moment where neither file exists
				File.Replace(tempFilename, _filename, GetBackupFilename());
			}
			else
			{
				File.Move(tempFilename, _filename);
			}
		}

		/// <summary>
		/// Gets the name of the backup file used for the specified file
		/// </summary>
		/// <param name="filename">Filename being written</param>
		/// <returns>Backup filename</returns>
		public static string GetBackupFilename(string filename)
		{
			return Path.ChangeExtension(filename, "bak");
		}

		string GetTempFilename()
		{
			return Path.ChangeExtension(_filename, "tmp");
		}

		string GetBackupFilename()
		{
			return GetBackupFilename(_filename);
		}
	}
}
