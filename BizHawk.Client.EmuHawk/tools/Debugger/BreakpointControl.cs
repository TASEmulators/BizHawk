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

namespace BizHawk.Client.EmuHawk.tools.Debugger
{
	public partial class BreakpointControl : UserControl
	{
		public IDebuggable Core { get; set; }
		public GenericDebugger ParentDebugger { get; set; }
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

		}

		private void BreakPointView_QueryItemText(int index, int column, out string text)
		{
			text = string.Empty;
			switch (column)
			{
				case 0:
					text = string.Format("{0:X4}", Breakpoints[index].Address);
					break;
				case 1:
					text = Breakpoints[index].Type.ToString();
					break;
				case 2:
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
		}

		public void UpdateValues()
		{
			if (this.Enabled)
			{

			}
		}

		public void GenerateUI()
		{
			if (Core.MemoryCallbacksAvailable())
			{
				foreach (var callback in Core.MemoryCallbacks)
				{
					Breakpoints.Add(new Breakpoint(Core, callback));
				}

				BreakpointView.ItemCount = Breakpoints.Count;
				BreakpointView.Refresh();
				UpdateBreakpointRemoveButton();
			}
			else
			{
				this.Enabled = false;
			}
		}

		public void Shutdown()
		{
			Breakpoints.Clear();
		}

		private void AddBreakpointButton_Click(object sender, EventArgs e)
		{
			var b = new AddBreakpointDialog
			{
				// TODO: don't use Global.Emulator! Pass in an IMemoryDomains implementation from the parent tool
				MaxAddressSize = (Global.Emulator.AsMemoryDomains().MemoryDomains.SystemBus.Size - 1).NumHexDigits()
			};

			if (b.ShowDialog() == DialogResult.OK)
			{
				Breakpoints.Add(Core, b.Address, b.BreakType);
			}

			BreakpointView.ItemCount = Breakpoints.Count;
			UpdateBreakpointRemoveButton();
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
				}
			}
		}

		private void UpdateBreakpointRemoveButton()
		{
			RemoveBreakpointButton.Enabled = EditableItems.Any();
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
	}
}
