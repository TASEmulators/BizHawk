#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "Memory.h"
#include "Z80A.h"
#include "TMS9918A.h"
#include "AY_3_8910.h"
#include "SCC.h"

using namespace std;

namespace MSXHawk
{
	uint8_t MemoryManager::HardwareRead(uint32_t port)
	{
		port &= 0xFF;

		if (port == 0x98) // VDP
		{
			return vdp_pntr->ReadData();
		}
		else if (port == 0x99) // VDP
		{
			return vdp_pntr->ReadVdpStatus();
		}
		else if (port == 0xA2)
		{
			if (psg_pntr->port_sel == 0xE) { lagged = false; }
			return psg_pntr->ReadReg();
		}
		else if (port == 0xA8)
		{
			return PortA8;
		}
		else if (port == 0xA9)
		{
			lagged = false;
			return ~kb_rows[kb_rows_sel];
		}
		else if (port == 0xAA)
		{
			// TODO: casette, caps lamp, keyboard sound click
			return kb_rows_sel;
		}
		
		return 0xFF;
	}
	
	void MemoryManager::HardwareWrite(uint32_t port, uint8_t value)
	{
		port &= 0xFF;

		if (port == 0x98) // VDP
		{
			vdp_pntr->WriteVdpData(value);
		}				
		else if(port == 0x99) // VDP
		{
			vdp_pntr->WriteVdpControl(value);
		}
		else if (port == 0xA0)
		{
			psg_pntr->port_sel = (value & 0xF);
		}
		else if (port == 0xA1)
		{
			psg_pntr->WriteReg(value);

			// update controller port data if port F is written to
			if (psg_pntr->port_sel == 0xF) 
			{
				if ((psg_pntr->Register[0xF] & 0x40) > 0)
				{
					psg_pntr->Register[0xE] = controller_byte_2;
				}
				else
				{
					psg_pntr->Register[0xE] = controller_byte_1;
				}
			}
		}
		else if (port == 0xA8)
		{
			PortA8 = value;
			remap();
		}
		else if (port == 0xAA)
		{
			kb_rows_sel = value & 0xF;
			remap();
		}
	}
	
