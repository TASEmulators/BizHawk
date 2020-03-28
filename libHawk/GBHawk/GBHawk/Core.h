#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "LR35902.h"
#include "GBAudio.h"
#include "Memory.h"
#include "Timer.h"
#include "SerialPort.h"
#include "Mappers.h"
#include "PPU.h"

namespace GBHawk
{
	class GBCore
	{
	public:
		GBCore() 
		{

		};

		PPU* ppu;
		LR35902 cpu;
		GBAudio psg;
		MemoryManager MemMap;
		Timer timer;
		SerialPort serialport;
		Mapper* mapper;

		void Load_BIOS(uint8_t* bios, bool GBC_console, bool GBC_as_GBA)
		{
			MemMap.Load_BIOS(bios, GBC_console, GBC_as_GBA);
		}

		void Load_ROM(uint8_t* ext_rom_1, uint32_t ext_rom_size_1, string MD5, uint32_t RTC_initial, uint32_t RTC_offset)
		{
			MemMap.Load_ROM(ext_rom_1, ext_rom_size_1);

			// After we load the ROM we need to initialize the rest of the components (ppu and mapper)
			// tell the cpu the console type
			cpu.is_GBC = MemMap.is_GBC;

			//initialize system components
			// initialize the proper ppu
			if (MemMap.is_GBC)
			{
				if ((MemMap.header[0x43] != 0x80) && (MemMap.header[0x43] != 0xC0))
				{
					ppu = new GBC_GB_PPU();
				}
				else
				{
					ppu = new GBC_PPU();
				}
			}
			else
			{
				ppu = new GB_PPU();
			}

			MemMap.ppu_pntr = &ppu[0];

			// initialize the proper mapper
			Setup_Mapper(MD5, RTC_initial, RTC_offset);

			// set up pointers
			MemMap.cpu_pntr = &cpu;
			MemMap.psg_pntr = &psg;
			MemMap.timer_pntr = &timer;
			MemMap.serialport_pntr = &serialport;
			cpu.mem_ctrl = &MemMap;

			MemMap.ppu_pntr->FlagI = &cpu.FlagI;
			MemMap.ppu_pntr->in_vblank = &MemMap.in_vblank;
			MemMap.ppu_pntr->cpu_LY = &cpu.LY;
			MemMap.ppu_pntr->REG_FFFF = &MemMap.REG_FFFF;
			MemMap.ppu_pntr->REG_FF0F = &MemMap.REG_FF0F;
			MemMap.ppu_pntr->_scanlineCallbackLine = &MemMap._scanlineCallbackLine;
			MemMap.ppu_pntr->OAM = &MemMap.OAM[0];
			MemMap.ppu_pntr->VRAM = &MemMap.VRAM[0];
			MemMap.ppu_pntr->VRAM_Bank = &MemMap.VRAM_Bank;
			MemMap.ppu_pntr->cpu_halted = &cpu.halted;
			MemMap.ppu_pntr->_vidbuffer = &MemMap._vidbuffer[0];
			MemMap.ppu_pntr->color_palette = &MemMap.color_palette[0];
			MemMap.ppu_pntr->HDMA_transfer = &MemMap.HDMA_transfer;
			MemMap.ppu_pntr->GBC_compat = &MemMap.GBC_compat;

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

			MemMap.mapper_pntr->addr_access = &MemMap.addr_access;
			MemMap.mapper_pntr->Acc_X_state = &MemMap.Acc_X_state;
			MemMap.mapper_pntr->Acc_Y_state = &MemMap.Acc_Y_state;
			MemMap.mapper_pntr->ROM_Length = &MemMap.ROM_Length;
			MemMap.mapper_pntr->Cart_RAM_Length = &MemMap.Cart_RAM_Length;
			MemMap.mapper_pntr->ROM = &MemMap.ROM[0];
			MemMap.mapper_pntr->Cart_RAM = &MemMap.Cart_RAM[0];
		}

