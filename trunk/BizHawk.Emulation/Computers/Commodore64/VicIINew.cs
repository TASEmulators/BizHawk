using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public struct VicIIDataGenerator
	{

	}

	public struct VicIISpriteGenerator
	{

	}

	public partial class VicIINew
	{
		public Memory mem;
		public Region region;
		public ChipSignals signal;

		public VicIINew(ChipSignals newSignal, Region newRegion)
		{
			region = newRegion;
			signal = newSignal;
			HardReset();
			InitPipeline(newRegion);
		}

		public void HardReset()
		{
			InitRegs();
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
		}

	}
}
