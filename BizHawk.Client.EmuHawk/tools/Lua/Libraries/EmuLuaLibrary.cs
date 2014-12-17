using System;
using System.Linq;
using System.Threading;

using BizHawk.Client.Common;
using LuaInterface;
using System.Reflection;
using System.Collections.Generic;

namespace BizHawk.Client.EmuHawk
{
	public class EmuLuaLibrary
	{
		private readonly Dictionary<Type, LuaLibraryBase> Libraries = new Dictionary<Type, LuaLibraryBase>();
		private readonly LuaConsole _caller;

		private Lua _lua = new Lua();
		private Lua _currThread;

		public EmuLuaLibrary()
		{
			Docs = new LuaDocumentation();
		}

		private FormsLuaLibrary FormsLibrary
		{
			get { return (FormsLuaLibrary)Libraries[typeof(FormsLuaLibrary)]; }
		}

		private EventLuaLibrary EventsLibrary
		{
			get { return (EventLuaLibrary)Libraries[typeof(EventLuaLibrary)]; }
		}

		private EmulatorLuaLibrary EmulatorLuaLibrary
		{
			get { return (EmulatorLuaLibrary)Libraries[typeof(EmulatorLuaLibrary)]; }
		}

		public GuiLuaLibrary GuiLibrary
		{
			get { return (GuiLuaLibrary)Libraries[typeof(GuiLuaLibrary)]; }
		}

		public EmuLuaLibrary(LuaConsole passed)
			: this()
		{
			LuaWait = new AutoResetEvent(false);
			Docs.Clear();
			_caller = passed.Get();

			// what was this?
			/*
			var tt = typeof(TastudioLuaLibrary);
			var mm = typeof(MainMemoryLuaLibrary);

			var tatt = tt.GetCustomAttributes(typeof(LuaLibraryAttributes), false);
			var matt = mm.GetCustomAttributes(typeof(LuaLibraryAttributes), false);
			*/

			// Register lua libraries
			var libs = Assembly
				.Load("BizHawk.Client.Common")
				.GetTypes()
				.Where(t => typeof(LuaLibraryBase).IsAssignableFrom(t))
				.Where(t => t.IsSealed)
				.ToList();

			libs.AddRange(
				Assembly
				.GetAssembly(typeof(EmuLuaLibrary))
				.GetTypes()
				.Where(t => typeof(LuaLibraryBase).IsAssignableFrom(t))
				.Where(t => t.IsSealed)
			);

			foreach (var lib in libs)
			{
				bool addLibrary = true;
				var attributes = lib.GetCustomAttributes(typeof(LuaLibraryAttributes), false);
				if (attributes.Any())
				{
					addLibrary = VersionInfo.DeveloperBuild || (attributes.First() as LuaLibraryAttributes).Released;
				}

				if (addLibrary)
				{
					var instance = (LuaLibraryBase)Activator.CreateInstance(lib, _lua);
					instance.LuaRegister(lib, Docs);
					instance.LogOutputCallback = ConsoleLuaLibrary.LogOutput;
					Libraries.Add(lib, instance);
				}
			}

			_lua.RegisterFunction("print", this, GetType().GetMethod("Print"));

			EmulatorLuaLibrary.FrameAdvanceCallback = Frameadvance;
			EmulatorLuaLibrary.YieldCallback = EmuYield;
		}

		public LuaDocumentation Docs { get; private set; }
		public bool IsRunning { get; set; }
		public EventWaitHandle LuaWait { get; private set; }
		public bool FrameAdvanceRequested { get; private set; }

		public LuaFunctionList RegisteredFunctions
		{
			get { return EventsLibrary.RegisteredFunctions; }
		}

		public void WindowClosed(IntPtr handle)
		{
			FormsLibrary.WindowClosed(handle);
		}

		public void CallSaveStateEvent(string name)
		{
			EventsLibrary.CallSaveStateEvent(name);
		}

		public void CallLoadStateEvent(string name)
		{
			EventsLibrary.CallLoadStateEvent(name);
		}

		public void CallFrameBeforeEvent()
		{
			EventsLibrary.CallFrameBeforeEvent();
		}

		public void CallFrameAfterEvent()
		{
			EventsLibrary.CallFrameAfterEvent();
		}

		public void CallExitEvent(Lua thread)
		{
			EventsLibrary.CallExitEvent(thread);
		}

		public void Close()
		{
			_lua = new Lua();
			GuiLibrary.Dispose();
		}

		public Lua SpawnCoroutine(string file)
		{
			var lua = _lua.NewThread();
			var main = lua.LoadFile(PathManager.MakeAbsolutePath(file,null));
			lua.Push(main); // push main function on to stack for subsequent resuming
			return lua;
		}

		public ResumeResult ResumeScript(Lua script)
		{
			EventsLibrary.CurrentThread = script;
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
