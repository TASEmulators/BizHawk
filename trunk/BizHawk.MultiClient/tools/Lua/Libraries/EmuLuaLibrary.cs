using System;
using System.Threading;
using LuaInterface;

namespace BizHawk.MultiClient
{
	public partial class EmuLuaLibrary
	{
		private Lua _lua = new Lua();
		private readonly LuaConsole _caller;
		private int _current_memory_domain; //Main memory by default
		private Lua currThread;

		public LuaDocumentation Docs = new LuaDocumentation();
		public bool IsRunning;
		public EventWaitHandle LuaWait;
		public bool FrameAdvanceRequested;

		public EmuLuaLibrary(LuaConsole passed)
		{
			LuaWait = new AutoResetEvent(false);
			Docs.Clear();
			_caller = passed.get();
			LuaRegister(_lua);
		}

		public void Close()
		{
			_lua = new Lua();
			foreach (var brush in SolidBrushes.Values) brush.Dispose();
			foreach (var brush in Pens.Values) brush.Dispose();
		}

		#region Register Library Functions

		public static string[] GuiFunctions = new[]
		{
			"addmessage",
			"alert",
			"cleartext",
			"drawBezier",
			"drawBox",
			"drawEllipse",
			"drawIcon",
			"drawImage",
			"drawLine",
			"drawPie",
			"drawPixel",
			"drawPolygon",
			"drawRectangle",
			"drawString",
			"drawText",
			"text",
		};

		public static string[] EmuFunctions = new[]
		{
			"displayvsync",
			"enablerewind",
			"frameadvance",
			"framecount",
			"frameskip",
			"getsystemid",
			"islagged",
			"ispaused",		
			"lagcount",
			"limitframerate",
			"minimizeframeskip",
			"on_snoop",
			"pause",
			"setrenderplanes",
			"speedmode",
			"togglepause",
			"unpause",
			"yield",
		};

		public static string[] EventFunctions = new[]
		{
			"onframeend",
			"onframestart",
			"oninputpoll",
			"onloadstate",
			"onmemoryread",
			"onmemorywrite",
			"onsavestate",
			"unregisterbyid",
			"unregisterbyname",
		};

		public static string[] FormsFunctions = new[]
		{
			"addclick",
			"button",
			"clearclicks",
			"destroy",
			"destroyall",
			"getproperty",
			"gettext",
			"label",
			"newform",
			"openfile",
			"setlocation",
			"setproperty",
			"setsize",
			"settext",
			"textbox",
		};

		public static string[] InputFunctions = new[]
		{
			"get",
			"getmouse"
		};

		public static string[] JoypadFunctions = new[]
		{
			"get",
			"getimmediate",
			"set",
			"setanalog"
		};

		public static string[] MainMemoryFunctions = new[]
		{
			"getname",
			"readbyte",
			"readbyterange",
			"readfloat",
			"writebyte",
			"writebyterange",
			"writefloat",

			"read_s8",
			"read_u8",
			"read_s16_le",
			"read_s24_le",
			"read_s32_le",
			"read_u16_le",
			"read_u24_le",
			"read_u32_le",
			"read_s16_be",
			"read_s24_be",
			"read_s32_be",
			"read_u16_be",
			"read_u24_be",
			"read_u32_be",
			"write_s8",
			"write_u8",
			"write_s16_le",
			"write_s24_le",
			"write_s32_le",
			"write_u16_le",
			"write_u24_le",
			"write_u32_le",
			"write_s16_be",
			"write_s24_be",
			"write_s32_be",
			"write_u16_be",
			"write_u24_be",
			"write_u32_be",
		};

