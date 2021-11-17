using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Gameboy
{
	public partial class GambatteLink : IDebuggable
	{
		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var linkedRegs = new IEnumerable<KeyValuePair<string, RegisterValue>>[_numCores];
			// for some reason a simple for loop screws up, so this hack has to be used
			switch (_numCores)
			{
				case 4:
					linkedRegs[P4] = _linkedCores[P4].GetCpuFlagsAndRegisters()
						.Select(reg => new KeyValuePair<string, RegisterValue>("P4 " + reg.Key, reg.Value));
					goto case 3; // hacky fallthrough
				case 3:
					linkedRegs[P3] = _linkedCores[P3].GetCpuFlagsAndRegisters()
						.Select(reg => new KeyValuePair<string, RegisterValue>("P3 " + reg.Key, reg.Value));
					goto case 2; // hacky fallthrough
				case 2:
					linkedRegs[P2] = _linkedCores[P2].GetCpuFlagsAndRegisters()
						.Select(reg => new KeyValuePair<string, RegisterValue>("P2 " + reg.Key, reg.Value));
					linkedRegs[P1] = _linkedCores[P1].GetCpuFlagsAndRegisters()
						.Select(reg => new KeyValuePair<string, RegisterValue>("P1 " + reg.Key, reg.Value));
					break;
				default:
					throw new Exception();
			}

			return _numCores switch
			{
				2 => linkedRegs[P1].Union(linkedRegs[P2]).ToDictionary(pair => pair.Key, pair => pair.Value),
				3 => linkedRegs[P1].Union(linkedRegs[P2]).Union(linkedRegs[P3]).ToDictionary(pair => pair.Key, pair => pair.Value),
				4 => linkedRegs[P1].Union(linkedRegs[P2]).Union(linkedRegs[P3]).Union(linkedRegs[P4]).ToDictionary(pair => pair.Key, pair => pair.Value),
				_ => throw new Exception()
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			for (int i = 0; i < _numCores; i++)
			{
				if (register.StartsWith($"P{i + 1} "))
				{
					_linkedCores[i].SetCpuRegister(register.Replace($"P{i + 1} ", ""), value);
				}
			}
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; } = new MemoryCallbackSystem(new[] { "System Bus" });

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		[FeatureNotImplemented]
		public long TotalExecutedCycles => throw new NotImplementedException();

		private readonly MemoryCallbackSystem _memorycallbacks = new MemoryCallbackSystem(new[] { "System Bus" });
	}
}
