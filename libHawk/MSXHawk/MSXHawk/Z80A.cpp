#include <cstdint>
#include <iomanip>
#include <string>

#include "Memory.h"
#include "Z80A.h"

using namespace std;

namespace MSXHawk
{
	void Z80A::Memory_Write(uint32_t addr, uint8_t value) 
	{
		if ((addr & 0xFFFF) >= 0xFFFC) 
		{
			mem_ctrl->MemoryWrite(addr, value);
		}
	}

	void Z80A::HW_Write(uint32_t addr, uint8_t value)
	{
		mem_ctrl->HardwareWrite(addr, value);
	}

	uint8_t Z80A::HW_Read(uint32_t addr)
	{
		return mem_ctrl->HardwareRead(addr);
	}
}