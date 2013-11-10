using System;
using System.Collections.Generic;
using System.Linq;
using LuaInterface;

namespace BizHawk.Client.Common
{
	public class EventLuaLibrary : LuaLibraryBase
	{
		public EventLuaLibrary(Action<string> logOutputCallback)
			: base()
		{
			LogOutputCallback = logOutputCallback;
		}

		public override string Name { get { return "event"; } }
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"onframeend",
					"onframestart",
					"oninputpoll",
					"onloadstate",
					"onmemoryread",
					"onmemorywrite",
					"onsavestate",
					"unregisterbyid",
					"unregisterbyname",
				};
			}
		}

		public Action<string> LogOutputCallback = null;

		#region Events Library Helpers

		private readonly LuaFunctionList lua_functions = new LuaFunctionList();

		public LuaFunctionList RegisteredFunctions { get { return lua_functions; } }

		public void CallSaveStateEvent(string name)
		{
			List<NamedLuaFunction> lfs = lua_functions.Where(x => x.Event == "OnSavestateSave").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (NamedLuaFunction lf in lfs)
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
			List<NamedLuaFunction> lfs = lua_functions.Where(x => x.Event == "OnSavestateLoad").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (NamedLuaFunction lf in lfs)
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
			List<NamedLuaFunction> lfs = lua_functions.Where(x => x.Event == "OnFrameStart").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (NamedLuaFunction lf in lfs)
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
			List<NamedLuaFunction> lfs = lua_functions.Where(x => x.Event == "OnFrameEnd").ToList();
			if (lfs.Any())
			{
				try
				{
					foreach (NamedLuaFunction lf in lfs)
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

		public string event_onframeend(LuaFunction luaf, string name = null)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnFrameEnd", name);
			lua_functions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public string event_onframestart(LuaFunction luaf, string name = null)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnFrameStart", name);
			lua_functions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public void event_oninputpoll(LuaFunction luaf)
		{
			if (luaf != null)
			{
				Global.Emulator.CoreComm.InputCallback.Add(delegate
				{
					try
					{
						luaf.Call();
					}
					catch (SystemException e)
					{
						LogOutputCallback(
							"error running function attached by lua function event.oninputpoll" +
							"\nError message: "
							+ e.Message);
					}
				});
			}
			else
			{
				Global.Emulator.CoreComm.InputCallback = null;
			}
		}

		public string event_onloadstate(LuaFunction luaf, object name = null)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnSavestateLoad", name != null ? name.ToString() : null);
			lua_functions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public void event_onmemoryread(LuaFunction luaf, object address = null)
		{
			//TODO: allow a list of addresses
			if (luaf != null)
			{
				int? _addr;
				if (address == null)
				{
					_addr = null;
				}
				else
				{
					_addr = LuaInt(address);
				}

				Global.Emulator.CoreComm.MemoryCallbackSystem.ReadAddr = _addr;
				Global.Emulator.CoreComm.MemoryCallbackSystem.SetReadCallback(delegate(uint addr)
				{
					try
					{
						luaf.Call(addr);
					}
					catch (SystemException e)
					{
						LogOutputCallback(
							"error running function attached by lua function event.onmemoryread" +
							"\nError message: " +
							e.Message);
					}
				});
			}
			else
			{
				Global.Emulator.CoreComm.MemoryCallbackSystem.SetReadCallback(null);
			}
		}

		public void event_onmemorywrite(LuaFunction luaf, object address = null)
		{
			//TODO: allow a list of addresses
			if (luaf != null)
			{
				int? _addr;
				if (address == null)
				{
					_addr = null;
				}
				else
				{
					_addr = LuaInt(address);
				}

				Global.Emulator.CoreComm.MemoryCallbackSystem.WriteAddr = _addr;
				Global.Emulator.CoreComm.MemoryCallbackSystem.SetWriteCallback(delegate(uint addr)
				{
					try
					{
						luaf.Call(addr);
					}
					catch (SystemException e)
					{
						LogOutputCallback(
							"error running function attached by lua function event.onmemoryread" +
							"\nError message: " +
							e.Message);
					}
				});
			}
			else
			{
				Global.Emulator.CoreComm.MemoryCallbackSystem.SetWriteCallback(null);
			}
		}

		public string event_onsavestate(LuaFunction luaf, object name = null)
		{
			NamedLuaFunction nlf = new NamedLuaFunction(luaf, "OnSavestateSave", name != null ? name.ToString() : null);
			lua_functions.Add(nlf);
			return nlf.GUID.ToString();
		}

		public bool event_unregisterbyid(object guid)
		{
			foreach (NamedLuaFunction nlf in lua_functions)
			{
				if (nlf.GUID.ToString() == guid.ToString())
				{
					lua_functions.Remove(nlf);
					return true;
				}
			}

			return false;
		}

		public bool event_unregisterbyname(object name)
		{
			foreach (NamedLuaFunction nlf in lua_functions)
			{
				if (nlf.Name == name.ToString())
				{
					lua_functions.Remove(nlf);
					return true;
				}
			}

			return false;
		}
	}
}
