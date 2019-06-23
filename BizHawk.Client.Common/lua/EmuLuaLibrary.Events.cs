using System;
using System.Linq;
using System.ComponentModel;

using NLua;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

using BizHawk.Emulation.Cores.Nintendo.N64;

namespace BizHawk.Client.Common
{
	[Description("A library for registering lua functions to emulator events.\n All events support multiple registered methods.\nAll registered event methods can be named and return a Guid when registered")]
	public sealed class EventLuaLibrary : LuaLibraryBase
	{
		[OptionalService]
		private IInputPollable InputPollableCore { get; set; }

		[OptionalService]
		private IDebuggable DebuggableCore { get; set; }

		[RequiredService]
		private IEmulator Emulator { get; set; }

		[OptionalService]
		private IMemoryDomains Domains { get; set; }

		private readonly LuaFunctionList _luaFunctions = new LuaFunctionList();

		public EventLuaLibrary(Lua lua)
			: base(lua) { }

		public EventLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "event";

		#region Events Library Helpers

		public void CallExitEvent(Lua thread)
		{
			var exitCallbacks = _luaFunctions.Where(l => l.Lua == thread && l.Event == "OnExit");
			foreach (var exitCallback in exitCallbacks)
			{
				exitCallback.Call();
			}
		}

		public LuaFunctionList RegisteredFunctions => _luaFunctions;

		public void CallSaveStateEvent(string name)
		{
			var lfs = _luaFunctions.Where(l => l.Event == "OnSavestateSave");
			try
			{
				foreach (var lf in lfs)
				{
					lf.Call(name);
				}
			}
			catch (Exception e)
			{
				Log($"error running function attached by lua function event.onsavestate\nError message: {e.Message}");
			}
		}

		public void CallLoadStateEvent(string name)
		{
			var lfs = _luaFunctions.Where(l => l.Event == "OnSavestateLoad");
			try
			{
				foreach (var lf in lfs)
				{
					lf.Call(name);
				}
			}
			catch (Exception e)
			{
				Log($"error running function attached by lua function event.onloadstate\nError message: {e.Message}");
			}
		}

		public void CallFrameBeforeEvent()
		{
			var lfs = _luaFunctions.Where(l => l.Event == "OnFrameStart");
			try
			{
				foreach (var lf in lfs)
				{
					lf.Call();
				}
			}
			catch (Exception e)
			{
				Log($"error running function attached by lua function event.onframestart\nError message: {e.Message}");
			}
		}

		public void CallFrameAfterEvent()
		{
			var lfs = _luaFunctions.Where(l => l.Event == "OnFrameEnd");
			try
			{
				foreach (var lf in lfs)
				{
					lf.Call();
				}
			}
			catch (Exception e)
			{
				Log($"error running function attached by lua function event.onframeend\nError message: {e.Message}");
			}
		}

		private bool N64CoreTypeDynarec()
		{
			//if ((Emulator as N64)?.GetSyncSettings().Core == N64SyncSettings.CoreType.Dynarec)
			//{
			//	Log("N64 Error: Memory callbacks are not implemented for Dynamic Recompiler core type\nUse Interpreter or Pure Interpreter\n");
			//	return true;
			//}

			return false;
		}

		private void LogMemoryCallbacksNotImplemented()
		{
			Log($"{Emulator.Attributes().CoreName} does not implement memory callbacks");
		}

		private void LogMemoryExecuteCallbacksNotImplemented()
		{
			Log($"{Emulator.Attributes().CoreName} does not implement memory execute callbacks");
		}

		#endregion


