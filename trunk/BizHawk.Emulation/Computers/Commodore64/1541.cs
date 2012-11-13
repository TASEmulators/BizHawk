using BizHawk.Emulation.CPUs.M6502;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public class Drive1541
	{
		// the 1541 drive:
		//
		// 2kb ram, mapped 0000-07FF
		// two 6522 VIA chips, mapped at 1800 (communication to C64) and 1C00 (drive mechanics)

		public MOS6502X cpu;
		public Via via0;
		public Via via1;

		public Drive1541()
		{
			HardReset();
		}

		public void HardReset()
		{
			cpu = new MOS6502X();
			via0 = new Via();
			via1 = new Via();
		}
	}
}
