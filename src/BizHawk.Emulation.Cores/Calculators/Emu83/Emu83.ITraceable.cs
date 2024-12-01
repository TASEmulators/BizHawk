using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Calculators.Emu83
{
	public partial class Emu83
	{
		private ITraceable Tracer { get; }
		private readonly LibEmu83.TraceCallback _traceCallback;

		private void MakeTrace(long _cycleCount)
		{
			int[] regs = new int[12];
			LibEmu83.TI83_GetRegs(Context, regs);
			ushort PC = (ushort)regs[10];

			string disasm = Z80ADisassembler.Disassemble(PC, addr => LibEmu83.TI83_ReadMemory(Context, addr), out int bytes_read);
			string byte_code = null;

			for (ushort i = 0; i < bytes_read; i++)
			{
				byte_code += $"{LibEmu83.TI83_ReadMemory(Context, (ushort)(PC + i)):X2}";
				if (i < (bytes_read - 1))
				{
					byte_code += " ";
				}
			}

			Tracer.Put(new(
				disassembly:
					$"{PC:X4}: {byte_code,-12} {disasm,-26}",
				registerInfo:
					$"AF:{regs[0]:X4} BC:{regs[1]:X4} DE:{regs[2]:X4} HL:{regs[3]:X4} IX:{regs[8]:X4} IY:{regs[9]:X4} SP:{regs[11]:X4} Cy:{_cycleCount}"
			));
		}
	}
}
