namespace BizHawk.Emulation.Consoles.Nintendo
{
	// one of the VS unisystem mappers
	// a lot of dumps are labelled incorrectly
	public class Mapper099 : NES.NESBoardBase
	{
		int chr;
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER099":
					AssertPrg(32); AssertChr(16); Cart.vram_size = 0; Cart.wram_size = 0;
					break;
				default:
					return false;
			}
			return true;
		}

		public void Signal4016(int val)
		{
			chr = val & 1;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[addr | chr << 13];
			else
				return base.ReadPPU(addr);
		}
	}
}
