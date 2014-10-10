using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.CassettePort
{
	public class CassettePortDevice
	{
		public Func<bool> ReadDataOutput;
		public Func<bool> ReadMotor;

		public void HardReset()
		{
		}

		virtual public bool ReadDataInputBuffer()
		{
			return true;
		}

		virtual public bool ReadSenseBuffer()
		{
			return true;
		}

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}
	}
}
