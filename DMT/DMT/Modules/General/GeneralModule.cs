#region copyright
// This file is part of Dual Monitor Tools which is a set of tools to assist
// users with multiple monitor setups.
// Copyright (C) 2015-2026  Gerald Evans
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

namespace DMT.Modules.General
{
	using System;
	using System.Collections.Generic;
	using System.Drawing;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Text;
	using System.Threading.Tasks;
	using System.Windows.Forms;

	using DMT.Library.Environment;
	using DMT.Library.GuiUtils;
	using DMT.Library.HotKeys;
	using DMT.Library.Logging;
	using DMT.Library.PInvoke;
	using DMT.Library.Settings;
	using DMT.Resources;

	using Microsoft.Win32;
	using DMT.Library.DisplayHandling;
	using DMT.Library.Utils;

	/// <summary>
	/// The general module
	/// </summary>
	class GeneralModule : DMT.Modules.Module
	{
		const string AutoStartKeyName = "GNE_DualMonitorTools";
		const string InstalledKeyName = @"SOFTWARE\GNE\Dual Monitor Tools";
		const string Installed6432KeyName = @"SOFTWARE\WOW6432Node\GNE\Dual Monitor Tools";
		const string InstalledValueName = "installed";

		ISettingsService _settingsService;
		ILocalEnvironment _localEnvironment;
		ILogger _logger;
		AppForm _appForm;

		// these panels can change when monitors are added/removed/changes
		GeneralMonitorNamesOptionsPanel _generalMonitorNamesOptionsPanel;
		GeneralMonitorOrderingOptionsPanel _generalMonitorOrderingOptionsPanel;
		GeneralMonitorsOptionsPanel _generalMonitorsOptionsPanel;

		IntSetting MonitorOrderSetting { get; set; }
		StringSetting MonitorNameOrderSetting { get; set; }
		StringSetting MonitorNamesSetting { get; set; }

		MonitorNameIdMap _monitorNames;

		/// <summary>
		/// Initialises a new instance of the <see cref="GeneralModule" /> class.
		/// </summary>
		/// <param name="settingsService">The settings service</param>
		/// <param name="hotKeyService">The hotkey service</param>
		/// <param name="logger">Application logger</param>
		/// <param name="appForm">Application (hidden) window</param>
		public GeneralModule(ISettingsService settingsService, IHotKeyService hotKeyService, ILocalEnvironment localEnvironment, ILogger logger, AppForm appForm)
			: base(hotKeyService)
		{
			_settingsService = settingsService;
			_localEnvironment = localEnvironment;
			_logger = logger;
			_appForm = appForm;

			ModuleName = "General";
		}

		/// <summary>
		/// Gets or sets a value indicating whether DMT to start when windows starts
		/// </summary>
		public bool StartWhenWindowsStarts
		{
			get
			{
				return AutoStart.IsAutoStart(AutoStartKeyName);
			}

			set
			{
				if (value)
				{
					AutoStart.SetAutoStart(AutoStartKeyName);
				}
				else
				{
					AutoStart.UnsetAutoStart(AutoStartKeyName);
				}
			}
		}

		public bool InDesktopContextMenu
		{
			get
			{
				return DesktopContextMenu.IsActive();
			}

			set
			{
				if (value)
				{
					DesktopContextMenu.Add();
				}
				else
				{
					DesktopContextMenu.Remove();
				}
			}
		}

