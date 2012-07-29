using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	class Mapper074 : MMC3Board_Base
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_074
		
		//TODO: fix CHR-RAM behavior

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

			BaseSetup();
			return true;
		}

		public override void WritePPU(int addr, byte value)
		{
			if (addr < 0x2000)
			{
				VRAM[addr & 0x7FF] = value;
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
					
					
					//adelikat: value ==255 is a hack to prevent a regression.
					//Without any chr-ram mapping this game works fine other than the missing chinese characters.  This current mapping does not fix that issue.  
					//In addition, the blue caret on the title screen is missing without this hack, so I put it in to prevent a regression.
					//Note: FCEUX and Nintendulator are missing this blue caret (and chinese characters) suggesting a possible logical flaw in the mapper documentation.  
					//Nestopia achieves the correct behavior but I was unable to determine how its logic was any different.
					if (value == 255) 
					{
						return VROM[(addr & 0x03FF) + 0x2000];
					}
					else
					{
						return value;
					}
					
				}
				else if (bank == 0x09)
				{
					return VRAM[(addr & 0x03FF) + 0x400];
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
