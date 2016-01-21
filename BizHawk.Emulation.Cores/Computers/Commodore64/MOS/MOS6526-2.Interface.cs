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

		public int PortAData
		{
			get
			{
				return portA.ReadOutput();
			}
		}

		public int PortAMask
		{
			get;
			set;
		}

		public int PortADirection
		{
			get
			{
				return portA.Direction;
			}
		}

		public int PortALatch
		{
			get
			{
				return portA.Latch;
			}
		}

		public int PortBData
		{
			get
			{
				return portB.ReadOutput();
			}
		}

		public int PortBDirection
		{
			get
			{
				return portB.Direction;
			}
		}

		public int PortBLatch
		{
			get
			{
				return portB.Latch;
			}
		}

		public int PortBMask
		{
			get;
			set;
		}
	}
}
