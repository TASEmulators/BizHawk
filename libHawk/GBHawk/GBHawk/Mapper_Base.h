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

		uint8_t* ROM = nullptr;
		uint8_t* Cart_RAM = nullptr;
		uint32_t* ROM_Length = nullptr;
		uint32_t* Cart_RAM_Length = nullptr;
		uint32_t* addr_access = nullptr;
		uint32_t* Acc_X_state = nullptr;
		uint32_t* Acc_Y_state = nullptr;

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

		// camera
		bool regs_enable;
		uint8_t regs_cam[0x80] = {};

		// sachen
		bool locked, locked_GBC, finished, reg_access;
		uint32_t ROM_bank_mask;
		uint32_t BASE_ROM_Bank;
		uint32_t addr_last;
		uint32_t counter;

		// TAMA5
		uint8_t RTC_regs_TAMA[10] = {};
		uint32_t ctrl;
		uint32_t RAM_addr_low;
		uint32_t RAM_addr_high;
		uint32_t RAM_val_low;
		uint32_t RAM_val_high;
		uint8_t Chip_return_low;
		uint8_t Chip_return_high;

		// MBC7
		bool RAM_enable_1, RAM_enable_2, is_erased;
		uint8_t acc_x_low;
		uint8_t acc_x_high;
		uint8_t acc_y_low;
		uint8_t acc_y_high;
		// EEPROM related
		bool CS_prev;
		bool CLK_prev;
		bool DI_prev;
		bool DO;
		bool instr_read;
		bool perf_instr;
		bool WR_EN;
		bool countdown_start;
		uint32_t instr_bit_counter;
		uint32_t instr;		
		uint32_t EE_addr;
		uint32_t instr_case;
		uint32_t instr_clocks;
		uint32_t EE_value;
		uint32_t countdown;
		


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

			saver = bool_saver(regs_enable, saver);
			for (int i = 0; i < 5; i++) { saver = byte_saver(regs_cam[i], saver); }

			saver = bool_saver(locked, saver);
			saver = bool_saver(locked_GBC, saver);
			saver = bool_saver(finished, saver);
			saver = bool_saver(reg_access, saver);
			saver = int_saver(ROM_bank_mask, saver);
			saver = int_saver(BASE_ROM_Bank, saver);
			saver = int_saver(addr_last, saver);
			saver = int_saver(counter, saver);

			for (int i = 0; i < 10; i++) { saver = byte_saver(RTC_regs_TAMA[i], saver); }
			saver = byte_saver(Chip_return_low, saver);
			saver = byte_saver(Chip_return_high, saver);
			saver = int_saver(ctrl, saver);
			saver = int_saver(RAM_addr_low, saver);
			saver = int_saver(RAM_addr_high, saver);
			saver = int_saver(RAM_val_low, saver);
			saver = int_saver(RAM_val_high, saver);

			saver = bool_saver(RAM_enable_1, saver);
			saver = bool_saver(RAM_enable_2, saver);
			saver = bool_saver(is_erased, saver);
			saver = byte_saver(acc_x_low, saver);
			saver = byte_saver(acc_x_high, saver);
			saver = byte_saver(acc_y_low, saver);
			saver = byte_saver(acc_y_high, saver);
			// EEPROM related
			saver = bool_saver(CS_prev, saver);
			saver = bool_saver(CLK_prev, saver);
			saver = bool_saver(DI_prev, saver);
			saver = bool_saver(DO, saver);
			saver = bool_saver(instr_read, saver);
			saver = bool_saver(perf_instr, saver);
			saver = bool_saver(WR_EN, saver);
			saver = bool_saver(countdown_start, saver);
			saver = int_saver(instr_bit_counter, saver);
			saver = int_saver(instr, saver);
			saver = int_saver(EE_addr, saver);
			saver = int_saver(instr_case, saver);
			saver = int_saver(instr_clocks, saver);
			saver = int_saver(EE_value, saver);
			saver = int_saver(countdown, saver);

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			loader = bool_loader(&RAM_enable, loader);
			loader = bool_loader(&sel_mode, loader);
			loader = bool_loader(&IR_signal, loader);
			loader = int_loader(&ROM_bank, loader);
			loader = int_loader(&RAM_bank, loader);
			loader = int_loader(&ROM_mask, loader);
			loader = int_loader(&RAM_mask, loader);

			loader = bool_loader(&halt, loader);
			loader = int_loader(&RTC_timer, loader);
			loader = int_loader(&RTC_low_clock, loader);

			loader = bool_loader(&timer_read, loader);
			loader = byte_loader(&control, loader);
			loader = byte_loader(&chip_read, loader);
			loader = int_loader(&time_val_shift, loader);
			loader = int_loader(&time, loader);
			loader = int_loader(&RTC_seconds, loader);

			for (int i = 0; i < 5; i++) { loader = byte_loader(&RTC_regs[i], loader); }
			for (int i = 0; i < 5; i++) { loader = byte_loader(&RTC_regs_latch[i], loader); }
			loader = bool_loader(&RTC_regs_latch_wr, loader);
			loader = int_loader(&RTC_offset, loader);

			loader = bool_loader(&regs_enable, loader);
			for (int i = 0; i < 5; i++) { loader = byte_loader(&regs_cam[i], loader); }

			loader = bool_loader(&locked, loader);
			loader = bool_loader(&locked_GBC, loader);
			loader = bool_loader(&finished, loader);
			loader = bool_loader(&reg_access, loader);
			loader = int_loader(&ROM_bank_mask, loader);
			loader = int_loader(&BASE_ROM_Bank, loader);
			loader = int_loader(&addr_last, loader);
			loader = int_loader(&counter, loader);

			for (int i = 0; i < 10; i++) { loader = byte_loader(&RTC_regs_TAMA[i], loader); }
			loader = byte_loader(&Chip_return_low, loader);
			loader = byte_loader(&Chip_return_high, loader);
			loader = int_loader(&ctrl, loader);
			loader = int_loader(&RAM_addr_low, loader);
			loader = int_loader(&RAM_addr_high, loader);
			loader = int_loader(&RAM_val_low, loader);
			loader = int_loader(&RAM_val_high, loader);

			loader = bool_loader(&RAM_enable_1, loader);
			loader = bool_loader(&RAM_enable_2, loader);
			loader = bool_loader(&is_erased, loader);
			loader = byte_loader(&acc_x_low, loader);
			loader = byte_loader(&acc_x_high, loader);
			loader = byte_loader(&acc_y_low, loader);
			loader = byte_loader(&acc_y_high, loader);
			// EEPROM related
			loader = bool_loader(&CS_prev, loader);
			loader = bool_loader(&CLK_prev, loader);
			loader = bool_loader(&DI_prev, loader);
			loader = bool_loader(&DO, loader);
			loader = bool_loader(&instr_read, loader);
			loader = bool_loader(&perf_instr, loader);
			loader = bool_loader(&WR_EN, loader);
			loader = bool_loader(&countdown_start, loader);
			loader = int_loader(&instr_bit_counter, loader);
			loader = int_loader(&instr, loader);
			loader = int_loader(&EE_addr, loader);
			loader = int_loader(&instr_case, loader);
			loader = int_loader(&instr_clocks, loader);
			loader = int_loader(&EE_value, loader);
			loader = int_loader(&countdown, loader);

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
