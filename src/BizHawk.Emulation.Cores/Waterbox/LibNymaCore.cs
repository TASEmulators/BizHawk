using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;

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

		[BizImport(CC, Compatibility = true)]
		public abstract bool Init([In]InitData data);

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
			public int SkipRendering;
			/// <summary>
			/// a single command to run at the start of this frame
			/// </summary>
			public CommandType Command;
			/// <summary>
			/// raw data for each input port, assumed to be MAX_PORTS * MAX_PORT_DATA long
			/// </summary>
			public byte* InputPortData;
		}

		[BizImport(CC)]
		public abstract byte* GetLayerData();

		/// <summary>
		/// Gets a string array of valid layers to pass to SetLayers, or null if that method should not be called
		/// TODO: This needs to be in NymaCore under a monitor lock
		/// </summary>
		public string[] GetLayerDataReal()
		{
			var p = GetLayerData();
			if (p == null)
				return null;
			var ret = new List<string>();
			var q = p;
			while (true)
			{
				if (*q == 0)
				{
					if (q > p)
						ret.Add(Marshal.PtrToStringAnsi((IntPtr)p));
					else
						break;
					p = ++q;
				}
				q++;
			}
			return ret.ToArray();
		}

		/// <summary>
		/// Set enabled layers (or is it disabled layers?).  Only call if GetLayerDataReal() returned non null
		/// </summary>
		/// <param name="layers">bitmask in order defined by GetLayerDataReal</param>
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
		public class NPortInfos
		{
			[MarshalAs(UnmanagedType.LPArray, SizeConst = 16)]
			public NPortInfo[] Infos;
		}
		[StructLayout(LayoutKind.Sequential)]
		public class NPortInfo
		{
			public string ShortName;
			public string FullName;
			public string DefaultDeviceShortName;

			[StructLayout(LayoutKind.Sequential)]
			public class NDeviceInfo
			{
				public string ShortName;
				public string FullName;
				public string Description;
				public DeviceFlags Flags;
				public uint ByteLength;
				[StructLayout(LayoutKind.Sequential)]
				public class NInput
				{
					public string SettingName;
					public string Name;
					public short ConfigOrder;
					public ushort BitOffset;
					public InputType Type;
					public AxisFlags Flags;
					public byte BitSize;
					public byte _Padding;
					[StructLayout(LayoutKind.Sequential)]
					public class Button
					{
						public string ExcludeName;
					}
					[StructLayout(LayoutKind.Sequential)]
					public class Axis
					{
						public string SettingsNameNeg;
						public string SettingsNamePos;
						public string NameNeg;
						public string NamePos;
					}
					[StructLayout(LayoutKind.Sequential)]
					public class Switch
					{
						[StructLayout(LayoutKind.Sequential)]
						public class Position
						{
							public string SettingName;
							public string Name;
							public string Description;
						}
						[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
						public Position[] Positions;
						public uint NumPositions;
						public uint DefaultPosition;
					}
					[StructLayout(LayoutKind.Sequential)]
					public class Status
					{
						public class State
						{
							public IntPtr ShortName;
							public IntPtr Name;
							public int Color; // (msb)0RGB(lsb), -1 for unused.
							public int _Padding;
						}
						[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
						public State[] States;
						public uint NumStates;
					}
					[MarshalAs(UnmanagedType.ByValArray, SizeConst = 400)]
					public byte[] UnionData;
				}
				[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
				public NInput[] Inputs;
			}
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
			public NDeviceInfo[] Devices;
		}

		[BizImport(CC, Compatibility = true)]
		public abstract NPortInfos GetInputDevices();

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
		public class SystemInfo
		{
			public int MaxWidth;
			public int MaxHeight;
			public int NominalWidth;
			public int NominalHeight;
			public VideoSystem VideoSystem;
			public int FpsFixed;
		}

		[BizImport(CC, Compatibility = true)]
		public abstract SystemInfo GetSystemInfo();
	}
}
