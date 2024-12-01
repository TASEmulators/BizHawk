using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericDebugger : ToolFormBase, IToolFormAutoConfig, IControlMainform
	{
		private const string AddressColumnName = "Address";
		private const string InstructionColumnName = "Instruction";

		public static Icon ToolIcon
			=> Properties.Resources.BugIcon;

		protected override string WindowTitleStatic => "Debugger";

		public GenericDebugger()
		{
			InitializeComponent();
			Icon = ToolIcon;
			Closing += (o, e) => DisengageDebugger();

#if false
			var blockScroll = false;
			DisassemblerView.RowScroll += (_, _) =>
			{
				if (blockScroll) return;
				//TODO is this still needed?
			};
#endif
			DisassemblerView.QueryItemText += DisassemblerView_QueryItemText;
			DisassemblerView.QueryItemBkColor += DisassemblerView_QueryItemBkColor;
			DisassemblerView.AllColumns.Clear();
			DisassemblerView.AllColumns.Add(new(name: AddressColumnName, widthUnscaled: 94, text: AddressColumnName));
			DisassemblerView.AllColumns.Add(new(name: InstructionColumnName, widthUnscaled: 291, text: InstructionColumnName));
		}

		private void EngageDebugger()
		{
			Label GenDisabledCPUPicker(string text)
				=> new()
				{
					Location = new(UIHelper.ScaleX(35), UIHelper.ScaleY(23)),
					Size = new(UIHelper.ScaleX(100), UIHelper.ScaleY(15)),
					Text = text,
				};
			Control GenCPUPicker()
			{
				try
				{
					if (CanSetCpu && Disassembler.AvailableCpus.CountIsAtLeast(2))
					{
						ComboBox c = new()
						{
							DropDownStyle = ComboBoxStyle.DropDownList,
							Location = new(UIHelper.ScaleX(35), UIHelper.ScaleY(17)),
							Width = UIHelper.ScaleX(121),
						};
						c.Items.AddRange(Disassembler.AvailableCpus.Cast<object>().ToArray());
						c.SelectedItem = Disassembler.Cpu;
						c.SelectedIndexChanged += OnCpuDropDownIndexChanged;
						return c;
					}
				}
				catch (NotImplementedException)
				{
					// fall through
				}
				return GenDisabledCPUPicker(Disassembler.Cpu);
			}

			_disassemblyLines.Clear();
			MainForm.OnPauseChanged += OnPauseChanged;
			CancelSeekBtn.Enabled = false;
			if (CanDisassemble)
			{
				DisassemblerBox.Controls.Add(GenCPUPicker());
				_pcRegisterSize = PCRegister.BitSize / 4;
				SetDisassemblerItemCount();
				UpdatePC();
				UpdateDisassembler();
			}
			else
			{
				DisassemblerBox.Enabled = false;
				DisassemblerView.RowCount = 0;
				DisassemblerBox.Controls.Add(GenDisabledCPUPicker("Unknown"));
				toolTip1.SetToolTip(DisassemblerBox, "This core does not currently support disassembling");
			}

			RegisterPanel.Core = Debuggable;
			RegisterPanel.ParentDebugger = this;
			RegisterPanel.GenerateUI();

			if (CanUseMemoryCallbacks)
			{
				BreakPointControl1.MainForm = MainForm;
				BreakPointControl1.Core = Debuggable;
				BreakPointControl1.Mcs = MemoryCallbacks;
				BreakPointControl1.ParentDebugger = this;
				BreakPointControl1.MemoryDomains = MemoryDomains;
				BreakPointControl1.GenerateUI();
				EnabledBreakpointBox();
			}
			else
			{
				DisableBreakpointBox();
			}

			SeekToBox.Enabled = SeekToBtn.Enabled = CanUseMemoryCallbacks && RegisterPanel.CanGetCpuRegisters;

			if (RegisterPanel.CanGetCpuRegisters && CanDisassemble)
			{
				var pc = PCRegister;
				SeekToBox.Nullable = false;
				SeekToBox.SetHexProperties((long)Math.Pow(2, pc.BitSize));
				SeekToBox.SetFromRawInt(0);
			}
			else
			{
				SeekToBox.Nullable = true;
				SeekToBox.Text = "";
			}

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
			BreakPointControl1.Shutdown();
			MainForm.OnPauseChanged -= OnPauseChanged;
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

		public void EnabledBreakpointBox()
		{
			BreakpointsGroupBox.Enabled = true;
			toolTip1.SetToolTip(BreakpointsGroupBox, "");
		}

		private void OnCpuDropDownIndexChanged(object sender, EventArgs e)
		{
			Disassembler.Cpu = ((ComboBox) sender).SelectedItem.ToString();
		}

		private void RunBtn_Click(object sender, EventArgs e)
		{
			MainForm.UnpauseEmulator();
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

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.F11)
			{
				StepIntoMenuItem_Click(null, null);
				return true;
			}

			if (keyData == (Keys.F11 | Keys.Shift))
			{
				StepOutMenuItem_Click(null, null);
				return true;
			}

			if (keyData == Keys.F10)
			{
				StepOverMenuItem_Click(null, null);
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
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

		public void DisableCancelSeekBtn()
		{
			CancelSeekBtn.Enabled = false;
		}

		private void SeekToBtn_Click(object sender, EventArgs e)
		{
			CancelSeekBtn.Enabled = true;
			var pcVal = (uint)(SeekToBox.ToRawInt() ?? 0);
			var pcBitSize = PCRegister.BitSize;

			BreakPointControl1.RemoveCurrentSeek();
			BreakPointControl1.AddSeekBreakpoint(pcVal, pcBitSize);
			BreakPointControl1.UpdateValues();
		}

		private void CancelSeekBtn_Click(object sender, EventArgs e)
		{
			BreakPointControl1.RemoveCurrentSeek();
			CancelSeekBtn.Enabled = false;
		}

		private void ToPCBtn_Click(object sender, EventArgs e)
		{
			UpdatePC();
			UpdateDisassembler();
			DisassemblerView.Refresh();
		}

		private void RefreshMenuItem_Click(object sender, EventArgs e)
		{
			FullUpdate();
		}

		public void AddBreakpoint(uint address, uint mask, MemoryCallbackType type)
		{
			this.BreakPointControl1.AddBreakpoint(address, mask, type);
		}
	}
}
