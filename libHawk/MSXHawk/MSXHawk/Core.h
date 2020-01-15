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

		VDP vdp;
		Z80A cpu;
		SN76489sms psg;
		MemoryManager MemMap;
	};
}

