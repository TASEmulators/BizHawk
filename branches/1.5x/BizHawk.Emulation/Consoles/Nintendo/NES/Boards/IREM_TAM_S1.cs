namespace BizHawk.Emulation.Consoles.Nintendo
{
	//iNES Mapper 97
	//Kaiketsu Yanchamaru (Kid Niki 1)

	public sealed class IREM_TAM_S1 : NES.NESBoardBase
	{
		int prg_bank_mask_16k;
		byte prg_bank_16k;
		ByteBuffer prg_banks_16k = new ByteBuffer(2);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER097":
					break;
				case "IREM-TAM-S1":
					break;
				default:
					return false;
			}
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			prg_bank_mask_16k = (Cart.prg_size / 16) - 1;
			prg_banks_16k[0] = 0xFF;
			return true;
		}

		public override void Dispose()
		{
			prg_banks_16k.Dispose();
			base.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_bank_mask_16k", ref prg_bank_mask_16k);
			ser.Sync("prg_bank_16k", ref prg_bank_16k);
			ser.Sync("prg_banks_16k", ref prg_banks_16k);
		}

		void SyncPRG()
		{
			prg_banks_16k[1] = prg_bank_16k;
		}

		public override void WritePRG(int addr, byte value)
		{
			prg_bank_16k = (byte)(value & 15);
			SyncPRG();

			int m = value >> 6;
			switch (m)
			{
				case 0:
					SetMirrorType(EMirrorType.OneScreenA);
					break;
				case 1:
					SetMirrorType(EMirrorType.Horizontal);
					break;
				case 2:
					SetMirrorType(EMirrorType.Vertical);
					break;
				case 3:
					SetMirrorType(EMirrorType.OneScreenB);
					break;
			}
		}

		public override byte ReadPRG(int addr)
		{
			int bank_16k = addr >> 14;
			int ofs = addr & ((1 << 14) - 1);
			bank_16k = prg_banks_16k[bank_16k];
			bank_16k &= prg_bank_mask_16k;
			addr = (bank_16k << 14) | ofs;
			return ROM[addr];
		}
	}
}
