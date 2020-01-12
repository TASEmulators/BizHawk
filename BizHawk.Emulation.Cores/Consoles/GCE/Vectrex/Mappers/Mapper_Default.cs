using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	// Default mapper with no bank switching
	// make sure peekmemory and poke memory don't effect the rest of the system!
	public class MapperDefault : MapperBase
	{
		public override void Initialize()
		{
			// nothing to initialize
		}

		public override byte ReadMemory(ushort addr)
		{
			return Core._rom[addr];
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMemory(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
		{

		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMemory(addr, value);
		}
	}
}
