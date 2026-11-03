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
	using System.Drawing;
	using System.IO;
	using System.Windows.Forms;

	using DMT.Library.Ui;

	/// <summary>
	/// The window shown when DMT is run as its installer
	/// </summary>
	class InstallDialog : ContentDialog
	{
		ToggleSwitch _startMenu;
		ToggleSwitch _desktop;
		ToggleSwitch _autoStart;
		ToggleSwitch _openWhenDone;
		SettingsCard _importCard;
		InfoBar _resultBar;
		string _importFrom;
		bool _installed;

		public InstallDialog()
			: base(Installer.IsInstalled ? "Update " + Installer.AppName : "Install " + Installer.AppName, 460)
		{
			StartPosition = FormStartPosition.CenterScreen;
			ShowInTaskbar = true;
			ShowIcon = true;
			Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath);

			bool update = Installer.IsInstalled;
			Body.Add(new TextBlock(string.Format("Version {0}. Installed just for you, so no administrator rights are needed.", Installer.Version), TextRole.Body, TextTone.Secondary));

			if (update)
			{
				Body.Add(new InfoBar(Severity.Info, "Already installed", "This will update it. Your settings, shortcuts and magic words are kept."), 16);
			}

			Body.Add(new SettingsCard("Install location", Installer.InstallDirectory, Theme.GlyphFolder), 16);

			_startMenu = AddToggle("Start menu shortcut", true, 4);
			_desktop = AddToggle("Desktop shortcut", update && Installer.HasDesktopShortcut, 4);
			_autoStart = AddToggle("Start with Windows", update ? Installer.StartsWithWindows : true, 4);
			_openWhenDone = AddToggle("Open when finished", true, 4);

			if (!Installer.HasSettings)
			{
				FluentButton choose = new FluentButton("Choose", Theme.GlyphFolder);
				choose.Click += ChooseImport_Click;
				_importCard = Body.Add(new SettingsCard("Bring your old settings", "If you used a portable copy of DMT, choose its DmtSettings.xml to keep your shortcuts, zones and magic words", Theme.GlyphSwap, choose), 16);
			}

			Body.Add(new TextBlock("If Dual Monitor Tools is running, it will be closed first.", TextRole.Caption, TextTone.Tertiary), 12);

			_resultBar = Body.Add(new InfoBar(Severity.Error, null, string.Empty), 12);
			_resultBar.Visible = false;

			PrimaryButton.Text = update ? "Update" : "Install";
			PrimaryButton.Click += Install_Click;
		}

		ToggleSwitch AddToggle(string title, bool value, int spaceBefore)
		{
			ToggleSwitch toggle = new ToggleSwitch { Checked = value };
			Body.Add(new SettingsCard(title, null, null, toggle), spaceBefore);
			return toggle;
		}

		void ChooseImport_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog { Filter = "DMT settings|DmtSettings.xml|All files|*.*", Title = "Choose the old DmtSettings.xml" })
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					_importFrom = Path.GetDirectoryName(dlg.FileName);
					_importCard.Description = "Will be copied from " + _importFrom;
				}
			}
		}

		void Install_Click(object sender, EventArgs e)
		{
			if (_installed)
			{
				Close();
				return;
			}

			try
			{
				UseWaitCursor = true;
				PrimaryButton.Enabled = false;
				Application.DoEvents();

				Installer.Install(_startMenu.Checked, _desktop.Checked, _autoStart.Checked);
				string message = "Installed in " + Installer.InstallDirectory + ".";
				if (_importFrom != null)
				{
					int count = Installer.ImportSettings(_importFrom);
					message += string.Format(" {0} settings file{1} brought across.", count, count == 1 ? string.Empty : "s");
				}

				if (_openWhenDone.Checked)
				{
					Process.Start(new ProcessStartInfo(Installer.InstalledExe) { UseShellExecute = true, WorkingDirectory = Installer.InstallDirectory });
					Close();
					return;
				}

				_installed = true;
				_resultBar.Severity = Severity.Success;
				_resultBar.Title = "Done";
				_resultBar.Text = message;
				_resultBar.Visible = true;
				PrimaryButton.Text = "Close";
				SecondaryButton.Visible = false;
				FitToContent();
			}
			catch (Exception ex)
			{
				_resultBar.Severity = Severity.Error;
				_resultBar.Title = "Couldn't install";
				_resultBar.Text = ex.Message;
				_resultBar.Visible = true;
				FitToContent();
			}
			finally
			{
				UseWaitCursor = false;
				PrimaryButton.Enabled = true;
			}
		}
	}

	/// <summary>
	/// The window shown when uninstalling (from Settings > Apps)
	/// </summary>
	class UninstallDialog : ContentDialog
	{
		ToggleSwitch _removeSettings;

		public UninstallDialog()
			: base("Uninstall " + Installer.AppName, 440)
		{
			StartPosition = FormStartPosition.CenterScreen;
			ShowInTaskbar = true;
			ShowIcon = true;
			Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath);

			Body.Add(new TextBlock("This removes Dual Monitor Tools, its shortcuts and its start-up entry.", TextRole.Body, TextTone.Secondary));
			_removeSettings = new ToggleSwitch { Checked = false };
			Body.Add(new SettingsCard("Also delete my settings", "Shortcuts, zones and magic words in " + Installer.DataDirectory, Theme.GlyphDelete, _removeSettings), 16);

			PrimaryButton.Text = "Uninstall";
			PrimaryButton.Click += (s, e) =>
			{
				Installer.Uninstall(_removeSettings.Checked);
				DialogResult = DialogResult.OK;
				Close();
			};
		}
	}
}
