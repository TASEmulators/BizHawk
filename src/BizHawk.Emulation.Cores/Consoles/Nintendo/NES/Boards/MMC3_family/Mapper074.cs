//27

//TODO - could merge functionality with 192 somehow

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	internal sealed class Mapper074 : MMC3Board_Base
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_074

		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER074":
					break;
				case "MAPPER224":
					break;
				default:
					return false;
			}

			Vram = new byte[2048];

			if (Cart.ChrSize == 0 && Cart.BoardType == "MAPPER074") 
				throw new Exception("Mapper074 carts MUST have chr rom!");
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
				// Ying Kiong Chuan Qi, no VROM
				// Nestopia maps this to mapper 224, perhaps we should do the same instead of attempt to account for this scenario here
				else
				{
					addr = MapCHR(addr);
					Vram[addr & (Vram.Length - 1)] = value;
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
					return Vram[addr & 0x03FF];
				}
				
				if (bank == 0x09)
				{
					return Vram[(addr & 0x03FF) + 0x400];
				}

				addr = MapCHR(addr);

				// Ying Kiong Chuan Qi, no VROM
				// Nestopia maps this to mapper 224, perhaps we should do the same instead of attempt to account for this scenario here
				if (Vrom == null)
				{
					return Vram[addr];
				}

				return Vrom[addr];
			}

			return base.ReadPpu(addr);
		}
	}
}
