using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/**
	Cartridge class used for Atari's 8K bankswitched games with
	128 bytes of RAM.  There are two 4K banks.
	*/
	internal sealed class mF8SC : MapperBase
	{
		private int _bank4K;
		private byte[] _ram = new byte[128];

		public mF8SC(Atari2600 core) : base(core)
		{
		}

		public override byte[] CartRam => _ram;

		public override void HardReset()
		{
			_bank4K = 0;
			_ram = new byte[128];
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("bank_4k", ref _bank4K);
			ser.Sync("auxRam", ref _ram, false);
		}

		public override byte ReadMemory(ushort addr) => ReadMem(addr, false);

		public override byte PeekMemory(ushort addr) => ReadMem(addr, true);

		public override void WriteMemory(ushort addr, byte value)
			=> WriteMem(addr, value, false);

		public override void PokeMemory(ushort addr, byte value)
			=> WriteMem(addr, value, true);

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

			if (addr < 0x1100)
			{
				return _ram[addr & 0x7F];
			}

			return Core.Rom[(_bank4K << 12) + (addr & 0xFFF)];
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

		private void Address(ushort addr)
		{
			if (addr == 0x1FF8)
			{
				_bank4K = 0;
			}
			else if (addr == 0x1FF9)
			{
				_bank4K = 1;
			}
		}
	}
}