		[LuaMethodExample("local steveonf = event.onframeend(\r\n\tfunction()\r\n\t\tconsole.log( \"Calls the given lua function at the end of each frame, after all emulation and drawing has completed. Note: this is the default behavior of lua scripts\" );\r\n\tend\r\n\t, \"Frame name\" );")]
		[LuaMethod("onframeend", "Calls the given lua function at the end of each frame, after all emulation and drawing has completed. Note: this is the default behavior of lua scripts")]
		public string OnFrameEnd(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnFrameEnd", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodExample("local steveonf = event.onframestart(\r\n\tfunction()\r\n\t\tconsole.log( \"Calls the given lua function at the beginning of each frame before any emulation and drawing occurs\" );\r\n\tend\r\n\t, \"Frame name\" );")]
		[LuaMethod("onframestart", "Calls the given lua function at the beginning of each frame before any emulation and drawing occurs")]
		public string OnFrameStart(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnFrameStart", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodExample("local steveoni = event.oninputpoll(\r\n\tfunction()\r\n\t\tconsole.log( \"Calls the given lua function after each time the emulator core polls for input\" );\r\n\tend\r\n\t, \"Frame name\" );")]
		[LuaMethod("oninputpoll", "Calls the given lua function after each time the emulator core polls for input")]
		public string OnInputPoll(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnInputPoll", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);

			if (InputPollableCore != null)
			{
				try
				{
					InputPollableCore.InputCallbacks.Add(nlf.Callback);
					return nlf.Guid.ToString();
				}
				catch (NotImplementedException)
				{
					LogNotImplemented();
					return Guid.Empty.ToString();
				}
			}

			LogNotImplemented();
			return Guid.Empty.ToString();
		}

		private void LogNotImplemented()
		{
			Log($"Error: {Emulator.Attributes().CoreName} does not yet implement input polling callbacks");
		}

		[LuaMethodExample("local steveonl = event.onloadstate(\r\n\tfunction()\r\n\tconsole.log( \"Fires after a state is loaded. Receives a lua function name, and registers it to the event immediately following a successful savestate event\" );\r\nend\", \"Frame name\" );")]
		[LuaMethod("onloadstate", "Fires after a state is loaded. Receives a lua function name, and registers it to the event immediately following a successful savestate event")]
		public string OnLoadState(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnSavestateLoad", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodExample("local steveonm = event.onmemoryexecute(\r\n\tfunction()\r\n\t\tconsole.log( \"Fires after the given address is executed by the core\" );\r\n\tend\r\n\t, 0x200, \"Frame name\", \"System Bus\" );")]
		[LuaMethod("onmemoryexecute", "Fires after the given address is executed by the core")]
		public string OnMemoryExecute(LuaFunction luaf, uint address, string name = null, string domain = null)
		{
			try
			{
				if (DebuggableCore != null && DebuggableCore.MemoryCallbacksAvailable() &&
					DebuggableCore.MemoryCallbacks.ExecuteCallbacksAvailable)
				{
					if (N64CoreTypeDynarec())
					{
						return Guid.Empty.ToString();
					}

					var nlf = new NamedLuaFunction(luaf, "OnMemoryExecute", LogOutputCallback, CurrentThread, name);
					_luaFunctions.Add(nlf);

					if (string.IsNullOrWhiteSpace(domain))
					{
						if (Domains != null && Domains.HasSystemBus)
						{
							domain = Domains.SystemBus.Name;
						}
					}

					DebuggableCore.MemoryCallbacks.Add(
						new MemoryCallback(domain, MemoryCallbackType.Execute, "Lua Hook", nlf.MemCallback, address, null));
					return nlf.Guid.ToString();
				}
			}
			catch (NotImplementedException)
			{
				LogMemoryExecuteCallbacksNotImplemented();
				return Guid.Empty.ToString();
			}

			LogMemoryExecuteCallbacksNotImplemented();
			return Guid.Empty.ToString();
		}

		[LuaMethodExample("local steveonm = event.onmemoryread(\r\n\tfunction()\r\n\t\tconsole.log( \"Fires after the given address is read by the core. If no address is given, it will attach to every memory read\" );\r\n\tend\r\n\t, 0x200, \"Frame name\" );")]
		[LuaMethod("onmemoryread", "Fires after the given address is read by the core. If no address is given, it will attach to every memory read")]
		public string OnMemoryRead(LuaFunction luaf, uint? address = null, string name = null, string domain = null)
		{
			try
			{
				if (DebuggableCore != null && DebuggableCore.MemoryCallbacksAvailable())
				{
					if (N64CoreTypeDynarec())
					{
						return Guid.Empty.ToString();
					}

					var nlf = new NamedLuaFunction(luaf, "OnMemoryRead", LogOutputCallback, CurrentThread, name);
					_luaFunctions.Add(nlf);

					if (string.IsNullOrWhiteSpace(domain))
					{
						if (Domains != null && Domains.HasSystemBus)
						{
							domain = Domains.SystemBus.Name;
						}
					}

					DebuggableCore.MemoryCallbacks.Add(
						new MemoryCallback(domain, MemoryCallbackType.Read, "Lua Hook", nlf.MemCallback, address, null));
					return nlf.Guid.ToString();
				}
			}
			catch (NotImplementedException)
			{
				LogMemoryCallbacksNotImplemented();
				return Guid.Empty.ToString();
			}

			LogMemoryCallbacksNotImplemented();
			return Guid.Empty.ToString();
		}

		[LuaMethodExample("local steveonm = event.onmemorywrite(\r\n\tfunction()\r\n\t\tconsole.log( \"Fires after the given address is written by the core. If no address is given, it will attach to every memory write\" );\r\n\tend\r\n\t, 0x200, \"Frame name\" );")]
		[LuaMethod("onmemorywrite", "Fires after the given address is written by the core. If no address is given, it will attach to every memory write")]
		public string OnMemoryWrite(LuaFunction luaf, uint? address = null, string name = null, string domain = null)
		{
			try
			{
				if (DebuggableCore != null && DebuggableCore.MemoryCallbacksAvailable())
				{
					if (N64CoreTypeDynarec())
					{
						return Guid.Empty.ToString();
					}

					var nlf = new NamedLuaFunction(luaf, "OnMemoryWrite", LogOutputCallback, CurrentThread, name);
					_luaFunctions.Add(nlf);

					if (string.IsNullOrWhiteSpace(domain))
					{
						if (Domains != null && Domains.HasSystemBus)
						{
							domain = Domains.SystemBus.Name;
						}
					}

					DebuggableCore.MemoryCallbacks.Add(
						new MemoryCallback(domain, MemoryCallbackType.Write, "Lua Hook", nlf.MemCallback, address, null));
					return nlf.Guid.ToString();
				}
			}
			catch (NotImplementedException)
			{
				LogMemoryCallbacksNotImplemented();
				return Guid.Empty.ToString();
			}

			LogMemoryCallbacksNotImplemented();
			return Guid.Empty.ToString();
		}

		[LuaMethodExample("local steveons = event.onsavestate(\r\n\tfunction()\r\n\t\tconsole.log( \"Fires after a state is saved\" );\r\n\tend\r\n\t, \"Frame name\" );")]
		[LuaMethod("onsavestate", "Fires after a state is saved")]
		public string OnSaveState(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnSavestateSave", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodExample("local steveone = event.onexit(\r\n\tfunction()\r\n\t\tconsole.log( \"Fires after the calling script has stopped\" );\r\n\tend\r\n\t, \"Frame name\" );")]
		[LuaMethod("onexit", "Fires after the calling script has stopped")]
		public string OnExit(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnExit", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodExample("if ( event.unregisterbyid( \"4d1810b7 - 0d28 - 4acb - 9d8b - d87721641551\" ) ) then\r\n\tconsole.log( \"Removes the registered function that matches the guid.If a function is found and remove the function will return true.If unable to find a match, the function will return false.\" );\r\nend;")]
		[LuaMethod("unregisterbyid", "Removes the registered function that matches the guid. If a function is found and remove the function will return true. If unable to find a match, the function will return false.")]
		public bool UnregisterById(string guid)
		{
			foreach (var nlf in _luaFunctions.Where(nlf => nlf.Guid.ToString() == guid.ToString()))
			{
				_luaFunctions.Remove(nlf);
				return true;
			}

			return false;
		}

		[LuaMethodExample("if ( event.unregisterbyname( \"Function name\" ) ) then\r\n\tconsole.log( \"Removes the first registered function that matches Name.If a function is found and remove the function will return true.If unable to find a match, the function will return false.\" );\r\nend;")]
		[LuaMethod("unregisterbyname", "Removes the first registered function that matches Name. If a function is found and remove the function will return true. If unable to find a match, the function will return false.")]
		public bool UnregisterByName(string name)
		{
			foreach (var nlf in _luaFunctions.Where(nlf => nlf.Name == name))
			{
				_luaFunctions.Remove(nlf);
				return true;
			}

			return false;
		}
	}
}
