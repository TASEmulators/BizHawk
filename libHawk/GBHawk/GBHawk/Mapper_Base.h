#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace GBHawk
{
	class MemoryManager;

	class Mapper
	{
	public:

		Mapper()
		{

		}

		uint32_t* ROM_Length = nullptr;
		uint32_t* Cart_RAM_Length = nullptr;
		uint32_t* ROM = nullptr;
		uint32_t* Cart_RAM = nullptr;

		// Generic Mapper Variables
		bool RAM_enable;
		bool sel_mode;
		bool IR_signal;
		uint32_t ROM_bank;
		uint32_t RAM_bank;		
		uint32_t ROM_mask;
		uint32_t RAM_mask;

		// Common
		bool halt;
		uint32_t RTC_timer;
		uint32_t RTC_low_clock;	

		// HuC3
		bool timer_read;	
		uint8_t control;
		uint8_t chip_read;	
		uint32_t time_val_shift;
		uint32_t time;
		uint32_t RTC_seconds;

		// MBC3
		uint8_t RTC_regs[5] = {};
		uint8_t RTC_regs_latch[5] = {};
		bool RTC_regs_latch_wr;
		uint32_t RTC_offset;



		// TAMA5
		uint8_t RTC_regs_TAMA[10] = {};
		uint32_t ctrl;
		uint32_t RAM_addr_low;
		uint32_t RAM_addr_high;
		uint32_t RAM_val_low;
		uint32_t RAM_val_high;
		uint8_t Chip_return_low;
		uint8_t Chip_return_high;


		virtual uint8_t ReadMemory(uint32_t addr)
		{
			return 0;
		}

		virtual uint8_t PeekMemory(uint32_t addr)
		{
			return 0;
		}

		virtual void WriteMemory(uint32_t addr, uint8_t value)
		{
		}

		virtual void PokeMemory(uint32_t addr, uint8_t value)
		{
		}

	
		virtual void Dispose()
		{
		}

		virtual void Reset()
		{
		}

		virtual void Mapper_Tick()
		{
		}

		virtual void RTC_Get(int value, int index)
		{
		}
		/*
		virtual void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
		}

		protected void SetCDLROM(LR35902.eCDLogMemFlags flags, int cdladdr)
		{
			Core.SetCDL(flags, "ROM", cdladdr);
		}

		protected void SetCDLRAM(LR35902.eCDLogMemFlags flags, int cdladdr)
		{
			Core.SetCDL(flags, "CartRAM", cdladdr);
		}
		*/

		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			saver = bool_saver(RAM_enable, saver);
			saver = bool_saver(sel_mode, saver);
			saver = bool_saver(IR_signal, saver);
			saver = int_saver(ROM_bank, saver);
			saver = int_saver(RAM_bank, saver);
			saver = int_saver(ROM_mask, saver);
			saver = int_saver(RAM_mask, saver);

			saver = bool_saver(halt, saver);
			saver = int_saver(RTC_timer, saver);
			saver = int_saver(RTC_low_clock, saver);

			saver = bool_saver(timer_read, saver);
			saver = byte_saver(control, saver);
			saver = byte_saver(chip_read, saver);
			saver = int_saver(time_val_shift, saver);
			saver = int_saver(time, saver);
			saver = int_saver(RTC_seconds, saver);

			for (int i = 0; i < 5; i++) { saver = byte_saver(RTC_regs[i], saver); }
			for (int i = 0; i < 5; i++) { saver = byte_saver(RTC_regs_latch[i], saver); }
			saver = bool_saver(RTC_regs_latch_wr, saver);
			saver = int_saver(RTC_offset, saver);

			for (int i = 0; i < 10; i++) { saver = byte_saver(RTC_regs_TAMA[i], saver); }
			saver = byte_saver(Chip_return_low, saver);
			saver = byte_saver(Chip_return_high, saver);
			saver = int_saver(ctrl, saver);
			saver = int_saver(RAM_addr_low, saver);
			saver = int_saver(RAM_addr_high, saver);
			saver = int_saver(RAM_val_low, saver);
			saver = int_saver(RAM_val_high, saver);

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{

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

		#pragma endregion

	};
}
