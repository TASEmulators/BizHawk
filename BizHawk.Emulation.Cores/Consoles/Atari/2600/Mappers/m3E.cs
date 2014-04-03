using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	3E (Boulderdash
	-----

	This works similar to 3F (Tigervision) above, except RAM has been added.  The range of
	addresses has been restricted, too.  Only 3E and 3F can be written to now.

	1000-17FF - this bank is selectable
	1800-1FFF - this bank is the last 2K of the ROM

	To select a particular 2K ROM bank, its number is poked into address 3F.  Because there's
	8 bits, there's enough for 256 2K banks, or a maximum of 512K of ROM.

	Writing to 3E, however, is what's new.  Writing here selects a 1K RAM bank into 
	1000-17FF.  The example (Boulderdash) uses 16K of RAM, however there's theoretically
	enough space for 256K of RAM.  When RAM is selected, 1000-13FF is the read port while
	1400-17FF is the write port.
	*/
	internal class m3E : MapperBase 
	{
		private int _lowbank_2K;
		private int _rambank_1K;
		private bool _hasRam;
		private ByteBuffer _ram = new ByteBuffer(262144); // Up to 256k

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("lowbank_2k", ref _lowbank_2K);
			ser.Sync("rambank_1k", ref _rambank_1K);
			ser.Sync("cart_ram", ref _ram);
			ser.Sync("hasRam", ref _hasRam);
		}

		public override void Dispose()
		{
			base.Dispose();
			_ram.Dispose();
		}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}
			
			if (addr < 0x17FF) // Low 2k Bank
			{
				if (_hasRam)
				{
					if (addr < 0x13FF)
					{
						return _ram[(addr & 0x03FF) + (_rambank_1K << 10)];
					}

					return _ram[(addr & 0x03FF) + (_rambank_1K << 10)] = 0xFF; // Reading from the write port triggers an unwanted write
				}
				
				return core.rom[(_lowbank_2K << 11) + (addr & 0x07FF)];
			}
			
			if (addr < 0x2000) // High bank fixed to last 2k of ROM
			{
				return core.rom[(core.rom.Length - 2048) + (addr & 0x07FF)];
			}

			return base.ReadMemory(addr);
		}

		public override byte PeekMemory(ushort addr)
		{
			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}
			
			if (addr < 0x17FF) // Low 2k Bank
			{
				if (_hasRam)
				{
					if (addr < 0x13FF)
					{
						return _ram[(addr & 0x03FF) + (_rambank_1K << 10)];
					}

					return _ram[(addr & 0x03FF) + (_rambank_1K << 10)]; // Reading from the write port triggers an unwanted write
				}
				
				return core.rom[(_lowbank_2K << 11) + (addr & 0x07FF)];
			}
			
			if (addr < 0x2000) // High bank fixed to last 2k of ROM
			{
				return core.rom[(core.rom.Length - 2048) + (addr & 0x07FF)];
			}

			return base.ReadMemory(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x1000)
			{
				if (addr == 0x003E)
				{
					_hasRam = true;
					_rambank_1K = value;
				}
				else if (addr == 0x003F)
				{
					_hasRam = false;
					if ((value << 11) < core.rom.Length)
					{
						_lowbank_2K = value;
					}
					else
					{
						_lowbank_2K = value & (core.rom.Length >> 11);
					}
				}

				base.WriteMemory(addr, value);
			}
			else if (addr < 0x1400)
			{
				// Writing to the read port, for shame!
			}
			else if (addr < 0x1800) // Write port
			{
				_ram[(_rambank_1K << 10) + (addr & 0x3FF)] = value;
			}
		}
	}
}
