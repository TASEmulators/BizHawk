using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	internal sealed class Mapper0005 : CartridgeDevice
	{
		private readonly int[][] _banksA; // 8000

		private readonly int[][] _banksB = new int[0][]; // A000

		private int _bankMask;

		private int _bankNumber;

		private int[] _currentBankA;

		private int[] _currentBankB;

		private readonly int[] _dummyBank;

		public Mapper0005(IList<int> newAddresses, IList<int> newBanks, IList<int[]> newData)
		{
			var count = newAddresses.Count;

			// build dummy bank
			_dummyBank = new int[0x2000];
			for (var i = 0; i < 0x2000; i++)
			{
				_dummyBank[i] = 0xFF; // todo: determine if this is correct
			}

			switch (count)
			{
				case 64:
					pinGame = true;
					pinExRom = false;
					_bankMask = 0x3F;
					_banksA = new int[64][];
					break;
				case 32:
					// this specific config is a weird exception
					pinGame = false;
					pinExRom = false;
					_bankMask = 0x0F;
					_banksA = new int[16][];
					_banksB = new int[16][];
					break;
				case 16:
					pinGame = true;
					pinExRom = false;
					_bankMask = 0x0F;
					_banksA = new int[16][];
					break;
				case 8:
					pinGame = true;
					pinExRom = false;
					_bankMask = 0x07;
					_banksA = new int[8][];
					break;
				case 4:
					pinGame = true;
					pinExRom = false;
					_bankMask = 0x03;
					_banksA = new int[4][];
					break;
				case 2:
					pinGame = true;
					pinExRom = false;
					_bankMask = 0x01;
					_banksA = new int[2][];
					break;
				case 1:
					pinGame = true;
					pinExRom = false;
					_bankMask = 0x00;
					_banksA = new int[1][];
					break;
				default:
					throw new Exception("This looks like an Ocean cartridge but cannot be loaded...");
			}

			// for safety, initialize all banks to dummy
			for (var i = 0; i < _banksA.Length; i++)
			{
				_banksA[i] = _dummyBank;
			}

			for (var i = 0; i < _banksB.Length; i++)
			{
				_banksB[i] = _dummyBank;
			}

			// now load in the banks
			for (var i = 0; i < count; i++)
			{
				switch (newAddresses[i])
				{
					case 0x8000:
						_banksA[newBanks[i] & _bankMask] = newData[i];
						break;
					case 0xA000:
					case 0xE000:
						_banksB[newBanks[i] & _bankMask] = newData[i];
						break;
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

		private void BankSet(int index)
		{
			_bankNumber = index & _bankMask;
			_currentBankA = !pinExRom ? _banksA[_bankNumber] : _dummyBank;
			_currentBankB = !pinGame ? _banksB[_bankNumber] : _dummyBank;
		}

		public override int Peek8000(int addr)
		{
			return _currentBankA[addr];
		}

		public override int PeekA000(int addr)
		{
			return _currentBankB[addr];
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
			return _currentBankA[addr];
		}

		public override int ReadA000(int addr)
		{
			return _currentBankB[addr];
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
