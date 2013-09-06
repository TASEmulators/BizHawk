using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public class RecentFiles : IEnumerable
	{
		private readonly int MAX_RECENT_FILES;       //Maximum number of files
		private readonly List<string> recentlist;    //List of recent files

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

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

		public void Clear()
		{
			recentlist.Clear();
		}

		public bool Empty
		{
			get { return recentlist.Count == 0; }
		}

		public int Count
		{
			get { return recentlist.Count; }
		}

		public void Add(string newFile)
		{
			for (int x = 0; x < recentlist.Count; x++)
			{
				if (string.Compare(newFile, recentlist[x]) == 0)
				{
					recentlist.Remove(newFile); //intentionally keeps iterating after this to remove duplicate instances, though those should never exist in the first place
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
			bool removed = false;
			for (int x = 0; x < recentlist.Count; x++)
			{
				if (string.Compare(newFile, recentlist[x]) == 0)
				{
					recentlist.Remove(newFile); //intentionally keeps iterating after this to remove duplicate instances, though those should never exist in the first place
					removed = true;
				}
			}
			return removed;
		}

		public List<string> GetRecentListTruncated(int length)
		{
			//iterate through list, truncating each item to length, and return the result in a List<string>
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
                    return "";
                }
            }
        }

        public void GenerateRecentMenu(ToolStripMenuItem menu, Action<string> loadFileCallback,  bool? autoloadConfigVar = null, Action autoloadCallback = null)
        {
            menu.DropDownItems.Clear();

            if (Empty)
            {
                var none = new ToolStripMenuItem { Enabled = false, Text = "None" };
                menu.DropDownItems.Add(none);
            }
            else
            {
                foreach (string filename in recentlist)
               {
                   var item = new ToolStripMenuItem { Text = filename };
                   item.Click += (o, ev) => loadFileCallback(filename);
                   menu.DropDownItems.Add(item);
               }
            }

            menu.DropDownItems.Add("-");

            var clearitem = new ToolStripMenuItem { Text = "&Clear" };
            clearitem.Click += (o, ev) => recentlist.Clear();
            menu.DropDownItems.Add(clearitem);

            if (autoloadConfigVar.HasValue)
            {
                var auto = new ToolStripMenuItem { Text = "&Auto-Load", Checked = autoloadConfigVar.Value };
                auto.Click += (o, ev) => autoloadCallback();
                menu.DropDownItems.Add(auto);
            }
        }
	}
}
