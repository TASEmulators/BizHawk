using System;
using System.Collections.Generic;
using System.Linq;
using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	public partial class EmuLuaLibrary
	{
		#region Events Library Helpers

		private readonly LuaFunctionList lua_functions = new LuaFunctionList();

		public LuaFunctionList RegisteredFunctions { get { return lua_functions; } }

		public void SavestateRegisterSave(string name)
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
					GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(
						"error running function attached by lua function savestate.registersave" +
						"\nError message: " + e.Message);
				}
			}
		}

		public void SavestateRegisterLoad(string name)
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
					GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(
						"error running function attached by lua function savestate.registerload" +
						"\nError message: " + e.Message);
				}
			}
		}

		public void FrameRegisterBefore()
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
					GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(
						"error running function attached by lua function emu.registerbefore" +
						"\nError message: " + e.Message);
				}
			}
		}

		public void FrameRegisterAfter()
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
					GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(
						"error running function attached by lua function emu.registerafter" +
						"\nError message: " + e.Message);
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
			emu_on_snoop(luaf);
		}

		public string event_onloadstate(LuaFunction luaf, object name = null)
		{
			return savestate_registerload(luaf, name);
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
						GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(
							"error running function attached by lua function event.onmemoryread" +
							"\nError message: " + e.Message);
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
						GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(
							"error running function attached by lua function event.onmemoryread" +
							"\nError message: " + e.Message);
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
			return savestate_registersave(luaf, name);
		}

		public bool event_unregisterbyid(object guid)
		{
			//Iterating every possible event type is not very scalable
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
			//Horribly redundant to the function above!
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
