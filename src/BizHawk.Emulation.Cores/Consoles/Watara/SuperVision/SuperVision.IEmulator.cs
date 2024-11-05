
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class SuperVision : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition { get; }

		public string SystemId => VSystemID.Raw.SuperVision;

		public bool DeterministicEmulation => true;

		/// <summary>
		/// CPU frequency
		/// </summary>
		public double CpuFreq;

		/// <summary>
		/// Total number of CPU cycles in a frame
		/// </summary>
		public double CpuTicksPerFrame;

		/// <summary>
		/// Number of frames per second
		/// </summary>
		public double RefreshRate;

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
			CpuFreq = 4_000_000.0;
			CpuTicksPerFrame = 
				(40 + 1) *	// pixel clocks per scanline + 1 latch write pixel clock
				6 *         // cpu clocks per pixel
				160 *		// scanlines per field
				2;          // fields per frame

			RefreshRate = CpuFreq / CpuTicksPerFrame;  // 50.8130081300813

			_asic.Screen.SetRates(
				(int) CpuFreq,
				(int) CpuTicksPerFrame);

			_asic.InitAudio();
		}

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			_controller = controller;
			_isLag = true;

			/*
			if (_tracer.IsEnabled())
			{
				_cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				_cpu.TraceCallback = null;
			}
			*/

			PollInput();

			//_asic.FrameStart = true;

			while (_frameClock < CpuTicksPerFrame)
			{
				int ticks = _cpu.ExecuteInstruction();
				_asic.Clock(ticks);
				//_cpu.ExecuteOne();
			}

			_frameClock %= (int)CpuTicksPerFrame;
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
