using System;
using System.Linq;

using LuaInterface;

namespace BizHawk.Client.Common
{
	public class EventLuaLibrary : LuaLibraryBase
	{
		public EventLuaLibrary(Action<string> logOutputCallback)
		{
			LogOutputCallback = logOutputCallback;
		}

		public override string Name { get { return "event"; } }

		public Action<string> LogOutputCallback { get; set; }
		public Lua CurrentThread { get; set; }

		#region Events Library Helpers

		private readonly LuaFunctionList _luaFunctions = new LuaFunctionList();

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
					LogOutputCallback(
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
					LogOutputCallback(
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
					LogOutputCallback(
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
					LogOutputCallback(
						"error running function attached by lua function event.onframeend" +
						"\nError message: " +
						e.Message);
				}
			}
		}

		#endregion

		[LuaMethodAttributes(
			"onframeend",
			"TODO"
		)]
		public string OnFrameEnd(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnFrameEnd", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"onframestart",
			"TODO"
		)]
		public string OnFrameStart(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnFrameStart", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"oninputpoll",
			"TODO"
		)]
		public void OnInputPoll(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnInputPoll", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			Global.Emulator.CoreComm.InputCallback.Add(nlf.Callback);
		}

		[LuaMethodAttributes(
			"onloadstate",
			"TODO"
		)]
		public string OnLoadState(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnSavestateLoad", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"onmemoryexecute",
			"TODO"
		)]
		public string OnMemoryExecute(LuaFunction luaf, object address, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnMemoryExecute", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			Global.CoreComm.MemoryCallbackSystem.AddExecute(nlf.Callback, LuaUInt(address));
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"onmemoryread",
			"TODO"
		)]
		public string OnMemoryRead(LuaFunction luaf, object address = null, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnMemoryRead", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			Global.CoreComm.MemoryCallbackSystem.AddRead(nlf.Callback, (address != null ? LuaUInt(address) : (uint?)null));
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"onmemorywrite",
			"TODO"
		)]
		public string OnMemoryWrite(LuaFunction luaf, object address = null, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnMemoryWrite", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			Global.CoreComm.MemoryCallbackSystem.AddWrite(nlf.Callback, (address != null ? LuaUInt(address) : (uint?)null));
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"onsavestate",
			"TODO"
		)]
		public string OnSaveState(LuaFunction luaf, string name = null)
		{
			var nlf = new NamedLuaFunction(luaf, "OnSavestateSave", LogOutputCallback, CurrentThread, name);
			_luaFunctions.Add(nlf);
			return nlf.Guid.ToString();
		}

		[LuaMethodAttributes(
			"unregisterbyid",
			"TODO"
		)]
		public bool UnregisterById(object guid)
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
			"TODO"
		)]
		public bool UnregisterByName(object name)
		{
			foreach (var nlf in _luaFunctions.Where(nlf => nlf.Name == name.ToString()))
			{
				_luaFunctions.Remove(nlf);
				return true;
			}

			return false;
		}
	}
}
