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
		LuaConsole Caller;
		public static string[] EmuFunctions = new string[] {
			"frameadvance",
			"pause",
			"unpause",
			"speedmode",
			"framecount",
			"lagcount",
			"islagged",
			"registerbefore",
			"registerafter",
			"register"
		};
		public static string[] MemoryFunctions = new string[] {
			"readbyte",
			//"readbytesigned",
			//"readword",
			//"readwordsigned",
			//"readdword",
			//"readdwordsigned",
			//"readbyterange",
			//"writebyte",
			//"writeword",
			//"writedword",
			//"registerwrite",
			//"registerread",
			};
		public static string[] SaveStateFunctions = new string[] {
			"create",
			"save",
			"load",
			"write"
		};
		public static string[] MovieFunctions = new string[] {
			"mode",
			"rerecordcount",
			"stop"
		};
		public static string[] JoypadFunctions = new string[] {
			"set",
			//"get",
		};
		public LuaImplementation(LuaConsole passed)
		{
			Caller = passed.get();
			lua.RegisterFunction("print", this, this.GetType().GetMethod("print"));
			lua.NewTable("memory");
			for (int i = 0; i < MemoryFunctions.Length; i++)
			{
				lua.RegisterFunction("memory." + MemoryFunctions[i], this, this.GetType().GetMethod("memory_" + MemoryFunctions[i]));
			}
			lua.NewTable("joypad");
			for (int i = 0; i < JoypadFunctions.Length; i++)
			{
				lua.RegisterFunction("joypad." + MemoryFunctions[i], this, this.GetType().GetMethod("joypad_" + JoypadFunctions[i]));
			}
		}

		public void DoLuaFile(string File)
		{

		}
		public void print(string s)
		{
			Caller.AddText(string.Format(s));
		}
		public string memory_readbyte(object lua_input)
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
		public void memory_writebyte(object lua_input)
		{
			Global.Emulator.MainMemory.PokeByte((int)lua_input, (byte)lua_input);
		}
		public void joypad_get(object lua_input)
		{

		}
		public void joypad_set(object lua_input)
		{

		}
		public string movie_rerecordcount()
		{
			return "No";
		}
		public void movie_stop()
		{
		}
	}
}
