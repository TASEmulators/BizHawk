namespace BizHawk.Emulation.Consoles.Nintendo
{
	public sealed class Mapper015 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_8k;

		//state
		ByteBuffer prg_banks_8k = new ByteBuffer(4);

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER015":
					break;
				default:
					return false;
			}
			prg_bank_mask_8k = (Cart.prg_size / 8) - 1;

			prg_banks_8k[0] = 0;
			prg_banks_8k[1] = 1;
			prg_banks_8k[2] = 2;
			prg_banks_8k[3] = 3;
			ApplyMemoryMapMask(prg_bank_mask_8k, prg_banks_8k);

			return true;
		}


		public override byte ReadPRG(int addr)
		{
			addr = ApplyMemoryMap(13, prg_banks_8k, addr);
			return ROM[addr];
		}

		public override void WritePRG(int addr, byte value)
		{
			int mode = addr & 3;
			int prg_high = value & 0x3F;
			bool prg_low = value.Bit(7);
			int prg_low_val = prg_low ? 1 : 0;
			bool mirror = value.Bit(6);
			SetMirrorType(mirror ? EMirrorType.Horizontal : EMirrorType.Vertical);

			switch(mode)
			{
				case 0:
					prg_banks_8k[0] = (byte)((prg_high * 2 + 0) ^ prg_low_val);
					prg_banks_8k[1] = (byte)((prg_high * 2 + 1) ^ prg_low_val);
					prg_banks_8k[2] = (byte)((prg_high * 2 + 2) ^ prg_low_val);
					prg_banks_8k[3] = (byte)((prg_high * 2 + 3) ^ prg_low_val);
					break;
				case 1:
					prg_banks_8k[0] = (byte)((prg_high*2+0) ^ prg_low_val);
					prg_banks_8k[1] = (byte)((prg_high*2+1) ^ prg_low_val);
					prg_banks_8k[2] = (byte)(0xFE);
					prg_banks_8k[3] = (byte)(0xFF);
					//maybe all 4?
					break;
				case 2:
					prg_banks_8k[0] = (byte)((prg_high * 2 + 0) ^ prg_low_val);
					prg_banks_8k[1] = (byte)((prg_high * 2 + 0) ^ prg_low_val);
					prg_banks_8k[2] = (byte)((prg_high * 2 + 0) ^ prg_low_val);
					prg_banks_8k[3] = (byte)((prg_high * 2 + 0) ^ prg_low_val);
					break;
				case 3:
					prg_banks_8k[0] = (byte)((prg_high * 2 + 0) ^ prg_low_val);
					prg_banks_8k[1] = (byte)((prg_high * 2 + 1) ^ prg_low_val);
					prg_banks_8k[2] = (byte)((prg_high * 2 + 0) ^ prg_low_val);
					prg_banks_8k[3] = (byte)((prg_high * 2 + 1) ^ prg_low_val);
					break;
			}

			ApplyMemoryMapMask(prg_bank_mask_8k, prg_banks_8k);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_banks_8k", ref prg_banks_8k);
		}
	}
}
