﻿using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	/*
	3F (TigerVision)
	-----

	Traditionally, this method was used on the TigerVision games.  The ROMs were all 8K, and
	there's two 2K pages in the 4K of address space. The upper bank is fixed to the last 2K
	of the ROM.

	The first 2K is selectable by writing to any location between 0000 and 003F.  Yes, this
	overlaps the TIA, but this is not a big deal.  You simply use the mirrors of the TIA at
	40-7F instead!  To select a bank, the games write to 3Fh, because it's not implemented
	on the TIA.

	The homebrew community has decided that if 8K is good, more ROM is better!  This mapper
	can support up to 512K bytes of ROM just by implementing all 8 bits on the mapper
	register, and this has been done... however do not think 512K ROMs have been made just
	yet.
	*/
	internal sealed class m3F : MapperBase
	{
		private int _lowBank2K;

		public m3F(Atari2600 core) : base(core)
		{
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("lowbank_2k", ref _lowBank2K);
		}

		public override void HardReset()
		{
			_lowBank2K = 0;
		}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}

			if (addr < 0x1800) // Low 2k Bank
			{
				return Core.Rom[(_lowBank2K << 11) + (addr & 0x07FF)];
			}

			if (addr < 0x2000) // High bank fixed to last 2k of ROM
			{
				return Core.Rom[Core.Rom.Length - 2048 + (addr & 0x07FF)];
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
				if (addr < 0x0040)
				{
					if ((value << 11) < Core.Rom.Length)
					{
						_lowBank2K = value;
					}
					else
					{
						_lowBank2K = value & (Core.Rom.Length >> 11);
					}
				}
			}

			base.WriteMemory(addr, value);
		}
	}
}
