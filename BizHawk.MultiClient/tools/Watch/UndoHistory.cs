using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public class UndoHistory<T>
	{
		private List<List<T>> _history = new List<List<T>>();
		private int curPos; //1-based

		public bool Enabled { get; private set; }

		public UndoHistory(bool enabled)
		{
			Enabled = enabled;
		}

		public UndoHistory(IEnumerable<T> newState, bool enabled)
		{
			AddState(newState);
			Enabled = enabled;
		}

		public void Clear()
		{
			_history = new List<List<T>>();
			curPos = 0;
		}

		public bool CanUndo
		{
			get { return Enabled && curPos > 1; }
		}

		public bool CanRedo
		{
			get { return Enabled && curPos < _history.Count; }
		}

		public bool HasHistory
		{
			get { return Enabled && _history.Any(); }
		}

		public void AddState(IEnumerable<T> newState)
		{
			if (Enabled)
			{
				if (curPos < _history.Count)
				{
					for (int i = curPos + 1; i <= _history.Count; i++)
					{
						_history.Remove(_history[i - 1]);
					}
				}

				_history.Add(newState.ToList());
				curPos = _history.Count;
			}
		}

		public IEnumerable<T> Undo()
		{
			if (CanUndo && Enabled)
			{
				curPos--;
				return _history[curPos - 1];
			}
			else
			{
				return null;
			}
		}

		public IEnumerable<T> Redo()
		{
			if (CanRedo && Enabled)
			{
				curPos++;
				return _history[curPos - 1];
			}
			else
			{
				return null;
			}
		}
	}
}
