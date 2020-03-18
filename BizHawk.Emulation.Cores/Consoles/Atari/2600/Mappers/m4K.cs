namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	internal class m4K : MapperBase
	{
		public m4K(Atari2600 core) : base(core)
		{
		}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			return Core.Rom[addr & 0xFFF];
		}

		public override byte PeekMemory(ushort addr) => ReadMemory(addr);
	}
}