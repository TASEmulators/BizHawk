using System;
using System.Threading;

using BizHawk.Client.Common;
using LuaInterface;

namespace BizHawk.Client.EmuHawk
{
	public class EmuLuaLibrary
	{
		private readonly FormsLuaLibrary _formsLibrary = new FormsLuaLibrary();
		private readonly EventLuaLibrary _eventLibrary = new EventLuaLibrary(ConsoleLuaLibrary.Log);
		private readonly GuiLuaLibrary _guiLibrary = new GuiLuaLibrary();
		private readonly LuaConsole _caller;

		private Lua _lua = new Lua();
		private Lua _currThread;

		public EmuLuaLibrary()
		{
			Docs = new LuaDocumentation();
		}

		public EmuLuaLibrary(LuaConsole passed)
			: this()
		{
			LuaWait = new AutoResetEvent(false);
			Docs.Clear();
			_caller = passed.Get();
			LuaRegister(_lua);
		}

		public LuaDocumentation Docs { get; private set; }
		public bool IsRunning { get; set; }
		public EventWaitHandle LuaWait { get; private set; }
		public bool FrameAdvanceRequested { get; private set; }

		public GuiLuaLibrary GuiLibrary
		{
			get { return _guiLibrary; }
		}

		public LuaFunctionList RegisteredFunctions
		{
			get { return _eventLibrary.RegisteredFunctions; }
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

		public void CallFrameBeforeEvent()
		{
			_eventLibrary.CallFrameBeforeEvent();
		}

		public void CallFrameAfterEvent()
		{
			_eventLibrary.CallFrameAfterEvent();
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
			new EmuHawkLuaLibrary(ConsoleLuaLibrary.Log).LuaRegister(lua, Docs);
			new ConsoleLuaLibrary().LuaRegister(lua, Docs);
			
			new EmulatorLuaLibrary(
				_lua,
				Frameadvance,
				EmuYield).LuaRegister(lua, Docs);

			_eventLibrary.LuaRegister(lua, Docs);
			_formsLibrary.LuaRegister(lua, Docs);
			_guiLibrary.LuaRegister(lua, Docs);
			new InputLuaLibrary(_lua).LuaRegister(lua, Docs);
			new JoypadLuaLibrary(_lua).LuaRegister(lua, Docs);
			new MemoryLuaLibrary(_lua).LuaRegister(lua, Docs);
			new MainMemoryLuaLibrary(_lua).LuaRegister(lua, Docs);
			new MovieLuaLibrary(_lua).LuaRegister(lua, Docs);
			new NesLuaLibrary().LuaRegister(lua, Docs);
			new SavestateLuaLibrary().LuaRegister(lua, Docs);
			new SnesLuaLibrary().LuaRegister(lua, Docs);
			new StringLuaLibrary(_lua).LuaRegister(lua, Docs);
			new GameInfoLuaLibrary(_lua).LuaRegister(lua, Docs);
			Docs.Sort();
		}

		public Lua SpawnCoroutine(string file)
		{
			var lua = _lua.NewThread();
			var main = lua.LoadFile(file);
			lua.Push(main); // push main function on to stack for subsequent resuming
			return lua;
		}

		public ResumeResult ResumeScript(Lua script)
		{
			_eventLibrary.CurrentThread = script;
			_currThread = script;
			var execResult = script.Resume(0);
			_currThread = null;
			var result = new ResumeResult();
			if (execResult == 0)
			{
				// terminated
				result.Terminated = true;
			}
			else
			{
				// yielded
				result.WaitForFrame = FrameAdvanceRequested;
			}

			FrameAdvanceRequested = false;
			return result;
		}

		public static void Print(params object[] outputs)
		{
			ConsoleLuaLibrary.Log(outputs);
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

		public class ResumeResult
		{
			public bool WaitForFrame { get; set; }
			public bool Terminated { get; set; }
		}
	}
}
