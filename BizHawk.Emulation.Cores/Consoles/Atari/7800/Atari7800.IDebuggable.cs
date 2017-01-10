using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari7800
{
	public partial class Atari7800 : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				{ "A", theMachine.CPU.A },
				{ "P", theMachine.CPU.P },
				{ "PC", theMachine.CPU.PC },
				{ "S", theMachine.CPU.S },
				{ "X", theMachine.CPU.X },
				{ "Y", theMachine.CPU.Y },
				{ "Flag B", theMachine.CPU.fB },
				{ "Flag C", theMachine.CPU.fC },
				{ "Flag D", theMachine.CPU.fD },
				{ "Flag I", theMachine.CPU.fI },
				{ "Flag N", theMachine.CPU.fN },
				{ "Flag V", theMachine.CPU.fV },
				{ "Flag Z", theMachine.CPU.fZ }
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					theMachine.CPU.A = (byte)value;
					break;
				case "P":
					theMachine.CPU.P = (byte)value;
					break;
				case "PC":
					theMachine.CPU.PC = (ushort)value;
					break;
				case "S":
					theMachine.CPU.S = (byte)value;
					break;
				case "X":
					theMachine.CPU.X = (byte)value;
					break;
				case "Y":
					theMachine.CPU.Y = (byte)value;
					break;
			}
		}

		public IMemoryCallbackSystem MemoryCallbacks
		{
			[FeatureNotImplemented]
			get { throw new NotImplementedException(); }
		}

		public bool CanStep(StepType type) { return false; }

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		public int TotalExecutedCycles
		{
			get { return (int)theMachine.CPU.Clock; }
		}
	}
}
