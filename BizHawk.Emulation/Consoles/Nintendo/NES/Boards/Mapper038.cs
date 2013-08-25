namespace BizHawk.Emulation.Consoles.Nintendo
{
	// Crime Busters (Brazil) (Unl)
	public sealed class Mapper038 : NES.NESBoardBase
	{
		//configuraton
		int prg_mask, chr_mask;
		//state
		int prg, chr;
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER038":
				case "UNL-PCI556":
					break;
				default:
					return false;
			}
			AssertPrg(128);
			AssertChr(32);
			AssertVram(0);
			AssertWram(0);
			prg_mask = Cart.prg_size / 32 - 1;
			chr_mask = Cart.chr_size / 8 - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			return true;
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[addr + (prg << 15)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[addr + (chr << 13)];
			}
			else return base.ReadPPU(addr);
		}

		void writereg(byte value)
		{
			prg = value & 3 & prg_mask;
			chr = (value >> 2) & 3 & chr_mask;
		}

		// the standard way to access this register is at 7000:7fff, but due to
		// hardware design, f000:ffff also works
		public override void WritePRG(int addr, byte value)
		{
			//if ((addr & 0x7000) == 0x7000)
			//	writereg(value);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			if ((addr & 0x1000) == 0x1000)
				writereg(value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
			ser.Sync("prg", ref prg);
		}
	}
}
