using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64 : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				{ "A", board.cpu.A },
				{ "X", board.cpu.X },
				{ "Y", board.cpu.Y },
				{ "S", board.cpu.S },
				{ "PC", board.cpu.PC },
				{ "Flag C", board.cpu.FlagC },
				{ "Flag Z", board.cpu.FlagZ },
				{ "Flag I", board.cpu.FlagI },
				{ "Flag D", board.cpu.FlagD },
				{ "Flag B", board.cpu.FlagB },
				{ "Flag V", board.cpu.FlagV },
				{ "Flag N", board.cpu.FlagN },
				{ "Flag T", board.cpu.FlagT }
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					board.cpu.A = (byte)value;
					break;
				case "X":
					board.cpu.X = (byte)value;
					break;
				case "Y":
					board.cpu.Y = (byte)value;
					break;
				case "S":
					board.cpu.S = (byte)value;
					break;
				case "PC":
					board.cpu.PC = (ushort)value;
					break;
			}
		}

		public ITracer Tracer
		{
			[FeatureNotImplemented]
			get { throw new NotImplementedException(); }
		}

		public IMemoryCallbackSystem MemoryCallbacks
		{
			[FeatureNotImplemented]
			get { throw new NotImplementedException(); }
		}

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		public bool CanStep(StepType type) { return false; }
	}
}
