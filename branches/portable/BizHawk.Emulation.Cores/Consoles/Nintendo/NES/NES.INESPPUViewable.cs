using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	partial class NES : INESPPUViewable
	{
		public int[] GetPalette()
		{
			return palette_compiled;
		}

		public bool BGBaseHigh
		{
			get { return ppu.reg_2000.bg_pattern_hi; }
		}

		public bool SPBaseHigh
		{
			get { return ppu.reg_2000.obj_pattern_hi; }
		}

		public bool SPTall
		{
			get { return ppu.reg_2000.obj_size_16; }
		}

		public byte[] GetPPUBus()
		{
			byte[] ret = new byte[0x3000];
			for (int i = 0; i < 0x3000; i++)
			{
				ret[i] = ppu.ppubus_peek(i);
			}
			return ret;
		}

		public byte[] GetPalRam()
		{
			return ppu.PALRAM;
		}

		public byte[] GetOam()
		{
			return ppu.OAM;
		}

		public byte PeekPPU(int addr)
		{
			return Board.PeekPPU(addr);
		}

		public byte[] GetExTiles()
		{
			if (Board is ExROM)
			{
				return Board.VROM ?? Board.VRAM;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public bool ExActive
		{
			get { return Board is ExROM && (Board as ExROM).ExAttrActive; }
		}

		public byte[] GetExRam()
		{
			if (Board is ExROM)
			{
				return (Board as ExROM).GetExRAMArray();
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public MemoryDomain GetCHRROM()
		{
			return _memoryDomains["CHR VROM"];
		}


		public void InstallCallback1(Action cb, int sl)
		{
			ppu.NTViewCallback = new PPU.DebugCallback { Callback = cb, Scanline = sl };
		}

		public void InstallCallback2(Action cb, int sl)
		{
			ppu.PPUViewCallback = new PPU.DebugCallback { Callback = cb, Scanline = sl };
		}

		public void RemoveCallback1()
		{
			ppu.NTViewCallback = null;
		}

		public void RemoveCallback2()
		{
			ppu.PPUViewCallback = null;
		}
	}
}
