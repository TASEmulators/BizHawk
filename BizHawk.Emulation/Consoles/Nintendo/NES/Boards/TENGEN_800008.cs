namespace BizHawk.Emulation.Consoles.Nintendo
{
	// tetris (unl)
	// behaves identically to CNROM for the one board it is on, but supports more (64K prg, 64K chr)
	// http://kevtris.org/mappers/tengen/800008.html
	public class TENGEN_800008: NES.NESBoardBase
	{
		int prg_mask;
		int chr_mask;
		int prg;
		int chr;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "TENGEN-800008":
					AssertPrg(32, 64);
					AssertChr(8, 16, 32, 64);
					AssertWram(0);
					AssertVram(0);
					break;
				default:
					return false;
			}

			prg_mask = (Cart.prg_size / 32) - 1;
			chr_mask = (Cart.chr_size / 8) - 1;

			return true;
		}

		public override void WritePRG(int addr, byte value)
		{
			value = HandleNormalPRGConflict(addr, value);
			prg = value >> 3 & prg_mask;
			chr = value & chr_mask;
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr | prg << 15];
		}
		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[addr | chr << 13];
			else
				return base.ReadPPU(addr);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
			ser.Sync("chr", ref chr);
		}
	}
}
