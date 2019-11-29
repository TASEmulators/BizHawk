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
	public class EmuLuaLibrary : PlatformEmuLuaLibrary
	{
		public EmuLuaLibrary()
		{
//			if (NLua.Lua.WhichLua == "NLua")
				_lua["keepalives"] = _lua.NewTable();
		}

		public EmuLuaLibrary(IEmulatorServiceProvider serviceProvider)
			: this()
		{
			static APISubsetContainer InitApiHawkContainerInstance(IEmulatorServiceProvider sp, Action<string> logCallback)
			{
				var ctorParamTypes = new[] { typeof(Action<string>) };
				var ctorParams = new object[] { logCallback };
				var libDict = new Dictionary<Type, IExternalApi>();
				foreach (var api in Assembly.Load("BizHawk.Client.ApiHawk").GetTypes()
					.Concat(Assembly.GetAssembly(typeof(APISubsetContainer)).GetTypes())
					.Where(t => t.IsSealed && typeof(IExternalApi).IsAssignableFrom(t) && ServiceInjector.IsAvailable(sp, t)))
				{
					var ctorWithParams = api.GetConstructor(ctorParamTypes);
					var instance = (IExternalApi) (ctorWithParams == null ? Activator.CreateInstance(api) : ctorWithParams.Invoke(ctorParams));
					ServiceInjector.UpdateServices(sp, instance);
					libDict.Add(api, instance);
				}
				return ApiHawkContainerInstance = new APISubsetContainer(libDict);
			}

			LuaWait = new AutoResetEvent(false);
			Docs.Clear();

			// Register lua libraries
			var libs = Assembly
				.Load("BizHawk.Client.Common")
				.GetTypes()
				.Where(t => typeof(LuaLibraryBase).IsAssignableFrom(t))
				.Where(t => t.IsSealed)
				.Where(t => ServiceInjector.IsAvailable(serviceProvider, t))
				.Concat(Assembly
					.GetAssembly(typeof(EmuLuaLibrary))
					.GetTypes()
					.Where(t => typeof(LuaLibraryBase).IsAssignableFrom(t))
					.Where(t => t.IsSealed)
					.Where(t => ServiceInjector.IsAvailable(serviceProvider, t)));

			foreach (var lib in libs)
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
					if (instance is DelegatingLuaLibrary dlgInstance)
						dlgInstance.APIs = ApiHawkContainerInstance ?? InitApiHawkContainerInstance(serviceProvider, ConsoleLuaLibrary.LogOutput);
					Libraries.Add(lib, instance);
				}
			}

			_lua.RegisterFunction("print", this, GetType().GetMethod("Print"));

			EmulatorLuaLibrary.FrameAdvanceCallback = Frameadvance;
			EmulatorLuaLibrary.YieldCallback = EmuYield;

			// Add LuaCanvas to Docs
			Type luaCanvas = typeof(LuaCanvas);

			var methods = luaCanvas
				.GetMethods()
				.Where(m => m.GetCustomAttributes(typeof(LuaMethodAttribute), false).Any());

			foreach (var method in methods)
			{
				Docs.Add(new LibraryFunction(nameof(LuaCanvas), luaCanvas.Description(), method));
			}
		}

		/// <remarks>lazily instantiated</remarks>
		private static APISubsetContainer ApiHawkContainerInstance;

		private Lua _lua = new Lua();
		private Lua _currThread;

		private FormsLuaLibrary FormsLibrary => (FormsLuaLibrary)Libraries[typeof(FormsLuaLibrary)];

		private EventLuaLibrary EventsLibrary => (EventLuaLibrary)Libraries[typeof(EventLuaLibrary)];

		private EmulatorLuaLibrary EmulatorLuaLibrary => (EmulatorLuaLibrary)Libraries[typeof(EmulatorLuaLibrary)];

		public override void Restart(IEmulatorServiceProvider newServiceProvider)
		{
			foreach (var lib in Libraries)
			{
				ServiceInjector.UpdateServices(newServiceProvider, lib.Value);
			}
		}

		public override void StartLuaDrawing()
		{
			if (ScriptList.Any() && GuiLibrary.SurfaceIsNull)
			{
				GuiLibrary.DrawNew("emu");
			}
		}

		public override void EndLuaDrawing()
		{
			if (ScriptList.Any())
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
			EventsLibrary.CallFrameBeforeEvent();
		}

		public override void CallFrameAfterEvent()
		{
			EventsLibrary.CallFrameAfterEvent();
		}

		public void CallExitEvent(Lua thread)
		{
			EventsLibrary.CallExitEvent(thread);
		}

		public override void CallExitEvent(LuaFile lf)
		{
			CallExitEvent(lf.Thread);
		}

		public override void Close()
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
				LuaLibraryBase.SetCurrentThread(_currThread);

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
