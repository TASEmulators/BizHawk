using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public partial class NDS : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var regs = new uint[2 * 16];
			_core.GetRegs(_console, regs);

			var ret = new Dictionary<string, RegisterValue>();
			for (var i = 0; i < 2; i++)
			{
				var ncpu = i == 0 ? 9 : 7;
				for (var j = 0; j < 16; j++)
				{
					ret["ARM" + ncpu + " r" + j] = regs[i * 16 + j];
				}
			}
			return ret;
		}

		public void SetCpuRegister(string register, int value)
		{
			if (register.Length is not (7 or 8))
			{
				throw new InvalidOperationException("Wrong String Length???");
			}
			var ncpu = int.Parse(register.Substring(3, 1));
			if (ncpu is not (9 or 7))
			{
				throw new InvalidOperationException("Invalid CPU???");
			}
			var index = int.Parse(register.Substring(6, register.Length - 6));
			if (index is < 0 or > 15)
			{
				throw new InvalidOperationException("Invalid Reg Index???");
			}
			_core.SetReg(_console, ncpu == 9 ? 0 : 1, index, value);
		}

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => CycleCount + _core.GetCallbackCycleOffset(_console);

		public IMemoryCallbackSystem MemoryCallbacks => _memoryCallbacks;

		// FIXME: internally the code actually just does this for either bus (probably don't want to bother adding support)
		private readonly MemoryCallbackSystem _memoryCallbacks = new([ "ARM9 System Bus" ]);

		private LibMelonDS.MemoryCallback _readCallback;
		private LibMelonDS.MemoryCallback _writeCallback;
		private LibMelonDS.MemoryCallback _execCallback;

		private void InitMemoryCallbacks()
		{
			LibMelonDS.MemoryCallback CreateCallback(MemoryCallbackFlags flags, Func<bool> getHasCBOfType)
			{
				var rawFlags = (uint)flags;
				return address =>
				{
					if (getHasCBOfType())
					{
						MemoryCallbacks.CallMemoryCallbacks(address, 0, rawFlags, "ARM9 System Bus");
					}
				};
			}

			_readCallback = CreateCallback(MemoryCallbackFlags.AccessRead, () => MemoryCallbacks.HasReads);
			_writeCallback = CreateCallback(MemoryCallbackFlags.AccessWrite, () => MemoryCallbacks.HasWrites);
			_execCallback = CreateCallback(MemoryCallbackFlags.AccessExecute, () => MemoryCallbacks.HasExecutes);

			_memoryCallbacks.ActiveChanged += SetMemoryCallbacks;
		}

		private void SetMemoryCallbacks()
		{
			_core.SetMemoryCallback(0, MemoryCallbacks.HasReads ? _readCallback : null);
			_core.SetMemoryCallback(1, MemoryCallbacks.HasWrites ? _writeCallback : null);
			_core.SetMemoryCallback(2, MemoryCallbacks.HasExecutes ? _execCallback : null);
		}
	}
}
