using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericDebugger : ToolFormBase, IToolFormAutoConfig, IControlMainform
	{
		private const string AddressColumnName = "Address";
		private const string InstructionColumnName = "Instruction";

		public GenericDebugger()
		{
			InitializeComponent();
			Icon = Properties.Resources.BugIcon;
			Closing += (o, e) => DisengageDebugger();

			DisassemblerView.QueryItemText += DisassemblerView_QueryItemText;
			DisassemblerView.QueryItemBkColor += DisassemblerView_QueryItemBkColor;
			DisassemblerView.AllColumns.Clear();
			DisassemblerView.AllColumns.AddRange(new[]
			{
				new RollColumn
				{
					Name = AddressColumnName,
					Text = AddressColumnName,
					UnscaledWidth = 94,
					Type = ColumnType.Text
				},
				new RollColumn
				{
					Name = InstructionColumnName,
					Text = InstructionColumnName,
					UnscaledWidth = 291,
					Type = ColumnType.Text
				}
			});
		}

		private void EngageDebugger()
		{
			_disassemblyLines.Clear();
			MainForm.OnPauseChanged += OnPauseChanged;
			CancelSeekBtn.Enabled = false;
			if (CanDisassemble)
			{
				try
				{
					if (CanSetCpu && Disassembler.AvailableCpus.Count() > 1)
					{
						var c = new ComboBox
						{
							Location = new Point(UIHelper.ScaleX(35), UIHelper.ScaleY(17)),
							Width = UIHelper.ScaleX(121),
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
							Location = new Point(UIHelper.ScaleX(35), UIHelper.ScaleY(23)),
							Size = new Size(UIHelper.ScaleX(100), UIHelper.ScaleY(15)),
							Text = Disassembler.Cpu
						});
					}
				}
				catch (NotImplementedException)
				{
					DisassemblerBox.Controls.Add(new Label
					{
						Location = new Point(UIHelper.ScaleX(35), UIHelper.ScaleY(23)),
						Size = new Size(UIHelper.ScaleX(100), UIHelper.ScaleY(15)),
						Text = Disassembler.Cpu
					});
				}

				_pcRegisterSize = Debuggable.GetCpuFlagsAndRegisters()[Disassembler.PCRegisterName].BitSize / 4;
				SetDisassemblerItemCount();
				UpdateDisassembler();
			}
			else
			{
				DisassemblerBox.Enabled = false;
				DisassemblerView.RowCount = 0;
				DisassemblerBox.Controls.Add(new Label
				{
					Location = new Point(UIHelper.ScaleX(35), UIHelper.ScaleY(23)),
					Size = new Size(UIHelper.ScaleX(100), UIHelper.ScaleY(15)),
					Text = "Unknown"
				});

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
			UpdateDisassembler();
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
