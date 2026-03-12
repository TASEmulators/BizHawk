using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : ICycleTiming
	{
		public long CycleCount { get; private set; }

		public double ClockRate => 16777216;
	}
}
