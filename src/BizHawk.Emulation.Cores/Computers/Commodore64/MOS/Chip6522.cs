using System;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public static class Chip6522
	{
		public static Via Create(Func<int> readPrA, Func<int> readPrB) => new(readPrA, readPrB);

		public static Via Create(Func<bool> readClock, Func<bool> readData, Func<bool> readAtn, int driveNumber) => new(readClock, readData, readAtn, driveNumber);
	}
}
