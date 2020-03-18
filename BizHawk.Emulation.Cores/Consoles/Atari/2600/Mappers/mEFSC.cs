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
		private int _bank4K;
		private byte[] _ram = new byte[128];

		public mEFSC(Atari2600 core) : base(core)
		{
		}

		public override byte[] CartRam => _ram;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("bank4k", ref _bank4K);
			ser.Sync("auxRam", ref _ram, false);
		}

		public override void HardReset()
		{
			_bank4K = 0;
			_ram = new byte[128];
			base.HardReset();
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
				_ram[addr & 0x7F] = 0xFF; // Reading from the write port triggers an unwanted write of open bus
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
			_bank4K = addr switch
			{
				0x1FE0 => 0,
				0x1FE1 => 1,
				0x1FE2 => 2,
				0x1FE3 => 3,
				0x1FE4 => 4,
				0x1FE5 => 5,
				0x1FE6 => 6,
				0x1FE7 => 7,
				0x1FE8 => 8,
				0x1FE9 => 9,
				0x1FEA => 10,
				0x1FEB => 11,
				0x1FEC => 12,
				0x1FED => 13,
				0x1FEE => 14,
				0x1FEF => 15,
				_ => _bank4K
			};
		}
	}
}
