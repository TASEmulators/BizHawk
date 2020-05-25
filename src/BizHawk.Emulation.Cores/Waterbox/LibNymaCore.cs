using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Waterbox
{
	public abstract unsafe class LibNymaCore : LibWaterboxCore
	{
		[StructLayout(LayoutKind.Sequential)]
		public class InitData
		{
			/// <summary>
			/// Filename without extension.  Used in autodetect
			/// </summary>
			public string FileNameBase;
			/// <summary>
			/// Just the extension.  Used in autodetect.  LOWERCASE PLEASE.
			/// </summary>
			public string FileNameExt;
			/// <summary>
			/// Full filename.  This will be fopen()ed by the core
			/// </summary>
			public string FileNameFull;
		}

		/// <summary>
		/// Do this before calling anything, even settings queries
		/// </summary>
		[BizImport(CC, Compatibility = true)]
		public abstract void PreInit();

		/// <summary>
		/// Load a ROM
		/// </summary>
		[BizImport(CC, Compatibility = true)]
		public abstract bool InitRom([In]InitData data);

		/// <summary>
		/// Load some CDs
		/// </summary>
		[BizImport(CC)]
		public abstract bool InitCd(int numdisks);

		public enum CommandType : int
		{
			NONE = 0x00,
			RESET = 0x01,
			POWER = 0x02,

			INSERT_COIN = 0x07,

			TOGGLE_DIP0 = 0x10,
			TOGGLE_DIP1,
			TOGGLE_DIP2,
			TOGGLE_DIP3,
			TOGGLE_DIP4,
			TOGGLE_DIP5,
			TOGGLE_DIP6,
			TOGGLE_DIP7,
			TOGGLE_DIP8,
			TOGGLE_DIP9,
			TOGGLE_DIP10,
			TOGGLE_DIP11,
			TOGGLE_DIP12,
			TOGGLE_DIP13,
			TOGGLE_DIP14,
			TOGGLE_DIP15,
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			/// <summary>
			/// true to skip video rendering
			/// </summary>
			public short SkipRendering;
			/// <summary>
			/// true to skip audion rendering
			/// </summary>
			public short SkipSoundening;
			/// <summary>
			/// a single command to run at the start of this frame
			/// </summary>
			public CommandType Command;
			/// <summary>
			/// raw data for each input port, assumed to be MAX_PORTS * MAX_PORT_DATA long
			/// </summary>
			public byte* InputPortData;
		}

		/// <summary>
		/// Gets raw layer data to be handled by NymaCore.GetLayerData
		/// </summary>
		[BizImport(CC)]
		public abstract byte* GetLayerData();

		/// <summary>
		/// Set enabled layers (or is it disabled layers?).  Only call if NymaCore.GetLayerData() returned non null
		/// </summary>
		/// <param name="layers">bitmask in order defined by NymaCore.GetLayerData</param>
		[BizImport(CC)]
		public abstract void SetLayers(ulong layers);

		public enum InputType : byte
		{
			PADDING = 0,	// n-bit, zero
			BUTTON,		// 1-bit
			BUTTON_CAN_RAPID, // 1-bit
			SWITCH,		// ceil(log2(n))-bit
					// Current switch position(default 0).
					// Persistent, and bidirectional communication(can be modified driver side, and Mednafen core and emulation module side)
			STATUS,		// ceil(log2(n))-bit
					// emulation module->driver communication
			AXIS,		// 16-bits; 0 through 65535; 32768 is centered position
			POINTER_X,	// mouse pointer, 16-bits, signed - in-screen/window range before scaling/offseting normalized coordinates: [0.0, 1.0)
			POINTER_Y,	// see: mouse_scale_x, mouse_scale_y, mouse_offs_x, mouse_offs_y
			AXIS_REL,		// mouse relative motion, 16-bits, signed
			BYTE_SPECIAL,
			RESET_BUTTON,	// 1-bit
			BUTTON_ANALOG,	// 16-bits, 0 - 65535
			RUMBLE,		// 16-bits, lower 8 bits are weak rumble(0-255), next 8 bits are strong rumble(0-255), 0=no rumble, 255=max rumble.  Somewhat subjective, too...
		}

		[Flags]
		public enum AxisFlags: byte
		{
			NONE = 0,
			// Denotes analog data that may need to be scaled to ensure a more squareish logical range(for emulated analog sticks)
			SQLR = 0x01,
			// Invert config order of the two components(neg,pos) of the axis
			INVERT_CO = 0x02,
			SETTINGS_UNDOC = 0x80,
		}

		[Flags]
		public enum DeviceFlags: uint
		{
			NONE = 0,
			KEYBOARD = 1
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct NPortInfo
		{
			public IntPtr _shortName;
			public IntPtr _fullName;
			public IntPtr _defaultDeviceShortName;
			public uint NumDevices;

			public string ShortName => Mershul.PtrToStringUtf8(_shortName);
			public string FullName => Mershul.PtrToStringUtf8(_fullName);
			public string DefaultDeviceShortName => Mershul.PtrToStringUtf8(_defaultDeviceShortName);
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct NDeviceInfo
		{
			public IntPtr _shortName;
			public IntPtr _fullName;
			public IntPtr _description;
			public DeviceFlags Flags;
			public uint ByteLength;
			public uint NumInputs;

			public string ShortName => Mershul.PtrToStringUtf8(_shortName);
			public string FullName => Mershul.PtrToStringUtf8(_fullName);
			public string Description => Mershul.PtrToStringUtf8(_description);
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct NInputInfo
		{
			public IntPtr _settingName;
			public IntPtr _name;
			public short ConfigOrder;
			public ushort BitOffset;
			public InputType Type;
			public AxisFlags Flags;
			public byte BitSize;

			public string SettingName => Mershul.PtrToStringUtf8(_settingName);
			public string Name => Mershul.PtrToStringUtf8(_name);
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct NButtonInfo
		{
			public IntPtr _excludeName;

			public string ExcludeName => Mershul.PtrToStringUtf8(_excludeName);
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct NAxisInfo
		{
			public IntPtr _settingsNameNeg;
			public IntPtr _settingsNamePos;
			public IntPtr _nameNeg;
			public IntPtr _namePos;

			public string SettingsNameNeg => Mershul.PtrToStringUtf8(_settingsNameNeg);
			public string SettingsNamePos => Mershul.PtrToStringUtf8(_settingsNamePos);
			public string NameNeg => Mershul.PtrToStringUtf8(_nameNeg);
			public string NamePos => Mershul.PtrToStringUtf8(_namePos);
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct NSwitchInfo
		{
			public uint NumPositions;
			public uint DefaultPosition;
			[StructLayout(LayoutKind.Sequential)]
			public struct Position
			{
				public IntPtr _settingName;
				public IntPtr _name;
				public IntPtr _description;

				public string SettingName => Mershul.PtrToStringUtf8(_settingName);
				public string Name => Mershul.PtrToStringUtf8(_name);
				public string Description => Mershul.PtrToStringUtf8(_description);
			}
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct NStatusInfo
		{
			public uint NumStates;
			[StructLayout(LayoutKind.Sequential)]
			public struct State
			{
				public IntPtr _shortName;
				public IntPtr _name;
				public int Color; // (msb)0RGB(lsb), -1 for unused.
				public int _Padding;

				public string ShortName => Mershul.PtrToStringUtf8(_shortName);
				public string Name => Mershul.PtrToStringUtf8(_name);
			}
		}

		[BizImport(CC, Compatibility = true)]
		public abstract uint GetNumPorts();
		[BizImport(CC, Compatibility = true)]
		public abstract NPortInfo* GetPort(uint port);
		[BizImport(CC, Compatibility = true)]
		public abstract NDeviceInfo* GetDevice(uint port, uint dev);
		[BizImport(CC, Compatibility = true)]
		public abstract NInputInfo* GetInput(uint port, uint dev, uint input);
		[BizImport(CC, Compatibility = true)]
		public abstract NButtonInfo* GetButton(uint port, uint dev, uint input);
		[BizImport(CC, Compatibility = true)]
		public abstract NSwitchInfo* GetSwitch(uint port, uint dev, uint input);
		[BizImport(CC, Compatibility = true)]
		public abstract NSwitchInfo.Position* GetSwitchPosition(uint port, uint dev, uint input, int i);
		[BizImport(CC, Compatibility = true)]
		public abstract NStatusInfo* GetStatus(uint port, uint dev, uint input);
		[BizImport(CC, Compatibility = true)]
		public abstract NStatusInfo.State* GetStatusState(uint port, uint dev, uint input, int i);
		[BizImport(CC, Compatibility = true)]
		public abstract NAxisInfo* GetAxis(uint port, uint dev, uint input);

		/// <summary>
		/// Set what input devices we're going to use
		/// </summary>
		/// <param name="devices">MUST end with a null string</param>
		[BizImport(CC, Compatibility = true)]
		public abstract void SetInputDevices(string[] devices);

		public enum VideoSystem : int
		{
			NONE,
			PAL,
			PAL_M,
			NTSC,
			SECAM
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SystemInfo
		{
			public int MaxWidth;
			public int MaxHeight;
			public int NominalWidth;
			public int NominalHeight;
			public VideoSystem VideoSystem;
			public int FpsFixed;
		}

		[BizImport(CC, Compatibility = true)]
		public abstract SystemInfo* GetSystemInfo();

		[BizImport(CC, Compatibility = true)]
		public abstract void IterateSettings(int index, [In, Out]NymaCore.NymaSettingsInfo.MednaSettingS s);

		[BizImport(CC, Compatibility = true)]
		public abstract void IterateSettingEnums(int index, int enumIndex,[In, Out]NymaCore.NymaSettingsInfo.MednaSettingS.EnumValueS e);

		public delegate void FrontendSettingQuery(string setting, IntPtr dest);
		[BizImport(CC)]
		public abstract void SetFrontendSettingQuery(FrontendSettingQuery q);

		[StructLayout(LayoutKind.Sequential)]
		public class TOC
		{
			public int FirstTrack;
			public int LastTrack;
			public int DiskType;

			[StructLayout(LayoutKind.Sequential)]
			public struct Track
			{
				public int Adr;
				public int Control;
				public int Lba;
				public int Valid;
			}

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 101)]
			public Track[] Tracks;
		}
		[UnmanagedFunctionPointer(CC)]
		public delegate void CDTOCCallback(int disk, [In, Out]TOC toc);
		[UnmanagedFunctionPointer(CC)]
		public delegate void CDSectorCallback(int disk, int lba, IntPtr dest);
		[BizImport(CC)]
		public abstract void SetCDCallbacks(CDTOCCallback toccallback, CDSectorCallback sectorcallback);
	}
}
