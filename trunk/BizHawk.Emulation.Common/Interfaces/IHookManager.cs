using System;
using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	public interface IDebugHookReference
	{
	}

	public interface IDebugHookCallback
	{
		void Event(int address);
	}

	public interface IDebugHookManager
	{
		IDebugHookReference Register(DebugEvent eventType, int address, int size, IDebugHookCallback cb);
		void Unregister(IDebugHookReference hookReference);
	}

	class BasicDebugHookManager
	{
		WorkingDictionary<DebugEvent, Bag<uint, Reference>> database = new WorkingDictionary<DebugEvent, Bag<uint, Reference>>();

		class Reference : IDebugHookReference
		{
			public IDebugHookCallback cb;
			public DebugEvent eventType;
			public int address, size;
		}

		public IDebugHookReference Register(DebugEvent eventType, int address, int size, IDebugHookCallback cb)
		{
			var r = new Reference();
			r.cb = cb;
			r.eventType = eventType;
			r.address = address;
			r.size = size;
			for (int i = 0; i < size; i++)
			{
				uint a = (uint)(address + i);
				database[eventType][a].Add(r);
			}
			return r;
		}

		public void Unregister(IDebugHookReference hookReference)
		{
			var hr = hookReference as Reference;
			for (int i = 0; i < hr.size; i++)
			{
				uint a = (uint)(hr.address + i);
				database[hr.eventType].Remove(a,hr);
			}
		}
	}
}
