namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS
	{
		// Bank 0: Fixed - Maps $0000 - $3FFF
		// Bank 1: Fixed - Maps $4000 - $7FFF
		// Bank 2: Control Address $A000 - Maps $8000 - $BFFF
		private byte ReadMemoryKR(ushort address)
		{
			if (address < 0xC000)
			{
				if ((Port3E & 0x48) == 0x48) // cart and bios disabled, return empty bus
					return 0xFF;
				if (BiosMapped && BiosRom != null)
				{
					// korean BIOS (and a couple of rarer BIOses) use memory slot 2 mechanics as needed
					if (address < 0x8000)
					{
						return BiosRom[address];
					}

					return BiosRom[(Bios_bank * BankSize) + (address & BankSizeMask)];
				}

				if (address < 0x8000)
				{
					return RomData[address & 0x7FFF];
				}

				return RomData[(RomBank2 * BankSize) + (address & BankSizeMask)];
			}

			return SystemRam[address & RamSizeMask];
		}

		private CDLog_MapResults MapMemoryKR(ushort address, bool write)
		{
			if (address < 0x8000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = address & 0x7FFF };
			if (address < 0xC000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = (RomBank2 * BankSize) + (address & BankSizeMask) };
			return new CDLog_MapResults { Type = CDLog_AddrType.MainRAM, Address = address & RamSizeMask };
		}

		private void WriteMemoryKR(ushort address, byte value)
		{
			if (address >= 0xC000)
				SystemRam[address & RamSizeMask] = value;
			else if (address == 0xA000)
				RomBank2 = (byte)(value % RomBanks);

			if ((address == 0xFFFF) && BiosMapped)
			{
				Bios_bank = value;
			}
		}

		private void InitKoreaMapper()
		{
			ReadMemoryMapper = ReadMemoryKR;
			WriteMemoryMapper = WriteMemoryKR;
			MapMemory = MapMemoryKR;
			RomBank0 = 0;
			RomBank1 = 1;
			RomBank2 = 2;
			Bios_bank = 2;
		}

		// ======================================================================
		// MSX mapper & Nemesis mapper
		// ======================================================================

		private byte ReadMemoryMSX(ushort address)
		{
			if (address < 0x4000) return RomData[address & 0x3FFF];
			if (address < 0x6000) return RomData[(RomBank0 * 0x2000) + (address & 0x1FFF)];
			if (address < 0x8000) return RomData[(RomBank1 * 0x2000) + (address & 0x1FFF)];
			if (address < 0xA000) return RomData[(RomBank2 * 0x2000) + (address & 0x1FFF)];
			if (address < 0xC000) return RomData[(RomBank3 * 0x2000) + (address & 0x1FFF)];
			return SystemRam[address & RamSizeMask];
		}

		private CDLog_MapResults MapMemoryMSX(ushort address, bool write)
		{
			if (address < 0x4000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = address & 0x3FFF };
			if (address < 0x6000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = (RomBank0 * 0x2000) + (address & 0x1FFF) };
			if (address < 0x8000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = (RomBank1 * 0x2000) + (address & 0x1FFF) };
			if (address < 0xA000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = (RomBank2 * 0x2000) + (address & 0x1FFF) };
			if (address < 0xC000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = (RomBank3 * 0x2000) + (address & 0x1FFF) };
			return new CDLog_MapResults { Type = CDLog_AddrType.MainRAM, Address = address & RamSizeMask };
		}

		private byte ReadMemoryNemesis(ushort address)
		{
			if (address < 0x2000) return RomData[(15 * 0x2000) + (address & 0x1FFF)];
			if (address < 0x4000) return RomData[address & 0x3FFF];
			if (address < 0x6000) return RomData[(RomBank0 * 0x2000) + (address & 0x1FFF)];
			if (address < 0x8000) return RomData[(RomBank1 * 0x2000) + (address & 0x1FFF)];
			if (address < 0xA000) return RomData[(RomBank2 * 0x2000) + (address & 0x1FFF)];
			if (address < 0xC000) return RomData[(RomBank3 * 0x2000) + (address & 0x1FFF)];
			return SystemRam[address & RamSizeMask];
		}

		private CDLog_MapResults MapMemoryNemesis(ushort address, bool write)
		{
			if (address < 0x2000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = (15 * 0x2000) + (address & 0x1FFF) };
			if (address < 0x4000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = address & 0x3FFF };
			if (address < 0x6000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = (RomBank0 * 0x2000) + (address & 0x1FFF) };
			if (address < 0x8000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = (RomBank1 * 0x2000) + (address & 0x1FFF) };
			if (address < 0xA000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = (RomBank2 * 0x2000) + (address & 0x1FFF) };
			if (address < 0xC000) return new CDLog_MapResults { Type = CDLog_AddrType.ROM, Address = (RomBank3 * 0x2000) + (address & 0x1FFF) };
			return new CDLog_MapResults { Type = CDLog_AddrType.MainRAM, Address = address & RamSizeMask };
		}

		private void WriteMemoryMSX(ushort address, byte value)
		{
			if (address >= 0xC000)
				SystemRam[address & RamSizeMask] = value;

			else if (address == 0)
				RomBank2 = (byte)(value % (RomBanks*2));
			else if (address == 1)
				RomBank3 = (byte)(value % (RomBanks * 2));
			else if (address == 2)
				RomBank0 = (byte)(value % (RomBanks * 2));
			else if (address == 3)
				RomBank1 = (byte)(value % (RomBanks * 2));
		}

		private void InitMSXMapper()
		{
			ReadMemoryMapper = ReadMemoryMSX;
			WriteMemoryMapper = WriteMemoryMSX;
			ReadMemoryMapper = ReadMemoryMSX;
			RomBank0 = 0;
			RomBank1 = 0;
			RomBank2 = 0;
			RomBank3 = 0;
		}

		private void InitNemesisMapper()
		{
			InitMSXMapper();
			ReadMemoryMapper = ReadMemoryNemesis;
			MapMemory = MapMemoryNemesis;
		}
	}
}
