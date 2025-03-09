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
		public abstract bool Init(int port1Type, int port2Type);

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public JoystickButtons joystick1;
			public JoystickButtons joystick2;
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

	}
}