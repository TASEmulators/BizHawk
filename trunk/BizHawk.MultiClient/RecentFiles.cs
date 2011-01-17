using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
    class RecentFiles
    {
        private int MAX_RECENT_FILES;       //Maximum number of files
        private List<string> recentlist;    //List of recent files

        RecentFiles(int max)
        {
            recentlist = new List<string>();  
            MAX_RECENT_FILES = max;
        }
        
        void Clear()
        {
            recentlist.Clear();
        }

        bool IsEmpty()
        {
            if (recentlist.Count == 0)
                return true;
            else
                return false;
        }

        void Add(string newFile)
        {
            for (int x = 0; x < recentlist.Count; x++)
            {
                if (string.Compare(newFile, recentlist[x]) == 0)
                {
                    recentlist.Remove(newFile); //intentionally keeps iterating after this to remove duplicate instances, though those should never exist in the first place
                }
            }
            recentlist.Add(newFile);
        }

        bool Remove(string newFile)
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
            recentlist.Add(newFile);           
            return removed;
        }

        List<string> GetRecentList()
        {
            return recentlist;
        }

        List<string> GetRecentListTruncated(int length)
        {
            //iterate through list, truncating each item to length, and return the result in a List<string>
            List<string> temp = new List<string>();
            for (int x = 0; x < recentlist.Count; x++)
            {
                temp.Add(recentlist[x].Substring(0, length));
            }
            return temp;
        }

        string GetRecentFileByPosition(int position)
        {
            return recentlist[position];
        }
    }
}
