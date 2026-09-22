using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DMT.Library.DisplayHandling
{
	/// <summary>
	/// Native methods used by DisplayDevices
	/// 
	/// TODO: When complete, this needs moving/merging into PInvoke/NativeMethods
	/// </summary>
	public static class NativeDisplayMethods
	{
		// consts/structures extracted from WinGDI.h
		public const int QDC_ALL_PATHS = 1;

		// flags for SetDisplayConfig(,,,,Flags)
		public const int SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x00000020;
		public const int SDC_APPLY = 0x00000080;
		public const int SDC_ALLOW_CHANGES = 0x00000400;

		// flags for DISPLAYCONFIG_PATH_INFO.flags
		public const uint DISPLAYCONFIG_PATH_ACTIVE = 0x00000001;

		//flags for DISPLAYCONFIG_PATH_TARGET_INFO.modeInfoIdx
		public const uint DISPLAYCONFIG_PATH_MODE_IDX_INVALID = 0xffffffff;

		// flags for DISPLAYCONFIG_MODE_INFO.infoType
		public const int DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE = 1;


		public const int ERROR_INSUFFICIENT_BUFFER = 122;


		public enum DISPLAYCONFIG_DEVICE_INFO_TYPE : uint
		{
			DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1,
			DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2,
			DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_PREFERRED_MODE = 3,
			DISPLAYCONFIG_DEVICE_INFO_GET_ADAPTER_NAME = 4,
			DISPLAYCONFIG_DEVICE_INFO_SET_TARGET_PERSISTENCE = 5,
			DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_BASE_TYPE = 6,
			DISPLAYCONFIG_DEVICE_INFO_FORCE_UINT32 = 0xFFFFFFFF
		}

		public enum DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY : uint
		{
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_OTHER = 0xFFFFFFFF,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HD15 = 0,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SVIDEO = 1,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPOSITE_VIDEO = 2,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_COMPONENT_VIDEO = 3,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DVI = 4,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_HDMI = 5,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_LVDS = 6,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_D_JPN = 8,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDI = 9,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EXTERNAL = 10,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_DISPLAYPORT_EMBEDDED = 11,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EXTERNAL = 12,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_UDI_EMBEDDED = 13,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_SDTVDONGLE = 14,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_MIRACAST = 15,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL = 0x80000000,
			DISPLAYCONFIG_OUTPUT_TECHNOLOGY_FORCE_UINT32 = 0xFFFFFFFF
		}

		public enum DISPLAYCONFIG_PIXELFORMAT : uint
		{
			DISPLAYCONFIG_PIXELFORMAT_8BPP = 1,
			DISPLAYCONFIG_PIXELFORMAT_16BPP = 2,
			DISPLAYCONFIG_PIXELFORMAT_24BPP = 3,
			DISPLAYCONFIG_PIXELFORMAT_32BPP = 4,
			DISPLAYCONFIG_PIXELFORMAT_NONGDI = 5,
			DISPLAYCONFIG_PIXELFORMAT_FORCE_UINT32 = 0xffffffff
		}

		public enum DISPLAYCONFIG_ROTATION : uint
		{
			DISPLAYCONFIG_ROTATION_IDENTITY = 1,
			DISPLAYCONFIG_ROTATION_ROTATE90 = 2,
			DISPLAYCONFIG_ROTATION_ROTATE180 = 3,
			DISPLAYCONFIG_ROTATION_ROTATE270 = 4,
			DISPLAYCONFIG_ROTATION_FORCE_UINT32 = 0xFFFFFFFF
		}


		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct LUID
		{
			public uint LowPart;
			public int HighPart;
		};


		// 20 bytes
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct DISPLAYCONFIG_PATH_SOURCE_INFO
		{
			public LUID adapterId;
			public uint id;
			public uint modeInfoIdx;
			public uint statusFlags;
		};

		// 8 bytes
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct DISPLAYCONFIG_RATIONAL
		{
			public uint Numerator;
			public uint Denominator;
		};

		// 48 bytes
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct DISPLAYCONFIG_PATH_TARGET_INFO
		{
			public LUID adapterId;
			public uint id;
			public uint modeInfoIdx;
			public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
			public DISPLAYCONFIG_ROTATION rotation;
			public int scaling;
			public DISPLAYCONFIG_RATIONAL refreshRate;
			public int scanLineOrdering;
			public int targetAvailable;
			public uint statusFlags;
		};

		// 72 bytes
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct DISPLAYCONFIG_PATH_INFO
		{
			public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
			public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
			public uint flags;
		};

		// 8 bytes
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct DISPLAYCONFIG_2DREGION
		{
			public uint cx;
			public uint cy;
		};

		// 48 bytes
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct DISPLAYCONFIG_TARGET_MODE
		{
			public ulong pixelRate;
			public DISPLAYCONFIG_RATIONAL hSyncFreq;
			public DISPLAYCONFIG_RATIONAL vSyncFreq;
			public DISPLAYCONFIG_2DREGION activeSize;
			public DISPLAYCONFIG_2DREGION totalSize;
			public uint videoStandard;
			public uint scanLineOrdering;

		};

		// 8 bytes
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct POINTL
		{
			public int x;
			public int y;
		};

		// 20 bytes
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct DISPLAYCONFIG_SOURCE_MODE
		{
			public uint width;
			public uint height;
			public DISPLAYCONFIG_PIXELFORMAT pixelFormat;
			public POINTL position;
			// padding to make same length as DISPLAYCONFIG_TARGET_MODE
			public uint padding1;
			public uint padding2;
			public uint padding3;
			public uint padding4;
			public uint padding5;
			public uint padding6;
			public uint padding7;

		};

		// 64 bytes
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct DISPLAYCONFIG_MODE_INFO
		{
			public int infoType;
			public uint id;
			public LUID adapterId;
			//union 
			//{
			//    DISPLAYCONFIG_TARGET_MODE targetMode;
			//    DISPLAYCONFIG_SOURCE_MODE sourceMode;
			//}
			public DISPLAYCONFIG_SOURCE_MODE sourceMode;
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
		{
			public DISPLAYCONFIG_DEVICE_INFO_TYPE type;
			public uint size;
			public LUID adapterId;
			public uint id;
		}


		[StructLayout(LayoutKind.Sequential)]
		public struct DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS
		{
			public uint value;
		}


		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
		{
			public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string viewGdiDeviceName;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct DISPLAYCONFIG_TARGET_DEVICE_NAME
		{
			public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
			public DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS flags;
			public DISPLAYCONFIG_VIDEO_OUTPUT_TECHNOLOGY outputTechnology;
			public ushort edidManufactureId;
			public ushort edidProductCodeId;
			public uint connectorInstance;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
			public string monitorFriendlyDeviceName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string monitorDevicePath;
		}

		//[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		//public struct DISPLAYCONFIG_ADAPTER_NAME
		//{
		//	public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
		//	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		//	public string adapterDevicePath;
		//}

		#region For setting orientation
		// see https://stackoverflow.com/questions/39288135/rotating-the-display-programmatically

		public const int ENUM_CURRENT_SETTINGS = -1;

		public enum DISP_CHANGE : int
		{
			Successful = 0,
			Restart = 1,
			Failed = -1,
			BadMode = -2,
			NotUpdated = -3,
			BadFlags = -4,
			BadParam = -5,
			BadDualView = -6
		}

		[Flags()]
		public enum DisplayDeviceStateFlags : int
		{
			/// <summary>The device is part of the desktop.</summary>
			AttachedToDesktop = 0x1,
			MultiDriver = 0x2,
			/// <summary>The device is part of the desktop.</summary>
			PrimaryDevice = 0x4,
			/// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
			MirroringDriver = 0x8,
			/// <summary>The device is VGA compatible.</summary>
			VGACompatible = 0x10,
			/// <summary>The device is removable; it cannot be the primary display.</summary>
			Removable = 0x20,
			/// <summary>The device has more display modes than its output devices support.</summary>
			ModesPruned = 0x8000000,
			Remote = 0x4000000,
			Disconnect = 0x2000000
		}


		[Flags()]
		public enum DisplaySettingsFlags : int
		{
			CDS_NONE = 0,
			CDS_UPDATEREGISTRY = 0x00000001,
			CDS_TEST = 0x00000002,
			CDS_FULLSCREEN = 0x00000004,
			CDS_GLOBAL = 0x00000008,
			CDS_SET_PRIMARY = 0x00000010,
			CDS_VIDEOPARAMETERS = 0x00000020,
			CDS_ENABLE_UNSAFE_MODES = 0x00000100,
			CDS_DISABLE_UNSAFE_MODES = 0x00000200,
			CDS_RESET = 0x40000000,
			CDS_RESET_EX = 0x20000000,
			CDS_NORESET = 0x10000000
		}

		[Flags()]
		public enum DM : int
		{
			Orientation = 0x00000001,
			PaperSize = 0x00000002,
			PaperLength = 0x00000004,
			PaperWidth = 0x00000008,
			Scale = 0x00000010,
			Position = 0x00000020,
			NUP = 0x00000040,
			DisplayOrientation = 0x00000080,
			Copies = 0x00000100,
			DefaultSource = 0x00000200,
			PrintQuality = 0x00000400,
			Color = 0x00000800,
			Duplex = 0x00001000,
			YResolution = 0x00002000,
			TTOption = 0x00004000,
			Collate = 0x00008000,
			FormName = 0x00010000,
			LogPixels = 0x00020000,
			BitsPerPixel = 0x00040000,
			PelsWidth = 0x00080000,
			PelsHeight = 0x00100000,
			DisplayFlags = 0x00200000,
			DisplayFrequency = 0x00400000,
			ICMMethod = 0x00800000,
			ICMIntent = 0x01000000,
			MediaType = 0x02000000,
			DitherType = 0x04000000,
			PanningWidth = 0x08000000,
			PanningHeight = 0x10000000,
			DisplayFixedOutput = 0x20000000
		}


		[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
		public struct DEVMODE
		{
			public const int CCHDEVICENAME = 32;
			public const int CCHFORMNAME = 32;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
			[System.Runtime.InteropServices.FieldOffset(0)]
			public string dmDeviceName;
			[System.Runtime.InteropServices.FieldOffset(32)]
			public Int16 dmSpecVersion;
			[System.Runtime.InteropServices.FieldOffset(34)]
			public Int16 dmDriverVersion;
			[System.Runtime.InteropServices.FieldOffset(36)]
			public Int16 dmSize;
			[System.Runtime.InteropServices.FieldOffset(38)]
			public Int16 dmDriverExtra;
			[System.Runtime.InteropServices.FieldOffset(40)]
			public DM dmFields;

			[System.Runtime.InteropServices.FieldOffset(44)]
			Int16 dmOrientation;
			[System.Runtime.InteropServices.FieldOffset(46)]
			Int16 dmPaperSize;
			[System.Runtime.InteropServices.FieldOffset(48)]
			Int16 dmPaperLength;
			[System.Runtime.InteropServices.FieldOffset(50)]
			Int16 dmPaperWidth;
			[System.Runtime.InteropServices.FieldOffset(52)]
			Int16 dmScale;
			[System.Runtime.InteropServices.FieldOffset(54)]
			Int16 dmCopies;
			[System.Runtime.InteropServices.FieldOffset(56)]
			Int16 dmDefaultSource;
			[System.Runtime.InteropServices.FieldOffset(58)]
			Int16 dmPrintQuality;

			[System.Runtime.InteropServices.FieldOffset(44)]
			public POINTL dmPosition;
			[System.Runtime.InteropServices.FieldOffset(52)]
			public Int32 dmDisplayOrientation;
			[System.Runtime.InteropServices.FieldOffset(56)]
			public Int32 dmDisplayFixedOutput;

			[System.Runtime.InteropServices.FieldOffset(60)]
			public short dmColor;
			[System.Runtime.InteropServices.FieldOffset(62)]
			public short dmDuplex;
			[System.Runtime.InteropServices.FieldOffset(64)]
			public short dmYResolution;
			[System.Runtime.InteropServices.FieldOffset(66)]
			public short dmTTOption;
			[System.Runtime.InteropServices.FieldOffset(68)]
			public short dmCollate;
			[System.Runtime.InteropServices.FieldOffset(72)]
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
			public string dmFormName;
			[System.Runtime.InteropServices.FieldOffset(102)]
			public Int16 dmLogPixels;
			[System.Runtime.InteropServices.FieldOffset(104)]
			public Int32 dmBitsPerPel;
			[System.Runtime.InteropServices.FieldOffset(108)]
			public Int32 dmPelsWidth;
			[System.Runtime.InteropServices.FieldOffset(112)]
			public Int32 dmPelsHeight;
			[System.Runtime.InteropServices.FieldOffset(116)]
			public Int32 dmDisplayFlags;
			[System.Runtime.InteropServices.FieldOffset(116)]
			public Int32 dmNup;
			[System.Runtime.InteropServices.FieldOffset(120)]
			public Int32 dmDisplayFrequency;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct DISPLAY_DEVICE
		{
			[MarshalAs(UnmanagedType.U4)]
			public int cb;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string DeviceName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceString;
			[MarshalAs(UnmanagedType.U4)]
			public DisplayDeviceStateFlags StateFlags;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceID;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceKey;
		}

		[DllImport("user32.dll")]
		public static extern DISP_CHANGE ChangeDisplaySettingsEx(
			string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd,
			DisplaySettingsFlags dwflags, IntPtr lParam);

		// flags for EnumDisplayDevices(dwFlags)
		public const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x1;

		[DllImport("user32.dll")]
		public static extern bool EnumDisplayDevices(
			string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice,
			uint dwFlags);

		[DllImport("user32.dll", CharSet = CharSet.Ansi)]
		public static extern bool EnumDisplaySettings(
			string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);


		#endregion For setting orientation


		[DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
		public static extern int DisplayConfigGetDeviceInfo_Target(
			ref DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName);

		[DllImport("user32.dll", EntryPoint="DisplayConfigGetDeviceInfo")]
		public static extern int DisplayConfigGetDeviceInfo_Source(
			ref DISPLAYCONFIG_SOURCE_DEVICE_NAME deviceName);

		//[DllImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
		//public static extern int DisplayConfigGetDeviceInfo_Adapter(
		//	ref DISPLAYCONFIG_ADAPTER_NAME deviceName);

		[DllImport("user32.dll")]
		public static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeArrayElements);

		[DllImport("user32.dll")]
		public static extern int QueryDisplayConfig(uint Flags,
			//ref uint pNumPathArrayElements, IntPtr pPathInfoArray,
			ref uint pNumPathArrayElements, [In, Out] DISPLAYCONFIG_PATH_INFO[] pPathInfoArray,
			//ref uint pNumModeInfoArrayElements, IntPtr pModeInfoArray,
			ref uint pNumModeInfoArrayElements, [In, Out] DISPLAYCONFIG_MODE_INFO[] pModeInfoArray,
			IntPtr pCurrentTopologyId);

		[DllImport("user32.dll")]
		public static extern int SetDisplayConfig(
			uint numPathArrayElements, DISPLAYCONFIG_PATH_INFO[] pPathInfoArray,
			uint numModeInfoArrayElements, DISPLAYCONFIG_MODE_INFO[] pModeInfoArray,
			uint Flags);

		[DllImport("Dxva2.dll")]
		public static extern bool SetMonitorBrightness(IntPtr hMonitor, uint dwNewBrightness);

		[DllImport("Dxva2.dll")]
		public static extern bool SetMonitorContrast(IntPtr hMonitor, uint dwNewContrast);
	}
}
