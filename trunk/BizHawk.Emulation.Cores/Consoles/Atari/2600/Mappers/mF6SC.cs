using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/**
	Cartridge class used for Atari's 16K bankswitched games with
	128 bytes of RAM.  There are four 4K banks.
	*/
	internal class mF6SC : MapperBase
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
			ser.Sync("bank_4k", ref _bank4k);
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
			if (addr == 0x1FF6) _bank4k = 0;
			if (addr == 0x1FF7) _bank4k = 1;
			if (addr == 0x1FF8) _bank4k = 2;
			if (addr == 0x1FF9) _bank4k = 3;
		}
	}
}