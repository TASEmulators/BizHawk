using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	[ToolAttributes(released: false)]
	public partial class GenericDebugger : Form, IToolForm, IControlMainform
	{
		private int _defaultWidth;
		private int _defaultHeight;

		public GenericDebugger()
		{
			InitializeComponent();
			TopMost = Global.Config.GenericDebuggerSettings.TopMost;
			Closing += (o, e) => DisengageDebugger();

			DisassemblerView.QueryItemText += DisassemblerView_QueryItemText;
			DisassemblerView.QueryItemBkColor += DisassemblerView_QueryItemBkColor;
			DisassemblerView.VirtualMode = true;
			DisassemblerView.ItemCount = ADDR_MAX + 1;
		}

		private void GenericDebugger_Load(object sender, EventArgs e)
		{
			_defaultWidth = Size.Width;
			_defaultHeight = Size.Height;

			if (Global.Config.GenericDebuggerSettings.UseWindowPosition)
			{
				Location = Global.Config.GenericDebuggerSettings.WindowPosition;
			}

			if (Global.Config.GenericDebuggerSettings.UseWindowSize)
			{
				Size = Global.Config.GenericDebuggerSettings.WindowSize;
			}

			EngageDebugger();
		}

		private void DisassemblerView_QueryItemText(int index, int column, out string text)
		{
			text = "";
			if (column == 0)
			{
				if (addr <= index && index < addr + lines.Count)
				{
					int a = addr;
					for (int i = 0; i < index - addr; ++i)
						a += lines[i].size;
					text = string.Format("{0:X4}", a);
				}
			}
			else if (column == 1)
			{
				if (addr <= index && index < addr + lines.Count)
					text = lines[index - addr].mnemonic;
			}
		}

		private void DisassemblerView_QueryItemBkColor(int index, int column, ref Color color)
		{
			
		}

		public void DisableRegisterBox()
		{
			RegistersGroupBox.Enabled = false;
			toolTip1.SetToolTip(RegistersGroupBox, "This core does not currently support reading registers");
		}

		public void DisableBreakpointBox()
		{
			BreakpointsGroupBox.Enabled = false;
			toolTip1.SetToolTip(BreakpointsGroupBox, "This core does not currently support breakpoints");
		}

		private void OnCpuDropDownIndexChanged(object sender, EventArgs e)
		{
			Disassembler.Cpu = (sender as ComboBox).SelectedItem.ToString();
		}

		private void EngageDebugger()
		{
			if (CanDisassemble)
			{
				try
				{
					if (CanSetCpu && Disassembler.AvailableCpus.Count() > 1)
					{
						var c = new ComboBox
						{
							Location = new Point(35, 17),
							DropDownStyle = ComboBoxStyle.DropDownList
						};

						c.Items.AddRange(Disassembler.AvailableCpus.ToArray());
						c.SelectedItem = Disassembler.Cpu;
						c.SelectedIndexChanged += OnCpuDropDownIndexChanged;

						DisassemblerBox.Controls.Add(c);
					}
					else
					{
						DisassemblerBox.Controls.Add(new Label
						{
							Location = new Point(35, 23),
							Text = Disassembler.Cpu
						});
					}
				}
				catch (NotImplementedException)
				{
					DisassemblerBox.Controls.Add(new Label
					{
						Location = new Point(35, 23),
						Text = Disassembler.Cpu
					});
				}
			}
			else
			{
				DisassemblerBox.Enabled = false;
				DisassemblerBox.Controls.Add(new Label
				{
					Location = new Point(35, 23),
					Text = "Unknown"
				});

				toolTip1.SetToolTip(DisassemblerBox, "This core does not currently support disassembling");
			}

			RegisterPanel.Core = Debuggable;
			RegisterPanel.ParentDebugger = this;
			RegisterPanel.GenerateUI();

			BreakPointControl1.Core = Debuggable;
			BreakPointControl1.MCS = MCS;
			BreakPointControl1.ParentDebugger = this;
			BreakPointControl1.GenerateUI();

			StepIntoMenuItem.Enabled = StepIntoBtn.Enabled = CanStepInto;
			StepOutMenuItem.Enabled = StepOutBtn.Enabled = CanStepOut;
			StepOverMenuItem.Enabled = StepOverBtn.Enabled = CanStepOver;

			if (!StepIntoMenuItem.Enabled)
			{
				toolTip1.SetToolTip(StepIntoBtn, "This core does not currently implement this feature");
			}

			if (!StepOutMenuItem.Enabled)
			{
				toolTip1.SetToolTip(StepOutBtn, "This core does not currently implement this feature");
			}

			if (!StepOverMenuItem.Enabled)
			{
				toolTip1.SetToolTip(StepOverBtn, "This core does not currently implement this feature");
			}
		}

		private void DisengageDebugger()
		{
			SaveConfigSettings();
			if (Debuggable.Tracer != null)
			{
				Debuggable.Tracer.Enabled = false;
			}

			BreakPointControl1.Shutdown();
		}

		private void SaveConfigSettings()
		{
			if (Global.Config.GenericDebuggerSettings.SaveWindowPosition)
			{
				Global.Config.GenericDebuggerSettings.Wndx = Location.X;
				Global.Config.GenericDebuggerSettings.Wndy = Location.Y;
				Global.Config.GenericDebuggerSettings.Width = Right - Left;
				Global.Config.GenericDebuggerSettings.Height = Bottom - Top;
			}
		}

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl();
			base.OnShown(e);
		}

		private void RefreshFloatingWindowControl()
		{
			Owner = Global.Config.RamSearchSettings.FloatingWindow ? null : GlobalWin.MainForm;
		}

		#region Disassembler TODO refacotor

		private readonly List<DisasmOp> lines = new List<DisasmOp>();

		private struct DisasmOp
		{
			public readonly int size;
			public readonly string mnemonic;
			public DisasmOp(int s, string m) { size = s; mnemonic = m; }
		}

		private int addr;
		private const int ADDR_MAX = 0xFFFF; // TODO: this isn't a constant, calculate it off bus size
		private const int DISASM_LINE_COUNT = 100;

		private void UpdateDisassembler()
		{
			// Always show a window's worth of instructions (if possible)
			if (CanDisassemble)
			{
				addr = PC;

				DisassemblerView.BlazingFast = true;
				Disasm(DISASM_LINE_COUNT);
				DisassemblerView.ensureVisible(0xFFFF);
				DisassemblerView.ensureVisible(PC);

				DisassemblerView.Refresh();
				DisassemblerView.BlazingFast = false;
			}
		}

		private void Disasm(int line_count)
		{
			lines.Clear();
			int a = addr;
			for (int i = 0; i < line_count; ++i)
			{
				int advance;
				string line = Disassembler.Disassemble(MemoryDomains.SystemBus, (ushort)a, out advance);
				lines.Add(new DisasmOp(advance, line));
				a += advance;
				if (a > ADDR_MAX) break;
			}
		}

		#endregion

		#region Menu Items

		#region File

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Debug

		private void DebugSubMenu_DropDownOpened(object sender, EventArgs e)
		{

		}

		private void StepIntoMenuItem_Click(object sender, EventArgs e)
		{
			if (CanStepInto)
			{
				Debuggable.Step(StepType.Into);
				FullUpdate();
			}
		}

		private void StepOverMenuItem_Click(object sender, EventArgs e)
		{
			if (CanStepOver)
			{
				Debuggable.Step(StepType.Over);
				FullUpdate();
			}
		}

		private void StepOutMenuItem_Click(object sender, EventArgs e)
		{
			if (CanStepOut)
			{
				Debuggable.Step(StepType.Out);
				FullUpdate();
			}
		}

		#endregion

		#region Options

		private void OptionsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadMenuItem.Checked = Global.Config.GenericDebuggerAutoload;
			SaveWindowPositionMenuItem.Checked = Global.Config.GenericDebuggerSettings.SaveWindowPosition;
			AlwaysOnTopMenuItem.Checked = Global.Config.GenericDebuggerSettings.TopMost;
			FloatingWindowMenuItem.Checked = Global.Config.GenericDebuggerSettings.FloatingWindow;
		}

		private void AutoloadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GenericDebuggerAutoload ^= true;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GenericDebuggerSettings.SaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			TopMost = Global.Config.GenericDebuggerSettings.TopMost ^= true;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GenericDebuggerSettings.FloatingWindow ^= true;
			RefreshFloatingWindowControl();
		}

		private void RestoreDefaultsMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);
			Global.Config.GenericDebuggerSettings = new ToolDialogSettings();
			TopMost = Global.Config.GenericDebuggerSettings.TopMost;
			RefreshFloatingWindowControl();
		}

		#endregion

		#endregion

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.F11)
			{
				StepIntoMenuItem_Click(null, null);
				return true;
			}
			else if (keyData == (Keys.F11 | Keys.Shift))
			{
				StepOutMenuItem_Click(null, null);
				return true;
			}
			else if (keyData == Keys.F10)
			{
				StepOverMenuItem_Click(null, null);
				return true;
			}
			else
			{
				return base.ProcessCmdKey(ref msg, keyData);
			}
		}

		private Control _currentToolTipControl = null; 
		private void GenericDebugger_MouseMove(object sender, MouseEventArgs e)
		{
			var control = GetChildAtPoint(e.Location);
			if (control != null)
			{
				if (!control.Enabled && _currentToolTipControl == null)
				{
					string toolTipString = toolTip1.GetToolTip(control);
					toolTip1.Show(toolTipString, control, control.Width / 2, control.Height / 2);
					_currentToolTipControl = control;
				}
			}
			else
			{
				if (_currentToolTipControl != null)
				{
					toolTip1.Hide(_currentToolTipControl);
				}

				_currentToolTipControl = null;
			}
		}
	}
}
