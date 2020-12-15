namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS
	{
		//This doesn't look functional. Illogical and nothing like http://www.smspower.org/Articles/TerebiOekaki

		private byte xCoord = 128;
		private byte yCoord = 100;

		private enum Axis { XAxis, YAxis }

		private Axis axis = Axis.XAxis;


		private byte ReadMemoryTO(ushort address)
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
				if (_controller.IsPressed("P1 Left")) xCoord++;
				if (_controller.IsPressed("P1 Right")) xCoord++;
				if (_controller.IsPressed("P1 Up")) yCoord--;
				if (_controller.IsPressed("P1 Down")) yCoord++;
				return 0;

				//if (!Controller["P1 B1"]) return 0;
				//if (axis == Axis.XAxis) return xCoord;
				//return yCoord;
			}

			return SystemRam[address & RamSizeMask];
		}

		private void WriteMemoryTO(ushort address, byte value)
		{
			if (address >= 0xC000)
				SystemRam[address & RamSizeMask] = value;
			else if (address == 0x6000)
				axis = ((value & 1) == 0) ? Axis.XAxis : Axis.YAxis;
		}

		private void InitTerebiOekaki()
		{
			ReadMemoryMapper = ReadMemoryTO;
			WriteMemoryMapper = WriteMemoryTO;
		}
	}
}
