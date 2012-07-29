using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper192 : MMC3Board_Base
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_192
		
		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER192":
					break;
				default:
					return false;
			}
			VRAM = new byte[4096];
			BaseSetup();
			return true;
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				VRAM[addr & 0xFFF] = value;
			}
			else
			{
				base.WritePPU(addr, value);
			}
		}

		private int GetBankNum(int addr)
		{
			int bank_1k = Get_CHRBank_1K(addr);
			bank_1k &= chr_mask;
			return bank_1k;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank = GetBankNum(addr);
				if (bank == 0x08)
				{
					byte value = VRAM[addr & 0x03FF];
					return value;
				}
				else if (bank == 0x09)
				{
					return VRAM[(addr & 0x03FF) + 0x400];
				}
				else if (bank == 0x0A)
				{
					return VRAM[(addr & 0x03FF) + 0x800];
				}
				else if (bank == 0x0B)
				{
					return VRAM[(addr & 0x03FF) + 0xC00];
				}
				else
				{
					addr = MapCHR(addr);
					return VROM[addr + extra_vrom];
				}

			}
			else return base.ReadPPU(addr);
		}
	}
}
