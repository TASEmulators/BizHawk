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

		public LuaImplementation(LuaConsole passed)
		{
			Caller = passed.get();
			lua.RegisterFunction("print", this, this.GetType().GetMethod("print"));

			//Register libraries
			lua.NewTable("console");
			for (int i = 0; i < ConsoleFunctions.Length; i++)
			{
				lua.RegisterFunction("console." + ConsoleFunctions[i], this, this.GetType().GetMethod("console_" + ConsoleFunctions[i]));
			}

			lua.NewTable("emu");
			for (int i = 0; i < EmuFunctions.Length; i++)
			{
				lua.RegisterFunction("emu." + EmuFunctions[i], this, this.GetType().GetMethod("emu_" + EmuFunctions[i]));
			}

			lua.NewTable("memory");
			for (int i = 0; i < MemoryFunctions.Length; i++)
			{
				lua.RegisterFunction("memory." + MemoryFunctions[i], this, this.GetType().GetMethod("memory_" + MemoryFunctions[i]));
			}

			lua.NewTable("savestate");
			for (int i = 0; i < SaveStateFunctions.Length; i++)
			{
				//lua.RegisterFunction("statestate." + SaveStateFunctions[i], this, this.GetType().GetMethod("savestate_" + SaveStateFunctions[i]));
			}

			lua.NewTable("movie");
			for (int i = 0; i < MovieFunctions.Length; i++)
			{
				lua.RegisterFunction("movie." + MovieFunctions[i], this, this.GetType().GetMethod("movie_" + MovieFunctions[i]));
			}

			lua.NewTable("joypad");
			for (int i = 0; i < JoypadFunctions.Length; i++)
			{
				lua.RegisterFunction("joypad." + MemoryFunctions[i], this, this.GetType().GetMethod("joypad_" + JoypadFunctions[i]));
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

		/****************************************************/
		/*************library definitions********************/
		/****************************************************/
		public static string[] ConsoleFunctions = new string[] {
			"output"
		};

		public static string[] EmuFunctions = new string[] {
			//"frameadvance",
			"pause",
			"unpause",
			"togglepause",
			//"speedmode",
			//"framecount",
			//"lagcount",
			//"islagged",
			//"registerbefore",
			//"registerafter",
			//"register"
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
			//"create",
			"save",
			//"load",
			//"write"
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

		/****************************************************/
		/*************function definitions********************/
		/****************************************************/

		//----------------------------------------------------
		//Console library
		//----------------------------------------------------

		public void console_output(object lua_input)
		{
			Global.MainForm.LuaConsole1.WriteToOutputWindow(lua_input.ToString());
		}

		//----------------------------------------------------
		//Emu library
		//----------------------------------------------------

		public void emu_pause()
		{
			Global.MainForm.PauseEmulator();
		}

		public void emu_unpause()
		{
			Global.MainForm.UnpauseEmulator();
		}

		public void emu_togglepause()
		{
			Global.MainForm.TogglePause();
		}

		//----------------------------------------------------
		//Memory library
		//----------------------------------------------------
		
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

		//----------------------------------------------------
		//Savestate library
		//----------------------------------------------------

		public void savestate_save(object lua_input)
		{
			//
		}

		//----------------------------------------------------
		//Movie library
		//----------------------------------------------------
		public string movie_mode()
		{
			return Global.MovieSession.Movie.Mode.ToString();
		}

		public string movie_rerecordcount()
		{
			return Global.MovieSession.Movie.Rerecords.ToString();
		}
		public void movie_stop()
		{
			Global.MovieSession.Movie.StopMovie();
		}

		//----------------------------------------------------
		//Joypad library
		//----------------------------------------------------

		public void joypad_get(object lua_input)
		{

		}
		
		public void joypad_set(object lua_input)
		{

		}
	}
}
