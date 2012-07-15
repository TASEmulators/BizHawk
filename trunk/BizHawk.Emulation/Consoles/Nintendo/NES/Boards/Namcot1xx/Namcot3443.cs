using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	/*
	Here are Disch's original notes:  
	========================
	=  Mapper 088          =
	========================


	Example Games:
	--------------------------
	Quinty (J)
	Namcot Mahjong 3
	Dragon Spirit - Aratanaru Densetsu


	Registers:
	---------------------------

	Range,Mask:   $8000-FFFF, $8001

	$8000:  [.... .AAA]  Address for use with $8001


	$8001:  [DDDD DDDD]
	Data port:
	R:0 ->  CHR reg 0
	R:1 ->  CHR reg 1
	R:2 ->  CHR reg 2
	R:3 ->  CHR reg 3
	R:4 ->  CHR reg 4
	R:5 ->  CHR reg 5
	R:6 ->  PRG reg 0
	R:7 ->  PRG reg 1

	CHR Setup:
	---------------------------
 
	CHR is split into two halves.  $0xxx can only have CHR from the first 64k, $1xxx can only have CHR from the
	second 64k.

		* 
	$0000   $0400   $0800   $0C00   $1000   $1400   $1800   $1C00 
	+---------------+---------------+-------+-------+-------+-------+
	|     <R:0>     |     <R:1>     |  R:2  |  R:3  |  R:4  |  R:5  |
	+---------------+---------------+-------+-------+-------+-------+
	|                               |                               |
	|  AND written values with $3F  |  OR written values with $40   |


	PRG Setup:
	---------------------------

	$8000   $A000   $C000   $E000  
	+-------+-------+-------+-------+
	|  R:6  |  R:7  | { -2} | { -1} |
	+-------+-------+-------+-------+
	*/
	class Namcot3443 : Namcot108Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NAMCOT-3443":
				case "NAMCOT-3433": //TODO: just a guess, find a ROM with this
				case "MAPPER088":	//TODO: just a guess, find a ROM with this
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		int RewireCHR(int addr)
		{
			//int mapper_addr = addr >> 1;
			//int bank_1k = mapper.Get_CHRBank_1K(mapper_addr + 0x1000);
			//int ofs = addr & ((1 << 11) - 1);
			//return (bank_1k << 11) + ofs;
			if (addr < 0x1000)
			{
				int mapper_addr = addr >> 1;
				int bank_1k = mapper.Get_CHRBank_1K(mapper_addr);
				int ofs = addr & ((1 << 10) - 1);
				int bob = (bank_1k << 11) + ofs;
				return bob & 0x0F;
			}
			else
			{
				return addr ; //TODO
			}
		}

		public override byte ReadPPU(int addr)
		{
			//if (addr < 0x2000) return VROM[Get_CHRBank_1K(addr)];
			if (addr < 0x2000) return VROM[RewireCHR(addr)];
			else return base.ReadPPU(addr);
		}
		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000) { }
			else base.WritePPU(addr, value);
		}
	}
}
