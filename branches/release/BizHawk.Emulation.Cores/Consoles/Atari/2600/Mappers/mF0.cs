using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	F0 (Megaboy)
	-----

	This was used on one game, "megaboy".. Some kind of educational cartridge.  It supports
	64K of ROM making it the biggest single production game made during the original run
	of the 2600.

	Bankswitching is very simple. There's 16 4K banks, and accessing 1FF0 causes the bank 
	number to increment.

	This means that you must keep accessing 1FF0 until the bank you want is selected.  Each
	bank is numbered by means of one of the ROM locations, and the code simply keeps accessing
	1FF0 until the bank it is looking for comes up.
	*/

	internal class mF0 : MapperBase 
	{
		private int _bank;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("bank", ref _bank);
		}

		public override void HardReset()
		{
			_bank = 0;
			base.HardReset();
		}

		private byte ReadMem(ushort addr, bool peek)
		{
			if (!peek)
			{
				if (addr == 0x1FF0)
				{
					Increment();
				}
			}

			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			return Core.Rom[(_bank << 12) + (addr & 0xFFF)];
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
			if (addr < 0x1000)
			{
				base.WriteMemory(addr, value);
			}
			else if (addr == 0x1ff0 && !poke)
			{
				Increment();
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

		private void Increment()
		{
			_bank++;
			_bank &= 0x0F;
		}
	}
}
