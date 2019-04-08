using System;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk
	{
		public byte Read_Registers(int addr)
		{
			byte ret = 0;

			switch (addr)
			{
				default:
					break;
			}
			return ret;
		}

		public void Write_Registers(int addr, byte value)
		{
			switch (addr)
			{
				default:
					break;
			}
		}

		public void Register_Reset()
		{

		}
	}
}
