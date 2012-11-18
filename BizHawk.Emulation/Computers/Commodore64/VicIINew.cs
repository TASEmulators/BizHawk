using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicIINew : IVideoProvider
	{
		public Memory mem;
		public Region region;
		public ChipSignals signal;

		public VicIINew(ChipSignals newSignal, Region newRegion)
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
				return pipeline.Length * rasterLines;
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
