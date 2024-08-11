using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public partial class NDS
	{
		private ITraceable Tracer { get; }
		private readonly LibMelonDS.TraceCallback _traceCallback;

		private unsafe void MakeTrace(LibMelonDS.TraceMask type, uint opcode, IntPtr r, IntPtr disasm, uint cyclesOff)
		{
			// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
			var cpu = type switch
			{
				LibMelonDS.TraceMask.ARM7_THUMB => "ARM7 (Thumb)",
				LibMelonDS.TraceMask.ARM7_ARM => "ARM7",
				LibMelonDS.TraceMask.ARM9_THUMB => "ARM9 (Thumb)",
				LibMelonDS.TraceMask.ARM9_ARM => "ARM9",
				_ => throw new InvalidOperationException("Invalid CPU Mode???"),
			};

			var regs = (uint*)r;

			var isthumb = type is LibMelonDS.TraceMask.ARM7_THUMB or LibMelonDS.TraceMask.ARM9_THUMB;
			var opaddr = regs![15] - (isthumb ? 4u : 8u); // handle prefetch

			Tracer.Put(new(
				disassembly: $"{opaddr:x8}:  {opcode:x8} ".PadRight(12) + Marshal.PtrToStringAnsi(disasm)!.PadRight(36),
				registerInfo: $"r0:{regs[0]:x8} r1:{regs[1]:x8} r2:{regs[2]:x8} r3:{regs[3]:x8} r4:{regs[4]:x8} r5:{regs[5]:x8} " +
				$"r6:{regs[6]:x8} r7:{regs[7]:x8} r8:{regs[8]:x8} r9:{regs[9]:x8} r10:{regs[10]:x8} r11:{regs[11]:x8} " +
				$"r12:{regs[12]:x8} SP:{regs[13]:x8} LR:{regs[14]:x8} PC:{regs[15]:x8} Cy:{CycleCount + cyclesOff} {cpu}"));
		}
	}
}
