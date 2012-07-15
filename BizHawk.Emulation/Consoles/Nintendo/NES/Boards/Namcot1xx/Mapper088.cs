using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	/*
	Example Games:
	--------------------------
	Quinty (J)
	Namcot Mahjong 3
	Dragon Spirit - Aratanaru Densetsu
	
	This is the same as Mapper206, with the following exception:
	CHR support is increased to 128KB by connecting PPU's A12 line to the CHR ROM's A16 line.
	For example, masking the CHR ROM address output from the mapper by $FFFF, and then OR it with $10000 if the PPU address was >= $1000.
	Consequently, CHR is split into two halves. $0xxx can only have CHR from the first 64K, $1xxx can only have CHR from the second 64K.
	
	*/


	class Mapper088 : Namcot108Board_Base
	{
		//configuration
		int chr_byte_mask;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NAMCOT-3443":
				case "NAMCOT-3433": //TODO: just a guess, find a ROM with this
				case "MAPPER088":	
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirrorType(EMirrorType.Vertical);

			chr_byte_mask = (Cart.chr_size*1024) - 1;

			return true;
		}

		int RewireCHR(int addr)
		{
			int chrrom_addr = base.MapCHR(addr);
			chrrom_addr &= 0xFFFF;
			if (addr >= 0x1000)
				chrrom_addr |= 0x10000;
			chrrom_addr &= chr_byte_mask;
			return chrrom_addr;
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
