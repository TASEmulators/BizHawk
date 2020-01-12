using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;

using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// hudson mapper used in ex Daikaijuu monogatari
	public class MapperHuC1 : MapperBase
	{
		public int ROM_bank;
		public int RAM_bank;
		public bool RAM_enable;
		public int ROM_mask;
		public int RAM_mask;
		public bool IR_signal;

		public override void Initialize()
		{
			ROM_bank = 0;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = Core._rom.Length / 0x4000 - 1;

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
					if (Core.cart_RAM != null)
					{
						if (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length)
						{
							Core.cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
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

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync(nameof(ROM_bank), ref ROM_bank);
			ser.Sync(nameof(ROM_mask), ref ROM_mask);
			ser.Sync(nameof(RAM_bank), ref RAM_bank);
			ser.Sync(nameof(RAM_mask), ref RAM_mask);
			ser.Sync(nameof(RAM_enable), ref RAM_enable);
			ser.Sync(nameof(IR_signal), ref IR_signal);
		}
	}
}
