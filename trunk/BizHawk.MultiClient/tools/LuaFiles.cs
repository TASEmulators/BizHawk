using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
    class LuaFiles
    {
        public string Name;
        public string Path;
        public bool Enabled;
        public bool IsSeparator;

        public LuaFiles(string path)
        {
            Name = "";
            Path = path;
            Enabled = true;
        }

        public LuaFiles(string name, string path, bool enabled)
        {
            Name = name;
            Path = path;
            Enabled = enabled;
            IsSeparator = false;
        }

        public LuaFiles(bool isSeparator)
        {
            IsSeparator = isSeparator;
            Name = "";
            Path = "";
            Enabled = false;
        }

        public LuaFiles(LuaFiles l)
        {
            Name = l.Name;
            Path = l.Path;
            Enabled = l.Enabled;
            IsSeparator = l.IsSeparator;
        }

        public void Toggle()
        {
            Enabled ^= true;
        }
    }
}
