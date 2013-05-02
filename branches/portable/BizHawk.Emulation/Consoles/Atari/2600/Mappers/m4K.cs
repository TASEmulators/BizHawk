namespace BizHawk
{
	partial class Atari2600
	{
		class m4K : MapperBase
		{
			public override byte ReadMemory(ushort addr)
			{
				if (addr < 0x1000) return base.ReadMemory(addr);
				return core.rom[addr & 0xFFF];
			}

			public override byte PeekMemory(ushort addr)
			{
				return ReadMemory(addr);
			}
		}
	}
}