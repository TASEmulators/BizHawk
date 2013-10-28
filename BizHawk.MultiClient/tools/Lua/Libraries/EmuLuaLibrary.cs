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

		public static string[] BitwiseFunctions = new[]
		{
			"band",
			"bnot",
			"bor",
			"bxor",
			"lshift",
			"rol",
			"ror",
			"rshift",
		};

		public static string[] MultiClientFunctions = new[]
		{
			"closerom",
			"getwindowsize",
			"opencheats",
			"openhexeditor",
			"openramwatch",
			"openramsearch",
			"openrom",
			"opentasstudio",
			"opentoolbox",
			"opentracelogger",
			"pause_av",
			"reboot_core",
			"screenheight",
			"screenshot",
			"screenshottoclipboard",
			"screenwidth",
			"setscreenshotosd",
			"setwindowsize",
			"unpause_av",
			"xpos",
			"ypos",
		};

		public static string[] ConsoleFunctions = new[]
		{
			"clear",
			"getluafunctionslist",
			"log",
			"output",
		};

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

		public static string[] NESFunctions = new[]
		{
			"addgamegenie",
			"getallowmorethaneightsprites",
			"getbottomscanline",
			"getclipleftandright",
			"getdispbackground",
			"getdispsprites",
			"gettopscanline",
			"removegamegenie",
			"setallowmorethaneightsprites",
			"setclipleftandright",
			"setdispbackground",
			"setdispsprites",
			"setscanlines",
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

		public static string[] SNESFunctions = new[]
		{
			"getlayer_bg_1",
			"getlayer_bg_2",
			"getlayer_bg_3",
			"getlayer_bg_4",
			"getlayer_obj_1",
			"getlayer_obj_2",
			"getlayer_obj_3",
			"getlayer_obj_4",
			"setlayer_bg_1",
			"setlayer_bg_2",
			"setlayer_bg_3",
			"setlayer_bg_4",
			"setlayer_obj_1",
			"setlayer_obj_2",
			"setlayer_obj_3",
			"setlayer_obj_4",
		};

		public void LuaRegister(Lua lua)
		{
			lua.RegisterFunction("print", this, GetType().GetMethod("print"));

			//Register libraries
			lua.NewTable("console");
			foreach (string t in ConsoleFunctions)
			{
				lua.RegisterFunction("console." + t, this,
									 GetType().GetMethod("console_" + t));
				Docs.Add("console", t, GetType().GetMethod("console_" + t));
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

			lua.NewTable("client");
			foreach (string t in MultiClientFunctions)
			{
				lua.RegisterFunction("client." + t, this,
									 GetType().GetMethod("client_" + t));
				Docs.Add("client", t, GetType().GetMethod("client_" + t));
			}

			lua.NewTable("forms");
			foreach (string t in FormsFunctions)
			{
				lua.RegisterFunction("forms." + t, this, GetType().GetMethod("forms_" + t));
				Docs.Add("forms", t, GetType().GetMethod("forms_" + t));
			}

			lua.NewTable("bit");
			foreach (string t in BitwiseFunctions)
			{
				lua.RegisterFunction("bit." + t, this, GetType().GetMethod("bit_" + t));
				Docs.Add("bit", t, GetType().GetMethod("bit_" + t));
			}

			lua.NewTable("nes");
			foreach (string t in NESFunctions)
			{
				lua.RegisterFunction("nes." + t, this, GetType().GetMethod("nes_" + t));
				Docs.Add("nes", t, GetType().GetMethod("nes_" + t));
			}

			lua.NewTable("snes");
			foreach (string t in SNESFunctions)
			{
				lua.RegisterFunction("snes." + t, this, GetType().GetMethod("snes_" + t));
				Docs.Add("snes", t, GetType().GetMethod("snes_" + t));
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

		public int LuaInt(object lua_arg)
		{
			return Convert.ToInt32((double)lua_arg);
		}

		private uint LuaUInt(object lua_arg)
		{
			return Convert.ToUInt32((double)lua_arg);
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
