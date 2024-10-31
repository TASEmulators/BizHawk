
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class SuperVision : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition { get; }

		public string SystemId => VSystemID.Raw.SuperVision;

		public bool DeterministicEmulation => true;

		private double _cpuClocksPerFrame;
		private double _cpuClocksPerSecond;
		private int _frameClock;
		private int _frame;

		public int FrameClock
		{
			get => _frameClock;
			set => _frameClock = value;
		}	
		public int Frame => _frame;

		private void CalcClock()
		{
			_cpuClocksPerSecond = 4_000_000.0;
			_cpuClocksPerFrame = 
				(40 + 1) *	// pixel clocks per scanline + 1 latch write pixel clock
				6 *         // cpu clocks per pixel
				160 *		// scanlines per field
				2;          // fields per frame

			double refreshRate = _cpuClocksPerSecond / _cpuClocksPerFrame;  // 50.8130081300813

			_asic.Screen.SetRates(
				(int) _cpuClocksPerSecond,
				(int) _cpuClocksPerFrame);
		}

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			_controller = controller;
			_isLag = true;

			if (_tracer.IsEnabled())
			{
				_cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				_cpu.TraceCallback = null;
			}

			PollInput();

			_asic.FrameStart = true;

			while (_frameClock < _cpuClocksPerFrame)
			{
				_asic.Clock();
				_cpu.ExecuteOne();
			}

			_frameClock = 0;
			_frame++;

			if (_isLag)
				_lagCount++;			

			return true;
		}

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public void Dispose()
		{
		}
	}
}
