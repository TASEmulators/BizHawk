using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition { get; set; }

		public string SystemId => "ChannelF";

		public bool DeterministicEmulation { get; set; }

		private static double cpuFreq = 1.7897725;
		private static double refreshRate = 60;

		public int ClockPerFrame;
		public int FrameClock = 0;

		private void CalcClock()
		{
			var c = ((cpuFreq * 1000000) / refreshRate);
			ClockPerFrame = (int) c;

			SetupAudio();
		}

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			_controller = controller;
			_isLag = false;

			if (_tracer.Enabled)
			{
				CPU.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				CPU.TraceCallback = null;
			}

			_isLag = PollInput();

			while (FrameClock++ < ClockPerFrame)
			{
				CPU.ExecuteOne();
			}

			FrameClock = 0;
			_frame++;
			return true;
		}

		private int _frame;
		private int _lagcount;
		private bool _islag;

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		public int Frame => _frame;

		public CoreComm CoreComm { get; }

		public void Dispose()
		{

		}
	}
}
