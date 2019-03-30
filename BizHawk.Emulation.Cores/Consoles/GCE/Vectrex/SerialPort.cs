using System;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	// your core may have several integral peripherals beyond the usual graphics / sound / controller
	// here is one such example
	// Treat it the same way as any other component. you should be able to run it one tick at a time in sync with the 
	// other parts of the core

	public class SerialPort
	{
		public VectrexHawk Core { get; set; }

		public byte ReadReg(int addr)
		{
			return 0xFF;
		}

		public void WriteReg(int addr, byte value)
		{

		}


		public void serial_transfer_tick()
		{

		}

		public void Reset()
		{

		}

		public void SyncState(Serializer ser)
		{

		}
	}
}
