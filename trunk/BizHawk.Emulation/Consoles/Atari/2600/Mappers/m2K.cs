using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.CPUs.M6502;
using BizHawk.Emulation.Consoles.Atari;

namespace BizHawk
{
	partial class Atari2600
	{
		class m2K : MapperBase
		{
			public override byte ReadMemory(ushort addr)
			{
				if (addr < 0x1000) return base.ReadMemory(addr);
				return core.rom[addr & 0x7FF];
			}

			public override byte PeekMemory(ushort addr)
			{
				return ReadMemory(addr);
			}
		}
	}
}