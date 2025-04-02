using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Consoles.Sony.PSP
{
	public abstract class LibPPSSPP : LibWaterboxCore
	{
		[UnmanagedFunctionPointer(CC)]
		public delegate void CDReadCallback(int lba, IntPtr dst);

		[UnmanagedFunctionPointer(CC)]
		public delegate int CDSectorCountCallback();

		[BizImport(CC)]
		public abstract void SetCdCallbacks(CDReadCallback cdrc, CDSectorCountCallback cdscc);

		[BizImport(CC, Compatibility = true)]
		public abstract bool Init(string gameFile);

		[StructLayout(LayoutKind.Sequential)]
		public struct GamepadInputs
		{
			public int Up;
			public int Down;
			public int Left;
			public int Right;
			public int Start;
			public int Select;
			public int ButtonSquare;
			public int ButtonTriangle;
			public int ButtonCircle;
			public int ButtonCross;
			public int ButtonLTrigger;
			public int ButtonRTrigger;
			public int RightAnalogX;
			public int RightAnalogY;
			public int LeftAnalogX;
			public int LeftAnalogY;
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public GamepadInputs input;
		}
	}
}