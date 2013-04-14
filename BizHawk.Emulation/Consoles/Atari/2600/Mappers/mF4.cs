namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	F4 (Atari style 32K)
	-----

	Again, this works like F8 and F6 except now there's 8 4K banks.  Selection is performed
	by accessing 1FF4 through 1FFB.
	*/

	class mF4 :MapperBase 
	{
		int toggle = 0;

		private byte ReadMem(ushort addr, bool peek)
		{
			if (!peek)
			{
				Address(addr);
			}

			if (addr < 0x1000) return base.ReadMemory(addr);
			return core.rom[toggle * 4096 + (addr & 0xFFF)];
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
			if (addr == 0x1FF4) toggle = 0;
			if (addr == 0x1FF5) toggle = 1;
			if (addr == 0x1FF6) toggle = 2;
			if (addr == 0x1FF7) toggle = 3;
			if (addr == 0x1FF8) toggle = 4;
			if (addr == 0x1FF9) toggle = 5;
			if (addr == 0x1FF9) toggle = 5;
			if (addr == 0x1FFA) toggle = 6;
			if (addr == 0x1FFB) toggle = 7;
		}
	}
}
