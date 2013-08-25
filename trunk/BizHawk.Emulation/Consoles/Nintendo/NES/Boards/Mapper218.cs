namespace BizHawk.Emulation.Consoles.Nintendo
{
	// rewires pins to use internal CIRAM as both nametable and pattern data, so
	// the entire cart is just a single PRGROM chip (plus CIC)
	public sealed class Mapper218 : NES.NESBoardBase
	{
		//configuration
		int prg_byte_mask;
		int chr_addr_mask;
				
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER218":
					// the cart actually has 0k vram, but due to massive abuse of the ines format, is labeled as 8k
					// supposed vram is (correctly) not used in our implementation
					AssertPrg(8, 16, 32); AssertChr(0); /*AssertVram(0);*/ AssertWram(0);
					Cart.vram_size = 0; // force vram size 0
					break;
				default:
					return false;
			}

			// due to massive abuse of the ines format, the mirroring and 4 screen bits have slightly different meanings
			switch (Cart.inesmirroring)
			{
				case 1: // VA10 to PA10
					// pattern: ABABABAB
					// nametable: ABAB (vertical)
					chr_addr_mask = 1 << 10;
					break;
				case 0: // VA10 to PA11
					// pattern: AABBAABB
					// nametable: AABB (horizontal)
					chr_addr_mask = 1 << 11;
					break;
				case 2: // VA10 to PA12
					// pattern: AAAABBBB
					// nametable: AAAA (one screen A)
					chr_addr_mask = 1 << 12;
					break;
				case 3: // VA10 to PA13
					// pattern: AAAAAAAA
					// nametable: BBBB (one screen B)
					chr_addr_mask = 1 << 13;
					break;
				default:
					// we need an ines identification for correct mirroring
					return false;
			}
			prg_byte_mask = (Cart.prg_size*1024) - 1;
			return true;
		}

		int TransformPPU(int addr)
		{
			if ((addr & chr_addr_mask) != 0)
				addr = addr & 0x3ff | 0x400;
			else
				addr = addr & 0x3ff;
			return addr;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x3f00)
				return NES.CIRAM[TransformPPU(addr)];
			else
				// palettes only
				return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x3f00)
				NES.CIRAM[TransformPPU(addr)] = value;
			else
				// palettes only
				base.WritePPU(addr, value);
		}

		public override byte ReadPRG(int addr)
		{
			addr &= prg_byte_mask;
			return ROM[addr];
		}
	}
}
