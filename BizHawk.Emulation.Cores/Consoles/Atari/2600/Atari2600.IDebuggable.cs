using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600 : IDebuggable
	{
		public IDictionary<string, int> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, int>
			{
				{ "A", Cpu.A },
				{ "X", Cpu.X },
				{ "Y", Cpu.Y },
				{ "S", Cpu.S },
				{ "PC", Cpu.PC },

				{ "Flag C", Cpu.FlagC ? 1 : 0 },
				{ "Flag Z", Cpu.FlagZ ? 1 : 0 },
				{ "Flag I", Cpu.FlagI ? 1 : 0 },
				{ "Flag D", Cpu.FlagD ? 1 : 0 },

				{ "Flag B", Cpu.FlagB ? 1 : 0 },
				{ "Flag V", Cpu.FlagV ? 1 : 0 },
				{ "Flag N", Cpu.FlagN ? 1 : 0 },
				{ "Flag T", Cpu.FlagT ? 1 : 0 }
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					Cpu.A = (byte)value;
					break;
				case "X":
					Cpu.X = (byte)value;
					break;
				case "Y":
					Cpu.Y = (byte)value;
					break;
				case "S":
					Cpu.S = (byte)value;
					break;
				case "PC":
					Cpu.PC = (ushort)value;
					break;
				case "Flag I":
					Cpu.FlagI = value > 0;
					break;
			}
		}

		public ITracer Tracer { get; private set; }

		public IMemoryCallbackSystem MemoryCallbacks { get; private set; }

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }
	}
}
