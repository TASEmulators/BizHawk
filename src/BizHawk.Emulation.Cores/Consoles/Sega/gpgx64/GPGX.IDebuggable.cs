using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var regs = new LibGPGX.RegisterInfo[Core.gpgx_getmaxnumregs()];
			var n = Core.gpgx_getregs(regs);
			if (n > regs.Length)
			{
				throw new InvalidOperationException("A buffer overrun has occured!");
			}

			var ret = new Dictionary<string, RegisterValue>();
			using (_elf.EnterExit())
			{
				for (var i = 0; i < n; i++)
				{
					// el hacko
					var name = Marshal.PtrToStringAnsi(regs[i].Name);
					byte size = 32;
					if (name!.Contains("68K SR") || name.StartsWithOrdinal("Z80"))
					{
						size = 16;
					}

					ret[name] = new RegisterValue((ulong)regs[i].Value, size);
				}
			}

			return ret;
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
			=> throw new NotImplementedException();

		public IMemoryCallbackSystem MemoryCallbacks
		{
			get
			{
				if (SystemId == VSystemID.Raw.GEN)
				{
					return _memoryCallbacks;
				}

#pragma warning disable CA1065 // I guess this is like a conditional [FeatureNotImplemented], for which the convention is to throw NIE
				throw new NotImplementedException();
#pragma warning restore CA1065
			}
		}

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		[FeatureNotImplemented]
#pragma warning disable CA1065 // convention for [FeatureNotImplemented] is to throw NIE
		public long TotalExecutedCycles => throw new NotImplementedException();
#pragma warning restore CA1065

		private readonly MemoryCallbackSystem _memoryCallbacks = new([ "M68K BUS" ]);

		private LibGPGX.mem_cb ExecCallback;
		private LibGPGX.mem_cb ReadCallback;
		private LibGPGX.mem_cb WriteCallback;
		private readonly LibGPGX.CDCallback CDCallback;

		private void InitMemCallbacks()
		{
			ExecCallback = (addr, val) =>
			{
				if (MemoryCallbacks.HasExecutes)
				{
					const uint flags = (uint)MemoryCallbackFlags.AccessExecute;
					val = MemoryCallbacks.CallMemoryCallbacks(addr, val, flags, "M68K BUS");
				}
				return val;
			};
			ReadCallback = (addr, val) =>
			{
				if (MemoryCallbacks.HasReads)
				{
					const uint flags = (uint)MemoryCallbackFlags.AccessRead;
					val = MemoryCallbacks.CallMemoryCallbacks(addr, val, flags, "M68K BUS");
				}
				return val;
			};
			WriteCallback = (addr, val) =>
			{
				if (MemoryCallbacks.HasWrites)
				{
					const uint flags = (uint)MemoryCallbackFlags.AccessWrite;
					val = MemoryCallbacks.CallMemoryCallbacks(addr, val, flags, "M68K BUS");
				}
				return val;
			};
			_memoryCallbacks.ActiveChanged += RefreshMemCallbacks;
		}

		private void RefreshMemCallbacks()
		{
			Core.gpgx_set_mem_callback(
				_memoryCallbacks.HasReads ? ReadCallback : null,
				_memoryCallbacks.HasWrites ? WriteCallback : null,
				_memoryCallbacks.HasExecutes ? ExecCallback : null);
		}

		private void KillMemCallbacks()
			=> Core.gpgx_set_mem_callback(null, null, null);
	}
}
