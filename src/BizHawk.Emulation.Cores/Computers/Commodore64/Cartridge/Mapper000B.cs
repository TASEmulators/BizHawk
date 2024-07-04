using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	// Westermann Learning mapper.
	// Starts up with both banks enabled, any read to DFxx
	// turns off the high bank by bringing GAME high.
	// I suspect that the game loads by copying all hirom to
	// the RAM underneath (BASIC variable values probably)
	// and then disables once loaded.
	internal sealed class Mapper000B : CartridgeDevice
	{
		private readonly int[] _rom = new int[0x4000];

		public Mapper000B(IList<int> newAddresses, IList<int[]> newData)
		{
			validCartridge = false;

			for (var i = 0; i < 0x4000; i++)
			{
				_rom[i] = 0xFF;
			}

			if (newAddresses[0] != 0x8000)
			{
				return;
			}

			Array.Copy(newData[0], _rom, Math.Min(newData[0].Length, 0x4000));
			validCartridge = true;
		}

		protected override void SyncStateInternal(Serializer ser)
		{
			// Nothing to save
		}

		public override int Peek8000(int addr)
		{
			return _rom[addr];
		}

		public override int PeekA000(int addr)
		{
			return _rom[addr | 0x2000];
		}

		public override int Read8000(int addr)
		{
			return _rom[addr];
		}

		public override int ReadA000(int addr)
		{
			return _rom[addr | 0x2000];
		}

		public override int ReadDF00(int addr)
		{
			pinGame = true;
			return base.ReadDF00(addr);
		}
	}
}
