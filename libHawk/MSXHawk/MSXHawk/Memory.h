#ifndef MEMORY_H
#define MEMORY_H

#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>
#include <cstring>

using namespace std;

namespace MSXHawk
{
	class Z80A;
	class TMS9918A;
	class AY_3_8910;
	class SCC;
	
	class MemoryManager
	{
	public:
				
		TMS9918A* vdp_pntr = nullptr;
		AY_3_8910* psg_pntr = nullptr;
		SCC* SCC_1_pntr = nullptr;
		SCC* SCC_2_pntr = nullptr;
		Z80A* cpu_pntr = nullptr;
		uint8_t* rom_1 = nullptr;
		uint8_t* rom_2 = nullptr;
		uint8_t* bios_rom = nullptr;
		uint8_t* basic_rom = nullptr;

		// initialized by core loading, not savestated
		uint32_t rom_size_1;
		uint32_t rom_mapper_1;
		uint32_t rom_size_2;
		uint32_t rom_mapper_2;

		// controls are not stated
		uint8_t controller_byte_1, controller_byte_2;
		uint8_t* kb_rows;

		// State
		bool PortDEEnabled = false;
		bool lagged;
		bool start_pressed;

		uint8_t kb_rows_sel;
		uint8_t PortA8 = 0x00;
		uint8_t reg_FFFC, reg_FFFD, reg_FFFE, reg_FFFF;
		uint8_t ram[0x10000] = {};
		uint8_t cart_ram[0x8000] = {};
		uint8_t unmapped[0x400] = {};
		uint8_t SCC_1_page[0x400] = {};
		uint8_t SCC_2_page[0x400] = {};

		// mapper support and variables
		uint8_t slot_0_has_rom, slot_1_has_rom, slot_2_has_rom, slot_3_has_rom;
		uint8_t rom1_konami_page_0, rom1_konami_page_1, rom1_konami_page_2, rom1_konami_page_3;
		uint8_t rom2_konami_page_0, rom2_konami_page_1, rom2_konami_page_2, rom2_konami_page_3;

		bool SCC_1_enabled = false;
		bool SCC_2_enabled = false;

		MemoryManager()
		{

		};

		int msg_len = 0;

		string Mem_text_1 = " ";

		uint8_t HardwareRead(uint32_t value);

		void HardwareWrite(uint32_t addr, uint8_t value);

		void MemoryWrite(uint32_t addr, uint8_t value);

		void remap();

		uint8_t* remap_rom1(uint32_t, uint32_t);

		uint8_t* remap_rom2(uint32_t, uint32_t);

		// NOTE: only called from source when both are available and of correct size (0x4000)
		void Load_BIOS(uint8_t* bios, uint8_t* basic) 
		{
			bios_rom = new uint8_t[0x4000];
			basic_rom = new uint8_t[0x4000];
			
			std::memcpy(bios_rom, bios, 0x4000);
			std::memcpy(basic_rom, basic, 0x4000);
		}