		/// <summary>
		/// Gets the version number currently running
		/// </summary>
		public Version Version
		{
			get
			{
				return Assembly.GetExecutingAssembly().GetName().Version;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this version was installed with an msi installer
		/// </summary>
		public bool IsMsiInstall
		{
			get
			{
				// installed by DMT's own installer
				if (string.Equals(System.Environment.ProcessPath, DMT.Setup.Installer.InstalledExe, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}

				object keyValue = null;
				try
				{
					RegistryKey key = Registry.LocalMachine.OpenSubKey(InstalledKeyName);
					if (key != null)
					{
						keyValue = key.GetValue(InstalledValueName);
					}

					if (keyValue == null)
					{
						// installer is 32bit, but we could be running on a 64 bit O/S
						key = Registry.LocalMachine.OpenSubKey(Installed6432KeyName);
						if (key != null)
						{
							keyValue = key.GetValue(InstalledValueName);
						}
					}
				}
				catch (Exception)
				{
					// if we can't read the registry, assume this is a portable install.
					keyValue = null;
				}

				return keyValue != null;
			}
		}

		public Monitor.EMonitorOrder MonitorOrder 
		{ 
			get
			{
				return (Monitor.EMonitorOrder)MonitorOrderSetting.Value;
			}
			
			set
			{
				MonitorOrderSetting.Value = (int)value;
				Monitor.MonitorOrder = value;
				//if (_generalMonitorOrderingOptionsPanel != null)
				//{
				//	_generalMonitorOrderingOptionsPanel.ShowCurrentLayout();
				//}

				// this will inform all modules that the ordering has changed
				_appForm.MonitorOrderingChanged();
			}
		}

		public string[] MonitorNameOrdering
		{
			get
			{
				return MonitorNameOrderFromString(MonitorNameOrderSetting.Value);
			}

			set
			{
				MonitorNameOrderSetting.Value = MonitorNameOrderToString(value);
				Monitor.MonitorNameOrder = value;

				// this will inform all modules that the ordering may have changed
				_appForm.MonitorOrderingChanged();
			}
		}

		//public MonitorNames MonitorNames
		//{
		//	get
		//	{
		//		return MonitorNamesFromString(MonitorNamesSetting.Value);
		//	}

		//	set
		//	{
		//		MonitorNamesSetting.Value = value.ToString();
		//		Monitor.MonitorNames = value;

		//		// this will inform all modules that the ordering may have changed
		//		_appForm.MonitorOrderingChanged();
		//	}
		//}

		/// <summary>
		/// Called when the display resolution or monitor layout changes
		/// </summary>
		public override void DisplayResolutionChanged()
		{
			DisplayDevices displayDevices = GetDisplayDevices();

			if (_generalMonitorNamesOptionsPanel != null)
			{
				_generalMonitorNamesOptionsPanel.ShowCurrentLayout(displayDevices);
			}

			if (_generalMonitorOrderingOptionsPanel != null)
			{
				_generalMonitorOrderingOptionsPanel.ShowCurrentLayout(displayDevices);
			}

			if (_generalMonitorsOptionsPanel != null)
			{
				_generalMonitorsOptionsPanel.ShowCurrentInfo(displayDevices);
			}
		}

		//public bool AllowShowVirtualMonitors
		//{
		//	get
		//	{
		//		// we only need to allow the user to be able to select virtual monitors if running Vista or later
		//		return _localEnvironment.IsVistaOrLater();
		//	}
		//}

		public IList<DisplayDevice> GetAllMonitorProperties()
		{
			// TODO: any performance advantage in keeping this at the class level?
			DisplayDevices displayDevices = new DisplayDevices();

			return displayDevices.Items;
		}

		public DisplayDevices GetDisplayDevices()
		{
			DisplayDevices displayDevices = new DisplayDevices();

			return displayDevices;
		}

		/// <summary>
		/// Starts the module up
		/// </summary>
		public override void Start()
		{
			// hot keys / commands
			AddCommand("Options", GeneralStrings.OptionsDescription, string.Empty, ShowOptions, false, true);
			if (_localEnvironment.IsWin7OrLater())
			{
				AddCommand("ChangePrimary", GeneralStrings.ChangePrimaryDescription, string.Empty, nop, ChangePrimary);
				AddCommand("ChangeBrightness", GeneralStrings.ChangeBrightnessDescription, string.Empty, nop, ChangeBrightness);
				AddCommand("ChangeContrast", GeneralStrings.ChangeContrastDescription, string.Empty, nop, ChangeContrast);

				AddCommand("ChangeOrientation", GeneralStrings.ChangeOrientationDescription, string.Empty, nop, ChangeOrientation);
			}

			// settings
			MonitorOrderSetting = new IntSetting(_settingsService, ModuleName, "MonitorOrder", (int)Monitor.EMonitorOrder.LeftRight);
			MonitorNameOrderSetting = new StringSetting(_settingsService, ModuleName, "MonitorNameOrder");
			MonitorNamesSetting = new StringSetting(_settingsService, ModuleName, "MonitorNames");

			// set the monitor order now, so can be used by the other modules
			Monitor.MonitorOrder = (Monitor.EMonitorOrder)MonitorOrderSetting.Value;
			Monitor.MonitorNameOrder = MonitorNameOrderFromString(MonitorNameOrderSetting.Value);
			Monitor.MonitorNames = MonitorNamesFromString(MonitorNamesSetting.Value);
		}

		/// <summary>
		/// Starts the process of shutting DMT down
		/// </summary>
		public void StartShutdown()
		{
			Application.Exit();
		}

		/// <summary>
		/// Gets the option nodes for this module
		/// </summary>
		/// <returns>The root node</returns>
		public override ModuleOptionNode GetOptionNodes()
		{
			DisplayDevices displayDevices = GetDisplayDevices();

			Image image = new Bitmap(Properties.Resources.DMT_16_16);
			ModuleOptionNodeBranch options = new ModuleOptionNodeBranch("Dual Monitor Tools", image, new GeneralRootOptionsPanel());
			options.Nodes.Add(new ModuleOptionNodeLeaf("General", image, new GeneralOptionsPanel(this)));

			_generalMonitorNamesOptionsPanel = new GeneralMonitorNamesOptionsPanel(this, displayDevices);
			options.Nodes.Add(new ModuleOptionNodeLeaf("Monitor Names", image, _generalMonitorNamesOptionsPanel));

			_generalMonitorOrderingOptionsPanel = new GeneralMonitorOrderingOptionsPanel(this, displayDevices);
			options.Nodes.Add(new ModuleOptionNodeLeaf("Monitor Order", image, _generalMonitorOrderingOptionsPanel));

			// To simplify logic, we only support Win 7 or later
			// if needed, limited support for XP/Vista could be added at a later date
			if (_localEnvironment.IsWin7OrLater())
			{
				_generalMonitorsOptionsPanel = new GeneralMonitorsOptionsPanel(this, displayDevices);
				options.Nodes.Add(new ModuleOptionNodeLeaf("Monitors", image, _generalMonitorsOptionsPanel));
			}

			return options;
		}

		void ShowOptions()
		{
			_appForm.ShowOptions();
		}

		#region Primary monitor changing
		public void MakePrimary(int monitorIndex)	// 0 based
		{
			DisplayDevices displayDevices = new DisplayDevices();

			displayDevices.MakePrimary(monitorIndex);
		}

		// DMT:General:ChangePrimary <monitorIndex>
		void ChangePrimary(string parameters)
		{
			int monitorIndex;
			if (Monitor.ParseMonitorIndex(parameters, out monitorIndex))
			{
				MakePrimary(monitorIndex);
			}
		}
		#endregion Primary monitor changing

		#region Brightness/Contrast changing
		#region Brightness
		public void ChangeMonitorBrightness(int monitorIndex, int brightness, bool isDelta = false)
		{
			DisplayDevices displayDevices = new DisplayDevices();

			displayDevices.ChangeMonitorBrightness(monitorIndex, brightness, isDelta);
		}

		// DMT:General:ChangeBrightness <monitorIndex>:<value|delta>
		void ChangeBrightness(string parameters)
		{
			int monitorIndex;
			int brightness;
			bool isDelta;
			bool showDlg;
			if (ParseMonitorAdjustment(parameters, out monitorIndex, out brightness, out isDelta, out showDlg))
			{
				if (showDlg)
				{
					// no value given, so want dlg
					ShowBrightnessDlg(monitorIndex, null);
				}
				else
				{
					ChangeMonitorBrightness(monitorIndex, brightness, isDelta);
				}
			}
		}

		public bool ShowBrightnessDlg(int monitorIndex, IWin32Window owner)
		{
			// TODO: seems silly getting all devices, when we only want the one
			//System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
			DisplayDevices displayDevices = GetDisplayDevices();
			//DMT.Library.Logging.Logger logger = new DMT.Library.Logging.Logger();
			//logger.LogInfo("Performance", "GetDisplayDevices took: {0}ms", stopwatch.ElapsedMilliseconds);

			if (monitorIndex >= 0 && monitorIndex < displayDevices.Items.Count)
			{
				DisplayDevice displayDevice = displayDevices.Items[monitorIndex];
				// can only change brightness for active monitors
				if (displayDevice.IsActive)
				{
					//if (displayDevice.Brightness.Editable)
					NumericRange brightness = displayDevice.GetBrightness();
					if (brightness.Editable)
					{
						//EditNumericValueForm dlg = new EditNumericValueForm("Change Monitor Brightness", monitorIndex, displayDevice.Brightness,
						EditNumericValueForm dlg = new EditNumericValueForm("Change Monitor Brightness", monitorIndex, brightness,
							delegate(int monIndex, int numericValue)
							{
								displayDevices.ChangeMonitorBrightness(monIndex, numericValue, false);
							});
						//dlg.StartPosition = System.Windows.Forms.Cursor.Position;
						//dlg.StartPosition = FormStartPosition.CenterParent;
						if (dlg.ShowDialog(owner) == DialogResult.OK)
						{
							return true;
						}
					}
				}
			}

			return false;
		}
		#endregion Brightness

		#region Contrast
		public void ChangeMonitorContrast(int monitorIndex, int contrast, bool isDelta = false)
		{
			DisplayDevices displayDevices = new DisplayDevices();

			displayDevices.ChangeMonitorContrast(monitorIndex, contrast, isDelta);
		}

		// DMT:General:ChangeContrast <monitorIndex>:<value|delta>
		void ChangeContrast(string parameters)
		{
			int monitorIndex;
			int contrast;
			bool isDelta;
			bool showDlg;
			if (ParseMonitorAdjustment(parameters, out monitorIndex, out contrast, out isDelta, out showDlg))
			{
				if (showDlg)
				{
					// no value given, so want dlg
					ShowContrastDlg(monitorIndex, null);
				}
				else
				{
					ChangeMonitorContrast(monitorIndex, contrast, isDelta);
				}
			}
		}

		public bool ShowContrastDlg(int monitorIndex, IWin32Window owner)
		{
			DisplayDevices displayDevices = GetDisplayDevices();
			if (monitorIndex >= 0 && monitorIndex < displayDevices.Items.Count)
			{
				DisplayDevice displayDevice = displayDevices.Items[monitorIndex];
				// can only change brightness for active monitors
				if (displayDevice.IsActive)
				{
					//if (displayDevice.Contrast.Editable)
					NumericRange contrast = displayDevice.GetContrast();
					if (contrast.Editable)
					{
						//EditNumericValueForm dlg = new EditNumericValueForm("Change Monitor Contrast", monitorIndex, displayDevice.Contrast,
						EditNumericValueForm dlg = new EditNumericValueForm("Change Monitor Contrast", monitorIndex, contrast,
							delegate(int monIndex, int numericValue)
							{
								displayDevices.ChangeMonitorContrast(monIndex, numericValue, false);
							});
						if (dlg.ShowDialog(owner) == DialogResult.OK)
						{
							return true;
						}
					}
				}
			}

			return false;
		}
		#endregion Contrast

		bool ParseMonitorAdjustment(string parameters, out int monitorIndex, out int value, out bool isDelta, out bool showDlg)
		{
			monitorIndex = 0;
			value = 0;
			isDelta = false;
			showDlg = false;

			string[] fields = parameters.Split(new char[] { ':' });
			if (fields.Length >= 1)
			{
				if (Monitor.ParseMonitorIndex(fields[0], out monitorIndex))
				{
					if (fields.Length >= 2)
					{
						string amount = fields[1];
						if (amount.Length > 0 && (amount[0] == '+' || amount[0] == '-'))
						{
							// this is a delta value rather than absolute value
							isDelta = true;
						}
						if (int.TryParse(fields[1], out value))
						{
							return true;
						}
					}
					else
					{
						// Let user use GUI to adjust value
						showDlg = true;
						return true;
					}
				}
			}

			return false;
		}
		#endregion Brightness/Contrast changing

		#region Orientation
		public void ChangeMonitorOrientation(int monitorIndex, int degrees, bool isDelta = false)
		{
			DisplayDevices displayDevices = new DisplayDevices();

			displayDevices.ChangeMonitorOrientation(monitorIndex, degrees, isDelta);
		}

		void ChangeOrientation(string parameters)
		{
			int monitorIndex;
			int degrees;
			bool isDelta;
			bool showDlg;
			if (ParseMonitorAdjustment(parameters, out monitorIndex, out degrees, out isDelta, out showDlg))
			{
				if (showDlg)
				{
					// no value given, so want dlg
					ShowOrientationDlg(monitorIndex, null);
				}
				else
				{
					ChangeMonitorOrientation(monitorIndex, degrees, isDelta);
				}
			}
		}

		public bool ShowOrientationDlg(int monitorIndex, IWin32Window owner)
		{
			DisplayDevices displayDevices = GetDisplayDevices();
			if (monitorIndex >= 0 && monitorIndex < displayDevices.Items.Count)
			{
				DisplayDevice displayDevice = displayDevices.Items[monitorIndex];
				// can only change orientation for active monitors
				if (displayDevice.IsActive)
				{
					ChangeOrientationForm dlg = new ChangeOrientationForm(monitorIndex, displayDevice.RotationDegrees);
					if (dlg.ShowDialog(owner) == DialogResult.OK)
					{
						ChangeMonitorOrientation(monitorIndex, dlg.NewOrientation);

						return true;
					}
				}
			}

			return false;
		}

		#endregion Orientation

		#region Monitor names support

		public string GetMonitorName(string monitorId)
		{
			return Monitor.MonitorNames.GetName(monitorId);
		}

		public void SetMonitorName(string monitorId, string monitorName)
		{
			Monitor.MonitorNames.SetName(monitorId, monitorName);

			// persist this change
			MonitorNamesSetting.Value = Monitor.MonitorNames.ToString();

			// this may have an impact on monitor ordering 
			if (Monitor.MonitorOrder == Monitor.EMonitorOrder.DisplayName)
			{
				// rebuild monitor order
				Monitor.ReBuild();

				// and update panels that depend on this
				_appForm.MonitorOrderingChanged();
			}
			else
			{
				// still need to update any options panels that show the monitor name
				// TODO: need a better way of doing this,
				// but this will do what we want
				_appForm.MonitorOrderingChanged();
			}
		}

		string[] MonitorNameOrderFromString(string setting)
		{
			return MonitorNamesParser.StringToList(setting);
		}

		public string MonitorNameOrderToString()
		{
			return MonitorNameOrderToString(MonitorNameOrdering);
		}

		string MonitorNameOrderToString(string[] nameOrder)
		{
			//return ListParser.ListToString(nameOrder.ToList(), GeneralModule.MonitorNameSeparatorChars, ", ");
			return MonitorNamesParser.ListToString(nameOrder.ToList());
		}

		MonitorNameIdMap MonitorNamesFromString(string setting)
		{
			MonitorNameIdMap names = new MonitorNameIdMap();
			names.LoadNames(setting);
			return names;
		}

		public bool ShowEditNameDlg(int monitorIndex, IWin32Window owner)
		{
			// TODO: seems silly getting all devices, when we only want the one
			//System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
			DisplayDevices displayDevices = GetDisplayDevices();
			//DMT.Library.Logging.Logger logger = new DMT.Library.Logging.Logger();
			//logger.LogInfo("Performance", "GetDisplayDevices took: {0}ms", stopwatch.ElapsedMilliseconds);

			if (monitorIndex >= 0 && monitorIndex < displayDevices.Items.Count)
			{
				DisplayDevice displayDevice = displayDevices.Items[monitorIndex];

				string monitorId = displayDevice.GetMonitorId();
				// can only change name for monitors that have an id
				if (!string.IsNullOrEmpty(monitorId))
				{
					string monitorName = GetMonitorName(monitorId);

					EditMonitorNameForm dlg = new EditMonitorNameForm();
					dlg.MonitorName = monitorName;

					if (dlg.ShowDialog(owner) == DialogResult.OK)
					{
						// name may have changed
						string newMonitorName = dlg.MonitorName;

						// this will also force the panels that depend on the monitors 
						// (including ourselve) to redisplay
						SetMonitorName(monitorId, newMonitorName);
						return true;
					}
				}
			}

			return false;
		}

		#endregion Monitor names support
	}
}
