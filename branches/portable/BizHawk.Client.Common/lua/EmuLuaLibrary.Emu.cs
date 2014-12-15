using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.WonderSwan;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;

using LuaInterface;

namespace BizHawk.Client.Common
{
	[Description("A library for interacting with the currently loaded emulator core")]
	public sealed class EmulatorLuaLibrary : LuaLibraryBase
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
		public int GetRegister(string name)
		{
			try
			{
				var debuggable = Global.Emulator.AsDebuggable();
				if (!Global.Emulator.CanDebug())
				{
					throw new NotImplementedException();
				}

				var registers = debuggable.AsDebuggable().GetCpuFlagsAndRegisters();
				return registers.ContainsKey(name)
					? registers[name]
					: 0;
			}
			catch (NotImplementedException)
			{

				Log(string.Format(
					"Error: {0} does not yet implement getregister()",
					Global.Emulator.Attributes().CoreName));
				return 0;
			}
		}

		[LuaMethodAttributes(
			"getregisters",
			"returns the complete set of available flags and registers for a given core"
		)]
		public LuaTable GetRegisters()
		{
			var table = Lua.NewTable();

			try
			{
				var debuggable = Global.Emulator.AsDebuggable();
				if (!Global.Emulator.CanDebug())
				{
					throw new NotImplementedException();
				}

				foreach (var kvp in debuggable.GetCpuFlagsAndRegisters())
				{
					table[kvp.Key] = kvp.Value;
				}
			}
			catch (NotImplementedException)
			{
				Log(string.Format(
					"Error: {0} does not yet implement getregisters()",
					Global.Emulator.Attributes().CoreName));
			}

			return table;
		}

		[LuaMethodAttributes(
			"setregister",
			"sets the given register name to the given value"
		)]
		public void SetRegister(string register, int value)
		{
			try
			{
				var debuggable = Global.Emulator.AsDebuggable();
				if (!Global.Emulator.CanDebug())
				{
					throw new NotImplementedException();
				}

				debuggable.SetCpuRegister(register, value);
			}
			catch (NotImplementedException)
			{
				Log(string.Format(
					"Error: {0} does not yet implement setregister()",
					Global.Emulator.Attributes().CoreName));
			}
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
		public bool IsLagged()
		{
			if (Global.Emulator.CanPollInput())
			{
				return Global.Emulator.AsInputPollable().IsLagFrame;
			}
			else
			{
				Log("Can not get lag information, core does not implement IInputPollable");
				return false;
			}
		}

		[LuaMethodAttributes(
			"lagcount",
			"Returns the current lag count"
		)]
		public int LagCount()
		{
			if (Global.Emulator.CanPollInput())
			{
				return Global.Emulator.AsInputPollable().LagCount;
			}
			else
			{
				Log("Can not get lag information, core does not implement IInputPollable");
				return 0;
			}
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
				var nes = Global.Emulator as NES;
				var s = nes.GetSettings();
				s.DispSprites = (bool)luaParam[0];
				s.DispBackground = (bool)luaParam[1];
				nes.PutSettings(s);
			}
			else if (Global.Emulator is QuickNES)
			{
				var quicknes = Global.Emulator as QuickNES;
				var s = quicknes.GetSettings();
				// this core doesn't support disabling BG
				bool showsp = GetSetting(0, luaParam);
				if (showsp && s.NumSprites == 0)
					s.NumSprites = 8;
				else if (!showsp && s.NumSprites > 0)
					s.NumSprites = 0;
				quicknes.PutSettings(s);
			}
			else if (Global.Emulator is PCEngine)
			{
				var pce = Global.Emulator as PCEngine;
				var s = pce.GetSettings();
				s.ShowOBJ1 = GetSetting(0, luaParam);
				s.ShowBG1 = GetSetting(1, luaParam);
				if (luaParam.Length > 2)
				{
					s.ShowOBJ2 = GetSetting(2, luaParam);
					s.ShowBG2 = GetSetting(3, luaParam);
				}

				pce.PutSettings(s);
			}
			else if (Global.Emulator is SMS)
			{
				var sms = Global.Emulator as SMS;
				var s = sms.GetSettings();
				s.DispOBJ = GetSetting(0, luaParam);
				s.DispBG = GetSetting(1, luaParam);
				sms.PutSettings(s);
			}
			else if (Global.Emulator is WonderSwan)
			{
				var ws = Global.Emulator as WonderSwan;
				var s = ws.GetSettings();
				s.EnableSprites = GetSetting(0, luaParam);
				s.EnableFG = GetSetting(1, luaParam);
				s.EnableBG = GetSetting(2, luaParam);
				ws.PutSettings(s);
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
