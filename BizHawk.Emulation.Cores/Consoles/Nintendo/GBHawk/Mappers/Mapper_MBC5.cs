using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// MBC5, common mapper for GBC games
	public class MapperMBC5 : MapperBase
	{
		public int ROM_bank;
		public int RAM_bank;
		public bool RAM_enable;
		public int ROM_mask;
		public int RAM_mask;

		public override void Initialize()
		{
			ROM_bank = 1;
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
			else
			{
				if (Core.cart_RAM != null)
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length))
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
				if (Core.cart_RAM != null)
				{
					if (RAM_enable && (((addr - 0xA000) + RAM_bank * 0x2000) < Core.cart_RAM.Length))
					{
						Core.cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000] = value;
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
			ser.Sync("ROM_Bank", ref ROM_bank);
			ser.Sync("ROM_Mask", ref ROM_mask);
			ser.Sync("RAM_Bank", ref RAM_bank);
			ser.Sync("RAM_Mask", ref RAM_mask);
			ser.Sync("RAM_enable", ref RAM_enable);
		}
	}
}
