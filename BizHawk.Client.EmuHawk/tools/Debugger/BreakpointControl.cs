using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk.tools.Debugger
{
	public partial class BreakpointControl : UserControl
	{
		public IDebuggable Core { get; set; }
		public IMemoryCallbackSystem MCS { get; set; }
		public GenericDebugger ParentDebugger { get; set; }
		public IMemoryDomains MemoryDomains { get; set; }
		private readonly BreakpointList Breakpoints = new BreakpointList();

		public BreakpointControl()
		{
			InitializeComponent();
			BreakpointView.QueryItemText += BreakPointView_QueryItemText;
			BreakpointView.QueryItemBkColor += BreakPointView_QueryItemBkColor;
			BreakpointView.VirtualMode = true;
			Breakpoints.Callback = BreakpointCallback;
		}

		private void BreakpointControl_Load(object sender, EventArgs e)
		{
			UpdateStatsLabel();
		}

		private void BreakPointView_QueryItemText(int index, int column, out string text)
		{
			text = string.Empty;
			switch (column)
			{
				case 0:
					text = string.Format("{0:X}", Breakpoints[index].Address);
					break;
				case 1:
					text = string.Format("{0:X}", Breakpoints[index].AddressMask);
					break;
				case 2:
					text = Breakpoints[index].Type.ToString();
					break;
				case 3:
					text = Breakpoints[index].Name;
					break;
			}
		}

		private void BreakPointView_QueryItemBkColor(int index, int column, ref Color color)
		{
			color = Breakpoints[index].ReadOnly ? SystemColors.Control
				: Breakpoints[index].Active ? Color.LightCyan
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

			var seekBreakpoint = Breakpoints.FirstOrDefault(x => x.Name.StartsWith(SeekName));

			if (seekBreakpoint != null)
			{
				Breakpoints.Remove(seekBreakpoint);
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

				BreakpointView.ItemCount = Breakpoints.Count;
				UpdateStatsLabel();
			}
		}

		/// <summary>
		/// Did any breakpoints get added from other sources such as lua?
		/// </summary>
		private void CheckForNewBreakpoints()
		{

			if (MCS != null)
			{
				foreach (var callback in MCS)
				{
					if (!Breakpoints.Any(b =>
						b.Type == callback.Type &&
						b.Address == callback.Address &&
						b.AddressMask == callback.AddressMask &&
						b.Name == callback.Name &&
						b.Callback == callback.Callback
						))
					{
						Breakpoints.Add(new Breakpoint(Core, callback));
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
					Breakpoints.Add(new Breakpoint(Core, callback));
				}

				BreakpointView.ItemCount = Breakpoints.Count;
				BreakpointView.Refresh();
				UpdateBreakpointRemoveButton();
				UpdateStatsLabel();
			}
			else
			{
				this.Enabled = false;
				ParentDebugger.DisableBreakpointBox();
			}
		}

		public void Shutdown()
		{
			Breakpoints.Clear();
			UpdateStatsLabel();
		}

		public void AddBreakpoint(uint address, uint mask, MemoryCallbackType type)
		{
			Breakpoints.Add(Core, address, mask, type);

			BreakpointView.ItemCount = Breakpoints.Count;
			UpdateBreakpointRemoveButton();
			UpdateStatsLabel();
		}

		private void AddBreakpointButton_Click(object sender, EventArgs e)
		{
			var b = CreateAddBreakpointDialog(BreakpointOperation.Add);

			if (b.ShowHawkDialog() == DialogResult.OK)
			{
				Breakpoints.Add(Core, b.Address, b.AddressMask, b.BreakType);
			}

			BreakpointView.ItemCount = Breakpoints.Count;
			UpdateBreakpointRemoveButton();
			UpdateStatsLabel();
		}

		private const string SeekName = "Seek to PC 0x";

		public void AddSeekBreakpoint(uint pcVal, int pcBitSize)
		{
			var Name = SeekName + pcVal.ToHexString(pcBitSize / 4);
			Breakpoints.Add(new Breakpoint(Name, true, Core, SeekCallback, pcVal, 0xFFFFFFFF, MemoryCallbackType.Execute));
		}

		public void RemoveCurrentSeek()
		{
			var seekBreakpoint = Breakpoints.FirstOrDefault(x => x.Name.StartsWith(SeekName));

			if (seekBreakpoint != null)
			{
				Breakpoints.Remove(seekBreakpoint);
				UpdateValues();
			}
		}

		private IEnumerable<int> SelectedIndices
		{
			get { return BreakpointView.SelectedIndices.Cast<int>(); }
		}

		private IEnumerable<Breakpoint> SelectedItems
		{
			get { return SelectedIndices.Select(index => Breakpoints[index]); }
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
						Breakpoints.Remove(item);
					}

					BreakpointView.ItemCount = Breakpoints.Count;
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

					BreakpointView.ItemCount = Breakpoints.Count;
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
			BreakpointStatsLabel.Text = string.Format("{0} Total / {1} Active", Breakpoints.Count(), Breakpoints.Count(x => x.Active));
		}

		private void ToggleButton_Click(object sender, EventArgs e)
		{
			foreach (var item in SelectedItems)
			{
				item.Active ^= true;
			}

			BreakpointView.ItemCount = Breakpoints.Count;
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
					Breakpoints.Add(new Breakpoint(Core, breakpoint.Callback, b.Address, b.AddressMask, b.BreakType, breakpoint.Active));
				}
			}

			BreakpointView.ItemCount = Breakpoints.Count;
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

			BreakpointView.ItemCount = Breakpoints.Count;
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

		public enum BreakpointOperation
		{
			Add, Edit, Duplicate
		}
	}
}
