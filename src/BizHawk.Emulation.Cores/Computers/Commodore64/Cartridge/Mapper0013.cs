using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	// Mapper for a few Domark and HES Australia games.
	// It seems a lot of people dumping these have remapped
	// them to the Ocean mapper (0005) but this is still here
	// for compatibility.
	//
	// Bank select is DE00, bit 7 enabled means to disable
	// ROM in 8000-9FFF.
	internal sealed class Mapper0013 : CartridgeDevice
	{
		private readonly int[][] _banks; // 8000

		private int _bankMask;
		private int _bankNumber;

		private int[] _currentBank;

		private bool _romEnable;

		public Mapper0013(IList<int> newAddresses, IList<int> newBanks, IList<int[]> newData)
		{
			var count = newAddresses.Count;

			pinGame = true;
			pinExRom = false;
			_romEnable = true;

			// build dummy bank
			var dummyBank = new int[0x2000];
			for (var i = 0; i < 0x2000; i++)
			{
				dummyBank[i] = 0xFF; // todo: determine if this is correct
			}

			switch (count)
			{
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
				default:
					throw new Exception("This looks like a Domark/HES cartridge but cannot be loaded...");
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
			ser.Sync("ROMEnable", ref _romEnable);

			if (ser.IsReader)
			{
				BankSet(_bankNumber | (_romEnable ? 0x00 : 0x80));
			}
		}

		private void BankSet(int index)
		{
			_bankNumber = index & _bankMask;
			_romEnable = (index & 0x80) == 0;
			UpdateState();
		}

		public override int Peek8000(int addr)
		{
			return _currentBank[addr];
		}

		public override void PokeDE00(int addr, int val)
		{
			if (addr == 0x00)
			{
				BankSet(val);
			}
		}

		public override int Read8000(int addr)
		{
			return _currentBank[addr];
		}

		private void UpdateState()
		{
			_currentBank = _banks[_bankNumber];
			if (_romEnable)
			{
				pinExRom = false;
				pinGame = true;
			}
			else
			{
				pinExRom = true;
				pinGame = true;
			}
		}

		public override void WriteDE00(int addr, int val)
		{
			if (addr == 0x00)
			{
				BankSet(val);
			}
		}
	}
}
