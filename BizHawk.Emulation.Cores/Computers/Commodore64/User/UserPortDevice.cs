using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.User
{
	public abstract class UserPortDevice
	{
		public Func<bool> ReadCounter1;
		public Func<bool> ReadCounter2;
		public Func<bool> ReadHandshake;
		public Func<bool> ReadSerial1;
		public Func<bool> ReadSerial2;

		public virtual void HardReset()
		{
			// note: this will not disconnect any attached media
		}

		public virtual bool ReadAtn()
		{
			return true;
		}

		public virtual int ReadData()
		{
			return 0xFF;
		}

		public virtual bool ReadFlag2()
		{
			return true;
		}

		public virtual bool ReadPa2()	
		{
			return true;
		}

		public virtual bool ReadReset()
		{
			return true;
		}

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}
	}
}
