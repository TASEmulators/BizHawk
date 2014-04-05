using System;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
		Mapper used for multi-cart mappers
	*/
	internal class Multicart : MapperBase
	{
		public Multicart()
		{
			throw new NotImplementedException();
		}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			return Core.Rom[addr & 0xFFF];
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMemory(addr);
		}
	}
}
