namespace BizHawk.Emulation.Consoles.Nintendo
{
	class MLT_ACTION52 : NES.NESBoardBase
	{
		/*
		 Here are Disch's original notes:
		========================
		=  Mapper 228          =
		========================


		Example Games:
		--------------------------
		Action 52
		Cheetah Men II


		Notes:
		---------------------------
		Cheetah Men II is infamous for how freaking terrible it is.  Action 52 is none better.  These games are SO
		bad, it's hilarious.
 
		Action 52's PRG size is weird (not a power of 2 value).  This is because there are 3 seperate 512k PRG chips.
		PRG Setup section will cover details.


		Powerup and Reset:
		---------------------------
		Apparently the games expect $00 to be written to $8000 on powerup/reset.


		Registers:
		---------------------------

		$4020-4023:  [.... RRRR]  RAM  (readable/writable)
		(16 bits of RAM -- 4 bits in each of the 4 regs)
		$4024-5FFF:    mirrors $4020-4023

		$8000-FFFF:    [.... ..CC]   Low 2 bits of CHR
		A~[..MH HPPP PPO. CCCC]

		M = Mirroring (0=Vert, 1=Horz)
		H = PRG Chip Select
		P = PRG Page Select
		O = PRG Mode
		C = High 4 bits of CHR

		CHR Setup:
		---------------------------

		$0000   $0400   $0800   $0C00   $1000   $1400   $1800   $1C00 
		+---------------------------------------------------------------+
		|                             $8000                             |
		+---------------------------------------------------------------+


		PRG Setup:
		---------------------------

		'H' bits select the PRG chip.  Each chip is 512k in size.  Chip 2 does not exist, and when selected, will
		result in open bus.  The Action 52 .nes ROM file contains chips 0, 1, and 3:

		chip 0:  offset 0x000010
		chip 1:  offset 0x080010
		chip 2:  -- non existant --
		chip 3:  offset 0x100010

		'P' selects the PRG page on the currently selected chip.

		$8000   $A000   $C000   $E000  
					+-------------------------------+
		PRG Mode 0: |            <$8000>            |
					+-------------------------------+
		PRG Mode 1: |     $8000     |     $8000     |
					+---------------+---------------+
		*/
		public bool prg_mode;
		public int prg_reg;
		public int chr_reg;
		public int chip_offset;
		public bool cheetahmen = false;
		ByteBuffer eRAM = new ByteBuffer(4);
		int chr_bank_mask_8k, prg_bank_mask_16k, prg_bank_mask_32k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER228":
				case "MLT-ACTION52":
					break;
				default:
					return false;
			}

			chr_bank_mask_8k = Cart.chr_size / 8 - 1;
			prg_bank_mask_16k = Cart.prg_size / 16 - 1;
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;

			if (Cart.prg_size == 256)
			{
				cheetahmen = true;
			}

			prg_mode = false;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_reg", ref prg_reg);
			ser.Sync("chr_reg", ref chr_reg);
			ser.Sync("prg_mode", ref prg_mode);
			ser.Sync("chip", ref chip_offset);
			ser.Sync("eRAM", ref eRAM);
			base.SyncState(ser);
		}

		public override void Dispose()
		{
			eRAM.Dispose();
			base.Dispose();
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

		public override void WritePRG(int addr, byte value)
		{
			//$8000-FFFF:    [.... ..CC]   Low 2 bits of CHR
			//A~[..MH HPPP PPO. CCCC]

			if (addr.Bit(13))
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}

			prg_mode = addr.Bit(5);
			prg_reg = (addr >> 6) & 0x1F;
			chr_reg = ((addr & 0x0F) << 2) | (value & 0x03);
			if (!cheetahmen)
			{
				int chip = ((addr >> 11) & 0x03);
				switch (chip)
				{
					case 0:
						chip_offset = 0;
						break;
					case 1:
						chip_offset = 0x80000;
						break;
					case 2:
						break; //TODO: this chip doesn't exist and should access open bus
					case 3:
						chip_offset = 0x100000;
						break;
				}
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

		public override byte ReadPRG(int addr)
		{
			if (prg_mode == false)
			{
				int bank = (prg_reg >> 1) & prg_bank_mask_32k;
				return ROM[(bank * 0x8000) + addr + chip_offset];
			}
			else
			{
				return ROM[((prg_reg & prg_bank_mask_16k) * 0x4000) + (addr & 0x3FFF) + chip_offset];
			}
		}
	}
}
