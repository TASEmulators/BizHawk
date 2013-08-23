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

        public bool ReadDataInputBuffer()
        {
            return true;
        }

        public bool ReadSenseBuffer()
        {
            return true;
        }

        public void SyncState(Serializer ser)
        {
            Sync.SyncObject(ser, this);
        }
	}
}
