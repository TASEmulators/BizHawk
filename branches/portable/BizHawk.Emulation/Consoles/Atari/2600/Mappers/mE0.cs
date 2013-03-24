using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	E0 (Parker Bros)
	-----

	Parker Brothers used this, and it was used on one other game (Tooth Protectors).  It
	uses 8K of ROM and can map 1K sections of it.

	This mapper has 4 1K banks of ROM in the address space.  The address space is broken up
	into the following locations:

	1000-13FF : To select a 1K ROM bank here, access 1FE0-1FE7 (1FE0 = select first 1K, etc)
	1400-17FF : To select a 1K ROM bank, access 1FE8-1FEF
	1800-1BFF : To select a 1K ROM bank, access 1FF0-1FF7
	1C00-1FFF : This is fixed to the last 1K ROM bank of the 8K

	Like F8, F6, etc. accessing one of the locations indicated will perform the switch.

	Example Games:
		Frogger II - Threedeep! (1983) (Parker Bros)
	*/

	class mE0 : MapperBase 
	{
		int toggle1 = 0;
		int toggle2 = 0;
		int toggle3 = 0;

		private byte ReadMem(ushort addr, bool peek)
		{
			if (!peek)
			{
				Address(addr);
			}

			if (addr < 0x1000) return base.ReadMemory(addr);
			else if (addr < 0x1400) return core.rom[toggle1 * 1024 + (addr & 0x3FF)];
			else if (addr < 0x1800) return core.rom[toggle2 * 1024 + (addr & 0x3FF)];
			else if (addr < 0x1C00) return core.rom[toggle3 * 1024 + (addr & 0x3FF)];
			else
				return core.rom[7 * 1024 + (addr & 0x3FF)]; //7 because final bank is always set to last
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
			ser.Sync("toggle1", ref toggle1);
			ser.Sync("toggle2", ref toggle2);
			ser.Sync("toggle3", ref toggle3);
		}

		void Address(ushort addr)
		{
			switch (addr)
			{
				case 0x1FE0:
					toggle1 = 0;
					break;
				case 0x1FE1:
					toggle1 = 1;
					break;
				case 0x1FE2:
					toggle1 = 2;
					break;
				case 0x1FE3:
					toggle1 = 3;
					break;
				case 0x1FE4:
					toggle1 = 4;
					break;
				case 0x1FE5:
					toggle1 = 5;
					break;
				case 0x1FE6:
					toggle1 = 6;
					break;
				case 0x1FE7:
					toggle1 = 7;
					break;

				case 0x1FE8:
					toggle2 = 0;
					break;
				case 0x1FE9:
					toggle2 = 1;
					break;
				case 0x1FEA:
					toggle2 = 2;
					break;
				case 0x1FEB:
					toggle2 = 3;
					break;
				case 0x1FEC:
					toggle2 = 4;
					break;
				case 0x1FED:
					toggle2 = 5;
					break;
				case 0x1FEE:
					toggle2 = 6;
					break;
				case 0x1FEF:
					toggle2 = 7;
					break;

				case 0x1FF0:
					toggle3 = 0;
					break;
				case 0x1FF1:
					toggle3 = 1;
					break;
				case 0x1FF2:
					toggle3 = 2;
					break;
				case 0x1FF3:
					toggle3 = 3;
					break;
				case 0x1FF4:
					toggle3 = 4;
					break;
				case 0x1FF5:
					toggle3 = 5;
					break;
				case 0x1FF6:
					toggle3 = 6;
					break;
				case 0x1FF7:
					toggle3 = 7;
					break;
			}
		}
	}
}
