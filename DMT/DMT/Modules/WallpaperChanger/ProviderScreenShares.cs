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

namespace DMT.Modules.WallpaperChanger
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;

	using DMT.Library.WallpaperPlugin;

	/// <summary>
	/// How much each wallpaper source is used on each screen, as a percentage of its weight.
	/// This works for every kind of source, so it's kept alongside the source rather than in its own settings.
	/// Screens without a value use 100%.
	/// </summary>
	static class ProviderScreenShares
	{
		static readonly ConditionalWeakTable<IImageProvider, int[]> _shares = new ConditionalWeakTable<IImageProvider, int[]>();

		/// <summary>
		/// Gets the percentage (0 to 100) of the source's weight used on the screen
		/// </summary>
		public static int Get(IImageProvider provider, int screenIndex)
		{
			int[] shares;
			if (screenIndex >= 0 && _shares.TryGetValue(provider, out shares) && screenIndex < shares.Length)
			{
				return shares[screenIndex];
			}

			return 100;
		}

		public static void Set(IImageProvider provider, int screenIndex, int percent)
		{
			int[] shares;
			if (!_shares.TryGetValue(provider, out shares))
			{
				shares = new int[0];
			}

			if (screenIndex >= shares.Length)
			{
				int oldLength = shares.Length;
				Array.Resize(ref shares, screenIndex + 1);
				for (int i = oldLength; i < shares.Length; i++)
				{
					shares[i] = 100;
				}
			}

			shares[screenIndex] = Math.Max(0, Math.Min(100, percent));
			_shares.AddOrUpdate(provider, shares);
		}

		/// <summary>
		/// The shares as saved in the providers file, e.g. "100,0,50" (empty when every screen is 100%)
		/// </summary>
		public static string ToSetting(IImageProvider provider)
		{
			int[] shares;
			if (!_shares.TryGetValue(provider, out shares) || shares.All(s => s == 100))
			{
				return string.Empty;
			}

			return string.Join(",", shares);
		}

		public static void FromSetting(IImageProvider provider, string setting)
		{
			if (string.IsNullOrWhiteSpace(setting))
			{
				return;
			}

			string[] fields = setting.Split(',');
			for (int i = 0; i < fields.Length; i++)
			{
				int percent;
				if (int.TryParse(fields[i].Trim(), out percent))
				{
					Set(provider, i, percent);
				}
			}
		}

		/// <summary>
		/// A short description of the shares, e.g. "Screen 1: 100% · Screen 2: 0%", or "All screens"
		/// </summary>
		public static string Describe(IImageProvider provider, int screenCount)
		{
			List<int> shares = Enumerable.Range(0, screenCount).Select(i => Get(provider, i)).ToList();
			if (shares.All(s => s == 100))
			{
				return "All screens";
			}

			if (shares.All(s => s == 0))
			{
				return "No screens";
			}

			return string.Join("  ·  ", shares.Select((s, i) => string.Format("{0}: {1}%", i + 1, s)));
		}
	}
}
