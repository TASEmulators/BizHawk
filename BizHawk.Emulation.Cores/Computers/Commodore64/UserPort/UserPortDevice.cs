using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.UserPort
{
	public class UserPortDevice
	{
		public Func<bool> ReadCounter1;
		public Func<bool> ReadCounter2;
		public Func<bool> ReadHandshake;
		public Func<bool> ReadSerial1;
		public Func<bool> ReadSerial2;

		public UserPortDevice()
		{
		}

		public virtual void HardReset()
		{
			// note: this will not disconnect any attached media
		}

		public virtual bool ReadAtn()
		{
			return true;
		}

		public virtual bool ReadCounter1Buffer()
		{
			return true;
		}

		public virtual bool ReadCounter2Buffer()
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

		public virtual bool ReadPA2()	
		{
			return true;
		}

		public virtual bool ReadReset()
		{
			return true;
		}

		public virtual bool ReadSerial1Buffer()
		{
			return true;
		}

		public virtual bool ReadSerial2Buffer()
		{
			return true;
		}

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}
	}
}
