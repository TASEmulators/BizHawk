namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS
	{
		byte[] ExtRam;
		int ExtRamMask;

		byte ReadMemoryExt(ushort address)
		{
			byte ret;

			if (address < 0x8000)
				ret = RomData[address];
			else if (address < 0xC000)
				ret = ExtRam[address & ExtRamMask];
			else
				ret = SystemRam[address & RamSizeMask];

			MemoryCallbacks.CallReads(address);
			return ret;
		}

		void WriteMemoryExt(ushort address, byte value)
		{
			if (address < 0xC000 && address >= 0x8000)
				ExtRam[address & ExtRamMask] = value;
			else if (address >= 0xC000)
				SystemRam[address & RamSizeMask] = value;

			MemoryCallbacks.CallWrites((uint)address);
		}

		void InitExt2kMapper(int size)
		{
			ExtRam = new byte[size];
			ExtRamMask = size - 1;
			Cpu.ReadMemory = ReadMemoryExt;
			Cpu.WriteMemory = WriteMemoryExt;
		}
	}
}