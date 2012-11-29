using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Cartridges
{
	// This is a mapper used commonly by System 3. It is
	// also utilized by the short-lived C64 Game System.

	// Bank select is DExx. You select them by writing to the
	// register DE00+BankNr. For example, bank 01 is a write
	// to DE01.

	public class Mapper000F : Cartridge
	{
		private byte[][] banks = new byte[0][]; //8000
		private uint bankMask;
		private uint bankNumber;
		private byte[] currentBank;
		private byte[] dummyBank;

		public Mapper000F(List<uint> newAddresses, List<uint> newBanks, List<byte[]> newData)
		{
			uint count = (uint)newAddresses.Count;

			pinGame = true;
			pinExRom = false;

			// build dummy bank
			dummyBank = new byte[0x2000];
			for (uint i = 0; i < 0x2000; i++)
				dummyBank[i] = 0xFF; // todo: determine if this is correct

			if (count == 64) //512k
			{
				bankMask = 0x3F;
				banks = new byte[64][];
			}
			else if (count == 32) //256k
			{
				bankMask = 0x1F;
				banks = new byte[32][];
			}
			else if (count == 16) //128k
			{
				bankMask = 0x0F;
				banks = new byte[16][];
			}
			else if (count == 8) //64k
			{
				bankMask = 0x07;
				banks = new byte[8][];
			}
			else if (count == 4) //32k
			{
				bankMask = 0x03;
				banks = new byte[4][];
			}
			else if (count == 2) //16k
			{
				bankMask = 0x01;
				banks = new byte[2][];
			}
			else if (count == 1) //8k
			{
				bankMask = 0x00;
				banks = new byte[1][];
			}
			else
			{
				// we don't know what format this is...
				throw new Exception("This looks like a System 3/C64GS cartridge but cannot be loaded...");
			}

			// for safety, initialize all banks to dummy
			for (uint i = 0; i < banks.Length; i++)
				banks[i] = dummyBank;

			// now load in the banks
			for (int i = 0; i < count; i++)
			{
				if (newAddresses[i] == 0x8000)
				{
					banks[newBanks[i] & bankMask] = newData[i];
				}
			}

			BankSet(0);
		}

		private void BankSet(uint index)
		{
			bankNumber = index & bankMask;
			UpdateState();
		}

		public override byte Peek8000(int addr)
		{
			return currentBank[addr];
		}

		public override void PokeDE00(int addr, byte val)
		{
			BankSet((uint)addr);
		}

		public override byte Read8000(ushort addr)
		{
			return currentBank[addr];
		}

		private void UpdateState()
		{
			currentBank = banks[bankNumber];
		}

		public override void WriteDE00(ushort addr, byte val)
		{
			BankSet((uint)addr);
		}
	}
}
