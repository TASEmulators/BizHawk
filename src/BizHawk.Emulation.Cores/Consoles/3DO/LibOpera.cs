using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Consoles._3DO
{
	public abstract class LibOpera : LibWaterboxCore
	{

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