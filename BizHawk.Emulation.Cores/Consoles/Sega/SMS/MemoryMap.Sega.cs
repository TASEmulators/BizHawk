using System;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS
	{
		// The Sega memory mapper layout looks like so:
		// $0000-$03FF - ROM (unpaged)
		// $0400-$3FFF - ROM mapper slot 0
		// $4000-$7FFF - ROM mapper slot 1
		// $8000-$BFFF - ROM mapper slot 2 - OR - SaveRAM
		// $C000-$DFFF - System RAM
		// $E000-$FFFF - System RAM (mirror)
		// $FFFC - SaveRAM mapper control
		// $FFFD - Mapper slot 0 control
		// $FFFE - Mapper slot 1 control
		// $FFFF - Mapper slot 2 control

		const ushort BankSizeMask = 0x3FFF;
		const ushort RamSizeMask = 0x1FFF;

		bool BiosMapped { get { return (Port3E & 0x40) == 0x40; } }

		byte ReadMemorySega(ushort address)
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
						case 1: if (SaveRAM != null) ret = SaveRAM[(address & BankSizeMask) % SaveRAM.Length]; break;
						case 2: if (SaveRAM != null) ret = SaveRAM[(BankSize + (address & BankSizeMask)) % SaveRAM.Length]; break;
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

		CDLog_MapResults MapMemorySega(ushort address, bool write)
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
						case 1:
							if (SaveRAM != null) return new CDLog_MapResults() { Type = CDLog_AddrType.SaveRAM, Address = (address & BankSizeMask) % SaveRAM.Length };
							else return new CDLog_MapResults();
						case 2:
							if (SaveRAM != null) return new CDLog_MapResults() { Type = CDLog_AddrType.SaveRAM, Address = (BankSize + (address & BankSizeMask)) & BankSizeMask };
							else return new CDLog_MapResults();
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

		void WriteMemorySega(ushort address, byte value)
		{
			if (address >= 0xC000)
				SystemRam[address & RamSizeMask] = value;

			else if (address >= 0x8000) 
			{
				if (SaveRAM != null)
				{
					SaveRamModified = true;
					switch (SaveRamBank)
					{
						case 1: SaveRAM[(address & BankSizeMask) % SaveRAM.Length] = value; return;
						case 2: SaveRAM[(BankSize + (address & BankSizeMask)) % SaveRAM.Length] = value; return;
					}
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

		void InitSegaMapper()
		{
			ReadMemory = ReadMemorySega;
			WriteMemory = WriteMemorySega;
			MapMemory = MapMemorySega;
			WriteMemorySega(0xFFFC, 0);
			WriteMemorySega(0xFFFD, 0);
			WriteMemorySega(0xFFFE, 1);
			WriteMemorySega(0xFFFF, 2);
		}

		// Mapper when loading a BIOS as a ROM (simulating no cart loaded)

		byte ReadMemoryBIOS(ushort address)
		{
			if ((Port3E & 0x08) != 0 && address < 0xC000)
				return 0xFF;

			if (address < 1024)
				return RomData[address];
			if (address < 0x4000)
				return RomData[(RomBank0 * BankSize) + address];
			if (address < 0x8000)
				return RomData[(RomBank1 * BankSize) + (address & BankSizeMask)];
			if (address < 0xC000)
				return RomData[(RomBank2 * BankSize) + (address & BankSizeMask)];

			return SystemRam[address & RamSizeMask];
		}

		void WriteMemoryBIOS(ushort address, byte value)
		{
			if (address >= 0xC000)
				SystemRam[address & RamSizeMask] = value;

			if (address >= 0xFFFC)
			{
				if (address == 0xFFFD) RomBank0 = (byte)(value % RomBanks);
				else if (address == 0xFFFE) RomBank1 = (byte)(value % RomBanks);
				else if (address == 0xFFFF) RomBank2 = (byte)(value % RomBanks);
				return;
			}
		}

		void InitBiosMapper()
		{
			ReadMemory = ReadMemoryBIOS;
			WriteMemory = WriteMemoryBIOS;
			WriteMemorySega(0xFFFC, 0);
			WriteMemorySega(0xFFFD, 0);
			WriteMemorySega(0xFFFE, 1);
			WriteMemorySega(0xFFFF, 2);
		}
	}
}