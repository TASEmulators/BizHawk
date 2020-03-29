#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace GBHawk
{
	class LR35902;
	class Timer;
	class PPU;
	class GBAudio;
	class SerialPort;
	class Mapper;
	
	class MemoryManager
	{
	public:
				
		MemoryManager()
		{

		};

		uint8_t ReadMemory(uint32_t addr);
		uint8_t PeekMemory(uint32_t addr);
		void WriteMemory(uint32_t addr, uint8_t value);
		uint8_t Read_Registers(uint32_t addr);
		void Write_Registers(uint32_t addr, uint8_t value);

		#pragma region Declarations

		PPU* ppu_pntr = nullptr;
		GBAudio* psg_pntr = nullptr;
		LR35902* cpu_pntr = nullptr;
		Timer* timer_pntr = nullptr;
		SerialPort* serialport_pntr = nullptr;
		Mapper* mapper_pntr = nullptr;
		uint8_t* ROM = nullptr;
		uint8_t* Cart_RAM = nullptr;
		uint8_t* bios_rom = nullptr;

		// initialized by core loading, not savestated
		uint32_t ROM_Length;
		uint32_t ROM_Mapper;
		uint32_t Cart_RAM_Length;

		// State
		bool lagged;
		bool is_GBC;
		bool GBC_compat;
		bool speed_switch, double_speed;
		bool in_vblank;
		bool in_vblank_old;
		bool vblank_rise;
		bool GB_bios_register;
		bool HDMA_transfer;
		bool Use_MT;
		bool has_bat;

		uint8_t IR_reg, IR_mask, IR_signal, IR_receive, IR_self;

		// several undocumented GBC Registers
		uint8_t undoc_6C, undoc_72, undoc_73, undoc_74, undoc_75, undoc_76, undoc_77;
		uint8_t controller_state;

		uint8_t REG_FFFF, REG_FF0F, REG_FF0F_OLD;

		uint8_t _scanlineCallbackLine;
		uint8_t input_register;
		uint32_t RAM_Bank;
		uint32_t VRAM_Bank;
		uint32_t IR_write;
		uint32_t addr_access;
		uint32_t Acc_X_state;
		uint32_t Acc_Y_state;

		uint8_t ZP_RAM[0x80] = {};
		uint8_t RAM[0x8000] = {};
		uint8_t VRAM[0x4000] = {};
		uint8_t OAM[0xA0] = {};
		uint8_t header[0x50] = {};
		uint32_t vidbuffer[160 * 144] = {};
		uint32_t frame_buffer[160 * 144] = {};
		uint32_t color_palette[4] = { 0xFFFFFFFF , 0xFFAAAAAA, 0xFF555555, 0xFF000000 };

		const uint8_t GBA_override[13] = { 0xFF, 0x00, 0xCD, 0x03, 0x35, 0xAA, 0x31, 0x90, 0x94, 0x00, 0x00, 0x00, 0x00 };

		#pragma endregion

		#pragma region Functions

		// NOTE: only called when checks pass that the files are correct
		void Load_BIOS(uint8_t* bios, bool GBC_console, bool GBC_as_GBA)
		{
			if (GBC_console)
			{
				bios_rom = new uint8_t[2304];
				memcpy(bios_rom, bios, 2304);
				is_GBC = true;

				// set up IR variables if it's GBC
				IR_mask = 0; IR_reg = 0x3E; IR_receive = 2; IR_self = 2; IR_signal = 2;

				if (GBC_as_GBA) 
				{
					for (int i = 0; i < 13; i++)
					{
						bios_rom[i + 0xF3] = (uint8_t)((GBA_override[i] + bios_rom[i + 0xF3]) & 0xFF);
					}
					IR_mask = 2;
				}
			}
			else
			{
				bios_rom = new uint8_t[256];
				memcpy(bios_rom, bios, 256);
			}
		}

		void Load_ROM(uint8_t* ext_rom_1, uint32_t ext_rom_size_1)
		{
			ROM = new uint8_t[ext_rom_size_1];

			memcpy(ROM, ext_rom_1, ext_rom_size_1);

			ROM_Length = ext_rom_size_1;

			std::memcpy(header, ext_rom_1 + 0x100, 0x50);
		}

		// Switch Speed (GBC only)
		uint32_t SpeedFunc(uint32_t temp)
		{
			if (is_GBC)
			{
				if (speed_switch)
				{
					speed_switch = false;
					uint32_t ret = double_speed ? 70224 * 2 : 70224 * 2; // actual time needs checking
					double_speed = !double_speed;
					return ret;
				}

				// if we are not switching speed, return 0
				return 0;
			}

			// if we are in GB mode, return 0 indicating not switching speed
			return 0;
		}

		void Register_Reset()
		{
			input_register = 0xCF; // not reading any input

			REG_FFFF = 0;
			REG_FF0F = 0xE0;
			REG_FF0F_OLD = 0xE0;

			//undocumented registers
			undoc_6C = 0xFE;
			undoc_72 = 0;
			undoc_73 = 0;
			undoc_74 = 0;
			undoc_75 = 0x8F;
			undoc_76 = 0;
			undoc_77 = 0;
		}

		#pragma endregion

		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			saver = bool_saver(lagged, saver);
			saver = bool_saver(is_GBC, saver);
			saver = bool_saver(GBC_compat, saver);
			saver = bool_saver(speed_switch, saver);
			saver = bool_saver(double_speed, saver);
			saver = bool_saver(in_vblank, saver);
			saver = bool_saver(in_vblank_old, saver);
			saver = bool_saver(vblank_rise, saver);
			saver = bool_saver(GB_bios_register, saver);
			saver = bool_saver(HDMA_transfer, saver);
			saver = bool_saver(Use_MT, saver);
			saver = bool_saver(has_bat, saver);

			saver = byte_saver(IR_reg, saver);
			saver = byte_saver(IR_mask, saver);
			saver = byte_saver(IR_signal, saver);
			saver = byte_saver(IR_receive, saver);
			saver = byte_saver(IR_self, saver);
			saver = byte_saver(undoc_6C, saver);
			saver = byte_saver(undoc_72, saver);
			saver = byte_saver(undoc_73, saver);
			saver = byte_saver(undoc_74, saver);
			saver = byte_saver(undoc_75, saver);
			saver = byte_saver(undoc_76, saver);
			saver = byte_saver(undoc_77, saver);

			saver = byte_saver(controller_state, saver);

			saver = byte_saver(REG_FFFF, saver);
			saver = byte_saver(REG_FF0F, saver);
			saver = byte_saver(REG_FF0F_OLD, saver);
			saver = byte_saver(_scanlineCallbackLine, saver);
			saver = byte_saver(input_register, saver);

			saver = int_saver(RAM_Bank, saver);
			saver = int_saver(VRAM_Bank, saver);
			saver = int_saver(IR_write, saver);
			saver = int_saver(addr_access, saver);
			saver = int_saver(Acc_X_state, saver);
			saver = int_saver(Acc_Y_state, saver);

			for (int i = 0; i < 0x80; i++) { saver = byte_saver(ZP_RAM[i], saver); }
			for (int i = 0; i < 0x8000; i++) { saver = byte_saver(RAM[i], saver); }
			for (int i = 0; i < 0x4000; i++) { saver = byte_saver(VRAM[i], saver); }
			for (int i = 0; i < 0xA0; i++) { saver = byte_saver(OAM[i], saver); }
			for (int i = 0; i < 0x50; i++) { saver = byte_saver(header[i], saver); }

			for (int i = 0; i < (160 * 144); i++) { saver = int_saver(vidbuffer[i], saver); }
			for (int i = 0; i < (160 * 144); i++) { saver = int_saver(frame_buffer[i], saver); }

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			loader = bool_loader(&lagged, loader);
			loader = bool_loader(&is_GBC, loader);
			loader = bool_loader(&GBC_compat, loader);
			loader = bool_loader(&speed_switch, loader);
			loader = bool_loader(&double_speed, loader);
			loader = bool_loader(&in_vblank, loader);
			loader = bool_loader(&in_vblank_old, loader);
			loader = bool_loader(&vblank_rise, loader);
			loader = bool_loader(&GB_bios_register, loader);
			loader = bool_loader(&HDMA_transfer, loader);
			loader = bool_loader(&Use_MT, loader);
			loader = bool_loader(&has_bat, loader);

			loader = byte_loader(&IR_reg, loader);
			loader = byte_loader(&IR_mask, loader);
			loader = byte_loader(&IR_signal, loader);
			loader = byte_loader(&IR_receive, loader);
			loader = byte_loader(&IR_self, loader);
			loader = byte_loader(&undoc_6C, loader);
			loader = byte_loader(&undoc_72, loader);
			loader = byte_loader(&undoc_73, loader);
			loader = byte_loader(&undoc_74, loader);
			loader = byte_loader(&undoc_75, loader);
			loader = byte_loader(&undoc_76, loader);
			loader = byte_loader(&undoc_77, loader);

			loader = byte_loader(&controller_state, loader);

			loader = byte_loader(&REG_FFFF, loader);
			loader = byte_loader(&REG_FF0F, loader);
			loader = byte_loader(&REG_FF0F_OLD, loader);
			loader = byte_loader(&_scanlineCallbackLine, loader);
			loader = byte_loader(&input_register, loader);

			loader = int_loader(&RAM_Bank, loader);
			loader = int_loader(&VRAM_Bank, loader);
			loader = int_loader(&IR_write, loader);
			loader = int_loader(&addr_access, loader);
			loader = int_loader(&Acc_X_state, loader);
			loader = int_loader(&Acc_Y_state, loader);

			for (int i = 0; i < 0x80; i++) { loader = byte_loader(&ZP_RAM[i], loader); }
			for (int i = 0; i < 0x8000; i++) { loader = byte_loader(&RAM[i], loader); }
			for (int i = 0; i < 0x4000; i++) { loader = byte_loader(&VRAM[i], loader); }
			for (int i = 0; i < 0xA0; i++) { loader = byte_loader(&OAM[i], loader); }
			for (int i = 0; i < 0x50; i++) { loader = byte_loader(&header[i], loader); }

			for (int i = 0; i < (160 * 144); i++) { loader = int_loader(&vidbuffer[i], loader); }
			for (int i = 0; i < (160 * 144); i++) { loader = int_loader(&frame_buffer[i], loader); }

			return loader;
		}

		uint8_t* bool_saver(bool to_save, uint8_t* saver)
		{
			*saver = (uint8_t)(to_save ? 1 : 0); saver++;

			return saver;
		}

		uint8_t* byte_saver(uint8_t to_save, uint8_t* saver)
		{
			*saver = to_save; saver++;

			return saver;
		}

		uint8_t* int_saver(uint32_t to_save, uint8_t* saver)
		{
			*saver = (uint8_t)(to_save & 0xFF); saver++; *saver = (uint8_t)((to_save >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((to_save >> 16) & 0xFF); saver++; *saver = (uint8_t)((to_save >> 24) & 0xFF); saver++;

			return saver;
		}

		uint8_t* bool_loader(bool* to_load, uint8_t* loader)
		{
			to_load[0] = *to_load == 1; loader++;

			return loader;
		}

		uint8_t* byte_loader(uint8_t* to_load, uint8_t* loader)
		{
			to_load[0] = *loader; loader++;

			return loader;
		}

		uint8_t* int_loader(uint32_t* to_load, uint8_t* loader)
		{
			to_load[0] = *loader; loader++; to_load[0] |= (*loader << 8); loader++;
			to_load[0] |= (*loader << 16); loader++; to_load[0] |= (*loader << 24); loader++;

			return loader;
		}

		uint8_t* sint_loader(int32_t* to_load, uint8_t* loader)
		{
			to_load[0] = *loader; loader++; to_load[0] |= (*loader << 8); loader++;
			to_load[0] |= (*loader << 16); loader++; to_load[0] |= (*loader << 24); loader++;

			return loader;
		}

		#pragma endregion
	};
}