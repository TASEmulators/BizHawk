using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Common
{
	public class MemoryCallbackSystem : IMemoryCallbackSystem
	{
		private readonly List<IMemoryCallback> Reads = new List<IMemoryCallback>();
		private readonly List<IMemoryCallback> Writes = new List<IMemoryCallback>();
		private readonly List<IMemoryCallback> Execs = new List<IMemoryCallback>();

		bool empty = true;

		public void Add(IMemoryCallback callback)
		{
			switch (callback.Type)
			{
				case MemoryCallbackType.Execute: Execs.Add(callback); break;
				case MemoryCallbackType.Read: Reads.Add(callback); break;
				case MemoryCallbackType.Write: Writes.Add(callback); break;
			}
			if (empty)
				Changes();
			empty = false;
		}

		private static void Call(List<IMemoryCallback> cbs, uint addr)
		{
			for (int i = 0; i < cbs.Count; i++)
			{
				if (!cbs[i].Address.HasValue || cbs[i].Address == addr)
					cbs[i].Callback();
			}
		}

		public void CallReads(uint addr)
		{
			Call(Reads, addr);
		}

		public void CallWrites(uint addr)
		{
			Call(Writes, addr);
		}

		public void CallExecutes(uint addr)
		{
			Call(Execs, addr);
		}

		public bool HasReads
		{
			get { return Reads.Count > 0; }
		}

		public bool HasWrites
		{
			get { return Writes.Count > 0; }
		}

		public bool HasExecutes
		{
			get { return Execs.Count > 0; }
		}

		private int RemoveInternal(Action action)
		{
			int ret = 0;
			ret += Reads.RemoveAll(imc => imc.Callback == action);
			ret += Writes.RemoveAll(imc => imc.Callback == action);
			ret += Execs.RemoveAll(imc => imc.Callback == action);
			return ret;
		}

		public void Remove(Action action)
		{
			if (RemoveInternal(action) > 0)
			{
				bool newEmpty = !HasReads && !HasWrites && !HasExecutes;
				if (newEmpty != empty)
					Changes();
				empty = newEmpty;
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
				if (newEmpty != empty)
					Changes();
				empty = newEmpty;
			}
		}

		public void Clear()
		{
			Reads.Clear();
			Writes.Clear();
			Execs.Clear();
			if (!empty)
				Changes();
			empty = true;
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
				yield return imc;
			foreach (var imc in Writes)
				yield return imc;
			foreach (var imc in Execs)
				yield return imc;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (var imc in Reads)
				yield return imc;
			foreach (var imc in Writes)
				yield return imc;
			foreach (var imc in Execs)
				yield return imc;
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
