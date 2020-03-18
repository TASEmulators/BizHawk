using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	EF (no name?)
	-----

	This is a fairly simple method that allows for up to 64K of ROM, using 16 4K banks.
	It works similar to F8, F6, etc.  Only the addresses to perform the switch is
	1FE0-1FEF.  Accessing one of these will select the desired bank. 1FE0 = bank 0,
	1FE1 = bank 1, etc.
	*/
	internal class mEF : MapperBase 
	{
		private int _toggle;

		public mEF(Atari2600 core) : base(core)
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
				0x1FE0 => 0,
				0x1FE1 => 1,
				0x1FE2 => 2,
				0x1FE3 => 3,
				0x1FE4 => 4,
				0x1FE5 => 5,
				0x1FE6 => 6,
				0x1FE7 => 7,
				0x1FE8 => 8,
				0x1FE9 => 9,
				0x1FEA => 10,
				0x1FEB => 11,
				0x1FEC => 12,
				0x1FED => 13,
				0x1FEE => 14,
				0x1FEF => 15,
				_ => _toggle
			};
		}
	}
}
