using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Common
{
	public class UndoHistory<T>
	{
		private List<List<T>> _history = new List<List<T>>();
		private int _curPos; // 1-based

		public UndoHistory(bool enabled)
		{
			Enabled = enabled;
		}

		public UndoHistory(IEnumerable<T> newState, bool enabled)
		{
			AddState(newState);
			Enabled = enabled;
		}

		public bool Enabled { get; private set; }

		public bool CanUndo
		{
			get { return Enabled && _curPos > 1; }
		}

		public bool CanRedo
		{
			get { return Enabled && _curPos < _history.Count; }
		}

		public bool HasHistory
		{
			get { return Enabled && _history.Any(); }
		}

		public void Clear()
		{
			_history = new List<List<T>>();
			_curPos = 0;
		}

		public void AddState(IEnumerable<T> newState)
		{
			if (Enabled)
			{
				if (_curPos < _history.Count)
				{
					for (var i = _curPos + 1; i <= _history.Count; i++)
					{
						_history.Remove(_history[i - 1]);
					}
				}

				_history.Add(newState.ToList());
				_curPos = _history.Count;
			}
		}

		public IEnumerable<T> Undo()
		{
			if (CanUndo && Enabled)
			{
				_curPos--;
				return _history[_curPos - 1];
			}

			return Enumerable.Empty<T>();
		}

		public IEnumerable<T> Redo()
		{
			if (CanRedo && Enabled)
			{
				_curPos++;
				return _history[_curPos - 1];
			}
			
			return Enumerable.Empty<T>();
		}
	}
}
