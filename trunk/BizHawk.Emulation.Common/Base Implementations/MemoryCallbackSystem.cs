using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Common
{
	public class MemoryCallbackSystem : IMemoryCallbackSystem
	{
		private readonly List<Action> _reads = new List<Action>();
		private readonly List<uint?> _readAddrs = new List<uint?>();

		private readonly List<Action> _writes = new List<Action>();
		private readonly List<uint?> _writeAddrs = new List<uint?>();

		private readonly List<Action> _executes = new List<Action>();
		private readonly List<uint> _execAddrs = new List<uint>();

		public void AddRead(Action function, uint? addr)
		{
			var hadAny = _reads.Any() || _writes.Any() || _executes.Any();

			_reads.Add(function);
			_readAddrs.Add(addr);

			var hasAny = _reads.Any() || _writes.Any() || _executes.Any();
			Changes(hadAny, hasAny);
		}

		public void AddWrite(Action function, uint? addr)
		{
			var hadAny = _reads.Any() || _writes.Any() || _executes.Any();

			_writes.Add(function);
			_writeAddrs.Add(addr);

			var hasAny = _reads.Any() || _writes.Any() || _executes.Any();
			Changes(hadAny, hasAny);
		}

		public void AddExecute(Action function, uint addr)
		{
			var hadAny = _reads.Any() || _writes.Any() || _executes.Any();

			_executes.Add(function);
			_execAddrs.Add(addr);

			var hasAny = _reads.Any() || _writes.Any() || _executes.Any();
			Changes(hadAny, hasAny);
		}

		public void CallRead(uint addr)
		{
			for (int i = 0; i < _reads.Count; i++)
			{
				if (!_readAddrs[i].HasValue || _readAddrs[i].Value == addr)
				{
					_reads[i]();
				}
			}
		}

		public void CallWrite(uint addr)
		{
			for (int i = 0; i < _writes.Count; i++)
			{
				if (!_writeAddrs[i].HasValue || _writeAddrs[i] == addr)
				{
					_writes[i]();
				}
			}
		}

		public void CallExecute(uint addr)
		{
			for (int i = 0; i < _executes.Count; i++)
			{
				if (_execAddrs[i] == addr)
				{
					_executes[i]();
				}
			}
		}

		public bool HasReads { get { return _reads.Any(); } }
		public bool HasWrites { get { return _writes.Any(); } }
		public bool HasExecutes { get { return _executes.Any(); } }

		public void Remove(Action action)
		{
			var hadAny = _reads.Any() || _writes.Any() || _executes.Any();

			for (int i = 0; i < _reads.Count; i++)
			{
				if (_reads[i] == action)
				{
					_reads.Remove(_reads[i]);
					_readAddrs.Remove(_readAddrs[i]);
				}
			}

			for (int i = 0; i < _writes.Count; i++)
			{
				if (_writes[i] == action)
				{
					_writes.Remove(_writes[i]);
					_writeAddrs.Remove(_writeAddrs[i]);
				}
			}

			for (int i = 0; i < _executes.Count; i++)
			{
				if (_executes[i] == action)
				{
					_executes.Remove(_executes[i]);
					_execAddrs.Remove(_execAddrs[i]);
				}
			}

			var hasAny = _reads.Any() || _writes.Any() || _executes.Any();
			Changes(hadAny, hasAny);
		}

		public void RemoveAll(IEnumerable<Action> actions)
		{
			var hadAny = _reads.Any() || _writes.Any() || _executes.Any();

			foreach (var action in actions)
			{
				Remove(action);
			}

			var hasAny = _reads.Any() || _writes.Any() || _executes.Any();
			Changes(hadAny, hasAny);
		}

		public void Clear()
		{
			var hadAny = _reads.Any() || _writes.Any() || _executes.Any();

			_reads.Clear();
			_readAddrs.Clear();
			_writes.Clear();
			_writes.Clear();
			_executes.Clear();
			_execAddrs.Clear();

			
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
}
