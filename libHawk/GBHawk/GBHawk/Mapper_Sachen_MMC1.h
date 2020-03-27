#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>
#include <math.h>

#include "Mapper_Base.h"

using namespace std;

namespace GBHawk
{
	class Mapper_Sachen1 : Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 1;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;
			BASE_ROM_Bank = 0;
			ROM_bank_mask = 0xFF;
			locked = true;
			reg_access = false;
			addr_last = 0;
			counter = 0;
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				if (locked)
				{
					// header is scrambled
					if ((addr >= 0x100) && (addr < 0x200))
					{
						uint32_t temp0 = (addr & 1);
						uint32_t temp1 = (addr & 2);
						uint32_t temp4 = (addr & 0x10);
						uint32_t temp6 = (addr & 0x40);

						temp0 = temp0 << 6;
						temp1 = temp1 << 3;
						temp4 = temp4 >> 3;
						temp6 = temp6 >> 6;

						addr &= 0x1AC;
						addr |= (uint32_t)(temp0 | temp1 | temp4 | temp6);
					}
					addr |= 0x80;
				}

				return ROM[addr + BASE_ROM_Bank * 0x4000];
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
				if (locked)
				{
					// header is scrambled
					if ((addr >= 0x100) && (addr < 0x200))
					{
						uint32_t temp0 = (addr & 1);
						uint32_t temp1 = (addr & 2);
						uint32_t temp4 = (addr & 0x10);
						uint32_t temp6 = (addr & 0x40);

						temp0 = temp0 << 6;
						temp1 = temp1 << 3;
						temp4 = temp4 >> 3;
						temp6 = temp6 >> 6;

						addr &= 0x1AC;
						addr |= (uint32_t)(temp0 | temp1 | temp4 | temp6);
					}
					addr |= 0x80;
				}

				SetCDLROM(flags, addr + BASE_ROM_Bank * 0x4000);
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
			if (addr < 0x2000)
			{
				if (reg_access)
				{
					BASE_ROM_Bank = value;
				}
			}
			else if (addr < 0x4000)
			{
				ROM_bank = (value > 0) ? value : 1;

				if ((value & 0x30) == 0x30)
				{
					reg_access = true;
				}
				else
				{
					reg_access = false;
				}
			}
			else if (addr < 0x6000)
			{
				if (reg_access)
				{
					ROM_bank_mask = value;
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}

		void Mapper_Tick()
		{
			if (locked)
			{
				if (((addr_access[0] & 0x8000) == 0) && ((addr_last & 0x8000) > 0) && (addr_access[0] >= 0x100))
				{
					counter++;
				}

				if (addr_access[0] >= 0x100)
				{
					addr_last = addr_access[0];
				}

				if (counter == 0x30)
				{
					locked = false;
				}
			}
		}
	};
}
