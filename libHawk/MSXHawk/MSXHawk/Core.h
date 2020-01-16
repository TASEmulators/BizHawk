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

		void SetTraceCallback(void (*callback)(int)) 
		{
			cpu.TraceCallback = callback;
		}

		void GetHeader(char* h)
		{
			memcpy(h, cpu.TraceHeader, *(&cpu.TraceHeader + 1) - cpu.TraceHeader);
		}

		int GetHeaderLength()
		{
			return *(&cpu.TraceHeader + 1) - cpu.TraceHeader;
		}

		void GetRegisterState(char* r, int t)
		{
			if (t == 0) 
			{
				memcpy(r, cpu.CPURegisterState().c_str(), cpu.CPURegisterState().length() + 1);
			}
			else 
			{
				memcpy(r, cpu.No_Reg, *(&cpu.No_Reg + 1) - cpu.No_Reg);
			}		
		}

		void GetDisassembly(char* d, int t)
		{
			if (t == 0)
			{
				memcpy(d, cpu.CPUDisassembly().c_str(), cpu.CPUDisassembly().length() + 1);
			}
			else if (t == 1)
			{
				memcpy(d, cpu.NMI_event, *(&cpu.NMI_event + 1) - cpu.NMI_event);
			}
			else
			{
				memcpy(d, cpu.IRQ_event, *(&cpu.IRQ_event + 1) - cpu.IRQ_event);
			}
		}
	};
}

