namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA half of mapper 034 (the other half is AVE_NINA_001 which is entirely different..)
	class BxROM : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_32k;

		//state
		int prg_bank_32k;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_bank_32k", ref prg_bank_32k);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "IREM-BNROM": //Mashou (J).nes
				case "NES-BNROM": //Deadly Towers (U)
					AssertPrg(128); AssertChr(0); AssertWram(0); AssertVram(8);
					break;

				default:
					return false;
			}

			prg_bank_mask_32k = Cart.prg_size / 32 - 1;

			SetMirrorType(Cart.pad_h, Cart.pad_v);

			return true;
		}

		public override byte ReadPRG(int addr)
		{
			addr |= (prg_bank_32k << 15);
			return ROM[addr];
		}

		public override void WritePRG(int addr, byte value)
		{
			value = HandleNormalPRGConflict(addr, value);
			prg_bank_32k = value & prg_bank_mask_32k;
		}

	}
}