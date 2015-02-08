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
		private readonly List<DisasmOp> DisassemblyLines = new List<DisasmOp>();

		private class DisasmOp
		{
			public DisasmOp(uint address, int s, string m)
			{
				Address = address;
				Size = s;
				Mnemonic = m;
			}

			public uint Address { get; private set; }
			public int Size { get; private set; }
			public string Mnemonic { get; private set; }
		}

		private long BusMaxValue
		{
			get
			{
				return MemoryDomains.SystemBus.Size;
			}
		}

		private void UpdateDisassembler()
		{
			if (CanDisassemble)
			{
				DisassemblerView.BlazingFast = true;
				DisassemblerView.ItemCount = 0;
				currentDisassemblerAddress = (uint)PCRegister.Value;
				Disassemble();
				DisassemblerView.Refresh();
				DisassemblerView.BlazingFast = false;
			}
		}

		uint currentDisassemblerAddress = 0;

		private void Disassemble()
		{
			int line_count = DisassemblerView.NumberOfVisibleRows;

			DisassemblyLines.Clear();
			uint a = currentDisassemblerAddress;
			for (int i = 0; i < line_count; ++i)
			{
				int advance;
				string line = Disassembler.Disassemble(MemoryDomains.SystemBus, a, out advance);
				DisassemblyLines.Add(new DisasmOp(a, advance, line));
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

			if (index < DisassemblyLines.Count)
			{
				if (column == 0)
				{
					text = string.Format("{0:X4}", DisassemblyLines[index].Address);
				}
				else if (column == 1)
				{
					text = DisassemblyLines[index].Mnemonic;
				}
			}
		}

		private void DisassemblerView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (DisassemblyLines.Any() && index < DisassemblyLines.Count)
			{
				if (DisassemblyLines[index].Address == (uint)PCRegister.Value)
				{
					color = Color.LightCyan;
				}
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

				if (newaddress < 0)
				{
					newaddress = 0;
					break;
				}

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
			currentDisassemblerAddress += (uint)DisassemblyLines.First().Size;
			if (currentDisassemblerAddress >= BusMaxValue)
			{
				currentDisassemblerAddress = (uint)(BusMaxValue - 1);
			}
		}

		private void DisassemblerView_Scroll(object sender, ScrollEventArgs e)
		{
			if (e.Type == ScrollEventType.SmallIncrement)
			{
				IncrementCurrentAddress();
				Disassemble();
				DisassemblerView.Refresh();
			}

			if (e.Type == ScrollEventType.SmallDecrement)
			{
				DecrementCurrentAddress();
				Disassemble();
				DisassemblerView.Refresh();
			}
		}

		private void SetDisassemblerItemCount()
		{
			DisassemblerView.ItemCount = DisassemblerView.NumberOfVisibleRows + 1;
		}

		private void DisassemblerView_SizeChanged(object sender, EventArgs e)
		{
			SetDisassemblerItemCount();
			if (CanDisassemble)
			{
				Disassemble();
			}
		}

		private void DisassemblerView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.C) // Ctrl + C
			{
				CopySelectedDisassembler();
			}
		}

		private void CopySelectedDisassembler()
		{
			var indices = DisassemblerView.SelectedIndices;

			if (indices.Count > 0)
			{
				var blob = new StringBuilder();
				foreach (int index in indices)
				{
					blob.Append(DisassemblyLines[index].Address)
						.Append(" ")
						.Append(DisassemblyLines[index].Mnemonic)
						.AppendLine();
				}

				blob.Remove(blob.Length - 2, 2); // Lazy way to not have a line break at the end
				Clipboard.SetDataObject(blob.ToString());
			}
		}

		private void OnPauseChanged(object sender, MainForm.PauseChangedEventArgs e)
		{
			FullUpdate();
		}

		private void DisassemblerContextMenu_Opening(object sender, EventArgs e)
		{
			AddBreakpointContextMenuItem.Enabled = DisassemblerView.SelectedIndices.Count > 0;
		}

		private void AddBreakpointContextMenuItem_Click(object sender, EventArgs e)
		{
			var indices = DisassemblerView.SelectedIndices;

			if (indices.Count > 0)
			{
				var line = DisassemblyLines[indices[0]];
				BreakPointControl1.AddBreakpoint(line.Address, Emulation.Common.MemoryCallbackType.Execute);
			}
		}
	}
}
