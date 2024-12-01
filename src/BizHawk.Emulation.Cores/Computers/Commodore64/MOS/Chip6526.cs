namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// MOS technology 6526 "CIA"
	//
	// emulation notes:
	// * CS, R/W and RS# pins are not emulated. (not needed)
	// * A low RES pin is emulated via HardReset().
	public static class Chip6526
	{
		public static Cia CreateCia1(C64.CiaType type, Func<int> readIec, Func<int> readUserPort)
		{
			switch (type)
			{
				case C64.CiaType.Ntsc:
					return new Cia(14318181, 14 * 60, readIec, readUserPort)
					{
						DelayedInterrupts = true
					};
				case C64.CiaType.NtscRevA:
					return new Cia(14318181, 14 * 60, readIec, readUserPort)
					{
						DelayedInterrupts = false
					};
				case C64.CiaType.Pal:
					return new Cia(17734472, 18 * 50, readIec, readUserPort)
					{
						DelayedInterrupts = true
					};
				case C64.CiaType.PalRevA:
					return new Cia(17734472, 18 * 50, readIec, readUserPort)
					{
						DelayedInterrupts = false
					};
				default:
					throw new Exception("Unrecognized CIA timer type.");
			}
		}

		public static Cia CreateCia0(C64.CiaType type, Func<bool[]> keyboard, Func<bool[]> joysticks)
		{
			switch (type)
			{
				case C64.CiaType.Ntsc:
					return new Cia(14318181, 14 * 60, keyboard, joysticks)
					{
						DelayedInterrupts = true
					};
				case C64.CiaType.NtscRevA:
					return new Cia(14318181, 14 * 60, keyboard, joysticks)
					{
						DelayedInterrupts = false
					};
				case C64.CiaType.Pal:
					return new Cia(17734472, 18 * 50, keyboard, joysticks)
					{
						DelayedInterrupts = true
					};
				case C64.CiaType.PalRevA:
					return new Cia(17734472, 18 * 50, keyboard, joysticks)
					{
						DelayedInterrupts = false
					};
				default:
					throw new Exception("Unrecognized CIA timer type.");
			}
		}
	}
}
