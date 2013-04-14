namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	EF (no name?)
	-----

	This is a fairly simple method that allows for up to 64K of ROM, using 16 4K banks.
	It works similar to F8, F6, etc.  Only the addresses to perform the switch is
	1FE0-1FEF.  Accessing one of these will select the desired bank. 1FE0 = bank 0,
	1FE1 = bank 1, etc.
	*/

	class mEF : MapperBase 
	{
		int toggle = 0;

		private byte ReadMem(ushort addr, bool peek)
		{
			if (!peek)
			{
				Address(addr);
			}

			if (addr < 0x1000) return base.ReadMemory(addr);
			return core.rom[toggle * 4 * 1024 + (addr & 0xFFF)];
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
			if (addr < 0x1000) base.WriteMemory(addr, value);
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("toggle", ref toggle);
		}

		void Address(ushort addr)
		{
			if (addr == 0x1FE0) toggle = 0;
			if (addr == 0x1FE1) toggle = 1;
			if (addr == 0x1FE2) toggle = 2;
			if (addr == 0x1FE3) toggle = 3;
			if (addr == 0x1FE4) toggle = 4;
			if (addr == 0x1FE5) toggle = 5;
			if (addr == 0x1FE6) toggle = 6;
			if (addr == 0x1FE7) toggle = 7;
			if (addr == 0x1FE8) toggle = 8;
			if (addr == 0x1FE9) toggle = 9;
			if (addr == 0x1FEA) toggle = 10;
			if (addr == 0x1FEB) toggle = 11;
			if (addr == 0x1FEC) toggle = 12;
			if (addr == 0x1FED) toggle = 13;
			if (addr == 0x1FEE) toggle = 14;
			if (addr == 0x1FEF) toggle = 15;
		}
	}
}
