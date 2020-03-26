#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>
#include <math.h>

#include "Mapper_Base.h"

using namespace std;

namespace GBHawk
{
	class Mapper_Default : Mapper
	{
	public:

		void Reset()
		{
			// nothing to initialize
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x8000)
			{
				return ROM[addr];
			}
			else
			{
				if (Cart_RAM_Length > 0)
				{
					return Cart_RAM[addr - 0xA000];
				}
				else
				{
					return 0;
				}
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x8000)
			{
				SetCDLROM(flags, addr);
			}
			else
			{
				if (Cart_RAM != null)
				{
					SetCDLRAM(flags, addr - 0xA000);
				}
				else
				{
					return;
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
				// no mapping hardware available
			}
			else
			{
				if (Cart_RAM_Length > 0)
				{
					Cart_RAM[addr - 0xA000] = value;
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};
}
