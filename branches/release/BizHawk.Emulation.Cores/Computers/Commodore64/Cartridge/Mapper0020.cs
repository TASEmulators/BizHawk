using System.Collections.Generic;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
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

	// We emulate having the AM29F040 chip.

	sealed public class Mapper0020 : Cart
	{
		private byte[][] banksA = new byte[64][]; //8000
		private byte[][] banksB = new byte[64][]; //A000
		private int bankNumber;
		private bool boardLed;
		private byte[] currentBankA;
		private byte[] currentBankB;
		private byte[] dummyBank;
        private bool jumper = false;
        private int stateBits;
		private byte[] ram = new byte[256];
		private bool commandLatch55;
		private bool commandLatchAA;
		private int internalRomState;

		public Mapper0020(List<int> newAddresses, List<int> newBanks, List<byte[]> newData)
		{
			int count = newAddresses.Count;

			// build dummy bank
			dummyBank = new byte[0x2000];
			for (int i = 0; i < 0x2000; i++)
				dummyBank[i] = 0xFF; // todo: determine if this is correct

			// force ultimax mode (the cart SHOULD set this
			// otherwise on load, according to the docs)
			pinGame = false;
			pinExRom = true;

			// for safety, initialize all banks to dummy
			for (int i = 0; i < 64; i++)
				banksA[i] = dummyBank;
			for (int i = 0; i < 64; i++)
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

			// internal operation settings
			commandLatch55 = false;
			commandLatchAA = false;
			internalRomState = 0;
		}

		private void BankSet(int index)
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
            addr &= 0x02;
            if (addr == 0x00)
                return (byte)bankNumber;
            else
                return (byte)stateBits;
		}

		public override byte PeekDF00(int addr)
		{
			return ram[addr];
		}

		public override void PokeDE00(int addr, byte val)
		{
            addr &= 0x02;
			if (addr == 0x00)
				BankSet(val);
			else
				StateSet(val);
		}

		public override void PokeDF00(int addr, byte val)
		{
			ram[addr] = val;
		}

		public override byte Read8000(int addr)
		{
			return ReadInternal(addr);
		}

		public override byte ReadA000(int addr)
		{
			return ReadInternal(addr | 0x2000);
		}

		public override byte ReadDF00(int addr)
		{
			return ram[addr];
		}

		private byte ReadInternal(int addr)
		{
			switch (internalRomState)
			{
				case 0x80:
					break;
				case 0x90:
					switch (addr & 0x1FFF)
					{
						case 0x0000:
							return 0x01;
						case 0x0001:
							return 0xA4;
						case 0x0002:
							return 0x00;
					}
					break;
				case 0xA0:
					break;
				case 0xF0:
					break;
			}
			if ((addr & 0x3FFF) < 0x2000)
			{
				return currentBankA[addr & 0x1FFF];
			}
			else
			{
				return currentBankB[addr & 0x1FFF];
			}
		}

		private void StateSet(byte val)
		{
            stateBits = val &= 0x87;
            if ((val & 0x04) != 0)
                pinGame = ((val & 0x01) == 0);
            else
                pinGame = jumper;
			pinExRom = ((val & 0x02) == 0);
			boardLed = ((val & 0x80) != 0);
			internalRomState = 0;
			UpdateState();
		}

		private void UpdateState()
		{
			currentBankA = banksA[bankNumber];
			currentBankB = banksB[bankNumber];
		}

        public override void Write8000(int addr, byte val)
        {
			WriteInternal(addr, val);
        }

		public override void WriteA000(int addr, byte val)
		{
			WriteInternal(addr | 0x2000, val);
		}

		private void WriteInternal(int addr, byte val)
		{
			if (!pinGame && pinExRom)
			{
				System.Diagnostics.Debug.WriteLine("EasyFlash Write: $" + C64Util.ToHex(addr | 0x8000, 4) + " = " + C64Util.ToHex(val, 2));
				if (val == 0xF0) // any address, resets flash
				{
					internalRomState = 0;
					commandLatch55 = false;
					commandLatchAA = false;
				}
				else if (internalRomState != 0x00 && internalRomState != 0xF0)
				{
					switch (internalRomState)
					{
						case 0xA0:
							if ((addr & 0x2000) == 0)
							{
								addr &= 0x1FFF;
								banksA[bankNumber][addr] = val;
								currentBankA[addr] = val;
							}
							else
							{
								addr &= 0x1FFF;
								banksB[bankNumber][addr] = val;
								currentBankB[addr] = val;
							}
							break;
					}
				}
				else if (addr == 0x0555) // $8555
				{
					if (!commandLatchAA)
					{
						if (val == 0xAA)
						{
							commandLatch55 = true;
						}
					}
					else
					{
						// process EZF command
						internalRomState = val;
					}
				}
				else if (addr == 0x02AA) // $82AA
				{
					if (commandLatch55 && val == 0x55)
					{
						commandLatchAA = true;
					}
					else
					{
						commandLatch55 = false;
					}
				}
				else
				{
					commandLatch55 = false;
					commandLatchAA = false;
				}
			}
		}

		public override void WriteDE00(int addr, byte val)
		{
            addr &= 0x02;
			if (addr == 0x00)
				BankSet(val);
			else
				StateSet(val);
		}

		public override void WriteDF00(int addr, byte val)
		{
			ram[addr] = val;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			if (ser.IsReader)
				UpdateState();
		}
	}
}
