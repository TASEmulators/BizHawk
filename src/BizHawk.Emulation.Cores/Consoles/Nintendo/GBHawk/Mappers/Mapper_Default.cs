﻿using BizHawk.Emulation.Cores.Components.LR35902;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// Default mapper with no bank switching
	public sealed partial class MapperDefault : MapperBase
	{
		public override void Reset()
		{
			// nothing to initialize
		}

		public override byte ReadMemoryLow(ushort addr)
		{
			return Core._rom[addr];
		}

		public override byte ReadMemoryHigh(ushort addr)
		{
			if (Core.cart_RAM != null)
			{
				return Core.cart_RAM[addr - 0xA000];
			}
			else
			{
				return Core.cpu.TotalExecutedCycles > (Core.bus_access_time + 8)
					? (byte) 0xFF
					: Core.bus_value;
			}
		}

		public override void MapCDL(ushort addr, LR35902.eCDLogMemFlags flags)
		{
			if (addr < 0x8000)
			{
				SetCDLROM(flags, addr);
			}
			else
			{
				if (Core.cart_RAM != null)
				{
					SetCDLRAM(flags, addr - 0xA000);
				}
				else
				{
					return;
				}
			}
		}

		public override byte PeekMemoryLow(ushort addr)
		{
			return ReadMemoryLow(addr);
		}

		public override byte PeekMemoryHigh(ushort addr)
		{
			return ReadMemoryHigh(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x8000)
			{
				// no mapping hardware available
			}
			else
			{
				if (Core.cart_RAM != null)
				{
					Core.cart_RAM[addr - 0xA000] = value;
				}
			}
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}
	}
}
