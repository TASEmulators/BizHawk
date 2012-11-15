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

		public ViaRegs()
		{
			// power on state
		}

		public byte this[int addr]
		{
			get
			{
				return 0xFF;
			}
			set
			{
				// set register
			}
		}
	}

	// 0x0: port B
	// 0x1: port A
	// 0x2: port B data direction
	// 0x3: port A data direction
	// 0x4: timer lo
	// 0x5: timer hi
	// 0x6: timer latch lo
	// 0x7: timer latch hi
	// 0x8: unused
	// 0x9: unused
	// 0xA: unused
	// 0xB: timer control
	// 0xC: auxilary control
	// 0xD: interrupt status
	// 0xE: interrupt control
	// 0xF: unused

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
