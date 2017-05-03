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
		public MemoryCallbackSystem()
		{
			ExecuteCallbacksAvailable = true;

			_reads.CollectionChanged += OnCollectionChanged;
			_writes.CollectionChanged += OnCollectionChanged;
			_execs.CollectionChanged += OnCollectionChanged;
		}

		private readonly ObservableCollection<IMemoryCallback> _reads = new ObservableCollection<IMemoryCallback>();
		private readonly ObservableCollection<IMemoryCallback> _writes = new ObservableCollection<IMemoryCallback>();
		private readonly ObservableCollection<IMemoryCallback> _execs = new ObservableCollection<IMemoryCallback>();

		private bool _empty = true;

		private bool _hasReads;
		private bool _hasWrites;
		private bool _hasExecutes;

		public bool ExecuteCallbacksAvailable { get; }

		public void Add(IMemoryCallback callback)
		{
			switch (callback.Type)
			{
				case MemoryCallbackType.Execute:
					_execs.Add(callback);
					_hasExecutes = true;
					break;
				case MemoryCallbackType.Read:
					_reads.Add(callback);
					_hasReads = true;
					break;
				case MemoryCallbackType.Write:
					_writes.Add(callback);
					_hasWrites = true;
					break;
			}

			if (_empty)
			{
				Changes();
			}

			_empty = false;
		}

		private static void Call(ObservableCollection<IMemoryCallback> cbs, uint addr)
		{
			for (int i = 0; i < cbs.Count; i++)
			{
				if (!cbs[i].Address.HasValue || cbs[i].Address == (addr & cbs[i].AddressMask))
				{
					cbs[i].Callback();
				}
			}
		}

		public void CallReads(uint addr)
		{
			if (_hasReads)
			{
				Call(_reads, addr);
			}
		}

		public void CallWrites(uint addr)
		{
			if (_hasWrites)
			{
				Call(_writes, addr);
			}
		}

		public void CallExecutes(uint addr)
		{
			if (_hasExecutes)
			{
				Call(_execs, addr);
			}
		}

		public bool HasReads => _hasReads;

		public bool HasWrites => _hasWrites;

		public bool HasExecutes => _hasExecutes;

		private void UpdateHasVariables()
		{
			_hasReads = _reads.Count > 0;
			_hasWrites = _writes.Count > 0;
			_hasExecutes = _execs.Count > 0;
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

			UpdateHasVariables();

			return readsToRemove.Count + writesToRemove.Count + execsToRemove.Count;
		}

		public void Remove(Action action)
		{
			if (RemoveInternal(action) > 0)
			{
				bool newEmpty = !HasReads && !HasWrites && !HasExecutes;
				if (newEmpty != _empty)
				{
					Changes();
				}

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
				{
					Changes();
				}

				_empty = newEmpty;
			}

			UpdateHasVariables();
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

			if (!_empty)
			{
				Changes();
			}

			_empty = true;

			UpdateHasVariables();
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
			AddressMask = mask ?? 0xFFFFFFFF;
		}

		public MemoryCallbackType Type { get; }
		public string Name { get; }
		public Action Callback { get; }
		public uint? Address { get; }
		public uint? AddressMask { get; }
	}
}
