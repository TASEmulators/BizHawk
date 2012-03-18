using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//pretty much just one game. 
	//wires the mapper outputs to control the nametables. check out the companion board TLSROM
	public class Mapper095 : Namcot109Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NAMCOT-3425": //dragon buster (J)
					AssertPrg(128); AssertChr(32); AssertVram(0); AssertWram(0);
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirrorType(EMirrorType.Vertical);

			return true;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000) return base.ReadPPU(addr);
			else return base.ReadPPU(RewireNametable_Mapper095_and_TLSROM(addr, 5));
		}
		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000) base.WritePPU(addr, value);
			else base.WritePPU(RewireNametable_Mapper095_and_TLSROM(addr, 5), value);
		}
	}
}