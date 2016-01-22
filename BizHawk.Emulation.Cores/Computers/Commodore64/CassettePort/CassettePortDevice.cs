using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.Media;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.CassettePort
{
	public class CassettePortDevice
	{
		public Func<bool> ReadDataOutput;
		public Func<bool> ReadMotor;
	    private Tape _tape;

		public void HardReset()
		{
			if (_tape != null) _tape.Rewind();
		}

		public virtual bool ReadDataInputBuffer()
		{
			return _tape == null || ReadMotor() || _tape.Read();
		}

		public virtual bool ReadSenseBuffer()
		{
			return _tape == null; // Just assume that "play" is constantly pressed as long as a tape is inserted
		}

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}

		internal void Connect(Tape tape)
		{
			_tape = tape;
		}
	}
}
