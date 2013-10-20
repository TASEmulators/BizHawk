namespace BizHawk.Emulation.Consoles.Nintendo
{
	public sealed class NovelDiamond : NES.NESBoardBase
	{
		int prg;
		int chr;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER054": // ??
				case "UNIF_BMC-NovelDiamond9999999in1": // works
					break;
				default:
					return false;
			}
			AssertPrg(128);
			AssertChr(64);
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[addr | chr << 13];
			else
				return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr | prg << 15];
		}

		public override void WritePRG(int addr, byte value)
		{
			prg = addr & 3;
			chr = addr & 7;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg", ref prg);
			ser.Sync("chr", ref chr);
		}
	}
}
