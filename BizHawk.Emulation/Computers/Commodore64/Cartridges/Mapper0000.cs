using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Cartridges
{
	public class Mapper0000 : Cartridge
	{
		private byte[] romA;
		private byte[] romB;

		// standard cartridge mapper (Commodore)

		public Mapper0000(byte[] data, bool exrom, bool game)
		{
			pinGame = game;
			pinExRom = exrom;

			romA = new byte[0x2000];
			romB = new byte[0x2000];
			validCartridge = true;

			// we can expect three different configurations:
			// bank of 4k, bank of 8k, or two banks of 8k

			if (data.Length == 0x1000)
			{
				Array.Copy(data, 0x0000, romA, 0x0000, 0x1000);
				Array.Copy(data, 0x0000, romA, 0x1000, 0x1000);
				for (int i = 0; i < 0x2000; i++)
					romB[i] = 0xFF;
			}
			else if (data.Length == 0x2000)
			{
				Array.Copy(data, 0x0000, romA, 0x0000, 0x2000);
				for (int i = 0; i < 0x2000; i++)
					romB[i] = 0xFF;
			}
			else if (data.Length == 0x4000)
			{
				Array.Copy(data, 0x0000, romA, 0x0000, 0x2000);
				Array.Copy(data, 0x2000, romB, 0x0000, 0x2000);
			}
			else
			{
				validCartridge = false;
			}

			HardReset();
		}

		public override byte Peek8000(int addr)
		{
			return romA[addr];
		}

		public override byte PeekA000(int addr)
		{
			return romB[addr];
		}

		public override byte Read8000(ushort addr)
		{
			return romA[addr];
		}

		public override byte ReadA000(ushort addr)
		{
			return romB[addr];
		}
	}
}
