using System;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.Components.Z80GB;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy
	{
		private ITraceable Tracer { get; set; }
		private LibGambatte.TraceCallback tracecb;

		private void MakeTrace(IntPtr _s)
		{
			int[] s = new int[14];
			System.Runtime.InteropServices.Marshal.Copy(_s, s, 0, 14);
			ushort unused;

			Tracer.Put(new TraceInfo
			{
				Disassembly =
					NewDisassembler
						.Disassemble((ushort)s[1], (addr) => LibGambatte.gambatte_cpuread(GambatteState, addr), out unused)
						.PadRight(36),
				RegisterInfo =
					string.Format(
					"A:{3:x2} B:{4:x2} C:{5:x2} D:{6:x2} E:{7:x2} F:{8:x2} H:{9:x2} L:{10:x2} LY:{13:x2} SP:{2:x2} {11} Cy:{0}",
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
					s[13] & 0xff
				)
			});
		}
	}
}
