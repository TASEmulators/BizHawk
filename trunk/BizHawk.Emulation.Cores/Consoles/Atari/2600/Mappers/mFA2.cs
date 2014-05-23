using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/**
	This is an extended version of the CBS RAM Plus bankswitching scheme
	supported by the Harmony cartridge.
  
	There are six (or seven) 4K banks and 256 bytes of RAM.
	*/
	internal class mFA2 : MapperBase
	{
		private int _bank4k;
		private ByteBuffer _ram = new ByteBuffer(256);

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
			_ram = new ByteBuffer(256);
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

			if (addr < 0x1100)
			{
				return 0xFF;
			}

			if (addr < 0x1200)
			{
				return _ram[addr & 0xFF];
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
			else if (addr < 0x1100)
			{
				_ram[addr & 0xFF] = value;
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
			if (addr == 0x1FF5) _bank4k = 0;
			if (addr == 0x1FF6) _bank4k = 1;
			if (addr == 0x1FF7) _bank4k = 2;
			if (addr == 0x1FF8) _bank4k = 3;
			if (addr == 0x1FF9) _bank4k = 4;
			if (addr == 0x1FFA) _bank4k = 5;
			if (addr == 0x1FFB && Core.Rom.Length == 28 * 1024) // Only available on 28k Roms
			{
				_bank4k = 6;
			}
		}
	}
}
