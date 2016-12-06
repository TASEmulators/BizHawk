using System;
using System.Linq;
using System.Threading;

using BizHawk.Client.Common;
using LuaInterface;
using System.Reflection;
using System.Collections.Generic;
using BizHawk.Emulation.Common;
using System.IO;

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

		public EmuLuaLibrary(LuaConsole passed, IEmulatorServiceProvider serviceProvider)
			: this()
		{
			LuaWait = new AutoResetEvent(false);
			Docs.Clear();
			_caller = passed.Get();

			// Register lua libraries
			var libs = Assembly
				.Load("BizHawk.Client.Common")
				.GetTypes()
				.Where(t => typeof(LuaLibraryBase).IsAssignableFrom(t))
				.Where(t => t.IsSealed)
				.Where(t => ServiceInjector.IsAvailable(serviceProvider, t))
				.ToList();

			libs.AddRange(
				Assembly
				.GetAssembly(typeof(EmuLuaLibrary))
				.GetTypes()
				.Where(t => typeof(LuaLibraryBase).IsAssignableFrom(t))
				.Where(t => t.IsSealed)
				.Where(t => ServiceInjector.IsAvailable(serviceProvider, t))
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
					ServiceInjector.UpdateServices(serviceProvider, instance);
					Libraries.Add(lib, instance);
				}
			}

			_lua.RegisterFunction("print", this, GetType().GetMethod("Print"));

			EmulatorLuaLibrary.FrameAdvanceCallback = Frameadvance;
			EmulatorLuaLibrary.YieldCallback = EmuYield;
		}

		public void Restart(IEmulatorServiceProvider newServiceProvider)
		{
			foreach (var lib in Libraries)
			{
				ServiceInjector.UpdateServices(newServiceProvider, lib.Value);
			}
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
			FormsLibrary.DestroyAll();
			_lua.Close();
			_lua = new Lua();
			GuiLibrary.Dispose();
		}

		public Lua SpawnCoroutine(string file)
		{
			var lua = _lua.NewThread();
			var content = File.ReadAllText(file);
			var main = lua.LoadString(content, "main");
			lua.Push(main); // push main function on to stack for subsequent resuming
			return lua;
		}

		public void ExecuteString(string command)
		{
			_currThread = _lua.NewThread();

			_currThread.DoString(command);
		}

		public ResumeResult ResumeScript(Lua script)
		{
			_currThread = script;

			try
			{
				LuaLibraryBase.SetCurrentThread(_currThread);

				var execResult = script.Resume(0);

				_lua.RunScheduledDisposes();
				//not sure how this is going to work out, so do this too
				script.RunScheduledDisposes();

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
			finally
			{
				LuaLibraryBase.ClearCurrentThread();
			}
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
			_currThread.Yield(0);
		}

		public class ResumeResult
		{
			public bool WaitForFrame { get; set; }
			public bool Terminated { get; set; }
		}
	}
}
