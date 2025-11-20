using System.Collections.Generic;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cartridge
{
	// Mapper for a few Domark and HES Australia games.
	// It seems a lot of people dumping these have remapped
	// them to the Ocean mapper (0005) but this is still here
	// for compatibility.
	//
	// Bank select is DE00, bit 7 enabled means to disable
	// ROM in 8000-9FFF.

	internal sealed class Mapper0013 : CartridgeDevice
	{
		private const int BankSize = 0x2000;
		private const byte DummyData = 0xFF;

		private readonly byte[][] _banks = new byte[128][];

		private readonly byte _bankMask;
		private readonly int _bankCount;
		private byte _bankNumber;
		private byte[] _currentBank;
		private bool _romEnable;

		public Mapper0013(IEnumerable<CartridgeChip> chips)
		{
			pinGame = true;
			pinExRom = false;
			_romEnable = true;

			// This bank will be chosen if uninitialized.
			var dummyBank = new byte[BankSize];
			dummyBank.AsSpan().Fill(DummyData);
			_banks.AsSpan().Fill(dummyBank);
			_bankMask = 0x00;

			// Load in each bank.
			var maxBank = 0;
			foreach (var chip in chips)
			{
				// Maximum 128 banks.
				if (chip.Bank is > 0x7F or < 0x00)
				{
					throw new Exception("Cartridge image has an invalid bank");
				}

				// Addresses other than 0x8000 are not supported.
				if (chip.Address != 0x8000)
				{
					continue;
				}

				// Bank wrap-around is based on powers of 2.
				while (chip.Bank > _bankMask)
				{
					_bankMask = unchecked((byte) ((_bankMask << 1) | 1));
				}

				var bank = new byte[BankSize];

				bank.AsSpan().Fill(DummyData);
				chip.ConvertDataToBytes().CopyTo(bank.AsSpan());

				_banks[chip.Bank] = bank;

				if (chip.Bank > maxBank)
				{
					maxBank = chip.Bank;
				}
			}

			_bankCount = maxBank + 1;

			// Start with bank 0.
			BankSet(0);
		}

		public override IEnumerable<MemoryDomain> CreateMemoryDomains()
		{
			yield return new MemoryDomainDelegate(
				name: "ROM",
				size: _bankCount * BankSize,
				endian: MemoryDomain.Endian.Little,
				peek: a => _banks[a >> 13][a & 0x1FFF],
				poke: (a, d) => _banks[a >> 13][a & 0x1FFF] = d,
				wordSize: 1
			);
		}

		protected override void SyncStateInternal(Serializer ser)
		{
			ser.Sync("BankNumber", ref _bankNumber);
			ser.Sync("ROMEnable", ref _romEnable);

			if (ser.IsReader)
			{
				BankSet(_bankNumber | (_romEnable ? 0x00 : 0x80));
			}
		}

		private void BankSet(int index)
		{
			_bankNumber = unchecked((byte) (index & _bankMask));
			_romEnable = (index & 0x80) == 0;
			UpdateState();
		}

		public override int Peek8000(int addr) =>
			_currentBank[addr];

		public override void PokeDE00(int addr, int val)
		{
			if (addr == 0x00)
			{
				BankSet(val);
			}
		}

		public override int Read8000(int addr) =>
			_currentBank[addr];

		private void UpdateState()
		{
			_currentBank = _banks[_bankNumber];

			(pinExRom, pinGame) = _romEnable
				? (false, true)
				: (true, true);
		}

		public override void WriteDE00(int addr, int val)
		{
			if (addr == 0x00)
			{
				BankSet(val);
			}
		}
	}
}
