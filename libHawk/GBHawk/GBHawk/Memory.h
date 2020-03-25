#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace GBHawk
{
	class LR35902;
	class PPU;
	class GBAudio;
	
	class MemoryManager
	{
	public:
				
		PPU* ppu_pntr = nullptr;
		GBAudio* psg_pntr = nullptr;
		LR35902* cpu_pntr = nullptr;
		uint8_t* rom_1 = nullptr;
		uint8_t* rom_2 = nullptr;
		uint8_t* bios_rom = nullptr;

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
		uint8_t RAM[0x10000] = {};
		uint8_t VRAM[0x10000] = {};
		uint8_t OAM[0x10000] = {};
		uint8_t cart_ram[0x8000] = {};
		uint8_t unmapped[0x400] = {};
		uint32_t _vidbuffer[160 * 144] = {};
		uint32_t color_palette[4] = { 0xFFFFFFFF , 0xFFAAAAAA, 0xFF555555, 0xFF000000 };

		uint32_t FrameBuffer[0x400] = {};

		// state shared amongst different components
		bool in_vblank;
		uint8_t REG_FFFF, REG_FF0F;
		uint8_t _scanlineCallbackLine;


		MemoryManager()
		{

		};

		uint8_t HardwareRead(uint32_t value);

		void HardwareWrite(uint32_t addr, uint8_t value);

		// NOTE: only called from source when both are available and of correct size (0x4000)
		void Load_BIOS(uint8_t* bios, bool GBC_console) 
		{
			if (GBC_console) 
			{
				bios_rom = new uint8_t[2304];
				memcpy(bios_rom, bios, 2304);
			}
			else 
			{
				bios_rom = new uint8_t[256];
				memcpy(bios_rom, bios, 256);
			}
			
			
			
		}

		void Load_ROM(uint8_t* ext_rom_1, uint32_t ext_rom_size_1, uint32_t ext_rom_mapper_1, uint8_t* ext_rom_2, uint32_t ext_rom_size_2, uint32_t ext_rom_mapper_2)
		{
			rom_1 = new uint8_t[ext_rom_size_1];
			rom_2 = new uint8_t[ext_rom_size_2];

			memcpy(rom_1, ext_rom_1, ext_rom_size_1);
			memcpy(rom_2, ext_rom_2, ext_rom_size_2);

			rom_size_1 = ext_rom_size_1 / 0x4000;
			rom_mapper_1 = ext_rom_mapper_1;

			rom_size_2 = ext_rom_size_2 / 0x4000;
			rom_mapper_2 = ext_rom_mapper_2;

			// default memory map setup
			PortA8 = 0;
		}

		void MemoryWrite(uint32_t addr, uint8_t value)
		{

		}

		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			*saver = (uint8_t)(PortDEEnabled ? 1 : 0); saver++;
			*saver = (uint8_t)(lagged ? 1 : 0); saver++;
			*saver = (uint8_t)(start_pressed ? 1 : 0); saver++;

			*saver = kb_rows_sel; saver++;
			*saver = PortA8; saver++;
			*saver = reg_FFFC; saver++;
			*saver = reg_FFFD; saver++;
			*saver = reg_FFFE; saver++;
			*saver = reg_FFFF; saver++;

			std::memcpy(saver, &RAM, 0x10000); saver += 0x10000;
			std::memcpy(saver, &cart_ram, 0x8000); saver += 0x8000;

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			PortDEEnabled = *loader == 1; loader++;
			lagged = *loader == 1; loader++;
			start_pressed = *loader == 1; loader++;

			kb_rows_sel = *loader; loader++;
			PortA8 = *loader; loader++;
			reg_FFFC = *loader; loader++;
			reg_FFFD = *loader; loader++;
			reg_FFFE = *loader; loader++;
			reg_FFFF = *loader; loader++;

			std::memcpy(&RAM, loader, 0x10000); loader += 0x10000;
			std::memcpy(&cart_ram, loader, 0x8000); loader += 0x8000;

			return loader;
		}

		#pragma endregion
	};
}