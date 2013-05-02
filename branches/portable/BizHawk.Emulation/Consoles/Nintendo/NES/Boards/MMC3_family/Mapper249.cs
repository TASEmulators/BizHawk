namespace BizHawk.Emulation.Consoles.Nintendo
{
	public class Mapper249 : MMC3Board_Base
	{
		bool piratecrap = false;

		// mmc3 with pirate crap bolt on
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER249":
					break;
				default:
					return false;
			}
			AssertPrg(256, 512);
			AssertChr(256);
			Cart.wram_size = 8;
			BaseSetup();
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("piratecrap", ref piratecrap);
		}

		public override void WriteEXP(int addr, byte value)
		{
			piratecrap = value.Bit(1);
		}

		protected override int Get_CHRBank_1K(int addr)
		{
			int v = base.Get_CHRBank_1K(addr);
			if (piratecrap)
				v = v & 3 | v >> 1 & 4 | v >> 4 & 8 | v >> 2 & 0x10 | v << 3 & 0x20 | v << 2 & 0xC0;
			return v;
		}

		protected override int Get_PRGBank_8K(int addr)
		{
			int v = base.Get_PRGBank_8K(addr);
			if (piratecrap)
			{
				if (v < 0x20)
					v = v & 1 | v >> 3 & 2 | v >> 1 & 4 | v << 2 & 8 | v << 2 & 0x10;
				else
				{
					v -= 0x20;
					v = v & 3 | v >> 1 & 4 | v >> 4 & 8 | v >> 2 & 0x10 | v << 3 & 0x20 | v << 2 & 0xC0;
				}
			}
			return v;
		}
	}
}
