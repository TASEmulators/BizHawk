using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Consoles._3DO
{
	public abstract class LibOpera : LibWaterboxCore
	{
		// NTSC Specifications
		public const int NTSC_WIDTH = 320;
		public const int NTSC_HEIGHT = 240;
		public const int VIDEO_NUMERATOR_NTSC = 299130497;
		public const int VIDEO_DENOMINATOR_NTSC = 5000000;

		// PAL Specifications
		public const int PAL_WIDTH = 384;
		public const int PAL_HEIGHT = 288;
		public const int VIDEO_NUMERATOR_PAL = 102237;
		public const int VIDEO_DENOMINATOR_PAL = 2048;

		[BizImport(CC, Compatibility = true)]
		public abstract bool Init(string gameFile, string biosFile, string fontFile, int port1Type, int port2Type);

		[StructLayout(LayoutKind.Sequential)]
		public struct GamepadInputs
		{
			public int up;
			public int down;
			public int left;
			public int right;
			public int start;
			public int select;
			public int buttonA;
			public int buttonB;
			public int buttonX;
			public int buttonY;
			public int buttonL;
			public int buttonR;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MouseInputs
		{
			public int posX;
			public int posY;
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
			public int posX;
			public int posY;
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
		}
	}
}