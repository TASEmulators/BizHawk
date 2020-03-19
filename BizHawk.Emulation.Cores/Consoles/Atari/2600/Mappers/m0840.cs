using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	This is another 8K bankswitching method with two 4K banks.  The rationale is that it's
	cheap and easy to implement with only a single 74HC153 or 253 dual 4:1 multiplexer.

	This multiplexer can act as a 1 bit latch AND the inverter for A12.

	To bankswitch, the following mask it used:

	A13           A0
	----------------
	0 1xxx xBxx xxxx

	Each bit corresponds to one of the 13 address lines.  a 0 or 1 means that bit must be
	0 or 1 to trigger the bankswitch. x is a bit that is not considered (it can be either
	0 or 1 and is thus a "don't care" bit).

	B is the bank we will select. So, accessing 0800 will select bank 0, and 0840
	will select bank 1.
	*/
	internal sealed class m0840 : MapperBase
	{
		private int _bank4K;

		public m0840(Atari2600 core) : base(core)
		{
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("bank_4k", ref _bank4K);
		}

		public override void HardReset()
		{
			_bank4K = 0;
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

			return Core.Rom[(_bank4K << 12) + (addr & 0xFFF)];
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
			_bank4K = (addr & 0x1840) switch
			{
				0x0800 => 0,
				0x0840 => 1,
				_ => _bank4K
			};
		}
	}
}
