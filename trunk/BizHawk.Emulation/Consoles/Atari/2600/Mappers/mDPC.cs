using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Atari._2600
{
	partial class Atari2600
	{
		class mDPC : MapperBase
		{
			public override byte ReadMemory(ushort addr)
			{
				if (addr < 0x1000) return base.ReadMemory(addr);
				return core.rom[addr & 0x7FF];
			}
		}
	}
}
