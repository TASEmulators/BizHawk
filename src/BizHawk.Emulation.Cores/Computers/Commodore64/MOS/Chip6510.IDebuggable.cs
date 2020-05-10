using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Chip6510 : IDebuggable
	{
		IDictionary<string, RegisterValue> IDebuggable.GetCpuFlagsAndRegisters() => _cpu.GetCpuFlagsAndRegisters();

		void IDebuggable.SetCpuRegister(string register, int value) => _cpu.SetCpuRegister(register, value);

		bool IDebuggable.CanStep(StepType type)
		{
			switch (type)
			{
				case StepType.Into:
				case StepType.Over:
				case StepType.Out:
					return DebuggerStep != null;
				default:
					return false;
			}
		}

		void IDebuggable.Step(StepType type)
		{
			switch (type)
			{
				case StepType.Into:
					StepInto();
					break;
				case StepType.Out:
					StepOut();
					break;
				case StepType.Over:
					StepOver();
					break;
			}
		}

		long IDebuggable.TotalExecutedCycles => _cpu.TotalExecutedCycles;

		private void StepInto()
		{
			while (_cpu.AtInstructionStart())
			{
				DebuggerStep();
			}

			while (!_cpu.AtInstructionStart())
			{
				DebuggerStep();
			}
		}

		private void StepOver()
		{
			var instruction = Peek(_cpu.PC);

			if (instruction == Jsr)
			{
				var destination = _cpu.PC + JsrSize;
				while (_cpu.PC != destination)
				{
					StepInto();
				}
			}
			else
			{
				StepInto();
			}
		}

		private void StepOut()
		{
			var instructionsBeforeBailout = 1000000;
			var instr = Peek(_cpu.PC);
			_jsrCount = instr == Jsr ? 1 : 0;

			while (--instructionsBeforeBailout > 0)
			{
				StepInto();
				instr = Peek(_cpu.PC);
				if (instr == Jsr)
				{
					_jsrCount++;
				}
				else if ((instr == Rts || instr == Rti) && _jsrCount <= 0)
				{
					StepInto();
					_jsrCount = 0;
					break;
				}
				else if (instr == Rts || instr == Rti)
				{
					_jsrCount--;
				}
			}
		}

		private int _jsrCount;

		private const byte Jsr = 0x20;
		private const byte Rti = 0x40;
		private const byte Rts = 0x60;

		private const byte JsrSize = 3;

		IMemoryCallbackSystem IDebuggable.MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });
	}
}
