using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using NLua;
using NLua.Native;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public class LuaLibraries : ILuaLibraries
	{
		public static readonly bool IsAvailable = LuaNativeMethodLoader.EnsureNativeMethodsLoaded();

		public LuaLibraries(
			LuaFileList scriptList,
			LuaFunctionList registeredFuncList,
			IEmulatorServiceProvider serviceProvider,
			MainForm mainForm,
			DisplayManagerBase displayManager,
			InputManager inputManager,
			Config config,
			IEmulator emulator,
			IGameInfo game)
		{
			if (!IsAvailable)
			{
				throw new InvalidOperationException("The Lua dynamic library was not able to be loaded");
			}

			void EnumerateLuaFunctions(string name, Type type, LuaLibraryBase instance)
			{
				if (instance != null) _lua.NewTable(name);
				foreach (var method in type.GetMethods())
				{
					var foundAttrs = method.GetCustomAttributes(typeof(LuaMethodAttribute), false);
					if (foundAttrs.Length == 0) continue;
					if (instance != null) _lua.RegisterFunction($"{name}.{((LuaMethodAttribute)foundAttrs[0]).Name}", instance, method);
					LibraryFunction libFunc = new(
						name,
						type.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>()
							.Select(descAttr => descAttr.Description).FirstOrDefault() ?? string.Empty,
						method
					);
					Docs.Add(libFunc);
				}
			}

			_th = new NLuaTableHelper(_lua, LogToLuaConsole);
			_displayManager = displayManager;
			_inputManager = inputManager;
			_mainForm = mainForm;
			LuaWait = new AutoResetEvent(false);
			PathEntries = config.PathEntries;
			RegisteredFunctions = registeredFuncList;
			ScriptList = scriptList;
			Docs.Clear();
			_apiContainer = ApiManager.RestartLua(serviceProvider, LogToLuaConsole, _mainForm, _displayManager, _inputManager, _mainForm.MovieSession, _mainForm.Tools, config, emulator, game);

			// Register lua libraries
			foreach (var lib in Client.Common.ReflectionCache.Types.Concat(EmuHawk.ReflectionCache.Types)
				.Where(t => typeof(LuaLibraryBase).IsAssignableFrom(t) && t.IsSealed && ServiceInjector.IsAvailable(serviceProvider, t)))
			{
				if (VersionInfo.DeveloperBuild
					|| lib.GetCustomAttribute<LuaLibraryAttribute>(inherit: false)?.Released is not false)
				{
					var instance = (LuaLibraryBase)Activator.CreateInstance(lib, this, _apiContainer, (Action<string>)LogToLuaConsole);
					if (!ServiceInjector.UpdateServices(serviceProvider, instance, mayCache: true)) throw new Exception("Lua lib has required service(s) that can't be fulfilled");

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
					else if (instance is FormsLuaLibrary formsLib)
					{
						formsLib.MainForm = _mainForm;
					}
					else if (instance is GuiLuaLibrary guiLib)
					{
						// emu lib may be null now, depending on order of ReflectionCache.Types, but definitely won't be null when this is called
						guiLib.CreateLuaCanvasCallback = (width, height, x, y) =>
						{
							var canvas = new LuaCanvas(EmulationLuaLibrary, width, height, x, y, _th, LogToLuaConsole);
							canvas.Show();
							return _th.ObjectToTable(canvas);
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

			_lua.RegisterFunction("print", this, typeof(LuaLibraries).GetMethod(nameof(Print)));

			var packageTable = (LuaTable) _lua["package"];
			var luaPath = PathEntries.LuaAbsolutePath();
			if (OSTailoredCode.IsUnixHost)
			{
				// add %exe%/Lua to library resolution pathset (LUA_PATH)
				// this is done already on windows, but not on linux it seems?
				packageTable["path"] = $"{luaPath}/?.lua;{luaPath}?/init.lua;{packageTable["path"]}";
				// we need to modifiy the cpath so it looks at our lua dir too, and remove the relative pathing
				// we do this on Windows too, but keep in mind Linux uses .so and Windows use .dll
				// TODO: Does the relative pathing issue Windows has also affect Linux? I'd assume so...
				packageTable["cpath"] = $"{luaPath}/?.so;{luaPath}/loadall.so;{packageTable["cpath"]}";
				packageTable["cpath"] = ((string)packageTable["cpath"]).Replace(";./?.so", "");
			}
			else
			{
				packageTable["cpath"] = $"{luaPath}\\?.dll;{luaPath}\\loadall.dll;{packageTable["cpath"]}";
				packageTable["cpath"] = ((string)packageTable["cpath"]).Replace(";.\\?.dll", "");
			}

			EmulationLuaLibrary.FrameAdvanceCallback = FrameAdvance;
			EmulationLuaLibrary.YieldCallback = EmuYield;

			EnumerateLuaFunctions(nameof(LuaCanvas), typeof(LuaCanvas), null); // add LuaCanvas to Lua function reference table
		}

		private ApiContainer _apiContainer;

		private readonly DisplayManagerBase _displayManager;

		private GuiApi GuiAPI => (GuiApi)_apiContainer.Gui;

		private readonly InputManager _inputManager;

		private readonly MainForm _mainForm;

		private Lua _lua = new();
		private LuaThread _currThread;

		private readonly NLuaTableHelper _th;

		private static Action<object[]> _logToLuaConsoleCallback = a => Console.WriteLine("a Lua lib is logging during init and the console lib hasn't been initialised yet");

		private FormsLuaLibrary FormsLibrary => (FormsLuaLibrary)Libraries[typeof(FormsLuaLibrary)];

		public LuaDocumentation Docs { get; } = new LuaDocumentation();

		private EmulationLuaLibrary EmulationLuaLibrary => (EmulationLuaLibrary)Libraries[typeof(EmulationLuaLibrary)];

		public string EngineName => "NLua+Lua";

		public bool IsRebootingCore { get; set; }

		public bool IsUpdateSupressed { get; set; }

		public bool IsInInputOrMemoryCallback { get; set; }

		private readonly IDictionary<Type, LuaLibraryBase> Libraries = new Dictionary<Type, LuaLibraryBase>();

		private EventWaitHandle LuaWait;

		public PathEntryCollection PathEntries { get; private set; }

		public LuaFileList ScriptList { get; }

		private static void LogToLuaConsole(object outputs) => _logToLuaConsoleCallback(new[] { outputs });

		public NLuaTableHelper GetTableHelper() => _th;

		public void Restart(
			IEmulatorServiceProvider newServiceProvider,
			Config config,
			IEmulator emulator,
			IGameInfo game)
		{
			_apiContainer = ApiManager.RestartLua(newServiceProvider, LogToLuaConsole, _mainForm, _displayManager, _inputManager, _mainForm.MovieSession, _mainForm.Tools, config, emulator, game);
			PathEntries = config.PathEntries;
			foreach (var lib in Libraries.Values)
			{
				lib.APIs = _apiContainer;
				if (!ServiceInjector.UpdateServices(newServiceProvider, lib, mayCache: true))
				{
					throw new Exception("Lua lib has required service(s) that can't be fulfilled");
				}

				lib.Restarted();
			}
		}

		public bool FrameAdvanceRequested { get; private set; }

		public LuaFunctionList RegisteredFunctions { get; }

		public void CallSaveStateEvent(string name)
		{
			try
			{
				foreach (var lf in RegisteredFunctions.Where(static l => l.Event == NamedLuaFunction.EVENT_TYPE_SAVESTATE).ToList())
				{
					lf.Call(name);
				}
			}
			catch (Exception e)
			{
				LogToLuaConsole($"error running function attached by lua function event.onsavestate\nError message: {e.Message}");
			}
		}

		public void CallLoadStateEvent(string name)
		{
			try
			{
				foreach (var lf in RegisteredFunctions.Where(static l => l.Event == NamedLuaFunction.EVENT_TYPE_LOADSTATE).ToList())
				{
					lf.Call(name);
				}
			}
			catch (Exception e)
			{
				LogToLuaConsole($"error running function attached by lua function event.onloadstate\nError message: {e.Message}");
			}
		}

		public void CallFrameBeforeEvent()
		{
			if (IsUpdateSupressed) return;

			try
			{
				foreach (var lf in RegisteredFunctions.Where(static l => l.Event == NamedLuaFunction.EVENT_TYPE_PREFRAME).ToList())
				{
					lf.Call();
				}
			}
			catch (Exception e)
			{
				LogToLuaConsole($"error running function attached by lua function event.onframestart\nError message: {e.Message}");
			}
		}

		public void CallFrameAfterEvent()
		{
			if (IsUpdateSupressed) return;

			try
			{
				foreach (var lf in RegisteredFunctions.Where(static l => l.Event == NamedLuaFunction.EVENT_TYPE_POSTFRAME).ToList())
				{
					lf.Call();
				}
			}
			catch (Exception e)
			{
				LogToLuaConsole($"error running function attached by lua function event.onframeend\nError message: {e.Message}");
			}
		}

		public void CallExitEvent(LuaFile lf)
		{
			foreach (var exitCallback in RegisteredFunctions
				.Where(l => l.Event == NamedLuaFunction.EVENT_TYPE_ENGINESTOP
					&& (l.LuaFile.Path == lf.Path || ReferenceEquals(l.LuaFile.Thread, lf.Thread)))
				.ToList())
			{
				exitCallback.Call();
			}
		}

		public void Close()
		{
			foreach (var closeCallback in RegisteredFunctions
				.Where(static l => l.Event == NamedLuaFunction.EVENT_TYPE_CONSOLECLOSE)
				.ToList())
			{
				closeCallback.Call();
			}

			RegisteredFunctions.Clear(_mainForm.Emulator);
			ScriptList.Clear();
			FormsLibrary.DestroyAll();
			_lua.Dispose();
			_lua = null;
		}

		public INamedLuaFunction CreateAndRegisterNamedFunction(
			LuaFunction function,
			string theEvent,
			Action<string> logCallback,
			LuaFile luaFile,
			string name = null)
		{
			var nlf = new NamedLuaFunction(function, theEvent, logCallback, luaFile, () => _lua.NewThread(), this, name);
			RegisteredFunctions.Add(nlf);
			return nlf;
		}

		public bool RemoveNamedFunctionMatching(Func<INamedLuaFunction, bool> predicate)
		{
			var nlf = (NamedLuaFunction)RegisteredFunctions.FirstOrDefault(predicate);
			if (nlf == null) return false;
			RegisteredFunctions.Remove(nlf, _mainForm.Emulator);
			return true;
		}

		public LuaThread SpawnCoroutine(string file)
		{
			var content = File.ReadAllText(file);
			var main = _lua.LoadString(content, "main");
			return _lua.NewThread(main);
		}

		public void SpawnAndSetFileThread(string pathToLoad, LuaFile lf)
			=> lf.Thread = SpawnCoroutine(pathToLoad);

		public object[] ExecuteString(string command)
		{
			const string ChunkName = "input"; // shows up in error messages

			// Use LoadString to separate parsing and execution, to tell syntax errors and runtime errors apart
			LuaFunction func;
			try
			{
				// Adding a return is necessary to get out return values of functions and turn expressions ("1+1" etc.) into valid statements
				func = _lua.LoadString($"return {command}", ChunkName);
			}
			catch (Exception)
			{
				// command may be a valid statement without the added "return"
				// if previous attempt couldn't be parsed, run the raw command
				return _lua.DoString(command, ChunkName);
			}

			using (func)
			{
				return func.Call();
			}
		}

		public (bool WaitForFrame, bool Terminated) ResumeScript(LuaFile lf)
		{
			_currThread = lf.Thread;

			try
			{
				LuaLibraryBase.SetCurrentThread(lf);

				var execResult = _currThread.Resume();

				_currThread = null;
				var result = execResult switch
				{
					LuaStatus.OK => (WaitForFrame: false, Terminated: true),
					LuaStatus.Yield => (WaitForFrame: FrameAdvanceRequested, Terminated: false),
					_ => throw new InvalidOperationException($"{nameof(_currThread.Resume)}() returned {execResult}?")
				};

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

		private void FrameAdvance()
		{
			FrameAdvanceRequested = true;
			_currThread.Yield();
		}

		private void EmuYield()
		{
			_currThread.Yield();
		}
	}
}
