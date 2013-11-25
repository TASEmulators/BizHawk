using System;
using System.Threading;

using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class EmuLuaLibrary
	{
		private Lua _lua = new Lua();
		private readonly LuaConsole _caller;
		private Lua _currThread;
		private readonly FormsLuaLibrary _formsLibrary = new FormsLuaLibrary();
		private readonly EventLuaLibrary _eventLibrary = new EventLuaLibrary(ConsoleLuaLibrary.console_log);
		private readonly GuiLuaLibrary _guiLibrary = new GuiLuaLibrary();

		public LuaDocumentation Docs = new LuaDocumentation();
		public bool IsRunning;
		public EventWaitHandle LuaWait;
		public bool FrameAdvanceRequested;

		public GuiLuaLibrary GuiLibrary
		{
			get { return _guiLibrary; }
		}

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
			_caller = passed.Get();
			LuaRegister(_lua);
		}

		public void Close()
		{
			_lua = new Lua();
			_guiLibrary.Dispose();
		}

		public void LuaRegister(Lua lua)
		{
			lua.RegisterFunction("print", this, GetType().GetMethod("Print"));

			new BitLuaLibrary().LuaRegister(lua, Docs);
			new MultiClientLuaLibrary(ConsoleLuaLibrary.console_log).LuaRegister(lua, Docs);
			new ConsoleLuaLibrary().LuaRegister(lua, Docs);
			
			new EmulatorLuaLibrary(
				_lua,
				Frameadvance,
				EmuYield
			).LuaRegister(lua, Docs);

			_eventLibrary.LuaRegister(lua, Docs);
			_formsLibrary.LuaRegister(lua, Docs);
			_guiLibrary.LuaRegister(lua, Docs);
			new InputLuaLibrary(_lua).LuaRegister(lua, Docs);
			new JoypadLuaLibrary(_lua).LuaRegister(lua, Docs);
			new MemoryLuaLibrary().LuaRegister(lua, Docs);
			new MainMemoryLuaLibrary(_lua).LuaRegister(lua, Docs);
			new MovieLuaLibrary(_lua).LuaRegister(lua, Docs);
			new NESLuaLibrary().LuaRegister(lua, Docs);
			new SavestateLuaLibrary().LuaRegister(lua, Docs);
			new SNESLuaLibrary().LuaRegister(lua, Docs);

			Docs.Sort();
		}

		public Lua SpawnCoroutine(string file)
		{
			Lua lua = _lua.NewThread();
			var main = lua.LoadFile(file);
			lua.Push(main); //push main function on to stack for subsequent resuming
			return lua;
		}

		public class ResumeResult
		{
			public bool WaitForFrame;
			public bool Terminated;
		}

		public ResumeResult ResumeScript(Lua script)
		{
			_currThread = script;
			int execResult = script.Resume(0);
			_currThread = null;
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

		public void Print(string s)
		{
			_caller.ConsoleLog(s);
		}

		private void Frameadvance()
		{
			FrameAdvanceRequested = true;
			_currThread.Yield(0);
		}

		private void EmuYield()
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			_currThread.Yield(0);
		}
	}
}
