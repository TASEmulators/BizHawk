#include <cstdint>
#include <iomanip>
#include <string>

#include "Memory.h"
#include "PPU.h"

using namespace std;

namespace GBHawk
{
	uint8_t PPU::ReadMemory(uint32_t addr)
	{
		return mem_ctrl->ReadMemory(addr);
	}

	void PPU::vblank_process()
	{
		in_vblank[0] = true;
		vblank_rise[0] = true;

		if (scanlineCallback && (_scanlineCallbackLine[0] == -1))
		{
			scanlineCallback();
		}

		mem_ctrl->do_controller_check();

		// send the image on VBlank
		mem_ctrl->SendVideoBuffer();
	}
}