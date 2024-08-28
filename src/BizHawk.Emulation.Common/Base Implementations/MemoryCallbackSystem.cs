#nullable disable

using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

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
			availableScopes ??= new[] {"System Bus"};

			AvailableScopes = availableScopes;
			ExecuteCallbacksAvailable = true;

			_reads.ItemAdded += OnCallbackAdded;
			_reads.ItemRemoved += OnCallbackRemoved;
			_writes.ItemAdded += OnCallbackAdded;
			_writes.ItemRemoved += OnCallbackRemoved;
			_execs.ItemAdded += OnCallbackAdded;
			_execs.ItemRemoved += OnCallbackRemoved;
		}

		private readonly MemoryCallbackCollection _reads = new();
		private readonly MemoryCallbackCollection _writes = new();
		private readonly MemoryCallbackCollection _execs = new();

		private bool _hasAny;

		public bool ExecuteCallbacksAvailable { get; }

		public string[] AvailableScopes { get; }

		/// <exception cref="InvalidOperationException">scope of <paramref name="callback"/> isn't available</exception>
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

		private static void Call(MemoryCallbackCollection cbs, uint addr, uint value, uint flags, string scope)
		{
			foreach (var cb in cbs)
			{
				if (!cb.Address.HasValue || (cb.Scope == scope && cb.Address == (addr & cb.AddressMask)))
				{
					cb.Callback(addr, value, flags);
				}
			}
		}

		public void CallMemoryCallbacks(uint addr, uint value, uint flags, string scope)
		{
			if (!_hasAny)
			{
				return;
			}

			if (HasReads)
			{
				if ((flags & (uint) MemoryCallbackFlags.AccessRead) != 0)
				{
					Call(_reads, addr, value, flags, scope);
				}
			}

			if (HasWrites)
			{
				if ((flags & (uint) MemoryCallbackFlags.AccessWrite) != 0)
				{
					Call(_writes, addr, value, flags, scope);
				}
			}

			if (HasExecutes)
			{
				if ((flags & (uint) MemoryCallbackFlags.AccessExecute) != 0)
				{
					Call(_execs, addr, value, flags, scope);
				}
			}
		}

		public bool HasReads { get; private set; }

		public bool HasWrites { get; private set; }

		public bool HasExecutes { get; private set; }

		public bool HasReadsForScope(string scope)
		{
			return _reads.Any(e => e.Scope == scope);
		}

		public bool HasWritesForScope(string scope)
		{
			return _writes.Any(e => e.Scope == scope);
		}

		public bool HasExecutesForScope(string scope)
		{
			return _execs.Any(e => e.Scope == scope);
		}

		private bool UpdateHasVariables()
		{
			bool hadReads = HasReads;
			bool hadWrites = HasWrites;
			bool hadExecutes = HasExecutes;

			HasReads = _reads.Count > 0;
			HasWrites = _writes.Count > 0;
			HasExecutes = _execs.Count > 0;
			_hasAny = HasReads || HasWrites || HasExecutes;

			return HasReads != hadReads || HasWrites != hadWrites || HasExecutes != hadExecutes;
		}

		private bool RemoveInternal(MemoryCallbackDelegate action)
		{
			bool anyRemoved = false;
			anyRemoved |= _reads.Remove(action);
			anyRemoved |= _writes.Remove(action);
			anyRemoved |= _execs.Remove(action);
			return anyRemoved;
		}

		public void Remove(MemoryCallbackDelegate action)
		{
			if (RemoveInternal(action))
			{
				if (UpdateHasVariables())
				{
					Changes();
				}
			}
		}

		public void RemoveAll(IEnumerable<MemoryCallbackDelegate> actions)
		{
			bool changed = false;
			foreach (var action in actions)
			{
				changed |= RemoveInternal(action);
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
			_reads.Clear();
			_writes.Clear();
			_execs.Clear();

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

		private void OnCallbackAdded(object sender, IMemoryCallback callback)
		{
			CallbackAdded?.Invoke(callback);
		}

		private void OnCallbackRemoved(object sender, IMemoryCallback callback)
		{
			CallbackRemoved?.Invoke(callback);
		}

		public IEnumerator<IMemoryCallback> GetEnumerator()
			=> _reads.Concat(_writes).Concat(_execs).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();
	}

	public class MemoryCallback : IMemoryCallback
	{
		public MemoryCallback(string scope, MemoryCallbackType type, string name, MemoryCallbackDelegate callback, uint? address, uint? mask)
		{
			Type = type;
			Name = name;
			Callback = callback;
			Address = address;
			AddressMask = mask ?? 0xFFFFFFFF;
			Scope = scope;
		}

		public MemoryCallbackType Type { get; }
		public string Name { get; }
		public MemoryCallbackDelegate Callback { get; }
		public uint? Address { get; }
		public uint? AddressMask { get; }
		public string Scope { get; }
	}

	/// <summary>
	/// Specialized collection for memory callbacks with add/remove events and copy-on-write behavior during enumeration.
	/// </summary>
	/// <remarks>
	/// Reentrancy from ItemAdded and ItemRemoved events is not allowed.
	/// </remarks>
	internal class MemoryCallbackCollection : IReadOnlyCollection<IMemoryCallback>
	{
		private List<IMemoryCallback> _items = new();
		private int _copyOnWriteRequired = 0;
		private bool _modifyInProgress = false;

		public int Count => _items.Count;

		public void Add(IMemoryCallback item)
		{
			BeginModify();
			try
			{
				CopyIfRequired();

				_items.Add(item);
				ItemAdded?.Invoke(this, item);
			}
			finally
			{
				EndModify();
			}
		}

		private void RemoveAtInternal(int index)
		{
			Debug.Assert(_modifyInProgress, "unexpected collection mutation state");
			CopyIfRequired();

			var removedItem = _items[index];
			_items.RemoveAt(index);
			ItemRemoved?.Invoke(this, removedItem);
		}

		public bool Remove(MemoryCallbackDelegate callback)
		{
			BeginModify();
			try
			{
				int removed = 0;
				for (int i = 0; i < _items.Count;)
				{
					if (_items[i].Callback == callback)
					{
						RemoveAtInternal(i);
						removed++;
					}
					else
					{
						i++;
					}
				}
				return removed > 0;
			}
			finally
			{
				EndModify();
			}
		}

		public void Clear()
		{
			BeginModify();
			try
			{
				while (Count > 0)
					RemoveAtInternal(Count - 1);
			}
			finally
			{
				EndModify();
			}
		}

		private void CopyIfRequired()
		{
			if (_copyOnWriteRequired > 0)
			{
				_items = new List<IMemoryCallback>(_items);
			}
		}

		private void CheckModifyReentrancy()
		{
			if (_modifyInProgress)
				throw new InvalidOperationException("Reentrancy in MemoryCallbackCollection ItemAdded/ItemRemoved is not allowed.");
		}

		private void BeginModify()
		{
			CheckModifyReentrancy();
			_modifyInProgress = true;
		}

		private void EndModify()
		{
			_modifyInProgress = false;
		}

		private void BeginCopyOnWrite()
		{
			_copyOnWriteRequired++;
		}

		private void EndCopyOnWrite()
		{
			_copyOnWriteRequired--;
			Debug.Assert(_copyOnWriteRequired >= 0, "unexpected CoW state");
		}

		public Enumerator GetEnumerator()
		{
			CheckModifyReentrancy();
			return new Enumerator(this);
		}

		IEnumerator<IMemoryCallback> IEnumerable<IMemoryCallback>.GetEnumerator() => GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public EventHandler<IMemoryCallback> ItemAdded;
		public EventHandler<IMemoryCallback> ItemRemoved;



		/// <remarks>
		/// This struct must not be copied.
		/// </remarks>
		public struct Enumerator : IEnumerator<IMemoryCallback>, IDisposable
		{
			private readonly MemoryCallbackCollection _collection;
			private List<IMemoryCallback> _items;
			private int _position;

			public Enumerator(MemoryCallbackCollection collection)
			{
				_collection = collection;
				_items = collection._items;
				_position = -1;
				_collection.BeginCopyOnWrite();
			}

			public readonly IMemoryCallback Current => _items[_position];
			
			object IEnumerator.Current => Current;

			public bool MoveNext() => ++_position < _items.Count;

			public void Dispose()
			{
				if (_items != null)
				{
					_items = null;
					_collection.EndCopyOnWrite();
				}
			}

			void IEnumerator.Reset() => throw new NotSupportedException();
		}
	}
}
