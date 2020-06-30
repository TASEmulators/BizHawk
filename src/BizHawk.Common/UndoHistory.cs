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

		public bool Enabled { get; }

		public bool CanUndo => Enabled && _curPos > 1;

		public bool CanRedo => Enabled && _curPos < _history.Count;

		public bool HasHistory => Enabled && _history.Any();

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

				// TODO: don't assume we are one over, since MaxUndoLevels is public, it could be set to a small number after a large list has occured
				if (_history.Count > MaxUndoLevels)
				{
					foreach (var item in _history.Take(_history.Count - MaxUndoLevels))
					{
						_history.Remove(item);
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
