using System;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// the functions on this port are at the point of
	// view of an external device.

	public class SerialPort
	{
        public Func<bool> ReadAtnOut;
        public Func<bool> ReadClockOut;
        public Func<bool> ReadDataOut;

		public SerialPort()
		{
		}

		public void HardReset()
		{
		}

        public bool WriteClockIn()
        {
            return true;
        }

        public bool WriteDataIn()
        {
            return true;
        }
	}
}
