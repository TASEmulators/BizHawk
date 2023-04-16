using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class NDS : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			uint[] regs = new uint[2 * 16];
			_core.GetRegs(regs);

			var ret = new Dictionary<string, RegisterValue>();
			for (int i = 0; i < 2; i++)
			{
				int ncpu = i == 0 ? 9 : 7;
				for (int j = 0; j < 16; j++)
				{
					ret["ARM" + ncpu + " r" + j] = regs[i * 16 + j];
				}
			}
			return ret;
		}

		public void SetCpuRegister(string register, int value)
		{
			if (register.Length != 7 && register.Length != 8)
			{
				throw new InvalidOperationException("Wrong String Length???");
			}
			int ncpu = int.Parse(register.Substring(3, 1));
			if (ncpu != 9 && ncpu != 7)
			{
				throw new InvalidOperationException("Invalid CPU???");
			}
			int index = int.Parse(register.Substring(6, register.Length - 6));
			if (index < 0 || index > 15)
			{
				throw new InvalidOperationException("Invalid Reg Index???");
			}
			_core.SetReg(ncpu == 9 ? 0 : 1, index, value);
		}

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => CycleCount + _core.GetCallbackCycleOffset();

		public IMemoryCallbackSystem MemoryCallbacks => _memorycallbacks;

		private readonly MemoryCallbackSystem _memorycallbacks = new(new[] { "System Bus" });

		private LibMelonDS.MemoryCallback _readcb;
		private LibMelonDS.MemoryCallback _writecb;
		private LibMelonDS.MemoryCallback _execcb;

		private void InitMemoryCallbacks()
		{
			LibMelonDS.MemoryCallback CreateCallback(MemoryCallbackFlags flags, Func<bool> getHasCBOfType)
			{
				var rawFlags = (uint)flags;
				return (address) =>
				{
					if (getHasCBOfType())
					{
						MemoryCallbacks.CallMemoryCallbacks(address, 0, rawFlags, "System Bus");
					}
				};
			}

			_readcb = CreateCallback(MemoryCallbackFlags.AccessRead, () => MemoryCallbacks.HasReads);
			_writecb = CreateCallback(MemoryCallbackFlags.AccessWrite, () => MemoryCallbacks.HasWrites);
			_execcb = CreateCallback(MemoryCallbackFlags.AccessExecute, () => MemoryCallbacks.HasExecutes);

			_memorycallbacks.ActiveChanged += SetMemoryCallbacks;
		}

		private void SetMemoryCallbacks()
		{
			_core.SetMemoryCallback(0, MemoryCallbacks.HasReads ? _readcb : null);
			_core.SetMemoryCallback(1, MemoryCallbacks.HasWrites ? _writecb : null);
			_core.SetMemoryCallback(2, MemoryCallbacks.HasExecutes ? _execcb : null);
		}
	}
}
