#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>
#include <math.h>

#include "Mapper_Base.h"

using namespace std;

namespace GBHawk
{
	class Mapper_HuC1 : Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 0;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }

			RAM_mask = 0;
			if (Cart_RAM_Length[0] > 0)
			{
				RAM_mask = Cart_RAM_Length[0] / 0x2000 - 1;
				if (Cart_RAM_Length[0] == 0x800) { RAM_mask = 0; }
			}
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
			else if ((addr >= 0xA000) && (addr < 0xC000))
			{
				if (RAM_enable)
				{
					if (Cart_RAM_Length[0] > 0)
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0])
						{
							return Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000];
						}
						else
						{
							return 0xFF;
						}
					}
					else
					{
						return 0xFF;
					}
				}
				else
				{
					// when RAM isn't enabled, reading from this area will return IR sensor reading
					// for now we'll assume it never sees light (0xC0)
					return 0xC0;
				}
			}
			else
			{
				return 0xFF;
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				SetCDLROM(flags, addr);
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else if ((addr >= 0xA000) && (addr < 0xC000))
			{
				if (RAM_enable)
				{
					if (Cart_RAM != null)
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0])
						{
							SetCDLRAM(flags, (addr - 0xA000) + RAM_bank * 0x2000);
						}
						else
						{
							return;
						}
					}
					else
					{
						return;
					}
				}
				else
				{
					return;
				}
			}
			else
			{
				return;
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
					RAM_enable = (value & 0xF) != 0xE;
				}
				else if (addr < 0x4000)
				{
					value &= 0x3F;

					ROM_bank &= 0xC0;
					ROM_bank |= value;
					ROM_bank &= ROM_mask;
				}
				else if (addr < 0x6000)
				{
					RAM_bank = value & 3;
					RAM_bank &= RAM_mask;
				}
			}
			else
			{
				if (RAM_enable)
				{
					if (Cart_RAM_Length[0] > 0)
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0])
						{
							Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
						}
					}
				}
				else
				{
					// I don't know if other bits here have an effect
					if (value == 1)
					{
						IR_signal = true;
					}
					else if (value == 0)
					{
						IR_signal = false;
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
