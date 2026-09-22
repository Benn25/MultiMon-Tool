#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2025  Gerald Evans
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
// along with this program.  If not, see <http://www.gnu.org/licenses/>
#endregion

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DMT.Library.Environment
{
	/// <summary>
	/// Utility class to add DMT commands to the desktop context menu
	/// 
	/// Note: we use HKEY_CURRENT_USER so admin privileges are not required.
	/// </summary>
	public static class DesktopContextMenu
	{
		const string ContextMenuRootKey = @"Software\Classes\directory\Background\shell";

		const string DMTKeyName = "GNE_DualMonitorTools";
		const string DMTValue = "Dual Monitor Tools";

		const string iconName = "icon";
		const string shellKeyName = "shell";

		const string flagsName = "CommandFlags";
		const uint flagsBreakBefore = 0x20;

		const string optionsKeyName = @"shell\a_Options";
		const string optionsValue = "Options...";
		const string optionsCommand = "DMT:General:Options";

		const string magicWordKeyName = @"shell\b_MagicWord";
		const string magicWordValue = "Magic word...";
		const string magicWordCommand = "DMT:Launcher:Activate";

		const string addMagicWordKeyName = @"shell\c_AddMagicWord";
		const string addMagicWordValue = "Add magic word...";
		const string addMagicWordCommand = "DMT:Launcher:AddMagicWord";

		const string changePrimaryKeyName = @"shell\d_MakePrimary";
		const string changePrimaryValue = "Make primary";
		const string changePrimaryCommand = "DMT:General:ChangePrimary X";

		const string changeBrightnessKeyName = @"shell\e_ChangeBrightness";
		const string changeBrightnessValue = "Change brightness...";
		const string changeBrightnessCommand = "DMT:General:ChangeBrightness X";

		const string changeContrastKeyName = @"shell\f_ChangeContrast";
		const string changeContrastValue = "Change contrast...";
		const string changeContrastCommand = "DMT:General:ChangeContrast X";

		const string changeOrientationKeyName = @"shell\g_ChangeOrientation";
		const string changeOrientationValue = "Change orientation...";
		const string changeOrientationCommand = "DMT:General:ChangeOrientation X";

		const string minimiseAllKeyName = @"shell\h_MinimizeAll";
		const string minimiseAllValue = "Minimize all on screen";
		const string minimiseAllCommand = "DMT:SwapScreen:ShowDesktop X";

		const string moveAllKeyName = @"shell\i_MoveAll";
		const string moveAllValue = "Move all windows to this screen";
		const string moveAllCommand = "DMT:SwapScreen:MoveAllWindowsTo X";

		const string changeWallpaperKeyName = @"shell\j_ChangeWallpaper";
		const string changeWallpaperValue = "Change wallpaper";
		const string changeWallpaperCommand = "DMT:WallpaperChanger:ChangeWallpaper";

		const string cursorKeyName = @"shell\j_Cursor";
		const string cursorValue = "Cursor";

		const string cursorFreeKeyName = @"shell\a_free";
		const string cursorFreeValue = "Free";
		const string cursorFreeCommand = "DMT:Cursor:FreeCursor";

		const string cursorStickyKeyName = @"shell\b_sticky";
		const string cursorStickyValue = "Sticky";
		const string cursorStickyCommand = "DMT:Cursor:StickyCursor";

		const string cursorLockedKeyName = @"shell\c_locked";
		const string cursorLockedValue = "Locked";
		const string cursorLockedCommand = "DMT:Cursor:LockCursor";

		const string commandKeyName = "command";

		const string MUIVerbName = "MUIVerb";
		const string PositionName = "Position";
		const string PositionValue = "Bottom";
		const string SubCommandsName = "subCommands";

		public static bool IsActive()
		{
			bool ret = false;

			// could merge both of these opens into a single path
			using (RegistryKey contextMenuRootKey = Registry.CurrentUser.OpenSubKey(ContextMenuRootKey))
			{
				if (contextMenuRootKey != null)
				{
					using (RegistryKey key = contextMenuRootKey.OpenSubKey(DMTKeyName))
					{
						if (key != null)
						{
							ret = true;
						}
					}
				}
			}

			return ret;
		}

		public static bool Add()
		{
			bool ret = false;

			using (RegistryKey contextMenuRootKey = Registry.CurrentUser.CreateSubKey(ContextMenuRootKey))
			{
				if (contextMenuRootKey != null)
				{
					using (RegistryKey rootKey = contextMenuRootKey.CreateSubKey(DMTKeyName))
					{
						rootKey.SetValue(MUIVerbName, DMTValue);
						rootKey.SetValue(iconName, Application.ExecutablePath);
						rootKey.SetValue(PositionName, PositionValue);
						rootKey.SetValue(SubCommandsName, "");

						// "Options"
						AddSimpleCommand(rootKey, optionsKeyName, optionsValue, optionsCommand);

						// "Magic Word"
						AddSimpleCommand(rootKey, magicWordKeyName, magicWordValue, magicWordCommand);

						// "Add Magic Word"
						AddSimpleCommand(rootKey, addMagicWordKeyName, addMagicWordValue, addMagicWordCommand);


						// "Make Primary"
						AddSimpleCommand(rootKey, changePrimaryKeyName, changePrimaryValue, changePrimaryCommand, true);

						// "Change Brightness"
						AddSimpleCommand(rootKey, changeBrightnessKeyName, changeBrightnessValue, changeBrightnessCommand);

						// "Change Contrast"
						AddSimpleCommand(rootKey, changeContrastKeyName, changeContrastValue, changeContrastCommand);

						// "Change Orientation"
						AddSimpleCommand(rootKey, changeOrientationKeyName, changeOrientationValue, changeOrientationCommand);


						// "Minimize all on screen"
						AddSimpleCommand(rootKey, minimiseAllKeyName, minimiseAllValue, minimiseAllCommand, true);

						// "Move all windows to this screen"
						AddSimpleCommand(rootKey, moveAllKeyName, moveAllValue, moveAllCommand);


						// "Change wallpaper"
						AddSimpleCommand(rootKey, changeWallpaperKeyName, changeWallpaperValue, changeWallpaperCommand, true);

						// "cursor"
						using (RegistryKey cursorKey = rootKey.CreateSubKey(cursorKeyName))
						{
							cursorKey.SetValue(MUIVerbName, cursorValue);
							cursorKey.SetValue(SubCommandsName, "");

							// "Free"
							AddSimpleCommand(cursorKey, cursorFreeKeyName, cursorFreeValue, cursorFreeCommand);

							// "Sticky"
							AddSimpleCommand(cursorKey, cursorStickyKeyName, cursorStickyValue, cursorStickyCommand);

							// "Locked"
							AddSimpleCommand(cursorKey, cursorLockedKeyName, cursorLockedValue, cursorLockedCommand);
						}

						ret = true;
					}
				}
			}
			return ret;
		}

		public static void Remove()
		{
			using (RegistryKey contextMenuRootKey = Registry.CurrentUser.OpenSubKey(ContextMenuRootKey, true))
			{
				if (contextMenuRootKey != null)
				{
					contextMenuRootKey.DeleteSubKeyTree(DMTKeyName, false);
				}
			}
		}

		static void AddSimpleCommand(RegistryKey rootKey, string keyName, string keyValue, string dmtCmd, bool breakBefore=false)
		{
			using (RegistryKey key = rootKey.CreateSubKey(keyName))
			{
				key.SetValue("", keyValue);

				if (breakBefore)
				{
					key.SetValue(flagsName, flagsBreakBefore, RegistryValueKind.DWord);
				}

				using (RegistryKey commandKey = key.CreateSubKey(commandKeyName))
				{
					string cmd = MakeCommand(dmtCmd);
					commandKey.SetValue("", cmd);


				}
			}
		}

		static string MakeCommand(string dmtCmd)
		{
			StringBuilder sb = new StringBuilder();

			// get the location of the exe
			sb.Append("\"");
			sb.Append(Application.ExecutablePath);
			sb.Append("\"");

			// and add the DMT command
			sb.Append(" \"");
			sb.Append(dmtCmd);
			sb.Append("\"");

			return sb.ToString();
		}
	}
}
