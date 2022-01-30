#ifndef SCC_H
#define SCC_H

#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>
#include <cstring>

using namespace std;

namespace MSXHawk
{
	class SCC
	{
	public:

#pragma region SCC

		SCC() { }

		uint8_t* page_pntr = nullptr;

		bool ch_1_en, ch_2_en, ch_3_en, ch_4_en, ch_5_en;

		uint8_t ch_1_cnt, ch_2_cnt, ch_3_cnt, ch_4_cnt, ch_5_cnt;	
		uint8_t ch_1_vol, ch_2_vol, ch_3_vol, ch_4_vol, ch_5_vol;

		uint8_t ch_1_frl, ch_2_frl, ch_3_frl, ch_4_frl, ch_5_frl;
		uint8_t ch_1_frh, ch_2_frh, ch_3_frh, ch_4_frh, ch_5_frh;

		uint16_t ch_1_frq, ch_2_frq, ch_3_frq, ch_4_frq, ch_5_frq;
		uint16_t ch_1_clk, ch_2_clk, ch_3_clk, ch_4_clk, ch_5_clk;

		int32_t old_sample;
		int32_t current_sample;

		// channel output, not stated
		int32_t ch_1_out, ch_2_out, ch_3_out, ch_4_out, ch_5_out;

		/*
		const uint32_t VolumeTable[16] =
		{
			0x0000, 0x002A, 0x003C, 0x0055, 0x0078, 0x00AA, 0x00F1, 0x01FF,
			0x01E2, 0x02AA, 0x03C5, 0x0555, 0x078B, 0x0AAA, 0x0F15, 0x1555
		};
		*/

		const uint32_t VolumeTable[16] =
		{
			0,2,4,6,8,10,12,14,16,18,20,22,24,26,28,30
		};

		void Reset()
		{
			ch_1_clk = ch_2_clk = ch_3_clk = ch_4_clk = ch_5_clk = 0x1000;
			ch_1_cnt = ch_2_cnt = ch_3_cnt = ch_4_cnt = ch_5_cnt = 0;

			for (int i = 0; i < 0x90; i++)
			{
				WriteReg(i, 0);
			}
		}

		short Sample()
		{
			return current_sample;
		}

		// returns do not occur in this iplementation, they come from the core
		uint8_t ReadReg()
		{

		}

		void WriteReg(uint8_t addr, uint8_t value)
		{
			// addresses 0x90-0xA0 are the same as 0x80-90
			if ((addr >= 0x90) && (addr < 0xA0))
			{
				addr -= 0x10;
			}
			
			if (addr < 0x80)
			{
				// addresses below 0x80 (waveform tables) act as RAM, those above that range are write only
				page_pntr[addr] = value;
				page_pntr[addr + 0x100] = value;
				page_pntr[addr + 0x200] = value;
				page_pntr[addr + 0x300] = value;
			}
			else if (addr < 0x90)
			{
				// frequencies, volumes, enable
				if (addr == 0x80) { ch_1_frl = value; }
				else if (addr == 0x81) { ch_1_frh = (value & 0xF); }
				else if (addr == 0x82) { ch_2_frl = value; }
				else if (addr == 0x83) { ch_2_frh = (value & 0xF); }
				else if (addr == 0x84) { ch_3_frl = value; }
				else if (addr == 0x85) { ch_3_frh = (value & 0xF); }
				else if (addr == 0x86) { ch_4_frl = value; }
				else if (addr == 0x87) { ch_4_frh = (value & 0xF); }
				else if (addr == 0x88) { ch_5_frl = value; }
				else if (addr == 0x89) { ch_5_frh = (value & 0xF); }
				else if (addr == 0x8A) { ch_1_vol = value; }
				else if (addr == 0x8B) { ch_2_vol = value; }
				else if (addr == 0x8C) { ch_3_vol = value; }
				else if (addr == 0x8D) { ch_4_vol = value; }
				else if (addr == 0x8E) { ch_5_vol = value; }
				else if (addr == 0x8F)
				{ 
					ch_1_en = (value & 1) == 1;
					ch_2_en = (value & 2) == 2;
					ch_3_en = (value & 4) == 4;
					ch_4_en = (value & 8) == 8;
					ch_5_en = (value & 16) == 16;
				}

				ch_1_frq = (ch_1_frl | (ch_1_frh << 8)) + 1;
				ch_2_frq = (ch_2_frl | (ch_2_frh << 8)) + 1;
				ch_3_frq = (ch_3_frl | (ch_3_frh << 8)) + 1;
				ch_4_frq = (ch_4_frl | (ch_4_frh << 8)) + 1;
				ch_5_frq = (ch_5_frl | (ch_5_frh << 8)) + 1;

				if (ch_1_en) { ch_1_out = (int32_t)((int8_t)page_pntr[ch_1_cnt]) * VolumeTable[ch_1_vol]; } else { ch_1_out = 0; }
				if (ch_2_en) { ch_2_out = (int32_t)((int8_t)page_pntr[ch_2_cnt + 0x20]) * VolumeTable[ch_2_vol]; } else { ch_2_out = 0; }
				if (ch_3_en) { ch_3_out = (int32_t)((int8_t)page_pntr[ch_3_cnt + 0x40]) * VolumeTable[ch_3_vol]; } else { ch_3_out = 0; }
				if (ch_4_en) { ch_4_out = (int32_t)((int8_t)page_pntr[ch_4_cnt + 0x60]) * VolumeTable[ch_4_vol]; } else { ch_4_out = 0; }
				if (ch_5_en) { ch_5_out = (int32_t)((int8_t)page_pntr[ch_5_cnt + 0x60]) * VolumeTable[ch_5_vol]; } else { ch_5_out = 0; }
			}
			else 
			{
				// there is a test register in this range, but it is used by games, ignore for now
			}
		}

