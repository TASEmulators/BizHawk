using System.Collections.Generic;
using System.Linq;

namespace BizHawk.MultiClient
{
	public class HistoryCollection
	{
		public List<List<Watch>> History { get; private set; }
		private int curPos; //1-based
		public bool Enabled { get; private set; }

		public HistoryCollection(bool enabled)
		{
			History = new List<List<Watch>>();
			Enabled = enabled;
		}
		
		public HistoryCollection(List<Watch> newState, bool enabled)
		{
			History = new List<List<Watch>>();
			AddState(newState);
			Enabled = enabled;
		}

		public void Clear()
		{
			History = new List<List<Watch>>();
		}

		public bool CanUndo
		{
			get { return Enabled && curPos > 1; }
		}

		public bool CanRedo
		{
			get { return Enabled && curPos < History.Count; }
		}

		public bool HasHistory
		{
			get { return Enabled && History.Any(); }
		}

		public void AddState(List<Watch> newState)
		{
			if (Enabled)
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
		}

		public List<Watch> Undo()
		{
			if (CanUndo && Enabled)
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
			if (CanRedo && Enabled)
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
