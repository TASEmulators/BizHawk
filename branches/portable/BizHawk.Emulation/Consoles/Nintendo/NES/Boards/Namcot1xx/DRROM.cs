namespace BizHawk.Emulation.Consoles.Nintendo
{
	//this board contains a Namcot 109 and some extra ram for nametables
	public sealed class DRROM : Namcot108Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NES-DRROM": //gauntlet (U)
				case "TENGEN-800004": // gauntlet (Unl)
					AssertPrg(128); AssertChr(64); AssertVram(2); AssertWram(0);
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirroring(0, 1, 0, 1);

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