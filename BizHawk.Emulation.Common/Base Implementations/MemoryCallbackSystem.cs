using System;
using System.Collections;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This is a generic implementation of IMemoryCallbackSystem
	/// that can be used by used by any core
	/// </summary>
	/// <seealso cref="IMemoryCallbackSystem" />
	public class MemoryCallbackSystem : IMemoryCallbackSystem
	{
		public MemoryCallbackSystem()
		{
			ExecuteCallbacksAvailable = true;
		}

		private readonly List<IMemoryCallback> Reads = new List<IMemoryCallback>();
		private readonly List<IMemoryCallback> Writes = new List<IMemoryCallback>();
		private readonly List<IMemoryCallback> Execs = new List<IMemoryCallback>();

		private bool _empty = true;

		private bool _hasReads;
		private bool _hasWrites;
		private bool _hasExecutes;

		public bool ExecuteCallbacksAvailable { get; set; }

		public void Add(IMemoryCallback callback)
		{
			switch (callback.Type)
			{
				case MemoryCallbackType.Execute:
					Execs.Add(callback);
					_hasExecutes = true;
					break;
				case MemoryCallbackType.Read:
					Reads.Add(callback);
					_hasReads = true;
					break;
				case MemoryCallbackType.Write:
					Writes.Add(callback);
					_hasWrites = true;
					break;
			}

			if (_empty)
			{
				Changes();
			}

			_empty = false;
		}

		private static void Call(List<IMemoryCallback> cbs, uint addr)
		{
			for (int i = 0; i < cbs.Count; i++)
			{
				if (!cbs[i].Address.HasValue || cbs[i].Address == (addr & cbs[i].AddressMask))
					cbs[i].Callback();
			}
		}

		public void CallReads(uint addr)
		{
			if (_hasReads)
			{
				Call(Reads, addr);
			}
		}

		public void CallWrites(uint addr)
		{
			if (_hasWrites)
			{
				Call(Writes, addr);
			}
		}

		public void CallExecutes(uint addr)
		{
			if (_hasExecutes)
			{
				Call(Execs, addr);
			}
		}

		public bool HasReads
		{
			get { return _hasReads; }
		}

		public bool HasWrites
		{
			get { return _hasWrites; }
		}

		public bool HasExecutes
		{
			get { return _hasExecutes; }
		}

		private void UpdateHasVariables()
		{
			_hasReads = Reads.Count > 0;
			_hasWrites = Reads.Count > 0;
			_hasExecutes = Reads.Count > 0;
		}

		private int RemoveInternal(Action action)
		{
			int ret = 0;
			ret += Reads.RemoveAll(imc => imc.Callback == action);
			ret += Writes.RemoveAll(imc => imc.Callback == action);
			ret += Execs.RemoveAll(imc => imc.Callback == action);

			UpdateHasVariables();

			return ret;
		}

		public void Remove(Action action)
		{
			if (RemoveInternal(action) > 0)
			{
				bool newEmpty = !HasReads && !HasWrites && !HasExecutes;
				if (newEmpty != _empty)
					Changes();
				_empty = newEmpty;
			}
		}

		public void RemoveAll(IEnumerable<Action> actions)
		{
			bool changed = false;
			foreach (var action in actions)
			{
				changed |= RemoveInternal(action) > 0;
			}
			if (changed)
			{
				bool newEmpty = !HasReads && !HasWrites && !HasExecutes;
				if (newEmpty != _empty)
					Changes();
				_empty = newEmpty;
			}

			UpdateHasVariables();
		}

		public void Clear()
		{
			Reads.Clear();
			Writes.Clear();
			Execs.Clear();
			if (!_empty)
				Changes();
			_empty = true;

			UpdateHasVariables();
		}

		public delegate void ActiveChangedEventHandler();
		public event ActiveChangedEventHandler ActiveChanged;

		private void Changes()
		{
			if (ActiveChanged != null)
			{
				ActiveChanged();
			}
		}

		public IEnumerator<IMemoryCallback> GetEnumerator()
		{
			foreach (var imc in Reads)
			{
				yield return imc;
			}

			foreach (var imc in Writes)
			{
				yield return imc;
			}

			foreach (var imc in Execs)
			{
				yield return imc;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (var imc in Reads)
			{
				yield return imc;
			}

			foreach (var imc in Writes)
			{
				yield return imc;
			}

			foreach (var imc in Execs)
			{
				yield return imc;
			}
		}
	}

	public class MemoryCallback : IMemoryCallback
	{
		public MemoryCallback(MemoryCallbackType type, string name, Action callback, uint? address, uint? mask)
		{
			if (type == MemoryCallbackType.Execute && !address.HasValue)
			{
				throw new InvalidOperationException("When assigning an execute callback, an address must be specified");
			}

			Type = type;
			Name = name;
			Callback = callback;
			Address = address;
			AddressMask = (mask.HasValue ? mask : 0xFFFFFFFF);
		}

		public MemoryCallbackType Type { get; private set; }
		public string Name { get; private set; }
		public Action Callback { get; private set; }
		public uint? Address { get; private set; }
		public uint? AddressMask { get; private set; }
	}
}
