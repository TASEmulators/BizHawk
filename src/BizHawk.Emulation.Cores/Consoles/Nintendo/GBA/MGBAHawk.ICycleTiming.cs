using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : ICycleTiming
	{
		public long CycleCount => TotalExecutedCycles;

		public double ClockRate => 16777216;
	}
}
