using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicII : IVideoProvider
	{
		public Memory mem;
		public Region region;
		public ChipSignals signal;

		public VicII(ChipSignals newSignal, Region newRegion)
		{
			region = newRegion;
			signal = newSignal;
			InitPipeline(newRegion);
			HardReset();
		}

		public int CyclesPerFrame
		{
			get
			{
				return pipelineLength * rasterLines;
			}
		}

		public double FramesPerSecond
		{
			get
			{
				switch (region)
				{
					case Region.NTSC:
						return (14318181d / 14d) / (double)CyclesPerFrame;
					case Region.PAL:
						return (17734472d / 18d) / (double)CyclesPerFrame;
				}
				return 0;
			}
		}

		public void HardReset()
		{
			InitRegs();
			InitVideoBuffer();
			cycle = 0;
		}

		public bool Interrupt
		{
			get
			{
				return IRQ;
			}
		}

		public void PerformCycle()
		{
			ExecutePipeline();
			UpdateInterrupts();
			signal.VicIRQ = IRQ;
		}

	}
}
