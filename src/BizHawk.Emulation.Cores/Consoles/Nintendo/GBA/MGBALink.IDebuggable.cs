using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBALink : IDebuggable
	{
		[FeatureNotImplemented]
		public IMemoryCallbackSystem MemoryCallbacks => throw new NotImplementedException();

		[FeatureNotImplemented]
		public long TotalExecutedCycles => throw new NotImplementedException();

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var linkedRegs = new IEnumerable<KeyValuePair<string, RegisterValue>>[_numCores];
			for (int i = 0; i < _numCores; i++)
			{
				linkedRegs[i] = _linkedCores[i].GetCpuFlagsAndRegisters()
					.Select(reg => new KeyValuePair<string, RegisterValue>($"{i + 1} " + reg.Key, reg.Value));
			}

			return _numCores switch
			{
				2 => linkedRegs[0].Union(linkedRegs[1]).ToDictionary(pair => pair.Key, pair => pair.Value),
				3 => linkedRegs[0].Union(linkedRegs[1]).Union(linkedRegs[2]).ToDictionary(pair => pair.Key, pair => pair.Value),
				4 => linkedRegs[0].Union(linkedRegs[1]).Union(linkedRegs[2]).Union(linkedRegs[3]).ToDictionary(pair => pair.Key, pair => pair.Value),
				_ => throw new Exception()
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			for (int i = 0; i < _numCores; i++)
			{
				if (register.StartsWith($"{i + 1} "))
				{
					_linkedCores[i].SetCpuRegister(register.Replace($"{i + 1} ", ""), value);
				}
			}
		}

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();
	}
}
