namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class TVROM : MMC3Board_Base
	{
		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER004":
					if (Cart.InesMirroring != 2) // send these to TxROM
						return false;
					Cart.VramSize = 8;
					break;


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

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				//read patterns from mapper controlled area
				return base.ReadPpu(addr);
			}

			return Vram[addr & 0xFFF];
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				//nothing wired here
			}
			else
			{
				Vram[addr & 0xFFF] = value;
			}
		}
	}
}