using System;

namespace BizHawk.Emulation.Consoles.Gameboy
{
	partial class Gameboy
	{
		public class MemoryMapper
		{
			private readonly Gameboy gb;
			public MemoryMapper(Gameboy gb)
			{
				this.gb = gb;
			}
		}
	}
}