namespace BizHawk.Emulation.Consoles.Sega
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

		byte ReadMemory(ushort address)
		{
			byte ret;
			if (address < 1024)
				ret = RomData[address];
			else if (address < BankSize)
				ret = RomData[(RomBank0 * BankSize) + address];
			else if (address < BankSize * 2)
				ret = RomData[(RomBank1 * BankSize) + (address & BankSizeMask)];
			else if (address < BankSize * 3)
			{
				switch (SaveRamBank)
				{
					case 0: ret = RomData[(RomBank2 * BankSize) + (address & BankSizeMask)]; break;
					case 1: ret = SaveRAM[address & BankSizeMask]; break;
					case 2: ret = SaveRAM[BankSize + (address & BankSizeMask)]; break;
					default:
						ret = SystemRam[address & RamSizeMask];
						break;
				}
			}
			else
			{
				ret = SystemRam[address & RamSizeMask];
			}

			if (CoreInputComm.MemoryCallbackSystem.HasRead)
			{
				CoreInputComm.MemoryCallbackSystem.TriggerRead(address);
			}

			return ret;
		}

		void WriteMemory(ushort address, byte value)
		{
			if (address >= 0xC000)
				SystemRam[address & RamSizeMask] = value;

			else if (address >= 0x8000)
			{
				SaveRamModified = true;
				switch (SaveRamBank)
				{
					case 1: SaveRAM[address & BankSizeMask] = value; return;
					case 2: SaveRAM[BankSize + (address & BankSizeMask)] = value; return;
				}
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

			if (CoreInputComm.MemoryCallbackSystem.HasWrite)
			{
				CoreInputComm.MemoryCallbackSystem.TriggerWrite(address);
			}
		}

		void InitSegaMapper()
		{
			Cpu.ReadMemory = ReadMemory;
			Cpu.WriteMemory = WriteMemory;
			WriteMemory(0xFFFC, 0);
			WriteMemory(0xFFFD, 0);
			WriteMemory(0xFFFE, 1);
			WriteMemory(0xFFFF, 2);
		}

		// Mapper when loading a BIOS as a ROM

		bool BiosMapped { get { return (Port3E & 0x08) == 0; } }

		byte ReadMemoryBIOS(ushort address)
		{
			if (BiosMapped == false && address < BankSize * 3)
				return 0x00;

			if (address < 1024)
				return RomData[address];
			if (address < BankSize)
				return RomData[(RomBank0 * BankSize) + address];
			if (address < BankSize * 2)
				return RomData[(RomBank1 * BankSize) + (address & BankSizeMask)];
			if (address < BankSize * 3)
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
			Cpu.ReadMemory = ReadMemoryBIOS;
			Cpu.WriteMemory = WriteMemoryBIOS;
			WriteMemory(0xFFFC, 0);
			WriteMemory(0xFFFD, 0);
			WriteMemory(0xFFFE, 1);
			WriteMemory(0xFFFF, 2);
		}
	}
}