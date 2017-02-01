namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS
	{
		//This doesn't look functional. Illogical and nothing like http://www.smspower.org/Articles/TerebiOekaki

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
				if (Controller.IsPressed("P1 Left")) xCoord++;
				if (Controller.IsPressed("P1 Right")) xCoord++;
				if (Controller.IsPressed("P1 Up")) yCoord--;
				if (Controller.IsPressed("P1 Down")) yCoord++;
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
			ReadMemory = ReadMemoryTO;
			WriteMemory = WriteMemoryTO;
		}
	}
}
