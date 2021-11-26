using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition { get; set; }

		public string SystemId => VSystemID.Raw.ChannelF;

		public bool DeterministicEmulation { get; set; }

		private double cpuFreq => region == RegionType.NTSC ? 1.7897725 : 2.000000;
		private double refreshRate => region == RegionType.NTSC ? 60 : 50;

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
			_isLag = true;

			if (_tracer.IsEnabled())
			{
				CPU.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				CPU.TraceCallback = null;
			}

			while (FrameClock++ < ClockPerFrame)
			{
				CPU.ExecuteOne();
			}

			PollInput();

			FrameClock = 0;
			_frame++;

			if (_isLag)
				_lagCount++;

			return true;
		}

		private int _frame;
#pragma warning disable CS0414
		//private int _lagcount;
		//private bool _islag;
#pragma warning restore CS0414

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public int Frame => _frame;

		public void Dispose()
		{
		}
	}
}
