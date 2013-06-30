using System.Collections.Generic;
using System.Linq;

namespace BizHawk.MultiClient
{
	public class HistoryCollection
	{
		public List<List<Watch>> History { get; private set; }
		private int curPos; //1-based

		public HistoryCollection()
		{
			History = new List<List<Watch>>();
		}
		
		public HistoryCollection(List<Watch> newState)
		{
			History = new List<List<Watch>>();
			AddState(newState);
		}

		public void Clear()
		{
			History = new List<List<Watch>>();
		}

		public bool CanUndo
		{
			get { return curPos > 1; }
		}

		public bool CanRedo
		{
			get { return curPos < History.Count; }
		}

		public bool HasHistory
		{
			get { return History.Any(); }
		}

		public void AddState(List<Watch> newState)
		{
			if (curPos < History.Count)
			{
				for (int i = curPos + 1; i <= History.Count; i++)
				{
					History.Remove(History[i - 1]);
				}
			}

			History.Add(newState);
			curPos = History.Count;
		}

		public List<Watch> Undo()
		{
			if (CanUndo)
			{
				curPos--;
				return History[curPos - 1];
			}
			else
			{
				return null;
			}
		}

		public List<Watch> Redo()
		{
			if (CanRedo)
			{
				curPos++;
				return History[curPos - 1];
			}
			else
			{
				return null;
			}
		}
	}
}
