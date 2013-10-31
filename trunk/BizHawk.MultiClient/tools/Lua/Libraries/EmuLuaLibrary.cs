using System;
using System.Threading;

using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	public partial class EmuLuaLibrary
	{
		private Lua _lua = new Lua();
		private readonly LuaConsole _caller;
		private Lua currThread;
		private FormsLuaLibrary _formsLibrary = new FormsLuaLibrary();
		private EventLuaLibrary _eventLibrary = new EventLuaLibrary();

		public LuaDocumentation Docs = new LuaDocumentation();
		public bool IsRunning;
		public EventWaitHandle LuaWait;
		public bool FrameAdvanceRequested;

		public void WindowClosed(IntPtr handle)
		{
			_formsLibrary.WindowClosed(handle);
		}

		public void CallSaveStateEvent(string name)
		{
			_eventLibrary.CallSaveStateEvent(name);
		}

		public void CallLoadStateEvent(string name)
		{
			_eventLibrary.CallLoadStateEvent(name);
		}

		public LuaFunctionList RegisteredFunctions
		{
			get { return _eventLibrary.RegisteredFunctions; }
		}

		public void CallFrameBeforeEvent()
		{
			_eventLibrary.CallFrameBeforeEvent();
		}

		public void CallFrameAfterEvent()
		{
			_eventLibrary.CallFrameAfterEvent();
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

		public void LuaRegister(Lua lua)
		{
			lua.RegisterFunction("print", this, GetType().GetMethod("print"));

			new BitLuaLibrary().LuaRegister(lua, Docs);
			new MultiClientLuaLibrary(ConsoleLuaLibrary.console_log).LuaRegister(lua, Docs);
			new ConsoleLuaLibrary().LuaRegister(lua, Docs);
			
			new EmulatorLuaLibrary(
				new Action(Frameadvance),
				new Action(EmuYield)
			).LuaRegister(lua, Docs);

			_eventLibrary.LuaRegister(lua, Docs);
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
		}

		private void Frameadvance()
		{
			FrameAdvanceRequested = true;
			currThread.Yield(0);
		}

		private void EmuYield()
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			currThread.Yield(0);
		}

		#endregion
	}
}
