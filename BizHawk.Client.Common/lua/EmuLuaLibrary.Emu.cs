using System;
using System.Linq;

using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

using LuaInterface;

namespace BizHawk.Client.Common
{
	using System.Collections.Generic;

	public class EmulatorLuaLibrary : LuaLibraryBase
	{
		public EmulatorLuaLibrary(Lua lua, Action frameAdvanceCallback, Action yieldCallback)
		{
			_lua = lua;
			_frameAdvanceCallback = frameAdvanceCallback;
			_yieldCallback = yieldCallback;
		}

		public override string Name { get { return "emu"; } }

		private readonly Lua _lua;
		private readonly Action _frameAdvanceCallback;
		private readonly Action _yieldCallback;

		private static void SetrenderplanesDo(IList<object> luaParam)
		{
			if (Global.Emulator is NES)
			{
				// in the future, we could do something more arbitrary here.
				// but this isn't any worse than the old system
				var s = (NES.NESSettings)Global.Emulator.GetSettings();
				s.DispSprites = (bool)luaParam[0];
				s.DispBackground = (bool)luaParam[1];
				Global.Emulator.PutSettings(s);
			}
			else if (Global.Emulator is PCEngine)
			{
				var s = (PCEngine.PCESettings)Global.Emulator.GetSettings();
				s.ShowOBJ1 = (bool)luaParam[0];
				s.ShowBG1 = (bool)luaParam[1];
				if (luaParam.Count > 2)
				{
					s.ShowOBJ2 = (bool)luaParam[2];
					s.ShowBG2 = (bool)luaParam[3];
				}

				Global.Emulator.PutSettings(s);
			}
			else if (Global.Emulator is SMS)
			{
				var s = (SMS.SMSSettings)Global.Emulator.GetSettings();
				s.DispOBJ = (bool)luaParam[0];
				s.DispBG = (bool)luaParam[1];
				Global.Emulator.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"displayvsync",
			"TODO"
		)]
		public static void DisplayVsync(object boolean)
		{
			var temp = boolean.ToString();
			if (!String.IsNullOrWhiteSpace(temp))
			{
				if (temp == "0" || temp.ToLower() == "false")
				{
					Global.Config.VSyncThrottle = false;
				}
				else
				{
					Global.Config.VSyncThrottle = true;
				}
			}
		}

		[LuaMethodAttributes(
			"frameadvance",
			"TODO"
		)]
		public void FrameAdvance()
		{
			_frameAdvanceCallback();
		}

		[LuaMethodAttributes(
			"framecount",
			"TODO"
		)]
		public static int FrameCount()
		{
			return Global.Emulator.Frame;
		}

		[LuaMethodAttributes(
			"getregister",
			"TODO"
		)]
		public static int GetRegister(string name)
		{
			return Global.Emulator.GetCpuFlagsAndRegisters().FirstOrDefault(x => x.Key == name).Value;
		}

		[LuaMethodAttributes(
			"getregisters",
			"TODO"
		)]
		public LuaTable GetRegisters()
		{
			var table = _lua.NewTable();
			foreach (var kvp in Global.Emulator.GetCpuFlagsAndRegisters())
			{
				table[kvp.Key] = kvp.Value;
			}

			return table;
		}

		[LuaMethodAttributes(
			"getsystemid",
			"TODO"
		)]
		public static string GetSystemId()
		{
			return Global.Emulator.SystemId;
		}

		[LuaMethodAttributes(
			"islagged",
			"TODO"
		)]
		public static bool IsLagged()
		{
			return Global.Emulator.IsLagFrame;
		}

		[LuaMethodAttributes(
			"lagcount",
			"TODO"
		)]
		public static int LagCount()
		{
			return Global.Emulator.LagCount;
		}

		[LuaMethodAttributes(
			"limitframerate",
			"TODO"
		)]
		public static void LimitFramerate(object boolean)
		{
			var temp = boolean.ToString();
			if (!String.IsNullOrWhiteSpace(temp))
			{
				if (temp == "0" || temp.ToLower() == "false")
				{
					Global.Config.ClockThrottle = false;
				}
				else
				{
					Global.Config.ClockThrottle = true;
				}
			}
		}

		[LuaMethodAttributes(
			"minimizeframeskip",
			"TODO"
		)]
		public static void MinimizeFrameskip(object boolean)
		{
			var temp = boolean.ToString();
			if (!String.IsNullOrWhiteSpace(temp))
			{
				if (temp == "0" || temp.ToLower() == "false")
				{
					Global.Config.AutoMinimizeSkipping = false;
				}
				else
				{
					Global.Config.AutoMinimizeSkipping = true;
				}
			}
		}

		[LuaMethodAttributes(
			"setrenderplanes",
			"TODO"
		)]
		public static void SetRenderPlanes( // For now, it accepts arguments up to 5.
			object lua_p0, 
			object lua_p1 = null, 
			object lua_p2 = null,
			object lua_p3 = null, 
			object lua_p4 = null)
		{
			SetrenderplanesDo(LuaVarArgs(lua_p0, lua_p1, lua_p2, lua_p3, lua_p4));
		}

		[LuaMethodAttributes(
			"yield",
			"TODO"
		)]
		public void Yield()
		{
			_yieldCallback();
		}
	}
}
