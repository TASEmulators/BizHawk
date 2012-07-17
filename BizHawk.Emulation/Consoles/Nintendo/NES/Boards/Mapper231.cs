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
		public int reg;
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
			ser.Sync("reg", ref reg);
			ser.Sync("low", ref low);
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

			low = addr.Bit(5);
			reg = addr & 0x1E;
		}

		public override byte ReadPRG(int addr)
		{
			if (low)
			{
				int bank = ((reg >> 1) & 0x0F) & (prg_bank_mask_16k >> 1);
				return ROM[(bank * 0x8000) + addr];
			}
			else
			{
				return ROM[((reg & prg_bank_mask_16k) * 0x4000) + addr];
				
			}
		}
	}
}
