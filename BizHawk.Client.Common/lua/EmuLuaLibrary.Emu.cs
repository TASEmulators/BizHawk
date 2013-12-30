using System;
using System.Linq;

using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

using LuaInterface;

namespace BizHawk.Client.Common
{
	public class EmulatorLuaLibrary : LuaLibraryBase
	{
		public EmulatorLuaLibrary(Lua lua, Action frameAdvanceCallback, Action yieldCallback)
		{
			_lua = lua;
			_frameAdvanceCallback = frameAdvanceCallback;
			_yieldCallback = yieldCallback;
		}

		public override string Name { get { return "emu"; } }
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"displayvsync",
					"frameadvance",
					"framecount",
					"getregister",
					"getregisters",
					"getsystemid",
					"islagged",
					"lagcount",
					"limitframerate",
					"minimizeframeskip",
					"setrenderplanes",
					"yield",
				};
			}
		}

		private readonly Lua _lua;
		private readonly Action _frameAdvanceCallback;
		private readonly Action _yieldCallback;

		private static void emu_setrenderplanes_do(object[] lua_p)
		{
			if (Global.Emulator is NES)
			{
				// in the future, we could do something more arbitrary here.
				// but this isn't any worse than the old system
				var s = (NES.NESSettings)Global.Emulator.GetSettings();
				s.DispSprites = (bool)lua_p[0];
				s.DispBackground = (bool)lua_p[1];
				Global.Emulator.PutSettings(s);
			}
			else if (Global.Emulator is PCEngine)
			{
				var s = (PCEngine.PCESettings)Global.Emulator.GetSettings();
				s.ShowOBJ1 = (bool)lua_p[0];
				s.ShowBG1 = (bool)lua_p[1];
				if (lua_p.Length > 2)
				{
					s.ShowOBJ2 = (bool)lua_p[2];
					s.ShowBG2 = (bool)lua_p[3];
				}

				Global.Emulator.PutSettings(s);
			}
			else if (Global.Emulator is SMS)
			{
				var s = (SMS.SMSSettings)Global.Emulator.GetSettings();
				s.DispOBJ = (bool)lua_p[0];
				s.DispBG = (bool)lua_p[1];
				Global.Emulator.PutSettings(s);
			}
		}

		public static void emu_displayvsync(object boolean)
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

		public void emu_frameadvance()
		{
			_frameAdvanceCallback();
		}

		public static int emu_framecount()
		{
			return Global.Emulator.Frame;
		}

		public static int emu_getregister(string name)
		{
			return Global.Emulator.GetCpuFlagsAndRegisters().FirstOrDefault(x => x.Key == name).Value;
		}

		public LuaTable emu_getregisters()
		{
			var table = _lua.NewTable();
			foreach (var kvp in Global.Emulator.GetCpuFlagsAndRegisters())
			{
				table[kvp.Key] = kvp.Value;
			}

			return table;
		}

		public static string emu_getsystemid()
		{
			return Global.Emulator.SystemId;
		}

		public static bool emu_islagged()
		{
			return Global.Emulator.IsLagFrame;
		}

		public static int emu_lagcount()
		{
			return Global.Emulator.LagCount;
		}

		public static void emu_limitframerate(object boolean)
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

		public static void emu_minimizeframeskip(object boolean)
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

		public static void emu_setrenderplanes( // For now, it accepts arguments up to 5.
			object lua_p0, 
			object lua_p1 = null, 
			object lua_p2 = null,
			object lua_p3 = null, 
			object lua_p4 = null)
		{
			emu_setrenderplanes_do(LuaVarArgs(lua_p0, lua_p1, lua_p2, lua_p3, lua_p4));
		}

		public void emu_yield()
		{
			_yieldCallback();
		}
	}
}
