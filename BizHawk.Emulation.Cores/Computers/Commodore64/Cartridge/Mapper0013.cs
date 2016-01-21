using System;
using System.Collections.Generic;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	// Mapper for a few Domark and HES Australia games.
	// It seems a lot of people dumping these have remapped
	// them to the Ocean mapper (0005) but this is still here
	// for compatibility.
	//
	// Bank select is DE00, bit 7 enabled means to disable
	// ROM in 8000-9FFF.

	public sealed class Mapper0013 : Cart
	{
		private readonly byte[][] banks = new byte[0][]; //8000
		private readonly int bankMask;
		private int bankNumber;
		private byte[] currentBank;
		private byte[] dummyBank;
		private bool romEnable;

		public Mapper0013(IList<int> newAddresses, IList<int> newBanks, IList<byte[]> newData)
		{
			var count = newAddresses.Count;

			pinGame = true;
			pinExRom = false;
			romEnable = true;

			// build dummy bank
			dummyBank = new byte[0x2000];
			for (var i = 0; i < 0x2000; i++)
				dummyBank[i] = 0xFF; // todo: determine if this is correct

			switch (count)
			{
			    case 16:
			        bankMask = 0x0F;
			        banks = new byte[16][];
			        break;
			    case 8:
			        bankMask = 0x07;
			        banks = new byte[8][];
			        break;
			    case 4:
			        bankMask = 0x03;
			        banks = new byte[4][];
			        break;
			    default:
			        throw new Exception("This looks like a Domark/HES cartridge but cannot be loaded...");
			}

			// for safety, initialize all banks to dummy
			for (var i = 0; i < banks.Length; i++)
				banks[i] = dummyBank;

			// now load in the banks
			for (var i = 0; i < count; i++)
			{
				if (newAddresses[i] == 0x8000)
				{
					banks[newBanks[i] & bankMask] = newData[i];
				}
			}

			BankSet(0);
		}

		private void BankSet(int index)
		{
			bankNumber = index & bankMask;
			romEnable = ((index & 0x80) == 0);
			UpdateState();
		}

		public override int Peek8000(int addr)
		{
			return currentBank[addr];
		}

		public override void PokeDE00(int addr, int val)
		{
			if (addr == 0x00)
				BankSet(val);
		}

		public override int Read8000(int addr)
		{
			return currentBank[addr];
		}

		private void UpdateState()
		{
			currentBank = banks[bankNumber];
			if (romEnable)
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
				BankSet(val);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			if (ser.IsReader)
				BankSet(bankNumber | (romEnable ? 0x00 : 0x80));
		}
	}
}