		public static string[] MemoryFunctions = new[]
		{
			"getcurrentmemorydomain",
			"getcurrentmemorydomainsize",
			"getmemorydomainlist",
			"readbyte",
			"readfloat",
			"usememorydomain",
			"writebyte",
			"writefloat",

			"read_s8",
			"read_u8",
			"read_s16_le",
			"read_s24_le",
			"read_s32_le",
			"read_u16_le",
			"read_u24_le",
			"read_u32_le",
			"read_s16_be",
			"read_s24_be",
			"read_s32_be",
			"read_u16_be",
			"read_u24_be",
			"read_u32_be",
			"write_s8",
			"write_u8",
			"write_s16_le",
			"write_s24_le",
			"write_s32_le",
			"write_u16_le",
			"write_u24_le",
			"write_u32_le",
			"write_s16_be",
			"write_s24_be",
			"write_s32_be",
			"write_u16_be",
			"write_u24_be",
			"write_u32_be",
		};

		public static string[] MovieFunctions = new[]
		{
			"filename",
			"getinput",
			"getreadonly",
			"getrerecordcounting",
			"isloaded",
			"length",
			"mode",
			"rerecordcount",
			"setreadonly",
			"setrerecordcounting",
			"stop",
		};

		public static string[] SaveStateFunctions = new[]
		{
			"load",
			"loadslot",
			"registerload",
			"registersave",
			"save",
			"saveslot",
		};

		public void LuaRegister(Lua lua)
		{
			lua.RegisterFunction("print", this, GetType().GetMethod("print"));

			lua.NewTable("bit");
			foreach (var funcName in BitLuaLibrary.Functions)
			{
				string libName = BitLuaLibrary.Name + "." + funcName;
				var method = (typeof(BitLuaLibrary)).GetMethod(BitLuaLibrary.Name + "_" + funcName);
				lua.RegisterFunction(libName, this, method);
				Docs.Add(BitLuaLibrary.Name, funcName, method);
			}

			lua.NewTable("client");
			foreach (var funcName in MultiClientLuaLibrary.Functions)
			{
				string libName = MultiClientLuaLibrary.Name + "." + funcName;
				var method = (typeof(MultiClientLuaLibrary)).GetMethod(MultiClientLuaLibrary.Name + "_" + funcName);
				lua.RegisterFunction(libName, this, method);
				Docs.Add(MultiClientLuaLibrary.Name, funcName, method);
			}

			lua.NewTable("console");
			foreach (var funcName in ConsoleLuaLibrary.Functions)
			{
				string libName = ConsoleLuaLibrary.Name + "." + funcName;
				var method = (typeof(ConsoleLuaLibrary)).GetMethod(ConsoleLuaLibrary.Name + "_" + funcName);
				lua.RegisterFunction(libName, this, method);
				Docs.Add(ConsoleLuaLibrary.Name, funcName, method);
			}

			lua.NewTable("nes");
			foreach (var funcName in NESLuaLibrary.Functions)
			{
				string libName = NESLuaLibrary.Name + "." + funcName;
				var method = (typeof(NESLuaLibrary)).GetMethod(NESLuaLibrary.Name + "_" + funcName);
				lua.RegisterFunction(libName, this, method);
				Docs.Add(NESLuaLibrary.Name, funcName, method);
			}

			lua.NewTable("snes");
			foreach (var funcName in SNESLuaLibrary.Functions)
			{
				string libName = SNESLuaLibrary.Name + "." + funcName;
				var method = (typeof(SNESLuaLibrary)).GetMethod(SNESLuaLibrary.Name + "_" + funcName);
				lua.RegisterFunction(libName, this, method);
				Docs.Add(SNESLuaLibrary.Name, funcName, method);
			}

			lua.NewTable("gui");
			foreach (string t in GuiFunctions)
			{
				lua.RegisterFunction("gui." + t, this, GetType().GetMethod("gui_" + t));
				Docs.Add("gui", t, GetType().GetMethod("gui_" + t));
			}

			lua.NewTable("emu");
			foreach (string t in EmuFunctions)
			{
				lua.RegisterFunction("emu." + t, this, GetType().GetMethod("emu_" + t));
				Docs.Add("emu", t, GetType().GetMethod("emu_" + t));
			}

			lua.NewTable("memory");
			foreach (string t in MemoryFunctions)
			{
				lua.RegisterFunction("memory." + t, this, GetType().GetMethod("memory_" + t));
				Docs.Add("memory", t, GetType().GetMethod("memory_" + t));
			}

			lua.NewTable("mainmemory");
			foreach (string t in MainMemoryFunctions)
			{
				lua.RegisterFunction("mainmemory." + t, this,
									 GetType().GetMethod("mainmemory_" + t));
				Docs.Add("mainmemory", t, GetType().GetMethod("mainmemory_" + t));
			}

			lua.NewTable("savestate");
			foreach (string t in SaveStateFunctions)
			{
				lua.RegisterFunction("savestate." + t, this,
									 GetType().GetMethod("savestate_" + t));
				Docs.Add("savestate", t, GetType().GetMethod("savestate_" + t));
			}

			lua.NewTable("movie");
			foreach (string t in MovieFunctions)
			{
				lua.RegisterFunction("movie." + t, this, GetType().GetMethod("movie_" + t));
				Docs.Add("movie", t, GetType().GetMethod("movie_" + t));
			}

			lua.NewTable("input");
			foreach (string t in InputFunctions)
			{
				lua.RegisterFunction("input." + t, this, GetType().GetMethod("input_" + t));
				Docs.Add("input", t, GetType().GetMethod("input_" + t));
			}

			lua.NewTable("joypad");
			foreach (string t in JoypadFunctions)
			{
				lua.RegisterFunction("joypad." + t, this, GetType().GetMethod("joypad_" + t));
				Docs.Add("joypad", t, GetType().GetMethod("joypad_" + t));
			}

			lua.NewTable("forms");
			foreach (string t in FormsFunctions)
			{
				lua.RegisterFunction("forms." + t, this, GetType().GetMethod("forms_" + t));
				Docs.Add("forms", t, GetType().GetMethod("forms_" + t));
			}

			lua.NewTable("event");
			foreach (string t in EventFunctions)
			{
				lua.RegisterFunction("event." + t, this, GetType().GetMethod("event_" + t));
				Docs.Add("event", t, GetType().GetMethod("event_" + t));
			}

			Docs.Sort();
		}

