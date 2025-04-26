using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Panasonic3DO
{
	public abstract class LibOpera : LibWaterboxCore
	{
		// NTSC Specifications
		public const int NTSC_WIDTH = 320;
		public const int NTSC_HEIGHT = 240;
		public const int NTSC_VIDEO_NUMERATOR = 60;
		public const int NTSC_VIDEO_DENOMINATOR = 1;

		// PAL1 Specifications
		public const int PAL1_WIDTH = 320;
		public const int PAL1_HEIGHT = 288;
		public const int PAL1_VIDEO_NUMERATOR = 50;
		public const int PAL1_VIDEO_DENOMINATOR = 1;

		// PAL2 Specifications
		public const int PAL2_WIDTH = 384;
		public const int PAL2_HEIGHT = 288;
		public const int PAL2_VIDEO_NUMERATOR = 50;
		public const int PAL2_VIDEO_DENOMINATOR = 1;

		[UnmanagedFunctionPointer(CC)]
		public delegate void CDReadCallback(int lba, IntPtr dst);

		[UnmanagedFunctionPointer(CC)]
		public delegate int CDSectorCountCallback();

		[BizImport(CC)]
		public abstract void SetCdCallbacks(CDReadCallback cdrc, CDSectorCountCallback cdscc);

		[BizImport(CC, Compatibility = true)]
		public abstract bool Init(string gameFile, string biosFile, string fontFile, int port1Type, int port2Type, int videoStandard);

		[BizImport(CC, Compatibility = true)]
		public abstract bool sram_changed();

		[BizImport(CC, Compatibility = true)]
		public abstract int get_sram_size();

		[BizImport(CC, Compatibility = true)]
		public abstract void get_sram(IntPtr sramBuffer);

		[BizImport(CC, Compatibility = true)]
		public abstract void set_sram(IntPtr sramBuffer);

		[StructLayout(LayoutKind.Sequential)]
		public struct GamepadInputs
		{
			public int up;
			public int down;
			public int left;
			public int right;
			public int buttonX;
			public int buttonP;
			public int buttonA;
			public int buttonB;
			public int buttonC;
			public int buttonL;
			public int buttonR;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MouseInputs
		{
			public int dX;
			public int dY;
			public int leftButton;
			public int middleButton;
			public int rightButton;
			public int fourthButton;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct FlightStickInputs
		{
			public int up;
			public int down;
			public int left;
			public int right;
			public int fire;
			public int buttonA;
			public int buttonB;
			public int buttonC;
			public int buttonX;
			public int buttonP;
			public int leftTrigger;
			public int rightTrigger;
			public int horizontalAxis;
			public int verticalAxis;
			public int altitudeAxis;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LightGunInputs
		{
			public int trigger;
			public int select;
			public int reload;
			public int isOffScreen;
			public int screenX;
			public int screenY;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ArcadeLightGunInputs
		{
			public int trigger;
			public int select;
			public int start;
			public int reload;
			public int auxA;
			public int isOffScreen;
			public int screenX;
			public int screenY;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct OrbatakTrackballInputs
		{
			public int dX;
			public int dY;
			public int startP1;
			public int startP2;
			public int coinP1;
			public int coinP2;
			public int service;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct GameInput
		{
			public GamepadInputs gamepad;
			public MouseInputs mouse;
			public FlightStickInputs flightStick;
			public LightGunInputs lightGun;
			public ArcadeLightGunInputs arcadeLightGun;
			public OrbatakTrackballInputs orbatakTrackball;
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public GameInput port1;
			public GameInput port2;
			public int isReset = 0;
		}
	}
}
