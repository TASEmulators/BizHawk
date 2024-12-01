using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class LibsnesCore : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			Api.QUERY_peek_cpu_regs(out var regs);

			bool fn = (regs.p & 0x80) != 0;
			bool fv = (regs.p & 0x40) != 0;
			bool fm = (regs.p & 0x20) != 0;
			bool fx = (regs.p & 0x10) != 0;
			bool fd = (regs.p & 0x08) != 0;
			bool fi = (regs.p & 0x04) != 0;
			bool fz = (regs.p & 0x02) != 0;
			bool fc = (regs.p & 0x01) != 0;

			return new Dictionary<string, RegisterValue>
			{
				["PC"] = regs.pc,
				["A"] = regs.a,
				["X"] = regs.x,
				["Y"] = regs.y,
				["S"] = regs.s,
				["D"] = regs.d,
				["Vector"] = regs.vector,
				["P"] = regs.p,
				["Flag N"] = fn,
				["Flag V"] = fv,
				["Flag M"] = fm,
				["Flag X"] = fx,
				["Flag D"] = fd,
				["Flag I"] = fi,
				["Flag Z"] = fz,
				["Flag C"] = fc,
				["V"] = regs.v,
				["H"] = regs.h
			};
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		public bool CanStep(StepType type)
		{
			return false;
		}

		[FeatureNotImplemented]
		public void Step(StepType type)
		{
			throw new NotImplementedException();
		}

		[FeatureNotImplemented]
		public long TotalExecutedCycles => throw new NotImplementedException();
	}
}
