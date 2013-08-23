namespace BizHawk.Emulation.Consoles.Nintendo
{
	//AKA mapper 75
	public class VRC1 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_8k;
		int chr_bank_mask_4k;

		//state
		IntBuffer prg_banks_8k = new IntBuffer(4);
		IntBuffer chr_banks_4k = new IntBuffer(2);
		int[] chr_regs_4k = new int[2];

		public override void Dispose()
		{
			base.Dispose();
			prg_banks_8k.Dispose();
			chr_banks_4k.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_banks_8k", ref prg_banks_8k);
			ser.Sync("chr_banks_4k", ref chr_banks_4k);
			for (int i = 0; i < 2; i++) ser.Sync("chr_regs_4k_" + i, ref chr_regs_4k[i]);

			if (ser.IsReader)
			{
				SyncCHR();
			}
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER075":
					break;
				case "KONAMI-VRC-1":
					AssertPrg(128); AssertChr(128); AssertVram(0); AssertWram(0);
					break;
				default:
					return false;
			}

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_4k = Cart.chr_size / 4 - 1;

			SetMirrorType(EMirrorType.Vertical);

			prg_banks_8k[3] = (byte)(0xFF & prg_bank_mask_8k);

			return true;
		}
		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_banks_8k[bank_8k];
			addr = (bank_8k << 13) | ofs;
			return ROM[addr];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_4k = addr >> 12;
				int ofs = addr & ((1 << 12) - 1);
				bank_4k = chr_banks_4k[bank_4k];
				bank_4k &= chr_bank_mask_4k;
				addr = (bank_4k << 12) | ofs;
				return VROM[addr];
			}
			else return base.ReadPPU(addr);
		}

		void SyncCHR()
		{
			chr_banks_4k[0] = chr_regs_4k[0] & chr_bank_mask_4k;
			chr_banks_4k[1] = chr_regs_4k[1] & chr_bank_mask_4k;
		}

		public override void WritePRG(int addr, byte value)
		{
			switch (addr)
			{
				case 0x0000: prg_banks_8k[0] = (value & 0xF) & prg_bank_mask_8k; break;
				case 0x2000: prg_banks_8k[1] = (value & 0xF) & prg_bank_mask_8k; break;
				case 0x4000: prg_banks_8k[2] = (value & 0xF) & prg_bank_mask_8k; break;

				case 0x1000: //[.... .BAM]   Mirroring, CHR reg high bits
					if(value.Bit(0))
						SetMirrorType(NES.NESBoardBase.EMirrorType.Horizontal);
					else
						SetMirrorType(NES.NESBoardBase.EMirrorType.Vertical);
					chr_regs_4k[0] &= 0x0F;
					chr_regs_4k[1] &= 0x0F;
					if (value.Bit(1)) chr_regs_4k[0] |= 0x10;
					if (value.Bit(2)) chr_regs_4k[1] |= 0x10;
					SyncCHR();
					break;

				case 0x6000:
					chr_regs_4k[0] = (chr_regs_4k[0] & 0xF0) | (value & 0x0F);
					SyncCHR();
					break;
				case 0x7000:
					chr_regs_4k[1] = (chr_regs_4k[1] & 0xF0) | (value & 0x0F);
					SyncCHR();
					break;

			}
		}


	}
}