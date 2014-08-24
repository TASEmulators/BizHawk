using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	[JsonObject]
	public class RecentFiles : IEnumerable
	{
		private List<string> recentlist;
		public RecentFiles() : this(8) { }
		public RecentFiles(int max)
		{
			recentlist = new List<string>();
			MAX_RECENT_FILES = max;
		}

		public int MAX_RECENT_FILES { get; set; }
		public bool AutoLoad { get; set; }

		/// <summary>
		/// If true, the list can not change, or be cleared
		/// </summary>
		public bool Frozen { get; set; }

		[JsonIgnore]
		public bool Empty
		{
			get { return !recentlist.Any(); }
		}

		[JsonIgnore]
		public int Count
		{
			get { return recentlist.Count; }
		}

		[JsonIgnore]
		public string MostRecent
		{
			get
			{
				return recentlist.Any() ? recentlist[0] : string.Empty;
			}
		}

		public string this[int index]
		{
			get
			{
				if (recentlist.Any())
				{
					return recentlist[index];
				}

				return string.Empty;
			}
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
			if (!Frozen)
			{
				recentlist.Clear();
			}
		}

		public void Add(string newFile)
		{
			if (!Frozen)
			{
				Remove(newFile);
				recentlist.Insert(0, newFile);

				if (recentlist.Count > MAX_RECENT_FILES)
				{
					recentlist.Remove(recentlist.Last());
				}
			}
		}

		public bool Remove(string newFile)
		{
			if (!Frozen)
			{
				var removed = false;
				foreach (var recent in recentlist.ToList())
				{
					if (string.Compare(newFile, recent, StringComparison.CurrentCultureIgnoreCase) == 0)
					{
						recentlist.Remove(newFile); // intentionally keeps iterating after this to remove duplicate instances, though those should never exist in the first place
						removed = true;
					}
				}

				return removed;
			}

			return false;
		}

		public void ToggleAutoLoad()
		{
			AutoLoad ^= true;
		}
	}
}
