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
		private readonly Lua _lua;
		private readonly Action _frameAdvanceCallback;
		private readonly Action _yieldCallback;

		public EmulatorLuaLibrary(Lua lua, Action frameAdvanceCallback, Action yieldCallback)
		{
			_lua = lua;
			_frameAdvanceCallback = frameAdvanceCallback;
			_yieldCallback = yieldCallback;
		}

		public override string Name { get { return "emu"; } }

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
			_frameAdvanceCallback();
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
			var table = _lua.NewTable();
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
			return Global.Emulator.SystemId;
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
		public static void SetRenderPlanes( // For now, it accepts arguments up to 5.
			object param0, 
			object param1 = null, 
			object param2 = null,
			object param3 = null, 
			object param4 = null)
		{
			SetrenderplanesDo(LuaVarArgs(param0, param1, param2, param3, param4));
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
