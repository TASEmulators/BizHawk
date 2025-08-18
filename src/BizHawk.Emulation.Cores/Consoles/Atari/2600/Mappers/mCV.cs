﻿using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	This was used by Commavid.  It allowed for both ROM and RAM on the cartridge,
	without using bankswitching.  There's 2K of ROM and 1K of RAM.

	2K of ROM is mapped at 1800-1FFF.
	1K of RAM is mapped in at 1000-17FF.

	The read port is at 1000-13FF.
	The write port is at 1400-17FF.

	Example games:
		Magicard
	 */
	internal sealed class mCV : MapperBase
	{
		private byte[] _ram = new byte[1024];

		public mCV(Atari2600 core) : base(core)
		{
		}

		public override byte[] CartRam => _ram;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("aux_ram", ref _ram, false);
		}

		public override void HardReset()
		{
			_ram = new byte[1024];
		}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			if (addr < 0x1400)
			{
				return _ram[(addr & 0x3FF)];
			}

			if (addr >= 0x1800 && addr < 0x2000)
			{
				return Core.Rom[addr & 0x7FF];
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
			if (addr < 0x1000)
			{
				base.WriteMemory(addr, value);
			}
			else if (addr >= 0x1400 && addr < 0x1800)
			{
				_ram[addr & 0x3FF] = value;
			}
		}
	}
}