		void Load_ROM(uint8_t* ext_rom_1, uint32_t ext_rom_size_1, uint32_t ext_rom_mapper_1, uint8_t* ext_rom_2, uint32_t ext_rom_size_2, uint32_t ext_rom_mapper_2)
		{
			rom_1 = new uint8_t[ext_rom_size_1];
			rom_2 = new uint8_t[ext_rom_size_2];

			std::memcpy(rom_1, ext_rom_1, ext_rom_size_1);
			std::memcpy(rom_2, ext_rom_2, ext_rom_size_2);

			rom_mapper_1 = ext_rom_mapper_1;
			rom_mapper_2 = ext_rom_mapper_2;

			// page size 0x2000 for konami games
			if (rom_mapper_1 == 1 || rom_mapper_1 == 2) { rom_size_1 = ext_rom_size_1 / 0x2000 - 1; }
			if (rom_mapper_2 == 1 || rom_mapper_2 == 2) { rom_size_2 = ext_rom_size_2 / 0x2000 - 1; }

			// initial state
			if (rom_mapper_1 == 1 || rom_mapper_1 == 2)
			{
				rom1_konami_page_0 = 0;
				rom1_konami_page_1 = 1;
				rom1_konami_page_2 = 2;
				rom1_konami_page_3 = 3;
			}
			if (rom_mapper_2 == 1 || rom_mapper_2 == 2)
			{
				rom2_konami_page_0 = 0;
				rom2_konami_page_1 = 1;
				rom2_konami_page_2 = 2;
				rom2_konami_page_3 = 3;
			}

			// page size 0x2000 for generic ascii 8kb mapper
			if (rom_mapper_1 == 3) { rom_size_1 = ext_rom_size_1 / 0x2000 - 1; }
			if (rom_mapper_2 == 3) { rom_size_2 = ext_rom_size_2 / 0x2000 - 1; }

			// reuse konami page names (same size) however different initial state
			if (rom_mapper_1 == 3)
			{
				rom1_konami_page_0 = 0;
				rom1_konami_page_1 = 0;
				rom1_konami_page_2 = 0;
				rom1_konami_page_3 = 0;
			}
			if (rom_mapper_2 == 3)
			{
				rom2_konami_page_0 = 0;
				rom2_konami_page_1 = 0;
				rom2_konami_page_2 = 0;
				rom2_konami_page_3 = 0;
			}

			// default memory map setup
			PortA8 = 0;

			// SCC regs that aren't readable return 0xFF
			for (uint16_t i = 0; i < 0x400; i++) 
			{
				if ((i & 0x80) == 0x80)
				{
					SCC_1_page[i] = 0xFF;
					SCC_2_page[i] = 0xFF;
				}
			}

			remap();
		}

		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			*saver = (uint8_t)(PortDEEnabled ? 1 : 0); saver++;
			*saver = (uint8_t)(lagged ? 1 : 0); saver++;
			*saver = (uint8_t)(start_pressed ? 1 : 0); saver++;
			*saver = (uint8_t)(SCC_1_enabled ? 1 : 0); saver++;
			*saver = (uint8_t)(SCC_2_enabled ? 1 : 0); saver++;

			*saver = kb_rows_sel; saver++;
			*saver = PortA8; saver++;
			*saver = reg_FFFC; saver++;
			*saver = reg_FFFD; saver++;
			*saver = reg_FFFE; saver++;
			*saver = reg_FFFF; saver++;

			*saver = slot_0_has_rom; saver++;
			*saver = slot_1_has_rom; saver++;
			*saver = slot_2_has_rom; saver++;
			*saver = slot_3_has_rom; saver++;

			*saver = rom1_konami_page_0; saver++;
			*saver = rom1_konami_page_1; saver++;
			*saver = rom1_konami_page_2; saver++;
			*saver = rom1_konami_page_3; saver++;
			*saver = rom2_konami_page_0; saver++;
			*saver = rom2_konami_page_1; saver++;
			*saver = rom2_konami_page_2; saver++;
			*saver = rom2_konami_page_3; saver++;

			std::memcpy(saver, &ram, 0x10000); saver += 0x10000;
			std::memcpy(saver, &cart_ram, 0x8000); saver += 0x8000;
			std::memcpy(saver, &SCC_1_page, 0x400); saver += 0x400;
			std::memcpy(saver, &SCC_2_page, 0x400); saver += 0x400;

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			PortDEEnabled = *loader == 1; loader++;
			lagged = *loader == 1; loader++;
			start_pressed = *loader == 1; loader++;
			SCC_1_enabled = *loader == 1; loader++;
			SCC_2_enabled = *loader == 1; loader++;

			kb_rows_sel = *loader; loader++;
			PortA8 = *loader; loader++;
			reg_FFFC = *loader; loader++;
			reg_FFFD = *loader; loader++;
			reg_FFFE = *loader; loader++;
			reg_FFFF = *loader; loader++;

			slot_0_has_rom = *loader; loader++;
			slot_1_has_rom = *loader; loader++;
			slot_2_has_rom = *loader; loader++;
			slot_3_has_rom = *loader; loader++;

			rom1_konami_page_0 = *loader; loader++;
			rom1_konami_page_1 = *loader; loader++;
			rom1_konami_page_2 = *loader; loader++;
			rom1_konami_page_3 = *loader; loader++;
			rom2_konami_page_0 = *loader; loader++;
			rom2_konami_page_1 = *loader; loader++;
			rom2_konami_page_2 = *loader; loader++;
			rom2_konami_page_3 = *loader; loader++;

			std::memcpy(&ram, loader, 0x10000); loader += 0x10000;
			std::memcpy(&cart_ram, loader, 0x8000); loader += 0x8000;
			std::memcpy(&SCC_1_page, loader, 0x400); loader += 0x400;
			std::memcpy(&SCC_2_page, loader, 0x400); loader += 0x400;

			remap();

			return loader;
		}

		#pragma endregion
	};
}

#endif