		#endregion

		#region Library Helpers

		public Lua SpawnCoroutine(string File)
		{
			var t = _lua.NewThread();
			//LuaRegister(t); //adelikat: Not sure why this was here but it was causing the entire luaimplmeentaiton to be duplicated each time, eventually resulting in crashes
			var main = t.LoadFile(File);
			t.Push(main); //push main function on to stack for subsequent resuming
			return t;
		}

		/// <summary>
		/// LuaInterface requires the exact match of parameter count, except optional parameters. 
		/// So, if you want to support variable arguments, declare them as optional and pass
		/// them to this method.
		/// </summary>
		/// <param name="lua_args"></param>
		/// <returns></returns>
		private object[] LuaVarArgs(params object[] lua_args)
		{
			int n = lua_args.Length;
			int trim = 0;
			for (int i = n - 1; i >= 0; --i)
				if (lua_args[i] == null) ++trim;
			object[] lua_result = new object[n - trim];
			Array.Copy(lua_args, lua_result, n - trim);
			return lua_result;
		}

		public class ResumeResult
		{
			public bool WaitForFrame;
			public bool Terminated;
		}

		public ResumeResult ResumeScript(Lua script)
		{
			currThread = script;
			int execResult = script.Resume(0);
			currThread = null;
			var result = new ResumeResult();
			if (execResult == 0)
			{
				//terminated
				result.Terminated = true;
			}
			else
			{
				//yielded
				result.WaitForFrame = FrameAdvanceRequested;
			}
			FrameAdvanceRequested = false;
			return result;
		}

		public void print(string s)
		{
			_caller.AddText(s);
			_caller.AddText("\n");
		}

		#endregion
	}
}
