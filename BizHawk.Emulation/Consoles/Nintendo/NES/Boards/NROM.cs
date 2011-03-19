using System;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	public class NROM : NES.NESBoardBase
	{
		//configuration
		int prg_byte_mask;

		//state
		//(none)

		public override bool Configure()
		{
			//configure
			switch (Cart.board_type)
			{
				case "HVC-NROM-256": //super mario bros.
				case "NES-NROM-256": //10 yard fight
					AssertPrg(32); AssertChr(8); AssertVram(0); AssertWram(0);
					break;

				case "HVC-RROM": //balloon fight
				case "HVC-NROM-128":
				case "IREM-NROM-128":
				case "KONAMI-NROM-128":
				case "NES-NROM-128":
				case "NAMCOT-3301":
					AssertPrg(16); AssertChr(8); AssertVram(0); AssertWram(0);
					break;

				default:
					return false;
			}

			prg_byte_mask = (Cart.prg_size*1024) - 1;
			SetMirrorType(Cart.pad_h, Cart.pad_v);
			
			return true;
		}

		public override byte ReadPRG(int addr)
		{
			addr &= prg_byte_mask;
			return ROM[addr];
		}
	}
}