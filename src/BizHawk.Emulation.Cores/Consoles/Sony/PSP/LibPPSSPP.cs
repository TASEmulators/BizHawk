using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using static BizHawk.Emulation.Cores.Computers.Doom.CInterface;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	public abstract class LibPPSSPP
	{
		private const CallingConvention CC = CallingConvention.Cdecl;

		[UnmanagedFunctionPointer(CC)]
		public delegate void CDReadCallback(int lba, IntPtr dst);

		[UnmanagedFunctionPointer(CC)]
		public delegate int CDSectorCountCallback();

		[BizImport(CC)]
		public abstract void SetCdCallbacks(CDReadCallback cdrc, CDSectorCountCallback cdscc);

		[BizImport(CC, Compatibility = true)]
		public abstract bool Init(string gameFile);

		[BizImport(CC, Compatibility = true)]
		public abstract void FrameAdvance(FrameInfo f);

		[BizImport(CC, Compatibility = true)]
		public abstract int GetStateSize();

		[BizImport(CC, Compatibility = true)]
		public abstract void SaveState(byte[] buffer);

		[BizImport(CC, Compatibility = true)]
		public abstract void LoadState(byte[] buffer, int stateLen);

		[BizImport(CC, Compatibility = true)]

		public abstract void Deinit();

		[BizImport(CC)]
		public abstract void GetVideo(int[] buffer);

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
		public class FrameInfo
		{
			public GamepadInputs input;
		}
	}
}