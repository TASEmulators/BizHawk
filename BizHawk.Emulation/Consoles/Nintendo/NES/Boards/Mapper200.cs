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
		int reg;
		int prg_bank_mask_16k;
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

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("reg", ref reg);
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
			reg = addr & 0x07;
			low = addr.Bit(0);
		}

		public override byte ReadPRG(int addr)
		{
			if (addr < 0x4000)
			{
				return ROM[(reg * 0x4000) + addr];
			}
			else
			{
				int bank = reg >> 1;
				return ROM[(bank * 0x4000) + addr];
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				return VROM[(reg * 0x2000) + addr];
			}
			return base.ReadPPU(addr);
		}
	}
}
