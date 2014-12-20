using System;
using System.Collections.Generic;
using System.Drawing;
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

		//private const int ADDR_MAX = 0xFFFF; // TODO: this isn't a constant, calculate it off bus size

		private int BusMaxValue
		{
			get
			{
				return MemoryDomains.SystemBus.Size;
			}
		}

		private const int DISASM_LINE_COUNT = 100;

		private readonly List<DisasmOp> DisassemblyLines = new List<DisasmOp>();

		private void UpdateDisassembler()
		{
			// Always show a window's worth of instructions (if possible)
			if (CanDisassemble)
			{
				DisassemblerView.BlazingFast = true;
				Disassemble(DISASM_LINE_COUNT);
				DisassemblerView.ensureVisible(BusMaxValue);
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
				if (a > BusMaxValue)
				{
					break;
				}
			}
		}

		private void DisassemblerView_QueryItemText(int index, int column, out string text)
		{
			text = "";
			if (column == 0)
			{
				if (PC <= index && index < PC + DisassemblyLines.Count)
				{
					int a = PC;
					for (int i = 0; i < index - PC; ++i)
					{
						a += DisassemblyLines[i].Size;
					}

					text = string.Format("{0:X4}", a);
				}
			}
			else if (column == 1)
			{
				if (PC <= index && index < PC + DisassemblyLines.Count)
				{
					text = DisassemblyLines[index - PC].Mnemonic;
				}
			}
		}

		private void DisassemblerView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (index == PC)
			{
				color = Color.LightCyan;
			}
		}
	}
}
