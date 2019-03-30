using System;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk
	{
		// Interact with Hardware registers through these read and write methods
		// Typically you will only be able to access different parts of the hardware through their available registers
		// Sending the memory map of these regiesters through here helps keep things organized even though it results in an extra function call
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
			// Registers will start with a default value at power on, use this funciton to set them
		}
	}
}
