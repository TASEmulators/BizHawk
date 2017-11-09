using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// MBC1 with bank switching and RAM
	public class MapperMBC1 : MapperBase
	{
		public int ROM_bank;
		public int RAM_bank;
		public bool RAM_enable;
		public bool sel_mode;

		public override void Initialize()
		{
			ROM_bank = 1;
			RAM_bank = 0;
			RAM_enable = false;
			sel_mode = false;
	}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x4000)
			{
				// lowest bank is fixed
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
					if (RAM_enable)
					{
						return Core.cart_RAM[(addr - 0xA000) + RAM_bank * 0x2000];
					}
					else
					{
						return 0;
					}
					
				}
				else
				{
					return 0;
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
					RAM_enable = ((value & 0xA) == 0xA) ? true : false;
				}
				else if (addr < 0x4000)
				{
					value &= 0x1F;

					// writing zero gets translated to 1
					if (value == 0) { value = 1; }

					ROM_bank &= 0xE0;
					ROM_bank |= value;
				}
				else if (addr < 0x6000)
				{
					if (sel_mode)
					{
						RAM_bank = value & 0x3;
					}
					else
					{
						ROM_bank &= 0x1F;
						ROM_bank |= ((value & 3) << 5);
					}
				}
				else
				{
					sel_mode = (value & 1) > 0;

					if (sel_mode)
					{
						ROM_bank &= 0x1F;
					}
					else
					{
						RAM_bank = 0;
					}
				}
			}
			else
			{
				if (Core.cart_RAM != null)
				{
					if (RAM_enable)
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
			ser.Sync("RAM_Bank", ref RAM_bank);
			ser.Sync("RAM_enable", ref RAM_enable);
			ser.Sync("sel_mode", ref sel_mode);
		}
	}
}
