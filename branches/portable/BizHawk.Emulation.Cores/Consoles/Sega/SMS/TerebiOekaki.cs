namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS
	{
		// The CodeMasters mapper has 3 banks of 16kb, like the Sega mapper.
		// The differences are that the paging control addresses are different, and the first 1K of ROM is not protected.
		// Bank 0: Control Address $0000 - Maps $0000 - $3FFF
		// Bank 1: Control Address $4000 - Maps $4000 - $7FFF
		// Bank 2: Control Address $8000 - Maps $8000 - $BFFF
		// System RAM is at $C000+ as in the Sega mapper.

		byte xCoord = 128;
		byte yCoord = 100;

		enum Axis { XAxis, YAxis };
		Axis axis = Axis.XAxis;


		byte ReadMemoryTO(ushort address)
		{
			if (address < 0x8000) return RomData[address & 0x1FFF];
			if (address == 0x8000)
			{
				// return press status
				return 0;
				//return (byte)(Controller["P1 B1"] ? 1 : 0);
			}
			if (address == 0xA000)
			{
				if (Controller["P1 Left"]) xCoord++;
				if (Controller["P1 Right"]) xCoord++;
				if (Controller["P1 Up"]) yCoord--;
				if (Controller["P1 Down"]) yCoord++;
				return 0;

				//if (!Controller["P1 B1"]) return 0;
				//if (axis == Axis.XAxis) return xCoord;
				//return yCoord;
			}

			return SystemRam[address & RamSizeMask];
		}

		void WriteMemoryTO(ushort address, byte value)
		{
			if (address >= 0xC000)
				SystemRam[address & RamSizeMask] = value;
			else if (address == 0x6000)
				axis = ((value & 1) == 0) ? Axis.XAxis : Axis.YAxis;
		}

		void InitTerebiOekaki()
		{
			Cpu.ReadMemory = ReadMemoryTO;
			Cpu.WriteMemory = WriteMemoryTO;
		}
	}
}
