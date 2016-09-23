namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper195 : MMC3Board_Base
	{
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER195":
					break;
				default:
					return false;
			}

			BaseSetup();
			return true;
		}

		public override byte ReadEXP(int addr)
		{
			if (addr < 0x1000)
			{
				return ROM[(2 << 0x1000) + (addr & 0xFFF)];
			}

			return base.ReadEXP(addr);
		}

		public override void WritePRG(int addr, byte value)
		{
			base.WritePRG(addr, value);
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = Get_CHRBank_1K(addr);

				if (bank_1k<=3)
				{
					return VRAM[(bank_1k << 10) + (addr & 0x3FF)];
				}
				else
				{
					addr = MapCHR(addr);
					return VROM[addr + extra_vrom];
				}
			}
			else
				return base.ReadPPU(addr);
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				int bank_1k = Get_CHRBank_1K(addr);

				if (bank_1k <= 3)
				{
					VRAM[(bank_1k << 10) + (addr & 0x3FF)]=value;
				}
				else
				{
					// nothing to write to VROM
				}
			}
			else 
				base.WritePPU(addr, value);
		}
	}
}
