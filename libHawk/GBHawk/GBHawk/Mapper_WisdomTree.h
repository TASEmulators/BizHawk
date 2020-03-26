#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>
#include <math.h>

#include "Mapper_Base.h"

using namespace std;

namespace GBHawk
{
	class Mapper_WT : Mapper
	{
	public:

		uint32_t ROM_bank;
		uint32_t ROM_mask;

		void Reset()
		{
			ROM_bank = 0;
			ROM_mask = Core._rom.Length / 0x8000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }
			if (ROM_mask > 0x100) { ROM_mask |= 0xFF; }
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x8000)
			{
				return Core._rom[ROM_bank * 0x8000 + addr];
			}
			else
			{
				return 0xFF;
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x8000)
			{
				SetCDLROM(flags, ROM_bank * 0x8000 + addr);
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
			if (addr < 0x4000)
			{
				ROM_bank = ((addr << 1) & 0x1ff) >> 1;
				ROM_bank &= ROM_mask;
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}

		void SyncState(Serializer ser)
		{
			ser.Sync(nameof(ROM_bank), ref ROM_bank);
			ser.Sync(nameof(ROM_mask), ref ROM_mask);
		}
	};
}
