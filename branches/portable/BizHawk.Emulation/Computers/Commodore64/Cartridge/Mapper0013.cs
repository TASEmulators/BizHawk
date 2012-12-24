using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Cartridge
{
	// Mapper for a few Domark and HES Australia games.
	// It seems a lot of people dumping these have remapped
	// them to the Ocean mapper (0005) but this is still here
	// for compatibility.
	//
	// Bank select is DE00, bit 7 enabled means to disable
	// ROM in 8000-9FFF.

	public class Mapper0013 : Cart
	{
		private byte[][] banks = new byte[0][]; //8000
		private uint bankMask;
		private uint bankNumber;
		private byte[] currentBank;
		private byte[] dummyBank;
		private bool romEnable;

		public Mapper0013(List<uint> newAddresses, List<uint> newBanks, List<byte[]> newData)
		{
			uint count = (uint)newAddresses.Count;

			pinGame = true;
			pinExRom = false;
			romEnable = true;

			// build dummy bank
			dummyBank = new byte[0x2000];
			for (uint i = 0; i < 0x2000; i++)
				dummyBank[i] = 0xFF; // todo: determine if this is correct

			if (count == 16) //128k
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
			else
			{
				// we don't know what format this is...
				throw new Exception("This looks like a Domark/HES cartridge but cannot be loaded...");
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
			romEnable = ((index & 0x80) == 0);
			UpdateState();
		}

		public override byte Peek8000(int addr)
		{
			return currentBank[addr];
		}

		public override void PokeDE00(int addr, byte val)
		{
			if (addr == 0x00)
				BankSet(val);
		}

		public override byte Read8000(ushort addr)
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

		public override void WriteDE00(ushort addr, byte val)
		{
			if (addr == 0x00)
				BankSet(val);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("bankMask", ref bankMask);
			ser.Sync("bankNumber", ref bankNumber);
			ser.Sync("romEnable", ref romEnable);
			if (ser.IsReader)
				BankSet(bankNumber | (uint)(romEnable ? 0x00 : 0x80));
		}
	}
}
