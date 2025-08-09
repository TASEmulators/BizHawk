namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS
	{
		private byte ReadMemorySG_EX_B(ushort address)
		{
			byte ret;

			if (address < 0xC000)
				ret = RomData[address & RomMask];
			else
				ret = SystemRam[address & RamSizeMask];

			return ret;
		}

		private CDLog_MapResults MapMemorySG_EX_B(ushort address, bool write)
		{
			if (address < 0xC000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = address & ExtRamMask };
			return new CDLog_MapResults { Type = CDLog_AddrType.MainRAM, Address = address & RamSizeMask };
		}

		private void WriteMemorySG_EX_B(ushort address, byte value)
		{
			if (address >= 0xC000)
				SystemRam[address & RamSizeMask] = value;
		}

		private void Init_SG_EX_B()
		{
			ReadMemoryMapper = ReadMemorySG_EX_B;
			WriteMemoryMapper = WriteMemorySG_EX_B;
			MapMemory = MapMemorySG_EX_B;
		}
	}
}