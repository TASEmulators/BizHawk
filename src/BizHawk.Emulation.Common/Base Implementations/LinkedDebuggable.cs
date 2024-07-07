using System.Collections.Generic;
using System.Linq;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A generic linked implementation of IDebuggable that can be used by any link core
	/// </summary>
	/// <seealso cref="IDebuggable" />
	public class LinkedDebuggable : IDebuggable
	{
		private readonly IDebuggable[] _linkedCores;

		private readonly int _numCores;

		public LinkedDebuggable(IEnumerable<IEmulator> linkedCores, int numCores, MemoryCallbackSystem memoryCallbacks)
		{
			_linkedCores = linkedCores.Take(numCores).Select(static core => core.AsDebuggable()).ToArray();
			_numCores = numCores; // why though?
			MemoryCallbacks = memoryCallbacks;
		}

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			Dictionary<string, RegisterValue> dict = new();
			for (int i = 0; i < _numCores; i++)
			{
				var pfx = $"P{i + 1} ";
				foreach (var reg in _linkedCores[i].GetCpuFlagsAndRegisters()) dict[pfx + reg.Key] = reg.Value;
			}
			return dict;
		}

		public void SetCpuRegister(string register, int value)
		{
			for (int i = 0; i < _numCores; i++)
			{
				var pfx = $"P{i + 1} ";
				if (register.StartsWithOrdinal(pfx))
				{
					_linkedCores[i].SetCpuRegister(register.Substring(pfx.Length), value);
					return;
				}
			}
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; }

		public bool CanStep(StepType type) => false;

		[FeatureNotImplemented]
		public void Step(StepType type) => throw new NotImplementedException();

		[FeatureNotImplemented]
		public long TotalExecutedCycles => throw new NotImplementedException();
	}
}
