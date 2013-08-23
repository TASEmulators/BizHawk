namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper201 : NES.NESBoardBase
	{
		/*
			Here are Disch's original notes:  
		========================
		=  Mapper 201          =
		========================

		Example Games:
		--------------------------
		8-in-1
		21-in-1 (2006-CA) (Unl)


		Registers:
		---------------------------


		$8000-FFFF:  A~[.... ....  RRRR RRRR]
		R = PRG/CHR Reg


		CHR Setup:
		---------------------------

		$0000   $0400   $0800   $0C00   $1000   $1400   $1800   $1C00 
		+---------------------------------------------------------------+
		|                             $8000                             |
		+---------------------------------------------------------------+


		PRG Setup:
		---------------------------

		$8000   $A000   $C000   $E000  
		+-------------------------------+
		|             $8000             |
		+-------------------------------+
		*/

		public int reg;
		public int prg_bank_mask_32k;
		public int chr_bank_mask_8k;
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER201":
					break;
				default:
					return false;
			}
			SetMirrorType(EMirrorType.Vertical);
			prg_bank_mask_32k = Cart.prg_size / 32 - 1;
			chr_bank_mask_8k = Cart.chr_size / 8 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("reg", ref reg);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			if ((addr & 0x08) > 0)
			{
				reg = addr & 0x03;
			}
			else
			{
				reg = 0;
			}
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[((reg & prg_bank_mask_32k) * 0x8000) + addr];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[((reg & chr_bank_mask_8k) * 0x2000) + addr];
			}
			return base.ReadPPU(addr);
		}
	}
}
