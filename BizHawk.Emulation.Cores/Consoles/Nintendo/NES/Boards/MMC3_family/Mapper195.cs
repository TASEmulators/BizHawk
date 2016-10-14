using System;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper195 : MMC3Board_Base
	{
		private int vram_bank_mask_1k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER195":
					break;
				default:
					return false;
			}

			vram_bank_mask_1k = Cart.vram_size / 1 - 1;
			
			BaseSetup();
			return true;
		}

		public override byte ReadEXP(int addr)
		{
			if (addr >= 0x1000)
			{
				return WRAM[addr-0x1000];
			}

			return base.ReadEXP(addr);
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr >= 0x1000)
			{
				WRAM[addr - 0x1000] = value;
			}
			
			base.WriteEXP(addr, value);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if (!mmc3.wram_enable || mmc3.wram_write_protect) return;
			base.WriteWRAM(addr+0x1000, value);
		}

		public override byte ReadWRAM(int addr)
		{
			if (!mmc3.wram_enable) return NES.DB;
			return base.ReadWRAM(addr+0x1000);
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
