using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper62 : NES.NESBoardBase
	{
		/*
			Here are Disch's original notes:  
		========================
		=  Mapper 062          =
		========================

		Example Game:
		--------------------------
		Super 700-in-1


		Registers:
		---------------------------

		$8000-FFFF:  A~[..pp pppp MPOC CCCC]
					[.... ..cc]
		p = Low bits of PRG Reg
		P = High bit of PRG Reg
		c = Low bits of CHR Reg
		C = High bits of CHR Reg
		O = PRG Mode
		M = Mirroring (0=Vert, 1=Horz)


		PRG Setup:
		---------------------------


					  $8000   $A000   $C000   $E000  
					+-------------------------------+
		PRG Mode 0: |            <$8000>            |
					+-------------------------------+
		PRG Mode 1: |     $8000     |     $8000     |
					+---------------+---------------+


		CHR Setup:
		----------------------------
		'C' and 'c' select an 8k page @ $0000
		*/
		bool prg_mode = false;
		int chr_reg;
		int prg_reg; 

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER062":
					break;
				default:
					return false;
			}
			
			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_reg", ref prg_reg);
			ser.Sync("chr_reg", ref chr_reg);
			ser.Sync("prg_mode", ref prg_mode);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			prg_mode = addr.Bit(5);
			if (addr.Bit(7))
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}

			prg_reg = (addr & 0x40) | ((addr >> 8) & 0x3F);
			chr_reg = ((addr & 0x1F) << 2) | (value & 0x03); 
		}

		public override byte ReadPRG(int addr)
		{
			if (prg_mode == false)
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
