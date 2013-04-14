using System.Collections.Generic;

namespace BizHawk.Emulation.Computers.Commodore64.Cartridge
{
	// EasyFlash cartridge
	// No official games came on one of these but there
	// are a few dumps from GameBase64 that use this mapper

	// There are 64 banks total, DE00 is bank select.
	// Selecing a bank will select both Lo and Hi ROM.
	// DE02 will switch exrom/game bits: bit 0=game, 
	// bit 1=exrom, bit 2=for our cases, always set true.
	// These two registers are write only.

	// This cartridge always starts up in Ultimax mode,
	// with Game set high and ExRom set low.

	// There is also 256 bytes RAM at DF00-DFFF.

	public class Mapper0020 : Cart
	{
		private byte[][] banksA = new byte[64][]; //8000
		private byte[][] banksB = new byte[64][]; //A000
		private uint bankNumber;
		private bool boardLed;
		private byte[] currentBankA;
		private byte[] currentBankB;
		private byte[] dummyBank;
		private byte[] ram = new byte[256];

		public Mapper0020(List<uint> newAddresses, List<uint> newBanks, List<byte[]> newData)
		{
			uint count = (uint)newAddresses.Count;

			// build dummy bank
			dummyBank = new byte[0x2000];
			for (uint i = 0; i < 0x2000; i++)
				dummyBank[i] = 0xFF; // todo: determine if this is correct

			// force ultimax mode (the cart SHOULD set this
			// otherwise on load, according to the docs)
			pinGame = false;
			pinExRom = true;

			// for safety, initialize all banks to dummy
			for (uint i = 0; i < 64; i++)
				banksA[i] = dummyBank;
			for (uint i = 0; i < 64; i++)
				banksB[i] = dummyBank;

			// load in all banks
			for (int i = 0; i < count; i++)
			{
				if (newAddresses[i] == 0x8000)
				{
					banksA[newBanks[i]] = newData[i];
				}
				else if (newAddresses[i] == 0xA000 || newAddresses[i] == 0xE000)
				{
					banksB[newBanks[i]] = newData[i];
				}
			}

			// default to bank 0
			BankSet(0);
		}

		private void BankSet(uint index)
		{
			bankNumber = index & 0x3F;
			UpdateState();
		}

		public override byte Peek8000(int addr)
		{
			return currentBankA[addr];
		}

		public override byte PeekA000(int addr)
		{
			return currentBankB[addr];
		}

		public override byte PeekDE00(int addr)
		{
			// normally you can't read these regs
			// but Peek is provided here for debug reasons
			// and may not stay around
			if (addr == 0x00)
				return (byte)bankNumber;
			else if (addr == 0x02)
				return (byte)(
					(pinGame ? 0x00 : 0x01) |
					(pinExRom ? 0x00 : 0x02) |
					0x04 |
					0x08 |
					0x10 |
					0x20 |
					0x40 |
					(boardLed ? 0x80 : 0x00)
					);
			else
				return (byte)0xFF;
		}

		public override byte PeekDF00(int addr)
		{
			return ram[addr];
		}

		public override void PokeDE00(int addr, byte val)
		{
			if (addr == 0x00)
				BankSet(val);
			else if (addr == 0x02)
				StateSet(val);
		}

		public override void PokeDF00(int addr, byte val)
		{
			ram[addr] = val;
		}

		public override byte Read8000(ushort addr)
		{
			return currentBankA[addr];
		}

		public override byte ReadA000(ushort addr)
		{
			return currentBankB[addr];
		}

		public override byte ReadDF00(ushort addr)
		{
			return ram[addr];
		}

		private void StateSet(byte val)
		{
			pinGame = ((val & 0x01) == 0);
			pinExRom = ((val & 0x02) == 0);
			boardLed = ((val & 0x80) != 0) ? !boardLed : boardLed;
			UpdateState();
		}

		private void UpdateState()
		{
			currentBankA = banksA[bankNumber];
			currentBankB = banksB[bankNumber];
		}

		public override void WriteDE00(ushort addr, byte val)
		{
			if (addr == 0x00)
				BankSet(val);
			else if (addr == 0x02)
				StateSet(val);
		}

		public override void WriteDF00(ushort addr, byte val)
		{
			ram[addr] = val;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("bankNumber", ref bankNumber);
			ser.Sync("boardLed", ref boardLed);
			ser.Sync("ram", ref ram, false);
			if (ser.IsReader)
				UpdateState();
		}
	}
}