		bool generate_sound(int cycles)
		{		
			for (int i = 0; i < cycles; i++)
			{
				if (ch_1_en)
				{
					ch_1_clk--;

					if (ch_1_clk == 0)
					{
						ch_1_clk = ch_1_frq;
						ch_1_cnt++;
						ch_1_cnt &= 0x1F;

						ch_1_out = (int32_t)((int8_t)page_pntr[ch_1_cnt]) * VolumeTable[ch_1_vol];
					}
				}

				if (ch_2_en)
				{
					ch_2_clk--;

					if (ch_2_clk == 0)
					{
						ch_2_clk = ch_2_frq;
						ch_2_cnt++;
						ch_2_cnt &= 0x1F;

						ch_2_out = (int32_t)((int8_t)page_pntr[ch_2_cnt + 0x20]) * VolumeTable[ch_2_vol];
					}
				}

				if (ch_3_en)
				{
					ch_3_clk--;

					if (ch_3_clk == 0)
					{
						ch_3_clk = ch_3_frq;
						ch_3_cnt++;
						ch_3_cnt &= 0x1F;

						ch_3_out = (int32_t)((int8_t)page_pntr[ch_3_cnt + 0x40]) * VolumeTable[ch_3_vol];
					}
				}

				if (ch_4_en)
				{
					ch_4_clk--;

					if (ch_4_clk == 0)
					{
						ch_4_clk = ch_4_frq;
						ch_4_cnt++;
						ch_4_cnt &= 0x1F;

						ch_4_out = (int32_t)((int8_t)page_pntr[ch_4_cnt + 0x60]) * VolumeTable[ch_4_vol];
					}
				}

				if (ch_5_en)
				{
					ch_5_clk--;

					if (ch_5_clk == 0)
					{
						ch_5_clk = ch_5_frq;
						ch_5_cnt++;
						ch_5_cnt &= 0x1F;

						ch_5_out = (int32_t)((int8_t)page_pntr[ch_5_cnt + 0x60]) * VolumeTable[ch_5_vol];
					}
				}
			}

			current_sample = ch_1_out + ch_2_out + ch_3_out + ch_4_out + ch_5_out;

			if (current_sample != old_sample) { return true; }

			return false;
		}

#pragma endregion

#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			*saver = (uint8_t)(ch_1_en ? 1 : 0); saver++;
			*saver = (uint8_t)(ch_2_en ? 1 : 0); saver++;
			*saver = (uint8_t)(ch_3_en ? 1 : 0); saver++;
			*saver = (uint8_t)(ch_4_en ? 1 : 0); saver++;
			*saver = (uint8_t)(ch_5_en ? 1 : 0); saver++;

			*saver = ch_1_cnt; saver++;
			*saver = ch_2_cnt; saver++;
			*saver = ch_3_cnt; saver++;
			*saver = ch_4_cnt; saver++;
			*saver = ch_5_cnt; saver++;

			*saver = ch_1_vol; saver++;
			*saver = ch_2_vol; saver++;
			*saver = ch_3_vol; saver++;
			*saver = ch_4_vol; saver++;
			*saver = ch_5_vol; saver++;

			*saver = ch_1_frl; saver++;
			*saver = ch_2_frl; saver++;
			*saver = ch_3_frl; saver++;
			*saver = ch_4_frl; saver++;
			*saver = ch_5_frl; saver++;

			*saver = ch_1_frh; saver++;
			*saver = ch_2_frh; saver++;
			*saver = ch_3_frh; saver++;
			*saver = ch_4_frh; saver++;
			*saver = ch_5_frh; saver++;

			*saver = (uint8_t)(ch_1_frq & 0xFF); saver++; *saver = (uint8_t)((ch_1_frq >> 8) & 0xFF); saver++;
			*saver = (uint8_t)(ch_2_frq & 0xFF); saver++; *saver = (uint8_t)((ch_2_frq >> 8) & 0xFF); saver++;
			*saver = (uint8_t)(ch_3_frq & 0xFF); saver++; *saver = (uint8_t)((ch_3_frq >> 8) & 0xFF); saver++;
			*saver = (uint8_t)(ch_4_frq & 0xFF); saver++; *saver = (uint8_t)((ch_4_frq >> 8) & 0xFF); saver++;
			*saver = (uint8_t)(ch_5_frq & 0xFF); saver++; *saver = (uint8_t)((ch_5_frq >> 8) & 0xFF); saver++;

