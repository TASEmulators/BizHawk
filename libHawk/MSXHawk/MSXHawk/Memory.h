#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace MSXHawk
{
	class Z80A;
	class TMS9918A;
	class SN76489sms;
	
	class MemoryManager
	{
	public:
				
		TMS9918A* vdp_pntr = nullptr;
		SN76489sms* psg_pntr = nullptr;
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

		// State
		bool PortDEEnabled = false;
		bool lagged;
		bool start_pressed;

		uint8_t controller_byte_1, controller_byte_2;
		uint8_t PortA8 = 0x00;
		uint8_t reg_FFFC, reg_FFFD, reg_FFFE, reg_FFFF;
		uint8_t ram[0x10000] = {};
		uint8_t cart_ram[0x8000] = {};
		uint8_t unmapped[0x400] = {};

		MemoryManager()
		{

		};

		uint8_t HardwareRead(uint32_t value);

		void HardwareWrite(uint32_t addr, uint8_t value);

		void remap();

		void Load_BIOS(uint8_t* bios, uint8_t* basic) 
		{
			bios_rom = bios;
			basic_rom = basic;
		}

		void Load_ROM(uint8_t* ext_rom_1, uint32_t ext_rom_size_1, uint32_t ext_rom_mapper_1, uint8_t* ext_rom_2, uint32_t ext_rom_size_2, uint32_t ext_rom_mapper_2)
		{
			rom_1 = ext_rom_1;
			rom_size_1 = ext_rom_size_1 / 0x4000;
			rom_mapper_1 = ext_rom_mapper_1;
			rom_2 = ext_rom_2;
			rom_size_2 = ext_rom_size_2 / 0x4000;
			rom_mapper_2 = ext_rom_mapper_2;

			// default memory map setup
			PortA8 = 0;

			remap();
		}

		void MemoryWrite(uint32_t addr, uint8_t value)
		{

		}

		uint8_t ReadPort0()
		{
			lagged = false;

			uint8_t value = 0xFF;
			if (start_pressed)
			{
				value ^= 0x80;
			}

			return value;
		}

		uint8_t ReadControls1()
		{
			lagged = false;
			uint8_t value = 0xFF;

			value &= ~(controller_byte_1 & 0x3F);
			value &= ~(controller_byte_2 & 0xC0);

			return value;
		}

		uint8_t ReadControls2()
		{
			lagged = false;
			uint8_t value = 0xFF;

			value &= ~(controller_byte_2 & 0xF);

			return value;
		}

		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			*saver = (uint8_t)(PortDEEnabled ? 1 : 0); saver++;
			*saver = (uint8_t)(lagged ? 1 : 0); saver++;
			*saver = (uint8_t)(start_pressed ? 1 : 0); saver++;

			*saver = controller_byte_1; saver++;
			*saver = controller_byte_2; saver++;
			*saver = PortA8; saver++;
			*saver = reg_FFFC; saver++;
			*saver = reg_FFFD; saver++;
			*saver = reg_FFFE; saver++;
			*saver = reg_FFFF; saver++;

			std::memcpy(saver, &ram, 0x2000); saver += 0x2000;
			std::memcpy(saver, &cart_ram, 0x8000); saver += 0x8000;

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			PortDEEnabled = *loader == 1; loader++;
			lagged = *loader == 1; loader++;
			start_pressed = *loader == 1; loader++;

			controller_byte_1 = *loader; loader++;
			controller_byte_2 = *loader; loader++;
			PortA8 = *loader; loader++;
			reg_FFFC = *loader; loader++;
			reg_FFFD = *loader; loader++;
			reg_FFFE = *loader; loader++;
			reg_FFFF = *loader; loader++;

			std::memcpy(&ram, loader, 0x10000); loader += 0x10000;
			std::memcpy(&cart_ram, loader, 0x8000); loader += 0x8000;

			remap();

			return loader;
		}

		#pragma endregion
	};
}