namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper057 : NES.NESBoardBase
	{
		/*
		Here are Disch's original notes:  
		========================
		=  Mapper 057          =
		========================

		Example Games:
		--------------------------
		GK 47-in-1
		6-in-1 (SuperGK)


		Registers:
		---------------------------

		Range,Mask:   $8000-FFFF, $8800

		$8000:  [.H.. .AAA]
		H = High bit of CHR reg (bit 4)
		A = Low 3 bits of CHR Reg (OR with 'B' bits)

		$8800:  [PPPO MBBB]
		P = PRG Reg
		O = PRG Mode
		M = Mirroring (0=Vert, 1=Horz)
		B = Low 3 bits of CHR Reg (OR with 'A' bits)


		CHR Setup:
		---------------------------
		'A' and 'B' bits combine with an OR to get the low 3 bits of the desired page, and the 'H' bit is the high
		bit.  This 4-bit value selects an 8k page @ $0000


		PRG Setup:
		---------------------------

					  $8000   $A000   $C000   $E000  
					+---------------+---------------+
		PRG Mode 0: |     $8800     |     $8800     |
					+-------------------------------+
		PRG Mode 1: |            <$8800>            |
					+-------------------------------+
		*/

		bool prg_mode = false;
		int chr_reg_low_0, chr_reg_low_1, chr_reg;
		int prg_reg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER057":
					break;
				default:
					return false;
			}

			SetMirrorType(EMirrorType.Horizontal);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_reg", ref prg_reg);
			ser.Sync("chr_reg", ref chr_reg);
			ser.Sync("chr_reg_low_0", ref chr_reg);
			ser.Sync("chr_reg_low_1", ref chr_reg);
			ser.Sync("prg_mode", ref prg_mode);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr &= 0x8800;
			if (addr == 0)
			{
				chr_reg_low_0 = value & 0x07;
				chr_reg &= 0x08;
				chr_reg |= (value & 0x40) >> 3;
			}
			else if(addr == 0x800)
			{
				prg_reg = (value >> 5) & 0x07;
				prg_mode = value.Bit(4);
				chr_reg_low_1 = (value & 0x07);

				if (addr.Bit(3))
				{
					SetMirrorType(EMirrorType.Horizontal);
				}
				else
				{
					SetMirrorType(EMirrorType.Vertical);
				}
			}
			
			chr_reg &= ~0x07;
			chr_reg |= (chr_reg_low_0 | chr_reg_low_1);

			//Console.WriteLine("chr page = {0}", chr_reg);
		}

		public override byte ReadPRG(int addr)
		{
			if (prg_mode)
			{
				return ROM[((prg_reg >> 1) * 0x8000) + addr];
			}
			else
			{
				return ROM[(prg_reg * 0x4000) + (addr & 0x3FFF)];
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[(chr_reg * 0x2000) + addr];
			}
			return base.ReadPPU(addr);
		}
	}
}
