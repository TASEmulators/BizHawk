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
			}
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
					Breakpoints.Add(new Breakpoint(Core, callback.Callback, callback.Address ?? 0 /*TODO*/,callback.Type, true));
				}

				BreakpointView.Refresh();
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
			var b = new AddBreakpointDialog(); // TODO: rename and move this widget
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

		private void RemoveBreakpointButton_Click(object sender, EventArgs e)
		{
			if (BreakpointView.SelectedIndices.Count > 0)
			{
				var items = SelectedItems.ToList();
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
			RemoveBreakpointButton.Enabled = BreakpointView.SelectedIndices.Count > 0;
		}
	}
}
