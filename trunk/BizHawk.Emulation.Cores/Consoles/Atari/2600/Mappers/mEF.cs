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
			if (addr == 0x1FE0) _toggle = 0;
			if (addr == 0x1FE1) _toggle = 1;
			if (addr == 0x1FE2) _toggle = 2;
			if (addr == 0x1FE3) _toggle = 3;
			if (addr == 0x1FE4) _toggle = 4;
			if (addr == 0x1FE5) _toggle = 5;
			if (addr == 0x1FE6) _toggle = 6;
			if (addr == 0x1FE7) _toggle = 7;
			if (addr == 0x1FE8) _toggle = 8;
			if (addr == 0x1FE9) _toggle = 9;
			if (addr == 0x1FEA) _toggle = 10;
			if (addr == 0x1FEB) _toggle = 11;
			if (addr == 0x1FEC) _toggle = 12;
			if (addr == 0x1FED) _toggle = 13;
			if (addr == 0x1FEE) _toggle = 14;
			if (addr == 0x1FEF) _toggle = 15;
		}
	}
}
