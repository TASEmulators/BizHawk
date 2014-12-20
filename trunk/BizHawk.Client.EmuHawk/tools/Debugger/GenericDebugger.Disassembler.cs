using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericDebugger
	{
		private class DisasmOp
		{
			public DisasmOp(int s, string m)
			{
				Size = s;
				Mnemonic = m;
			}

			public int Size { get; private set; }
			public string Mnemonic { get; private set; }
		}

		private const int ADDR_MAX = 0xFFFF; // TODO: this isn't a constant, calculate it off bus size
		private const int DISASM_LINE_COUNT = 100;

		private readonly List<DisasmOp> DisassemblyLines = new List<DisasmOp>();

		private void UpdateDisassembler()
		{
			// Always show a window's worth of instructions (if possible)
			if (CanDisassemble)
			{
				DisassemblerView.BlazingFast = true;
				Disassemble(DISASM_LINE_COUNT);
				DisassemblerView.ensureVisible(0xFFFF);
				DisassemblerView.ensureVisible(PC);
				DisassemblerView.Refresh();
				DisassemblerView.BlazingFast = false;
			}
		}

		private void Disassemble(int line_count)
		{
			DisassemblyLines.Clear();
			int a = PC;
			for (int i = 0; i < line_count; ++i)
			{
				int advance;
				string line = Disassembler.Disassemble(MemoryDomains.SystemBus, (ushort)a, out advance);
				DisassemblyLines.Add(new DisasmOp(advance, line));
				a += advance;
				if (a > ADDR_MAX)
				{
					break;
				}
			}
		}
	}
}
