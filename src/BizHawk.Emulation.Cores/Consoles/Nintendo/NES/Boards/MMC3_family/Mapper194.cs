namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_194
	internal sealed class Mapper194 : MMC3Board_Base
	{
		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER194":
					break;
				default:
					return false;
			}
			Vram = new byte[2048];
			BaseSetup();
			return true;
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				Vram[addr & 0x7FF] = value;
			}
			else
			{
				base.WritePpu(addr, value);
			}
		}

		private int GetBankNum(int addr)
		{
			int bank_1k = Get_CHRBank_1K(addr);
			bank_1k &= chr_mask;
			return bank_1k;
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = GetBankNum(addr);
				if (bank == 0x00)
				{
					return Vram[addr & 0x03FF];
				}

				if (bank == 0x01)
				{
					return Vram[(addr & 0x03FF) + 0x400];
				}

				addr = MapCHR(addr);
				return Vrom[addr + extra_vrom];

			}

			return base.ReadPpu(addr);
		}
	}
}
