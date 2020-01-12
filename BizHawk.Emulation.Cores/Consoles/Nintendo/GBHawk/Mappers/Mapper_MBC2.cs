using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;

using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// MBC2 with bank switching and RAM
	public class MapperMBC2 : MapperBase
	{
		public int ROM_bank;
		public int RAM_bank;
		public bool RAM_enable;
		public int ROM_mask;

		public override void Initialize()
		{
			ROM_bank = 1;
			RAM_bank = 0;
			RAM_enable = false;
			ROM_mask = Core._rom.Length / 0x4000 - 1;
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
			else if ((addr >= 0xA000) && (addr < 0xA200))
			{
				if (RAM_enable)
				{
					return Core.cart_RAM[addr - 0xA000];
				}
				return 0xFF;
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

		public override byte PeekMemory(ushort addr)
		{
			return ReadMemory(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
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
					if (ROM_bank==0) { ROM_bank = 1; }
				}
			}
			else if ((addr >= 0xA000) && (addr < 0xA200))
			{
				if (RAM_enable)
				{
					Core.cart_RAM[addr - 0xA000] = (byte)(value & 0xF);
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
			ser.Sync(nameof(RAM_enable), ref RAM_enable);
		}
	}
}