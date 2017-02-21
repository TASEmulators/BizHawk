using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	public partial class NES : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				{ "A", cpu.A },
				{ "X", cpu.X },
				{ "Y", cpu.Y },
				{ "S", cpu.S },
				{ "PC", cpu.PC },
				{ "Flag C", cpu.FlagC },
				{ "Flag Z", cpu.FlagZ },
				{ "Flag I", cpu.FlagI },
				{ "Flag D", cpu.FlagD },
				{ "Flag B", cpu.FlagB },
				{ "Flag V", cpu.FlagV },
				{ "Flag N", cpu.FlagN },
				{ "Flag T", cpu.FlagT }
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					cpu.A = (byte)value;
					break;
				case "X":
					cpu.X = (byte)value;
					break;
				case "Y":
					cpu.Y = (byte)value;
					break;
				case "S":
					cpu.S = (byte)value;
					break;
				case "PC":
					cpu.PC = (ushort)value;
					break;
				case "Flag I":
					cpu.FlagI = value > 0;
					break;
			}
		}

		public bool CanStep(StepType type)
		{
			return false;
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; private set; }

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		public int TotalExecutedCycles
		{
			get { return cpu.TotalExecutedCycles; }
		}
	}
}
