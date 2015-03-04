using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class VBANext : IGBAGPUViewable
	{
		public GBAGPUMemoryAreas GetMemoryAreas()
		{
			var s = new LibVBANext.MemoryAreas();
			LibVBANext.GetMemoryAreas(Core, s);
			return new GBAGPUMemoryAreas
			{
				mmio = s.mmio,
				oam = s.oam,
				palram = s.palram,
				vram = s.vram
			};
		}

		public void SetScanlineCallback(Action callback, int scanline)
		{
			if (scanline < 0 || scanline > 227)
			{
				throw new ArgumentOutOfRangeException("scanline", "Scanline must be in [0, 227]!");
			}
			if (callback == null)
			{
				scanlinecb = null;
				LibVBANext.SetScanlineCallback(Core, scanlinecb, 0);
			}
			else
			{
				scanlinecb = new LibVBANext.StandardCallback(callback);
				LibVBANext.SetScanlineCallback(Core, scanlinecb, scanline);
			}
		}

		private LibVBANext.StandardCallback scanlinecb;
	}
}
