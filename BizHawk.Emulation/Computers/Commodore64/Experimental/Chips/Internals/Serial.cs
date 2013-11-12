using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Experimental
{
	public class Serial
	{
		public Func<bool> InputATN;
		public Func<bool> InputClock;
		public Func<bool> InputData;
		public Func<bool> InputReset;

		virtual public bool Clock { get { return true; } }
		virtual public bool Data { get { return true; } }
		public bool OutputClock() { return Clock; }
		public bool OutputData() { return Data; }
		public bool OutputSRQ() { return SRQ; }
		virtual public bool SRQ { get { return true; } }
		virtual public void SyncState(Serializer ser) { SaveState.SyncObject(ser, this); }
	}
}
