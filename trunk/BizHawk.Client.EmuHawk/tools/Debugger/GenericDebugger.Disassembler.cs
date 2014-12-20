using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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
				currentDisassemblerAddress = PC;
				Disassemble(DISASM_LINE_COUNT);
				DisassemblerView.ensureVisible(BusMaxValue);
				DisassemblerView.ensureVisible((int)PC);
				DisassemblerView.Refresh();
				DisassemblerView.BlazingFast = false;
			}
		}

		uint currentDisassemblerAddress = 0;

		private void Disassemble(int line_count)
		{
			DisassemblyLines.Clear();
			uint a = currentDisassemblerAddress;
			for (int i = 0; i < line_count; ++i)
			{
				int advance;
				string line = Disassembler.Disassemble(MemoryDomains.SystemBus, (ushort)a, out advance);
				DisassemblyLines.Add(new DisasmOp(advance, line));
				a += (uint)advance;
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
				//if (PC <= index && index < PC + DisassemblyLines.Count)
				if (currentDisassemblerAddress <= index && index < currentDisassemblerAddress + DisassemblyLines.Count)
				{
					int a = (int)currentDisassemblerAddress;
					for (int i = 0; i < index - currentDisassemblerAddress; ++i)
					{
						a += DisassemblyLines[i].Size;
					}

					text = string.Format("{0:X4}", a);
				}
			}
			else if (column == 1)
			{
				//if (PC <= index && index < PC + DisassemblyLines.Count)
				if (currentDisassemblerAddress <= index && index < currentDisassemblerAddress + DisassemblyLines.Count)
				{
					text = DisassemblyLines[index - (int)currentDisassemblerAddress].Mnemonic;
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

		private void DecrementCurrentAddress()
		{
			uint newaddress = currentDisassemblerAddress;
			while (true)
			{
				int bytestoadvance;
				Disassembler.Disassemble(MemoryDomains.SystemBus, newaddress, out bytestoadvance);
				if (newaddress + bytestoadvance == currentDisassemblerAddress)
				{
					break;
				}
				newaddress--;

				// Just in case
				if (currentDisassemblerAddress - newaddress > 5)
				{
					newaddress = currentDisassemblerAddress - 1;
					break;
				}
			}

			currentDisassemblerAddress = newaddress;
		}

		private void IncrementCurrentAddress()
		{
			currentDisassemblerAddress++;
		}

		private void DisassemblerView_Scroll(object sender, ScrollEventArgs e)
		{
			if (e.Type == ScrollEventType.SmallIncrement)
			{
				IncrementCurrentAddress();
				Disassemble(DISASM_LINE_COUNT);
			}

			if (e.Type == ScrollEventType.SmallDecrement)
			{
				DecrementCurrentAddress();
				Disassemble(DISASM_LINE_COUNT);
			}

				//int oldv = e.OldValue;
				//int newv = e.NewValue;
				//int diff = oldv - newv;
				
				//if (e.OldValue > e.NewValue) // Scrolled Up
				//{
				//	Disassemble(DISASM_LINE_COUNT, PC - (e.OldValue - e.NewValue));
				//}
				//else if (e.OldValue < e.NewValue) // Scrolled Down
				//{
				//	Disassemble(DISASM_LINE_COUNT, PC - (e.OldValue - e.NewValue));
				//}
			//}
		}
	}
}
