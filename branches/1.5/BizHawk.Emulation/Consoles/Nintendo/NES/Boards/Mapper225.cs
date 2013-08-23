namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper225 : NES.NESBoardBase
	{
		/*
		Here are Disch's original notes:  
		========================
		=  Mapper 225          =
		========================

		Example Games:
		--------------------------
		52 Games
		58-in-1
		64-in-1


		Registers:
		---------------------------

		$5800-5803:  [.... RRRR]  RAM  (readable/writable)
		(16 bits of RAM -- 4 bits in each of the 4 regs)
		$5804-5FFF:    mirrors $5800-5803

		$8000-FFFF:  A~[.HMO PPPP PPCC CCCC]
		H = High bit (acts as bit 7 for PRG and CHR regs)
		M = Mirroring (0=Vert, 1=Horz)
		O = PRG Mode
		P = PRG Reg
		C = CHR Reg


		CHR Setup:
		---------------------------

					  $0000   $0400   $0800   $0C00   $1000   $1400   $1800   $1C00 
					+---------------------------------------------------------------+
		CHR Mode 0: |                             $8000                             |
					+---------------------------------------------------------------+


		PRG Setup:
		---------------------------

					  $8000   $A000   $C000   $E000  
					+-------------------------------+
		PRG Mode 0: |            <$8000>            |
					+-------------------------------+
		PRG Mode 1: |     $8000     |     $8000     |
					+---------------+---------------+
		*/

		bool prg_mode = false;
		int chr_reg;
		int prg_reg;
		ByteBuffer eRAM = new ByteBuffer(4);
		int chr_bank_mask_8k, prg_bank_mask_16k, prg_bank_mask_32k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER225":
					break;
				default:
					return false;
			}
			chr_bank_mask_8k = Cart.chr_size / 8 - 1;
			prg_bank_mask_16k = Cart.prg_size / 16 - 1;
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;

			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_reg", ref prg_reg);
			ser.Sync("chr_reg", ref chr_reg);
			ser.Sync("prg_mode", ref prg_mode);
			ser.Sync("eRAM", ref eRAM);
			base.SyncState(ser);
		}

		public override void Dispose()
		{
			eRAM.Dispose();
			base.Dispose();
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000;
			prg_mode = addr.Bit(12);
			if (addr.Bit(13))
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}

			int high = (addr & 0x4000) >> 8;
			prg_reg = (addr >> 6) & 0x3F | high;
			chr_reg = addr & 0x3F | high;
		}

		public override byte ReadPRG(int addr)
		{
			if (prg_mode == false)
			{
				int bank = (prg_reg >> 1) & prg_bank_mask_32k;
				return ROM[(bank * 0x8000) + addr];
			}
			else
			{
				return ROM[((prg_reg & prg_bank_mask_16k) * 0x4000) + (addr & 0x3FFF)];
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[((chr_reg & chr_bank_mask_8k) * 0x2000) + addr];
			}
			return base.ReadPPU(addr);
		}

		public override void WriteEXP(int addr, byte value)
		{
			if (addr >= 0x1800)
			{
				eRAM[(addr & 0x07)] = (byte)(value & 0x0F);
			}
		}

		public override byte ReadEXP(int addr)
		{
			if (addr >= 0x1800)
			{
				return eRAM[(addr & 0x07)];
			}
			else
			{
				return base.ReadEXP(addr);
			}
		}
	}
}
