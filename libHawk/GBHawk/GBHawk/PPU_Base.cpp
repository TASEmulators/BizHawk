#include <cstdint>
#include <iomanip>
#include <string>

#include "Memory.h"
#include "PPU_Base.h"

using namespace std;

namespace GBHawk
{
	uint8_t PPU::ReadMemory(uint32_t addr)
	{
		return mem_ctrl->HardwareRead(addr);
	}
}