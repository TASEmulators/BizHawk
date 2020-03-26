#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "LR35902.h"
#include "GBAudio.h"
#include "PPU_Base.h"
#include "Memory.h"
#include "Timer.h"
#include "SerialPort.h"
#include "Mapper_Base.h"

namespace GBHawk
{
	class GBCore
	{
	public:
		GBCore() 
		{
			MemMap.cpu_pntr = &cpu;
			MemMap.ppu_pntr = &ppu;
			MemMap.psg_pntr = &psg;
			MemMap.timer_pntr = &timer;
			MemMap.serialport_pntr = &serialport;
			cpu.mem_ctrl = &MemMap;

			ppu.FlagI = &cpu.FlagI;
			ppu.in_vblank = &MemMap.in_vblank;
			ppu.cpu_LY = &cpu.LY;
			ppu.REG_FFFF = &MemMap.REG_FFFF;
			ppu.REG_FF0F = &MemMap.REG_FF0F;
			ppu._scanlineCallbackLine = &MemMap._scanlineCallbackLine;
			ppu.OAM = &MemMap.OAM[0];
			ppu.VRAM = &MemMap.VRAM[0];
			ppu.VRAM_Bank = &MemMap.VRAM_Bank;
			ppu.cpu_halted = &cpu.halted;
			ppu._vidbuffer = &MemMap._vidbuffer[0];
			ppu.color_palette = &MemMap.color_palette[0];
			ppu.HDMA_transfer = &MemMap.HDMA_transfer;
			ppu.GBC_compat = &MemMap.GBC_compat;

			timer.FlagI = &cpu.FlagI;
			timer.REG_FFFF = &MemMap.REG_FFFF;
			timer.REG_FF0F = &MemMap.REG_FF0F;

			serialport.GBC_compat = &MemMap.GBC_compat;
			serialport.FlagI = &cpu.FlagI;
			serialport.REG_FFFF = &MemMap.REG_FFFF;
			serialport.REG_FF0F = &MemMap.REG_FF0F;

			psg.is_GBC = &MemMap.is_GBC;
			psg.double_speed = &MemMap.double_speed;
			psg.timer_div_reg = &timer.divider_reg;
		};

		PPU ppu;
		LR35902 cpu;
		GBAudio psg;
		MemoryManager MemMap;
		Timer timer;
		SerialPort serialport;
		Mapper mapper;

		void Load_BIOS(uint8_t* bios, bool GBC_console)
		{
			MemMap.Load_BIOS(bios, GBC_console);
		}

		void Load_ROM(uint8_t* ext_rom_1, uint32_t ext_rom_size_1, uint32_t ext_rom_mapper_1, uint8_t* ext_rom_2, uint32_t ext_rom_size_2, uint32_t ext_rom_mapper_2)
		{
			MemMap.Load_ROM(ext_rom_1, ext_rom_size_1, ext_rom_mapper_1, ext_rom_2, ext_rom_size_2, ext_rom_mapper_2);
		}

		bool FrameAdvance(uint8_t controller_1, uint8_t controller_2, uint8_t* kb_rows_ptr, bool render, bool rendersound)
		{
			
			MemMap.controller_byte_1 = controller_1;
			MemMap.controller_byte_2 = controller_2;
			MemMap.kb_rows = kb_rows_ptr;
			MemMap.start_pressed = (controller_1 & 0x80) > 0;
			MemMap.lagged = true;

			uint32_t scanlinesPerFrame = 262;

			return MemMap.lagged;
		}

		void GetVideo(uint32_t* dest) 
		{
			uint32_t* src = MemMap.FrameBuffer;
			uint32_t* dst = dest;

			std::memcpy(dst, src, sizeof uint32_t * 256 * 192);
		}

		uint32_t GetAudio(int32_t* dest_L, int32_t* n_samp_L, int32_t* dest_R, int32_t* n_samp_R)
		{
			int32_t* src = psg.samples_L;
			int32_t* dst = dest_L;

			std::memcpy(dst, src, sizeof int32_t * psg.num_samples_L * 2);
			n_samp_L[0] = psg.num_samples_L;

			src = psg.samples_R;
			dst = dest_R;

			std::memcpy(dst, src, sizeof int32_t * psg.num_samples_R * 2);
			n_samp_R[0] = psg.num_samples_R;

			return psg.master_audio_clock;
		}

		#pragma region State Save / Load

		void SaveState(uint8_t* saver)
		{
			saver = ppu.SaveState(saver);
			saver = cpu.SaveState(saver);
			saver = psg.SaveState(saver);
			saver = MemMap.SaveState(saver);
		}

		void LoadState(uint8_t* loader)
		{
			loader = ppu.LoadState(loader);
			loader = cpu.LoadState(loader);
			loader = psg.LoadState(loader);
			loader = MemMap.LoadState(loader);
		}

		#pragma endregion

		#pragma region Memory Domain Functions

		uint8_t GetSysBus(uint32_t addr)
		{
			return cpu.PeekMemory(addr);
		}

		uint8_t GetVRAM(uint32_t addr) 
		{
			return MemMap.VRAM[addr & 0x3FFF];
		}

		uint8_t GetRAM(uint32_t addr)
		{
			return MemMap.RAM[addr & 0xFFFF];
		}

		#pragma endregion

		#pragma region Tracer

		void SetTraceCallback(void (*callback)(int))
		{
			cpu.TraceCallback = callback;
		}

		int GetHeaderLength()
		{
			return 105 + 1;
		}

		int GetDisasmLength()
		{
			return 48 + 1;
		}

		int GetRegStringLength()
		{
			return 86 + 1;
		}

		void GetHeader(char* h, int l)
		{
			memcpy(h, cpu.TraceHeader, l);
		}

		// the copy length l must be supplied ahead of time from GetRegStrngLength
		void GetRegisterState(char* r, int t, int l)
		{
			if (t == 0)
			{
				memcpy(r, cpu.CPURegisterState().c_str(), l);
			}
			else
			{
				memcpy(r, cpu.No_Reg, l);
			}
		}

		// the copy length l must be supplied ahead of time from GetDisasmLength
		void GetDisassembly(char* d, int t, int l)
		{
			if (t == 0)
			{
				memcpy(d, cpu.CPUDisassembly().c_str(), l);
			}
			else if (t == 1)
			{
				memcpy(d, cpu.Un_halt_event, l);
			}
			else if (t == 2)
			{
				memcpy(d, cpu.IRQ_event, l);
			}
			else
			{
				memcpy(d, cpu.Un_halt_event, l);
			}
		}

		#pragma endregion		
	};
}

