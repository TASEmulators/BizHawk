#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "Z80A.h"
#include "PSG.h"
#include "VDP.h"
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
			vdp.INT_FLAG = &cpu.FlagI;
		};

		VDP vdp;
		Z80A cpu;
		SN76489sms psg;
		MemoryManager MemMap;

		void Load_ROM(uint8_t* ext_rom, uint32_t ext_rom_size, uint32_t ext_rom_mapper)
		{
			MemMap.Load_ROM(ext_rom, ext_rom_size, ext_rom_mapper);
		}

		bool FrameAdvance(uint8_t controller_1, uint8_t controller_2, bool render, bool rendersound)
		{
			MemMap.controller_byte_1 = controller_1;
			MemMap.controller_byte_2 = controller_2;
			MemMap.lagged = true;

			int scanlinesPerFrame = 262;
			vdp.SpriteLimit = true;
			for (int i = 0; i < scanlinesPerFrame; i++)
			{
				vdp.ScanLine = i;

				vdp.RenderCurrentScanline(render);

				vdp.ProcessFrameInterrupt();
				vdp.ProcessLineInterrupt();

				for (int j = 0; j < vdp.IPeriod; j++)
				{
					cpu.ExecuteOne();

					psg.generate_sound();
					/*
					s_L = psg.current_sample_L;
					s_R = psg.current_sample_R;

					if (s_L != old_s_L)
					{
						blip_L.AddDelta(sampleclock, s_L - old_s_L);
						old_s_L = s_L;
					}

					if (s_R != old_s_R)
					{
						blip_R.AddDelta(sampleclock, s_R - old_s_R);
						old_s_R = s_R;
					}

					sampleclock++;
					*/
				}

				if (vdp.ScanLine == scanlinesPerFrame - 1)
				{
					vdp.ProcessGGScreen();
					//vdp.ProcessOverscan();
				}
			}
			/*
			while (cpu.TotalExecutedCycles < 211936) {
				cpu.ExecuteOne();
			}
			*/
			return MemMap.lagged;
		}

		void GetVideo(uint32_t* dest) {
			uint32_t* src = vdp.GameGearFrameBuffer;
			uint32_t* dst = dest;

			for (int i = 0; i < 144; i++)
			{
				std::memcpy(dst, src, sizeof uint32_t * 160);
				src += 160;
				dst += 160;
			}
		}

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

