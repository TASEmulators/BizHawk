#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace GBHawk
{
	class Mapper
	{
	public:
		#pragma region mapper base

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
		#pragma endregion

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

	#pragma region Camera

	class Mapper_Camera : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 1;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;

			RAM_mask = Cart_RAM_Length[0] / 0x2000 - 1;

			regs_enable = false;
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				return ROM[addr];
			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else
			{
				if (regs_enable)
				{
					if ((addr & 0x7F) == 0)
					{
						return 0;// regs[0];
					}
					else
					{
						return 0;
					}
				}
				else
				{
					if (/*RAM_enable && */(((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						return Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000];
					}
					else
					{
						return 0xFF;
					}
				}
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed, but is still effected by mode
				SetCDLROM(flags, addr);
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else
			{
				if (!regs_enable)
				{
					if ((((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						SetCDLRAM(flags, (addr - 0xA000) + RAM_bank * 0x2000);
					}
					else
					{
						return;
					}
				}
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x8000)
			{
				if (addr < 0x2000)
				{
					RAM_enable = (value & 0xF) == 0xA;
				}
				else if (addr < 0x4000)
				{
					ROM_bank = value;
					ROM_bank &= ROM_mask;
					//Console.WriteLine(addr + " " + value + " " + ROM_mask + " " + ROM_bank);
				}
				else if (addr < 0x6000)
				{
					if ((value & 0x10) == 0x10)
					{
						regs_enable = true;
					}
					else
					{
						regs_enable = false;
						RAM_bank = value & RAM_mask;
					}
				}
			}
			else
			{
				if (regs_enable)
				{
					regs_cam[(addr & 0x7F)] = (uint8_t)(value & 0x7);
				}
				else
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
					}
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};

	#pragma endregion

	#pragma region Default

	class Mapper_Default : public Mapper
	{
	public:

		void Reset()
		{
			// nothing to initialize
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x8000)
			{
				return ROM[addr];
			}
			else
			{
				if (Cart_RAM_Length > 0)
				{
					return Cart_RAM[addr - 0xA000];
				}
				else
				{
					return 0;
				}
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x8000)
			{
				SetCDLROM(flags, addr);
			}
			else
			{
				if (Cart_RAM != null)
				{
					SetCDLRAM(flags, addr - 0xA000);
				}
				else
				{
					return;
				}
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x8000)
			{
				// no mapping hardware available
			}
			else
			{
				if (Cart_RAM_Length > 0)
				{
					Cart_RAM[addr - 0xA000] = value;
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};

	#pragma endregion

	#pragma region HuC1

	class Mapper_HuC1 : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 0;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }

			RAM_mask = 0;
			if (Cart_RAM_Length[0] > 0)
			{
				RAM_mask = Cart_RAM_Length[0] / 0x2000 - 1;
				if (Cart_RAM_Length[0] == 0x800) { RAM_mask = 0; }
			}
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				return ROM[addr];
			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else if ((addr >= 0xA000) && (addr < 0xC000))
			{
				if (RAM_enable)
				{
					if (Cart_RAM_Length[0] > 0)
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0])
						{
							return Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000];
						}
						else
						{
							return 0xFF;
						}
					}
					else
					{
						return 0xFF;
					}
				}
				else
				{
					// when RAM isn't enabled, reading from this area will return IR sensor reading
					// for now we'll assume it never sees light (0xC0)
					return 0xC0;
				}
			}
			else
			{
				return 0xFF;
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				SetCDLROM(flags, addr);
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else if ((addr >= 0xA000) && (addr < 0xC000))
			{
				if (RAM_enable)
				{
					if (Cart_RAM != null)
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0])
						{
							SetCDLRAM(flags, (addr - 0xA000) + RAM_bank * 0x2000);
						}
						else
						{
							return;
						}
					}
					else
					{
						return;
					}
				}
				else
				{
					return;
				}
			}
			else
			{
				return;
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x8000)
			{
				if (addr < 0x2000)
				{
					RAM_enable = (value & 0xF) != 0xE;
				}
				else if (addr < 0x4000)
				{
					value &= 0x3F;

					ROM_bank &= 0xC0;
					ROM_bank |= value;
					ROM_bank &= ROM_mask;
				}
				else if (addr < 0x6000)
				{
					RAM_bank = value & 3;
					RAM_bank &= RAM_mask;
				}
			}
			else
			{
				if (RAM_enable)
				{
					if (Cart_RAM_Length[0] > 0)
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0])
						{
							Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
						}
					}
				}
				else
				{
					// I don't know if other bits here have an effect
					if (value == 1)
					{
						IR_signal = true;
					}
					else if (value == 0)
					{
						IR_signal = false;
					}
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};
	#pragma endregion

	#pragma region huC3

	class Mapper_HuC3 : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 0;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;
			control = 0;
			chip_read = 1;
			timer_read = false;
			time_val_shift = 0;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }

			RAM_mask = 0;
			if (Cart_RAM_Length[0] > 0)
			{
				RAM_mask = Cart_RAM_Length[0] / 0x2000 - 1;
				if (Cart_RAM_Length[0] == 0x800) { RAM_mask = 0; }
			}
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				return ROM[addr];
			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else if ((addr >= 0xA000) && (addr < 0xC000))
			{
				if ((control >= 0xB) && (control < 0xE))
				{
					if (control == 0xD)
					{
						return 1;
					}
					return chip_read;
				}

				if (RAM_enable)
				{
					if (Cart_RAM_Length[0] > 0)
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0])
						{
							return Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000];
						}
						else
						{
							return 0xFF;
						}
					}
					else
					{
						return 0xFF;
					}
				}
				else
				{
					// what to return if RAM not enabled and controller not selected?
					return 0xFF;
				}
			}
			else
			{
				return 0xFF;
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				SetCDLROM(flags, addr);
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else if ((addr >= 0xA000) && (addr < 0xC000))
			{
				if (RAM_enable)
				{
					if (Cart_RAM != null)
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0])
						{
							SetCDLRAM(flags, (addr - 0xA000) + RAM_bank * 0x2000);
						}
						else
						{
							return;
						}
					}
					else
					{
						return;
					}
				}
				else
				{
					return;
				}
			}
			else
			{
				return;
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x8000)
			{
				if (addr < 0x2000)
				{
					RAM_enable = (value & 0xA) == 0xA;
					control = value;
				}
				else if (addr < 0x4000)
				{
					if (value == 0) { value = 1; }

					ROM_bank = value;
					ROM_bank &= ROM_mask;
				}
				else if (addr < 0x6000)
				{
					RAM_bank = value;
					RAM_bank &= 0xF;
					RAM_bank &= RAM_mask;
				}
			}
			else
			{
				if (RAM_enable && ((control < 0xB) || (control > 0xE)))
				{
					if (Cart_RAM_Length[0] > 0)
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0])
						{
							Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
						}
					}
				}

				if (control == 0xB)
				{
					switch (value & 0xF0)
					{
					case 0x10:
						if (timer_read)
						{
							// return timer value
							chip_read = (uint8_t)((time >> time_val_shift) & 0xF);
							time_val_shift += 4;
							if (time_val_shift == 28) { time_val_shift = 0; }
						}
						break;
					case 0x20:
						break;
					case 0x30:
						if (!timer_read)
						{
							// write to timer
							if (time_val_shift == 0) { time = 0; }
							if (time_val_shift < 28)
							{
								time |= (uint32_t)((value & 0x0F) << time_val_shift);
								time_val_shift += 4;
								if (time_val_shift == 28) { timer_read = true; }
							}
						}
						break;
					case 0x40:
						// other commands
						switch (value & 0xF)
						{
						case 0x0:
							time_val_shift = 0;
							break;
						case 0x3:
							timer_read = false;
							time_val_shift = 0;
							break;
						case 0x7:
							timer_read = true;
							time_val_shift = 0;
							break;
						case 0xF:
							break;
						}
						break;
					case 0x50:
						break;
					case 0x60:
						timer_read = true;
						break;
					}
				}
				else if (control == 0xC)
				{
					// maybe IR
				}
				else if (control == 0xD)
				{
					// maybe IR
				}
			}
		}

		void RTC_Get(uint32_t value, uint32_t index)
		{
			time |= (uint32_t)((value & 0xFF) << index);
		}

		void Mapper_Tick()
		{
			RTC_timer++;

			if (RTC_timer == 128)
			{
				RTC_timer = 0;

				RTC_low_clock++;

				if (RTC_low_clock == 32768)
				{
					RTC_low_clock = 0;

					RTC_seconds++;
					if (RTC_seconds > 59)
					{
						RTC_seconds = 0;
						time++;
						if ((time & 0xFFF) > 1439)
						{
							time -= 1440;
							time += (1 << 12);
							if ((time >> 12) > 365)
							{
								time -= (365 << 12);
								time += (1 << 24);
							}
						}
					}
				}
			}
		}
	};

	#pragma endregion

	#pragma region MBC1

	class Mapper_MBC1 : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 1;
			RAM_bank = 0;
			RAM_enable = false;
			sel_mode = false;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }

			RAM_mask = 0;
			if (Cart_RAM_Length[0] > 0)
			{
				RAM_mask = Cart_RAM_Length[0] / 0x2000 - 1;
				if (Cart_RAM_Length[0] == 0x800) { RAM_mask = 0; }
			}
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed, but is still effected by mode
				if (sel_mode)
				{
					return ROM[(ROM_bank & 0x60) * 0x4000 + addr];
				}
				else
				{
					return ROM[addr];
				}
			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else
			{
				if (Cart_RAM_Length[0] > 0)
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						return Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000];
					}
					else
					{
						return 0xFF;
					}

				}
				else
				{
					return 0xFF;
				}
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed, but is still effected by mode
				if (sel_mode)
				{
					SetCDLROM(flags, (ROM_bank & 0x60) * 0x4000 + addr);
				}
				else
				{
					SetCDLROM(flags, addr);
				}
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else
			{
				if (Cart_RAM != null)
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						SetCDLRAM(flags, (addr - 0xA000) + RAM_bank * 0x2000);
					}
					else
					{
						return;
					}

				}
				else
				{
					return;
				}
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x8000)
			{
				if (addr < 0x2000)
				{
					RAM_enable = (value & 0xF) == 0xA;
				}
				else if (addr < 0x4000)
				{
					value &= 0x1F;

					// writing zero gets translated to 1
					if (value == 0) { value = 1; }

					ROM_bank &= 0xE0;
					ROM_bank |= value;
					ROM_bank &= ROM_mask;
				}
				else if (addr < 0x6000)
				{
					if (sel_mode && (Cart_RAM_Length[0] > 0))
					{
						RAM_bank = value & 3;
						RAM_bank &= RAM_mask;
					}
					else
					{
						ROM_bank &= 0x1F;
						ROM_bank |= ((value & 3) << 5);
						ROM_bank &= ROM_mask;
					}
				}
				else
				{
					sel_mode = (value & 1) > 0;

					if (sel_mode && (Cart_RAM_Length[0] > 0))
					{
						ROM_bank &= 0x1F;
						ROM_bank &= ROM_mask;
					}
					else
					{
						RAM_bank = 0;
					}
				}
			}
			else
			{
				if (Cart_RAM_Length[0] > 0)
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
					}
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};

	#pragma endregion

	#pragma region MBC1_Multi

	class Mapper_MBC1_Multi : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 1;
			RAM_bank = 0;
			RAM_enable = false;
			sel_mode = false;
			ROM_mask = (ROM_Length[0] / 0x4000 * 2) - 1; // due to how mapping works, we want a 1 bit higher mask
			RAM_mask = 0;
			if (Cart_RAM_Length[0] > 0)
			{
				RAM_mask = Cart_RAM_Length[0] / 0x2000 - 1;
				if (Cart_RAM_Length[0] == 0x800) { RAM_mask = 0; }
			}
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed, but is still effected by mode
				if (sel_mode)
				{
					return ROM[((ROM_bank & 0x60) >> 1) * 0x4000 + addr];
				}
				else
				{
					return ROM[addr];
				}
			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + (((ROM_bank & 0x60) >> 1) | (ROM_bank & 0xF)) * 0x4000];
			}
			else
			{
				if (Cart_RAM_Length[0] > 0)
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						return Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000];
					}
					else
					{
						return 0xFF;
					}

				}
				else
				{
					return 0;
				}
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed, but is still effected by mode
				if (sel_mode)
				{
					SetCDLROM(flags, ((ROM_bank & 0x60) >> 1) * 0x4000 + addr);
				}
				else
				{
					SetCDLROM(flags, addr);
				}
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + (((ROM_bank & 0x60) >> 1) | (ROM_bank & 0xF)) * 0x4000);
			}
			else
			{
				if (Cart_RAM != null)
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						SetCDLRAM(flags, (addr - 0xA000) + RAM_bank * 0x2000);
					}
					else
					{
						return;
					}

				}
				else
				{
					return;
				}
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x8000)
			{
				if (addr < 0x2000)
				{
					RAM_enable = ((value & 0xA) == 0xA);
				}
				else if (addr < 0x4000)
				{
					value &= 0x1F;

					// writing zero gets translated to 1
					if (value == 0) { value = 1; }

					ROM_bank &= 0xE0;
					ROM_bank |= value;
					ROM_bank &= ROM_mask;
				}
				else if (addr < 0x6000)
				{
					if (sel_mode && (Cart_RAM_Length[0] > 0))
					{
						RAM_bank = value & 3;
						RAM_bank &= RAM_mask;
					}
					else
					{
						ROM_bank &= 0x1F;
						ROM_bank |= ((value & 3) << 5);
						ROM_bank &= ROM_mask;
					}
				}
				else
				{
					sel_mode = (value & 1) > 0;

					if (sel_mode && (Cart_RAM_Length[0] > 0))
					{
						ROM_bank &= 0x1F;
						ROM_bank &= ROM_mask;
					}
					else
					{
						RAM_bank = 0;
					}
				}
			}
			else
			{
				if (Cart_RAM_Length[0] > 0)
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
					}
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};

	#pragma endregion

	#pragma region MBC2

	class Mapper_MBC2 : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 1;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				return ROM[addr];
			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else if ((addr >= 0xA000) && (addr < 0xA200))
			{
				if (RAM_enable)
				{
					return Cart_RAM[addr - 0xA000];
				}
				return 0xFF;
			}
			else
			{
				return 0xFF;
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				SetCDLROM(flags, addr);
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else if ((addr >= 0xA000) && (addr < 0xA200))
			{
				if (RAM_enable)
				{
					SetCDLRAM(flags, addr - 0xA000);
				}
				return;
			}
			else
			{
				return;
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x2000)
			{
				if ((addr & 0x100) == 0)
				{
					RAM_enable = ((value & 0xA) == 0xA);
				}
			}
			else if (addr < 0x4000)
			{
				if ((addr & 0x100) > 0)
				{
					ROM_bank = value & 0xF & ROM_mask;
					if (ROM_bank == 0) { ROM_bank = 1; }
				}
			}
			else if ((addr >= 0xA000) && (addr < 0xA200))
			{
				if (RAM_enable)
				{
					Cart_RAM[addr - 0xA000] = (uint8_t)(value & 0xF);
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};

	#pragma endregion

	#pragma region MBC3

	class Mapper_MBC3 : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 1;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }

			RAM_mask = 0;
			if (Cart_RAM_Length[0] > 0)
			{
				RAM_mask = Cart_RAM_Length[0] / 0x2000 - 1;
				if (Cart_RAM_Length[0] == 0x800) { RAM_mask = 0; }
			}

			RTC_regs_latch[0] = 0;
			RTC_regs_latch[1] = 0;
			RTC_regs_latch[2] = 0;
			RTC_regs_latch[3] = 0;
			RTC_regs_latch[4] = 0;

			RTC_regs_latch_wr = true;
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				return ROM[addr];
			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else
			{
				if (RAM_enable)
				{
					if ((Cart_RAM_Length[0] > 0) && (RAM_bank <= RAM_mask))
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0])
						{
							return Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000];
						}
						else
						{
							return 0xFF;
						}
					}

					if ((RAM_bank >= 8) && (RAM_bank <= 0xC))
					{
						//Console.WriteLine("reg: " + (RAM_bank - 8) + " value: " + RTC_regs_latch[RAM_bank - 8] + " cpu: " + Core.cpu.TotalExecutedCycles);
						return RTC_regs_latch[RAM_bank - 8];
					}
					else
					{
						return 0x0;
					}
				}
				else
				{
					return 0x0;
				}
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				SetCDLROM(flags, addr);
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else
			{
				if (RAM_enable)
				{
					if ((Cart_RAM != null) && (RAM_bank <= RAM_mask))
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0])
						{
							SetCDLRAM(flags, (addr - 0xA000) + RAM_bank * 0x2000);
						}
						else
						{
							return;
						}
					}

					if ((RAM_bank >= 8) && (RAM_bank <= 0xC))
					{
						return;
					}
					else
					{
						return;
					}
				}
				else
				{
					return;
				}
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x8000)
			{
				if (addr < 0x2000)
				{
					RAM_enable = ((value & 0xA) == 0xA);
				}
				else if (addr < 0x4000)
				{
					value &= 0x7F;

					// writing zero gets translated to 1
					if (value == 0) { value = 1; }

					ROM_bank = value;
					ROM_bank &= ROM_mask;
				}
				else if (addr < 0x6000)
				{
					RAM_bank = value;
				}
				else
				{
					if (!RTC_regs_latch_wr && ((value & 1) == 1))
					{
						for (uint32_t i = 0; i < 5; i++)
						{
							RTC_regs_latch[i] = RTC_regs[i];
						}
					}

					RTC_regs_latch_wr = (value & 1) > 0;
				}
			}
			else
			{
				if (RAM_enable)
				{
					if ((Cart_RAM_Length[0] > 0) && (RAM_bank <= RAM_mask))
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0])
						{
							Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
						}
					}
					else if ((RAM_bank >= 8) && (RAM_bank <= 0xC))
					{
						RTC_regs[RAM_bank - 8] = value;

						if ((RAM_bank - 8) == 0) { RTC_low_clock = RTC_timer = 0; }

						halt = (RTC_regs[4] & 0x40) > 0;
					}
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}

		void RTC_Get(uint32_t value, uint32_t index)
		{
			if (index < 5)
			{
				RTC_regs[index] = (uint8_t)value;
			}
			else
			{
				RTC_offset = value;
			}
		}

		void Mapper_Tick()
		{
			if (!halt)
			{
				RTC_timer++;

				if (RTC_timer == 128)
				{
					RTC_timer = 0;

					RTC_low_clock++;

					if (RTC_low_clock == 32768)
					{
						RTC_low_clock = 0;
						RTC_timer = RTC_offset;

						RTC_regs[0]++;

						if (RTC_regs[0] > 59)
						{
							RTC_regs[0] = 0;
							RTC_regs[1]++;
							if (RTC_regs[1] > 59)
							{
								RTC_regs[1] = 0;
								RTC_regs[2]++;
								if (RTC_regs[2] > 23)
								{
									RTC_regs[2] = 0;
									if (RTC_regs[3] < 0xFF)
									{
										RTC_regs[3]++;
									}
									else
									{
										RTC_regs[3] = 0;

										if ((RTC_regs[4] & 1) == 0)
										{
											RTC_regs[4] |= 1;
										}
										else
										{
											RTC_regs[4] &= 0xFE;
											RTC_regs[4] |= 0x80;
										}
									}
								}
							}
						}
					}
				}
			}
		}
	};

	#pragma endregion

	#pragma region MBC5

	class Mapper_MBC5 : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 1;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }
			if (ROM_mask > 0x100) { ROM_mask |= 0xFF; }

			RAM_mask = 0;
			if (Cart_RAM_Length[0] > 0)
			{
				RAM_mask = Cart_RAM_Length[0] / 0x2000 - 1;
				if (Cart_RAM_Length[0] == 0x800) { RAM_mask = 0; }
			}
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				return ROM[addr];
			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else
			{
				if (Cart_RAM_Length[0] > 0)
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						return Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000];
					}
					else
					{
						return 0xFF;
					}

				}
				else
				{
					return 0xFF;
				}
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				SetCDLROM(flags, addr);
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else
			{
				if (Cart_RAM != null)
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						SetCDLRAM(flags, (addr - 0xA000) + RAM_bank * 0x2000);
					}
					else
					{
						return;
					}

				}
				else
				{
					return;
				}
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x8000)
			{
				if (addr < 0x2000)
				{
					RAM_enable = (value & 0xF) == 0xA;
				}
				else if (addr < 0x3000)
				{
					value &= 0xFF;

					ROM_bank &= 0x100;
					ROM_bank |= value;
					ROM_bank &= ROM_mask;
				}
				else if (addr < 0x4000)
				{
					value &= 1;

					ROM_bank &= 0xFF;
					ROM_bank |= (value << 8);
					ROM_bank &= ROM_mask;
				}
				else if (addr < 0x6000)
				{
					RAM_bank = value & 0xF;
					RAM_bank &= RAM_mask;
				}
			}
			else
			{
				if (Cart_RAM_Length[0] > 0)
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Cart_RAM_Length[0]))
					{
						Cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
					}
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};

	#pragma endregion

	#pragma region MBC6

	class Mapper_MBC6 : public Mapper
	{
	public:

		void Reset()
		{
			// nothing to initialize
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x8000)
			{
				return ROM[addr];
			}
			else
			{
				if (Cart_RAM_Length[0] > 0)
				{
					return Cart_RAM[addr - 0xA000];
				}
				else
				{
					return 0;
				}
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x8000)
			{
				SetCDLROM(flags, addr);
			}
			else
			{
				if (Cart_RAM != null)
				{
					SetCDLRAM(flags, addr - 0xA000);
				}
				else
				{
					return;
				}
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x8000)
			{
				// no mapping hardware available
			}
			else
			{
				if (Cart_RAM_Length[0] > 0)
				{
					Cart_RAM[addr - 0xA000] = value;
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};

	#pragma endregion

	#pragma region MBC7

	class Mapper_MBC7 : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 1;
			RAM_enable_1 = RAM_enable_2 = false;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }

			acc_x_low = 0;
			acc_x_high = 0x80;
			acc_y_low = 0;
			acc_y_high = 0x80;

			// reset acceerometer
			is_erased = false;

			// EEPROM related
			CS_prev = CLK_prev = DI_prev = DO = instr_read = perf_instr = WR_EN = countdown_start = false;
			instr_bit_counter = instr = EE_addr = instr_case = instr_clocks = EE_value = countdown = 0;
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				return ROM[addr];
			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else if (addr < 0xA000)
			{
				return 0xFF;
			}
			else if (addr < 0xB000)
			{
				if (RAM_enable_1 && RAM_enable_2)
				{
					return Register_Access_Read(addr);
				}
				else
				{
					return 0xFF;
				}
			}
			else
			{
				return 0xFF;
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				SetCDLROM(flags, addr);
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else if (addr < 0xA000)
			{
				return;
			}
			else if (addr < 0xB000)
			{
				if (RAM_enable_1 && RAM_enable_2)
				{
					return;
				}
				else
				{
					return;
				}
			}
			else
			{
				return;
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0xA000)
			{
				if (addr < 0x2000)
				{
					RAM_enable_1 = (value & 0xF) == 0xA;
				}
				else if (addr < 0x4000)
				{
					value &= 0xFF;

					//Console.WriteLine(Core.cpu.TotalExecutedCycles);
					//Console.WriteLine(value);

					ROM_bank &= 0x100;
					ROM_bank |= value;
					ROM_bank &= ROM_mask;
				}
				else if (addr < 0x6000)
				{
					RAM_enable_2 = (value & 0xF0) == 0x40;
				}
			}
			else
			{
				if (RAM_enable_1 && RAM_enable_2)
				{
					Register_Access_Write(addr, value);
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}

		uint8_t Register_Access_Read(uint32_t addr)
		{
			if ((addr & 0xA0F0) == 0xA000)
			{
				return 0xFF;
			}
			else if ((addr & 0xA0F0) == 0xA010)
			{
				return 0xFF;
			}
			else if ((addr & 0xA0F0) == 0xA020)
			{
				return acc_x_low;
			}
			else if ((addr & 0xA0F0) == 0xA030)
			{
				return acc_x_high;
			}
			else if ((addr & 0xA0F0) == 0xA040)
			{
				return acc_y_low;
			}
			else if ((addr & 0xA0F0) == 0xA050)
			{
				return acc_y_high;
			}
			else if ((addr & 0xA0F0) == 0xA060)
			{
				return 0xFF;
			}
			else if ((addr & 0xA0F0) == 0xA070)
			{
				return 0xFF;
			}
			else if ((addr & 0xA0F0) == 0xA080)
			{
				return (uint8_t)((CS_prev ? 0x80 : 0) |
					(CLK_prev ? 0x40 : 0) |
					(DI_prev ? 2 : 0) |
					(DO ? 1 : 0));
			}
			else
			{
				return 0xFF;
			}
		}

		void Register_Access_Write(uint32_t addr, uint8_t value)
		{
			if ((addr & 0xA0F0) == 0xA000)
			{
				if (value == 0x55)
				{
					//Console.WriteLine("Erasing ACC");

					is_erased = true;
					acc_x_low = 0x00;
					acc_x_high = 0x80;
					acc_y_low = 0x00;
					acc_y_high = 0x80;
				}
			}
			else if ((addr & 0xA0F0) == 0xA010)
			{
				if ((value == 0xAA) && is_erased)
				{
					// latch new accelerometer values
					//Console.WriteLine("Latching ACC");
					acc_x_low = (uint8_t)(Acc_X_state[0] & 0xFF);
					acc_x_high = (uint8_t)((Acc_X_state[0] & 0xFF00) >> 8);
					acc_y_low = (uint8_t)(Acc_Y_state[0] & 0xFF);
					acc_y_high = (uint8_t)((Acc_Y_state[0] & 0xFF00) >> 8);
				}
			}
			else if ((addr & 0xA0F0) == 0xA080)
			{
				// EEPROM writes
				EEPROM_write(value);
			}
		}

		void EEPROM_write(uint8_t value)
		{
			bool CS = (value & 0x80) > 0;
			bool CLK = (value & 0x40) > 0;
			bool DI = (value & 0x2) > 0;

			// if we deselect the chip, complete instructions or countdown and stop
			if (!CS)
			{
				CS_prev = CS;
				CLK_prev = CLK;
				DI_prev = DI;

				DO = true;
				countdown_start = false;
				perf_instr = false;
				instr_read = false;

				//Console.Write("Chip De-selected: ");
				//Console.WriteLine(Core.cpu.TotalExecutedCycles);
			}

			if (!instr_read && !perf_instr)
			{
				// if we aren't performing an operation or reading an incoming instruction, we are waiting for one
				// this is signalled by CS and DI both being 1 while CLK goes from 0 to 1
				if (CLK && !CLK_prev && DI && CS)
				{
					instr_read = true;
					instr_bit_counter = 0;
					instr = 0;
					DO = false;
					//Console.Write("Initiating command: ");
					//Console.WriteLine(Core.cpu.TotalExecutedCycles);
				}
			}
			else if (instr_read && CLK && !CLK_prev)
			{
				// all instructions are 10 bits long
				instr = (instr << 1) | ((value & 2) >> 1);

				instr_bit_counter++;
				if (instr_bit_counter == 10)
				{
					instr_read = false;
					instr_clocks = 0;
					EE_addr = instr & 0x7F;
					EE_value = 0;

					switch (instr & 0x300)
					{
					case 0x0:
						switch (instr & 0xC0)
						{
						case 0x0: // disable writes
							instr_case = 0;
							WR_EN = false;
							DO = true;
							break;
						case 0x40: // fill mem with value
							instr_case = 1;
							perf_instr = true;
							break;
						case 0x80: // fill mem with FF
							instr_case = 2;
							if (WR_EN)
							{
								for (uint32_t i = 0; i < 256; i++)
								{
									Cart_RAM[i] = 0xFF;
								}
							}
							DO = true;
							break;
						case 0xC0: // enable writes
							instr_case = 3;
							WR_EN = true;
							DO = true;
							break;
						}
						break;
					case 0x100: // write to address
						instr_case = 4;
						perf_instr = true;
						break;
					case 0x200: // read from address
						instr_case = 5;
						perf_instr = true;
						break;
					case 0x300: // set address to FF
						instr_case = 6;
						if (WR_EN)
						{
							Cart_RAM[EE_addr * 2] = 0xFF;
							Cart_RAM[EE_addr * 2 + 1] = 0xFF;
						}
						DO = true;
						break;
					}

					//Console.Write("Selected Command: ");
					//Console.Write(instr_case);
					//Console.Write(" ");
					//Console.WriteLine(Core.cpu.TotalExecutedCycles);
				}
			}
			else if (perf_instr && CLK && !CLK_prev)
			{
				//Console.Write("Command In progress, Cycle: ");
				//Console.Write(instr_clocks);
				//Console.Write(" ");
				//Console.WriteLine(Core.cpu.TotalExecutedCycles);

				// for commands that require additional clocking
				switch (instr_case)
				{
				case 1:
					EE_value = (EE_value << 1) | ((value & 2) >> 1);

					if (instr_clocks == 15)
					{
						if (WR_EN)
						{
							for (uint32_t i = 0; i < 128; i++)
							{
								Cart_RAM[i * 2] = (uint8_t)(EE_value & 0xFF);
								Cart_RAM[i * 2 + 1] = (uint8_t)((EE_value & 0xFF00) >> 8);
							}
						}
						instr_case = 7;
						countdown = 8;
					}
					break;

				case 4:
					EE_value = (EE_value << 1) | ((value & 2) >> 1);

					if (instr_clocks == 15)
					{
						if (WR_EN)
						{
							Cart_RAM[EE_addr * 2] = (uint8_t)(EE_value & 0xFF);
							Cart_RAM[EE_addr * 2 + 1] = (uint8_t)((EE_value & 0xFF00) >> 8);
						}
						instr_case = 7;
						countdown = 8;
					}
					break;

				case 5:
					if ((instr_clocks >= 0) && (instr_clocks <= 7))
					{
						DO = ((Cart_RAM[EE_addr * 2 + 1] >> (7 - instr_clocks)) & 1) == 1;
					}
					else if ((instr_clocks >= 8) && (instr_clocks <= 15))
					{
						DO = ((Cart_RAM[EE_addr * 2] >> (15 - instr_clocks)) & 1) == 1;
					}

					if (instr_clocks == 15)
					{
						instr_case = 7;
						countdown = 8;
					}
					break;

				case 6:

					instr_case = 7;
					countdown = 8;
					break;

				case 7:
					// completed operations take time, so countdown a bit here. 
					// not cycle accurate for operations like writing to all of the EEPROM, but good enough

					break;
				}

				if (instr_case == 7)
				{
					perf_instr = false;
					countdown_start = true;
				}

				instr_clocks++;
			}
			else if (countdown_start)
			{
				countdown--;
				if (countdown == 0)
				{
					countdown_start = false;
					DO = true;

					//Console.Write("Command Complete: ");
					//Console.WriteLine(Core.cpu.TotalExecutedCycles);
				}
			}

			CS_prev = CS;
			CLK_prev = CLK;
			DI_prev = DI;
		}
	};

	#pragma endregion

	#pragma region MMM01

	class Mapper_MMM01 : public Mapper
	{
	public:

		void Reset()
		{
			// nothing to initialize
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x8000)
			{
				return ROM[addr];
			}
			else
			{
				if (Cart_RAM_Length[0] > 0)
				{
					return Cart_RAM[addr - 0xA000];
				}
				else
				{
					return 0;
				}
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x8000)
			{
				SetCDLROM(flags, addr);
			}
			else
			{
				if (Cart_RAM != null)
				{
					SetCDLRAM(flags, addr - 0xA000);
				}
				else
				{
					return;
				}
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x8000)
			{
				// no mapping hardware available
			}
			else
			{
				if (Cart_RAM_Length[0] > 0)
				{
					Cart_RAM[addr - 0xA000] = value;
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};

	#pragma endregion

	#pragma region RockMan8

	class Mapper_RM8 : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 1;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed
				return ROM[addr];

			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else
			{
				return 0xFF;
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed
				SetCDLROM(flags, addr);

			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else
			{
				return;
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if ((addr >= 0x2000) && (addr < 0x4000))
			{
				value &= 0x1F;

				if (value == 0) { value = 1; }

				// in hhugboy they just subtract 8, but to me looks like bits 4 and 5 are just swapped (and bit 4 is unused?)
				ROM_bank = ((value & 0xF) | ((value & 0x10) >> 1))& ROM_mask;
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};

	#pragma endregion

	#pragma region Sachen_MMC1

	class Mapper_Sachen1 : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 1;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;
			BASE_ROM_Bank = 0;
			ROM_bank_mask = 0xFF;
			locked = true;
			reg_access = false;
			addr_last = 0;
			counter = 0;
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				if (locked)
				{
					// header is scrambled
					if ((addr >= 0x100) && (addr < 0x200))
					{
						uint32_t temp0 = (addr & 1);
						uint32_t temp1 = (addr & 2);
						uint32_t temp4 = (addr & 0x10);
						uint32_t temp6 = (addr & 0x40);

						temp0 = temp0 << 6;
						temp1 = temp1 << 3;
						temp4 = temp4 >> 3;
						temp6 = temp6 >> 6;

						addr &= 0x1AC;
						addr |= (uint32_t)(temp0 | temp1 | temp4 | temp6);
					}
					addr |= 0x80;
				}

				return ROM[addr + BASE_ROM_Bank * 0x4000];
			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else
			{
				return 0xFF;
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				if (locked)
				{
					// header is scrambled
					if ((addr >= 0x100) && (addr < 0x200))
					{
						uint32_t temp0 = (addr & 1);
						uint32_t temp1 = (addr & 2);
						uint32_t temp4 = (addr & 0x10);
						uint32_t temp6 = (addr & 0x40);

						temp0 = temp0 << 6;
						temp1 = temp1 << 3;
						temp4 = temp4 >> 3;
						temp6 = temp6 >> 6;

						addr &= 0x1AC;
						addr |= (uint32_t)(temp0 | temp1 | temp4 | temp6);
					}
					addr |= 0x80;
				}

				SetCDLROM(flags, addr + BASE_ROM_Bank * 0x4000);
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else
			{
				return;
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x2000)
			{
				if (reg_access)
				{
					BASE_ROM_Bank = value;
				}
			}
			else if (addr < 0x4000)
			{
				ROM_bank = (value > 0) ? value : 1;

				if ((value & 0x30) == 0x30)
				{
					reg_access = true;
				}
				else
				{
					reg_access = false;
				}
			}
			else if (addr < 0x6000)
			{
				if (reg_access)
				{
					ROM_bank_mask = value;
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}

		void Mapper_Tick()
		{
			if (locked)
			{
				if (((addr_access[0] & 0x8000) == 0) && ((addr_last & 0x8000) > 0) && (addr_access[0] >= 0x100))
				{
					counter++;
				}

				if (addr_access[0] >= 0x100)
				{
					addr_last = addr_access[0];
				}

				if (counter == 0x30)
				{
					locked = false;
				}
			}
		}
	};

	#pragma endregion

	#pragma region Sachen_MMC2

	class Mapper_Sachen2 : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 1;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;
			BASE_ROM_Bank = 0;
			ROM_bank_mask = 0;
			locked = true;
			locked_GBC = false;
			finished = false;
			reg_access = false;
			addr_last = 0;
			counter = 0;
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				// header is scrambled
				if ((addr >= 0x100) && (addr < 0x200))
				{
					uint32_t temp0 = (addr & 1);
					uint32_t temp1 = (addr & 2);
					uint32_t temp4 = (addr & 0x10);
					uint32_t temp6 = (addr & 0x40);

					temp0 = temp0 << 6;
					temp1 = temp1 << 3;
					temp4 = temp4 >> 3;
					temp6 = temp6 >> 6;

					addr &= 0x1AC;
					addr |= (uint32_t)(temp0 | temp1 | temp4 | temp6);
				}

				if (locked_GBC) { addr |= 0x80; }

				return ROM[addr + BASE_ROM_Bank * 0x4000];
			}
			else if (addr < 0x8000)
			{
				uint32_t temp_bank = (ROM_bank & ~ROM_bank_mask) | (ROM_bank_mask & BASE_ROM_Bank);
				temp_bank &= ROM_mask;

				return ROM[(addr - 0x4000) + temp_bank * 0x4000];
			}
			else
			{
				return 0xFF;
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				// header is scrambled
				if ((addr >= 0x100) && (addr < 0x200))
				{
					uint32_t temp0 = (addr & 1);
					uint32_t temp1 = (addr & 2);
					uint32_t temp4 = (addr & 0x10);
					uint32_t temp6 = (addr & 0x40);

					temp0 = temp0 << 6;
					temp1 = temp1 << 3;
					temp4 = temp4 >> 3;
					temp6 = temp6 >> 6;

					addr &= 0x1AC;
					addr |= (uint32_t)(temp0 | temp1 | temp4 | temp6);
				}

				if (locked_GBC) { addr |= 0x80; }

				SetCDLROM(flags, addr + BASE_ROM_Bank * 0x4000);
			}
			else if (addr < 0x8000)
			{
				uint32_t temp_bank = (ROM_bank & ~ROM_bank_mask) | (ROM_bank_mask & BASE_ROM_Bank);
				temp_bank &= ROM_mask;

				SetCDLROM(flags, (addr - 0x4000) + temp_bank * 0x4000);
			}
			else
			{
				return;
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x2000)
			{
				if (reg_access)
				{
					BASE_ROM_Bank = value;
				}
			}
			else if (addr < 0x4000)
			{
				ROM_bank = (value > 0) ? (value) : 1;

				if ((value & 0x30) == 0x30)
				{
					reg_access = true;
				}
				else
				{
					reg_access = false;
				}
			}
			else if (addr < 0x6000)
			{
				if (reg_access)
				{
					ROM_bank_mask = value;
				}
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}

		void Mapper_Tick()
		{
			if (locked)
			{
				if (((addr_access[0] & 0x8000) == 0) && ((addr_last & 0x8000) > 0) && (addr_access[0] >= 0x100))
				{
					counter++;
				}

				if (addr_access[0] >= 0x100)
				{
					addr_last = addr_access[0];
				}

				if (counter == 0x30)
				{
					locked = false;
					locked_GBC = true;
					counter = 0;
				}
			}
			else if (locked_GBC)
			{
				if (((addr_access[0] & 0x8000) == 0) && ((addr_last & 0x8000) > 0) && (addr_access[0] >= 0x100))
				{
					counter++;
				}

				if (addr_access[0] >= 0x100)
				{
					addr_last = addr_access[0];
				}

				if (counter == 0x30)
				{
					locked_GBC = false;
					finished = true;
				}

				// The above condition seems to never be reached as described in the mapper notes
				// so for now add this one

				if ((addr_access[0] == 0x133) && (counter == 1))
				{
					locked_GBC = false;
					finished = true;
				}
			}
		}
	};

	#pragma endregion

	#pragma region TAMA5

	class Mapper_TAMA5 : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 0;
			RAM_bank = 0;
			ROM_mask = ROM_Length[0] / 0x4000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }

			RAM_mask = 0;
			if (Cart_RAM_Length[0] > 0)
			{
				RAM_mask = Cart_RAM_Length[0] / 0x2000 - 1;
				if (Cart_RAM_Length[0] == 0x800) { RAM_mask = 0; }
			}

			RAM_addr_low = RAM_addr_high = RAM_val_low = RAM_val_high = 0;
			Chip_return_low = Chip_return_high = 0;
			halt = false;

			ctrl = 0;
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x4000)
			{
				return ROM[addr];
			}
			else if (addr < 0x8000)
			{
				return ROM[(addr - 0x4000) + ROM_bank * 0x4000];
			}
			else
			{

				switch (ctrl)
				{
				case 0xA:
					// The game won't proceed unless this value (anded with 3) is 1
					// see bank 0: 0x1A7D to 0x1A89
					return 1;
				case 0xC:
					//Console.WriteLine("read low: " + Chip_return_low);
					return Chip_return_low;
				case 0xD:
					//Console.WriteLine("read high: " + Chip_return_high);
					return Chip_return_high;
				}

				return 0x0;
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x4000)
			{
				SetCDLROM(flags, addr);
			}
			else if (addr < 0x8000)
			{
				SetCDLROM(flags, (addr - 0x4000) + ROM_bank * 0x4000);
			}
			else
			{

			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr == 0xA000)
			{
				switch (ctrl)
				{
				case 0:
					ROM_bank &= 0xF0;
					ROM_bank |= (value & 0xF);
					break;
				case 1:
					ROM_bank &= 0x0F;
					ROM_bank |= ((value & 0x1) << 4);
					break;
				case 4:
					RAM_val_low = (value & 0xF);
					break;
				case 5:
					RAM_val_high = (value & 0xF);
					//Cart_RAM[(RAM_addr_high << 4) | RAM_addr_low] = (uint8_t)((RAM_val_high << 4) | RAM_val_low);
					break;
				case 6:
					RAM_addr_high = (value & 1);

					switch ((value & 0xE) >> 1)
					{
					case 0:
						// write to RAM
						Cart_RAM[(RAM_addr_high << 4) | RAM_addr_low] = (uint8_t)((RAM_val_high << 4) | RAM_val_low);
						break;
					case 1:
						// read from RAM
						Chip_return_high = (uint8_t)(Cart_RAM[(RAM_addr_high << 4) | RAM_addr_low] >> 4);
						Chip_return_low = (uint8_t)(Cart_RAM[(RAM_addr_high << 4) | RAM_addr_low] & 0xF);
						break;
					case 2:
						// read from RTC registers
						if (RAM_addr_low == 3)
						{
							Chip_return_high = RTC_regs_TAMA[2];
							Chip_return_low = RTC_regs_TAMA[1];
						}
						else if (RAM_addr_low == 6)
						{
							Chip_return_high = RTC_regs_TAMA[4];
							Chip_return_low = RTC_regs_TAMA[3];
						}
						else
						{
							Chip_return_high = 1;
							Chip_return_low = 1;
						}
						break;
					case 3:
						// write to RTC registers (probably wrong, not well tested)
						if (RAM_addr_low == 3)
						{
							RTC_regs_TAMA[2] = (uint8_t)(RAM_val_high & 0xF);
							RTC_regs_TAMA[1] = (uint8_t)(RAM_val_low & 0xF);
						}
						else if (RAM_addr_low == 6)
						{
							RTC_regs_TAMA[4] = (uint8_t)(RAM_val_high & 0xF);
							RTC_regs_TAMA[3] = (uint8_t)(RAM_val_low & 0xF);
						}
						else
						{

						}
						break;
					case 4:
						// read from seconds register (time changes are checked when it rolls over)
						Chip_return_low = (uint8_t)(RTC_regs_TAMA[0] & 0xF);
						break;
					}

					//Console.WriteLine("CTRL: " + (value >> 1) + " RAM_high:" + RAM_addr_high + " RAM_low: " + RAM_addr_low + " val: " + (uint8_t)((RAM_val_high << 4) | RAM_val_low) + " Cpu: " + Core.cpu.TotalExecutedCycles);
					break;
				case 7:
					RAM_addr_low = (value & 0xF);

					//Console.WriteLine(" RAM_low:" + RAM_addr_low + " Cpu: " + Core.cpu.TotalExecutedCycles);
					break;
				}
			}
			else if (addr == 0xA001)
			{
				ctrl = value;
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}

		void RTC_Get(uint32_t value, uint32_t index)
		{
			if (index < 10)
			{
				RTC_regs_TAMA[index] = (uint8_t)value;
			}
			else
			{
				RTC_offset = value;
			}
		}

		void Mapper_Tick()
		{
			if (!halt)
			{
				RTC_timer++;

				if (RTC_timer == 128)
				{
					RTC_timer = 0;

					RTC_low_clock++;

					if (RTC_low_clock == 32768)
					{
						RTC_low_clock = 0;
						RTC_timer = RTC_offset;

						RTC_regs_TAMA[0]++;

						if (RTC_regs_TAMA[0] > 59)
						{
							RTC_regs_TAMA[0] = 0;
							RTC_regs_TAMA[1]++;
							// 1's digit of minutes
							if (RTC_regs_TAMA[1] > 9)
							{
								RTC_regs_TAMA[1] = 0;
								RTC_regs_TAMA[2]++;
								// 10's digit of minutes
								if (RTC_regs_TAMA[2] > 5)
								{
									RTC_regs_TAMA[2] = 0;
									RTC_regs_TAMA[3]++;
									// 1's digit of hours
									if (RTC_regs_TAMA[3] > 9)
									{
										RTC_regs_TAMA[3] = 0;
										RTC_regs_TAMA[4]++;
										// 10's digit of hours
										if (RTC_regs_TAMA[4] > 2)
										{
											RTC_regs_TAMA[4] = 0;
											RTC_regs_TAMA[5]++;
										}
									}
								}
							}
						}
					}
				}
			}
		}
	};

	#pragma endregion

	#pragma region Wisdom Tree

	class Mapper_WT : public Mapper
	{
	public:

		void Reset()
		{
			ROM_bank = 0;
			ROM_mask = ROM_Length[0] / 0x8000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }
			if (ROM_mask > 0x100) { ROM_mask |= 0xFF; }
		}

		uint8_t ReadMemory(uint32_t addr)
		{
			if (addr < 0x8000)
			{
				return ROM[ROM_bank * 0x8000 + addr];
			}
			else
			{
				return 0xFF;
			}
		}

		/*
		void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x8000)
			{
				SetCDLROM(flags, ROM_bank * 0x8000 + addr);
			}
			else
			{
				return;
			}
		}
		*/

		uint8_t PeekMemory(uint32_t addr)
		{
			return ReadMemory(addr);
		}

		void WriteMemory(uint32_t addr, uint8_t value)
		{
			if (addr < 0x4000)
			{
				ROM_bank = ((addr << 1) & 0x1ff) >> 1;
				ROM_bank &= ROM_mask;
			}
		}

		void PokeMemory(uint32_t addr, uint8_t value)
		{
			WriteMemory(addr, value);
		}
	};

	#pragma endregion
}
