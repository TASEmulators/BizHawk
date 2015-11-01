namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS
	{
		// =======================================================================
		// The CodeMasters mapper has 3 banks of 16kb, like the Sega mapper.
		// The differences are that the paging control addresses are different, and the first 1K of ROM is not protected.
		// Bank 0: Control Address $0000 - Maps $0000 - $3FFF
		// Bank 1: Control Address $4000 - Maps $4000 - $7FFF
		// Bank 2: Control Address $8000 - Maps $8000 - $BFFF
		// System RAM is at $C000+ as in the Sega mapper.
		// =======================================================================

		byte ReadMemoryCM(ushort address)
		{
			if (address < 0x4000) return RomData[(RomBank0 * BankSize) + address];
			if (address < 0x8000) return RomData[(RomBank1 * BankSize) + (address & BankSizeMask)];
			if (address < 0xC000) return RomData[(RomBank2 * BankSize) + (address & BankSizeMask)];

			return SystemRam[address & RamSizeMask];
		}

		CDLog_MapResults MapMemoryCM(ushort address, bool write)
		{
			if (address < 0x4000) return new CDLog_MapResults() { Type = CDLog_AddrType.ROM, Address = address };
			else if (address < 0x8000) return new CDLog_MapResults() { Type = CDLog_AddrType.ROM, Address = (RomBank1 * BankSize) + (address & BankSizeMask) };
			else if (address < 0xC000) return new CDLog_MapResults() { Type = CDLog_AddrType.ROM, Address = (RomBank2 * BankSize) + (address & BankSizeMask) };
			else return new CDLog_MapResults() { Type = CDLog_AddrType.MainRAM, Address = address & RamSizeMask };
		}

		void WriteMemoryCM(ushort address, byte value)
		{
			if (address >= 0xC000)
				SystemRam[address & RamSizeMask] = value;

			else if (address == 0x0000) RomBank0 = (byte)(value % RomBanks);
			else if (address == 0x4000) RomBank1 = (byte)(value % RomBanks);
			else if (address == 0x8000) RomBank2 = (byte)(value % RomBanks);
		}

		void InitCodeMastersMapper()
		{
			ReadMemory = ReadMemoryCM;
			WriteMemory = WriteMemoryCM;
			MapMemory = MapMemoryCM;
			WriteMemoryCM(0x0000, 0);
			WriteMemoryCM(0x4000, 1);
			WriteMemoryCM(0x8000, 0);
		}

		// =======================================================================
		// CodeMasters with on-board volatile RAM
		// =======================================================================
		 
		byte ReadMemoryCMRam(ushort address)
		{
			if (address < 0x4000) 
				return RomData[(RomBank0 * BankSize) + address];
			if (address < 0x8000)
			{
				if (address >= 0x6000 && RomBank3 == 1)
					return ExtRam[address & 0x1FFF];
				return RomData[(RomBank1 * BankSize) + (address & BankSizeMask)];
			}
			if (address < 0xC000)
			{
				if (address >= 0xA000 && RomBank3 == 1)
					return ExtRam[address & 0x1FFF];
				return RomData[(RomBank2 * BankSize) + (address & BankSizeMask)];
			}

			return SystemRam[address & RamSizeMask];
		}

		CDLog_MapResults MapMemoryCMRam(ushort address, bool write)
		{
			if (address < 0x4000) return new CDLog_MapResults() { Type = CDLog_AddrType.ROM, Address = address };
			else if (address < 0x8000)
			{
				if (address >= 0x6000 && RomBank3 == 1)
					return new CDLog_MapResults() { Type = CDLog_AddrType.CartRAM, Address = address & 0x1FFF };
				else
					return new CDLog_MapResults() { Type = CDLog_AddrType.ROM, Address = (RomBank1 * BankSize) + (address & BankSizeMask) };
			}
			else if (address < 0xC000)
			{
				if (address >= 0xA000 && RomBank3 == 1)
					return new CDLog_MapResults() { Type = CDLog_AddrType.CartRAM, Address = address & 0x1FFF };
				return new CDLog_MapResults() { Type = CDLog_AddrType.ROM, Address = (RomBank2 * BankSize) + (address & BankSizeMask) };
			}
			else return new CDLog_MapResults() { Type = CDLog_AddrType.MainRAM, Address = address & RamSizeMask };
		}

		void WriteMemoryCMRam(ushort address, byte value)
		{
			if (address >= 0xC000)
				SystemRam[address & RamSizeMask] = value;

			else if (address == 0x0000)
				RomBank0 = (byte)(value & 0xF);
			else if (address == 0x4000)
			{
				RomBank1 = (byte)(value & 0xF);
				RomBank3 = (byte)(((value & 0x80) != 0) ? 1 : 0);
			}
			else if (address == 0x8000)
				RomBank2 = (byte)(value & 0xF);
			else if (address >= 0x6000 && address < 0x8000 && RomBank3 == 1)
				ExtRam[address & 0x1FFF] = value;
			else if (address >= 0xA000 && address < 0xC000 && RomBank3 == 1)
				ExtRam[address & 0x1FFF] = value;
		}

		void InitCodeMastersMapperRam()
		{
			ReadMemory = ReadMemoryCMRam;
			WriteMemory = WriteMemoryCMRam;
			MapMemory = MapMemoryCMRam;
			WriteMemoryCM(0x0000, 0);
			WriteMemoryCM(0x4000, 1);
			WriteMemoryCM(0x8000, 0);
			ExtRam = new byte[8192];
			RomBank3 = 0;
		}
	}
}
