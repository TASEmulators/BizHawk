using System;
using System.Threading;
using LuaInterface;

namespace BizHawk.MultiClient
{
	public partial class EmuLuaLibrary
	{
		private Lua _lua = new Lua();
		private readonly LuaConsole _caller;
		private Lua currThread;
		private FormsLuaLibrary _formsLibrary = new FormsLuaLibrary();

		public LuaDocumentation Docs = new LuaDocumentation();
		public bool IsRunning;
		public EventWaitHandle LuaWait;
		public bool FrameAdvanceRequested;

		public void WindowClosed(IntPtr handle)
		{
			_formsLibrary.WindowClosed(handle);
		}

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

		public void LuaRegister(Lua lua)
		{
			lua.RegisterFunction("print", this, GetType().GetMethod("print"));

			new BitLuaLibrary().LuaRegister(lua, Docs);
			new MultiClientLuaLibrary(ConsoleLuaLibrary.console_log).LuaRegister(lua, Docs);
			new ConsoleLuaLibrary().LuaRegister(lua, Docs);
			_formsLibrary.LuaRegister(lua, Docs);
			new InputLuaLibrary(_lua).LuaRegister(lua, Docs);
			new JoypadLuaLibrary(_lua).LuaRegister(lua, Docs);
			new MemoryLuaLibrary().LuaRegister(lua, Docs);
			new MainMemoryLuaLibrary(_lua).LuaRegister(lua, Docs);
			new MovieLuaLibrary(_lua).LuaRegister(lua, Docs);
			new NESLuaLibrary().LuaRegister(lua, Docs);
			new SavestateLuaLibrary().LuaRegister(lua, Docs);
			new SNESLuaLibrary().LuaRegister(lua, Docs);

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
