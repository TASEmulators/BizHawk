using System.Collections.Generic;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	partial class VirtualJaguar : IDebuggable
	{
		public unsafe IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			// 148 registers, oh my
			uint* regs = stackalloc uint[18 + 32 + 32 + 32 + 32 + 2];
			_core.GetRegisters((IntPtr)regs);

			var ret = new Dictionary<string, RegisterValue>();
			// M68K data regs
			for (int i = 0; i < 8; i++)
			{
				ret[$"M68K D{i}"] = regs[i];
			}
			// M68K address regs
			for (int i = 0; i < 8; i++)
			{
				ret[$"M68K A{i}"] = regs[8 + i];
			}
			ret["M68K PC"] = regs[16];
			ret["M68K SR"] = regs[17];
			// these registers aren't really 0-63, but it's two banks of 32 registers
			for (int i = 0; i < 64; i++)
			{
				ret[$"GPU R{i}"] = regs[18 + i];
			}
			ret["GPU PC"] = regs[146];
			for (int i = 0; i < 64; i++)
			{
				ret[$"DSP R{i}"] = regs[82 + i];
			}
			ret["DSP PC"] = regs[147];

			return ret;
		}

		public void SetCpuRegister(string register, int value)
		{
			register = register.ToUpperInvariant();
			if (register.StartsWithOrdinal("M68K "))
			{
				var reg = Enum.Parse(typeof(LibVirtualJaguar.M68KRegisters), register.Remove(0, 5));
				_core.SetRegister((int)reg, value);
			}
			else if (register.StartsWithOrdinal("GPU ") || register.StartsWithOrdinal("DSP "))
			{
				bool gpu = register.StartsWithOrdinal("GPU ");
				var regName = register.Remove(0, 4);

				if (regName == "PC")
				{
					_core.SetRegister(gpu ? 146 : 147, value);
				}
				else if (regName.StartsWith('R'))
				{
					var offset = gpu ? 18 : 82;
					var reg = int.Parse(regName.Remove(0, 1));
					if (reg > 63)
					{
						throw new ArgumentException("Invalid register", nameof(register));
					}
					_core.SetRegister(offset + reg, value);
				}
				else
				{
					throw new ArgumentException("Invalid register", nameof(register));
				}
			}
			else
			{
				throw new ArgumentException("Invalid register", nameof(register));
			}
		}

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
			_core.SetMemoryCallbacks(
				MemoryCallbacks.HasReads ? _readCallback : null,
				MemoryCallbacks.HasWrites ? _writeCallback : null,
				MemoryCallbacks.HasExecutes ? _execCallback : null);
		}
	}
}
