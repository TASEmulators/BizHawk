using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public class CassettePort
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
