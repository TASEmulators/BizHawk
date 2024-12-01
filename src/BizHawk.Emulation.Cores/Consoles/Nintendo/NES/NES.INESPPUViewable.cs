using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	partial class NES : INESPPUViewable
	{
		public int[] GetPalette() => palette_compiled;

		public bool BGBaseHigh => ppu.reg_2000.bg_pattern_hi;

		public bool SPBaseHigh => ppu.reg_2000.obj_pattern_hi;

		public bool SPTall => ppu.reg_2000.obj_size_16;

		public byte[] GetPPUBus()
		{
			byte[] ret = new byte[0x3000];
			for (int i = 0; i < 0x3000; i++)
			{
				ret[i] = ppu.ppubus_peek(i);
			}
			return ret;
		}

		public byte[] GetPalRam() => ppu.PALRAM;

		public byte[] GetOam() => ppu.OAM;

		public byte PeekPPU(int addr) => Board.PeekPPU(addr);

		public byte[] GetExTiles()
		{
			if (Board is ExROM)
			{
				return Board.Vrom ?? Board.Vram;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public bool ExActive => Board is ExROM ex && ex.ExAttrActive;

		public byte[] GetExRam()
		{
			if (Board is ExROM ex)
			{
				return ex.GetExRAMArray();
			}

			throw new InvalidOperationException();
		}

		public MemoryDomain GetCHRROM() => _memoryDomains["CHR VROM"];


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
