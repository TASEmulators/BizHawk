using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	// MOS Technologies VIA 6522
	// register count: 16
	// IO port count: 2

	public class ViaRegs
	{
		public int ACR;
		public int IER;
		public int IFR;
		public int PCR;
		public int SR;
		public int[] TC;
		public int[] TL;
	}

	public class Via
	{
		public ViaRegs regs;

		public Via()
		{
			HardReset();
		}

		public void HardReset()
		{
			regs = new ViaRegs();
		}
	}
}
