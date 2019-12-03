using BizHawk.Common.BizInvoke;
using BizHawk.Emulation.Cores.Consoles.Sega.Saturn;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.NEC.PCFX
{
	public abstract class LibTst : LibWaterboxCore
	{
		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public uint Port1Buttons;
			public uint Port2Buttons;
			public uint ConsoleButtons;
		}

		[Flags]
		public enum Layers : int
		{
			None = 0,
			BG0 = 1,
			BG1 = 2,
			BG2 = 4,
			BG3 = 8,
			VDCA_BG = 16,
			VDCA_SPR = 32,
			VDCB_BG = 64,
			VDCB_SPR = 128,
			RAINBOW = 256
		}

		[StructLayout(LayoutKind.Sequential)]
		public class FrontendSettings
		{
			public int AdpcmEmulateBuggyCodec;
			public int AdpcmSuppressChannelResetClicks;
			public int HiResEmulation;
			public int DisableSpriteLimit;
			public int ChromaInterpolation;
			public int ScanlineStart;
			public int ScanlineEnd;
			public int CdSpeed;
			public int CpuEmulation;
			public int Port1;
			public int Port2;
			public int PixelPro;
		}

		[BizImport(CC)]
		public abstract void SetCDCallbacks(LibSaturnus.CDTOCCallback toccallback,
			LibSaturnus.CDSectorCallback sectorcallback);

		[BizImport(CC)]
		public abstract bool Init(int numDisks, byte[] bios);

		[BizImport(CC)]
		public abstract void EnableLayers(Layers mask);

		[BizImport(CC)]
		public abstract void PutSettingsBeforeInit(FrontendSettings s);
	}
}
