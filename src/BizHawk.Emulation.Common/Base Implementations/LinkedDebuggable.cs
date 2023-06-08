using System;
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
		private readonly IEmulator[] _linkedCores;
		private readonly int _numCores;

		public LinkedDebuggable(IEmulator[] linkedCores, int numCores, MemoryCallbackSystem memoryCallbacks)
		{
			_linkedCores = linkedCores;
			_numCores = numCores;
			MemoryCallbacks = memoryCallbacks;
		}

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var ret = new List<KeyValuePair<string, RegisterValue>>();

			for (int i = 0; i < _numCores; i++)
			{
				ret.AddRange(_linkedCores[i].AsDebuggable().GetCpuFlagsAndRegisters()
					.Select(reg => new KeyValuePair<string, RegisterValue>($"P{i + 1} " + reg.Key, reg.Value)).ToList());
			}

			return ret.ToDictionary(pair => pair.Key, pair => pair.Value);
		}

		public void SetCpuRegister(string register, int value)
		{
			for (int i = 0; i < _numCores; i++)
			{
				if (register.StartsWithOrdinal($"P{i + 1} "))
				{
					_linkedCores[i].AsDebuggable().SetCpuRegister(register.Replace($"P{i + 1} ", ""), value);
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
