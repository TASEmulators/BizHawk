using System;
//27

//TODO - could merge functionality with 192 somehow

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public sealed class Mapper074 : MMC3Board_Base
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_074

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER074":
					break;
				case "MAPPER224":
					break;
				default:
					return false;
			}

			VRAM = new byte[2048];

			if (Cart.chr_size == 0 && Cart.board_type == "MAPPER074") 
				throw new Exception("Mapper074 carts MUST have chr rom!");
			BaseSetup();
			return true;
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				int bank = Get_CHRBank_1K(addr);
				if (bank == 0x08)
				{
					VRAM[addr & 0x03FF] = value;
				}
				else if (bank == 0x09)
				{
					VRAM[(addr & 0x03FF) + 0x400] = value;
				}
				// Ying Kiong Chuan Qi, no VROM
				// Nestopia maps this to mapper 224, perhaps we should do the same instead of attempt to account for this scenario here
				else
				{
					addr = MapCHR(addr);
					VRAM[addr & (VRAM.Length - 1)] = value;
				}
			}
			else
			{
				base.WritePPU(addr, value);
			}
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = Get_CHRBank_1K(addr);
				if (bank == 0x08)
				{
					return VRAM[addr & 0x03FF];
				}
				else if (bank == 0x09)
				{
					return VRAM[(addr & 0x03FF) + 0x400];
				}
				else
				{
					addr = MapCHR(addr);

					// Ying Kiong Chuan Qi, no VROM
					// Nestopia maps this to mapper 224, perhaps we should do the same instead of attempt to account for this scenario here
					if (VROM == null)
					{
						return VRAM[addr];
					}

					return VROM[addr];
				}
			}
			else return base.ReadPPU(addr);
		}
	}
}
