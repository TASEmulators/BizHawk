using System;
using System.Threading;

using BizHawk.Client.Common;
using LuaInterface;

namespace BizHawk.Client.EmuHawk
{
	public class EmuLuaLibrary
	{
		private readonly FormsLuaLibrary _formsLibrary;
		private readonly EventLuaLibrary _eventLibrary;
		private readonly GuiLuaLibrary _guiLibrary;
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

			// Register lua libraries
			
			_lua.RegisterFunction("print", this, GetType().GetMethod("Print"));

			// TODO: Search the assemblies for objects that inherit LuaBaseLibrary, and instantiate and register them and put them into an array,
			// rather than call them all by name here

			_formsLibrary = new FormsLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput);
			_formsLibrary.LuaRegister(Docs);

			_eventLibrary = new EventLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput);
			_eventLibrary.LuaRegister(Docs);

			_guiLibrary = new GuiLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput);
			_guiLibrary.LuaRegister(Docs);

			new BitLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput).LuaRegister(Docs);
			new EmuHawkLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput).LuaRegister(Docs);
			new ConsoleLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput).LuaRegister(Docs);

			var emuLib = new EmulatorLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput)
			{
				FrameAdvanceCallback = Frameadvance,
				YieldCallback = EmuYield
			};

			emuLib.LuaRegister(Docs);

			new InputLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput).LuaRegister(Docs);
			new JoypadLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput).LuaRegister(Docs);
			new MemoryLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput).LuaRegister(Docs);
			new MainMemoryLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput).LuaRegister(Docs);

			new MovieLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput).LuaRegister(Docs);
			new NesLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput).LuaRegister(Docs);
			new SavestateLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput).LuaRegister(Docs);
			new SnesLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput).LuaRegister(Docs);
			new StringLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput).LuaRegister(Docs);
			new GameInfoLuaLibrary(_lua, ConsoleLuaLibrary.LogOutput).LuaRegister(Docs);

			Docs.Sort();
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
