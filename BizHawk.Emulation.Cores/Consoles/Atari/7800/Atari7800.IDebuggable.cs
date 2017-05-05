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
				["A"] = _theMachine.CPU.A,
				["P"] = _theMachine.CPU.P,
				["PC"] = _theMachine.CPU.PC,
				["S"] = _theMachine.CPU.S,
				["X"] = _theMachine.CPU.X,
				["Y"] = _theMachine.CPU.Y,
				["Flag B"] = _theMachine.CPU.fB,
				["Flag C"] = _theMachine.CPU.fC,
				["Flag D"] = _theMachine.CPU.fD,
				["Flag I"] = _theMachine.CPU.fI,
				["Flag N"] = _theMachine.CPU.fN,
				["Flag V"] = _theMachine.CPU.fV,
				["Flag Z"] = _theMachine.CPU.fZ
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					_theMachine.CPU.A = (byte)value;
					break;
				case "P":
					_theMachine.CPU.P = (byte)value;
					break;
				case "PC":
					_theMachine.CPU.PC = (ushort)value;
					break;
				case "S":
					_theMachine.CPU.S = (byte)value;
					break;
				case "X":
					_theMachine.CPU.X = (byte)value;
					break;
				case "Y":
					_theMachine.CPU.Y = (byte)value;
					break;
			}
		}

		public IMemoryCallbackSystem MemoryCallbacks
		{
			[FeatureNotImplemented]
			get { throw new NotImplementedException(); }
		}

		public bool CanStep(StepType type)
		{
			return false;
		}

		[FeatureNotImplemented]
		public void Step(StepType type)
		{
			throw new NotImplementedException();
		}

		public int TotalExecutedCycles => (int)_theMachine.CPU.Clock;
	}
}
