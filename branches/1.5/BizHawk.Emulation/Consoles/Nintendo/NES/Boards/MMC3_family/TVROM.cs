namespace BizHawk.Emulation.Consoles.Nintendo
{
	[NES.INESBoardImplPriority]
	public class TVROM : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NES-TVROM": //rad racer II (U)
					AssertPrg(64); AssertChr(64); AssertVram(8); AssertWram(0);
					AssertBattery(false);
					break;
				case "NES-TR1ROM": // Gauntlet variant (untested!)
					break;
				default:
					return false;
			}

			BaseSetup();

			return true;
		}

		//nesdev wiki says that the nes CIRAM doesnt get used at all.
		//and that even though 8KB is really here, only 4KB gets used.
		//still, purists could validate it.

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				//read patterns from mapper controlled area
				return base.ReadPPU(addr);
			}
			else
			{
				return VRAM[addr & 0xFFF];
			}
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				//nothing wired here
			}
			else
			{
				VRAM[addr & 0xFFF] = value;
			}
		}

	}
}