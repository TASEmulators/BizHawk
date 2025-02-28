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

		public const int VIDEO_NUMERATOR_DOS = 350000000;
		public const int VIDEO_DENOMINATOR_DOS = 5000000;

		public const int FASTMEM_AUTO = -1;
		public const int MAX_FLOPPIES = 4;
		public const int FILENAME_MAXLENGTH = 64;
		public const int KEY_COUNT = 0x65;


		[BizImport(CC, Compatibility = true)]
		public abstract bool Init(bool joystick1Enabled, bool joystick2Enabled, bool mouseEnabled, ulong hardDiskDriveSize);

		[BizImport(CC, Compatibility = true)]

		public abstract bool sram_changed();

		[BizImport(CC, Compatibility = true)]
		public abstract int get_sram_size();

		[BizImport(CC, Compatibility = true)]
		public abstract void get_sram(IntPtr sramBuffer);

		[BizImport(CC, Compatibility = true)]
		public abstract void set_sram(IntPtr sramBuffer);

		[BizImport(CC, Compatibility = true)]
		public abstract bool getDriveActivityFlag();

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public KeyBuffer Keys;
			public struct KeyBuffer
			{
				public unsafe fixed byte Buffer[KEY_COUNT];
			}
			public DriveActions driveActions;
			public JoystickButtons joystick1;
			public JoystickButtons joystick2;
			public MouseInput mouse;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DriveActions
		{
			public int insertFloppyDisk;
			public int insertCDROM;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct JoystickButtons
		{
			public int up;
			public int down;
			public int left;
			public int right;
			public int button1;
			public int button2;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MouseInput
		{
			public int posX;
			public int posY;
			public int leftButton;
			public int middleButton;
			public int rightButton;
			public float sensitivity;
		}

		// Follows enumeration in DOSBox-x
		// DOSBox-x/source/base/core/include/keyboard.h
		public enum DOSBoxKeyboard : int
		{
			Key_1 = 1,
			Key_2,
			Key_3,
			Key_4,
			Key_5,
			Key_6,
			Key_7,
			Key_8,
			Key_9,
			Key_0,
			Key_Q,
			Key_W,
			Key_E,
			Key_R,
			Key_T,
			Key_Y,
			Key_U,
			Key_I,
			Key_O,
			Key_P,
			Key_A,
			Key_S,
			Key_D,
			Key_F,
			Key_G,
			Key_H,
			Key_J,
			Key_K,
			Key_L,
			Key_Z,
			Key_X,
			Key_C,
			Key_V,
			Key_B,
			Key_N,
			Key_M,
			Key_F1,
			Key_F2,
			Key_F3,
			Key_F4,
			Key_F5,
			Key_F6,
			Key_F7,
			Key_F8,
			Key_F9,
			Key_F10,
			Key_F11,
			Key_F12,
			Key_Escape,
			Key_Tab,
			Key_Backspace,
			Key_Enter,
			Key_Space,
			Key_LeftAlt,
			Key_RightAlt,
			Key_LeftCtrl,
			Key_RightCtrl,
			Key_LeftShift,
			Key_RightShift,
			Key_CapsLock,
			Key_ScrollLock,
			Key_NumLock,
			Key_Grave,
			Key_Minus,
			Key_Equals,
			Key_Backslash,
			Key_LeftBracket,
			Key_RightBracket,
			Key_Semicolon,
			Key_Quote,
			Key_Period,
			Key_Comma,
			Key_Slash,
			Key_ExtraLtGt,
			Key_PrintScreen,
			Key_Pause,
			Key_Insert,
			Key_Home,
			Key_Pageup,
			Key_Delete,
			Key_End,
			Key_Pagedown,
			Key_Left,
			Key_Up,
			Key_Down,
			Key_Right,
			Key_Keypad1,
			Key_Keypad2,
			Key_KeyPad3,
			Key_KeyPad4,
			Key_KeyPad5,
			Key_KeyPad6,
			Key_KeyPad7,
			Key_KeyPad8,
			Key_KeyPad9,
			Key_KeyPad0,
			Key_KeyPadDivide,
			Key_KeyPadMultiply,
			Key_KeyPadMinus,
			Key_keyPadPlus,
			Key_KeyPadEnter,
			Key_KeyPadPeriod
		}
	}
}