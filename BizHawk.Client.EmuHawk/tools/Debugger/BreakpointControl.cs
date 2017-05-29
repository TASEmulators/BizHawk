using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class BreakpointControl : UserControl
	{
		public IDebuggable Core { get; set; }
		public IMemoryCallbackSystem MCS { get; set; }
		public GenericDebugger ParentDebugger { get; set; }
		public IMemoryDomains MemoryDomains { get; set; }

		private readonly BreakpointList _breakpoints = new BreakpointList();

		public BreakpointControl()
		{
			InitializeComponent();
			BreakpointView.QueryItemText += BreakPointView_QueryItemText;
			BreakpointView.QueryItemBkColor += BreakPointView_QueryItemBkColor;
			BreakpointView.VirtualMode = true;
			_breakpoints.Callback = BreakpointCallback;
		}

		private void BreakpointControl_Load(object sender, EventArgs e)
		{
			UpdateStatsLabel();
		}

		private void BreakPointView_QueryItemText(int index, int column, out string text)
		{
			text = "";
			switch (column)
			{
				case 0:
					text = $"{_breakpoints[index].Address:X}";
					break;
				case 1:
					text = $"{_breakpoints[index].AddressMask:X}";
					break;
				case 2:
					text = _breakpoints[index].Type.ToString();
					break;
				case 3:
					text = _breakpoints[index].Name;
					break;
			}
		}

		private void BreakPointView_QueryItemBkColor(int index, int column, ref Color color)
		{
			color = _breakpoints[index].ReadOnly ? SystemColors.Control
				: _breakpoints[index].Active ? Color.LightCyan
				: Color.White;
		}

		private void BreakpointCallback()
		{
			GlobalWin.MainForm.PauseEmulator();
			UpdateValues();
			GlobalWin.OSD.AddMessage("Breakpoint hit");
		}

		private void SeekCallback()
		{
			BreakpointCallback();

			var seekBreakpoint = _breakpoints.FirstOrDefault(x => x.Name.StartsWith(SeekName));

			if (seekBreakpoint != null)
			{
				_breakpoints.Remove(seekBreakpoint);
				UpdateValues();
			}

			ParentDebugger.DisableCancelSeekBtn();
		}

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			if (Enabled)
			{
				CheckForNewBreakpoints();

				BreakpointView.ItemCount = _breakpoints.Count;
				UpdateStatsLabel();
			}
		}

		// Did any breakpoints get added from other sources such as lua?
		private void CheckForNewBreakpoints()
		{
			if (MCS != null)
			{
				foreach (var callback in MCS)
				{
					if (!_breakpoints.Any(b =>
						b.Type == callback.Type &&
						b.Address == callback.Address &&
						b.AddressMask == callback.AddressMask &&
						b.Name == callback.Name &&
						b.Callback == callback.Callback))
					{
						_breakpoints.Add(new Breakpoint(Core, callback));
					}
				}
			}
		}

		public void GenerateUI()
		{
			if (MCS != null)
			{
				foreach (var callback in MCS)
				{
					_breakpoints.Add(new Breakpoint(Core, callback));
				}

				BreakpointView.ItemCount = _breakpoints.Count;
				BreakpointView.Refresh();
				UpdateBreakpointRemoveButton();
				UpdateStatsLabel();
			}
			else
			{
				Enabled = false;
				ParentDebugger.DisableBreakpointBox();
			}
		}

		public void Shutdown()
		{
			_breakpoints.Clear();
			UpdateStatsLabel();
		}

		public void AddBreakpoint(uint address, uint mask, MemoryCallbackType type)
		{
			_breakpoints.Add(Core, address, mask, type);

			BreakpointView.ItemCount = _breakpoints.Count;
			UpdateBreakpointRemoveButton();
			UpdateStatsLabel();
		}

		private void AddBreakpointButton_Click(object sender, EventArgs e)
		{
			var b = CreateAddBreakpointDialog(BreakpointOperation.Add);

			if (b.ShowHawkDialog() == DialogResult.OK)
			{
				_breakpoints.Add(Core, b.Address, b.AddressMask, b.BreakType);
			}

			BreakpointView.ItemCount = _breakpoints.Count;
			UpdateBreakpointRemoveButton();
			UpdateStatsLabel();
		}

		private const string SeekName = "Seek to PC 0x";

		public void AddSeekBreakpoint(uint pcVal, int pcBitSize)
		{
			var name = SeekName + pcVal.ToHexString(pcBitSize / 4);
			_breakpoints.Add(new Breakpoint(name, true, Core, SeekCallback, pcVal, 0xFFFFFFFF, MemoryCallbackType.Execute));
		}

		public void RemoveCurrentSeek()
		{
			var seekBreakpoint = _breakpoints.FirstOrDefault(x => x.Name.StartsWith(SeekName));

			if (seekBreakpoint != null)
			{
				_breakpoints.Remove(seekBreakpoint);
				UpdateValues();
			}
		}

		private IEnumerable<int> SelectedIndices => BreakpointView.SelectedIndices.Cast<int>();

	    private IEnumerable<Breakpoint> SelectedItems
		{
			get { return SelectedIndices.Select(index => _breakpoints[index]); }
		}

		private IEnumerable<Breakpoint> EditableItems
		{
			get { return SelectedItems.Where(item => !item.ReadOnly); }
		}

		private void RemoveBreakpointButton_Click(object sender, EventArgs e)
		{
			if (EditableItems.Any())
			{
				var items = EditableItems.ToList();
				if (items.Any())
				{
					foreach (var item in items)
					{
						_breakpoints.Remove(item);
					}

					BreakpointView.ItemCount = _breakpoints.Count;
					UpdateBreakpointRemoveButton();
					UpdateStatsLabel();
				}
			}
		}

		private void UpdateBreakpointRemoveButton()
		{
			ToggleButton.Enabled =
			RemoveBreakpointButton.Enabled =
			EditableItems.Any();

			DuplicateBreakpointButton.Enabled =
			EditBreakpointButton.Enabled =
			EditableItems.Count() == 1;
		}

		private void BreakpointView_SelectedIndexChanged(object sender, EventArgs e)
		{
			UpdateBreakpointRemoveButton();
		}

		private void BreakpointView_ItemActivate(object sender, EventArgs e)
		{
			if (EditableItems.Any())
			{
				var items = EditableItems.ToList();
				if (items.Any())
				{
					foreach (var item in items)
					{
						item.Active ^= true;
					}

					BreakpointView.ItemCount = _breakpoints.Count;
					UpdateBreakpointRemoveButton();
					UpdateStatsLabel();
				}
			}
		}

		private void BreakpointView_KeyDown(object sender, KeyEventArgs e)
		{
			if (!e.Control && !e.Alt && !e.Shift && e.KeyCode == Keys.Delete) // Delete
			{
				RemoveBreakpointButton_Click(null, null);
			}
		}

		private void UpdateStatsLabel()
		{
			BreakpointStatsLabel.Text = $"{_breakpoints.Count} Total / {_breakpoints.Count(b => b.Active)} Active";
		}

		private void ToggleButton_Click(object sender, EventArgs e)
		{
			foreach (var item in SelectedItems)
			{
				item.Active ^= true;
			}

			BreakpointView.ItemCount = _breakpoints.Count;
			UpdateStatsLabel();
		}

		private void DuplicateBreakpointButton_Click(object sender, EventArgs e)
		{
			var breakpoint = SelectedItems.FirstOrDefault();

			if (breakpoint != null && !breakpoint.ReadOnly)
			{
				var b = CreateAddBreakpointDialog(BreakpointOperation.Duplicate, breakpoint.Type, breakpoint.Address, breakpoint.AddressMask);

				if (b.ShowHawkDialog() == DialogResult.OK)
				{
					_breakpoints.Add(new Breakpoint(Core, breakpoint.Callback, b.Address, b.AddressMask, b.BreakType, breakpoint.Active));
				}
			}

			BreakpointView.ItemCount = _breakpoints.Count;
			UpdateBreakpointRemoveButton();
			UpdateStatsLabel();
		}

		private void EditBreakpointButton_Click(object sender, EventArgs e)
		{
			var breakpoint = SelectedItems.FirstOrDefault();

			if (breakpoint != null && !breakpoint.ReadOnly)
			{
				var b = CreateAddBreakpointDialog(BreakpointOperation.Edit, breakpoint.Type, breakpoint.Address, breakpoint.AddressMask);

				if (b.ShowHawkDialog() == DialogResult.OK)
				{
					breakpoint.Type = b.BreakType;
					breakpoint.Address = b.Address;
					breakpoint.AddressMask = b.AddressMask;
					breakpoint.ResetCallback();
				}
			}

			BreakpointView.ItemCount = _breakpoints.Count;
			UpdateBreakpointRemoveButton();
			UpdateStatsLabel();
		}

		private AddBreakpointDialog CreateAddBreakpointDialog(BreakpointOperation op, MemoryCallbackType? type = null, uint? address = null, uint? mask = null)
		{
			var operation = (AddBreakpointDialog.BreakpointOperation)op;

			var b = new AddBreakpointDialog(operation)
			{
				MaxAddressSize = MemoryDomains.SystemBus.Size - 1
			};

			if (type != null)
			{
				b.BreakType = (MemoryCallbackType)type;
			}

			if (address != null)
			{
				b.Address = (uint)address;
			}

			if (mask != null)
			{
				b.AddressMask = (uint)mask;
			}

			if (!MCS.ExecuteCallbacksAvailable)
			{
				b.DisableExecuteOption();
			}

			return b;
		}

		private enum BreakpointOperation
		{
			Add, Edit, Duplicate
		}
	}
}
