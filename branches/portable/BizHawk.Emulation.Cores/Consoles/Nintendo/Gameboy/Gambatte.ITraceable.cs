using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy
	{
		private ITraceable Tracer { get; set; }
		private LibGambatte.TraceCallback tracecb;

		private void MakeTrace(IntPtr _s)
		{
			int[] s = new int[13];
			System.Runtime.InteropServices.Marshal.Copy(_s, s, 0, 13);
			ushort unused;

			Tracer.Put(string.Format(
				"{13} SP:{2:x2} A:{3:x2} B:{4:x2} C:{5:x2} D:{6:x2} E:{7:x2} F:{8:x2} H:{9:x2} L:{10:x2} {11} Cy:{0}",
				s[0],
				s[1] & 0xffff,
				s[2] & 0xffff,
				s[3] & 0xff,
				s[4] & 0xff,
				s[5] & 0xff,
				s[6] & 0xff,
				s[7] & 0xff,
				s[8] & 0xff,
				s[9] & 0xff,
				s[10] & 0xff,
				s[11] != 0 ? "skip" : "",
				s[12] & 0xff,
				Common.Components.Z80GB.NewDisassembler.Disassemble((ushort)s[1], (addr) => LibGambatte.gambatte_cpuread(GambatteState, addr), out unused).PadRight(30)
			));
		}
	}
}
