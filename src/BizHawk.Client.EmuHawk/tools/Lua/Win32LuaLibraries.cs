using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using NLua;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class Win32LuaLibraries : LuaLibraries
	{
		private readonly MainForm _mainForm;
		public Win32LuaLibraries()
		{
//			if (NLua.Lua.WhichLua == "NLua")
				_lua["keepalives"] = _lua.NewTable();
		}

		public Win32LuaLibraries(IEmulatorServiceProvider serviceProvider, MainForm mainForm)
			: this()
		{
			_mainForm = mainForm;

			LuaWait = new AutoResetEvent(false);
			Docs.Clear();

			// Register lua libraries
			foreach (var lib in Assembly.Load("BizHawk.Client.Common").GetTypes()
				.Concat(Assembly.GetAssembly(typeof(Win32LuaLibraries)).GetTypes())
				.Where(t => typeof(LuaLibraryBase).IsAssignableFrom(t) && t.IsSealed && ServiceInjector.IsAvailable(serviceProvider, t)))
			{
				bool addLibrary = true;
				var attributes = lib.GetCustomAttributes(typeof(LuaLibraryAttribute), false);
				if (attributes.Any())
				{
					addLibrary = VersionInfo.DeveloperBuild || ((LuaLibraryAttribute)attributes.First()).Released;
				}

				if (addLibrary)
				{
					var instance = (LuaLibraryBase)Activator.CreateInstance(lib, _lua);
					instance.LuaRegister(lib, Docs);
					instance.LogOutputCallback = ConsoleLuaLibrary.LogOutput;
					ServiceInjector.UpdateServices(serviceProvider, instance);

					// TODO: make EmuHawk libraries have a base class with common properties such as this
					// and inject them here
					if (instance is ClientLuaLibrary clientLib)
					{
						clientLib.MainForm = _mainForm;
					}

					ApiContainerInstance = ApiManager.RestartLua(serviceProvider, ConsoleLuaLibrary.LogOutput);
					if (instance is DelegatingLuaLibraryEmu dlgInstanceEmu) dlgInstanceEmu.APIs = ApiContainerInstance; // this is necessary as the property has the `new` modifier
					else if (instance is DelegatingLuaLibrary dlgInstance) dlgInstance.APIs = ApiContainerInstance;

					Libraries.Add(lib, instance);
				}
			}

			_lua.RegisterFunction("print", this, GetType().GetMethod("Print"));

			EmulationLuaLibrary.FrameAdvanceCallback = Frameadvance;
			EmulationLuaLibrary.YieldCallback = EmuYield;

			// Add LuaCanvas to Docs
			Type luaCanvas = typeof(LuaCanvas);

			foreach (var method in luaCanvas.GetMethods())
			{
				if (method.GetCustomAttributes(typeof(LuaMethodAttribute), false).Length != 0)
				{
					Docs.Add(new LibraryFunction(nameof(LuaCanvas), luaCanvas.Description(), method));
				}
			}
		}

		/// <remarks>lazily instantiated</remarks>
		private static ApiContainer ApiContainerInstance;

		private Lua _lua = new Lua();
		private Lua _currThread;

		private FormsLuaLibrary FormsLibrary => (FormsLuaLibrary)Libraries[typeof(FormsLuaLibrary)];

		private EventsLuaLibrary EventsLibrary => (EventsLuaLibrary)Libraries[typeof(EventsLuaLibrary)];

		private EmulationLuaLibrary EmulationLuaLibrary => (EmulationLuaLibrary)Libraries[typeof(EmulationLuaLibrary)];

		public override GuiLuaLibrary GuiLibrary => (GuiLuaLibrary) Libraries[typeof(GuiLuaLibrary)];

		public override void Restart(IEmulatorServiceProvider newServiceProvider)
		{
			foreach (var lib in Libraries)
			{
				ServiceInjector.UpdateServices(newServiceProvider, lib.Value);
			}
		}

		public override void StartLuaDrawing()
		{
			if (ScriptList.Count != 0 && GuiLibrary.SurfaceIsNull && !SuppressLua)
			{
				GuiLibrary.DrawNew("emu");
			}
		}

		public override void EndLuaDrawing()
		{
			if (ScriptList.Count != 0 && !SuppressLua)
			{
				GuiLibrary.DrawFinish();
			}
		}

		public bool FrameAdvanceRequested { get; private set; }

		public override LuaFunctionList RegisteredFunctions => EventsLibrary.RegisteredFunctions;

		public override void WindowClosed(IntPtr handle)
		{
			FormsLibrary.WindowClosed(handle);
		}

		public override void CallSaveStateEvent(string name)
		{
			EventsLibrary.CallSaveStateEvent(name);
		}

		public override void CallLoadStateEvent(string name)
		{
			EventsLibrary.CallLoadStateEvent(name);
		}

		public override void CallFrameBeforeEvent()
		{
			if (!SuppressLua)
			{
				EventsLibrary.CallFrameBeforeEvent();
			}
		}

		public override void CallFrameAfterEvent()
		{
			if (!SuppressLua)
			{
				EventsLibrary.CallFrameAfterEvent();
			}
		}

		public override void CallExitEvent(LuaFile lf)
		{
			EventsLibrary.CallExitEvent(lf);
		}

		public override void Close()
		{
			RegisteredFunctions.Clear(GlobalWin.Emulator); // TODO: don't use globals
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
			//if (NLua.Lua.WhichLua == "NLua")
			{
				_lua.GetTable("keepalives")[lua] = 1;
				//this not being run is the origin of a memory leak if you restart scripts too many times
				_lua.Pop();
			}
			return lua;
		}

		public override void SpawnAndSetFileThread(string pathToLoad, LuaFile lf)
		{
			lf.Thread = SpawnCoroutine(pathToLoad);
		}

		public override void ExecuteString(string command)
		{
			_currThread = _lua.NewThread();
			_currThread.DoString(command);
			//if (NLua.Lua.WhichLua == "NLua")
				_lua.Pop();
		}

		public override ResumeResult ResumeScript(LuaFile lf)
		{
			_currThread = lf.Thread;

			try
			{
				LuaLibraryBase.SetCurrentThread(lf);

				var execResult = _currThread.Resume(0);

				_lua.RunScheduledDisposes();

				// not sure how this is going to work out, so do this too
				_currThread.RunScheduledDisposes();

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
