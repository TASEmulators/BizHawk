namespace BizHawk.Emulation.Consoles.Atari._2600
{
	/*
	FA (RAM Plus)
	-----

	CBS Thought they'd throw a few tricks of their own at the 2600 with this.  It's got
	12K of ROM and 256 bytes of RAM.

	This works similar to F8, except there's only 3 4K ROM banks.  The banks are selected by
	accessing 1FF8, 1FF9, and 1FFA.   There's also 256 bytes of RAM mapped into 1000-11FF.
	The write port is at 1000-10FF, and the read port is 1100-11FF.
	 */

	class mFA : MapperBase 
	{
		int toggle;
		ByteBuffer aux_ram = new ByteBuffer(256);

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
			else if (addr < 0x1100)
			{
				return 0xFF;
			}
			else if (addr < 0x1200)
			{
				return aux_ram[addr & 0xFF];
			}
			else
			{
				return core.rom[(toggle << 12) + (addr & 0xFFF)];
			}
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
				base.WriteMemory(addr, value);
			else if (addr < 0x1100) 
				aux_ram[addr & 0xFF] = value;
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("toggle", ref toggle);
			ser.Sync("ram", ref aux_ram);
		}

		void Address(ushort addr)
		{
			if (addr == 0x1FF8) toggle = 0;
			if (addr == 0x1FF9) toggle = 1;
			if (addr == 0x1FFA) toggle = 2;
		}
	}
}
