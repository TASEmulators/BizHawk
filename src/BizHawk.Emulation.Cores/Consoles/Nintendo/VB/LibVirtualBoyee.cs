using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;
using System.Drawing;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.VB
{
	public abstract class LibVirtualBoyee : LibWaterboxCore
	{
		public enum Buttons : int
		{
			Up = 0x200,
			Down = 0x100,
			Left = 0x80,
			Right = 0x40,
			Select = 0x800,
			Start = 0x400,
			B = 0x2,
			A = 0x1,
			Up_R = 0x10,
			Down_R = 0x200,
			Left_R = 0x1000,
			Right_R = 0x2000,
			L = 0x8,
			R = 0x4
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public Buttons Buttons;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class NativeSyncSettings
		{
			public int InstantReadHack;
			public int DisableParallax;

			public static NativeSyncSettings FromFrontendSettings(VirtualBoyee.SyncSettings ss)
			{
				return new NativeSyncSettings
				{
					InstantReadHack = ss.InstantReadHack ? 1 : 0,
					DisableParallax = ss.DisableParallax ? 1 : 0,
				};
			}
		}

		public class NativeSettings
		{
			public int ThreeDeeMode;
			public int SwapViews;
			public int AnaglyphPreset;
			public int AnaglyphCustomLeftColor;
			public int AnaglyphCustomRightColor;
			public int NonAnaglyphColor;
			public int LedOnScale;
			public int InterlacePrescale;
			public int SideBySideSeparation;

			private static int ConvertColor(Color c)
			{
				return c.ToArgb();
			}

			public static NativeSettings FromFrontendSettings(VirtualBoyee.Settings s)
			{
				return new NativeSettings
				{
					ThreeDeeMode = (int)s.ThreeDeeMode,
					SwapViews = s.SwapViews ? 1 : 0,
					AnaglyphPreset = (int)s.AnaglyphPreset,
					AnaglyphCustomLeftColor = ConvertColor(s.AnaglyphCustomLeftColor),
					AnaglyphCustomRightColor = ConvertColor(s.AnaglyphCustomRightColor),
					NonAnaglyphColor = ConvertColor(s.NonAnaglyphColor),
					LedOnScale = s.LedOnScale,
					InterlacePrescale = s.InterlacePrescale,
					SideBySideSeparation = s.SideBySideSeparation
				};
			}
		}

		[BizImport(CC)]
		public abstract bool Load(byte[] rom, int length, NativeSyncSettings settings);

		[BizImport(CC)]
		public abstract void SetSettings(NativeSettings settings);

		[BizImport(CC)]
		public abstract void HardReset();

		[BizImport(CC)]
		public abstract void PredictFrameSize([In, Out]FrameInfo frame);
	}
}
