using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class BreakpointControl : UserControl, IDialogParent
	{
		public IMainFormForTools MainForm { get; set; }
		public IDebuggable Core { get; set; }
		public IMemoryCallbackSystem Mcs { get; set; }
		public GenericDebugger ParentDebugger { get; set; }
		public IMemoryDomains MemoryDomains { get; set; }

		private readonly BreakpointList _breakpoints = new BreakpointList();

		public IDialogController DialogController => MainForm;

		public BreakpointControl()
		{
			InitializeComponent();
			AddBreakpointButton.Image = Resources.Add;
			ToggleButton.Image = Resources.Refresh;
			RemoveBreakpointButton.Image = Resources.Delete;
			DuplicateBreakpointButton.Image = Resources.Duplicate;
			EditBreakpointButton.BackgroundImage = Resources.Pencil;
			BreakpointView.RetrieveVirtualItem += BreakPointView_QueryItemText;
			BreakpointView.VirtualMode = true;
			_breakpoints.Callback = BreakpointCallback;
		}

		private void BreakpointControl_Load(object sender, EventArgs e)
		{
			UpdateStatsLabel();
		}

		private void BreakPointView_QueryItemText(object sender, RetrieveVirtualItemEventArgs e)
		{
			var entry = _breakpoints[e.ItemIndex];
			e.Item = new ListViewItem($"{entry.Address:X}");
			e.Item.SubItems.Add($"{entry.AddressMask:X}");
			e.Item.SubItems.Add(entry.Type.ToString());
			e.Item.SubItems.Add(entry.Name);

			e.Item.BackColor = entry.ReadOnly ? SystemColors.Control
				: entry.Active ? Color.LightCyan
				: Color.White;
		}

		private void BreakpointCallback(uint addr, uint value, uint flags)
		{
			MainForm.PauseEmulator();
			UpdateValues();
			MainForm.AddOnScreenMessage("Breakpoint hit");
		}

		private void SeekCallback(uint addr, uint value, uint flags)
		{
			BreakpointCallback(addr, value, flags);

			var seekBreakpoint = _breakpoints.FirstOrDefault(x => x.Name.StartsWithOrdinal(SeekName));

			if (seekBreakpoint != null)
			{
				_breakpoints.Remove(seekBreakpoint);
				UpdateValues();
			}

			ParentDebugger.DisableCancelSeekBtn();
		}

		public void UpdateValues()
		{
			if (Enabled)
			{
				CheckForNewBreakpoints();

				BreakpointView.VirtualListSize = _breakpoints.Count;
				UpdateStatsLabel();
			}
		}

		// Did any breakpoints get added from other sources such as lua?
		private void CheckForNewBreakpoints()
		{
			if (Mcs != null)
			{
				foreach (var callback in Mcs)
				{
					if (!_breakpoints.Any(b => b.Type == callback.Type
						&& b.Address == callback.Address
						&& b.AddressMask == callback.AddressMask
						&& b.Name == callback.Name
						&& b.Callback == callback.Callback))
					{
						_breakpoints.Add(new Breakpoint(Core, callback));
					}
				}
			}
		}

		public void GenerateUI()
		{
			if (Mcs != null)
			{
				foreach (var callback in Mcs)
				{
					_breakpoints.Add(new Breakpoint(Core, callback));
				}

				BreakpointView.VirtualListSize = _breakpoints.Count;
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
			_breakpoints.Add(Core, MemoryDomains.SystemBus.Name, address, mask, type);

			BreakpointView.VirtualListSize = _breakpoints.Count;
			UpdateBreakpointRemoveButton();
			UpdateStatsLabel();
		}

		private void AddBreakpointButton_Click(object sender, EventArgs e)
		{
			var b = CreateAddBreakpointDialog(BreakpointOperation.Add);

			if (this.ShowDialogWithTempMute(b).IsOk())
			{
				_breakpoints.Add(Core, MemoryDomains.SystemBus.Name, b.Address, b.AddressMask, b.BreakType);
			}

			BreakpointView.VirtualListSize = _breakpoints.Count;
			UpdateBreakpointRemoveButton();
			UpdateStatsLabel();
		}

		private const string SeekName = "Seek to PC 0x";

		public void AddSeekBreakpoint(uint pcVal, int pcBitSize)
		{
			var name = SeekName + pcVal.ToHexString(pcBitSize / 4);
			_breakpoints.Add(new Breakpoint(name, true, Core, MemoryDomains.SystemBus.Name, SeekCallback, pcVal, 0xFFFFFFFF, MemoryCallbackType.Execute));
		}

		public void RemoveCurrentSeek()
		{
			var seekBreakpoint = _breakpoints.FirstOrDefault(x => x.Name.StartsWithOrdinal(SeekName));

			if (seekBreakpoint != null)
			{
				_breakpoints.Remove(seekBreakpoint);
				UpdateValues();
			}
		}

		private IEnumerable<int> SelectedIndices => BreakpointView.SelectedIndices.Cast<int>();
		
		private IEnumerable<Breakpoint> SelectedItems => SelectedIndices.Select(index => _breakpoints[index]);

		private IEnumerable<Breakpoint> EditableItems => SelectedItems.Where(item => !item.ReadOnly);

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

					BreakpointView.VirtualListSize = _breakpoints.Count;
					UpdateBreakpointRemoveButton();
					UpdateStatsLabel();
				}
			}
		}

		private void UpdateBreakpointRemoveButton()
		{
			var editableCount = EditableItems.Count();
			ToggleButton.Enabled = RemoveBreakpointButton.Enabled = editableCount > 0;
			DuplicateBreakpointButton.Enabled = EditBreakpointButton.Enabled = editableCount == 1;
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
					foreach (var item in items) item.Active = !item.Active;
					BreakpointView.VirtualListSize = _breakpoints.Count;
					UpdateBreakpointRemoveButton();
					UpdateStatsLabel();
				}
			}
		}

		private void BreakpointView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.IsPressed(Keys.Delete))
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
			foreach (var item in SelectedItems) item.Active = !item.Active;
			BreakpointView.VirtualListSize = _breakpoints.Count;
			UpdateStatsLabel();
		}

		private void DuplicateBreakpointButton_Click(object sender, EventArgs e)
		{
			var breakpoint = SelectedItems.FirstOrDefault();

			if (breakpoint != null && !breakpoint.ReadOnly)
			{
				var b = CreateAddBreakpointDialog(BreakpointOperation.Duplicate, breakpoint.Type, breakpoint.Address, breakpoint.AddressMask);

				if (this.ShowDialogWithTempMute(b).IsOk())
				{
					_breakpoints.Add(new Breakpoint(Core, MemoryDomains.SystemBus.Name, breakpoint.Callback, b.Address, b.AddressMask, b.BreakType, breakpoint.Active));
				}
			}

			BreakpointView.VirtualListSize = _breakpoints.Count;
			UpdateBreakpointRemoveButton();
			UpdateStatsLabel();
		}

		private void EditBreakpointButton_Click(object sender, EventArgs e)
		{
			var breakpoint = SelectedItems.FirstOrDefault();

			if (breakpoint != null && !breakpoint.ReadOnly)
			{
				var b = CreateAddBreakpointDialog(BreakpointOperation.Edit, breakpoint.Type, breakpoint.Address, breakpoint.AddressMask);

				if (this.ShowDialogWithTempMute(b).IsOk())
				{
					breakpoint.Type = b.BreakType;
					breakpoint.Address = b.Address;
					breakpoint.AddressMask = b.AddressMask;
					breakpoint.ResetCallback();
				}
			}

			BreakpointView.VirtualListSize = _breakpoints.Count;
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

			if (!Mcs.ExecuteCallbacksAvailable)
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
