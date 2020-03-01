using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public class MGBAMemoryCallbackSystem : IMemoryCallbackSystem
	{
		private readonly MGBAHawk _mgba;

		public MGBAMemoryCallbackSystem(MGBAHawk mgba)
		{
			_mgba = mgba;
		}

		private readonly List<CallbackContainer> _callbacks = new List<CallbackContainer>();

		public string[] AvailableScopes { get; } = { "System Bus" };
		public bool ExecuteCallbacksAvailable => false;

		public bool HasReads => _callbacks.Any(c => c.Callback.Type == MemoryCallbackType.Read);
		public bool HasWrites => _callbacks.Any(c => c.Callback.Type == MemoryCallbackType.Write);
		public bool HasExecutes => _callbacks.Any(c => c.Callback.Type == MemoryCallbackType.Execute);

		public bool HasReadsForScope(string scope) =>
			_callbacks.Any(c => c.Callback.Scope == scope
				&& c.Callback.Type == MemoryCallbackType.Read);

		public bool HasWritesForScope(string scope) =>
			_callbacks.Any(c => c.Callback.Scope == scope
				&& c.Callback.Type == MemoryCallbackType.Write);

		public bool HasExecutesForScope(string scope) =>
			_callbacks.Any(c => c.Callback.Scope == scope
				&& c.Callback.Type == MemoryCallbackType.Execute);

		public void Add(IMemoryCallback callback)
		{
			if (!AvailableScopes.Contains(callback.Scope))
			{
				throw new InvalidOperationException($"{callback.Scope} is not currently supported for callbacks");
			}

			var container = new CallbackContainer(callback);

			if (container.Callback.Type == MemoryCallbackType.Execute)
			{
				// TODO
			}
			else
			{
				LibmGBA.BizSetMemCallback(container.Call);
				container.ID = LibmGBA.BizSetWatchpoint(_mgba.Core, callback.Address.Value, container.WatchPointType);
			}

			_callbacks.Add(container);
		}

		public void Remove(MemoryCallbackDelegate action)
		{
			var cbToRemove = _callbacks.Single(container => container.Callback.Callback == action);

			if (LibmGBA.BizClearWatchpoint(_mgba.Core, cbToRemove.ID))
			{
				_callbacks.Remove(cbToRemove);
			}
		}

		public void RemoveAll(IEnumerable<MemoryCallbackDelegate> actions)
		{
			foreach (var action in actions)
			{
				Remove(action);
			}
		}

		public void Clear()
		{
			foreach (var cb in _callbacks)
			{
				if (LibmGBA.BizClearWatchpoint(_mgba.Core, cb.ID))
				{
					_callbacks.Remove(cb);
				}
			}
		}

		public IEnumerator<IMemoryCallback> GetEnumerator() => _callbacks.Select(c => c.Callback).GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _callbacks.Select(c => c.Callback).GetEnumerator();

		public void CallMemoryCallbacks(uint addr, uint value, uint flags, string scope)
		{
			// Not a thing in this implementation
		}
	}

	internal class CallbackContainer
	{
		public CallbackContainer(IMemoryCallback callBack)
		{
			Callback = callBack;
		}

		public IMemoryCallback Callback { get; }

		// the core returns this when setting a wp and needs it to clear that wp
		public int ID { get; set; }

		public LibmGBA.mWatchpointType WatchPointType
		{
			get
			{
				switch (Callback.Type)
				{
					default:
					case MemoryCallbackType.Read:
						return LibmGBA.mWatchpointType.WATCHPOINT_READ;
					case MemoryCallbackType.Write:
						return LibmGBA.mWatchpointType.WATCHPOINT_WRITE;
					case MemoryCallbackType.Execute:
						throw new NotImplementedException("Executes not implemented yet");
				}
			}
		}

		public void Call(uint addr, LibmGBA.mWatchpointType type, uint oldValue, uint newValue)
		{
			Callback.Callback?.Invoke(addr, newValue, 0);
		}
	}
}
