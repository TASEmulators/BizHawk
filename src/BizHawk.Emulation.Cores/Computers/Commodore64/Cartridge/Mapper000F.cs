using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	// This is a mapper used commonly by System 3. It is
	// also utilized by the short-lived C64 Game System.

	// Bank select is DExx. You select them by writing to the
	// register DE00+BankNr. For example, bank 01 is a write
	// to DE01.
	internal class Mapper000F : CartridgeDevice
	{
		private readonly int[][] _banks; // 8000

		private int _bankMask;
		private int _bankNumber;

		private int[] _currentBank;

		public Mapper000F(IList<int> newAddresses, IList<int> newBanks, IList<int[]> newData)
		{
			var count = newAddresses.Count;

			pinGame = true;
			pinExRom = false;

			// build dummy bank
			var dummyBank = new int[0x2000];
			for (var i = 0; i < 0x2000; i++)
			{
				dummyBank[i] = 0xFF; // todo: determine if this is correct
			}

			switch (count)
			{
				case 64:
					_bankMask = 0x3F;
					_banks = new int[64][];
					break;
				case 32:
					_bankMask = 0x1F;
					_banks = new int[32][];
					break;
				case 16:
					_bankMask = 0x0F;
					_banks = new int[16][];
					break;
				case 8:
					_bankMask = 0x07;
					_banks = new int[8][];
					break;
				case 4:
					_bankMask = 0x03;
					_banks = new int[4][];
					break;
				case 2:
					_bankMask = 0x01;
					_banks = new int[2][];
					break;
				case 1:
					_bankMask = 0x00;
					_banks = new int[1][];
					break;
				default:
					throw new Exception("This looks like a System 3/C64GS cartridge but cannot be loaded...");
			}

			// for safety, initialize all banks to dummy
			for (var i = 0; i < _banks.Length; i++)
			{
				_banks[i] = dummyBank;
			}

			// now load in the banks
			for (var i = 0; i < count; i++)
			{
				if (newAddresses[i] == 0x8000)
				{
					_banks[newBanks[i] & _bankMask] = newData[i];
				}
			}

			BankSet(0);
		}

		protected override void SyncStateInternal(Serializer ser)
		{
			ser.Sync("BankMask", ref _bankMask);
			ser.Sync("BankNumber", ref _bankNumber);

			if (ser.IsReader)
			{
				BankSet(_bankNumber);
			}
		}

		protected void BankSet(int index)
		{
			_bankNumber = index & _bankMask;
			UpdateState();
		}

		public override int Peek8000(int addr)
		{
			return _currentBank[addr];
		}

		public override void PokeDE00(int addr, int val)
		{
			BankSet(addr);
		}

		public override int Read8000(int addr)
		{
			return _currentBank[addr];
		}

		private void UpdateState()
		{
			_currentBank = _banks[_bankNumber];
		}

		public override int ReadDE00(int addr)
		{
			BankSet(0);

			return 0;
		}

		public override void WriteDE00(int addr, int val)
		{
			BankSet(addr);
		}
	}
}
