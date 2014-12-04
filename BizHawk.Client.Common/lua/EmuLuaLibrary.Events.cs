using System;
using System.Linq;
using System.ComponentModel;

using LuaInterface;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	[Description("A library for registering lua functions to emulator events.\n All events support multiple registered methods.\nAll registered event methods can be named and return a Guid when registered")]
	public sealed class EventLuaLibrary : LuaLibraryBase
	{
		private readonly LuaFunctionList _luaFunctions = new LuaFunctionList();

		public EventLuaLibrary(Lua lua)
			: base(lua) { }

		public EventLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name { get { return "event"; } }

		public Lua CurrentThread { get; set; }

		#region Events Library Helpers

		public void CallExitEvent(Lua thread)
		{
			var exitCallbacks = _luaFunctions.Where(x => x.Lua == thread && x.Event == "OnExit");
			foreach (var exitCallback in exitCallbacks)
			{
				exitCallback.Call();
			}
		}

		public LuaFunctionList RegisteredFunctions { get { return _luaFunctions; } }

		public void CallSaveStateEvent(string name)
		{
			var lfs = _luaFunctions.Where(x => x.Event == "OnSavestateSave").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (var lf in lfs)
					{
						lf.Call(name);
					}
				}
				catch (SystemException e)
				{
					Log(
						"error running function attached by lua function event.onsavestate" +
						"\nError message: " +
						e.Message);
				}
			}
		}

		public void CallLoadStateEvent(string name)
		{
			var lfs = _luaFunctions.Where(x => x.Event == "OnSavestateLoad").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (var lf in lfs)
					{
						lf.Call(name);
					}
				}
				catch (SystemException e)
				{
					Log(
						"error running function attached by lua function event.onloadstate" +
						"\nError message: " +
						e.Message);
				}
			}
		}

		public void CallFrameBeforeEvent()
		{
			var lfs = _luaFunctions.Where(x => x.Event == "OnFrameStart").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (var lf in lfs)
					{
						lf.Call();
					}
				}
				catch (SystemException e)
				{
					Log(
						"error running function attached by lua function event.onframestart" +
						"\nError message: " +
						e.Message);
				}
			}
		}

		public void CallFrameAfterEvent()
		{
			var lfs = _luaFunctions.Where(x => x.Event == "OnFrameEnd").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (var lf in lfs)
					{
						lf.Call();
					}
				}
				catch (SystemException e)
				{
					Log(
						"error running function attached by lua function event.onframeend" +
						"\nError message: " +
						e.Message);
				}
			}
		}

		#endregion

		[LuaMethodAttributes(
			"onframeend",
			"Calls the given lua function at the end of each frame, after all emulation and drawing has completed. Note: this is the default behavior of lua scripts"
		)]
		public string OnFrameEnd(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnFrameEnd", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"onframestart",
			"Calls the given lua function at the beginning of each frame before any emulation and drawing occurs"
		)]
		public string OnFrameStart(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnFrameStart", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"oninputpoll",
			"Calls the given lua function after each time the emulator core polls for input"
		)]
		public void OnInputPoll(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnInputPoll", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);

			if (Global.Emulator.CanPollInput())
			{
				try
				{
					(Global.Emulator as IInputPollable).InputCallbacks.Add(nlf.Callback);
				}
				catch (NotImplementedException)
				{
					LogNotImplemented();
				}
			}
			else
			{
				LogNotImplemented();
			}
		}

		private void LogNotImplemented()
		{
			Log(string.Format("Error: {0} does not yet implement input polling callbacks"));
		}

		[LuaMethodAttributes(
			"onloadstate",
			"Fires after a state is loaded. Receives a lua function name, and registers it to the event immediately following a successful savestate event"
		)]
		public string OnLoadState(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnSavestateLoad", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"onmemoryexecute",
			"Fires after the given address is executed by the core"
		)]
		public string OnMemoryExecute(LuaFunction luaf, uint address, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnMemoryExecute", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			Global.CoreComm.MemoryCallbackSystem.AddExecute(nlf.Callback, address);
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"onmemoryread",
			"Fires after the given address is read by the core. If no address is given, it will attach to every memory read"
		)]
		public string OnMemoryRead(LuaFunction luaf, uint? address = null, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnMemoryRead", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			Global.CoreComm.MemoryCallbackSystem.AddRead(nlf.Callback, address);
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"onmemorywrite",
			"Fires after the given address is written by the core. If no address is given, it will attach to every memory write"
		)]
		public string OnMemoryWrite(LuaFunction luaf, uint? address = null, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnMemoryWrite", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			Global.CoreComm.MemoryCallbackSystem.AddWrite(nlf.Callback, address);
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"onsavestate",
			"Fires after a state is saved"
		)]
		public string OnSaveState(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnSavestateSave", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"onexit",
			"Fires after the calling script has stopped"
		)]
		public string OnExit(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnExit", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"unregisterbyid",
			"Removes the registered function that matches the guid. If a function is found and remove the function will return true. If unable to find a match, the function will return false."
		)]
		public bool UnregisterById(string guid)
		{
			foreach (var nlf in _luaFunctions.Where(nlf => nlf.Guid.ToString() == guid.ToString()))
			{
				_luaFunctions.Remove(nlf);
				return true;
			}

			return false;
		}

		[LuaMethodAttributes(
			"unregisterbyname",
			"Removes the first registered function that matches Name. If a function is found and remove the function will return true. If unable to find a match, the function will return false."
		)]
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
