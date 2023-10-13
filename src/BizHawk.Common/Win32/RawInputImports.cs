#nullable enable

using System;
using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global

namespace BizHawk.Common
{
	public static class RawInputImports
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUTDEVICE
		{
			public HidUsagePage usUsagePage;
			public HidUsageId usUsage;
			public RIDEV dwFlags;
			public IntPtr hwndTarget;

			public enum HidUsagePage : ushort
			{
				GENERIC = 1,
				GAME = 5,
				LED = 8,
				BUTTON = 9,
			}

			public enum HidUsageId : ushort
			{
				GENERIC_POINTER = 1,
				GENERIC_MOUSE = 2,
				GENERIC_JOYSTICK = 4,
				GENERIC_GAMEPAD = 5,
				GENERIC_KEYBOARD = 6,
				GENERIC_KEYPAD = 7,
				GENERIC_MULTI_AXIS_CONTROLLER = 8,
			}

			[Flags]
			public enum RIDEV : int
			{
				REMOVE = 0x00000001,
				EXCLUDE = 0x00000010,
				PAGEONLY = 0x00000020,
				NOLEGACY = PAGEONLY | EXCLUDE,
				INPUTSINK = 0x00000100,
				CAPTUREMOUSE = 0x00000200,
				NOHOTKEYS = CAPTUREMOUSE,
				APPKEYS = 0x00000400,
				EXINPUTSINK = 0x00001000,
				DEVNOTIFY = 0x00002000,
			}
		}

		public enum RID : uint
		{
			HEADER = 0x10000005,
			INPUT = 0x10000003,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUTHEADER
		{
			public RIM_TYPE dwType;
			public uint dwSize;
			public IntPtr hDevice;
			public IntPtr wParam;

			public enum RIM_TYPE : uint
			{
				MOUSE = 0,
				KEYBOARD = 1,
				HID = 2,
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWMOUSE
		{
			public ushort usFlags;
			public uint ulButtons;
			public uint ulRawButtons;
			public int lLastX;
			public int lLastY;
			public uint ulExtraInformation;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWKEYBOARD
		{
			public ushort MakeCode;
			public RIM_KEY Flags;
			public ushort Reserved;
			public ushort VKey;
			public uint Message;
			public uint ExtraInformation;

			public enum RIM_KEY : ushort
			{
				MAKE = 0,
				BREAK = 1,
				E0 = 2,
				E1 = 3,
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWHID
		{
			public uint dwSizeHid;
			public uint dwCount;
			public byte bRawData;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct RAWINPUTDATA
		{
			[FieldOffset(0)]
			public RAWMOUSE mouse;
			[FieldOffset(0)]
			public RAWKEYBOARD keyboard;
			[FieldOffset(0)]
			public RAWHID hid;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUT
		{
			public RAWINPUTHEADER header;
			public RAWINPUTDATA data;
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern unsafe IntPtr DefRawInputProc(RAWINPUT* paRawInput, int nInput, int cbSizeHeader);

		[DllImport("user32.dll")]
		public static extern int GetRawInputData(IntPtr hRawInput, RID uiCommand, IntPtr pData, out int bSize, int cbSizeHeader);

		[DllImport("user32.dll")]
		public static extern unsafe int GetRawInputData(IntPtr hRawInput, RID uiCommand, RAWINPUT* pData, ref int bSize, int cbSizeHeader);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, int cbSize);
	}
}
