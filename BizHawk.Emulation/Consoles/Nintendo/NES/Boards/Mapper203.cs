using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper203 : NES.NESBoardBase
	{
		/*
		Here are Disch's original notes:  
		 ========================
		 =  Mapper 203          =
		 ========================
 
		 Example Games:
		 --------------------------
		 35-in-1
 
 
		 Registers:
		 ---------------------------
 
 
		   $8000-FFFF:  [PPPP PPCC]
			 P = PRG Reg
			 C = CHR Reg
 
 
		 CHR Setup:
		 ---------------------------
 
			   $0000   $0400   $0800   $0C00   $1000   $1400   $1800   $1C00 
			 +---------------------------------------------------------------+
			 |                             $8000                             |
			 +---------------------------------------------------------------+
 
 
		 PRG Setup:
		 ---------------------------
 
			  $8000   $A000   $C000   $E000  
			 +---------------+---------------+
			 |     $8000     |     $8000     |
			 +---------------+---------------+
		*/

		int prg_reg_16k, chr_reg_8k;
		int prg_bank_mask_16k;
		int chr_bank_mask_8k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER203":
					break;
				default:
					return false;
			}

			prg_bank_mask_16k = Cart.prg_size / 16 - 1;
			chr_bank_mask_8k = Cart.chr_size / 8 - 1;

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_reg_16k", ref prg_reg_16k);
			ser.Sync("chr_reg_8k", ref chr_reg_8k);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			prg_reg_16k = (value >> 2) & prg_bank_mask_16k;
			chr_reg_8k = (value & 0x03) & chr_bank_mask_8k;
		}

		public override byte ReadPRG(int addr)
		{
			return ROM[(prg_reg_16k * 0x4000) + (addr & 0x3FFF)];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[(chr_reg_8k * 0x2000) + addr];
			}
			return base.ReadPPU(addr);
		}
	}
}
