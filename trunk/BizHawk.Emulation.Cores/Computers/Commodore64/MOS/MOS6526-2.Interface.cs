using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	sealed public partial class MOS6526_2
	{
		public void ExecutePhase1()
		{
			proc_a();
			proc_b();
			if (--tod_cycles <= 0)
			{
				//tod_cycles += tod_period;
				tod();
			}
		}

		public void ExecutePhase2()
		{
		}

		public void HardReset()
		{
			reset();
		}

		public byte PortAData
		{
			get
			{
				return portA.ReadOutput();
			}
		}

		public byte PortAMask
		{
			get;
			set;
		}

		public byte PortADirection
		{
			get
			{
				return portA.Direction;
			}
		}

		public byte PortALatch
		{
			get
			{
				return portA.Latch;
			}
		}

		public byte PortBData
		{
			get
			{
				return portB.ReadOutput();
			}
		}

		public byte PortBDirection
		{
			get
			{
				return portB.Direction;
			}
		}

		public byte PortBLatch
		{
			get
			{
				return portB.Latch;
			}
		}

		public byte PortBMask
		{
			get;
			set;
		}
	}
}
