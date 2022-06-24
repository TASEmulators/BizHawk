using System;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class NDS
	{
		private ITraceable Tracer { get; }
		private readonly LibMelonDS.TraceCallback _tracecb;

		private unsafe void MakeTrace(LibMelonDS.TraceMask type, IntPtr r, IntPtr disasm, uint cyclesOff)
		{
			string cpu = type switch
			{	
				LibMelonDS.TraceMask.ARM7_THUMB => "ARM7 (Thumb)",
				LibMelonDS.TraceMask.ARM7_ARM => "ARM7",
				LibMelonDS.TraceMask.ARM9_THUMB => "ARM9 (Thumb)",
				LibMelonDS.TraceMask.ARM9_ARM => "ARM9",
				_ => throw new InvalidOperationException("Invalid CPU Mode???"),
			};

			uint* regs = (uint*)r;

			bool isthumb = type is LibMelonDS.TraceMask.ARM7_THUMB or LibMelonDS.TraceMask.ARM9_THUMB;
			uint pc = regs[15] - (isthumb ? 2u : 4u); // handle prefetch

			Tracer.Put(new(
				disassembly: string.Format("{0:x8}", pc).PadRight(12) + Marshal.PtrToStringAnsi(disasm).PadRight(64),
				registerInfo: string.Format(
					"r0:{0:x8} r1:{1:x8} r2:{2:x8} r3:{3:x8} r4:{4:x8} r5:{5:x8} r6:{6:x8} r7:{7:x8} r8:{8:x8} r9:{9:x8} r10:{10:x8} r11:{11:x8} r12:{12:x8} r13:{13:x8} r14:{14:x8} r15:{15:x8} Cy:{16} {17}",
					regs[0],
					regs[1],
					regs[2],
					regs[3],
					regs[4],
					regs[5],
					regs[6],
					regs[7],
					regs[8],
					regs[9],
					regs[10],
					regs[11],
					regs[12],
					regs[13],
					regs[14],
					regs[15],
					CycleCount + cyclesOff,
					cpu)));
		}
	}
}
