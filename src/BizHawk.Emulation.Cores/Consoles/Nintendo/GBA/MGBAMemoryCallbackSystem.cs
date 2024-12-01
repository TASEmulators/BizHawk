using System.Collections;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public class MGBAMemoryCallbackSystem : IMemoryCallbackSystem, IDisposable
	{
		private LibmGBA _mgba;
		private IntPtr _core;
		private readonly LibmGBA.MemCallback _readWriteCallback;
		private readonly LibmGBA.ExecCallback _executeCallback;
		private readonly Dictionary<uint, MemoryCallbackDelegate> _readCallbacks = new();
		private readonly Dictionary<uint, MemoryCallbackDelegate> _writeCallbacks = new();
		private readonly Dictionary<uint, MemoryCallbackDelegate> _execCallbacks = new();
		private readonly List<CallbackContainer> _callbacks = new();

		public MGBAMemoryCallbackSystem(LibmGBA mgba, IntPtr core)
		{
			_mgba = mgba;
			_core = core;
			_readWriteCallback = RunReadWriteCallback;
			_executeCallback = RunExecCallback;
		}

		public string[] AvailableScopes { get; } = { "System Bus" };
		public bool ExecuteCallbacksAvailable => true;

		public bool HasReads
			=> _callbacks.Exists(static c => c.Callback.Type is MemoryCallbackType.Read);

		public bool HasWrites
			=> _callbacks.Exists(static c => c.Callback.Type is MemoryCallbackType.Write);

		public bool HasExecutes
			=> _callbacks.Exists(static c => c.Callback.Type is MemoryCallbackType.Execute);

		public bool HasReadsForScope(string scope)
			=> _callbacks.Exists(c => c.Callback.Type is MemoryCallbackType.Read && c.Callback.Scope == scope);

		public bool HasWritesForScope(string scope)
			=> _callbacks.Exists(c => c.Callback.Type is MemoryCallbackType.Write && c.Callback.Scope == scope);

		public bool HasExecutesForScope(string scope)
			=> _callbacks.Exists(c => c.Callback.Type is MemoryCallbackType.Execute && c.Callback.Scope == scope);

		public void Add(IMemoryCallback callback)
		{
			if (!AvailableScopes.Contains(callback.Scope))
			{
				throw new InvalidOperationException($"{callback.Scope} is not currently supported for callbacks");
			}

			if (!callback.Address.HasValue)
			{
				throw new NotImplementedException("Wildcard callbacks (no address specified) not currently implemented.");
			}

			if (callback.AddressMask != 0xFFFFFFFF)
			{
				throw new NotImplementedException("Non 0xFFFFFFFF address masks are not currently implemented.");
			}

			var container = new CallbackContainer(callback);

			if (container.Callback.Type == MemoryCallbackType.Execute)
			{
				_mgba.BizSetExecCallback(_core, _executeCallback);
			}
			else
			{
				container.ID = _mgba.BizSetWatchpoint(_core, callback.Address.Value, container.WatchPointType);
				_mgba.BizSetMemCallback(_core, _readWriteCallback);
			}

			var cbDict = container.Callback.Type switch
			{
				MemoryCallbackType.Read => _readCallbacks,
				MemoryCallbackType.Write => _writeCallbacks,
				MemoryCallbackType.Execute => _execCallbacks,
				_ => throw new InvalidOperationException("Invalid callback type"),
			};

			if (cbDict.ContainsKey(container.Callback.Address.Value))
			{
				cbDict[container.Callback.Address.Value] += container.Callback.Callback;
			}
			else
			{
				cbDict[container.Callback.Address.Value] = container.Callback.Callback;
			}

			_callbacks.Add(container);
		}

		private void Remove(CallbackContainer cb)
		{
			_callbacks.Remove(cb);

			if (cb.Callback.Type == MemoryCallbackType.Execute)
			{
				_execCallbacks[cb.Callback.Address.Value] -= cb.Callback.Callback;

				if (!HasExecutes)
				{
					_mgba.BizSetExecCallback(_core, null);
				}
			}
			else
			{
				if (!_mgba.BizClearWatchpoint(_core, cb.ID))
				{
					throw new InvalidOperationException("Unable to clear watchpoint???");
				}

				if (cb.Callback.Type == MemoryCallbackType.Read)
				{
					_readCallbacks[cb.Callback.Address.Value] -= cb.Callback.Callback;
				}
				else if (cb.Callback.Type == MemoryCallbackType.Write)
				{
					_writeCallbacks[cb.Callback.Address.Value] -= cb.Callback.Callback;
				}
				else
				{
					throw new InvalidOperationException("Invalid watchpoint type");
				}

				if (!HasReads && !HasWrites)
				{
					_mgba.BizSetMemCallback(_core, null);
				}
			}
		}

		public void Remove(MemoryCallbackDelegate action)
		{
			var cbToRemove = _callbacks.SingleOrDefault(container => container.Callback.Callback == action);

			if (cbToRemove != null)
			{
				Remove(cbToRemove);
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
				Remove(cb);
			}
		}

		public IEnumerator<IMemoryCallback> GetEnumerator() => _callbacks.Select(c => c.Callback).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		public void CallMemoryCallbacks(uint addr, uint value, uint flags, string scope)
		{
			// Not a thing in this implementation
		}

		private void RunReadWriteCallback(uint addr, LibmGBA.mWatchpointType type, uint oldValue, uint newValue)
		{
			if (type == LibmGBA.mWatchpointType.WATCHPOINT_READ)
			{
				if (_readCallbacks.TryGetValue(addr, out var cb))
				{
					cb?.Invoke(addr, newValue, (uint)MemoryCallbackFlags.AccessRead);
				}
			}
			else if (type == LibmGBA.mWatchpointType.WATCHPOINT_WRITE)
			{
				if (_writeCallbacks.TryGetValue(addr, out var cb))
				{
					cb?.Invoke(addr, newValue, (uint)MemoryCallbackFlags.AccessWrite);
				}
			}
			else
			{
				throw new InvalidOperationException("Invalid watchpoint type");
			}
		}

		private void RunExecCallback(uint pc)
		{
			if (_execCallbacks.TryGetValue(pc, out var cb))
			{
				cb?.Invoke(pc, 0, (uint)MemoryCallbackFlags.AccessExecute);
			}
		}

		public void Dispose()
		{
			_mgba = null;
			_core = IntPtr.Zero;
		}

		private class CallbackContainer
		{
			public CallbackContainer(IMemoryCallback callBack)
			{
				Callback = callBack;
			}

			public IMemoryCallback Callback { get; }

			// the core returns this when setting a wp and needs it to clear that wp
			public long ID { get; set; }

			public LibmGBA.mWatchpointType WatchPointType
			{
				get
				{
					return Callback.Type switch
					{
						MemoryCallbackType.Read => LibmGBA.mWatchpointType.WATCHPOINT_READ,
						MemoryCallbackType.Write => LibmGBA.mWatchpointType.WATCHPOINT_WRITE,
						MemoryCallbackType.Execute => throw new NotImplementedException("Executes can not be used from watch points."),
						_ => throw new InvalidOperationException("Invalid callback type"),
					};
				}
			}
		}
	}
}
