using System.Collections;
using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// An implementation of <see cref="IInputCallbackSystem"/> that is implementation using only methods
	/// from <see cref="IDebuggable"/>,
	/// Useful for ported cores that have these hooks but no input callback hook,
	/// This allows for an input callback implementation without the need for additional APIs
	/// </summary>
	public class MemoryBasedInputCallbackSystem : IInputCallbackSystem
	{
		private readonly List<Action> _inputCallbacks = new List<Action>();

		public MemoryBasedInputCallbackSystem(IDebuggable debuggableCore, string scope, IEnumerable<uint> addresses)
		{
			if (!debuggableCore.MemoryCallbacksAvailable())
			{
				throw new InvalidOperationException("Memory callbacks are required");
			}

			foreach (var address in addresses)
			{
				var callback = new MemoryCallback(
					scope,
					MemoryCallbackType.Read,
					"InputCallback" + address,
					MemoryCallback,
					address,
					null);

				debuggableCore.MemoryCallbacks.Add(callback);
			}
		}

		private void MemoryCallback(uint address, uint value, uint flags)
		{
			foreach (var action in _inputCallbacks)
			{
				action.Invoke();
			}
		}

		public IEnumerator<Action> GetEnumerator() => _inputCallbacks.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _inputCallbacks.GetEnumerator();

		public void Add(Action item) => _inputCallbacks.Add(item);

		public void Clear()
		{
			_inputCallbacks.Clear();
		}

		public bool Contains(Action item) => _inputCallbacks.Contains(item);

		public void CopyTo(Action[] array, int arrayIndex) => _inputCallbacks.CopyTo(array, arrayIndex);

		public bool Remove(Action item) => _inputCallbacks.Remove(item);

		public int Count => _inputCallbacks.Count;
		public bool IsReadOnly => false;

		public void Call()
		{
			throw new InvalidOperationException("This implementation does not require being called directly");
		}

		public void RemoveAll(IEnumerable<Action> actions)
		{
			foreach (var action in actions)
			{
				Remove(action);
			}
		}
	}
}
