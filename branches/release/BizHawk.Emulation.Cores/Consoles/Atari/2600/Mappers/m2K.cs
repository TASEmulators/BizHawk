namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	internal class m2K : MapperBase
	{
		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			return this.Core.Rom[addr & 0x7FF];
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMemory(addr);
		}
	}
}