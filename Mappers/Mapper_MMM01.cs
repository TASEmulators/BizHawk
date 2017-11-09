using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// Default mapper with no bank switching
	public class MapperMMM01 : MapperBase
	{
		public override void Initialize()
		{
			// nothing to initialize
		}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x8000)
			{
				return Core._rom[addr];
			}
			else
			{
				if (Core.cart_RAM != null)
				{
					return Core.cart_RAM[addr - 0xA000];
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
