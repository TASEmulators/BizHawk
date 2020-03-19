using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	This is an extended version of the CBS RAM Plus bankswitching scheme
	supported by the Harmony cartridge.
  
	There are six (or seven) 4K banks and 256 bytes of RAM.
	*/
	internal sealed class mFA2 : MapperBase
	{
		private int _bank4K;
		private byte[] _ram = new  byte[256];

		public mFA2(Atari2600 core) : base(core)
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
			_ram = new byte[256];
		}

		public override byte ReadMemory(ushort addr)  => ReadMem(addr, false);

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

			if (addr < 0x1100)
			{
				return 0xFF;
			}

			if (addr < 0x1200)
			{
				return _ram[addr & 0xFF];
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
			else if (addr < 0x1100)
			{
				_ram[addr & 0xFF] = value;
			}
		}

		private void Address(ushort addr)
		{
			_bank4K = addr switch
			{
				0x1FF5 => 0,
				0x1FF6 => 1,
				0x1FF7 => 2,
				0x1FF8 => 3,
				0x1FF9 => 4,
				0x1FFA => 5,
				0x1FFB when Core.Rom.Length == 28 * 1024 => 6,
				_ => _bank4K
			};
		}
	}
}
