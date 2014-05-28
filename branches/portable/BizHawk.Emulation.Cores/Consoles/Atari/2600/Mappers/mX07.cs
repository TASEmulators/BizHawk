using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	X07 (Atariage)
	-----

	Apparently, this was only used on one cart: Stella's Stocking.
	Similar to EF, there are 16 4K banks, for a total of up to 64K of ROM.  

	The addresses to select banks is below the ROM area, however.

	The following TWO masks are used:

	A13           A0
	----------------
	0 1xxx nnnn 1101

	This means the address 80B selects bank 0, 81B selects bank 1, etc.

	In addition to this, there is another way:

	A13           A0
	----------------
	0 0xxx 0nxx xxxx

	This is somewhat special purpose:  Accessing here does nothing, unless one of the
	last two banks are selected (banks 14 or 15).  In that case, the new bank is:

	111n   i.e. accessing 0000 will select bank 14 (Eh, 1110b) while accessing 0040
	will select bank 15 (Fh, 1111b).  This allows for bankswitching by accessing 
	TIA registers at 00-3F or 40-7F without incurring any overhead.
	*/

	internal class mX07 : MapperBase
	{
		private int _rombank2K;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("rombank_2k", ref _rombank2K);
		}

		public override void HardReset()
		{
			_rombank2K = 0;
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
			
			return Core.Rom[(_rombank2K << 12) + (addr & 0xFFF)];
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
			if ((addr & 0x180F) == 0x080D)
			{
				Bank((addr & 0xF0) >> 4);
			}
			else if ((addr & 0x1880) == 0)
			{
				if ((_rombank2K & 0xE) == 0xE)
				{
					Bank(((addr & 0x40) >> 6) | (_rombank2K & 0xE));
				}
			}
		}

		private void Bank(int bank)
		{
			_rombank2K = bank & 0x0F;
		}
	}
}
