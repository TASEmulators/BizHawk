using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Serial
{
	public sealed partial class Drive1541 : IDebuggable
	{
		IDictionary<string, RegisterValue> IDebuggable.GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["A"] = _cpu.A,
				["X"] = _cpu.X,
				["Y"] = _cpu.Y,
				["S"] = _cpu.S,
				["PC"] = _cpu.PC,
				["Flag C"] = _cpu.FlagC,
				["Flag Z"] = _cpu.FlagZ,
				["Flag I"] = _cpu.FlagI,
				["Flag D"] = _cpu.FlagD,
				["Flag B"] = _cpu.FlagB,
				["Flag V"] = _cpu.FlagV,
				["Flag N"] = _cpu.FlagN,
				["Flag T"] = _cpu.FlagT
			};
		}

		void IDebuggable.SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					_cpu.A = (byte)value;
					break;
				case "X":
					_cpu.X = (byte)value;
					break;
				case "Y":
					_cpu.Y = (byte)value;
					break;
				case "S":
					_cpu.S = (byte)value;
					break;
				case "PC":
					_cpu.PC = (ushort)value;
					break;
			}
		}

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
			var instruction = CpuPeek(_cpu.PC);

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
			var instr = CpuPeek(_cpu.PC);
			_jsrCount = instr == Jsr ? 1 : 0;

			while (--instructionsBeforeBailout > 0)
			{
				StepInto();
				instr = CpuPeek(_cpu.PC);
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

		public IMemoryCallbackSystem MemoryCallbacks => throw new NotImplementedException();
	}
}
