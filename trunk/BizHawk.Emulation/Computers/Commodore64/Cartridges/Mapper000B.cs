using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Cartridges
{
	// Westermann Learning mapper.
	// Starts up with both banks enabled, any read to DFxx
	// turns off the high bank by bringing GAME high.
	// I suspect that the game loads by copying all hirom to
	// the RAM underneath (BASIC variable values probably)
	// and then disables once loaded.

	public class Mapper000B : Cartridge
	{
		private byte[] rom = new byte[0x4000];

		public Mapper000B(List<uint> newAddresses, List<uint> newBanks, List<byte[]> newData)
		{
			validCartridge = false;

			for (uint i = 0; i < 0x4000; i++)
				rom[i] = 0xFF;

			if (newAddresses[0] == 0x8000)
			{
				Array.Copy(newData[0], rom, Math.Min(newData[0].Length, 0x4000));
				validCartridge = true;
			}
		}

		public override byte Peek8000(int addr)
		{
			return rom[addr];
		}

		public override byte PeekA000(int addr)
		{
			return rom[addr | 0x2000];
		}

		public override byte Read8000(ushort addr)
		{
			return rom[addr];
		}

		public override byte ReadA000(ushort addr)
		{
			return rom[addr | 0x2000];
		}

		public override byte ReadDF00(ushort addr)
		{
			pinGame = true;
			return base.ReadDF00(addr);
		}
	}
}
