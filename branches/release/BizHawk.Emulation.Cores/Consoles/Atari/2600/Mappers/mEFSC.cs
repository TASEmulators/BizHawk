using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/**
	Cartridge class used for Homestar Runner by Paul Slocum.
	There are 16 4K banks (total of 64K ROM) with 128 bytes of RAM.
	Accessing $1FE0 - $1FEF switches to each bank.
	*/
	internal class mEFSC : MapperBase
	{
		private int _bank4k;
		private ByteBuffer _ram = new ByteBuffer(128);

		public override bool HasCartRam
		{
			get { return true; }
		}

		public override ByteBuffer CartRam
		{
			get { return _ram; }
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("bank4k", ref _bank4k);
			ser.Sync("auxRam", ref _ram);
		}

		public override void Dispose()
		{
			base.Dispose();
			_ram.Dispose();
		}

		public override void HardReset()
		{
			_bank4k = 0;
			_ram = new ByteBuffer(128);
			base.HardReset();
		}

		private byte ReadMem(ushort addr, bool peek)
		{
			if (!peek)
			{
				Address(addr);
			}

			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			if (addr < 0x1080)
			{
				_ram[(addr & 0x7F)] = 0xFF; // Reading from the write port triggers an unwanted write of open bus
				return 0xFF; // 0xFF is used for deterministic emulation, in reality it would be a random value based on pins being high or low
			}
			else if (addr < 0x1100)
			{
				return _ram[(addr & 0x7F)];
			}

			return Core.Rom[(_bank4k << 12) + (addr & 0xFFF)];
		}

		public override byte ReadMemory(ushort addr)
		{
			return ReadMem(addr, false);
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMem(addr, true);
		}

		private void WriteMem(ushort addr, byte value, bool poke)
		{
			if (!poke)
			{
				Address(addr);
			}

			if (addr < 0x1000)
			{
				base.WriteMemory(addr, value);
			}
			else if ((addr & 0x0FFF) < 0x80)
			{
				_ram[addr & 0x7F] = value;
			}
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			WriteMem(addr, value, poke: false);
		}

		public override void PokeMemory(ushort addr, byte value)
		{
			WriteMem(addr, value, poke: true);
		}

		private void Address(ushort addr)
		{
			if (addr == 0x1FE0) _bank4k = 0;
			if (addr == 0x1FE1) _bank4k = 1;
			if (addr == 0x1FE2) _bank4k = 2;
			if (addr == 0x1FE3) _bank4k = 3;
			if (addr == 0x1FE4) _bank4k = 4;
			if (addr == 0x1FE5) _bank4k = 5;
			if (addr == 0x1FE6) _bank4k = 6;
			if (addr == 0x1FE7) _bank4k = 7;
			if (addr == 0x1FE8) _bank4k = 8;
			if (addr == 0x1FE9) _bank4k = 9;
			if (addr == 0x1FEA) _bank4k = 10;
			if (addr == 0x1FEB) _bank4k = 11;
			if (addr == 0x1FEC) _bank4k = 12;
			if (addr == 0x1FED) _bank4k = 13;
			if (addr == 0x1FEE) _bank4k = 14;
			if (addr == 0x1FEF) _bank4k = 15;
		}
	}
}
