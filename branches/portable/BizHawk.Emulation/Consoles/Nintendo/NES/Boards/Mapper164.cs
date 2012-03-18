using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper164 : NES.NESBoardBase 
	{
		//Mapper 164
		//Final Fantasy V (Unl)

		int prg;

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "Mapper164":
					break;
				default:
					return false;
			}
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			prg = 0xFF;
			return true;
		}

		public override void WriteEXP(int addr, byte value)
		{
			addr &= 0xF300;
			if (addr == 0x5000)
				prg = value;
		}

		public override void WritePRG(int addr, byte value)
		{
			addr &= 0xF300;
			if (addr == 0xD000)
				prg = value;
		}

		public override byte ReadPPU(int addr)
		{
			return VROM[addr + (prg * 0x8000)];
		}
	}
}
