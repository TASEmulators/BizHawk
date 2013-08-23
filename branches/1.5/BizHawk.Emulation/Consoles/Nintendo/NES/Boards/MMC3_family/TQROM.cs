namespace BizHawk.Emulation.Consoles.Nintendo
{
	//aka mapper 119
	//just high speed and pinbot with an MMC3 and some custom logic to select between chr rom and chr ram
	[NES.INESBoardImplPriority]
	public class TQSROM : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "NES-TQROM": //high speed and pinbot
					AssertPrg(128); AssertChr(64); AssertVram(8); AssertWram(0);
					break;
				default:
					return false;
			}

			BaseSetup();

			return true;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = mmc3.Get_CHRBank_1K(addr);
				int use_ram = (bank_1k >> 6) & 1;
				if (use_ram == 1)
				{
					addr = ((bank_1k&0x3f) << 10) | (addr & 0x3FF);
					addr &= 0x1FFF;
					return VRAM[addr];
				}
				else return base.ReadPPU(addr);
			}
			else
				return base.ReadPPU(addr);
        }

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				int bank_1k = mmc3.Get_CHRBank_1K(addr);
				int use_ram = (bank_1k >> 6) & 1;
				if (use_ram == 1)
				{
					addr = ((bank_1k & 0x3f) << 10) | (addr & 0x3FF);
					addr &= 0x1FFF;
					VRAM[addr] = value;
				}
				//else
					// if this address is mapped to chrrom and not chrram, the write just does nothing
					//base.WritePPU(addr, value);					
			}
			else
				base.WritePPU(addr, value);
		}

	}


}
