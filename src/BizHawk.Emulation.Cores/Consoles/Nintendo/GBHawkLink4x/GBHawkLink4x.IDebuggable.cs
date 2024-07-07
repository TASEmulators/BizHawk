using System.Collections.Generic;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x
{
	public partial class GBHawkLink4x : IDebuggable
	{
		private const string PFX_A = "A ";

		private const string PFX_B = "B ";

		private const string PFX_C = "C ";

		private const string PFX_D = "D ";

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			Dictionary<string, RegisterValue> dict = new();
			foreach (var reg in A.GetCpuFlagsAndRegisters()) dict[PFX_A + reg.Key] = reg.Value;
			foreach (var reg in B.GetCpuFlagsAndRegisters()) dict[PFX_B + reg.Key] = reg.Value;
			foreach (var reg in C.GetCpuFlagsAndRegisters()) dict[PFX_C + reg.Key] = reg.Value;
			foreach (var reg in D.GetCpuFlagsAndRegisters()) dict[PFX_D + reg.Key] = reg.Value;
			return dict;
		}

		public void SetCpuRegister(string register, int value)
		{
			if (register.StartsWithOrdinal(PFX_A)) A.SetCpuRegister(register.Substring(PFX_A.Length), value);
			else if (register.StartsWithOrdinal(PFX_B)) B.SetCpuRegister(register.Substring(PFX_B.Length), value);
			else if (register.StartsWithOrdinal(PFX_C)) C.SetCpuRegister(register.Substring(PFX_C.Length), value);
			else if (register.StartsWithOrdinal(PFX_D)) D.SetCpuRegister(register.Substring(PFX_D.Length), value);
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => (long)A.cpu.TotalExecutedCycles;
	}
}
