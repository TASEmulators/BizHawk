// MSXHawk.cpp : Defines the exported functions for the DLL.
//

#include "MSXHawk.h"
#include "Core.h"

#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace MSXHawk;

// Create pointer to a core instance
MSXHAWK_EXPORT MSXCore* MSX_create()
{
	return new MSXCore();
}

// free the memory from the core pointer
MSXHAWK_EXPORT void MSX_destroy(MSXCore* p)
{
	std::free(p);
}

// load a rom into the core
MSXHAWK_EXPORT void MSX_load(MSXCore* p, uint8_t* rom, unsigned int size, int mapper)
{
	p->Load_ROM(rom, size, mapper);
}

// advance a frame
MSXHAWK_EXPORT void MSX_frame_advance(MSXCore* p, uint8_t ctrl1, uint8_t ctrl2, bool render, bool sound)
{
	p->FrameAdvance(ctrl1, ctrl2, render, sound);
}

