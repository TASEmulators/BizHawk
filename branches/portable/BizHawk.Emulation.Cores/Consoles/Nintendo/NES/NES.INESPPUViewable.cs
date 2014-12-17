using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

		public byte PeekPPU(int addr)
		{
			return board.PeekPPU(addr);
		}

		public byte[] GetExTiles()
		{
			if (board is ExROM)
			{
				return board.VROM ?? board.VRAM;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public bool ExActive
		{
			get { return board is ExROM && (board as ExROM).ExAttrActive; }
		}

		public byte[] GetExRam()
		{
			if (board is ExROM)
			{
				return (board as ExROM).GetExRAMArray();
			}
			else
			{
				throw new InvalidOperationException();
			}
		}
	}
}
