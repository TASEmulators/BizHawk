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
		private readonly List<DisasmOp> _disassemblyLines = new List<DisasmOp>();
		private int _pcRegisterSize = 4;
		private uint _currentDisassemblerAddress;

		private class DisasmOp
		{
			public DisasmOp(uint address, int size, string mnemonic)
			{
				Address = address;
				Size = size;
				Mnemonic = mnemonic;
			}

			public uint Address { get; }
			public int Size { get; }
			public string Mnemonic { get; }
		}

		private long BusMaxValue => MemoryDomains.SystemBus.Size;

		private void UpdatePC()
		{
			if (CanDisassemble)
			{
				_currentDisassemblerAddress = (uint)PCRegister.Value;
			}
		}

		private void UpdateDisassembler()
		{
			if (CanDisassemble)
			{
				DisassemblerView.BlazingFast = true;
				Disassemble();
				SetDisassemblerItemCount();
				DisassemblerView.BlazingFast = false;
			}
		}
		
		private void Disassemble()
		{
			int lineCount = DisassemblerView.NumberOfVisibleRows;

			_disassemblyLines.Clear();
			uint a = _currentDisassemblerAddress;
			for (int i = 0; i <= lineCount; ++i)
			{
				int advance;
				string line = Disassembler.Disassemble(MemoryDomains.SystemBus, a, out advance);
				_disassemblyLines.Add(new DisasmOp(a, advance, line));
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

			if (index < _disassemblyLines.Count)
			{
				if (column == 0)
				{
					text = string.Format("{0:X" + _pcRegisterSize + "}", _disassemblyLines[index].Address);
				}
				else if (column == 1)
				{
					text = _disassemblyLines[index].Mnemonic;
				}
			}
		}

		private void DisassemblerView_QueryItemBkColor(int index, int column, ref Color color)
		{
			if (_disassemblyLines.Any() && index < _disassemblyLines.Count)
			{
				if (_disassemblyLines[index].Address == _currentDisassemblerAddress)
				{
					color = Color.LightCyan;
				}
			}
		}

		private void DecrementCurrentAddress()
		{
			uint newaddress = _currentDisassemblerAddress;
			while (true)
			{
				int bytestoadvance;
				Disassembler.Disassemble(MemoryDomains.SystemBus, newaddress, out bytestoadvance);
				if (newaddress + bytestoadvance == _currentDisassemblerAddress)
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
				if (_currentDisassemblerAddress - newaddress > 5)
				{
					newaddress = _currentDisassemblerAddress - 1;
					break;
				}
			}

			_currentDisassemblerAddress = newaddress;
		}

		private void IncrementCurrentAddress()
		{
			_currentDisassemblerAddress += (uint)_disassemblyLines.First().Size;
			if (_currentDisassemblerAddress >= BusMaxValue)
			{
				_currentDisassemblerAddress = (uint)(BusMaxValue - 1);
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
					if (blob.Length != 0)
					{
						blob.AppendLine();
					}

					blob.Append(string.Format("{0:X" + _pcRegisterSize + "}", _disassemblyLines[index].Address))
						.Append(" ")
						.Append(_disassemblyLines[index].Mnemonic);
				}

				Clipboard.SetDataObject(blob.ToString());
			}
		}

		private void OnPauseChanged(object sender, MainForm.PauseChangedEventArgs e)
		{
			if (e.Paused)
			{
				FullUpdate();
			}
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
				var line = _disassemblyLines[indices[0]];
				BreakPointControl1.AddBreakpoint(line.Address, 0xFFFFFFFF, Emulation.Common.MemoryCallbackType.Execute);
			}
		}
	}
}
