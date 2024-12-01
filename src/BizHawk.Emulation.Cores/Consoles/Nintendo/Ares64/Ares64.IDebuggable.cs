using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64
{
	public partial class Ares64 : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var ret = new Dictionary<string, RegisterValue>();
			var data = new ulong[32 + 3]; // GPRs, lo, hi, pc (todo: other regs)
			_core.GetRegisters(data);

			for (int i = 0; i < 32; i++)
			{
				ret.Add(_GPRnames[i], data[i]);
			}

			ret.Add("LO", data[32]);
			ret.Add("HI", data[33]);
			ret.Add("PC", (uint)data[34]);
			// FIXME: the PC register is actually 64-bits (although in practice it will only ever be 32-bits)
			// Debugger UI doesn't like it as 64 bits, hence the uint cast

			return ret;
		}

		private readonly string[] _GPRnames = new string[32]
		{
			"R0",
			"AT",
			"V0", "V1",
			"A0", "A1", "A2", "A3",
			"T0", "T1", "T2", "T3", "T4", "T5", "T6", "T7",
			"S0", "S1", "S2", "S3", "S4", "S5", "S6", "S7",
			"T8", "T9",
			"K0", "K1",
			"GP",
			"SP",
			"S8",
			"RA",
		};

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value) => throw new NotImplementedException();

		[FeatureNotImplemented]
		public IMemoryCallbackSystem MemoryCallbacks => throw new NotImplementedException();

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public long TotalExecutedCycles => throw new NotImplementedException();

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();
	}
}
