using System;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public static class Chip6522
	{
		public static Via Create(Func<int> readPrA, Func<int> readPrB) => new Via(readPrA, readPrB);

		public static Via Create(Func<bool> readClock, Func<bool> readData, Func<bool> readAtn, int driveNumber) => new Via(readClock, readData, readAtn, driveNumber);
	}
}
