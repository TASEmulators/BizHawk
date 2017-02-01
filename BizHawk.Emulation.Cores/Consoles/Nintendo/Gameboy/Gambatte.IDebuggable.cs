using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class Gameboy : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			int[] data = new int[10];
			LibGambatte.gambatte_getregs(GambatteState, data);

			return new Dictionary<string, RegisterValue>
			{
				{ "PC", (ushort)(data[(int)LibGambatte.RegIndicies.PC] & 0xffff) },
				{ "SP", (ushort)(data[(int)LibGambatte.RegIndicies.SP] & 0xffff) },
				{ "A", (byte)(data[(int)LibGambatte.RegIndicies.A] & 0xff) },
				{ "B", (byte)(data[(int)LibGambatte.RegIndicies.B] & 0xff) },
				{ "C", (byte)(data[(int)LibGambatte.RegIndicies.C] & 0xff) },
				{ "D", (byte)(data[(int)LibGambatte.RegIndicies.D] & 0xff) },
				{ "E", (byte)(data[(int)LibGambatte.RegIndicies.E] & 0xff) },
				{ "F", (byte)(data[(int)LibGambatte.RegIndicies.F] & 0xff) },
				{ "H", (byte)(data[(int)LibGambatte.RegIndicies.H] & 0xff) },
				{ "L", (byte)(data[(int)LibGambatte.RegIndicies.L] & 0xff) }
			};
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value)
		{
			throw new NotImplementedException();
		}

		public bool CanStep(StepType type)
		{
			return false;
		}

		[FeatureNotImplemented]
		public void Step(StepType type) { throw new NotImplementedException(); }

		[FeatureNotImplemented]
		public int TotalExecutedCycles
		{
			get { throw new NotImplementedException(); }
		}

		public IMemoryCallbackSystem MemoryCallbacks
		{
			get { return _memorycallbacks; }
		}

		private LibGambatte.MemoryCallback readcb;
		private LibGambatte.MemoryCallback writecb;
		private LibGambatte.MemoryCallback execcb;

		private MemoryCallbackSystem _memorycallbacks = new MemoryCallbackSystem();

		/// <summary>
		/// for use in dual core
		/// </summary>
		/// <param name="ics"></param>
		internal void ConnectMemoryCallbackSystem(MemoryCallbackSystem mcs)
		{
			_memorycallbacks = mcs;
		}

		private void InitMemoryCallbacks()
		{
			readcb = (addr) => MemoryCallbacks.CallReads(addr);
			writecb = (addr) => MemoryCallbacks.CallWrites(addr);
			execcb = (addr) => MemoryCallbacks.CallExecutes(addr);
			_memorycallbacks.ActiveChanged += RefreshMemoryCallbacks;
		}

		private void RefreshMemoryCallbacks()
		{
			var mcs = MemoryCallbacks;

			LibGambatte.gambatte_setreadcallback(GambatteState, mcs.HasReads ? readcb : null);
			LibGambatte.gambatte_setwritecallback(GambatteState, mcs.HasWrites ? writecb : null);
			LibGambatte.gambatte_setexeccallback(GambatteState, mcs.HasExecutes ? execcb : null);
		}
	}
}
