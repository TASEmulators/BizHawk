using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// Mapper with built in EEPROM, also used with Kirby's tilt 'n tumble
	// The EEPROM contains 256 bytes of read/write memory
	public class MapperMBC7 : MapperBase
	{
		public int ROM_bank;
		public bool RAM_enable_1, RAM_enable_2;
		public int ROM_mask;
		public byte acc_x_low;
		public byte acc_x_high;
		public byte acc_y_low;
		public byte acc_y_high;
		public bool is_erased;

		public override void Initialize()
		{
			ROM_bank = 1;
			RAM_enable_1 = RAM_enable_2 = false;
			ROM_mask = Core._rom.Length / 0x4000 - 1;

			// some games have sizes that result in a degenerate ROM, account for it here
			if (ROM_mask > 4) { ROM_mask |= 3; }

			acc_x_low = 0;
			acc_x_high = 0x80;
			acc_y_low = 0;
			acc_y_high = 0x80;
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
			else if (addr < 0xA000)
			{
				return 0xFF;
			}
			else if (addr < 0xB000)
			{
				if (RAM_enable_2)
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

		public override byte PeekMemory(ushort addr)
		{
			return ReadMemory(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
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

					ROM_bank &= 0x100;
					ROM_bank |= value;
					ROM_bank &= ROM_mask;
				}
				else if (addr < 0x6000)
				{
					if (RAM_enable_1)
					{
						RAM_enable_2 = (value & 0xF0) == 0x40;
					}				
				}
			}
			else
			{
				if (RAM_enable_2)
				{
					Register_Access_Write(addr, value);
				}
			}
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("ROM_Bank", ref ROM_bank);
			ser.Sync("ROM_Mask", ref ROM_mask);
			ser.Sync("RAM_enable_1", ref RAM_enable_1);
			ser.Sync("RAM_enable_2", ref RAM_enable_2);
			ser.Sync("acc_x_low", ref acc_x_low);
			ser.Sync("acc_x_high", ref acc_x_high);
			ser.Sync("acc_y_low", ref acc_y_low);
			ser.Sync("acc_y_high", ref acc_y_high);
			ser.Sync("is_erased", ref is_erased);
		}

		public void Register_Access_Write(ushort addr, byte value)
		{
			if ((addr & 0xA0F0) == 0xA000)
			{
				if (value == 0x55)
				{
					is_erased = true;
					acc_x_low = 0;
					acc_x_high = 0x80;
					acc_y_low = 0;
					acc_y_high = 0x80;
				}
			}
			else if ((addr & 0xA0F0) == 0xA010)
			{
				if ((value == 0xAA) && is_erased)
				{
					// latch new accelerometer values
				}
			}
			else if ((addr & 0xA0F0) == 0xA080)
			{
				
			}
		}

		public byte Register_Access_Read(ushort addr)
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
				return 0xFF;
			}
			else
			{
				return 0xFF;
			}
		}


	}
}
