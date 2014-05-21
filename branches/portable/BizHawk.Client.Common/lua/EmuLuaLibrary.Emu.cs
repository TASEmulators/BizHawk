using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

using LuaInterface;

namespace BizHawk.Client.Common
{
	public class EmulatorLuaLibrary : LuaLibraryBase
	{
		public Action FrameAdvanceCallback { get; set; }
		public Action YieldCallback { get; set; }



		public EmulatorLuaLibrary(Lua lua)
			: base(lua) { }

		public EmulatorLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name { get { return "emu"; } }

		[LuaMethodAttributes(
			"displayvsync",
			"Sets the display vsync property of the emulator"
		)]
		public static void DisplayVsync(bool enabled)
		{
			Global.Config.VSyncThrottle = enabled;
		}

		[LuaMethodAttributes(
			"frameadvance",
			"Signals to the emulator to resume emulation. Necessary for any lua script while loop or else the emulator will freeze!"
		)]
		public void FrameAdvance()
		{
			FrameAdvanceCallback();
		}

		[LuaMethodAttributes(
			"framecount",
			"Returns the current frame count"
		)]
		public static int FrameCount()
		{
			return Global.Emulator.Frame;
		}

		[LuaMethodAttributes(
			"getregister",
			"returns the value of a cpu register or flag specified by name. For a complete list of possible registers or flags for a given core, use getregisters"
		)]
		public static int GetRegister(string name)
		{
			return Global.Emulator.GetCpuFlagsAndRegisters().FirstOrDefault(x => x.Key == name).Value;
		}

		[LuaMethodAttributes(
			"getregisters",
			"returns the complete set of available flags and registers for a given core"
		)]
		public LuaTable GetRegisters()
		{
			var table = Lua.NewTable();
			foreach (var kvp in Global.Emulator.GetCpuFlagsAndRegisters())
			{
				table[kvp.Key] = kvp.Value;
			}

			return table;
		}

		[LuaMethodAttributes(
			"getsystemid",
			"Returns the ID string of the current core loaded. Note: No ROM loaded will return the string NULL"
		)]
		public static string GetSystemId()
		{
			return Global.Game.System;
		}

		[LuaMethodAttributes(
			"islagged",
			"returns whether or not the current frame is a lag frame"
		)]
		public static bool IsLagged()
		{
			return Global.Emulator.IsLagFrame;
		}

		[LuaMethodAttributes(
			"lagcount",
			"Returns the current lag count"
		)]
		public static int LagCount()
		{
			return Global.Emulator.LagCount;
		}

		[LuaMethodAttributes(
			"limitframerate",
			"sets the limit framerate property of the emulator"
		)]
		public static void LimitFramerate(bool enabled)
		{
			Global.Config.ClockThrottle = enabled;
		}

		[LuaMethodAttributes(
			"minimizeframeskip",
			"Sets the autominimizeframeskip value of the emulator"
		)]
		public static void MinimizeFrameskip(bool enabled)
		{
			Global.Config.AutoMinimizeSkipping = enabled;
		}

		[LuaMethodAttributes(
			"setrenderplanes",
			"Toggles the drawing of sprites and background planes. Set to false or nil to disable a pane, anything else will draw them"
		)]
		public static void SetRenderPlanes(params bool[] luaParam)
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
				s.ShowOBJ1 = GetSetting(0, luaParam);
				s.ShowBG1 = GetSetting(1, luaParam);
				if (luaParam.Length > 2)
				{
					s.ShowOBJ2 = GetSetting(2, luaParam);
					s.ShowBG2 = GetSetting(3, luaParam);
				}

				Global.Emulator.PutSettings(s);
			}
			else if (Global.Emulator is SMS)
			{
				var s = (SMS.SMSSettings)Global.Emulator.GetSettings();
				s.DispOBJ = GetSetting(0, luaParam);
				s.DispBG = GetSetting(1, luaParam);
				Global.Emulator.PutSettings(s);
			}
		}

		private static bool GetSetting(int index, bool[] settings)
		{
			if (index < settings.Length)
			{
				return settings[index];
			}

			return true;
		}

		[LuaMethodAttributes(
			"yield",
			"allows a script to run while emulation is paused and interact with the gui/main window in realtime "
		)]
		public void Yield()
		{
			YieldCallback();
		}

		[LuaMethodAttributes(
			"getdisplaytype",
			"returns the display type (PAL vs NTSC) that the emulator is currently running in"
		)]
		public string GetDisplayType()
		{
			if (Global.Game != null)
			{
				var displaytype = Global.Emulator.GetType().GetProperty("DisplayType");
				if (displaytype != null)
				{
					return displaytype.GetValue(Global.Emulator, null).ToString();
				}
			}

			return string.Empty;
		}
	}
}
