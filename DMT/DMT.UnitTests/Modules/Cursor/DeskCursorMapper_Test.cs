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
using System.Collections.Generic;
using System.Drawing;

using DMT.Modules.Cursor;

namespace DMT.UnitTests.Modules.Cursor
{
	[TestFixture]
	public class DeskCursorMapper_Test
	{
		// a 1000x500 pixel screen that is 500x250 mm (0.5 mm per pixel),
		// with a portrait screen to its right that's much denser (0.2 mm per pixel) and sits lower down on the desk
		static readonly Rectangle MainPixels = new Rectangle(0, 0, 1000, 500);
		static readonly RectangleF MainDesk = new RectangleF(0, 0, 500, 250);
		static readonly Rectangle RightPixels = new Rectangle(1000, -200, 500, 1000);
		static readonly RectangleF RightDesk = new RectangleF(500, 50, 100, 200);

		static DeskCursorMapper SideBySide()
		{
			return new DeskCursorMapper(new List<Rectangle> { MainPixels, RightPixels }, new List<RectangleF> { MainDesk, RightDesk });
		}

		[Test]
		public void NotMappedWhileOnTheSameScreen()
		{
			Point result;
			Assert.IsFalse(SideBySide().TryMap(0, new Point(10, 10), out result));
		}

		[Test]
		public void CrossesAtTheSamePhysicalHeight()
		{
			// halfway down the main screen is 125 mm down the desk, which is 37.6% of the way down the right screen
			Point result;
			Assert.IsTrue(SideBySide().TryMap(0, new Point(1000, 250), out result));
			Assert.AreEqual(new Point(1000, -200 + 376), result);
		}

		[Test]
		public void CrossesBackToTheSamePlace()
		{
			Point result;
			Assert.IsTrue(SideBySide().TryMap(1, new Point(999, 176), out result));
			Assert.AreEqual(new Point(999, 250), result);
		}

		[Test]
		public void AboveTheOtherScreenGoesToItsTop()
		{
			Point result;
			Assert.IsTrue(SideBySide().TryMap(0, new Point(1000, 0), out result));
			Assert.AreEqual(new Point(1000, -200), result);
		}

		[Test]
		public void BottomEdgeLinesUp()
		{
			// both screens end at 250 mm, so the bottom row of one is next to the bottom row of the other
			Point result;
			Assert.IsTrue(SideBySide().TryMap(0, new Point(1000, 499), out result));
			Assert.AreEqual(new Point(1000, -200 + 998), result);
		}

		[Test]
		public void KeepsHowFarItWentPastTheEdge()
		{
			Point result;
			Assert.IsTrue(SideBySide().TryMap(0, new Point(1003, 250), out result));
			Assert.AreEqual(1003, result.X);
		}

		[Test]
		public void NoScreenBeyondThatEdge()
		{
			Point result;
			Assert.IsFalse(SideBySide().TryMap(0, new Point(-1, 250), out result));
			Assert.IsFalse(SideBySide().TryMap(1, new Point(1500, 250), out result));
		}

		[Test]
		public void CrossesDownToAScreenBelow()
		{
			DeskCursorMapper mapper = new DeskCursorMapper(
				new List<Rectangle> { MainPixels, new Rectangle(0, 500, 2000, 1000) },
				new List<RectangleF> { MainDesk, new RectangleF(100, 250, 300, 150) });

			// halfway across the main screen is 250 mm across the desk, just over halfway across the screen below
			Point result;
			Assert.IsTrue(mapper.TryMap(0, new Point(500, 500), out result));
			Assert.AreEqual(new Point(1001, 500), result);
		}

		[Test]
		public void PicksTheScreenNearestTheExit()
		{
			// two screens stacked on the right
			DeskCursorMapper mapper = new DeskCursorMapper(
				new List<Rectangle> { MainPixels, new Rectangle(1000, 0, 500, 250), new Rectangle(1000, 250, 500, 250) },
				new List<RectangleF> { MainDesk, new RectangleF(500, 0, 250, 125), new RectangleF(500, 125, 250, 125) });

			Point result;
			Assert.IsTrue(mapper.TryMap(0, new Point(1000, 400), out result));
			Assert.AreEqual(1000, result.X);
			Assert.That(result.Y, Is.InRange(250, 499));
		}

		[Test]
		public void ReadsSizeFromEdidTiming()
		{
			byte[] edid = new byte[128];
			edid[21] = 70;
			edid[22] = 39;
			edid[54] = 1;
			edid[66] = 698 & 0xFF;
			edid[67] = 393 & 0xFF;
			edid[68] = (byte)(((698 >> 8) << 4) | (393 >> 8));
			Assert.AreEqual(new SizeF(698, 393), DeskLayout.ParseEdidSize(edid));
		}

		[Test]
		public void FallsBackToEdidCentimetres()
		{
			byte[] edid = new byte[128];
			edid[21] = 34;
			edid[22] = 22;
			Assert.AreEqual(new SizeF(340, 220), DeskLayout.ParseEdidSize(edid));
		}

		[Test]
		public void NoSizeInEdid()
		{
			Assert.AreEqual(SizeF.Empty, DeskLayout.ParseEdidSize(new byte[128]));
			Assert.AreEqual(SizeF.Empty, DeskLayout.ParseEdidSize(null));
		}

		[Test]
		public void StartsArrangedLikeWindows()
		{
			// a 31.5" 1440p screen with a 16" portrait screen to its right, raised up a little in Windows
			DeskScreen main = new DeskScreen { Id = "main", Pixels = new Rectangle(0, 0, 2560, 1440), ReportedMm = new SizeF(698, 393) };
			DeskScreen portrait = new DeskScreen { Id = "portrait", Pixels = new Rectangle(2560, -941, 1600, 2560), ReportedMm = new SizeF(215, 345) };
			DeskLayout layout = new DeskLayout(new List<DeskScreen> { main, portrait });

			layout.ArrangeLikeWindows();

			// side by side, touching
			Assert.AreEqual(main.DeskRect.Right, portrait.PositionMm.X, 0.01f);

			// and offset by the same distance (measured on the main screen) as Windows has them
			float windowsOffset = (720 - (-941 + 1280)) * (698f / 2560);
			float deskOffset = (main.DeskRect.Top + main.DeskRect.Height / 2) - (portrait.DeskRect.Top + portrait.DeskRect.Height / 2);
			Assert.AreEqual(windowsOffset, deskOffset, 0.5f);
			Assert.AreEqual(0, System.Math.Min(main.PositionMm.Y, portrait.PositionMm.Y), 0.01f);
		}

		[Test]
		public void SettingRoundTrips()
		{
			DeskScreen screen = new DeskScreen { Id = @"IVM7608\5&9c373da&0&UID4353", Pixels = new Rectangle(0, 0, 2560, 1440), DiagonalOverride = 31.5f, PositionMm = new PointF(12.5f, 80) };
			string setting = new DeskLayout(new List<DeskScreen> { screen }).ToSetting("other|0|1|2");
			StringAssert.Contains("other|0|1|2", setting);
			StringAssert.Contains(@"IVM7608\5&9c373da&0&UID4353|31.5|12.5|80", setting);
		}
	}
}
