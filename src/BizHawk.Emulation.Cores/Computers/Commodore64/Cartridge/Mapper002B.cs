using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	// Prophet 64 cartridge. Because we can.
	// 32 banks of 8KB.
	// DFxx = status register, xxABBBBB. A=enable cart, B=bank
	// Thanks to VICE team for the info: http://vice-emu.sourceforge.net/vice_15.html
	internal class Mapper002B : CartridgeDevice
	{
		private readonly int[] _rom;

		private int _romOffset;
		private bool _romEnabled;

		public Mapper002B(IList<int> newAddresses, IList<int> newBanks, IList<int[]> newData)
		{
			pinExRom = false;
			pinGame = true;
			_rom = new int[0x40000];
			Array.Copy(newData.First(), _rom, 0x2000);
			pinGame = true;
			for (var i = 0; i < newData.Count; i++)
			{
				if (newAddresses[i] == 0x8000)
				{
					Array.Copy(newData[i], 0, _rom, newBanks[i] * 0x2000, 0x2000);
				}
			}
		}

		protected override void SyncStateInternal(Serializer ser)
		{
			ser.Sync("RomOffset", ref _romOffset);
			ser.Sync("RomEnabled", ref _romEnabled);
		}

		public override void HardReset()
		{
			_romEnabled = true;
			_romOffset = 0;
		}

		public override int Peek8000(int addr)
		{
			return _romOffset | (addr & 0x1FFF);
		}

		public override int PeekDF00(int addr)
		{
			// For debugging only. The processor does not see this.
			return ((_romOffset >> 13) & 0x1F) | (_romEnabled ? 0x20 : 0x00);
		}

		public override void PokeDF00(int addr, int val)
		{
			_romOffset = (val & 0x1F) << 13;
			_romEnabled = (val & 0x20) != 0;
		}

		public override int Read8000(int addr)
		{
			return _romOffset | (addr & 0x1FFF);
		}

		public override int ReadDF00(int addr)
		{
			return 0x00;
		}

		public override void WriteDF00(int addr, int val)
		{
			_romOffset = (val & 0x1F) << 13;
			_romEnabled = (val & 0x20) != 0;
		}
	}
}
