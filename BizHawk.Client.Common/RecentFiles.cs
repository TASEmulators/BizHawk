using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	[Newtonsoft.Json.JsonObject]
	public class RecentFiles : IEnumerable
	{
		public int MAX_RECENT_FILES { get; private set; }       // Maximum number of files
		public List<string> recentlist { get; private set; }    // List of recent files
		
		public bool AutoLoad = false;

		public RecentFiles() : this(8) { }
		public RecentFiles(int max)
		{
			recentlist = new List<string>();
			MAX_RECENT_FILES = max;
		}

		public IEnumerator<string> GetEnumerator()
		{
			return recentlist.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Clear()
		{
			recentlist.Clear();
		}

		public bool Empty
		{
			get { return !recentlist.Any(); }
		}

		public int Count
		{
			get { return recentlist.Count; }
		}

		public void Add(string newFile)
		{
			for (int i = 0; i < recentlist.Count; i++)
			{
				if (String.Compare(newFile, recentlist[i], StringComparison.CurrentCultureIgnoreCase) == 0)
				{
					recentlist.Remove(newFile); // intentionally keeps iterating after this to remove duplicate instances, though those should never exist in the first place
				}
			}

			recentlist.Insert(0, newFile);
			if (recentlist.Count > MAX_RECENT_FILES)
			{
				recentlist.Remove(recentlist[recentlist.Count - 1]);
			}
		}

		public bool Remove(string newFile)
		{
			var removed = false;
			for (int i = 0; i < recentlist.Count; i++)
			{
				if (String.Compare(newFile, recentlist[i], StringComparison.CurrentCultureIgnoreCase) == 0)
				{
					recentlist.Remove(newFile); // intentionally keeps iterating after this to remove duplicate instances, though those should never exist in the first place
					removed = true;
				}
			}

			return removed;
		}

		public List<string> GetRecentListTruncated(int length)
		{
			return recentlist.Select(t => t.Substring(0, length)).ToList();
		}

		public string this[int index]
		{
			get
			{
				if (recentlist.Any())
				{
					return recentlist[index];
				}
				else
				{
					return String.Empty;
				}
			}
		}

		public void ToggleAutoLoad()
		{
			AutoLoad ^= true;
		}

		public string MostRecent
		{
			get
			{
				return recentlist.Any() ? recentlist[0] : string.Empty;
			}
		}
	}
}
