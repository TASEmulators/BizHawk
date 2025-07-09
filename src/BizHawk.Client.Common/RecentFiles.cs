using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.Common
{
	public class RecentFiles
	{
		[JsonInclude]
		[JsonPropertyOrder(-1)]
		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		private List<string> recentlist;

		public RecentFiles()
			: this(8)
		{
		}

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
			=> recentlist.Count is 0;

		[JsonIgnore]
		public int Count => recentlist.Count;

		[JsonIgnore]
		public string MostRecent
			=> recentlist.Count is 0 ? string.Empty : recentlist[0];

		public string this[int index]
			=> recentlist.Count is 0 ? string.Empty : recentlist[index];

		public IEnumerator<string> GetEnumerator() => recentlist.GetEnumerator();

		public void Clear()
		{
			if (!Frozen)
			{
				recentlist.Clear();
			}
		}

		public void ClearMoved()
		{
			if (Frozen) return;
			recentlist.RemoveAll(entry =>
			{
				if (!OpenAdvancedSerializer.ParseRecentFile(filePath: ref entry, out _)) return false; // weird thing, don't touch
				// else `entry` is a regular file path
				return !File.Exists(entry);
			});
		}

		public void Add(string newFile)
		{
			if (!Frozen)
			{
				Remove(newFile);
				recentlist.Insert(0, newFile);

				if (recentlist.Count > MAX_RECENT_FILES)
				{
					recentlist.RemoveAt(recentlist.Count - 1);
				}
			}
		}

		public bool Remove(string newFile)
		{
			if (!Frozen)
			{
				return recentlist.RemoveAll(newFile.EqualsIgnoreCase) is not 0; // none removed => return false
			}

			return false;
		}

		public void ToggleAutoLoad()
			=> AutoLoad = !AutoLoad;
	}
}
