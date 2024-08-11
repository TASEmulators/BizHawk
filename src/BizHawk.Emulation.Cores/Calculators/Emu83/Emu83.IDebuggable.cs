using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators.Emu83
{
	public partial class Emu83 : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			int[] regs = new int[12];
			LibEmu83.TI83_GetRegs(Context, regs);
			return new Dictionary<string, RegisterValue>
			{
				["AF"] = (ushort)regs[0],
				["BC"] = (ushort)regs[1],
				["DE"] = (ushort)regs[2],
				["HL"] = (ushort)regs[3],
				["AF'"] = (ushort)regs[4],
				["BC'"] = (ushort)regs[5],
				["DE'"] = (ushort)regs[6],
				["HL'"] = (ushort)regs[7],
				["IX"] = (ushort)regs[8],
				["IY"] = (ushort)regs[9],
				["PC"] = (ushort)regs[10],
				["SP"] = (ushort)regs[11],
			};
		}

		[FeatureNotImplemented]
		public void SetCpuRegister(string register, int value) => throw new NotImplementedException();

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		private long _callbackCycleCount = 0;
		public long TotalExecutedCycles => Math.Max(LibEmu83.TI83_GetCycleCount(Context), _callbackCycleCount);

		public IMemoryCallbackSystem MemoryCallbacks => _memoryCallbacks;

		private readonly MemoryCallbackSystem _memoryCallbacks = new(new[] { "System Bus" });

		private LibEmu83.MemoryCallback _readCallback;
		private LibEmu83.MemoryCallback _writeCallback;
		private LibEmu83.MemoryCallback _execCallback;

		private void InitMemoryCallbacks()
		{
			LibEmu83.MemoryCallback CreateCallback(MemoryCallbackFlags flags, Func<bool> getHasCBOfType)
			{
				var rawFlags = (uint)flags;
				return (address, cycleCount) =>
				{
					_callbackCycleCount = cycleCount;
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
			LibEmu83.TI83_SetMemoryCallback(Context, LibEmu83.MemoryCallbackId_t.MEM_CB_READ, MemoryCallbacks.HasReads ? _readCallback : null);
			LibEmu83.TI83_SetMemoryCallback(Context, LibEmu83.MemoryCallbackId_t.MEM_CB_WRITE, MemoryCallbacks.HasWrites ? _writeCallback : null);
			LibEmu83.TI83_SetMemoryCallback(Context, LibEmu83.MemoryCallbackId_t.MEM_CB_EXECUTE, MemoryCallbacks.HasExecutes ? _execCallback : null);
		}
	}
}
