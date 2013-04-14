namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	3F (Tigervision)
	-----

	Traditionally, this method was used on the Tigervision games.  The ROMs were all 8K, and 
	there's two 2K pages in the 4K of address space.  The upper bank is fixed to the last 2K
	of the ROM.

	The first 2K is selectable by writing to any location between 0000 and 003F.  Yes, this
	overlaps the TIA, but this is not a big deal.  You simply use the mirrors of the TIA at
	40-7F instead!  To select a bank, the games write to 3Fh, because it's not implemented
	on the TIA.

	The homebrew community has decided that if 8K is good, more ROM is better!  This mapper
	can support up to 512K bytes of ROM just by implementing all 8 bits on the mapper
	register, and this has been done... however I do not think 512K ROMs have been made just
	yet.
	*/

	class m3F :MapperBase 
	{
		int lowbank_2k = 0;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("lowbank_2k", ref lowbank_2k);
		}

		public override byte ReadMemory(ushort addr)
		{
			if (addr < 0x1000)
			{
				return base.ReadMemory(addr);
			}
			else if (addr < 0x17FF) //Low 2k Bank
			{
				int a = addr & 0x07FF; //2K
				int bank = lowbank_2k << 11;
				return core.rom[bank + a];
			}
			else if (addr < 0x2000) //High bank fixed to last 2k of ROM
			{
				return core.rom[(core.rom.Length - 2048) + (addr & 0x07FF)];
			}
			return base.ReadMemory(addr);
		}

		public override byte PeekMemory(ushort addr)
		{
			return ReadMemory(addr);
		}

		public override void WriteMemory(ushort addr, byte value)
		{
			if (addr < 0x0040)
			{
				if ((value << 11) < core.rom.Length)
				{
					lowbank_2k = value;
				}
				else
				{
					lowbank_2k = value & (core.rom.Length >> 11);
				}
			}
			base.WriteMemory(addr, value);
		}
	}
}
