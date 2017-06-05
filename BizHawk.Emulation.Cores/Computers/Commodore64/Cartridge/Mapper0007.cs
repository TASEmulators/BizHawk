using System.Collections.Generic;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	internal sealed class Mapper0007 : CartridgeDevice
	{
		private readonly int[,] _banks = new int[16, 0x2000]; 

		private int _bankNumber;
		private bool _disabled;

		// Fun Play mapper
		// bank switching is done from DE00
		public Mapper0007(IList<int[]> newData, bool game, bool exrom)
		{
			pinGame = game;
			pinExRom = exrom;

			_disabled = false;

			// load data into the banks from the list
			for (var j = 0; j < 16; j++)
			{
				for (var i = 0; i < 0x2000; i++)
				{
					_banks[j, i] = newData[j][i];
				}
			}

			_bankNumber = 0;
		}

		protected override void SyncStateInternal(Serializer ser)
		{
			ser.Sync("BankNumber", ref _bankNumber);
			ser.Sync("Disabled", ref _disabled);
		}

		public override int Peek8000(int addr)
		{
			if (!_disabled)
			{
				return _banks[_bankNumber, addr];
			}

			return base.Read8000(addr);
		}

		public override void PokeDE00(int addr, int val)
		{
			if (addr == 0)
			{
				byte tempBank = (byte)((val & 0x1) << 3);
				tempBank |= (byte)((val & 0x38) >> 3);
				_bankNumber = tempBank;
				if (val == 0x86)
				{
					_disabled = true;
				}
			}
		}

		public override int Read8000(int addr)
		{
			if (!_disabled)
			{
				return _banks[_bankNumber, addr];
			}

			return base.Read8000(addr);
		}

		public override void WriteDE00(int addr, int val)
		{
			if (addr == 0)
			{
				byte tempBank = (byte)((val & 0x1) << 3);
				tempBank |= (byte)((val & 0x38) >> 3);
				_bankNumber = tempBank;
				if (val == 0x86)
				{
					_disabled = true;
				}
			}
		}
	}
}
