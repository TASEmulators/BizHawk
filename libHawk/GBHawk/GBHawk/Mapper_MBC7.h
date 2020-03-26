#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>
#include <math.h>

#include "Mapper_Base.h"

using namespace std;

namespace GBHawk
{
	class Mapper_MBC7 : Mapper
	{
	public:

		bool RAM_enable_1, RAM_enable_2;
		uint8_t acc_x_low;
		uint8_t acc_x_high;
		uint8_t acc_y_low;
		uint8_t acc_y_high;
		bool is_erased;

		// EEPROM related
		bool CS_prev;
		bool CLK_prev;
		bool DI_prev;
		bool DO;
		bool instr_read;
		bool perf_instr;
		uint32_t instr_bit_counter;
		uint32_t instr;
		bool WR_EN;
		uint32_t EE_addr;
		uint32_t instr_case;
		uint32_t instr_clocks;
		uint32_t EE_value;
		uint32_t countdown;
		bool countdown_start;


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
					acc_x_low = (uint8_t)(Core.Acc_X_state & 0xFF);
					acc_x_high = (uint8_t)((Core.Acc_X_state & 0xFF00) >> 8);
					acc_y_low = (uint8_t)(Core.Acc_Y_state & 0xFF);
					acc_y_high = (uint8_t)((Core.Acc_Y_state & 0xFF00) >> 8);
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
			bool CS = value.Bit(7);
			bool CLK = value.Bit(6);
			bool DI = value.Bit(1);

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
}
