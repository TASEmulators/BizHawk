using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	// Epyx Fastload. Uppermost page is always visible at DFxx.
	// They use a capacitor that is discharged by accesses to DExx
	// to pull down EXROM. Also, accesses to LOROM while it is active
	// discharge the capacitor.
	// Thanks to VICE team for the info: http://vice-emu.sourceforge.net/vice_15.html
	internal class Mapper000A : CartridgeDevice
	{
		// This constant differs depending on whose research you reference. TODO: Verify.
		private const int RESET_CAPACITOR_CYCLES = 512;

		private readonly int[] _rom;
		private int _capacitorCycles;

		public Mapper000A(IList<int[]> newData)
		{
			_rom = new int[0x2000];
			Array.Copy(newData.First(), _rom, 0x2000);
			pinGame = true;
		}

		protected override void SyncStateInternal(Serializer ser)
		{
			ser.Sync("CapacitorCycles", ref _capacitorCycles);
		}

		public override void ExecutePhase()
		{
			pinExRom = !(_capacitorCycles > 0);
			if (!pinExRom)
			{
				_capacitorCycles--;
			}
		}

		public override void HardReset()
		{
			_capacitorCycles = RESET_CAPACITOR_CYCLES;
			base.HardReset();
		}

		public override int Peek8000(int addr)
		{
			return _rom[addr & 0x1FFF];
		}

		public override int PeekDE00(int addr)
		{
			return 0x00;
		}

		public override int PeekDF00(int addr)
		{
			return _rom[(addr & 0xFF) | 0x1F00];
		}

		public override int Read8000(int addr)
		{
			_capacitorCycles = RESET_CAPACITOR_CYCLES;
			return _rom[addr & 0x1FFF];
		}

		public override int ReadDE00(int addr)
		{
			_capacitorCycles = RESET_CAPACITOR_CYCLES;
			return 0x00;
		}

		public override int ReadDF00(int addr)
		{
			return _rom[(addr & 0xFF) | 0x1F00];
		}
	}
}
