using System;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	public class UserPort
	{
        public Func<bool> ReadCounter1;
        public Func<bool> ReadCounter2;
        public Func<bool> ReadHandshake;
        public Func<bool> ReadSerial1;
        public Func<bool> ReadSerial2;

		public UserPort()
		{
		}

		public void HardReset()
		{
			// note: this will not disconnect any attached media
		}

        public bool ReadAtn()
        {
            return true;
        }

        public bool ReadCounter1Buffer()
        {
            return true;
        }

        public bool ReadCounter2Buffer()
        {
            return true;
        }

        public byte ReadData()
        {
            return 0xFF;
        }

        public bool ReadFlag2()
        {
            return true;
        }

        public bool ReadPA2()
        {
            return true;
        }

        public bool ReadReset()
        {
            return true;
        }

        public bool ReadSerial1Buffer()
        {
            return true;
        }

        public bool ReadSerial2Buffer()
        {
            return true;
        }

        public void SyncState(Serializer ser)
        {
            Sync.SyncObject(ser, this);
        }
    }
}
