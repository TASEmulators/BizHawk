using System;
using System.Linq;

using LuaInterface;
using BizHawk.Emulation.Consoles.Nintendo;

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
				Global.CoreComm.NES_ShowOBJ = Global.Config.NESDispSprites = (bool)lua_p[0];
				Global.CoreComm.NES_ShowBG = Global.Config.NESDispBackground = (bool)lua_p[1];
			}
			else if (Global.Emulator is Emulation.Consoles.TurboGrafx.PCEngine)
			{
				Global.CoreComm.PCE_ShowOBJ1 = Global.Config.PCEDispOBJ1 = (bool)lua_p[0];
				Global.CoreComm.PCE_ShowBG1 = Global.Config.PCEDispBG1 = (bool)lua_p[1];
				if (lua_p.Length > 2)
				{
					Global.CoreComm.PCE_ShowOBJ2 = Global.Config.PCEDispOBJ2 = (bool)lua_p[2];
					Global.CoreComm.PCE_ShowBG2 = Global.Config.PCEDispBG2 = (bool)lua_p[3];
				}
			}
			else if (Global.Emulator is Emulation.Consoles.Sega.SMS)
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
