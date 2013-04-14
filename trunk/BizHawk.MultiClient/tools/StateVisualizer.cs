using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BizHawk.MultiClient
{
	class StateVisualizer
	{
		public StateVisualizer()
		{
			TimeLine movietimeline = new TimeLine(Global.MovieSession.Movie.LogDump);

			Timelines = new List<TimeLine> {movietimeline};

			//Load all 10 saveslots and process
			for (int i = 0; i < 10; i++)
			{
				string name = "QuickSave" + i.ToString();
				string path = PathManager.SaveStatePrefix(Global.Game) + "." + name + ".State";
				if (File.Exists(path))
				{
					Movie m = new Movie();
					m.LoadLogFromSavestateText(path);
					AddLog(m.LogDump, i);
				}
			}
		}

		public int TimeLineCount
		{
			get
			{
				return Timelines.Count;
			}
		}

		private void AddLog(MovieLog log, int? slot)
		{
			sort();

			bool added = false;
			foreach (TimeLine timeline in Timelines)
			{
				if (timeline.TryAddToTimeline(log, slot))
				{
					added = true;
				}
			}

			if (!added)
			{
				TimeLine t = new TimeLine(log);
				Timelines.Add(t);
			}
		}

		private void sort()
		{
			Timelines = Timelines.OrderByDescending(x => x.Length).ToList();
		}

		List<TimeLine> Timelines; //MovieLogs of all savestates and the loaded movie

		//Represents a single timeline that consists of at least 1 slot
		private class TimeLine
		{
			public TimeLine(MovieLog log, int? slot_number = null)
			{
				timeline = new Event(log, slot_number);
				subevents = new List<Event>();
			}

			private Event timeline;
			private List<Event> subevents;

			private class Event
			{
				public int? Slot;
				public MovieLog Log;

				public Event(MovieLog log, int? slot)
				{
					Slot = slot;
					Log = log;
				}

				public Event()
				{
					Slot = null;
					Log = new MovieLog();
				}
			}

			public int Points
			{
				get
				{
					return subevents.Count + 1;
				}
			}

			public int Length
			{
				get
				{
					return timeline.Log.Length;
				}
			}

			public int? GetPoint(int position)
			{
				sort();
				if (position < subevents.Count)
				{
					return subevents[position].Log.Length;
				}
				else
				{
					return null;
				}
			}

			public bool TryAddToTimeline(MovieLog log, int? slot)
			{
				bool isdifferent = false;
				if (log.Length < timeline.Log.Length)
				{
					for (int i = 0; i < log.Length; i++)
					{
						if (log.GetFrame(i) != timeline.Log.GetFrame(i))
						{
							isdifferent = true;
						}
					}

					if (isdifferent)
					{
						return false;
					}
					else
					{
						subevents.Add(new Event(log, slot));
						sort();
						return true;
					}
				}
				else
				{
					for (int i = 0; i < timeline.Log.Length; i++)
					{
						if (log.GetFrame(i) != timeline.Log.GetFrame(i))
						{
							isdifferent = true;
						}
					}

					if (isdifferent)
					{
						return false;
					}
					else
					{
						subevents.Add(timeline);
						timeline = new Event(log, slot);
						sort();
						return true;
					}
				}
			}

			private void sort()
			{
				subevents = subevents.OrderByDescending(x => x.Log.Length).ToList();
			}
		}
	}
}