	void MemoryManager::remap()
	{
		if ((PortA8 & 3) == 0) 
		{
			slot_0_has_rom = 0;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i] = &bios_rom[(0x400 * i)];
				cpu_pntr->MemoryMapMask[i] = 0;
			}
		}
		else if ((PortA8 & 3) == 1)
		{
			slot_0_has_rom = 1;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i] = remap_rom1(0, i);
				cpu_pntr->MemoryMapMask[i] = 0;	
			}
		}
		else if ((PortA8 & 3) == 2)
		{
			slot_0_has_rom = 2;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i] = remap_rom2(0, i);
				cpu_pntr->MemoryMapMask[i] = 0;
			}
		}
		else if ((PortA8 & 3) == 3)
		{
			slot_0_has_rom = 0;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i] = &ram[(0x400 * i)];
				cpu_pntr->MemoryMapMask[i] = 0xFF;
			}
		}

		if (((PortA8 >> 2) & 3) == 0)
		{
			slot_1_has_rom = 0;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 16] = &basic_rom[(0x400 * i)];
				cpu_pntr->MemoryMapMask[i + 16] = 0;
			}
		}
		else if (((PortA8 >> 2) & 3) == 1)
		{
			slot_1_has_rom = 1;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 16] = remap_rom1(0x4000, i);
				cpu_pntr->MemoryMapMask[i + 16] = 0;	
			}
		}
		else if (((PortA8 >> 2) & 3) == 2)
		{
			slot_1_has_rom = 2;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 16] = remap_rom2(0x4000, i);
				cpu_pntr->MemoryMapMask[i + 16] = 0;
			}
		}
		else if (((PortA8 >> 2) & 3) == 3)
		{
			slot_1_has_rom = 0;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 16] = &ram[0x4000 + (0x400 * i)];
				cpu_pntr->MemoryMapMask[i + 16] = 0xFF;
			}
		}

		if (((PortA8 >> 4) & 3) == 0)
		{
			slot_2_has_rom = 0;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 32] = &unmapped[0];
				cpu_pntr->MemoryMapMask[i + 32] = 0;
			}
		}
		else if (((PortA8 >> 4) & 3) == 1)
		{
			slot_2_has_rom = 1;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 32] = remap_rom1(0x8000, i);
				cpu_pntr->MemoryMapMask[i + 32] = 0;	
			}
		}
		else if (((PortA8 >> 4) & 3) == 2)
		{
			slot_2_has_rom = 2;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 32] = remap_rom2(0x8000, i);
				cpu_pntr->MemoryMapMask[i + 32] = 0;
			}
		}
		else if (((PortA8 >> 4) & 3) == 3)
		{
			slot_2_has_rom = 0;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 32] = &ram[0x8000 + (0x400 * i)];
				cpu_pntr->MemoryMapMask[i + 32] = 0xFF;
			}
		}

		if (((PortA8 >> 6) & 3) == 0)
		{
			slot_3_has_rom = 0;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 48] = &unmapped[0];
				cpu_pntr->MemoryMapMask[i + 48] = 0;
			}
		}
		else if (((PortA8 >> 6) & 3) == 1)
		{
			slot_3_has_rom = 1;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 48] = remap_rom1(0xC000, i);
				cpu_pntr->MemoryMapMask[i + 48] = 0;
			}
		}
		else if (((PortA8 >> 6) & 3) == 2)
		{
			slot_3_has_rom = 2;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 48] = remap_rom2(0xC000, i);
				cpu_pntr->MemoryMapMask[i + 48] = 0;
			}
		}
		else if (((PortA8 >> 6) & 3) == 3)
		{
			slot_3_has_rom = 0;
			for (uint32_t i = 0; i < 16; i++)
			{
				cpu_pntr->MemoryMap[i + 48] = &ram[0xC000 + (0x400 * i)];
				cpu_pntr->MemoryMapMask[i + 48] = 0xFF;
			}
		}
	}

	uint8_t* MemoryManager::remap_rom1(uint32_t base_addr, uint32_t segment)
	{
		if (rom_mapper_1 == 0) 
		{
			return &rom_1[base_addr + (0x400 * segment)];
		}
		else if (rom_mapper_1 == 1) // basic konami mapper
		{
			if (base_addr == 0)
			{
				if (segment < 8)
				{
					return &rom_1[rom1_konami_page_2 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_1[rom1_konami_page_3 * 0x2000 + (0x400 * segment)];
				}
			}
			else if (base_addr == 0x4000) 
			{
				if (segment < 8) 
				{
					return &rom_1[(0x400 * segment)];
				}
				else 
				{
					segment -= 8;
					return &rom_1[rom1_konami_page_1 * 0x2000 + (0x400 * segment)];
				}
			}
			else if (base_addr == 0x8000)
			{
				if (segment < 8)
				{
					return &rom_1[rom1_konami_page_2 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_1[rom1_konami_page_3 * 0x2000 + (0x400 * segment)];
				}
			}
			else
			{
				if (segment < 8)
				{
					return &rom_1[(0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_1[rom1_konami_page_1 * 0x2000 + (0x400 * segment)];
				}
			}		
		}
		else if (rom_mapper_1 == 2) // konami mapper with SCC
		{
			if (base_addr == 0)
			{
				if (segment < 8)
				{
					return &rom_1[rom1_konami_page_2 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_1[rom1_konami_page_3 * 0x2000 + (0x400 * segment)];
				}
			}
			else if (base_addr == 0x4000)
			{
				if (segment < 8)
				{
					return &rom_1[(0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_1[rom1_konami_page_1 * 0x2000 + (0x400 * segment)];
				}
			}
			else if (base_addr == 0x8000)
			{
				if (segment < 8)
				{
					if (SCC_1_enabled) 
					{
						if (segment < 6) 
						{
							return &rom_1[rom1_konami_page_2 * 0x2000 + (0x400 * segment)];
						}
						else 
						{
							return &SCC_1_page[0];
						}
					}
					return &rom_1[rom1_konami_page_2 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_1[rom1_konami_page_3 * 0x2000 + (0x400 * segment)];
				}
			}
			else
			{
				if (segment < 8)
				{
					return &rom_1[(0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_1[rom1_konami_page_1 * 0x2000 + (0x400 * segment)];
				}
			}
		}
		else if (rom_mapper_1 == 3) // Ascii 8kb
		{
			if (base_addr == 0)
			{
				if (segment < 8)
				{
					return &rom_1[rom1_konami_page_2 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_1[rom1_konami_page_3 * 0x2000 + (0x400 * segment)];
				}
			}
			else if (base_addr == 0x4000)
			{
				if (segment < 8)
				{
					return &rom_1[rom1_konami_page_0 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_1[rom1_konami_page_1 * 0x2000 + (0x400 * segment)];
				}
			}
			else if (base_addr == 0x8000)
			{
				if (segment < 8)
				{
					return &rom_1[rom1_konami_page_2 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_1[rom1_konami_page_3 * 0x2000 + (0x400 * segment)];
				}
			}
			else
			{
				if (segment < 8)
				{
					return &rom_1[rom1_konami_page_0 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_1[rom1_konami_page_1 * 0x2000 + (0x400 * segment)];
				}
			}
		}
		else 
		{
			return &unmapped[0];
		}
	}

	uint8_t* MemoryManager::remap_rom2(uint32_t base_addr, uint32_t segment)
	{
		if (rom_mapper_2 == 0)
		{
			return &rom_2[base_addr + (0x400 * segment)];
		}
		else if (rom_mapper_2 == 1) // basic konami mapper
		{
			if (base_addr == 0)
			{
				if (segment < 8)
				{
					return &rom_2[rom2_konami_page_2 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_2[rom2_konami_page_3 * 0x2000 + (0x400 * segment)];
				}
			}
			else if (base_addr == 0x4000)
			{
				if (segment < 8)
				{
					return &rom_2[(0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_2[rom2_konami_page_1 * 0x2000 + (0x400 * segment)];
				}
			}
			else if (base_addr == 0x8000)
			{
				if (segment < 8)
				{
					return &rom_2[rom2_konami_page_2 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_2[rom2_konami_page_3 * 0x2000 + (0x400 * segment)];
				}
			}
			else
			{
				if (segment < 8)
				{
					return &rom_2[(0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_2[rom2_konami_page_1 * 0x2000 + (0x400 * segment)];
				}
			}
		}
		else if (rom_mapper_2 == 2) // konami mapper with SCC
		{
			if (base_addr == 0)
			{
				if (segment < 8)
				{
					return &rom_2[rom2_konami_page_2 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_2[rom2_konami_page_3 * 0x2000 + (0x400 * segment)];
				}
			}
			else if (base_addr == 0x4000)
			{
				if (segment < 8)
				{
					return &rom_2[(0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_2[rom2_konami_page_1 * 0x2000 + (0x400 * segment)];
				}
			}
			else if (base_addr == 0x8000)
			{
				if (segment < 8)
				{
					if (SCC_2_enabled)
					{
						if (segment < 6)
						{
							return &rom_2[rom2_konami_page_2 * 0x2000 + (0x400 * segment)];
						}
						else
						{
							return &SCC_2_page[0];
						}
					}
					return &rom_2[rom2_konami_page_2 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_2[rom2_konami_page_3 * 0x2000 + (0x400 * segment)];
				}
			}
			else
			{
				if (segment < 8)
				{
					return &rom_2[(0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_2[rom2_konami_page_1 * 0x2000 + (0x400 * segment)];
				}
			}
		}
		else if (rom_mapper_2 == 3) // Ascii 8kb
		{
			if (base_addr == 0)
			{
				if (segment < 8)
				{
					return &rom_2[rom2_konami_page_2 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_2[rom2_konami_page_3 * 0x2000 + (0x400 * segment)];
				}
			}
			else if (base_addr == 0x4000)
			{
				if (segment < 8)
				{
					return &rom_2[rom2_konami_page_0 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_2[rom2_konami_page_1 * 0x2000 + (0x400 * segment)];
				}
			}
			else if (base_addr == 0x8000)
			{
				if (segment < 8)
				{
					return &rom_2[rom2_konami_page_2 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_2[rom2_konami_page_3 * 0x2000 + (0x400 * segment)];
				}
			}
			else
			{
				if (segment < 8)
				{
					return &rom_2[rom2_konami_page_0 * 0x2000 + (0x400 * segment)];
				}
				else
				{
					segment -= 8;
					return &rom_2[rom2_konami_page_1 * 0x2000 + (0x400 * segment)];
				}
			}
		}
		else
		{
			return &unmapped[0];
		}
	}

	void MemoryManager::MemoryWrite(uint32_t addr, uint8_t value)
	{
		// Konami addresses without SCC
		if (rom_mapper_1 == 1)
		{
			if (addr >= 0x6000 && addr < 0x8000 && slot_1_has_rom == 1) { rom1_konami_page_1 = (uint8_t)(value & rom_size_1); remap(); }
			if (addr >= 0x8000 && addr < 0xA000 && slot_2_has_rom == 1) { rom1_konami_page_2 = (uint8_t)(value & rom_size_1); remap(); }
			if (addr >= 0xA000 && addr < 0xC000 && slot_2_has_rom == 1) { rom1_konami_page_3 = (uint8_t)(value & rom_size_1); remap(); }
		}
		/*
		if (rom_mapper_2 == 1)
		{
			if (addr >= 0x6000 && addr < 0x8000 && slot_1_has_rom == 2) { rom2_konami_page_1 = (uint8_t)(value & rom_size_2); remap(); }
			if (addr >= 0x8000 && addr < 0xA000 && slot_2_has_rom == 2) { rom2_konami_page_2 = (uint8_t)(value & rom_size_2); remap(); }
			if (addr >= 0xA000 && addr < 0xC000 && slot_2_has_rom == 2) { rom2_konami_page_3 = (uint8_t)(value & rom_size_2); remap(); }
		}
		*/

		// Konami addresses with SCC
		if (rom_mapper_1 == 2)
		{
			if (addr >= 0x5000 && addr < 0x5800 && slot_1_has_rom == 1) { rom1_konami_page_0 = (uint8_t)(value & rom_size_1); remap(); }
			if (addr >= 0x7000 && addr < 0x7800 && slot_1_has_rom == 1) { rom1_konami_page_1 = (uint8_t)(value & rom_size_1); remap(); }
			if (addr >= 0x9000 && addr < 0x9800 && slot_2_has_rom == 1)
			{
				if ((value & 0xFF) == 0x3F) { SCC_1_enabled = true; }
				else { SCC_1_enabled = false; }
				rom1_konami_page_2 = (uint8_t)(value & rom_size_1); remap();
			}
			if (addr >= 0x9800 && addr < 0xA000 && slot_2_has_rom == 1 && SCC_1_enabled)
			{
				SCC_1_pntr->WriteReg((uint8_t)(addr & 0xFF), value);
			}
			if (addr >= 0xB000 && addr < 0xB800 && slot_2_has_rom == 1) { rom1_konami_page_3 = (uint8_t)(value & rom_size_1); remap(); }
		}
		/*
		if (rom_mapper_2 == 2)
		{
			if (addr >= 0x5000 && addr < 0x5800 && slot_1_has_rom == 2) { rom2_konami_page_0 = (uint8_t)(value & rom_size_2); remap(); }
			if (addr >= 0x7000 && addr < 0x7800 && slot_1_has_rom == 2) { rom2_konami_page_1 = (uint8_t)(value & rom_size_2); remap(); }
			if (addr >= 0x9000 && addr < 0x9800 && slot_2_has_rom == 2)
			{
				if ((value & 0xFF) == 0x3F) { SCC_2_enabled = true; }
				else { SCC_2_enabled = false; }
				rom2_konami_page_2 = (uint8_t)(value & rom_size_2); remap();
			}
			if (addr >= 0x9800 && addr < 0xA000 && slot_2_has_rom == 1 && SCC_2_enabled)
			{
				SCC_2_pntr->WriteReg((uint8_t)(addr & 0xFF), value);
			}
			if (addr >= 0xB000 && addr < 0xB800 && slot_2_has_rom == 2) { rom2_konami_page_3 = (uint8_t)(value & rom_size_2); remap(); }
		}
		*/

		// Ascii 8kb
		if (rom_mapper_1 == 3)
		{
			if (addr >= 0x6000 && addr < 0x6800 && slot_1_has_rom == 1) { rom1_konami_page_0 = (uint8_t)(value & rom_size_1); remap(); }
			if (addr >= 0x6800 && addr < 0x7000 && slot_1_has_rom == 1) { rom1_konami_page_1 = (uint8_t)(value & rom_size_1); remap(); }
			if (addr >= 0x7000 && addr < 0x7800 && slot_1_has_rom == 1) { rom1_konami_page_2 = (uint8_t)(value & rom_size_1); remap(); }
			if (addr >= 0x7800 && addr < 0x8000 && slot_1_has_rom == 1) { rom1_konami_page_3 = (uint8_t)(value & rom_size_1); remap(); }
		}
		/*
		if (rom_mapper_2 == 3)
		{
			if (addr >= 0x6000 && addr < 0x6800 && slot_1_has_rom == 2) { rom2_konami_page_0 = (uint8_t)(value & rom_size_2); remap(); }
			if (addr >= 0x6800 && addr < 0x7000 && slot_1_has_rom == 2) { rom2_konami_page_1 = (uint8_t)(value & rom_size_2); remap(); }
			if (addr >= 0x7000 && addr < 0x7800 && slot_1_has_rom == 2) { rom2_konami_page_2 = (uint8_t)(value & rom_size_2); remap(); }
			if (addr >= 0x7800 && addr < 0x8000 && slot_1_has_rom == 2) { rom2_konami_page_3 = (uint8_t)(value & rom_size_2); remap(); }
		}
		*/
	}
}