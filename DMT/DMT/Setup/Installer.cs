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

namespace DMT.Setup
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading;

	using DMT.Library.Environment;
	using DMT.Library.PInvoke;

	using Microsoft.Win32;

	/// <summary>
	/// Installs (and uninstalls) DMT for the current user.
	/// DMT.exe is its own installer: a copy named *Setup*.exe (or run with --install) installs itself.
	/// No administrator rights are needed as everything goes into the user's profile.
	/// </summary>
	static class Installer
	{
		public const string AppName = "Dual Monitor Tools";

		const string UninstallKeyName = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\DualMonitorTools";
		const string RunKeyName = @"Software\Microsoft\Windows\CurrentVersion\Run";

		// the same name DMT itself uses for "Start with Windows"
		const string AutoStartValueName = "GNE_DualMonitorTools";

		// files that hold the user's data (copied when importing settings)
		static readonly string[] DataFiles = { "DmtSettings.xml", "DmtMagicWords.xml", "DmtWallpaperProviders.xml" };

		public static string InstallDirectory
		{
			get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", AppName); }
		}

		public static string InstalledExe
		{
			get { return Path.Combine(InstallDirectory, "DMT.exe"); }
		}

		public static string DataDirectory
		{
			get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName); }
		}

		static string StartMenuShortcut
		{
			get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), AppName + ".lnk"); }
		}

		static string DesktopShortcut
		{
			get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk"); }
		}

		public static string Version
		{
			get { return typeof(Installer).Assembly.GetName().Version.ToString(); }
		}

		/// <summary>
		/// Gets a value indicating whether this copy should act as the installer
		/// </summary>
		public static bool IsSetupLaunch(string[] args)
		{
			if (args.Any(a => string.Equals(a, "--install", StringComparison.OrdinalIgnoreCase)))
			{
				return true;
			}

			string name = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? string.Empty);
			return name.IndexOf("setup", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		public static bool IsUninstallLaunch(string[] args)
		{
			return args.Any(a => string.Equals(a, "--uninstall", StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Gets a value indicating whether DMT has already been installed by this installer
		/// </summary>
		public static bool IsInstalled
		{
			get
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(UninstallKeyName))
				{
					return key != null && File.Exists(InstalledExe);
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether DMT is currently set to start with Windows
		/// </summary>
		public static bool StartsWithWindows
		{
			get
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyName))
				{
					return key != null && key.GetValue(AutoStartValueName) != null;
				}
			}
		}

		public static bool HasDesktopShortcut
		{
			get { return File.Exists(DesktopShortcut); }
		}

		public static bool HasSettings
		{
			get { return File.Exists(Path.Combine(DataDirectory, "DmtSettings.xml")); }
		}

		/// <summary>
		/// Installs (or updates) DMT
		/// </summary>
		public static void Install(bool startMenuShortcut, bool desktopShortcut, bool startWithWindows)
		{
			CloseRunningCopies();

			Directory.CreateDirectory(InstallDirectory);
			CopyWithRetry(Environment.ProcessPath, InstalledExe);

			// keep the settings in the user's roaming profile, away from the program
			Directory.CreateDirectory(DataDirectory);
			File.WriteAllText(Path.Combine(InstallDirectory, "DmtFileLocations.xml"),
				"<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
				"<!-- Written by the installer: DMT keeps its settings in the folder below -->\r\n" +
				"<filelocations>\r\n" +
				"\t<filelocation name=\"datadirectory\" value=\"%APPDATA%\\" + AppName + "\" />\r\n" +
				"</filelocations>\r\n");

			SetShortcut(StartMenuShortcut, startMenuShortcut);
			SetShortcut(DesktopShortcut, desktopShortcut);

			using (RegistryKey run = Registry.CurrentUser.CreateSubKey(RunKeyName))
			{
				if (startWithWindows)
				{
					run.SetValue(AutoStartValueName, InstalledExe);
				}
				else
				{
					run.DeleteValue(AutoStartValueName, false);
				}
			}

			// so it shows in Settings > Apps and can be uninstalled from there
			using (RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallKeyName))
			{
				key.SetValue("DisplayName", AppName);
				key.SetValue("DisplayVersion", Version);
				key.SetValue("Publisher", "Dual Monitor Tools contributors");
				key.SetValue("DisplayIcon", InstalledExe);
				key.SetValue("InstallLocation", InstallDirectory);
				key.SetValue("UninstallString", "\"" + InstalledExe + "\" --uninstall");
				key.SetValue("URLInfoAbout", "https://github.com/Benn25/MultiMon-Tool");
				key.SetValue("NoModify", 1, RegistryValueKind.DWord);
				key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
				key.SetValue("EstimatedSize", (int)(new FileInfo(InstalledExe).Length / 1024), RegistryValueKind.DWord);
			}
		}

		/// <summary>
		/// Copies the user's data files from a folder (e.g. a portable copy of DMT) into the settings folder
		/// </summary>
		/// <returns>Number of files copied</returns>
		public static int ImportSettings(string fromDirectory)
		{
			Directory.CreateDirectory(DataDirectory);
			int copied = 0;
			foreach (string file in DataFiles)
			{
				string source = Path.Combine(fromDirectory, file);
				if (File.Exists(source))
				{
					File.Copy(source, Path.Combine(DataDirectory, file), true);
					copied++;
				}
			}

			return copied;
		}

		/// <summary>
		/// Removes DMT.  The program folder is deleted just after we exit (we're running from it).
		/// </summary>
		public static void Uninstall(bool removeSettings)
		{
			CloseRunningCopies();

			using (RegistryKey run = Registry.CurrentUser.OpenSubKey(RunKeyName, true))
			{
				if (run != null)
				{
					run.DeleteValue(AutoStartValueName, false);
				}
			}

			DesktopContextMenu.Remove();
			SetShortcut(StartMenuShortcut, false);
			SetShortcut(DesktopShortcut, false);
			Registry.CurrentUser.DeleteSubKeyTree(UninstallKeyName, false);

			if (removeSettings && Directory.Exists(DataDirectory))
			{
				Directory.Delete(DataDirectory, true);
			}

			// wait a moment for us to exit, then remove the program folder
			ProcessStartInfo cleanup = new ProcessStartInfo("cmd.exe",
				"/c ping -n 3 127.0.0.1 >nul & rmdir /s /q \"" + InstallDirectory + "\"")
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				WindowStyle = ProcessWindowStyle.Hidden,
			};
			Process.Start(cleanup);
		}

		/// <summary>
		/// Asks any running DMT to close (so it saves its settings), forcing it if it doesn't
		/// </summary>
		static void CloseRunningCopies()
		{
			IntPtr hWnd = NativeMethods.FindWindow(null, "DMT_GUI_WINDOW");
			if (hWnd != IntPtr.Zero)
			{
				NativeMethods.SendMessage(hWnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
			}

			int self = Environment.ProcessId;
			foreach (Process process in Process.GetProcessesByName("DMT"))
			{
				using (process)
				{
					if (process.Id == self)
					{
						continue;
					}

					try
					{
						if (!process.WaitForExit(5000))
						{
							process.Kill();
							process.WaitForExit(2000);
						}
					}
					catch (Exception)
					{
						// it may have gone already
					}
				}
			}
		}

		static void CopyWithRetry(string source, string destination)
		{
			if (string.Equals(Path.GetFullPath(source), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			for (int attempt = 0; ; attempt++)
			{
				try
				{
					File.Copy(source, destination, true);
					return;
				}
				catch (IOException)
				{
					// the old exe can take a moment to be released after it exits
					if (attempt >= 10)
					{
						throw;
					}

					Thread.Sleep(500);
				}
			}
		}

		static void SetShortcut(string shortcutPath, bool wanted)
		{
			if (!wanted)
			{
				if (File.Exists(shortcutPath))
				{
					File.Delete(shortcutPath);
				}

				return;
			}

			Type shellType = Type.GetTypeFromProgID("WScript.Shell");
			dynamic shell = Activator.CreateInstance(shellType);
			try
			{
				dynamic shortcut = shell.CreateShortcut(shortcutPath);
				shortcut.TargetPath = InstalledExe;
				shortcut.WorkingDirectory = InstallDirectory;
				shortcut.IconLocation = InstalledExe + ",0";
				shortcut.Description = "Tools for multiple monitors";
				shortcut.Save();
			}
			finally
			{
				System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
			}
		}
	}
}
