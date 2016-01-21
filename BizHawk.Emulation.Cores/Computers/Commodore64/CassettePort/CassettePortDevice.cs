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
		Commodore64.CassettePort.Tape tape;

		public void HardReset()
		{
			if (tape != null) tape.Rewind();
		}

		public virtual bool ReadDataInputBuffer()
		{
			return tape == null || ReadMotor() || tape.Read();
		}

		public virtual bool ReadSenseBuffer()
		{
			return tape == null; // Just assume that "play" is constantly pressed as long as a tape is inserted
		}

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}

		internal void Connect(Tape tape)
		{
			this.tape = tape;
		}
	}
}