			*saver = (uint8_t)(ch_1_clk & 0xFF); saver++; *saver = (uint8_t)((ch_1_clk >> 8) & 0xFF); saver++;
			*saver = (uint8_t)(ch_2_clk & 0xFF); saver++; *saver = (uint8_t)((ch_2_clk >> 8) & 0xFF); saver++;
			*saver = (uint8_t)(ch_3_clk & 0xFF); saver++; *saver = (uint8_t)((ch_3_clk >> 8) & 0xFF); saver++;
			*saver = (uint8_t)(ch_4_clk & 0xFF); saver++; *saver = (uint8_t)((ch_4_clk >> 8) & 0xFF); saver++;
			*saver = (uint8_t)(ch_5_clk & 0xFF); saver++; *saver = (uint8_t)((ch_5_clk >> 8) & 0xFF); saver++;

			*saver = (uint8_t)(old_sample & 0xFF); saver++; *saver = (uint8_t)((old_sample >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((old_sample >> 16) & 0xFF); saver++; *saver = (uint8_t)((old_sample >> 24) & 0xFF); saver++;

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			ch_1_en = *loader == 1; loader++;
			ch_2_en = *loader == 1; loader++;
			ch_3_en = *loader == 1; loader++;
			ch_4_en = *loader == 1; loader++;
			ch_5_en = *loader == 1; loader++;

			ch_1_cnt = *loader; loader++;
			ch_2_cnt = *loader; loader++;
			ch_3_cnt = *loader; loader++;
			ch_4_cnt = *loader; loader++;
			ch_5_cnt = *loader; loader++;

			ch_1_vol = *loader; loader++;
			ch_2_vol = *loader; loader++;
			ch_3_vol = *loader; loader++;
			ch_4_vol = *loader; loader++;
			ch_5_vol = *loader; loader++;

			ch_1_frl = *loader; loader++;
			ch_2_frl = *loader; loader++;
			ch_3_frl = *loader; loader++;
			ch_4_frl = *loader; loader++;
			ch_5_frl = *loader; loader++;

			ch_1_frh = *loader; loader++;
			ch_2_frh = *loader; loader++;
			ch_3_frh = *loader; loader++;
			ch_4_frh = *loader; loader++;
			ch_5_frh = *loader; loader++;

			ch_1_frq = *loader; loader++; ch_1_frq |= (*loader << 8); loader++;
			ch_2_frq = *loader; loader++; ch_2_frq |= (*loader << 8); loader++;
			ch_3_frq = *loader; loader++; ch_3_frq |= (*loader << 8); loader++;
			ch_4_frq = *loader; loader++; ch_4_frq |= (*loader << 8); loader++;
			ch_5_frq = *loader; loader++; ch_4_frq |= (*loader << 8); loader++;

			ch_1_clk = *loader; loader++; ch_1_clk |= (*loader << 8); loader++;
			ch_2_clk = *loader; loader++; ch_2_clk |= (*loader << 8); loader++;
			ch_3_clk = *loader; loader++; ch_3_clk |= (*loader << 8); loader++;
			ch_4_clk = *loader; loader++; ch_4_clk |= (*loader << 8); loader++;
			ch_5_clk = *loader; loader++; ch_5_clk |= (*loader << 8); loader++;

			old_sample = *loader; loader++; old_sample |= (*loader << 8); loader++;
			old_sample |= (*loader << 16); loader++; old_sample |= (*loader << 24); loader++;

			if (ch_1_en) { ch_1_out = (int32_t)((int8_t)page_pntr[ch_1_cnt]) * VolumeTable[ch_1_vol]; } else { ch_1_out = 0; }
			if (ch_2_en) { ch_2_out = (int32_t)((int8_t)page_pntr[ch_2_cnt + 0x20]) * VolumeTable[ch_2_vol]; } else { ch_2_out = 0; }
			if (ch_3_en) { ch_3_out = (int32_t)((int8_t)page_pntr[ch_3_cnt + 0x40]) * VolumeTable[ch_3_vol]; } else { ch_3_out = 0; }
			if (ch_4_en) { ch_4_out = (int32_t)((int8_t)page_pntr[ch_4_cnt + 0x60]) * VolumeTable[ch_4_vol]; } else { ch_4_out = 0; }
			if (ch_5_en) { ch_5_out = (int32_t)((int8_t)page_pntr[ch_5_cnt + 0x60]) * VolumeTable[ch_5_vol]; } else { ch_5_out = 0; }

			return loader;
		}

#pragma endregion
	};
}

#endif
