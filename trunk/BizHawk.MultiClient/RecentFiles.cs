using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.MultiClient
{
	public class RecentFiles
	{
		private readonly int MAX_RECENT_FILES;       //Maximum number of files
		private readonly List<string> _recent_list;    //List of recent files

		public RecentFiles() 
            : this(8) { }

		public RecentFiles(int max)
		{
			_recent_list = new List<string>();
			MAX_RECENT_FILES = max;
		}

		public void Clear()
		{
			_recent_list.Clear();
		}

		public bool Empty
		{
			get { return _recent_list.Count == 0; }
		}

		public int Count
		{
			get { return _recent_list.Count; }
		}

		public void Add(string newFile)
		{
            foreach (string recent in _recent_list)
            {
                if (String.Compare(newFile, recent, true) == 0)
                {
                    _recent_list.Remove(newFile);  //intentionally keeps iterating after this to remove duplicate instances, though those should never exist in the first place
                }
            }

			_recent_list.Insert(0, newFile);
			if (_recent_list.Count > MAX_RECENT_FILES)
			{
				_recent_list.Remove(_recent_list[_recent_list.Count - 1]);
			}
		}

		public bool Remove(string newFile)
		{
			bool removed = false;
            foreach(string recent in _recent_list)
			{
                if (string.Compare(newFile, recent, true) == 0)
				{
					_recent_list.Remove(newFile); //intentionally keeps iterating after this to remove duplicate instances, though those should never exist in the first place
					removed = true;
				}
			}
			return removed;
		}

		public List<string> GetRecentListTruncated(int length)
		{
			//iterate through list, truncating each item to length, and return the result in a List<string>
			return _recent_list.Select(t => t.Substring(0, length)).ToList();
		}

		public string GetRecentFileByPosition(int position)
		{
			if (_recent_list.Count > 0)
			{
				return _recent_list[position];
			}
			else
			{
				return "";
			}
		}
	}
}
