using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper200 : NES.NESBoardBase
	{
		/*
		Here are Disch's original notes:  
		========================
		=  Mapper 200          =
		========================
 
		Example Games:
		--------------------------
		1200-in-1
		36-in-1
 
 
		Registers:
		---------------------------
 
 
		$8000-FFFF:  A~[.... ....  .... MRRR]
		M = Mirroring (0=Vert, 1=Horz)
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
		+---------------+---------------+
		|     $8000     |     $8000     |
		+---------------+---------------+
		*/
		int prg_reg_16k, chr_reg_8k;
		int prg_bank_mask_16k;
		int chr_bank_mask_8k;
		bool low;
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER200":
				case "MAPPER229":
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
			if (addr.Bit(3))
			{
				SetMirrorType(EMirrorType.Horizontal);
			}
			else
			{
				SetMirrorType(EMirrorType.Vertical);
			}
			int reg = addr & 0x07;
			prg_reg_16k = reg & prg_bank_mask_16k;
			chr_reg_8k = reg & chr_bank_mask_8k;
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x4000)
			{
				return ROM[(prg_reg_16k * 0x4000) + addr];
			}
			else
			{
				return ROM[(prg_reg_16k * 0x4000) + addr - 0x4000];
			}
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
