using System;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class NDS
	{
		private ITraceable Tracer { get; }
		private LibMelonDS.TraceCallback _tracecb;

		private void MakeTrace(LibMelonDS.CpuTypes _cpu, IntPtr _regs, uint _opcode, long _ccoffset)
		{
			string cpu;
			switch (_cpu)
			{
				case LibMelonDS.CpuTypes.ARM9:
					cpu = "ARM9";
					break;
				case LibMelonDS.CpuTypes.ARM7:
					cpu = "ARM7";
					break;
				case LibMelonDS.CpuTypes.ARM9_THUMB:
					cpu = "ARM9 (Thumb)";
					break;
				case LibMelonDS.CpuTypes.ARM7_THUMB:
					cpu = "ARM7 (Thumb)";
					break;
				default:
					throw new InvalidOperationException("Invalid CPU Mode???");
			}

			int[] regs = new int[16];
			Marshal.Copy(_regs, regs, 0, 16);

			Tracer.Put(new(
				disassembly: _disassembler.Trace(
					(uint)regs[16],
					_opcode,
					((uint)_cpu & 2u) == 2).PadRight(36),
				registerInfo: string.Format(
					"r0:{0} r1:{1} r2:{2} r3:{3} r4:{4} r5:{5} r6:{6} r7:{7} r8:{8} r9:{9} r10:{10} r11:{11} r12:{12} r13:{13} r14:{14} r15:{15} Cy:{16} {17}",
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
					CycleCount + _ccoffset,
					cpu)));
		}

	}
}
