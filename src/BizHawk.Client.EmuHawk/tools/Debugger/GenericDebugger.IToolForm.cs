using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericDebugger : IToolForm
	{
		[RequiredService]
		private IDebuggable Debuggable { get; set; }

		[OptionalService]
		private IDisassemblable Disassembler { get; set; }

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		private IMemoryCallbackSystem MemoryCallbacks => Debuggable.MemoryCallbacks;


		private RegisterValue PCRegister => Debuggable.GetCpuFlagsAndRegisters()[Disassembler.PCRegisterName];

		private bool CanUseMemoryCallbacks = false;

		private bool CanDisassemble = false;

		private bool CanSetCpu = false;

		private bool CanStepInto = false;

		private bool CanStepOver = false;

		private bool CanStepOut = false;

		private void UpdateCapabilitiesProps()
		{
			try
			{
				_ = MemoryCallbacks.HasReads;
				CanUseMemoryCallbacks = true;
			}
			catch (NotImplementedException)
			{
				CanUseMemoryCallbacks = false;
			}

			if (Disassembler is null)
			{
				CanDisassemble = false;
				CanSetCpu = false;
			}
			else
			{
				try
				{
					_ = (uint) PCRegister.Value;
					CanDisassemble = true;
				}
				catch (NotImplementedException)
				{
					CanDisassemble = false;
				}
				try
				{
					Disassembler.Cpu = Disassembler.Cpu;
					CanSetCpu = true;
				}
				catch (NotImplementedException)
				{
					CanSetCpu = false;
				}
			}

			try
			{
				CanStepInto = Debuggable.CanStep(StepType.Into);
			}
			catch (NotImplementedException)
			{
				CanStepInto = false;
			}
			try
			{
				CanStepOver = Debuggable.CanStep(StepType.Over);
			}
			catch (NotImplementedException)
			{
				CanStepOver = false;
			}
			try
			{
				CanStepOut = Debuggable.CanStep(StepType.Out);
			}
			catch (NotImplementedException)
			{
				CanStepOut = false;
			}
		}

		private void FullUpdate()
		{
			RegisterPanel.UpdateValues();
			UpdatePC();
			UpdateDisassembler();
			BreakPointControl1.UpdateValues();
		}

		public override void Restart()
		{
			UpdateCapabilitiesProps();
			DisengageDebugger();
			EngageDebugger();
			FullUpdate();
		}
	}
}
