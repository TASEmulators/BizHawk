namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	F6 (Atari style 16K)
	-----

	This is a fairly standard 16K bankswitching method.  It works like F8, except there's
	four 4K banks of ROM, selected by accessing 1FF6 through 1FF9.  These sequentially
	select one of the 4 banks.  i.e. 1FF6 selects bank 0, 1FF7 selects bank 1, etc.
	*/

	class mF6 : MapperBase 
	{
		int toggle;

		private byte ReadMem(ushort addr, bool peek)
		{
			if (!peek)
			{
				Address(addr);
			}

			if (addr < 0x1000) return base.ReadMemory(addr);
			return core.rom[(toggle << 12) + (addr & 0xFFF)];
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
			if (addr == 0x1FF6) toggle = 0;
			if (addr == 0x1FF7) toggle = 1;
			if (addr == 0x1FF8) toggle = 2;
			if (addr == 0x1FF9) toggle = 3;
		}
	}
}
