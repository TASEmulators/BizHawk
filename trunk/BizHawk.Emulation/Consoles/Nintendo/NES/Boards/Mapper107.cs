namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper107 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_32k, chr_bank_mask_8k;

		//state
		int prg_bank_32k, chr_bank_8k;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_bank_32k", ref prg_bank_32k);
			ser.Sync("chr_bank_8k", ref chr_bank_8k);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER107":
					AssertPrg(128); AssertChr(64); AssertWram(8); AssertVram(0); AssertBattery(false);
					break;
				default:
					return false;
			}

			prg_bank_mask_32k = (Cart.prg_size / 32) - 1;
			chr_bank_mask_8k = (Cart.chr_size / 8) - 1;

			SetMirrorType(Cart.pad_h, Cart.pad_v);

			return true;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int ofs = addr & ((1 << 13) - 1);
				addr = (chr_bank_8k << 13) | ofs;
				return VROM[addr];
			}
			else return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			int ofs = addr & ((1 << 15) - 1);
			addr = (prg_bank_32k << 15) | ofs;
			return ROM[addr];
		}

		public override void WritePRG(int addr, byte value)
		{
			chr_bank_8k = value;
			prg_bank_32k = value >> 1;
			chr_bank_8k &= chr_bank_mask_8k;
			prg_bank_32k &= prg_bank_mask_32k;
		}

	}
}
