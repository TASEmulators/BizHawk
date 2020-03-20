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

		public override byte ReadExp(int addr)
		{
			if (addr >= 0x1000)
			{
				return Wram[addr-0x1000];
			}

			return base.ReadExp(addr);
		}

		public override void WriteExp(int addr, byte value)
		{
			if (addr >= 0x1000)
			{
				Wram[addr - 0x1000] = value;
			}
			
			base.WriteExp(addr, value);
		}

		public override void WriteWram(int addr, byte value)
		{
			if (!mmc3.wram_enable || mmc3.wram_write_protect) return;
			base.WriteWram(addr+0x1000, value);
		}

		public override byte ReadWram(int addr)
		{
			if (!mmc3.wram_enable) return NES.DB;
			return base.ReadWram(addr+0x1000);
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = Get_CHRBank_1K(addr);

				if (bank_1k<=3)
				{
					return Vram[(bank_1k << 10) + (addr & 0x3FF)];
				}
				else
				{
					addr = MapCHR(addr);
					return Vrom[addr + extra_vrom];
				}
			}
			else
				return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				int bank_1k = Get_CHRBank_1K(addr);

				if (bank_1k <= vram_bank_mask_1k)
				{
					Vram[(bank_1k  << 10) + (addr & 0x3FF)] = value;
				}
				else
				{
					// nothing to write to VROM
				}
			}
			else 
				base.WritePpu(addr, value);
		}
	}
}
