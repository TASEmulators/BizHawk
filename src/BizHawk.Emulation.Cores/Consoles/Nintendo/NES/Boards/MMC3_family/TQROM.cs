namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//aka mapper 119
	//just high speed and pinbot with an MMC3 and some custom logic to select between chr rom and chr ram
	internal sealed class TQSROM : MMC3Board_Base
	{
		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER119":
					Cart.VramSize = 8; Cart.WramSize = 0; // Junk ROMs get these wrong
					break;
				case "NES-TQROM": // High Speed and Pin Bot
					AssertPrg(128); AssertChr(64); AssertVram(8); AssertWram(0);
					break;
				default:
					return false;
			}

			BaseSetup();

			return true;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = mmc3.Get_CHRBank_1K(addr);
				int use_ram = (bank_1k >> 6) & 1;
				if (use_ram == 1)
				{
					addr = ((bank_1k&0x3f) << 10) | (addr & 0x3FF);
					addr &= 0x1FFF;
					return Vram[addr];
				}
				else return base.ReadPpu(addr);
			}
			else
				return base.ReadPpu(addr);
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				int bank_1k = mmc3.Get_CHRBank_1K(addr);
				int use_ram = (bank_1k >> 6) & 1;
				if (use_ram == 1)
				{
					addr = ((bank_1k & 0x3f) << 10) | (addr & 0x3FF);
					addr &= 0x1FFF;
					Vram[addr] = value;
				}
				//else
					// if this address is mapped to chrrom and not chrram, the write just does nothing
					//base.WritePPU(addr, value);
			}
			else
				base.WritePpu(addr, value);
		}
	}
}
