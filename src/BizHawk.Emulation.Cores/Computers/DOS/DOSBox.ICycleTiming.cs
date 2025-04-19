using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public partial class DOSBox : ICycleTiming
	{
		// DOSBox emulates the internal core at stable 1ms increments, regardless of the configuration. Therefore, this is basis for time calculation.
		public double ClockRate => 1000.0;
	}
}
