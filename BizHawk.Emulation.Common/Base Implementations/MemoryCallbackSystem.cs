using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This is a generic implementation of IMemoryCallbackSystem
	/// that can be used by used by any core
	/// </summary>
	/// <seealso cref="IMemoryCallbackSystem" />
	public class MemoryCallbackSystem : IMemoryCallbackSystem
	{
		public MemoryCallbackSystem(string[] availableScopes)
		{
			if (availableScopes == null)
			{
				availableScopes = new[] { "System Bus" };
			}

			AvailableScopes = availableScopes;
			ExecuteCallbacksAvailable = true;

			_reads.CollectionChanged += OnCollectionChanged;
			_writes.CollectionChanged += OnCollectionChanged;
			_execs.CollectionChanged += OnCollectionChanged;
		}

		private readonly ObservableCollection<IMemoryCallback> _reads = new ObservableCollection<IMemoryCallback>();
		private readonly ObservableCollection<IMemoryCallback> _writes = new ObservableCollection<IMemoryCallback>();
		private readonly ObservableCollection<IMemoryCallback> _execs = new ObservableCollection<IMemoryCallback>();

		private bool _hasReads;
		private bool _hasWrites;
		private bool _hasExecutes;

		public bool ExecuteCallbacksAvailable { get; }

		public string[] AvailableScopes { get; }

		public void Add(IMemoryCallback callback)
		{
			if (!AvailableScopes.Contains(callback.Scope))
			{
				throw new InvalidOperationException($"{callback.Scope} is not currently supported for callbacks");
			}

			switch (callback.Type)
			{
				case MemoryCallbackType.Execute:
					_execs.Add(callback);
					break;
				case MemoryCallbackType.Read:
					_reads.Add(callback);
					break;
				case MemoryCallbackType.Write:
					_writes.Add(callback);
					break;
			}

			if (UpdateHasVariables())
			{
				Changes();
			}
		}

		private static void Call(ObservableCollection<IMemoryCallback> cbs, uint addr, string scope)
		{
			for (int i = 0; i < cbs.Count; i++)
			{
				if (!cbs[i].Address.HasValue || (cbs[i].Scope == scope && cbs[i].Address == (addr & cbs[i].AddressMask)))
				{
					cbs[i].Callback();
				}
			}
		}

		public void CallReads(uint addr, string scope)
		{
			if (_hasReads)
			{
				Call(_reads, addr, scope);
			}
		}

		public void CallWrites(uint addr, string scope)
		{
			if (_hasWrites)
			{
				Call(_writes, addr, scope);
			}
		}

		public void CallExecutes(uint addr, string scope)
		{
			if (_hasExecutes)
			{
				Call(_execs, addr, scope);
			}
		}

		public bool HasReads => _hasReads;

		public bool HasWrites => _hasWrites;

		public bool HasExecutes => _hasExecutes;

		public bool HasReadsForScope(string scope)
		{
			return _reads.Where(e => e.Scope == scope).Any();
		}

		public bool HasWritesForScope(string scope)
		{
			return _writes.Where(e => e.Scope == scope).Any();
		}

		public bool HasExecutesForScope(string scope)
		{
			return _execs.Where(e => e.Scope == scope).Any();
		}

		private bool UpdateHasVariables()
		{
			bool hadReads = _hasReads;
			bool hadWrites = _hasWrites;
			bool hadExecutes = _hasExecutes;

			_hasReads = _reads.Count > 0;
			_hasWrites = _writes.Count > 0;
			_hasExecutes = _execs.Count > 0;

			return (_hasReads != hadReads || _hasWrites != hadWrites || _hasExecutes != hadExecutes);
		}

		private int RemoveInternal(Action action)
		{
			var readsToRemove = _reads.Where(imc => imc.Callback == action).ToList();
			var writesToRemove = _writes.Where(imc => imc.Callback == action).ToList();
			var execsToRemove = _execs.Where(imc => imc.Callback == action).ToList();

			foreach (var read in readsToRemove)
			{
				_reads.Remove(read);
			}

			foreach (var write in writesToRemove)
			{
				_writes.Remove(write);
			}

			foreach (var exec in execsToRemove)
			{
				_execs.Remove(exec);
			}

			return readsToRemove.Count + writesToRemove.Count + execsToRemove.Count;
		}

		public void Remove(Action action)
		{
			if (RemoveInternal(action) > 0)
			{
				if (UpdateHasVariables())
				{
					Changes();
				}
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
				if (UpdateHasVariables())
				{
					Changes();
				}
			}
		}

		public void Clear()
		{
			// Remove one-by-one to avoid NotifyCollectionChangedAction.Reset events.
			for (int i = _reads.Count - 1; i >= 0; i--)
			{
				_reads.RemoveAt(i);
			}

			for (int i = _reads.Count - 1; i >= 0; i--)
			{
				_writes.RemoveAt(i);
			}

			for (int i = _reads.Count - 1; i >= 0; i--)
			{
				_execs.RemoveAt(i);
			}

			if (UpdateHasVariables())
			{
				Changes();
			}
		}

		public delegate void ActiveChangedEventHandler();
		public event ActiveChangedEventHandler ActiveChanged;

		public delegate void CallbackAddedEventHandler(IMemoryCallback callback);
		public event CallbackAddedEventHandler CallbackAdded;

		public delegate void CallbackRemovedEventHandler(IMemoryCallback callback);
		public event CallbackRemovedEventHandler CallbackRemoved;

		private void Changes()
		{
			ActiveChanged?.Invoke();
		}

		public void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			switch (args.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (IMemoryCallback callback in args.NewItems)
					{
						CallbackAdded?.Invoke(callback);
					}

					break;
				case NotifyCollectionChangedAction.Remove:
					foreach (IMemoryCallback callback in args.OldItems)
					{
						CallbackRemoved?.Invoke(callback);
					}

					break;
			}
		}

		public IEnumerator<IMemoryCallback> GetEnumerator()
		{
			foreach (var imc in _reads)
			{
				yield return imc;
			}

			foreach (var imc in _writes)
			{
				yield return imc;
			}

			foreach (var imc in _execs)
			{
				yield return imc;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (var imc in _reads)
			{
				yield return imc;
			}

			foreach (var imc in _writes)
			{
				yield return imc;
			}

			foreach (var imc in _execs)
			{
				yield return imc;
			}
		}
	}

	public class MemoryCallback : IMemoryCallback
	{
		public MemoryCallback(string scope, MemoryCallbackType type, string name, Action callback, uint? address, uint? mask)
		{
			if (type == MemoryCallbackType.Execute && !address.HasValue)
			{
				throw new InvalidOperationException("When assigning an execute callback, an address must be specified");
			}

			Type = type;
			Name = name;
			Callback = callback;
			Address = address;
			AddressMask = mask ?? 0xFFFFFFFF;
			Scope = scope;
		}

		public MemoryCallbackType Type { get; }
		public string Name { get; }
		public Action Callback { get; }
		public uint? Address { get; }
		public uint? AddressMask { get; }
		public string Scope { get; }
	}
}
