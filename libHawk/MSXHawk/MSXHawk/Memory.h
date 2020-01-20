#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace MSXHawk
{
	class Z80A;
	class VDP;
	class SN76489sms;
	
	class MemoryManager
	{
	public:
				
		VDP* vdp_pntr = nullptr;
		SN76489sms* psg_pntr = nullptr;
		Z80A* cpu_pntr = nullptr;
		uint8_t* rom = nullptr;

		// initialized by core loading, not savestated
		uint32_t rom_size;
		uint32_t rom_mapper;

		// State
		bool PortDEEnabled = false;
		bool lagged;
		bool start_pressed;

		uint8_t controller_byte_1, controller_byte_2;
		uint8_t Port01 = 0xFF;
		uint8_t Port02 = 0xFF;
		uint8_t Port03 = 0x00;
		uint8_t Port04 = 0xFF;
		uint8_t Port05 = 0x00;
		uint8_t Port3E = 0xAF;
		uint8_t Port3F = 0xFF;
		uint8_t PortDE = 0x00;		
		uint8_t reg_FFFC, reg_FFFD, reg_FFFE, reg_FFFF;
		uint8_t ram[0x2000] = {};
		uint8_t cart_ram[0x8000] = {};

		MemoryManager()
		{

		};

		uint8_t HardwareRead(uint32_t value);

		void HardwareWrite(uint32_t addr, uint8_t value);

		void remap_ROM_0();

		void remap_ROM_1();

		void remap_ROM_2();

		void remap_RAM();

		void Load_ROM(uint8_t* ext_rom, uint32_t ext_rom_size, uint32_t ext_rom_mapper)
		{
			rom = ext_rom;
			rom_size = ext_rom_size / 0x4000;
			rom_mapper = ext_rom_mapper;

			// default memory map setup
			reg_FFFC = 0;
			reg_FFFD = 0;
			reg_FFFE = 1;
			reg_FFFF = 2;
			remap_ROM_0();
			remap_ROM_1();
			remap_ROM_2();
			remap_RAM();
		}

		void MemoryWrite(uint32_t addr, uint8_t value)
		{
			switch (addr)
			{
			case 0xFFFC:
				reg_FFFC = value;
				remap_ROM_2();
				remap_RAM();
				break;
			case 0xFFFD:
				reg_FFFD = value;
				remap_ROM_0();
				break;
			case 0xFFFE:
				reg_FFFE = value;
				remap_ROM_1();
				break;
			case 0xFFFF:
				reg_FFFF = value;
				remap_ROM_2();
				break;
			}
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

			if ((Port3F & 0x0F) == 5)
			{
				if (Port3F >> 4 == 0x0F)
				{
					value |= 0xC0;
				}
					
				else
				{
					value &= 0x3F;
				}					
			}

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
			*saver = Port01; saver++;
			*saver = Port02; saver++; 
			*saver = Port03; saver++;
			*saver = Port04; saver++;
			*saver = Port05; saver++;
			*saver = Port3E; saver++;
			*saver = Port3F; saver++;
			*saver = PortDE; saver++;
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
			Port01 = *loader; loader++;
			Port02 = *loader; loader++;
			Port03 = *loader; loader++;
			Port04 = *loader; loader++;
			Port05 = *loader; loader++;
			Port3E = *loader; loader++;
			Port3F = *loader; loader++;
			PortDE = *loader; loader++;
			reg_FFFC = *loader; loader++;
			reg_FFFD = *loader; loader++;
			reg_FFFE = *loader; loader++;
			reg_FFFF = *loader; loader++;

			std::memcpy(&ram, loader, 0x2000); loader += 0x2000;
			std::memcpy(&cart_ram, loader, 0x2000); loader += 0x2000;

			remap_ROM_0();
			remap_ROM_1();
			remap_ROM_2();
			remap_RAM();

			return loader;
		}

		#pragma endregion
	};
}