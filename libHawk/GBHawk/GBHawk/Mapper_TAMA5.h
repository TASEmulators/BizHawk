#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>
#include <math.h>

#include "Mapper_Base.h"

using namespace std;

namespace GBHawk
{
	class Mapper_TAMA5 : Mapper
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
}
