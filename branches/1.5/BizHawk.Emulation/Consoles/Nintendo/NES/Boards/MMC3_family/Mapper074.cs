//27

//TODO - could merge functionality with 192 somehow

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper074 : MMC3Board_Base
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_074

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER074":
					break;
				default:
					return false;
			}
			VRAM = new byte[2048];
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
			    return VROM[addr];
			  }
			}
			else return base.ReadPPU(addr);
		}
	}
}
