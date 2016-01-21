using System;
using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	// This is a mapper used commonly by System 3. It is
	// also utilized by the short-lived C64 Game System.

	// Bank select is DExx. You select them by writing to the
	// register DE00+BankNr. For example, bank 01 is a write
	// to DE01.

	public class Mapper000F : Cart
	{
		private readonly byte[][] banks = new byte[0][]; //8000
		private readonly int bankMask;
		private int bankNumber;
		private byte[] currentBank;

	    public Mapper000F(IList<int> newAddresses, IList<int> newBanks, IList<byte[]> newData)
		{
		    var count = newAddresses.Count;

			pinGame = true;
			pinExRom = false;

			// build dummy bank
			var dummyBank = new byte[0x2000];
			for (var i = 0; i < 0x2000; i++)
				dummyBank[i] = 0xFF; // todo: determine if this is correct

			switch (count)
			{
			    case 64:
			        bankMask = 0x3F;
			        banks = new byte[64][];
			        break;
			    case 32:
			        bankMask = 0x1F;
			        banks = new byte[32][];
			        break;
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
			    case 2:
			        bankMask = 0x01;
			        banks = new byte[2][];
			        break;
			    case 1:
			        bankMask = 0x00;
			        banks = new byte[1][];
			        break;
			    default:
			        throw new Exception("This looks like a System 3/C64GS cartridge but cannot be loaded...");
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

		protected void BankSet(int index)
		{
			bankNumber = index & bankMask;
			UpdateState();
		}

		public override int Peek8000(int addr)
		{
			return currentBank[addr];
		}

		public override void PokeDE00(int addr, int val)
		{
			BankSet(addr);
		}

		public override int Read8000(int addr)
		{
			return currentBank[addr];
		}

		private void UpdateState()
		{
			currentBank = banks[bankNumber];
		}

		public override void WriteDE00(int addr, int val)
		{
			BankSet(addr);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			if (ser.IsReader)
				BankSet(bankNumber);
		}
	}
}
