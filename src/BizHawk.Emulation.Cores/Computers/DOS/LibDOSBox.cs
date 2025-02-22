using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public abstract class LibDOSBox : LibWaterboxCore
	{

		public const int VGA_MAX_WIDTH = 640;
		public const int VGA_MAX_HEIGHT = 480;
		public const int SVGA_MAX_WIDTH = 800;
		public const int SVGA_MAX_HEIGHT = 600;

		public const int VIDEO_NUMERATOR_PAL = 102237;
		public const int VIDEO_DENOMINATOR_PAL = 2048;
		// libretro defines PUAE_VIDEO_HZ_NTSC as 59.8260993957519531f
		public const int VIDEO_NUMERATOR_NTSC = 299130497;
		public const int VIDEO_DENOMINATOR_NTSC = 5000000;

		public const int FASTMEM_AUTO = -1;
		public const int MAX_FLOPPIES = 4;
		public const int FILENAME_MAXLENGTH = 64;
		public const int KEY_COUNT = 0x65;


		public const byte MouseButtonsMask =
		(byte) (AllButtons.Button_1
		| AllButtons.Button_2
		| AllButtons.Button_3);
		public const byte JoystickMask =
			(byte) (AllButtons.Up
			| AllButtons.Down
			| AllButtons.Left
			| AllButtons.Right
			| AllButtons.Button_1
			| AllButtons.Button_2
			| AllButtons.Button_3);

		[BizImport(CC, Compatibility = true)]
		public abstract bool Init();

		[BizImport(CC)]
		public abstract void SetLEDCallback(EmptyCallback callback);

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public KeyBuffer Keys;
			public struct KeyBuffer
			{
				public unsafe fixed byte Buffer[KEY_COUNT];
			}
			public DriveActions driveActions;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DriveActions
		{
			public int insertFloppyDisk;
			public int insertCDROM;
			public int insertHardDiskDrive;
		}

		[Flags]
		public enum AllButtons : short
		{
			Up       = 0b0000000000000001,
			Down     = 0b0000000000000010,
			Left     = 0b0000000000000100,
			Right    = 0b0000000000001000,
			Button_1 = 0b0000000000010000,
			Button_2 = 0b0000000000100000,
			Button_3 = 0b0000000001000000,
			Play     = 0b0000000010000000,
			Rewind   = 0b0000000100000000,
			Forward  = 0b0000001000000000,
			Green    = 0b0000010000000000,
			Yellow   = 0b0000100000000000,
			Red      = 0b0001000000000000,
			Blue     = 0b0010000000000000
		}

		// Follows enumeration in DOSBox-x
		// DOSBox-x/source/base/core/include/keyboard.h
		public enum DOSBoxKeyboard : int
		{
			KEY_1 = 1,
			KEY_2,
			KEY_3,
			KEY_4,
			KEY_5,
			KEY_6,
			KEY_7,
			KEY_8,
			KEY_9,
			KEY_0,
			KEY_Q,
			KEY_W,
			KEY_E,
			KEY_R,
			KEY_T,
			KEY_Y,
			KEY_U,
			KEY_I,
			KEY_O,
			KEY_P,
			KEY_A,
			KEY_S,
			KEY_D,
			KEY_F,
			KEY_G,
			KEY_H,
			KEY_J,
			KEY_K,
			KEY_L,
			KEY_Z,
			KEY_X,
			KEY_C,
			KEY_V,
			KEY_B,
			KEY_N,
			KEY_M,
			KEY_F1,
			KEY_F2,
			KEY_F3,
			KEY_F4,
			KEY_F5,
			KEY_F6,
			KEY_F7,
			KEY_F8,
			KEY_F9,
			KEY_F10,
			KEY_F11,
			KEY_F12,
			KEY_Escape,
			KEY_Tab,
			KEY_Backspace,
			KEY_Enter,
			KEY_Space,
			KEY_LeftAlt,
			KEY_RightAlt,
			KEY_LeftCtrl,
			KEY_RightCtrl,
			KEY_LeftShift,
			KEY_RightShift,
			KEY_CapsLock,
			KEY_ScrollLock,
			KEY_NumLock,
			KEY_Grave,
			KEY_Minus,
			KEY_Equals,
			KEY_Backslash,
			KEY_LeftBracket,
			KEY_RightBracket,
			KEY_Semicolon,
			KEY_Quote,
			KEY_Period,
			KEY_Comma,
			KEY_Slash,
			KEY_ExtraLtGt,
			KEY_PrintScreen,
			KEY_Pause,
			KEY_Insert,
			KEY_Home,
			KEY_Pageup,
			KEY_Delete,
			KEY_End,
			KEY_Pagedown,
			KEY_Left,
			KEY_Up,
			KEY_Down,
			KEY_Right,
			KEY_Keypad1,
			KEY_Keypad2,
			KEY_KeyPad3,
			KEY_KeyPad4,
			KEY_KeyPad5,
			KEY_KeyPad6,
			KEY_KeyPad7,
			KEY_KeyPad8,
			KEY_KeyPad9,
			KEY_KeyPad0,
			KEY_KeyPadDivide,
			KEY_KeyPadMultiply,
			KEY_KeyPadMinus,
			KEY_keyPadPlus,
			KEY_KeyPadEnter,
			KEY_KeyPadPeriod
		}
	}
}