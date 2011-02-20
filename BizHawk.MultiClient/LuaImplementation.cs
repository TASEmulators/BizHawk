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
        public static string[] MemoryFunctions = new string[] {
            "readbyte",
            "writebyte"};           
        public LuaImplementation(LuaWindow passed)
        {
            Caller = passed.get();
            lua.RegisterFunction("print",this, this.GetType().GetMethod("print"));
			lua.NewTable("memory");
			for (int i = 0; i < MemoryFunctions.Length; i++)
            {
                lua.RegisterFunction("memory." + MemoryFunctions[i], this, this.GetType().GetMethod(MemoryFunctions[i]));
			}

        }
        public void DoLuaFile(string File)
        {
            lua.DoFile(File);
        }
        public void print(string s)
        {
            Caller.AddText(string.Format(s));
        }
        public string readbyte(object lua_input)
        {
            
            byte x;
            if (lua_input.GetType() == typeof(string))
            {
                x = Global.Emulator.MainMemory.PeekByte(int.Parse((string)lua_input));
                return x.ToString();
            }
            else
            {
                double y = (double)lua_input;             
                x = Global.Emulator.MainMemory.PeekByte(Convert.ToInt32(y));
                return x.ToString();
            }
             
        }
        public void writebyte(object lua_input)
        {            
            Global.Emulator.MainMemory.PokeByte((int)lua_input, (byte)lua_input);
        }
    }
}
