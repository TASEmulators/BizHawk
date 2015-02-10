using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class GBA : IGBAGPUViewable
	{
		public GBAGPUMemoryAreas GetMemoryAreas()
		{
			IntPtr _vram = LibMeteor.libmeteor_getmemoryarea(LibMeteor.MemoryArea.vram);
			IntPtr _palram = LibMeteor.libmeteor_getmemoryarea(LibMeteor.MemoryArea.palram);
			IntPtr _oam = LibMeteor.libmeteor_getmemoryarea(LibMeteor.MemoryArea.oam);
			IntPtr _mmio = LibMeteor.libmeteor_getmemoryarea(LibMeteor.MemoryArea.io);

			if (_vram == IntPtr.Zero || _palram == IntPtr.Zero || _oam == IntPtr.Zero || _mmio == IntPtr.Zero)
				throw new Exception("libmeteor_getmemoryarea() failed!");

			return new GBAGPUMemoryAreas
			{
				vram = _vram,
				palram = _palram,
				oam = _oam,
				mmio = _mmio
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
				LibMeteor.libmeteor_setscanlinecallback(null, 0);
			}
			else
			{
				scanlinecb = new LibMeteor.ScanlineCallback(callback);
				LibMeteor.libmeteor_setscanlinecallback(scanlinecb, scanline);
			}
		}

		private LibMeteor.ScanlineCallback scanlinecb = null;
	}
}
