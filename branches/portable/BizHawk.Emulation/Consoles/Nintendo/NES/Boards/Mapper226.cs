using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper226 : NES.NESBoardBase
	{
		/*
		 *  Here are Disch's original notes:  
		 ========================
		 =  Mapper 226          =
		 ========================
 
		 Example Games:
		 --------------------------
		 76-in-1
		 Super 42-in-1
 
 
		 Registers:
		 ---------------------------
 
		 Range, Mask:  $8000-FFFF, $8001
 
		   $8000:  [PMOP PPPP]
			  P = Low 6 bits of PRG Reg
			  M = Mirroring (0=Horz, 1=Vert)
			  O = PRG Mode
 
		   $8001:  [.... ...H]
			  H = high bit of PRG
 
 
		 PRG Setup:
		 ---------------------------
 
		 Low 6 bits of the PRG Reg come from $8000, high bit comes from $8001
 
 
						$8000   $A000   $C000   $E000  
					  +-------------------------------+
		 PRG Mode 0:  |             <Reg>             |
					  +-------------------------------+
		 PRG Mode 1:  |      Reg      |      Reg      |
					  +---------------+---------------+
		*/

		public int prg_page;
		public bool prg_mode;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER226":
					break;
				default:
					return false;
			}
			prg_page = 0;
			prg_mode = false;
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_page", ref prg_page);
			ser.Sync("prg_mode", ref prg_mode);
			base.SyncState(ser);
		}

		public override void WritePRG(int addr, byte value)
		{
			addr &= 1;
			if (addr == 0)
			{
				prg_page &= ~0x3F;
				prg_page |= ((value & 0x1F) + ((value & 0x80) >> 2));
				prg_mode = value.Bit(5);

				if (value.Bit(6))
				{
					SetMirrorType(EMirrorType.Vertical);
				}
				else
				{
					SetMirrorType(EMirrorType.Horizontal);
				}
			}
			else if (addr == 1)
			{
				prg_page &= ~0x40;
				prg_page |= ((value & 0x1) << 6);
			}
		}
		
		public override byte ReadPRG(int addr)
		{
			if (prg_mode == false)
			{
				return ROM[((prg_page >> 1) * 0x8000) + addr];
			}
			else
			{
				return ROM[(prg_page * 0x4000) + (addr & 0x03FFF)];
			}
		}
	}
}
