using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	partial class VirtualJaguar : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			uint[] regs = new uint[18];
			_core.GetRegisters(regs);

			var ret = new Dictionary<string, RegisterValue>();
			for (int i = 0; i < 8; i++)
			{
				ret[$"D{i}"] = regs[i];
				ret[$"A{i}"] = regs[8 + i];
			}
			ret["PC"] = regs[16];
			ret["SR"] = regs[17];
			return ret;
		}

		public void SetCpuRegister(string register, int value)
			=> _core.SetRegister((LibVirtualJaguar.M68KRegisters)Enum.Parse(typeof(LibVirtualJaguar.M68KRegisters), register.ToUpperInvariant()), value);

		public bool CanStep(StepType type)
			=> false;

		[FeatureNotImplemented]
		public void Step(StepType type)
			=> throw new NotImplementedException();

		[FeatureNotImplemented]
		public long TotalExecutedCycles
			=> throw new NotImplementedException();

		public IMemoryCallbackSystem MemoryCallbacks => _memoryCallbacks;

		private readonly MemoryCallbackSystem _memoryCallbacks = new(new[] { "System Bus" });

		private LibVirtualJaguar.MemoryCallback _readCallback;
		private LibVirtualJaguar.MemoryCallback _writeCallback;
		private LibVirtualJaguar.MemoryCallback _execCallback;

		private void InitMemoryCallbacks()
		{
			LibVirtualJaguar.MemoryCallback CreateCallback(MemoryCallbackFlags flags, Func<bool> getHasCBOfType)
			{
				var rawFlags = (uint)flags;
				return address =>
				{
					if (getHasCBOfType())
					{
						MemoryCallbacks.CallMemoryCallbacks(address, 0, rawFlags, "System Bus");
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
