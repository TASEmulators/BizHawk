#include <cstdint>
#include <iomanip>
#include <string>

#include "Memory.h"
#include "LR35902.h"

using namespace std;

namespace GBHawk
{
	void LR35902::WriteMemory(uint32_t addr, uint8_t value)
	{
		mem_ctrl->WriteMemory(addr, value);
	}

	uint8_t LR35902::ReadMemory(uint32_t addr)
	{
		return mem_ctrl->ReadMemory(addr);
	}

	uint8_t LR35902::PeekMemory(uint32_t addr)
	{
		return mem_ctrl->PeekMemory(addr);
	}

	uint8_t LR35902::SpeedFunc(uint32_t val)
	{
		return mem_ctrl->SpeedFunc(val);
	}
}