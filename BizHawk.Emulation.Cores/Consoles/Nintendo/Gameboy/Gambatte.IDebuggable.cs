using System;
using System.Collections.Generic;

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
				["PC"] = (ushort)(data[(int)LibGambatte.RegIndicies.PC] & 0xffff),
				["SP"] = (ushort)(data[(int)LibGambatte.RegIndicies.SP] & 0xffff),
				["A"] = (byte)(data[(int)LibGambatte.RegIndicies.A] & 0xff),
				["B"] = (byte)(data[(int)LibGambatte.RegIndicies.B] & 0xff),
				["C"] = (byte)(data[(int)LibGambatte.RegIndicies.C] & 0xff),
				["D"] = (byte)(data[(int)LibGambatte.RegIndicies.D] & 0xff),
				["E"] = (byte)(data[(int)LibGambatte.RegIndicies.E] & 0xff),
				["F"] = (byte)(data[(int)LibGambatte.RegIndicies.F] & 0xff),
				["H"] = (byte)(data[(int)LibGambatte.RegIndicies.H] & 0xff),
				["L"] = (byte)(data[(int)LibGambatte.RegIndicies.L] & 0xff)
			};
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value) => throw new NotImplementedException();

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		public long TotalExecutedCycles => Math.Max((long)_cycleCount, (long)callbackCycleCount);

		private MemoryCallbackSystem _memorycallbacks = new MemoryCallbackSystem(new[] { "System Bus" });
		public IMemoryCallbackSystem MemoryCallbacks => _memorycallbacks;

		private LibGambatte.MemoryCallback _readcb;
		private LibGambatte.MemoryCallback _writecb;
		private LibGambatte.MemoryCallback _execcb;

		private void ReadCallback(uint address, ulong cycleOffset)
		{
			callbackCycleCount = _cycleCount + cycleOffset;
			uint flags = (uint)(MemoryCallbackFlags.AccessRead);
			MemoryCallbacks.CallMemoryCallbacks(address, 0, flags, "System Bus");
		}

		private void WriteCallback(uint address, ulong cycleOffset)
		{
			callbackCycleCount = _cycleCount + cycleOffset;
			uint flags = (uint)(MemoryCallbackFlags.AccessWrite);
			MemoryCallbacks.CallMemoryCallbacks(address, 0, flags,"System Bus");
		}

		private void ExecCallback(uint address, ulong cycleOffset)
		{
			callbackCycleCount = _cycleCount + cycleOffset;
			uint flags = (uint)(MemoryCallbackFlags.AccessExecute);
			MemoryCallbacks.CallMemoryCallbacks(address, 0, flags, "System Bus");
		}

		/// <summary>
		/// for use in dual core
		/// </summary>
		internal void ConnectMemoryCallbackSystem(MemoryCallbackSystem mcs)
		{
			_memorycallbacks = mcs;
		}

		private void InitMemoryCallbacks()
		{
			_readcb = new LibGambatte.MemoryCallback(ReadCallback);
			_writecb = new LibGambatte.MemoryCallback(WriteCallback);
			_execcb = new LibGambatte.MemoryCallback(ExecCallback);
			_memorycallbacks.ActiveChanged += RefreshMemoryCallbacks;
		}

		private void RefreshMemoryCallbacks()
		{
			var mcs = MemoryCallbacks;

			LibGambatte.gambatte_setreadcallback(GambatteState, mcs.HasReads ? _readcb : null);
			LibGambatte.gambatte_setwritecallback(GambatteState, mcs.HasWrites ? _writecb : null);
			LibGambatte.gambatte_setexeccallback(GambatteState, mcs.HasExecutes ? _execcb : null);
		}
	}
}
