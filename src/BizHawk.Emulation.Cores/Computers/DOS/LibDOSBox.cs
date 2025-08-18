using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public abstract class LibDOSBox : LibWaterboxCore
	{

		public const int VGA_MAX_WIDTH = 640;
		public const int VGA_MAX_HEIGHT = 480;
		public const int SVGA_MAX_WIDTH = 1024;
		public const int SVGA_MAX_HEIGHT = 768;

		// Default FPS: 70.086592427616921
		public const int DEFAULT_FRAMERATE_NUMERATOR_DOS = 3146888;
		public const int DEFAULT_FRAMERATE_DENOMINATOR_DOS = 44900;

		public const int FASTMEM_AUTO = -1;
		public const int MAX_FLOPPIES = 4;
		public const int FILENAME_MAXLENGTH = 64;
		public const int KEY_COUNT = 0x65;


		// CD Management Logic Start

		[StructLayout(LayoutKind.Sequential)]
		public class CDTrack
		{
			public int Offset;
			public int Start;
			public int End;
			public int Mode;
			public int LoopEnabled;
			public int LoopOffset;
		}

		public const int CD_MAX_TRACKS = 100;
		[StructLayout(LayoutKind.Sequential)]
		public class CDData
		{
			public int End;
			public int Last;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = CD_MAX_TRACKS)]
			public readonly CDTrack[] Tracks = new CDTrack[CD_MAX_TRACKS];
		}

		[UnmanagedFunctionPointer(CC)]
		public delegate void CDReadCallback(string cdRomName, int lba, IntPtr dst, int sectorSize);

		[BizImport(CC)]
		public abstract void SetCdCallbacks(CDReadCallback cdrc);

		[BizImport(CC)]
		public abstract void PushCDData(int cdIdx, int numSectors, int numTracks);

		[BizImport(CC)]
		public abstract void PushTrackData(int cdIdx, int trackId, CDTrack data);

		[BizImport(CC)]
		public abstract uint GetTicksElapsed();

		[BizImport(CC)]
		public abstract int GetHDDSize();

		[BizImport(CC)]
		public abstract void GetHDDData(byte[] buffer);

		[BizImport(CC)]
		public abstract void SetHDDData(byte[] buffer);


		[BizImport(CC)]
		public abstract int GetRefreshRateNumerator();

		[BizImport(CC)]
		public abstract int GetRefreshRateDenominator();

		[StructLayout(LayoutKind.Sequential)]
		public class InitSettings
		{
			public int Joystick1Enabled;
			public int Joystick2Enabled;
			public ulong HardDiskDriveSize;
		}

		// CD Management Logic END

		[BizImport(CC)]
		public abstract bool Init(InitSettings settings);

		[BizImport(CC)]
		public abstract bool GetDriveActivityFlag();

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public KeyBuffer Keys;
			public struct KeyBuffer
			{
				public unsafe fixed byte Buffer[KEY_COUNT];
			}
			public DriveActions DriveActions;
			public JoystickButtons Joystick1;
			public JoystickButtons Joystick2;
			public MouseInput Mouse;
			public int framerateNumerator;
			public int framerateDenominator;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DriveActions
		{
			public int InsertFloppyDisk;
			public int InsertCDROM;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct JoystickButtons
		{
			public int Up;
			public int Down;
			public int Left;
			public int Right;
			public int Button1;
			public int Button2;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MouseInput
		{
			public int PosX;
			public int PosY;
			public int DeltaX;
			public int DeltaY;
			public int LeftButtonPressed;
			public int MiddleButtonPressed;
			public int RightButtonPressed;
			public int LeftButtonReleased;
			public int MiddleButtonReleased;
			public int RightButtonReleased;
			public float Sensitivity;
		}

		// Follows enumeration in DOSBox-x
		// DOSBox-x/source/base/core/include/keyboard.h
		public enum DOSBoxKeyboard : int
		{
			Key_None = 0,

			Key_1,
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

			Key_KeyPad1,
			Key_KeyPad2,
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
			Key_KeyPadPlus,
			Key_KeyPadEnter,
			Key_KeyPadPeriod,

			// 45 more, not including "count" member
		}
	}
}
