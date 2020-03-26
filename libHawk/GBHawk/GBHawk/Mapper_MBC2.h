#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>
#include <math.h>

#include "Mapper_Base.h"

using namespace std;

namespace GBHawk
{
	class Mapper_MBC2 : Mapper
	{
	public:

		uint32_t ROM_bank;
		uint32_t RAM_bank;
		bool RAM_enable;
		uint32_t ROM_mask;

		void Reset()
		{
			ROM_bank = 1;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = Core._rom.Length / 0x4000 - 1;
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				return Core._rom[addr];
			}
			else if (addr < 0x8000)
			{
				return Core._rom[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else if ((addr >= 0xA000) && (addr < 0xA200))
			{
				if (RAM_enable)
				{
					return Core.cart_RAM[addr - 0xA000];
				}
				return 0xFF;
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
			else if ((addr >= 0xA000) && (addr < 0xA200))
			{
				if (RAM_enable)
				{
					SetCDLRAM(flags, addr - 0xA000);
				}
				return;
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
				if ((addr & 0x100) == 0)
				{
					RAM_enable = ((value & 0xA) == 0xA);
				}
			}
			else if (addr < 0x4000)
			{
				if ((addr & 0x100) > 0)
				{
					ROM_bank = value & 0xF & ROM_mask;
					if (ROM_bank==0) { ROM_bank = 1; }
				}
			}
			else if ((addr >= 0xA000) && (addr < 0xA200))
			{
				if (RAM_enable)
				{
					Core.cart_RAM[addr - 0xA000] = (uint8_t)(value & 0xF);
				}				
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
			ser.Sync(nameof(RAM_bank), ref RAM_bank);
			ser.Sync(nameof(RAM_enable), ref RAM_enable);
		}
	};
}