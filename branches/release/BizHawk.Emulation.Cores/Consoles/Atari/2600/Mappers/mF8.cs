using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	F8 (Atari style 8K)
	-----

	This is the fairly standard way 8K of cartridge ROM was implemented.  There are two
	4K ROM banks, which get mapped into the 4K of cartridge space.  Accessing 1FF8 or
	1FF9 selects one of the two 4K banks.  When one of these two addresses are accessed,
	the banks switch spontaniously.

	ANY kind of access will trigger the switching- reading or writing.  Usually games use
	LDA or BIT on 1FF8/1FF9 to perform the switch.

	When the switch occurs, the entire 4K ROM bank switches, including the code that is
	reading the 1FF8/1FF9 location.  Usually, games put a small stub of code in BOTH banks
	so when the switch occurs, the code won't crash.
	*/

	internal class mF8 : MapperBase
	{
		private int _bank4K;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("bank_4k", ref _bank4K);
		}

		public override void HardReset()
		{
			_bank4K = 0;
			base.HardReset();
		}

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

			return Core.Rom[(_bank4K << 12) + (addr & 0xFFF)];
		}

		public override byte ReadMemory(ushort addr)
		{
			return ReadMem(addr, false);
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMem(addr, true);
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

		public override void WriteMemory(ushort addr, byte value)
		{
			WriteMem(addr, value, poke: false);
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMem(addr, value, poke: true);
		}

		private void Address(ushort addr)
		{
			if (addr == 0x1FF8)
			{
				_bank4K = 0;
			}
			else if (addr == 0x1FF9)
			{
				_bank4K = 1;
			}
		}
	}
}