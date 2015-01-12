using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk.tools.Debugger
{
	public partial class BreakpointControl : UserControl
	{
		public IDebuggable Core { get; set; }
		public IMemoryCallbackSystem MCS { get; set; }
		public GenericDebugger ParentDebugger { get; set; }
		public MemoryDomainList MemoryDomains { get; set; }
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

		private void AddBreakpointButton_Click(object sender, EventArgs e)
		{
			var b = new AddBreakpointDialog
			{
				// TODO: don't use Global.Emulator! Pass in an IMemoryDomains implementation from the parent tool
				MaxAddressSize = Global.Emulator.AsMemoryDomains().MemoryDomains.CheatDomain.Size - 1
			};

			if (b.ShowDialog() == DialogResult.OK)
			{
				Breakpoints.Add(Core, b.Address, b.BreakType);
			}

			BreakpointView.ItemCount = Breakpoints.Count;
			UpdateBreakpointRemoveButton();
			UpdateStatsLabel();
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
	}
}
