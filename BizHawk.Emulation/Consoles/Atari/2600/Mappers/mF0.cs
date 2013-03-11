using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Atari._2600
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

	class mF0 : MapperBase 
	{
		int bank = 0;

		private byte ReadMem(ushort addr, bool peek)
		{
			if (!peek)
			{
				if (addr == 0x1FF0)
					Increment();
			}

			if (addr < 0x1000) return base.ReadMemory(addr);
			else return core.rom[bank * 4096 + (addr & 0xFFF)];
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
			if (addr < 0x1000) base.WriteMemory(addr, value);
			else if (addr == 0x1ff0)
				Increment();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("bank", ref bank);
		}

		void Increment()
		{
			bank++;
			bank &= 0x0F;
		}
	}
}
