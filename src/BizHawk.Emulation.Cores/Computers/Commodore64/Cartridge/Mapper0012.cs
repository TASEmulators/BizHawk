using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	internal sealed class Mapper0012 : CartridgeDevice
	{
		private readonly int[] _bankMain;

		private readonly int[][] _bankHigh;

		private int[] _bankHighSelected;

		private int _bankIndex;

		// Zaxxon and Super Zaxxon cartridges
		// - read to 8xxx selects bank 0 in A000-BFFF
		// - read to 9xxx selects bank 1 in A000-BFFF
		public Mapper0012(IList<int> newAddresses, IList<int> newBanks, IList<int[]> newData)
		{
			_bankMain = new int[0x2000];
			_bankHigh = new int[2][];
			var dummyBank = new int[0x2000];

			// create dummy bank just in case
			for (var i = 0; i < 0x2000; i++)
			{
				dummyBank[i] = 0xFF;
			}

			_bankHigh[0] = dummyBank;
			_bankHigh[1] = dummyBank;

			// load in the banks
			for (var i = 0; i < newAddresses.Count; i++)
			{
				if (newAddresses[i] == 0x8000)
				{
					Array.Copy(newData[i], _bankMain, 0x1000);
				}
				else if ((newAddresses[i] == 0xA000 || newAddresses[i] == 0xE000) && newBanks[i] < 2)
				{
					_bankHigh[newBanks[i]] = newData[i];
				}
			}

			// mirror the main rom from 8000 to 9000
			Array.Copy(_bankMain, 0x0000, _bankMain, 0x1000, 0x1000);

			// set both pins low for 16k rom config
			pinExRom = false;
			pinGame = false;
		}

		protected override void SyncStateInternal(Serializer ser)
		{
			ser.Sync("BankIndex", ref _bankIndex);
			if (ser.IsReader)
			{
				_bankHighSelected = _bankHigh[_bankIndex];
			}
		}

		public override int Peek8000(int addr)
		{
			return _bankMain[addr];
		}

		public override int PeekA000(int addr)
		{
			return _bankHighSelected[addr];
		}

		public override int Read8000(int addr)
		{
			_bankIndex = (addr & 0x1000) >> 12;
			_bankHighSelected = _bankHigh[_bankIndex];
			return _bankMain[addr];
		}

		public override int ReadA000(int addr)
		{
			return _bankHighSelected[addr];
		}
	}
}
