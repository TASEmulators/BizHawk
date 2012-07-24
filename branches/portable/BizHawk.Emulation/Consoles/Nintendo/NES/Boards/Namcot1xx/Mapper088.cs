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
	For example, mask the CHR ROM 1K bank output from the mapper by $3F, and then OR it with $40 if the PPU address was >= $1000.
	Consequently, CHR is split into two halves. $0xxx can only have CHR from the first 64K, $1xxx can only have CHR from the second 64K.
	*/


	class Mapper088 : Namcot108Board_Base
	{
		//configuration
		int chr_bank_mask_1k;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NAMCOT-3443":
				case "NAMCOT-3433":
				case "MAPPER088":
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirrorType(EMirrorType.Vertical);

			chr_bank_mask_1k = Cart.chr_size - 1;

			return true;
		}

		int RewireCHR(int addr)
		{
			int bank_1k = mapper.Get_CHRBank_1K(addr);
			bank_1k &= 0x3F;
			if (addr >= 0x1000)
				bank_1k |= 0x40;
			bank_1k &= chr_bank_mask_1k;
			int ofs = addr & ((1 << 10) - 1);
			addr = (bank_1k << 10) + ofs;
			return addr;
		}

		public override byte ReadPPU(int addr)
		{
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
