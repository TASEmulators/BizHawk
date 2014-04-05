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
		private ByteBuffer _auxRam = new ByteBuffer(256);

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
				return _auxRam[addr & 0xFF];
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

		public override void WriteMemory(ushort addr, byte value)
		{
			Address(addr);
			if (addr < 0x1000)
			{
				base.WriteMemory(addr, value);
			}
			else if (addr < 0x1100)
			{
				_auxRam[addr & 0xFF] = value;
			}
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("bank4k", ref _bank4k);
			ser.Sync("auxRam", ref _auxRam);
		}

		public override void Dispose()
		{
			base.Dispose();
			_auxRam.Dispose();
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
