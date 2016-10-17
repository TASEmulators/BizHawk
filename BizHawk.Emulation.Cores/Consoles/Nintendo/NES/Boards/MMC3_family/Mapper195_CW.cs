using System;
// This is a modified 195 board specifically for Chaos World (CH)
// It is a Work in Progress
// Save Ram is broken and the game will not load with save ram written through this mapper
// This is also true on punes
// The game also requires 1 screen mirroring modes that other 195 games dont
// Although the game runs correctly with 195 CHR mapping, other aspects do not work
// More research is needed

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper195_CW : MMC3Board_Base
	{
		private int vram_bank_mask_1k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER195_CW":
					break;
				default:
					return false;
			}

			vram_bank_mask_1k = Cart.vram_size / 1 - 1;
			
			BaseSetup();

			mmc3.MirrorMask = 3;
			return true;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = Get_CHRBank_1K(addr);

				if (bank_1k<=3)
				{
					return VRAM[(bank_1k << 10) + (addr & 0x3FF)];
				}
				else
				{
					addr = MapCHR(addr);
					return VROM[addr + extra_vrom];
				}
			}
			else
				return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				int bank_1k = Get_CHRBank_1K(addr);

				if (bank_1k <= vram_bank_mask_1k)
				{
					VRAM[(bank_1k  << 10) + (addr & 0x3FF)] = value;
				}
				else
				{
					// nothing to write to VROM
				}
			}
			else 
				base.WritePPU(addr, value);
		}
	}
}
