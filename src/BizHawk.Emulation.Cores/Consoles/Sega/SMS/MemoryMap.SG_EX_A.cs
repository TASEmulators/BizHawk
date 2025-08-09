namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS
	{
		private byte ReadMemorySG_EX_A(ushort address)
		{
			byte ret;

			if (address < 0x2000)
				ret = RomData[address & RomMask];
			else if (address < 0x4000)
				ret = ExtRam[address & ExtRamMask];
			else if (address < 0xC000)
				ret = RomData[address & RomMask];
			else
				ret = SystemRam[address & 0x3FF]; // only 1 KB

			return ret;
		}

		private CDLog_MapResults MapMemorySG_EX_A(ushort address, bool write)
		{
			if (address < 0x2000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = address };
			if (address < 0x4000) return new CDLog_MapResults { Type = CDLog_AddrType.CartRAM, Address = address & ExtRamMask };
			if (address < 0xC000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = address };
			return new CDLog_MapResults { Type = CDLog_AddrType.MainRAM, Address = address & RamSizeMask };
		}

		private void WriteMemorySG_EX_A(ushort address, byte value)
		{
			if (address < 0x4000 && address >= 0x2000)
				ExtRam[address & ExtRamMask] = value;
			else if (address >= 0xC000)
				SystemRam[address & 0x3FF] = value;
		}

		private void Init_SG_EX_A()
		{
			// 2 regions of RAM
			ExtRam = new byte[0x2000];
			ExtRamMask = 0x1FFF;
			ReadMemoryMapper = ReadMemorySG_EX_A;
			WriteMemoryMapper = WriteMemorySG_EX_A;
			MapMemory = MapMemorySG_EX_A;
		}
	}
}