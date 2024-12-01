using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
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
	internal sealed class Mapper0020 : CartridgeDevice
	{
		private int _bankOffset = 63 << 13;

		private int[] _banksA = new int[64 << 13]; // 8000
		private int[] _banksB = new int[64 << 13]; // A000

		private readonly int[] _originalMediaA; // 8000
		private readonly int[] _originalMediaB; // A000

		private bool _boardLed;

		private bool _jumper;

		private int _stateBits;

		private int[] _ram = new int[256];

		private bool _commandLatch55;
		private bool _commandLatchAa;

		private int _internalRomState;

		public Mapper0020(IList<int> newAddresses, IList<int> newBanks, IList<int[]> newData)
		{
			DriveLightEnabled = true;
			var count = newAddresses.Count;

			// force ultimax mode (the cart SHOULD set this
			// otherwise on load, according to the docs)
			pinGame = false;
			pinExRom = true;

			// for safety, initialize all banks to dummy
			for (var i = 0; i < 64 * 0x2000; i++)
			{
				_banksA[i] = 0xFF;
				_banksB[i] = 0xFF;
			}

			// load in all banks
			for (var i = 0; i < count; i++)
			{
				switch (newAddresses[i])
				{
					case 0x8000:
						Array.Copy(newData[i], 0, _banksA, newBanks[i] * 0x2000, 0x2000);
						break;
					case 0xA000:
					case 0xE000:
						Array.Copy(newData[i], 0, _banksB, newBanks[i] * 0x2000, 0x2000);
						break;
				}
			}

			// default to bank 0
			BankSet(0);

			// internal operation settings
			_commandLatch55 = false;
			_commandLatchAa = false;
			_internalRomState = 0;

			// back up original media
			_originalMediaA = _banksA.Select(d => d).ToArray();
			_originalMediaB = _banksB.Select(d => d).ToArray();
		}

		protected override void SyncStateInternal(Serializer ser)
		{
			ser.Sync("BankOffset", ref _bankOffset);
			ser.Sync("BoardLed", ref _boardLed);
			ser.Sync("Jumper", ref _jumper);
			ser.Sync("StateBits", ref _stateBits);
			ser.Sync("RAM", ref _ram, useNull: false);
			ser.Sync("CommandLatch55", ref _commandLatchAa);
			ser.Sync("CommandLatchAA", ref _commandLatchAa);
			ser.Sync("InternalROMState", ref _internalRomState);
			ser.SyncDelta("MediaStateA", _originalMediaA, _banksA);
			ser.SyncDelta("MediaStateB", _originalMediaB, _banksB);
			DriveLightOn = _boardLed;
		}

		private void BankSet(int index)
		{
			_bankOffset = (index & 0x3F) << 13;
		}

		public override int Peek8000(int addr)
		{
			addr &= 0x1FFF;
			return _banksA[addr | _bankOffset];
		}

		public override int PeekA000(int addr)
		{
			addr &= 0x1FFF;
			return _banksB[addr | _bankOffset];
		}

		public override int PeekDE00(int addr)
		{
			// normally you can't read these regs
			// but Peek is provided here for debug reasons
			// and may not stay around
			addr &= 0x02;
			return addr == 0x00 ? _bankOffset >> 13 : _stateBits;
		}

		public override int PeekDF00(int addr)
		{
			addr &= 0xFF;
			return _ram[addr];
		}

		public override void PokeDE00(int addr, int val)
		{
			addr &= 0x02;
			if (addr == 0x00)
			{
				BankSet(val);
			}
			else
			{
				StateSet(val);
			}
		}

		public override void PokeDF00(int addr, int val)
		{
			addr &= 0xFF;
			_ram[addr] = val & 0xFF;
		}

		public override int Read8000(int addr)
		{
			return ReadInternal(addr & 0x1FFF, _banksA);
		}

		public override int ReadA000(int addr)
		{
			return ReadInternal(addr & 0x1FFF, _banksB);
		}

		public override int ReadDF00(int addr)
		{
			addr &= 0xFF;
			return _ram[addr];
		}

		private int ReadInternal(int addr, int[] bank)
		{
			switch (_internalRomState)
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

			return bank[addr | _bankOffset];
		}

		private void StateSet(int val)
		{
			_stateBits = val &= 0x87;
			if ((val & 0x04) != 0)
			{
				pinGame = (val & 0x01) == 0;
			}
			else
			{
				pinGame = _jumper;
			}

			pinExRom = (val & 0x02) == 0;
			_boardLed = (val & 0x80) != 0;
			_internalRomState = 0;
			DriveLightOn = _boardLed;
		}

		public override void Write8000(int addr, int val)
		{
			WriteInternal(addr, val);
		}

		public override void WriteA000(int addr, int val)
		{
			WriteInternal(addr | 0x2000, val);
		}

		private void WriteInternal(int addr, int val)
		{
			if (pinGame || !pinExRom)
			{
				return;
			}

			if (val == 0xF0) // any address, resets flash
			{
				_internalRomState = 0;
				_commandLatch55 = false;
				_commandLatchAa = false;
			}
			else if (_internalRomState != 0x00 && _internalRomState != 0xF0)
			{
				switch (_internalRomState)
				{
					case 0xA0:
						if ((addr & 0x2000) == 0)
						{
							addr &= 0x1FFF;
							_banksA[addr | _bankOffset] = val & 0xFF;
						}
						else
						{
							addr &= 0x1FFF;
							_banksB[addr | _bankOffset] = val & 0xFF;
						}

						break;
				}
			}
			else if (addr == 0x0555) // $8555
			{
				if (!_commandLatchAa)
				{
					if (val == 0xAA)
					{
						_commandLatch55 = true;
					}
				}
				else
				{
					// process EZF command
					_internalRomState = val;
				}
			}
			else if (addr == 0x02AA) // $82AA
			{
				if (_commandLatch55 && val == 0x55)
				{
					_commandLatchAa = true;
				}
				else
				{
					_commandLatch55 = false;
				}
			}
			else
			{
				_commandLatch55 = false;
				_commandLatchAa = false;
			}
		}

		public override void WriteDE00(int addr, int val)
		{
			addr &= 0x02;
			if (addr == 0x00)
			{
				BankSet(val);
			}
			else
			{
				StateSet(val);
			}
		}

		public override void WriteDF00(int addr, int val)
		{
			_ram[addr] = val & 0xFF;
		}
	}
}
