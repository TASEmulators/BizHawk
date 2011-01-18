using System.Collections.Generic;
using System.Text;

namespace BizHawk.MultiClient
{
    public class RecentFiles : IConfigSerializable
    {
        private int MAX_RECENT_FILES;       //Maximum number of files
        private List<string> recentlist;    //List of recent files

        public RecentFiles() : this(8) {} 
        public RecentFiles(int max)
        {
            recentlist = new List<string>();  
            MAX_RECENT_FILES = max;
        }
        
        public void Clear()
        {
            recentlist.Clear();
        }

        public bool IsEmpty()
        {
            if (recentlist.Count == 0)
                return true;
            else
                return false;
        }

        public int Length()
        {
            return recentlist.Count;
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
                recentlist.Remove(recentlist[recentlist.Count-1]);
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

        public List<string> GetRecentList()
        {
            return recentlist;
        }

        public List<string> GetRecentListTruncated(int length)
        {
            //iterate through list, truncating each item to length, and return the result in a List<string>
            List<string> temp = new List<string>();
            for (int x = 0; x < recentlist.Count; x++)
            {
                temp.Add(recentlist[x].Substring(0, length));
            }
            return temp;
        }

        public string GetRecentFileByPosition(int position)
        {
            return recentlist[position];
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(MAX_RECENT_FILES);
            sb.Append("@");
            foreach (string file in recentlist)
                sb.AppendFormat("\"{0}\"|", file);
            return sb.ToString();
        }

        public void Deserialize(string str)
        {
                var sections = str.Split('@');
                MAX_RECENT_FILES = int.Parse(sections[0]);
                var files = sections[1].Split('|');
                recentlist.Clear();
                foreach (string file in files)
                    if (string.IsNullOrEmpty(file) == false)
                        recentlist.Add(file.Replace("\"", ""));
        }
    }
}
