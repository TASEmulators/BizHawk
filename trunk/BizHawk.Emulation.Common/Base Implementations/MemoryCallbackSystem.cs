using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Common
{
	public class MemoryCallbackSystem : IMemoryCallbackSystem
	{
		private readonly List<IMemoryCallback> Callbacks = new List<IMemoryCallback>();

		public void Add(IMemoryCallback callback)
		{
			var hadAny = Callbacks.Any();

			Callbacks.Add(callback);

			var hasAny = Callbacks.Any();
			Changes(hadAny, hasAny);
		}

		public void CallReads(uint addr)
		{
			foreach (var read in Callbacks.Where(callback => callback.Type == MemoryCallbackType.Read))
			{
				if (!read.Address.HasValue || read.Address == addr)
				{
					read.Callback();
				}
			}
		}

		public void CallWrites(uint addr)
		{
			foreach (var read in Callbacks.Where(callback => callback.Type == MemoryCallbackType.Write))
			{
				if (!read.Address.HasValue || read.Address == addr)
				{
					read.Callback();
				}
			}
		}

		public void CallExecutes(uint addr)
		{
			foreach (var read in Callbacks.Where(callback => callback.Type == MemoryCallbackType.Execute))
			{
				if (!read.Address.HasValue || read.Address == addr)
				{
					read.Callback();
				}
			}
		}

		public bool HasReads
		{
			get { return Callbacks.Any(callback => callback.Type == MemoryCallbackType.Read); }
		}

		public bool HasWrites
		{
			get { return Callbacks.Any(callback => callback.Type == MemoryCallbackType.Write); }
		}

		public bool HasExecutes
		{
			get { return Callbacks.Any(callback => callback.Type == MemoryCallbackType.Execute); }
		}

		public void Remove(Action action)
		{
			var hadAny = Callbacks.Any();

			var actions = Callbacks.Where(c => c.Callback == action);
			Callbacks.RemoveAll(c => actions.Contains(c));

			var hasAny = Callbacks.Any();
			Changes(hadAny, hasAny);
		}

		public void RemoveAll(IEnumerable<Action> actions)
		{
			var hadAny = Callbacks.Any();

			foreach (var action in actions)
			{
				Remove(action);
			}

			var hasAny = Callbacks.Any();
			Changes(hadAny, hasAny);
		}

		public void Clear()
		{
			var hadAny = Callbacks.Any();
			Callbacks.Clear();
			Changes(hadAny, false);
		}

		public delegate void ActiveChangedEventHandler();
		public event ActiveChangedEventHandler ActiveChanged;

		private void Changes(bool hadAny, bool hasAny)
		{
			if ((hadAny && !hasAny) || (!hadAny && hasAny))
			{
				if (ActiveChanged != null)
				{
					ActiveChanged();
				}
			}
		}
	}

	public class MemoryCallback : IMemoryCallback
	{
		public MemoryCallback(MemoryCallbackType type, string name, Action callback, uint? address)
		{
			if (type == MemoryCallbackType.Execute && !address.HasValue)
			{
				throw new InvalidOperationException("When assigning an execute callback, an address must be specified");
			}

			Type = type;
			Name = name;
			Callback = callback;
			Address = address;
		}

		public MemoryCallbackType Type { get; private set; }
		public string Name { get; private set; }
		public Action Callback { get; private set; }
		public uint? Address { get; private set; }
	}
}
