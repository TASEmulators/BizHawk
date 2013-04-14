namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper046 : NES.NESBoardBase
	{
		//Rumblestation 15-in-1 (Unl).nes

		/*
			Regs at $6000-7FFF means no PRG-RAM.

			$6000-7FFF:  [CCCC PPPP]   High CHR, PRG bits
			$8000-FFFF:  [.CCC ...P]   Low CHR, PRG bits
 
			'C' selects 8k CHR @ $0000
			'P' select 32k PRG @ $8000
		 */
		
		//configuration
		int prg_bank_mask_32k, chr_bank_mask_8k;

		//state
		int prg_bank_32k_H, prg_bank_32k_L,
			chr_bank_8k_H, chr_bank_8k_L;

		public override void WriteWRAM(int addr, byte value)
		{
			prg_bank_32k_H = value & 0x0F;
			chr_bank_8k_H = value >> 4;
		}

		public override void WritePRG(int addr, byte value)
		{
			prg_bank_32k_L = value & 0x01;
			chr_bank_8k_L = (value >> 4) & 0x07;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[addr + (((chr_bank_8k_H << 3) + chr_bank_8k_L) * 0x2000)];
			}
			else return base.ReadPPU(addr);
		}

		public override byte ReadPRG(int addr)
		{
			//TODO: High bits
			int offset = (prg_bank_32k_H << 1) + prg_bank_32k_L;
			return ROM[addr + (offset * 0x8000)];
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//configure
			switch (Cart.board_type)
			{
				case "MAPPER046":
					break;
				default:
					return false;
			}

			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			chr_bank_mask_8k = Cart.chr_size / 8 - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);

			prg_bank_32k_H = 0;
			prg_bank_32k_L = 0;
			chr_bank_8k_H = 0;
			chr_bank_8k_L = 0;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("prg_bank_32k_H", ref prg_bank_32k_H);
			ser.Sync("prg_bank_32k_L", ref prg_bank_32k_L);
			ser.Sync("chr_bank_8k_H", ref chr_bank_8k_H);
			ser.Sync("chr_bank_8k_L", ref chr_bank_8k_L);
		}
	}
}
