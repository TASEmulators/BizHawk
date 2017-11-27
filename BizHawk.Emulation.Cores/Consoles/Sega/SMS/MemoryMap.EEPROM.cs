using System;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS
	{
		// The 93c46-connected mapper is assumed to be equivalent to the Sega mapper except for $8000-..
		// The Sega memory mapper layout looks like so:
		// $0000-$03FF - ROM (unpaged)
		// $0400-$3FFF - ROM mapper slot 0
		// $4000-$7FFF - ROM mapper slot 1
		// $8000-$BFFF - ROM mapper slot 2 - OR - EEPROM
		// $C000-$DFFF - System RAM
		// $E000-$FFFF - System RAM (mirror)
		// $FFFC - SaveRAM mapper control
		// $FFFD - Mapper slot 0 control
		// $FFFE - Mapper slot 1 control
		// $FFFF - Mapper slot 2 control

		EEPROM93c46 EEPROM;

		byte ReadMemoryEEPROM(ushort address)
		{
			byte ret = 0xFF;

			if (address < 0xC000)
			{
				if ((Port3E & 0x48) == 0x48) // cart and bios disabled, return empty bus
					ret = 0xFF;
				else if (BiosMapped && BiosRom != null)
					ret = BiosRom[address & 0x1FFF];
				else if (address < 1024)
					ret = RomData[address];
				else if (address < 0x4000)
					ret = RomData[(RomBank0 * BankSize) + address];
				else if (address < 0x8000)
					ret = RomData[(RomBank1 * BankSize) + (address & BankSizeMask)];
				else
				{
					switch (SaveRamBank)
					{
						case 0: ret = RomData[(RomBank2 * BankSize) + (address & BankSizeMask)]; break;
						case 1: if (SaveRAM != null && EEPROM != null) ret = EEPROM.Read(SaveRAM); break;
						default:
							ret = SystemRam[address & RamSizeMask];
							break;
					}
				}
			}
			else
			{
				ret = SystemRam[address & RamSizeMask];
			}

			return ret;
		}

		CDLog_MapResults MapMemoryEEPROM(ushort address, bool write)
		{
			if (address < 0xC000)
			{
				if ((Port3E & 0x48) == 0x48) // cart and bios disabled, return empty bus
					return new CDLog_MapResults();
				else if (BiosMapped && BiosRom != null)
					return new CDLog_MapResults(); //bios tracking of CDL is not supported
				else if (address < 1024)
					return new CDLog_MapResults() { Type = CDLog_AddrType.ROM, Address = address };
				else if (address < 0x4000)
					return new CDLog_MapResults() { Type = CDLog_AddrType.ROM, Address = (RomBank0 * BankSize) + address };
				else if (address < 0x8000)
					return new CDLog_MapResults() { Type = CDLog_AddrType.ROM, Address = (RomBank1 * BankSize) + (address & BankSizeMask) };
				else
				{
					switch (SaveRamBank)
					{
						case 0: return new CDLog_MapResults() { Type = CDLog_AddrType.ROM, Address = (RomBank2 * BankSize) + (address & BankSizeMask) };
						case 1: return new CDLog_MapResults(); // a serial IO port
						case 2: return new CDLog_MapResults(); // a serial IO port
						default:
							return new CDLog_MapResults() { Type = CDLog_AddrType.MainRAM, Address = address & RamSizeMask };
					}
				}
			}
			else
			{
				return new CDLog_MapResults() { Type = CDLog_AddrType.MainRAM, Address = address & RamSizeMask };
			}
		}

		void WriteMemoryEEPROM(ushort address, byte value)
		{
			if (address >= 0xC000)
				SystemRam[address & RamSizeMask] = value;

			else if (address >= 0x8000)
			{
				if (SaveRAM != null)
				{
					SaveRamModified = true;
					EEPROM.Write(value, SaveRAM);
					return;
				}
				else System.Console.WriteLine("Game attempt to use SRAM but SRAM not present");
			}

			if (address >= 0xFFFC)
			{
				if (address == 0xFFFC)
				{
					if ((value & 8) != 0)
						SaveRamBank = (byte)((value & 4) == 0 ? 1 : 2); // SaveRAM selected
					else
						SaveRamBank = 0; // ROM bank selected

				}
				else if (address == 0xFFFD) RomBank0 = (byte)(value % RomBanks);
				else if (address == 0xFFFE) RomBank1 = (byte)(value % RomBanks);
				else if (address == 0xFFFF) RomBank2 = (byte)(value % RomBanks);
				return;
			}
		}

		void InitEEPROMMapper()
		{
			ReadMemoryMapper = ReadMemoryEEPROM;
			WriteMemoryMapper = WriteMemoryEEPROM;
			MapMemory = MapMemoryEEPROM;
			WriteMemoryEEPROM(0xFFFC, 0);
			WriteMemoryEEPROM(0xFFFD, 0);
			WriteMemoryEEPROM(0xFFFE, 1);
			WriteMemoryEEPROM(0xFFFF, 2);

			EEPROM = new EEPROM93c46();
		}
	}
}