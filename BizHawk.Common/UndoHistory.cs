using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Common
{
	public class UndoHistory<T>
	{
		private List<List<T>> _history = new List<List<T>>();
		private int _curPos; // 1-based

		public int MaxUndoLevels { get; }

		public UndoHistory(bool enabled)
		{
			MaxUndoLevels = 5;
			Enabled = enabled;
		}

		public UndoHistory(IEnumerable<T> newState, bool enabled)
		{
			AddState(newState);
			Enabled = enabled;
		}

		public bool Enabled { get; }

		public bool CanUndo => Enabled && _curPos > 1;

		public bool CanRedo => Enabled && _curPos < _history.Count;

		public bool HasHistory => Enabled && _history.Count > 0;

		public void Clear()
		{
			_history = new List<List<T>>();
			_curPos = 0;
		}

		public void AddState(IEnumerable<T> newState)
		{
			if (Enabled)
			{
				for (int i = _curPos, end = _history.Count - 2; i < end; i++) _history.RemoveAt(i);
				while (_history.Count > MaxUndoLevels) _history.RemoveAt(0); // using a while rather than an if because MaxUndoLevels is public and could be lowered between calls
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
