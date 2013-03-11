using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Atari._2600
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

	class mX07 : MapperBase
	{
		int rombank_2k = 0;

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
			else
			{
				return core.rom[(rombank_2k << 12) + (addr & 0xFFF)];
			}
		}

		public override byte ReadMemory(ushort addr)
		{
			return ReadMem(addr, false);
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMem(addr, true);
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			Address(addr);
			if (addr < 0x1000) base.WriteMemory(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("rombank_2k", ref rombank_2k);
		}

		void Address(ushort addr)
		{
			if ((addr & 0x180F) == 0x080D)
			{
				bank((addr & 0xF0) >> 4);
			}
			else if ((addr & 0x1880) == 0)
			{
				if ((rombank_2k & 0xE) == 0xE)
				{
					bank(((addr & 0x40) >> 6) | (rombank_2k & 0xE));
				}
			}
		}

		private void bank(int bank)
		{
			rombank_2k = (bank & 0x0F);
		}
	}
}
