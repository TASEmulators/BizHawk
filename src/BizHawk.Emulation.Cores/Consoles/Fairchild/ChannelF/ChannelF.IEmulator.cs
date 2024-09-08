using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition { get; }

		public string SystemId => VSystemID.Raw.ChannelF;

		public bool DeterministicEmulation => true;

		private int _cpuClocksPerFrame;
		private int _frameClock;

		private void CalcClock()
		{
			// CPU speeds from https://en.wikipedia.org/wiki/Fairchild_Channel_F
			// also https://github.com/mamedev/mame/blob/c8192c898ce7f68c0c0b87e44199f0d3e710439b/src/mame/drivers/channelf.cpp
			double cpuFreq, pixelClock;
			if (Region == DisplayType.NTSC)
			{
				HTotal = 256;
				HBlankOff = 8;
				HBlankOn = 212;
				VTotal = 264;
				VBlankOff = 16;
				VBlankOn = 248;
				ScanlineRepeats = 4;
				PixelWidth = 2;

				// NTSC CPU speed is NTSC Colorburst / 2
				const double NTSC_COLORBURST = 4500000 * 227.5 / 286;
				cpuFreq = NTSC_COLORBURST / 2;
				// NTSC pixel clock is NTSC Colorburst * 8 / 7
				pixelClock = NTSC_COLORBURST * 8 / 7;
				// NTSC refresh rate is (pixelclock * 8 / 7) / (HTotal * VTotal)
				// (aka (1023750000 * 8) / (256 * 264 * 286 * 7)
				// reduced to 234375 / 3872
				VsyncNumerator = 234375;
				VsyncDenominator = 3872;
			}
			else
			{
				HTotal = 256;
				HBlankOff = 8;
				HBlankOn = 212;
				VTotal = 312;
				VBlankOff = 20;
				VBlankOn = 310;
				ScanlineRepeats = 5;
				PixelWidth = 2;

				if (_version == ConsoleVersion.ChannelF)
				{
					// PAL CPU speed is 2MHz
					cpuFreq = 2000000;
					// PAL pixel clock is 4MHz
					pixelClock = 4000000;
					// PAL refresh rate is pixelclock / (HTotal * VTotal)
					// reduced to 15625 / 312
					VsyncNumerator = 15625;
					VsyncDenominator = 312;
				}
				else if (_version == ConsoleVersion.ChannelF_II)
				{
					// PAL CPU speed for gen 2 seems to be contested
					// various sources seem to say 1.77MHz (i.e. PAL Colorburst * 2 / 5)
					// wikipedia used to have such, before changing it to 1.97MHz (i.e. PAL Colorburst * 4 / 9)
					// if we assume the pixel clock is double the cpu freq, then 1.97MHz makes more sense (49.3Hz)
					// 1.77MHz * 2 would result in 44.3Hz, complete nonsense for PAL
					// however, this kind of relationship between the CPU freq and pixel clock is necessarily the case (see NTSC)
					// wikipedia's numbers seem to come from a mame contributor (e5frog) editing the page anyways, so it's probably trustworthy
					const double PAL_COLORBURST = 15625 * 283.75 + 25;
					cpuFreq = PAL_COLORBURST * 4 / 9;
					// not entirely sure what the pixel clock for PAL is here
					// presumingly, it's just cpuFreq * 2?
					pixelClock = PAL_COLORBURST * 8 / 9;
					// PAL refresh rate is pixelclock / (HTotal * VTotal)
					// (aka (4433618.75 * 8) / (256 * 312 * 9)
					// reduced to 17734475 / 359424
					VsyncNumerator = 17734475;
					VsyncDenominator = 359424;
				}
				else
				{
					throw new InvalidOperationException();
				}
			}

			PixelClocksPerCpuClock = pixelClock / cpuFreq;
			PixelClocksPerFrame = HTotal * VTotal;

			var c = cpuFreq * PixelClocksPerFrame / pixelClock;
			// note: this always results in a nice integer, no precision is lost!
			_cpuClocksPerFrame = (int)c;

			SetupAudio();
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

			while (_frameClock++ < _cpuClocksPerFrame)
			{
				_cpu.ExecuteOne();
				ClockVideo();
			}

			_frameClock = 0;
			_frame++;

			if (_isLag)
				_lagCount++;

			return true;
		}

		private int _frame;

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

		private void ConsoleReset()
		{
			_cpu.Reset();
			_cartridge.Reset();
		}
	}
}
