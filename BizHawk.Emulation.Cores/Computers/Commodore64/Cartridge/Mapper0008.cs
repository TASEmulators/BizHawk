using System.Collections.Generic;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	internal sealed class Mapper0008 : CartridgeDevice
	{
		private readonly int[,] _banks = new int[4, 0x4000]; 

		private int _bankMask;
		private int _bankNumber;
		private bool _disabled;
		private int _latchedval;

		// SuperGame mapper
		// bank switching is done from DF00
		public Mapper0008(IList<int[]> newData)
		{
			pinGame = false;
			pinExRom = false;

			_bankMask = 0x03;
			_disabled = false;
			_latchedval = 0;

			// load data into the banks from the list
			for (var j = 0; j < 4; j++)
			{
				for (var i = 0; i < 0x4000; i++)
				{
					_banks[j, i] = newData[j][i];
				}
			}

			BankSet(0);
		}

		protected override void SyncStateInternal(Serializer ser)
		{
			ser.Sync("BankMask", ref _bankMask);
			ser.Sync("BankNumber", ref _bankNumber);
			ser.Sync("Disabled", ref _disabled);
			ser.Sync("Latchedvalue", ref _latchedval);
		}

		private void BankSet(int index)
		{
			if (!_disabled)
			{
				_bankNumber = index & _bankMask;
				pinExRom = (index & 0x4) > 0;
				pinGame = (index & 0x4) > 0;
				_disabled = (index & 0x8) > 0;
				_latchedval = index;
			}
		}

		public override int Peek8000(int addr)
		{
			return _banks[_bankNumber, addr];
		}

		public override int PeekA000(int addr)
		{
			return _banks[_bankNumber, addr + 0x2000];
		}

		public override void PokeDF00(int addr, int val)
		{
			if (addr == 0)
			{
				BankSet(val);
			}
		}

		public override int Read8000(int addr)
		{
				return _banks[_bankNumber, addr];
		}

		public override int ReadA000(int addr)
		{
				return _banks[_bankNumber, addr + 0x2000];
		}

		public override void WriteDF00(int addr, int val)
		{
			if (addr == 0)
			{
				BankSet(val);
			}
		}

		public override int ReadDF00(int addr)
		{
			return _latchedval;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			if (ser.IsReader)
			{
				BankSet(_bankNumber);
			}
		}
	}
}
