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

			return ret;
		}

		CDLog_MapResults MapMemoryExt(ushort address, bool write)
		{
			if (address < 0x8000) return new CDLog_MapResults() { Type = CDLog_AddrType.ROM, Address = address };
			else if (address < 0xC000) return new CDLog_MapResults() { Type = CDLog_AddrType.CartRAM, Address = address & ExtRamMask };
			else return new CDLog_MapResults() { Type = CDLog_AddrType.MainRAM, Address = address & RamSizeMask };
		}

		void WriteMemoryExt(ushort address, byte value)
		{
			if (address < 0xC000 && address >= 0x8000)
				ExtRam[address & ExtRamMask] = value;
			else if (address >= 0xC000)
				SystemRam[address & RamSizeMask] = value;
		}

		void InitExt2kMapper(int size)
		{
			ExtRam = new byte[size];
			ExtRamMask = size - 1;
			ReadMemory = ReadMemoryExt;
			WriteMemory = WriteMemoryExt;
			MapMemory = MapMemoryExt;
		}
	}
}