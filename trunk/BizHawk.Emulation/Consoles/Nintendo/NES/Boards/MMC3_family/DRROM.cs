using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//this board contains a Namcot 109 and some extra ram for nametables
	public class DRROM : Namcot109Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NES-DRROM": //gauntlet (U)
					AssertPrg(128); AssertChr(64); AssertVram(2); AssertWram(0);
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirrorType(Cart.pad_h, Cart.pad_v);

			return true;
		}

		//the addressing logic for nametables is a bit speculative here
		//how it is wired back to the NES and locally mirrored is unknown,
		//but it probably doesnt matter in practice.
		//still, purists could validate it.

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				//read patterns from mapper controlled area
				return base.ReadPPU(addr);
			}
			else if (addr < 0x2800)
			{
				return VRAM[addr - 0x2000];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				//nothing wired here
			}
			else if (addr < 0x2800)
			{
				VRAM[addr - 0x2000] = value;
			}
			else base.WritePPU(addr, value);
		}
	}

}