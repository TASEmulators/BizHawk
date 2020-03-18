using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	F4 (Atari style 32K)
	-----

	Again, this works like F8 and F6 except now there's 8 4K banks.  Selection is performed
	by accessing 1FF4 through 1FFB.
	*/
	internal class mF4 :MapperBase 
	{
		private int _toggle;

		public mF4(Atari2600 core) : base(core)
		{
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("toggle", ref _toggle);
		}

		public override void HardReset()
		{
			_toggle = 0;
			base.HardReset();
		}

		public override byte ReadMemory(ushort addr) => ReadMem(addr, false);

		public override byte PeekMemory(ushort addr) => ReadMem(addr, true);

		public override void WriteMemory(ushort addr, byte value)
			=> WriteMem(addr, value, false);

		public override void PokeMemory(ushort addr, byte value)
			=> WriteMem(addr, value, true);

		private byte ReadMem(ushort addr, bool peek)
		{
			if (!peek)
			{
				Address(addr);
			}

			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			return Core.Rom[(_toggle << 12) + (addr & 0xFFF)];
		}

		private void WriteMem(ushort addr, byte value, bool poke)
		{
			if (!poke)
			{
				Address(addr);
			}

			if (addr < 0x1000)
			{
				base.WriteMemory(addr, value);
			}
		}

		private void Address(ushort addr)
		{
			_toggle = addr switch
			{
				0x1FF4 => 0,
				0x1FF5 => 1,
				0x1FF6 => 2,
				0x1FF7 => 3,
				0x1FF8 => 4,
				0x1FF9 => 5,
				0x1FFA => 6,
				0x1FFB => 7,
				_ => _toggle
			};
		}
	}
}
