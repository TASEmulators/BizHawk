using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public partial class DOSBox : ICycleTiming
	{
		// DOSBox emulates the internal core at stable 1ms increments, regardless of the configuration. Therefore, this is basis for time calculation.
		double ICycleTiming.ClockRate => 1000.0;

		// How many ms have elapsed in the previous emulated frame
		// The emulation driver (bizhawk.cpp) follows the configured framerate in 1ms increments. If it ever drifts to accumulate 1ms over, it emulates 1ms less in the next frame.
		// In this way, it keeps a stable timing over the long run
		long ICycleTiming.CycleCount => _libDOSBox.getTicksElapsed();
	}
}
