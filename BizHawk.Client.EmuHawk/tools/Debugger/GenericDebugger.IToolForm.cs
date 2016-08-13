using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericDebugger : IToolForm
	{
		[RequiredService]
		private IDebuggable Debuggable { get; set; }

		[OptionalService]
		private IDisassemblable Disassembler { get; set; }

		[OptionalService]
		private IMemoryDomains MemoryDomains { get; set; }

		private IMemoryCallbackSystem MemoryCallbacks { get { return Debuggable.MemoryCallbacks; } }


		private RegisterValue PCRegister
		{
			get { return Debuggable.GetCpuFlagsAndRegisters()[Disassembler.PCRegisterName]; }
		}

		#region Implementation checking

		private bool CanUseMemoryCallbacks
		{
			get
			{
				if (Debuggable != null)
				{
					try
					{
						var result = Debuggable.MemoryCallbacks.HasReads;
						return true;
					}
					catch (NotImplementedException)
					{
						return false;
					}
				}

				return false;
			}
		}

		private bool CanDisassemble
		{
			get
			{
				if (Disassembler == null)
				{
					return false;
				}

				try
				{
					var pc = (uint)PCRegister.Value;
					return true;
				}
				catch (NotImplementedException)
				{
					return false;
				}

			}
		}

		private bool CanSetCpu
		{
			get
			{
				try
				{
					Disassembler.Cpu = Disassembler.Cpu;
					return true;
				}
				catch (NotImplementedException)
				{
					return false;
				}
			}
		}

		private bool CanStepInto
		{
			get
			{
				try
				{
					return Debuggable.CanStep(StepType.Into);
				}
				catch (NotImplementedException)
				{
					return false;
				}
			}
		}

		private bool CanStepOver
		{
			get
			{
				try
				{
					return Debuggable.CanStep(StepType.Over);
				}
				catch (NotImplementedException)
				{
					return false;
				}
			}
		}

		private bool CanStepOut
		{
			get
			{
				try
				{
					return Debuggable.CanStep(StepType.Out);
				}
				catch (NotImplementedException)
				{
					return false;
				}
			}
		}

		#endregion

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			// Nothing to do
		}

		private void FullUpdate()
		{
			RegisterPanel.UpdateValues();
			UpdatePC();
			UpdateDisassembler();
			BreakPointControl1.UpdateValues();
		}

		public void FastUpdate()
		{
			// Nothing to do
		}

		public void Restart()
		{
			DisengageDebugger();
			EngageDebugger();
		}

		public bool AskSaveChanges()
		{
			// TODO
			return true;
		}

		public bool UpdateBefore
		{
			get { return false; }
		}
	}
}
