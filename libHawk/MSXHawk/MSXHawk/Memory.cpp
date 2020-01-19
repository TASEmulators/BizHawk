#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "Memory.h"
#include "Z80A.h"
#include "VDP.h"
#include "PSG.h"

using namespace std;

namespace MSXHawk
{
	uint8_t MemoryManager::HardwareRead(uint32_t port)
	{
		port &= 0xFF;
		if (port < 0x40) // General IO ports
		{

			switch (port)
			{
			case 0x00: return ReadPort0();
			case 0x01: return Port01;
			case 0x02: return Port02;
			case 0x03: return Port03;
			case 0x04: return Port04;
			case 0x05: return Port05;
			case 0x06: return 0xFF;
			case 0x3E: return Port3E;
			default: return 0xFF;
			}
		}
		if (port < 0x80)  // VDP Vcounter/HCounter
		{
			if ((port & 1) == 0)
				return vdp_pntr->ReadVLineCounter();
			else
				return vdp_pntr->ReadHLineCounter();
		}
		if (port < 0xC0) // VDP data/control ports
		{
			if ((port & 1) == 0)
				return vdp_pntr->ReadData();
			else
				return vdp_pntr->ReadVdpStatus();
		}
		switch (port)
		{
		case 0xC0:
		case 0xDC: return ReadControls1();
		case 0xC1:
		case 0xDD: return ReadControls2();
		case 0xDE: return PortDEEnabled ? PortDE : 0xFF;
		case 0xF2: return 0xFF;
		default: return 0xFF;
		}
	}
	
	void MemoryManager::HardwareWrite(uint32_t port, uint8_t value)
	{
		port &= 0xFF;
		if (port < 0x40) // general IO ports
		{
			switch (port & 0xFF)
			{
			case 0x01: Port01 = value; break;
			case 0x02: Port02 = value; break;
			case 0x03: Port03 = value; break;
			case 0x04: /*Port04 = value*/; break; // receive port, not sure what writing does
			case 0x05: Port05 = (uint8_t)(value & 0xF8); break;
			case 0x06: psg_pntr->Set_Panning(value); break;
			case 0x3E: Port3E = value; break;
			case 0x3F: Port3F = value; break;
			}
		}
		else if (port < 0x80) // PSG
		{
			psg_pntr->WriteReg(value);
		}
		else if (port < 0xC0) // VDP
		{
			if ((port & 1) == 0) 
			{
				vdp_pntr->WriteVdpData(value);
			}				
			else
			{
				vdp_pntr->WriteVdpControl(value);
			}				
		}
		else if (port == 0xDE && PortDEEnabled) PortDE = value;
	}
	
	void MemoryManager::remap_ROM_0()
	{
		// 0x0000 - 0x03FF always maps to start of ROM
		cpu_pntr->MemoryMap[0] = &rom[0];
		cpu_pntr->MemoryMapMask[0] = 0;

		for (uint32_t i = 1; i < 16; i++)
		{
			cpu_pntr->MemoryMap[i] = &rom[(reg_FFFD % rom_size) * 0x4000 + (0x400 * i)];
			cpu_pntr->MemoryMapMask[i] = 0;
		}
	}

	void MemoryManager::remap_ROM_1()
	{
		for (uint32_t i = 0; i < 16; i++)
		{
			cpu_pntr->MemoryMap[i + 16] = &rom[(reg_FFFE % rom_size) * 0x4000 + (0x400 * i)];
			cpu_pntr->MemoryMapMask[i + 16] = 0;
		}
	}

	void MemoryManager::remap_ROM_2()
	{
		if ((reg_FFFC & 0x8) > 0)
		{
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 32] = &cart_ram[((reg_FFFC >> 2) & 0x1) * 0x4000 + (0x400 * i)];
				cpu_pntr->MemoryMapMask[i + 32] = 0xFF;
			}
		}
		else
		{
			for (int i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 32] = &rom[(reg_FFFF % rom_size) * 0x4000 + (0x400 * i)];
				cpu_pntr->MemoryMapMask[i + 32] = 0;
			}
		}
	}
	
	void MemoryManager::remap_RAM()
	{
		if ((reg_FFFC & 0x10) > 0)
		{
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 48] = &cart_ram[(0x400 * i)];
				cpu_pntr->MemoryMapMask[i + 48] = 0xFF;
			}
		}
		else
		{
			for (uint32_t i = 0; i < 8; i++)
			{
				cpu_pntr->MemoryMap[i + 48] = &ram[(0x400 * i)];
				cpu_pntr->MemoryMap[i + 48 + 8] = &ram[(0x400 * i)];
				cpu_pntr->MemoryMapMask[i + 48] = 0xFF;
				cpu_pntr->MemoryMapMask[i + 48 + 8] = 0xFF;
			}
		}
	}
}