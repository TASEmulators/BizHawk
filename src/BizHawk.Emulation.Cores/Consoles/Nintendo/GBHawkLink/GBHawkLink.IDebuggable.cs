using System.Collections.Generic;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawkLink
{
	public partial class GBHawkLink : IDebuggable
	{
		private const string PFX_L = "Left ";

		private const string PFX_R = "Right ";

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			Dictionary<string, RegisterValue> dict = new();
			foreach (var reg in L.GetCpuFlagsAndRegisters()) dict[PFX_L + reg.Key] = reg.Value;
			foreach (var reg in R.GetCpuFlagsAndRegisters()) dict[PFX_R + reg.Key] = reg.Value;
			return dict;
		}

		public void SetCpuRegister(string register, int value)
		{
			if (register.StartsWithOrdinal(PFX_L)) L.SetCpuRegister(register.Substring(PFX_L.Length), value);
			else if (register.StartsWithOrdinal(PFX_R)) R.SetCpuRegister(register.Substring(PFX_R.Length), value);
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => (long)L.cpu.TotalExecutedCycles;
	}
}
