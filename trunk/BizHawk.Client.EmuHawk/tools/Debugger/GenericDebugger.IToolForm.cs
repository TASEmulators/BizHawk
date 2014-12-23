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
		private IMemoryDomains MemoryDomainSource { get; set; }
		[OptionalService]
		private IMemoryCallbackSystem MCS { get; set; }

		private MemoryDomainList MemoryDomains { get { return MemoryDomainSource.MemoryDomains; } }

		private uint PC
		{
			// TODO: is this okay for N64?
			get { return (uint)Debuggable.GetCpuFlagsAndRegisters()[Disassembler.PCRegisterName].Value; }
		}

		#region Implementation checking

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
					var pc = PC;
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

		public void UpdateValues()
		{
			// TODO: probably none of this
			RegisterPanel.UpdateValues();
			UpdateDisassembler();
		}

		private void FullUpdate()
		{
			RegisterPanel.UpdateValues();
			UpdateDisassembler();
			// TODO: check for new breakpoints and add them to the Breakpoint list?
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
