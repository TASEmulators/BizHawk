namespace BizHawk.Emulation.Consoles.Nintendo
{
	public sealed class Mapper178 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_32k;

		//state
		ByteBuffer prg_banks_32k = new ByteBuffer(1);
		int reg4802;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER178":
					break;
				default:
					return false;
			}
			prg_bank_mask_32k = (Cart.prg_size / 32) - 1;

			prg_banks_32k[0] = 0;

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void WriteEXP(int addr, byte value)
		{
			switch (addr)
			{
				case 0x0800: //$4800
					SetMirrorType(value.Bit(0) ? EMirrorType.Horizontal : EMirrorType.Vertical);
					break;
				case 0x0801: //$4801
					{
						int reg4801 = (value >> 1) & 0xF;
						int prg = reg4801 + (reg4802 << 2);
						prg_banks_32k[0] = (byte)prg;
						ApplyMemoryMapMask(prg_bank_mask_32k, prg_banks_32k);
						break;
					}
				case 0x0802: //$4802
					reg4802 = value;
					break;
			}
			}

		public override byte ReadPRG(int addr)
		{
			addr = ApplyMemoryMap(15, prg_banks_32k, addr);
			return ROM[addr];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_banks_32k", ref prg_banks_32k);
			ser.Sync("reg4802", ref reg4802);
		}
	}
}
