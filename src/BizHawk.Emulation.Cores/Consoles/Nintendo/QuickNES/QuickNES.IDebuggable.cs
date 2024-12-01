using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES
{
	public partial class QuickNES : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			int[] regs = new int[6];
			var ret = new Dictionary<string, RegisterValue>();
			QN.qn_get_cpuregs(Context, regs);
			ret["A"] = (byte)regs[0];
			ret["X"] = (byte)regs[1];
			ret["Y"] = (byte)regs[2];
			ret["SP"] = (ushort)regs[3];
			ret["PC"] = (ushort)regs[4];
			ret["P"] = (byte)regs[5];
			return ret;
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		public bool CanStep(StepType type) { return false; }

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		public IMemoryCallbackSystem MemoryCallbacks
		{
			[FeatureNotImplemented]
			get => throw new NotImplementedException();
		}

		[FeatureNotImplemented]
		public long TotalExecutedCycles => throw new NotImplementedException();
	}
}
