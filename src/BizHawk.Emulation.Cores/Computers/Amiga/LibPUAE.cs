using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Computers.Amiga
{
	public abstract class LibPUAE : LibWaterboxCore
	{
		public const int PAL_WIDTH = 720;
		public const int PAL_HEIGHT = 576;
		public const int NTSC_WIDTH = 720;
		public const int NTSC_HEIGHT = 480;
		public const int FASTMEM_AUTO = -1;
		public const int MAX_FLOPPIES = 4;
		public const int FILENAME_MAXLENGTH = 64;
		public const int KEY_COUNT = 0x68;
		public const byte b00000001 = 1 << 0;
		public const byte b00000010 = 1 << 1;
		public const byte b00000100 = 1 << 2;
		public const byte b00001000 = 1 << 3;
		public const byte b00010000 = 1 << 4;
		public const byte b00100000 = 1 << 5;
		public const byte b01000000 = 1 << 6;
		public const byte b10000000 = 1 << 7;

		[BizImport(CC, Compatibility = true)]
		public abstract bool Init(int argc, string[] argv);

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public PUAEJoystick JoystickState;
			public byte MouseButtons;
			public int MouseX;
			public int MouseY;
			public KeyBuffer Keys;
			public struct KeyBuffer
			{
				public unsafe fixed byte Buffer[LibPUAE.KEY_COUNT];
			}
			public int CurrentDrive;
			public DriveAction Action;
			public FileName Name;
			public struct FileName
			{
				public unsafe fixed byte Buffer[LibPUAE.FILENAME_MAXLENGTH];
			}
		}

		public enum DriveAction : int
		{
			None,
			Eject,
			Insert
		}

		[Flags]
		public enum PUAEJoystick : byte
		{
			Joystick_Up       = b00000001,
			Joystick_Down     = b00000010,
			Joystick_Left     = b00000100,
			Joystick_Right    = b00001000,
			Joystick_Button_1 = b00010000,
			Joystick_Button_2 = b00100000,
			Joystick_Button_3 = b01000000
		}

		// https://wiki.amigaos.net/wiki/Keymap_Library
		public enum PUAEKeyboard : int
		{
			Key_Backquote      = 0x00,
			Key_1              = 0x01,
			Key_2              = 0x02,
			Key_3              = 0x03,
			Key_4              = 0x04,
			Key_5              = 0x05,
			Key_6              = 0x06,
			Key_7              = 0x07,
			Key_8              = 0x08,
			Key_9              = 0x09,
			Key_0              = 0x0A,
			Key_Minus          = 0x0B,
			Key_Equal          = 0x0C,
			Key_Backslash      = 0x0D,
//			Undefined          = 0x0E,
			Key_NP_0           = 0x0F,
			Key_Q              = 0x10,
			Key_W              = 0x11,
			Key_E              = 0x12,
			Key_R              = 0x13,
			Key_T              = 0x14,
			Key_Y              = 0x15,
			Key_U              = 0x16,
			Key_I              = 0x17,
			Key_O              = 0x18,
			Key_P              = 0x19,
			Key_Left_Bracket   = 0x1A,
			Key_Right_Bracket  = 0x1B,
//			Undefined          = 0x1C,
			Key_NP_1           = 0x1D,
			Key_NP_2           = 0x1E,
			Key_NP_3           = 0x1F,
			Key_A              = 0x20,
			Key_S              = 0x21,
			Key_D              = 0x22,
			Key_F              = 0x23,
			Key_G              = 0x24,
			Key_H              = 0x25,
			Key_J              = 0x26,
			Key_K              = 0x27,
			Key_L              = 0x28,
			Key_Semicolon      = 0x29,
			Key_Quote          = 0x2A,
			Key_Number_Sign    = 0x2B, // not on most USA keyboards
//			Undefined          = 0x2C,
			Key_NP_4           = 0x2D,
			Key_NP_5           = 0x2E,
			Key_NP_6           = 0x2F,
			Key_Less           = 0x30, // not on most USA keyboards
			Key_Z              = 0x31,
			Key_X              = 0x32,
			Key_C              = 0x33,
			Key_V              = 0x34,
			Key_B              = 0x35,
			Key_N              = 0x36,
			Key_M              = 0x37,
			Key_Comma          = 0x38,
			Key_Period         = 0x39,
			Key_Slash          = 0x3A,
//			Undefined          = 0x3B,
			Key_NP_Delete      = 0x3C,
			Key_NP_7           = 0x3D,
			Key_NP_8           = 0x3E,
			Key_NP_9           = 0x3F,
			Key_Space          = 0x40,
			Key_Backspace      = 0x41,
			Key_Tab            = 0x42,
			Key_NP_Enter       = 0x43,
			Key_Return         = 0x44,
			Key_Escape         = 0x45,
			Key_Delete         = 0x46,
//			Undefined          = 0x47,
//			Undefined          = 0x48,
//			Undefined          = 0x49,
			Key_NP_Sub         = 0x4A,
//			Undefined          = 0x4B,
			Key_Up             = 0x4C,
			Key_Down           = 0x4D,
			Key_Right          = 0x4E,
			Key_Left           = 0x4F,
			Key_F1             = 0x50,
			Key_F2             = 0x51,
			Key_F3             = 0x52,
			Key_F4             = 0x53,
			Key_F5             = 0x54,
			Key_F6             = 0x55,
			Key_F7             = 0x56,
			Key_F8             = 0x57,
			Key_F9             = 0x58,
			Key_F10            = 0x59,
			Key_NP_Left_Paren  = 0x5A,
			Key_NP_Right_Paren = 0x5B,
			Key_NP_Div         = 0x5C,
			Key_NP_Mul         = 0x5D,
			Key_NP_Add         = 0x5E,
			Key_Help           = 0x5F,
			Key_Left_Shift     = 0x60,
			Key_Right_Shift    = 0x61,
			Key_Caps_Lock      = 0x62,
			Key_Ctrl           = 0x63,
			Key_Left_Alt       = 0x64,
			Key_Right_Alt      = 0x65,
			Key_Left_Amiga     = 0x66,
			Key_Right_Amiga    = 0x67,
		}
	}
}