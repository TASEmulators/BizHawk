using System.Collections.Generic;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64 : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			// note: the approach this code takes is somewhat bug-prone
			const int STRUCT_SIZE_OCTETS = 32 * sizeof(long)
				+ sizeof(uint)
				+ sizeof(int)
				+ sizeof(long)
				+ sizeof(long)
				+ sizeof(int)
				+ sizeof(int)
				+ 32 * sizeof(uint)
				+ 32 * sizeof(long);
			var data0 = new byte[STRUCT_SIZE_OCTETS];
			api.getRegisters(data0);
			var data = data0.AsSpan();
			unsafe T ReadAndAdvance<T>(ref Span<byte> span)
				where T : unmanaged
			{
				var value = MemoryMarshal.Read<T>(span);
				span = span.Slice(sizeof(T));
				return value;
			}

			// warning: tracer magically relies on these register names!
			var ret = new Dictionary<string, RegisterValue>();
			void AddS64AsHalves(string name, long value)
			{
				ret.Add($"{name}_lo", unchecked((int) (value)));
				ret.Add($"{name}_hi", unchecked((int) (value >> 32)));
			}
			foreach (var name in GPRnames) AddS64AsHalves(name, ReadAndAdvance<long>(ref data));
			ret.Add("PC", ReadAndAdvance<int>(ref data));
			ret.Add("LL", ReadAndAdvance<int>(ref data));
			AddS64AsHalves("LO", ReadAndAdvance<long>(ref data));
			AddS64AsHalves("HI", ReadAndAdvance<long>(ref data));
			ret.Add("FCR0", ReadAndAdvance<int>(ref data));
			ret.Add("FCR31", ReadAndAdvance<int>(ref data));
			for (var i = 0; i < 32; i++) ret.Add($"CP0 REG{i}", ReadAndAdvance<uint>(ref data));
			for (var i = 0; i < 32; i++) AddS64AsHalves($"CP1 FGR REG{i}", ReadAndAdvance<long>(ref data));
			return ret;
		}

		public string[] GPRnames = new string[32]
		{
			"r0",
			"at",
			"v0", "v1",
			"a0", "a1", "a2", "a3",
			"t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7",
			"s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7",
			"t8", "t9",
			"k0", "k1",
			"gp",
			"sp",
			"s8",
			"ra"
		};

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		public IMemoryCallbackSystem MemoryCallbacks => _memoryCallbacks;

		private readonly MemoryCallbackSystem _memoryCallbacks = new MemoryCallbackSystem(new[] { "System Bus" });

		public bool CanStep(StepType type)
		{
			switch(type)
			{
				case StepType.Into:
					return false; // Implemented but disabled for now. Should be re-enabled once BizHawk supports mid-frame pausing.
				case StepType.Out:
					return false;
				case StepType.Over:
					return false;
			}

			return false;
		}

		[FeatureNotImplemented]
#pragma warning disable CA1065 // convention for [FeatureNotImplemented] is to throw NIE
		public long TotalExecutedCycles => throw new NotImplementedException();
#pragma warning restore CA1065

		public void Step(StepType type)
		{
			switch(type)
			{
				case StepType.Into:
					api.Step();
					break;
			}
		}

		private void SetBreakpointHandler()
		{
			var mcs = MemoryCallbacks;

			api.BreakpointHit += (address, type) => api.OnBreakpoint(new mupen64plusApi.BreakParams
			{
				_type = type,
				_addr = address,
				_mcs = mcs
			});
		}

		private void AddBreakpoint(IMemoryCallback callback)
		{
			switch(callback.Type)
			{
				case MemoryCallbackType.Read:
					api.SetBreakpoint(mupen64plusApi.BreakType.Read, callback.Address);
					break;

				case MemoryCallbackType.Write:
					api.SetBreakpoint(mupen64plusApi.BreakType.Write, callback.Address);
					break;

				case MemoryCallbackType.Execute:
					api.SetBreakpoint(mupen64plusApi.BreakType.Execute, callback.Address);
					break;
			}
		}

		private void RemoveBreakpoint(IMemoryCallback callback)
		{
			switch(callback.Type)
			{
				case MemoryCallbackType.Read:
					api.RemoveBreakpoint(mupen64plusApi.BreakType.Read, callback.Address);
					break;

				case MemoryCallbackType.Write:
					api.RemoveBreakpoint(mupen64plusApi.BreakType.Write, callback.Address);
					break;

				case MemoryCallbackType.Execute:
					api.RemoveBreakpoint(mupen64plusApi.BreakType.Execute, callback.Address);
					break;
			}
		}
	}
}
