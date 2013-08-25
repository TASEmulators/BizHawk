namespace BizHawk.Emulation.Consoles.Nintendo
{
	//Mapper 86
	
	//Example Games:
	//--------------------------
	//Moero!! Pro Yakyuu (Black)
	//Moero!! Pro Yakyuu (Red)

	public sealed class JALECO_JF_13 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_32k;
		int chr_bank_mask_8k;

		//state
		int chr;
		int prg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER086":
					break;
				case "JALECO-JF-13":
					break;
				default:
					return false;
			}

			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			chr_bank_mask_8k = Cart.chr_size / 8 - 1;

			SetMirrorType(Cart.pad_h, Cart.pad_v);

			return true;
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x8000)
				return ROM[addr + (prg * 0x8000)];
			else
				return base.ReadPRG(addr);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
				return VROM[(addr & 0x1FFF) + (chr * 0x2000)];
			else
				return base.ReadPPU(addr);
		}

		public override void WriteWRAM(int addr, byte value)
		{
			switch (addr & 0x1000)
			{
				case 0x0000:
					prg = (value >> 4) & 3;
					prg &= prg_bank_mask_32k;
					chr = (value & 3) + ((value >> 4) & 0x04);
					chr &= chr_bank_mask_8k;
					break;
				case 0x1000:
					//sound regs
					break;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("chr", ref chr);
			ser.Sync("prg", ref prg);
		}
	}
}
