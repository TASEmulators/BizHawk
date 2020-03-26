#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>
#include <math.h>

#include "Mapper_Base.h"

using namespace std;

namespace GBHawk
{
	class Mapper_HuC3 : Mapper
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
}
