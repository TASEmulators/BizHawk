using System;
using BizHawk.Common;

using BizHawk.Emulation.Common.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// Hudson HuC3 used with Robopon and others
	public class MapperHuC3 : MapperBase
	{
		public int ROM_bank;
		public int RAM_bank;
		public bool RAM_enable;
		public int ROM_mask;
		public int RAM_mask;
		public bool IR_signal;
		public byte control;
		public byte chip_read;
		public bool timer_read;
		public int time_val_shift;
		public uint time;
		public int RTC_timer;
		public int RTC_low_clock;
		public int RTC_seconds;

		public override void Initialize()
		{
			ROM_bank = 0;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = Core._rom.Length / 0x4000 - 1;
			control = 0;
			chip_read = 1;
			timer_read = false;
			time_val_shift = 0;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }

			RAM_mask = 0;
			if (Core.cart_RAM != null)
			{
				RAM_mask = Core.cart_RAM.Length / 0x2000 - 1;
				if (Core.cart_RAM.Length == 0x800) { RAM_mask = 0; }
			}
		}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x4000)
			{
				return Core._rom[addr];
			}
			else if (addr < 0x8000)
			{
				return Core._rom[(addr - 0x4000) + ROM_bank * 0x4000];
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
					if (Core.cart_RAM != null)
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length)
						{
							return Core.cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000];
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

		public override void MapCDL(ushort addr, LR35902.eCDLogMemFlags flags)
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
					if (Core.cart_RAM != null)
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length)
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

		public override byte PeekMemory(ushort addr)
		{
			return ReadMemory(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
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
					if (Core.cart_RAM != null)
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length)
						{
							Core.cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
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
								chip_read = (byte)((time >> time_val_shift) & 0xF);
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
									time |= (uint)((value & 0x0F) << time_val_shift);
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

		public override void RTC_Get(int value, int index)
		{
			time |= (uint)((value & 0xFF) << index);
		}

		public override void Mapper_Tick()
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

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(ROM_bank), ref ROM_bank);
			ser.Sync(nameof(ROM_mask), ref ROM_mask);
			ser.Sync(nameof(RAM_bank), ref RAM_bank);
			ser.Sync(nameof(RAM_mask), ref RAM_mask);
			ser.Sync(nameof(RAM_enable), ref RAM_enable);
			ser.Sync(nameof(IR_signal), ref IR_signal);
			ser.Sync(nameof(control), ref control);
			ser.Sync(nameof(chip_read), ref chip_read);
			ser.Sync(nameof(timer_read), ref timer_read);
			ser.Sync(nameof(time_val_shift), ref time_val_shift);
			ser.Sync(nameof(time), ref time);
			ser.Sync(nameof(RTC_timer), ref RTC_timer);
			ser.Sync(nameof(RTC_low_clock), ref RTC_low_clock);
			ser.Sync(nameof(RTC_seconds), ref RTC_seconds);
		}
	}
}
