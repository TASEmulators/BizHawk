using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	partial class QuickNES
	{
		public TraceBuffer Tracer { get; private set; }

		private LibQuickNES.TraceCallback _tracecb;

		private void MakeTrace(IntPtr data)
		{
			int[] s = new int[7];
			System.Runtime.InteropServices.Marshal.Copy(data, s, 0, 7);

			byte a = (byte)s[0];
			byte x = (byte)s[1];
			byte y = (byte)s[2];
			ushort sp = (ushort)s[3];
			ushort pc = (ushort)s[4];
			byte p = (byte)s[5];

			byte opcode = (byte)s[6];

			Tracer.Put(string.Format("{0:X2} {1:X2} {2:X2} {3:X4} {4:X4} {5:X2} {6:X2}",
				a, x, y, sp, pc, p, opcode));

		}

		private const string TraceHeader = "_A _X _Y _SP_ _PC_ _P OP";

		private void ConnectTracer()
		{
			Tracer = new TraceBuffer
				{
					Header = TraceHeader
				};
			(ServiceProvider as BasicServiceProvider).Register<ITraceable>(Tracer);
			_tracecb = new LibQuickNES.TraceCallback(MakeTrace);
		}
	}
}
