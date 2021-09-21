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
		private LibmGBA.ExecCallback _executeCallback;
		private readonly Dictionary<uint, IMemoryCallback> _execPcs = new Dictionary<uint, IMemoryCallback> { [0] = null };

		public MGBAMemoryCallbackSystem(MGBAHawk mgba)
		{
			_mgba = mgba;
		}

		private readonly List<CallbackContainer> _callbacks = new List<CallbackContainer>();

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

			var container = new CallbackContainer(callback);

			if (container.Callback.Type == MemoryCallbackType.Execute)
			{
				_executeCallback = RunExecCallback;
				_execPcs[callback.Address.Value] = callback;
				MGBAHawk.ZZHacky.BizSetExecCallback(_mgba.Core, _executeCallback);
			}
			else
			{
				MGBAHawk.ZZHacky.BizSetMemCallback(_mgba.Core, container._cd);
				container.ID = MGBAHawk.ZZHacky.BizSetWatchpoint(_mgba.Core, callback.Address.Value, container.WatchPointType);
			}

			_callbacks.Add(container);
		}

		private void Remove(CallbackContainer cb)
		{
			if (MGBAHawk.ZZHacky.BizClearWatchpoint(_mgba.Core, cb.ID)) _callbacks.Remove(cb);
		}

		public void Remove(MemoryCallbackDelegate action)
		{
			var cbToRemove = _callbacks.SingleOrDefault(container => container.Callback.Callback == action);
			if (cbToRemove == null) return;

			if (cbToRemove.Callback.Type is MemoryCallbackType.Execute)
			{
				_callbacks.Remove(cbToRemove);
				if (!_callbacks.Any(cb => cb.Callback.Type is MemoryCallbackType.Execute))
				{
					_executeCallback = null;
					MGBAHawk.ZZHacky.BizSetExecCallback(_mgba.Core, null);
				}
			}
			else
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
			foreach (var cb in _callbacks) Remove(cb);
		}

		public IEnumerator<IMemoryCallback> GetEnumerator() => _callbacks.Select(c => c.Callback).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();

		public void CallMemoryCallbacks(uint addr, uint value, uint flags, string scope)
		{
			// Not a thing in this implementation
		}

		private void RunExecCallback(uint pc)
		{
			if (_execPcs.TryGetValue(pc, out var callback))
			{
				callback?.Callback?.Invoke(pc, 0, 0);
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
			CallDelegate = Call;
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
						throw new NotImplementedException("Executes can not be used from watch points.");
				}
			}
		}

		public LibmGBA.MemCallback _cd => CallDelegate;
		public static LibmGBA.MemCallback CallDelegate;

		private void Call(uint addr, LibmGBA.mWatchpointType type, uint oldValue, uint newValue)
		{
			Callback.Callback?.Invoke(addr, newValue, 0);
		}
	}
}
