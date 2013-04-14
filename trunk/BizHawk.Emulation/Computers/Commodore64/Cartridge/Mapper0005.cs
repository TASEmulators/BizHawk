using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Computers.Commodore64.Cartridge
{
	public class Mapper0005 : Cart
	{
		private byte[][] banksA = new byte[0][]; //8000
		private byte[][] banksB = new byte[0][]; //A000
		private uint bankMask;
		private uint bankNumber;
		private byte[] currentBankA;
		private byte[] currentBankB;
		private byte[] dummyBank;

		public Mapper0005(List<uint> newAddresses, List<uint> newBanks, List<byte[]> newData)
		{
			uint count = (uint)newAddresses.Count;

			// build dummy bank
			dummyBank = new byte[0x2000];
			for (uint i = 0; i < 0x2000; i++)
				dummyBank[i] = 0xFF; // todo: determine if this is correct

			if (count == 64) //512k
			{
				pinGame = true;
				pinExRom = false;
				bankMask = 0x3F;
				banksA = new byte[64][];
			}
			else if (count == 32) //256k
			{
				// this specific config is a weird exception
				pinGame = false;
				pinExRom = false;
				bankMask = 0x0F;
				banksA = new byte[16][];
				banksB = new byte[16][];
			}
			else if (count == 16) //128k
			{
				pinGame = true;
				pinExRom = false;
				bankMask = 0x0F;
				banksA = new byte[16][];
			}
			else if (count == 8) //64k
			{
				pinGame = true;
				pinExRom = false;
				bankMask = 0x07;
				banksA = new byte[8][];
			}
			else if (count == 4) //32k
			{
				pinGame = true;
				pinExRom = false;
				bankMask = 0x03;
				banksA = new byte[4][];
			}
			else if (count == 2) //16k
			{
				pinGame = true;
				pinExRom = false;
				bankMask = 0x01;
				banksA = new byte[2][];
			}
			else if (count == 1) //8k
			{
				pinGame = true;
				pinExRom = false;
				bankMask = 0x00;
				banksA = new byte[1][];
			}
			else
			{
				// we don't know what format this is...
				throw new Exception("This looks like an Ocean cartridge but cannot be loaded...");
			}

			// for safety, initialize all banks to dummy
			for (uint i = 0; i < banksA.Length; i++)
				banksA[i] = dummyBank;
			for (uint i = 0; i < banksB.Length; i++)
				banksB[i] = dummyBank;

			// now load in the banks
			for (int i = 0; i < count; i++)
			{
				if (newAddresses[i] == 0x8000)
				{
					banksA[newBanks[i] & bankMask] = newData[i];
				}
				else if (newAddresses[i] == 0xA000 || newAddresses[i] == 0xE000)
				{
					banksB[newBanks[i] & bankMask] = newData[i];
				}
			}

			BankSet(0);
		}

		private void BankSet(uint index)
		{
			bankNumber = index & bankMask;
			if (!pinExRom)
				currentBankA = banksA[bankNumber];
			else
				currentBankA = dummyBank;

			if (!pinGame)
				currentBankB = banksB[bankNumber];
			else
				currentBankB = dummyBank;
		}

		public override byte Peek8000(int addr)
		{
			return currentBankA[addr];
		}

		public override byte PeekA000(int addr)
		{
			return currentBankB[addr];
		}

		public override void PokeDE00(int addr, byte val)
		{
			if (addr == 0x00)
				BankSet(val);
		}

		public override byte Read8000(ushort addr)
		{
			return currentBankA[addr];
		}

		public override byte ReadA000(ushort addr)
		{
			return currentBankB[addr];
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
			if (ser.IsReader)
				BankSet(bankNumber);
		}
	}
}
