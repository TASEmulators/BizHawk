﻿using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	3E (Boulderdash
	-----

	This works similar to 3F (TigerVision) above, except RAM has been added.  The range of
	addresses has been restricted, too.  Only 3E and 3F can be written to now.

	1000-17FF - this bank is selectable
	1800-1FFF - this bank is the last 2K of the ROM

	To select a particular 2K ROM bank, its number is poked into address 3F.  Because there's
	8 bits, there's enough for 256 2K banks, or a maximum of 512K of ROM.

	Writing to 3E, however, is what's new.  Writing here selects a 1K RAM bank into
	1000-17FF.  The example (BoulderDash) uses 16K of RAM, however there's theoretically
	enough space for 256K of RAM.  When RAM is selected, 1000-13FF is the read port while
	1400-17FF is the write port.
	*/
	internal sealed class m3E : MapperBase
	{
		public m3E(Atari2600 core) : base(core)
		{
		}

		private int _lowBank2K;
		private int _ramBank1K;
		private bool _hasRam;
		private byte[] _ram = new byte[256 * 1024]; // Up to 256k

		public override byte[] CartRam => _ram;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("lowbank_2k", ref _lowBank2K);
			ser.Sync("rambank_1k", ref _ramBank1K);
			ser.Sync("cart_ram", ref _ram, false);
			ser.Sync("hasRam", ref _hasRam);
		}

		public override void HardReset()
		{
			_lowBank2K = 0;
			_ramBank1K = 0;
			_hasRam = false;
			_ram = new byte[256 * 1024];
		}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			if (addr < 0x1800) // Low 2k Bank
			{
				if (_hasRam)
				{
					if (addr < 0x1400)
					{
						return _ram[(addr & 0x03FF) + (_ramBank1K << 10)];
					}

					return _ram[(addr & 0x03FF) + (_ramBank1K << 10)] = 0xFF; // Reading from the write port triggers an unwanted write
				}

				return Core.Rom[(_lowBank2K << 11) + (addr & 0x07FF)];
			}

			if (addr < 0x2000) // High bank fixed to last 2k of ROM
			{
				return Core.Rom[Core.Rom.Length - 0x800 + (addr & 0x07FF)];
			}

			return base.ReadMemory(addr);
		}

		public override byte PeekMemory(ushort addr) => ReadMemory(addr);

		public override void WriteMemory(ushort addr, byte value)
			=> WriteMem(addr, value, false);

		public override void PokeMemory(ushort addr, byte value)
			=> WriteMem(addr, value, true);

		private void WriteMem(ushort addr, byte value, bool poke)
		{
			if (!poke)
			{
				if (addr == 0x003E)
				{
					_hasRam = true;
					_ramBank1K = value;
				}
				else if (addr == 0x003F)
				{
					_hasRam = false;
					if (value << 11 < Core.Rom.Length)
					{
						_lowBank2K = value;
					}
					else
					{
						_lowBank2K = value & (Core.Rom.Length >> 11);
					}
				}
			}

			if (addr < 0x1000)
			{
				base.WriteMemory(addr, value);
			}
			else if (addr < 0x1400)
			{
				// Writing to the read port, for shame!
			}
			else if (addr < 0x1800) // Write port
			{
				_ram[(_ramBank1K << 10) + (addr & 0x3FF)] = value;
			}
		}
	}
}
