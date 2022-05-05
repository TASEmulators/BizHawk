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
		private readonly LibmGBA.MemCallback _readWriteCallback;
		private readonly LibmGBA.ExecCallback _executeCallback;
		private readonly Dictionary<uint, MemoryCallbackDelegate> _readCallbacks = new();
		private readonly Dictionary<uint, MemoryCallbackDelegate> _writeCallbacks = new();
		private readonly Dictionary<uint, MemoryCallbackDelegate> _execCallbacks = new();
		private readonly List<CallbackContainer> _callbacks = new();

		public MGBAMemoryCallbackSystem(MGBAHawk mgba)
		{
			_mgba = mgba;
			_readWriteCallback = RunReadWriteCallback;
			_executeCallback = RunExecCallback;
		}

		public string[] AvailableScopes { get; } = { "System Bus" };
		public bool ExecuteCallbacksAvailable => true;

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
				MGBAHawk.ZZHacky.BizSetExecCallback(_mgba.Core, _executeCallback);
			}
			else
			{
				container.ID = MGBAHawk.ZZHacky.BizSetWatchpoint(_mgba.Core, callback.Address.Value, container.WatchPointType);
				MGBAHawk.ZZHacky.BizSetMemCallback(_mgba.Core, _readWriteCallback);
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
					MGBAHawk.ZZHacky.BizSetExecCallback(_mgba.Core, null);
				}
			}
			else
			{
				if (!MGBAHawk.ZZHacky.BizClearWatchpoint(_mgba.Core, cb.ID))
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
					MGBAHawk.ZZHacky.BizSetMemCallback(_mgba.Core, null);
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

		private MemoryCallbackDelegate DebugCallback = null;

		private bool DebugCallbackExecuted = false;

		public void Debug2805()
		{
			DebugCallback = (_, _, _) =>
			{
				Console.WriteLine(DebugCallbackExecuted ? "subsequent call" : "first call");
				Remove(DebugCallback);
				DebugCallbackExecuted = true;
			};
			Add(new MemoryCallback("System Bus", MemoryCallbackType.Write, "Plugin Hook", DebugCallback, 0x020096E0, null));
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