		bool FrameAdvance(uint8_t controller_1, uint8_t controller_2, uint8_t* kb_rows_ptr, bool render, bool rendersound)
		{
			
			MemMap.controller_byte_1 = controller_1;
			MemMap.controller_byte_2 = controller_2;
			MemMap.kb_rows = kb_rows_ptr;
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

		void Setup_Mapper(string MD5, uint32_t RTC_initial, uint32_t RTC_offset)
		{
			// setup up mapper based on header entry
			string mppr;

			switch (MemMap.header[0x47])
			{
			case 0x0: mapper = new Mapper_Default();		mppr = "NROM";									break;
			case 0x1: mapper = new Mapper_MBC1();			mppr = "MBC1";									break;
			case 0x2: mapper = new Mapper_MBC1();			mppr = "MBC1";									break;
			case 0x3: mapper = new Mapper_MBC1();			mppr = "MBC1";		MemMap.has_bat = true;		break;
			case 0x5: mapper = new Mapper_MBC2();			mppr = "MBC2";									break;
			case 0x6: mapper = new Mapper_MBC2();			mppr = "MBC2";		MemMap.has_bat = true;		break;
			case 0x8: mapper = new Mapper_Default();		mppr = "NROM";									break;
			case 0x9: mapper = new Mapper_Default();		mppr = "NROM";		MemMap.has_bat = true;		break;
			case 0xB: mapper = new Mapper_MMM01();			mppr = "MMM01";									break;
			case 0xC: mapper = new Mapper_MMM01();			mppr = "MMM01";									break;
			case 0xD: mapper = new Mapper_MMM01();			mppr = "MMM01";		MemMap.has_bat = true;		break;
			case 0xF: mapper = new Mapper_MBC3();			mppr = "MBC3";		MemMap.has_bat = true;		break;
			case 0x10: mapper = new Mapper_MBC3();			mppr = "MBC3";		MemMap.has_bat = true;		break;
			case 0x11: mapper = new Mapper_MBC3();			mppr = "MBC3";									break;
			case 0x12: mapper = new Mapper_MBC3();			mppr = "MBC3";									break;
			case 0x13: mapper = new Mapper_MBC3();			mppr = "MBC3";		MemMap.has_bat = true;		break;
			case 0x19: mapper = new Mapper_MBC5();			mppr = "MBC5";									break;
			case 0x1A: mapper = new Mapper_MBC5();			mppr = "MBC5";		MemMap.has_bat = true;		break;
			case 0x1B: mapper = new Mapper_MBC5();			mppr = "MBC5";									break;
			case 0x1C: mapper = new Mapper_MBC5();			mppr = "MBC5";									break;
			case 0x1D: mapper = new Mapper_MBC5();			mppr = "MBC5";									break;
			case 0x1E: mapper = new Mapper_MBC5();			mppr = "MBC5";		MemMap.has_bat = true;		break;
			case 0x20: mapper = new Mapper_MBC6();			mppr = "MBC6";									break;
			case 0x22: mapper = new Mapper_MBC7();			mppr = "MBC7";		MemMap.has_bat = true;		break;
			case 0xFC: mapper = new Mapper_Camera();		mppr = "CAM";		MemMap.has_bat = true;		break;
			case 0xFD: mapper = new Mapper_TAMA5();			mppr = "TAMA5";		MemMap.has_bat = true;		break;
			case 0xFE: mapper = new Mapper_HuC3();			mppr = "HuC3";									break;
			case 0xFF: mapper = new Mapper_HuC1();			mppr = "HuC1";									break;

				// Bootleg mappers
				// NOTE: Sachen mapper selection does not account for scrambling, so if another bootleg mapper
				// identifies itself as 0x31, this will need to be modified
			case 0x31: mapper = new Mapper_Sachen2();		mppr = "Schn2";									break;

			case 0x4:
			case 0x7:
			case 0xA:
			case 0xE:
			case 0x14:
			case 0x15:
			case 0x16:
			case 0x17:
			case 0x18:
			case 0x1F:
			case 0x21:
			default:
				// mapper not implemented
				mapper = nullptr;
			}

			// special case for multi cart mappers
			if ((MD5 == "97122B9B183AAB4079C8D36A4CE6E9C1") ||
				(MD5 == "9FB9C42CF52DCFDCFBAD5E61AE1B5777") ||
				(MD5 == "CF1F58AB72112716D3C615A553B2F481")
				)
			{
				mapper = new Mapper_MBC1_Multi();
			}

			// Wisdom Tree does not identify their mapper, so use hash instead
			if ((MD5 == "2C07CAEE51A1F0C91C72C7C6F380B0F6") || // Joshua
				(MD5 == "37E017C8D1A45BAB609FB5B43FB64337") || // Spiritual Warfare
				(MD5 == "AB1FA0ED0207B1D0D5F401F0CD17BEBF") || // Exodus
				(MD5 == "BA2AC3587B3E1B36DE52E740274071B0") || // Bible - KJV
				(MD5 == "8CDDB8B2DCD3EC1A3FDD770DF8BDA07C")    // Bible - NIV
				)
			{
				mapper = new Mapper_WT();
				mppr = "Wtree";
			}

			// special case for bootlegs
			if ((MD5 == "CAE0998A899DF2EE6ABA8E7695C2A096"))
			{
				mapper = new Mapper_RM8();
			}
			if ((MD5 == "D3C1924D847BC5D125BF54C2076BE27A"))
			{
				mapper = new Mapper_Sachen1();
				mppr = "Schn1";
			}

			MemMap.Cart_RAM = nullptr;

			switch (MemMap.header[0x49])
			{
			case 1:
				MemMap.Cart_RAM = new uint8_t[0x800];
				break;
			case 2:
				MemMap.Cart_RAM = new uint8_t[0x2000];
				break;
			case 3:
				MemMap.Cart_RAM = new uint8_t[0x8000];
				break;
			case 4:
				MemMap.Cart_RAM = new uint8_t[0x20000];
				break;
			case 5:
				MemMap.Cart_RAM = new uint8_t[0x10000];
				break;
			case 0:
				MemMap.has_bat = false;
				break;
			}

			// Sachen maper not known to have RAM
			if ((mppr == "Schn1") || (mppr == "Schn2"))
			{
				MemMap.Cart_RAM = nullptr;
				MemMap.Use_MT = true;
			}

			// mbc2 carts have built in RAM
			if (mppr == "MBC2")
			{
				MemMap.Cart_RAM = new uint8_t[0x200];
			}

			// mbc7 has 256 bytes of RAM, regardless of any header info
			if (mppr == "MBC7")
			{
				MemMap.Cart_RAM = new uint8_t[0x100];
				MemMap.has_bat = true;
			}

			// TAMA5 has 0x1000 bytes of RAM, regardless of any header info
			if (mppr == "TAMA5")
			{
				MemMap.Cart_RAM = new uint8_t[0x20];
				MemMap.has_bat = true;
			}

			MemMap.Cart_RAM_Length = sizeof(MemMap.Cart_RAM);

			if (MemMap.Cart_RAM != nullptr && (mppr != "MBC7"))
			{
				for (uint32_t i = 0; i < MemMap.Cart_RAM_Length; i++)
				{
					MemMap.Cart_RAM[i] = 0xFF;
				}
			}

			// Extra RTC initialization for mbc3, HuC3, and TAMA5
			if (mppr == "MBC3")
			{
				MemMap.Use_MT = true;

				mapper->RTC_Get(RTC_offset, 5);

				int days = (int)floor(RTC_initial / 86400.0);

				int days_upper = ((days & 0x100) >> 8) | ((days & 0x200) >> 2);

				mapper->RTC_Get(days_upper, 4);
				mapper->RTC_Get(days & 0xFF, 3);

				int remaining = RTC_initial - (days * 86400);

				int hours = (int)floor(remaining / 3600.0);

				mapper->RTC_Get(hours & 0xFF, 2);

				remaining = remaining - (hours * 3600);

				int minutes = (int)floor(remaining / 60.0);

				mapper->RTC_Get(minutes & 0xFF, 1);

				remaining = remaining - (minutes * 60);

				mapper->RTC_Get(remaining & 0xFF, 0);
			}

			if (mppr == "HuC3")
			{
				MemMap.Use_MT = true;

				int years = (int)floor(RTC_initial / 31536000.0);

				mapper->RTC_Get(years, 24);

				int remaining = RTC_initial - (years * 31536000);

				int days = (int)floor(remaining / 86400.0);
				int days_upper = (days >> 8) & 0xF;

				mapper->RTC_Get(days_upper, 20);
				mapper->RTC_Get(days & 0xFF, 12);

				remaining = remaining - (days * 86400);

				int minutes = (int)floor(remaining / 60.0);
				int minutes_upper = (minutes >> 8) & 0xF;

				mapper->RTC_Get(minutes_upper, 8);
				mapper->RTC_Get(remaining & 0xFF, 0);
			}

			if (mppr == "TAMA5")
			{
				MemMap.Use_MT = true;

				// currently no date / time input for TAMA5

			}
		}

		#pragma region State Save / Load

		void SaveState(uint8_t* saver)
		{
			saver = ppu->SaveState(saver);
			saver = cpu.SaveState(saver);
			saver = psg.SaveState(saver);
			saver = MemMap.SaveState(saver);
		}

		void LoadState(uint8_t* loader)
		{
			loader = ppu->LoadState(loader);
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

