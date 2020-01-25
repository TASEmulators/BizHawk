#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "Z80A.h"
#include "AY_3_8910.h"
#include "TMS9918A.h"
#include "Memory.h"

namespace MSXHawk
{
	class MSXCore
	{
	public:
		MSXCore() 
		{
			MemMap.cpu_pntr = &cpu;
			MemMap.vdp_pntr = &vdp;
			MemMap.psg_pntr = &psg;
			cpu.mem_ctrl = &MemMap;
			vdp.IRQ_PTR = &cpu.FlagI;
			vdp.SHOW_BG = vdp.SHOW_SPRITES = true;
			psg.Clock_Divider = 16;
		};

		TMS9918A vdp;
		Z80A cpu;
		AY_3_8910 psg;
		MemoryManager MemMap;

		void Load_BIOS(uint8_t* bios, uint8_t* basic)
		{
			MemMap.Load_BIOS(bios, basic);
		}

		void Load_ROM(uint8_t* ext_rom_1, uint32_t ext_rom_size_1, uint32_t ext_rom_mapper_1, uint8_t* ext_rom_2, uint32_t ext_rom_size_2, uint32_t ext_rom_mapper_2)
		{
			MemMap.Load_ROM(ext_rom_1, ext_rom_size_1, ext_rom_mapper_1, ext_rom_2, ext_rom_size_2, ext_rom_mapper_2);
		}

		bool FrameAdvance(uint8_t controller_1, uint8_t controller_2, bool render, bool rendersound)
		{
			MemMap.controller_byte_1 = controller_1;
			MemMap.controller_byte_2 = controller_2;
			MemMap.start_pressed = (controller_1 & 0x80) > 0;
			MemMap.lagged = true;

			uint32_t scanlinesPerFrame = 262;
			vdp.SpriteLimit = true;

			psg.num_samples = 0;
			psg.sampleclock = 0;

			for (uint32_t i = 0; i < scanlinesPerFrame; i++)
			{
				vdp.ScanLine = i;

				vdp.RenderScanline(i);

				if (vdp.ScanLine == 192)
				{
					vdp.InterruptPendingSet(true);

					if (vdp.EnableInterrupts()) { cpu.FlagI = true; }						
				}

				for (uint32_t j = 0; j < vdp.IPeriod; j++)
				{
					cpu.ExecuteOne();

					psg.psg_clock++;
					if (psg.psg_clock == psg.Clock_Divider) 
					{
						psg.generate_sound();
						psg.psg_clock = 0;
					}
					psg.sampleclock++;						
				}
			}

			return MemMap.lagged;
		}

		void GetVideo(uint32_t* dest) {
			uint32_t* src = vdp.FrameBuffer;
			uint32_t* dst = dest;

			std::memcpy(dst, src, sizeof uint32_t * 256 * 192);
		}

		uint32_t GetAudio(uint32_t* dest, uint32_t* n_samp) 
		{
			uint32_t* src = psg.samples;
			uint32_t* dst = dest;

			std::memcpy(dst, src, sizeof uint32_t * psg.num_samples * 2);
			n_samp[0] = psg.num_samples;

			return psg.sampleclock;
		}

		#pragma region State Save / Load

		void SaveState(uint8_t* saver)
		{
			saver = vdp.SaveState(saver);
			saver = cpu.SaveState(saver);
			saver = psg.SaveState(saver);
			saver = MemMap.SaveState(saver);
		}

		void LoadState(uint8_t* loader)
		{
			loader = vdp.LoadState(loader);
			loader = cpu.LoadState(loader);
			loader = psg.LoadState(loader);
			loader = MemMap.LoadState(loader);
		}

		#pragma endregion

		#pragma region Memory Domain Functions

		uint8_t GetSysBus(uint32_t addr)
		{
			cpu.bank_num = cpu.bank_offset = addr & 0xFFFF;
			cpu.bank_offset &= cpu.low_mask;
			cpu.bank_num = (cpu.bank_num >> cpu.bank_shift)& cpu.high_mask;

			return cpu.MemoryMap[cpu.bank_num][cpu.bank_offset];
		}

		uint8_t GetVRAM(uint32_t addr) 
		{
			return vdp.VRAM[addr & 0x3FFF];
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
				memcpy(d, cpu.NMI_event, l);
			}
			else
			{
				memcpy(d, cpu.IRQ_event, l);
			}
		}

		#pragma endregion		
	};
}

