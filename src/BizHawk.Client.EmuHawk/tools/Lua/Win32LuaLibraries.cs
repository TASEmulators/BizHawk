using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;

using NLua;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class Win32LuaLibraries : LuaLibraries
	{
		public Win32LuaLibraries()
		{
			if (true /*NLua.Lua.WhichLua == "NLua"*/) _lua["keepalives"] = _lua.NewTable();
		}

		public Win32LuaLibraries(
			IEmulatorServiceProvider serviceProvider,
			MainForm mainForm,
			DisplayManager displayManager,
			InputManager inputManager,
			Config config,
			IEmulator emulator,
			IGameInfo game
		) : this()
		{
			void EnumerateLuaFunctions(string name, Type type, LuaLibraryBase instance)
			{
				instance?.Lua?.NewTable(name);
				foreach (var method in type.GetMethods())
				{
					var foundAttrs = method.GetCustomAttributes(typeof(LuaMethodAttribute), false);
					if (foundAttrs.Length == 0) continue;
					instance?.Lua?.RegisterFunction($"{name}.{((LuaMethodAttribute) foundAttrs[0]).Name}", instance, method);
					Docs.Add(new LibraryFunction(
						name,
						type.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>()
							.Select(descAttr => descAttr.Description).FirstOrDefault() ?? string.Empty,
						method
					));
				}
			}

			_mainForm = mainForm;
			LuaWait = new AutoResetEvent(false);
			Docs.Clear();
			var apiContainer = ApiManager.RestartLua(serviceProvider, LogToLuaConsole, _mainForm, displayManager, inputManager, _mainForm.MovieSession, _mainForm.Tools, config, emulator, game);

			// Register lua libraries
			foreach (var lib in Client.Common.ReflectionCache.Types.Concat(EmuHawk.ReflectionCache.Types)
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
					var instance = (LuaLibraryBase) Activator.CreateInstance(lib, this, apiContainer, _lua, (Action<string>) LogToLuaConsole);
					ServiceInjector.UpdateServices(serviceProvider, instance);

					// TODO: make EmuHawk libraries have a base class with common properties such as this
					// and inject them here
					if (instance is ClientLuaLibrary clientLib)
					{
						clientLib.MainForm = _mainForm;
					}
					else if (instance is ConsoleLuaLibrary consoleLib)
					{
						consoleLib.Tools = _mainForm.Tools;
						_logToLuaConsoleCallback = consoleLib.Log;
					}
					else if (instance is GuiLuaLibrary guiLib)
					{
						guiLib.CreateLuaCanvasCallback = (width, height, x, y) =>
						{
							var canvas = new LuaCanvas(width, height, x, y, LogToLuaConsole);
							canvas.Show();
							return _lua.TableFromObject(canvas);
						};
					}
					else if (instance is TAStudioLuaLibrary tastudioLib)
					{
						tastudioLib.Tools = _mainForm.Tools;
					}

					EnumerateLuaFunctions(instance.Name, lib, instance);
					Libraries.Add(lib, instance);
				}
			}

			_lua.RegisterFunction("print", this, GetType().GetMethod("Print"));

			EmulationLuaLibrary.FrameAdvanceCallback = Frameadvance;
			EmulationLuaLibrary.YieldCallback = EmuYield;

			EnumerateLuaFunctions(nameof(LuaCanvas), typeof(LuaCanvas), null); // add LuaCanvas to Lua function reference table
		}

		private readonly MainForm _mainForm;

		private Lua _lua = new Lua();
		private Lua _currThread;

		private static Action<object[]> _logToLuaConsoleCallback = a => Console.WriteLine("a Lua lib is logging during init and the console lib hasn't been initialised yet");

		private FormsLuaLibrary FormsLibrary => (FormsLuaLibrary)Libraries[typeof(FormsLuaLibrary)];

		private EventsLuaLibrary EventsLibrary => (EventsLuaLibrary)Libraries[typeof(EventsLuaLibrary)];

		private EmulationLuaLibrary EmulationLuaLibrary => (EmulationLuaLibrary)Libraries[typeof(EmulationLuaLibrary)];

		public override GuiLuaLibrary GuiLibrary => (GuiLuaLibrary) Libraries[typeof(GuiLuaLibrary)];

		private static void LogToLuaConsole(object outputs) => _logToLuaConsoleCallback(new[] { outputs });

		public override void Restart(IEmulatorServiceProvider newServiceProvider)
		{
			foreach (var lib in Libraries)
			{
				ServiceInjector.UpdateServices(newServiceProvider, lib.Value);
			}
		}

		public override void StartLuaDrawing()
		{
			if (ScriptList.Count != 0 && GuiLibrary.SurfaceIsNull && !IsUpdateSupressed)
			{
				GuiLibrary.DrawNew("emu");
			}
		}

		public override void EndLuaDrawing()
		{
			if (ScriptList.Count != 0 && !IsUpdateSupressed)
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
			if (!IsUpdateSupressed)
			{
				EventsLibrary.CallFrameBeforeEvent();
			}
		}

		public override void CallFrameAfterEvent()
		{
			if (!IsUpdateSupressed)
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
			RegisteredFunctions.Clear(_mainForm.Emulator);
			ScriptList.Clear();
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
			if (true /*NLua.Lua.WhichLua == "NLua"*/)
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
			if (true /*NLua.Lua.WhichLua == "NLua"*/) _lua.Pop();
		}

		public override void RunScheduledDisposes() => _lua.RunScheduledDisposes();

		public override (bool WaitForFrame, bool Terminated) ResumeScript(LuaFile lf)
		{
			_currThread = lf.Thread;

			try
			{
				LuaLibraryBase.SetCurrentThread(lf);

				var execResult = _currThread.Resume(0);

				_lua.RunScheduledDisposes(); // TODO: I don't think this is needed anymore, we run this regularly anyway

				// not sure how this is going to work out, so do this too
				_currThread.RunScheduledDisposes();

				_currThread = null;
				var result = execResult == 0
					? (WaitForFrame: false, Terminated: true) // terminated
					: (WaitForFrame: FrameAdvanceRequested, Terminated: false); // yielded

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
			_logToLuaConsoleCallback(outputs);
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
	}
}
