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

		public virtual bool ReadAtn() => true;

		public virtual int ReadData() => 0xFF;

		public virtual bool ReadFlag2() => true;

		public virtual bool ReadPa2() => true;

		public virtual bool ReadReset() => true;

		public abstract void SyncState(Serializer ser);
	}
}
