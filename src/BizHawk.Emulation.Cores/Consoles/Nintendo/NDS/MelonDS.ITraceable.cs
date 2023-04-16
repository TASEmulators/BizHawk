using System;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class NDS
	{
		private ITraceable Tracer { get; }
		private readonly LibMelonDS.TraceCallback _tracecb;

		private void MakeTrace(LibMelonDS.CpuTypes _cpu, IntPtr _regs, uint _opcode)
		{
			string cpu = _cpu switch
			{
				LibMelonDS.CpuTypes.ARM9 => "ARM9",
				LibMelonDS.CpuTypes.ARM7 => "ARM7",
				LibMelonDS.CpuTypes.ARM9_THUMB => "ARM9 (Thumb)",
				LibMelonDS.CpuTypes.ARM7_THUMB => "ARM7 (Thumb)",
				_ => throw new InvalidOperationException("Invalid CPU Mode???"),
			};

			int[] regs = new int[16];
			Marshal.Copy(_regs, regs, 0, 16);

			bool isthumb = ((uint)_cpu & 2u) == 2u;
			uint pc = (uint)regs[15] - (isthumb ? 2u : 4u); // handle prefetch

			Tracer.Put(new(
				disassembly: string.Format("{0:x8}", pc).PadRight(12) + _disassembler.Trace(pc, _opcode, isthumb).PadRight(32),
				registerInfo: string.Format(
					"r0:{0:x8} r1:{1:x8} r2:{2:x8} r3:{3:x8} r4:{4:x8} r5:{5:x8} r6:{6:x8} r7:{7:x8} r8:{8:x8} r9:{9:x8} r10:{10:x8} r11:{11:x8} r12:{12:x8} r13:{13:x8} r14:{14:x8} r15:{15:x8} Cy:{16} {17}",
					(uint)regs[0],
					(uint)regs[1],
					(uint)regs[2],
					(uint)regs[3],
					(uint)regs[4],
					(uint)regs[5],
					(uint)regs[6],
					(uint)regs[7],
					(uint)regs[8],
					(uint)regs[9],
					(uint)regs[10],
					(uint)regs[11],
					(uint)regs[12],
					(uint)regs[13],
					(uint)regs[14],
					(uint)regs[15],
					TotalExecutedCycles,
					cpu)));
		}
	}
}
