namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://wiki.nesdev.com/w/index.php/INES_Mapper_192
	internal sealed class Mapper192 : MMC3Board_Base
	{
		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER192":
					break;
				default:
					return false;
			}
			Vram = new byte[4096];
			BaseSetup();
			return true;
		}

		public override void WritePpu(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				int bank = Get_CHRBank_1K(addr);
				if (bank == 0x08)
				{
					Vram[addr & 0x03FF] = value;
				}
				else if (bank == 0x09)
				{
					Vram[(addr & 0x03FF) + 0x400] = value;
				}
				if (bank == 0x0A)
				{
					Vram[addr & 0x03FF + 0x800] = value;
				}
				else if (bank == 0x0B)
				{
					Vram[(addr & 0x03FF) + 0xC00] = value;
				}
			}
			else
			{
				base.WritePpu(addr, value);
			}
		}

		public override byte ReadPpu(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = Get_CHRBank_1K(addr);
				if (bank == 0x08)
				{
					byte value = Vram[addr & 0x03FF];
					return value;
				}

				if (bank == 0x09)
				{
					return Vram[(addr & 0x03FF) + 0x400];
				}

				if (bank == 0x0A)
				{
					return Vram[(addr & 0x03FF) + 0x800];
				}

				if (bank == 0x0B)
				{
					return Vram[(addr & 0x03FF) + 0xC00];
				}

				addr = MapCHR(addr);
				return Vrom[addr + extra_vrom];

			}

			return base.ReadPpu(addr);
		}
	}
}
