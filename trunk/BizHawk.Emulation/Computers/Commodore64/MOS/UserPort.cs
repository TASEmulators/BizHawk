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

        virtual public void HardReset()
		{
			// note: this will not disconnect any attached media
		}

        virtual public bool ReadAtn()
        {
            return true;
        }

        virtual public bool ReadCounter1Buffer()
        {
            return true;
        }

        virtual public bool ReadCounter2Buffer()
        {
            return true;
        }

        virtual public byte ReadData()
        {
            return 0xFF;
        }

        virtual public bool ReadFlag2()
        {
            return true;
        }

        virtual public bool ReadPA2()
        {
            return true;
        }

        virtual public bool ReadReset()
        {
            return true;
        }

        virtual public bool ReadSerial1Buffer()
        {
            return true;
        }

        virtual public bool ReadSerial2Buffer()
        {
            return true;
        }

        public void SyncState(Serializer ser)
        {
            SaveState.SyncObject(ser, this);
        }
    }
}
