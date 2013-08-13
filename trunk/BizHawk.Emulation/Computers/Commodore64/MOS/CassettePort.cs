using System;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public class CassettePort
	{
        public Func<bool> ReadDataOutput;
        public Func<bool> ReadMotor;

		public void HardReset()
		{
		}

        public bool DataInput
        {
            get
            {
                return true;
            }
        }

        public bool Sense
        {
            get
            {
                return true;
            }
        }
	}
}
