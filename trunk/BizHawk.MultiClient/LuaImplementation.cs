using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LuaInterface;
using System.Windows.Forms;
using BizHawk.MultiClient.tools;

namespace BizHawk.MultiClient
{
    class LuaImplementation
    {
        Lua lua = new Lua();
        LuaWindow Caller;
        public LuaImplementation(LuaWindow passed)
        {
            Caller = passed.get();
            lua.RegisterFunction("print",this, this.GetType().GetMethod("print"));
        }
        public void DoLuaFile(string File)
        {
            lua.DoFile(File);
        }
        public void print(string s)
        {
            Caller.AddText(s);
        }
    }
}
