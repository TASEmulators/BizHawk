#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>
#include <math.h>

#include "Mapper_Base.h"

using namespace std;

namespace GBHawk
{
	class Mapper_Camera : Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 1;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;

			RAM_mask = Cart_RAM_Length[0] / 0x2000 - 1;

			regs_enable = false;
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				return ROM[addr];
			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else
			{
				if (regs_enable)
				{
					if ((addr & 0x7F) == 0)
					{
						return 0;// regs[0];
					}
					else
					{
						return 0;
					}
				}
				else 
				{
					if (/*RAM_enable && */(((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						return Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000];
					}
					else
					{
						return 0xFF;
					}
				}
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed, but is still effected by mode
				SetCDLROM(flags, addr);
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else
			{
				if (!regs_enable)
				{
					if ((((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						SetCDLRAM(flags, (addr - 0xA000) + RAM_bank * 0x2000);
					}
					else
					{
						return;
					}
				}
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x8000)
			{
				if (addr < 0x2000)
				{
					RAM_enable = (value & 0xF) == 0xA;
				}
				else if (addr < 0x4000)
				{
					ROM_bank = value;
					ROM_bank &= ROM_mask;
					//Console.WriteLine(addr + " " + value + " " + ROM_mask + " " + ROM_bank);
				}
				else if (addr < 0x6000)
				{
					if ((value & 0x10) == 0x10)
					{
						regs_enable = true;
					}
					else
					{
						regs_enable = false;
						RAM_bank = value & RAM_mask;
					}
				}
			}
			else
			{
				if (regs_enable)
				{
					regs_cam[(addr & 0x7F)] = (uint8_t)(value & 0x7);
				}
				else
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
					}
				}				
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};
}
