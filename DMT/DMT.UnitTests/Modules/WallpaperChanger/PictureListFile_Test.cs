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

using NUnit.Framework;

using DMT.Modules.WallpaperChanger.Plugins.List;

namespace DMT.UnitTests.Modules.WallpaperChanger
{
	[TestFixture]
	public class PictureListFile_Test
	{
		[TestCase("")]
		[TestCase("   ")]
		[TestCase("# a comment")]
		[TestCase("  # indented comment")]
		public void IgnoresBlankAndCommentLines(string line)
		{
			PictureListEntry entry;
			Assert.IsFalse(PictureListFile.TryParse(line, out entry));
		}

		[Test]
		public void ReadsPlainPath()
		{
			PictureListEntry entry;
			Assert.IsTrue(PictureListFile.TryParse(@"C:\Pictures\beach.jpg", out entry));
			Assert.AreEqual(@"C:\Pictures\beach.jpg", entry.Path);
			Assert.IsNull(entry.Focus);
		}

		[Test]
		public void ReadsFocus()
		{
			PictureListEntry entry;
			Assert.IsTrue(PictureListFile.TryParse("C:\\Pictures\\beach.jpg\tfocus=0.35", out entry));
			Assert.AreEqual(@"C:\Pictures\beach.jpg", entry.Path);
			Assert.AreEqual(0.35f, entry.Focus.Value, 0.0001f);
		}

		[TestCase("focus=-2", 0f)]
		[TestCase("focus=7", 1f)]
		public void ClampsFocus(string setting, float expected)
		{
			PictureListEntry entry;
			Assert.IsTrue(PictureListFile.TryParse("a.jpg\t" + setting, out entry));
			Assert.AreEqual(expected, entry.Focus.Value);
		}

		[Test]
		public void IgnoresBadFocus()
		{
			PictureListEntry entry;
			Assert.IsTrue(PictureListFile.TryParse("a.jpg\tfocus=abc", out entry));
			Assert.IsNull(entry.Focus);
		}

		[TestCase(null)]
		[TestCase(0.125f)]
		[TestCase(1f)]
		public void FormatRoundTrips(float? focus)
		{
			PictureListEntry entry;
			string line = PictureListFile.Format(new PictureListEntry(@"D:\x y\pic.png", focus));
			Assert.IsTrue(PictureListFile.TryParse(line, out entry));
			Assert.AreEqual(@"D:\x y\pic.png", entry.Path);
			Assert.AreEqual(focus, entry.Focus);
		}
	}
}
