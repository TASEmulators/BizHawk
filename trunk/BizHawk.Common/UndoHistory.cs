using System.Collections.Generic;
using System.Linq;

namespace BizHawk.MultiClient
{
	public class WatchHistory
	{
		private List<List<RamSearchEngine.IMiniWatch>> _history = new List<List<RamSearchEngine.IMiniWatch>>();
		private int curPos; //1-based
		public bool Enabled { get; private set; }

		public WatchHistory(bool enabled)
		{
			Enabled = enabled;
		}

		public WatchHistory(List<RamSearchEngine.IMiniWatch> newState, bool enabled)
		{
			AddState(newState);
			Enabled = enabled;
		}

		public void Clear()
		{
			_history = new List<List<RamSearchEngine.IMiniWatch>>();
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

		public void AddState(List<RamSearchEngine.IMiniWatch> newState)
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

				_history.Add(newState);
				curPos = _history.Count;
			}
		}

		public List<RamSearchEngine.IMiniWatch> Undo()
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

		public List<RamSearchEngine.IMiniWatch> Redo()
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
