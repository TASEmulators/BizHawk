using System;
using System.Linq;

using LuaInterface;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.Common
{
	public partial class EmulatorLuaLibrary : LuaLibraryBase
	{
		public EmulatorLuaLibrary(Lua lua, Action frameAdvanceCallback, Action yieldCallback)
			: base()
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

		private Lua _lua;
		private Action _frameAdvanceCallback;
		private Action _yieldCallback;

		private static void emu_setrenderplanes_do(object[] lua_p)
		{
			if (Global.Emulator is NES)
			{
				// in the future, we could do something more arbitrary here.
				// but this isn't any worse than the old system
				NES.NESSettings s = (NES.NESSettings)Global.Emulator.GetSettings();
				s.DispSprites = (bool)lua_p[0];
				s.DispBackground = (bool)lua_p[1];
				Global.Emulator.PutSettings(s);
			}
			else if (Global.Emulator is PCEngine)
			{
				PCEngine.PCESettings s = (PCEngine.PCESettings)Global.Emulator.GetSettings();
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
				Global.CoreComm.SMS_ShowOBJ = Global.Config.SMSDispOBJ = (bool)lua_p[0];
				Global.CoreComm.SMS_ShowBG = Global.Config.SMSDispBG = (bool)lua_p[1];
			}
		}

		public static void emu_displayvsync(object boolean)
		{
			string temp = boolean.ToString();
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
			LuaTable table = _lua.NewTable();
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
			string temp = boolean.ToString();
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
			string temp = boolean.ToString();
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
			object lua_p0, object lua_p1 = null, object lua_p2 = null,
			object lua_p3 = null, object lua_p4 = null)
		{
			emu_setrenderplanes_do(LuaVarArgs(lua_p0, lua_p1, lua_p2, lua_p3, lua_p4));
		}

		public void emu_yield()
		{
			_yieldCallback();
		}
	}
}
