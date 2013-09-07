using System.Collections.Generic;
using System.Linq;

namespace BizHawk.MultiClient
{
	public class HistoryCollection
	{
		public List<List<Watch_Legacy>> History { get; private set; }
		private int curPos; //1-based
		public bool Enabled { get; private set; }

		public HistoryCollection(bool enabled)
		{
			History = new List<List<Watch_Legacy>>();
			Enabled = enabled;
		}
		
		public HistoryCollection(List<Watch_Legacy> newState, bool enabled)
		{
			History = new List<List<Watch_Legacy>>();
			AddState(newState);
			Enabled = enabled;
		}

		public void Clear()
		{
			History = new List<List<Watch_Legacy>>();
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

		public void AddState(List<Watch_Legacy> newState)
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

		public List<Watch_Legacy> Undo()
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

		public List<Watch_Legacy> Redo()
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
