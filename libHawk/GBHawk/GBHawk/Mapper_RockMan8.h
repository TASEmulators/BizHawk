#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>
#include <math.h>

#include "Mapper_Base.h"

using namespace std;

namespace GBHawk
{
	class Mapper_RM8 : Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 1;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed
				return ROM[addr];

			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + ROM_bank * 0x4000];
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
				// lowest bank is fixed
				SetCDLROM(flags, addr);

			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
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
			if ((addr >= 0x2000) && (addr < 0x4000))
			{
				value &= 0x1F;

				if (value == 0) { value = 1; }

				// in hhugboy they just subtract 8, but to me looks like bits 4 and 5 are just swapped (and bit 4 is unused?)
				ROM_bank = ((value & 0xF) | ((value & 0x10) >> 1))& ROM_mask;
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};
}
