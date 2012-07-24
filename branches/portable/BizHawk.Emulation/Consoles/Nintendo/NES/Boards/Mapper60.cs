using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper60 : NES.NESBoardBase
	{
		/*
		Here are Disch's original notes:  
		========================
		=  Mapper 060          =
		========================

		Example Game:
		--------------------------
		Reset Based 4-in-1


		Notes:
		---------------------------
		This mapper is very, very unique.

		It's a multicart that consists of four NROM games, each with 16k PRG (put at $8000 and $C000) and 8k CHR.
		The current block that is selected is determined by an internal register that can only be incremented by a
		soft reset!

		I would assume the register is 2 bits wide?  Don't know for sure.
		*/

		int reg = 0;
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER060":
					break;
				default:
					return false;
			}

			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("reg", ref reg);
			base.SyncState(ser);
		}

		public override void NESSoftReset()
		{
			if (reg >= 3)
			{
				reg = 0;
			}
			else
			{
				reg++;
			}
		}

		public override byte ReadPRG(int addr)
		{
			addr &= 0x3FFF;
			return ROM[addr + (reg * 0x4000)];
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
