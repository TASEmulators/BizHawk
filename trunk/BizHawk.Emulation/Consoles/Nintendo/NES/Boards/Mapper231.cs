using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper231 : NES.NESBoardBase
	{
		/*
		* Here are Disch's original notes:  
		========================
		=  Mapper 231          =
		========================
 
		Example Game:
		--------------------------
		20-in-1
 
 
 
		Registers:
		---------------------------
 
		$8000-FFFF:     A~[.... .... M.LP PPP.]
		M = Mirroring (0=Vert, 1=Horz)
		L = Low bit of PRG
		P = High bits of PRG
 
 
 
		PRG Setup:
		---------------------------
 
		Note that 'L' and 'P' bits make up the PRG reg, and the 'L' is the low bit.
 
 
		$8000   $A000   $C000   $E000  
		+---------------+---------------+
		| $8000 AND $1E |     $8000     |
		+---------------+---------------+
		*/
		public int prg_reg;
		public bool low;
		public int prg_bank_mask_16k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER231":
					break;
				default:
					return false;
			}

			prg_bank_mask_16k = Cart.prg_size / 16 - 1;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_reg", ref prg_reg);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			if (addr.Bit(7))
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}

			int prg_reg_P = (addr >> 1) & 0xF;
			int prg_reg_L = (addr >> 5) & 1;
			prg_reg = (prg_reg_P<<1) | prg_reg_L;
			prg_reg &= prg_bank_mask_16k;
		}

		public override byte ReadPRG(int addr)
		{
			if (low)
			{
				int bank = prg_reg & 0x1E;
				return ROM[(bank << 14) + addr];
			}
			else
			{
				int bank = prg_reg;
				return ROM[(bank << 14) + addr - 0x4000];
				
			}
		}
	}
}
