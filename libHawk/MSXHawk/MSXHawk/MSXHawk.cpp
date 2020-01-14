// MSXHawk.cpp : Defines the exported functions for the DLL.
//

#include "MSXHawk.h"
#include "Core.h"

#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "Z80A.h"
#include "PSG.h"
#include "VDP.h"

using namespace MSXHawk;

// initialize static members
uint8_t MSXCore::reg_FFFC = 0;
uint8_t MSXCore::reg_FFFD = 0;
uint8_t MSXCore::reg_FFFE = 0;
uint8_t MSXCore::reg_FFFF = 0;

uint8_t* MSXCore::rom = nullptr;
uint32_t MSXCore::rom_size = 0;
uint32_t MSXCore::rom_mapper = 0;
uint8_t MSXCore::ram[0x2000] = {};
uint8_t MSXCore::cart_ram[0x8000] = {};

Z80A MSXCore::cpu;
SN76489sms MSXCore::psg;
VDP MSXCore::vdp;

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

