#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "Memory.h"
#include "LR35902.h"
#include "PPU_Base.h"
#include "GBAudio.h"

using namespace std;

namespace GBHawk
{
	uint8_t MemoryManager::HardwareRead(uint32_t port)
	{

		return 0xFF;
	}
	
	void MemoryManager::HardwareWrite(uint32_t port, uint8_t value)
	{

	}
}