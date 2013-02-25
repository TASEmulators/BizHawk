using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
	public class HistoryCollection
	{
		public List<List<Watch>> History = new List<List<Watch>>();
		private int curPos = 0; //1-based

		public HistoryCollection(List<Watch> newState)
		{
			AddState(newState);
		}

		public bool CanUndo
		{
			get
			{
				return curPos > 1;
			}
		}

		public bool CanRedo
		{
			get
			{
				return curPos < History.Count;
			}
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
