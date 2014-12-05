using System;
using System.ComponentModel;
using System.Linq;

using LuaInterface;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;

namespace BizHawk.Client.Common
{
	[Description("Functions related specifically to Nes Cores")]
	public sealed class NesLuaLibrary : LuaLibraryBase
	{
		// TODO:  
		// perhaps with the new core config system, one could
		// automatically bring out all of the settings to a lua table, with names.  that
		// would be completely arbitrary and would remove the whole requirement for this mess
		public NesLuaLibrary(Lua lua)
			: base(lua) { }

		public NesLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name { get { return "nes"; } }

		[LuaMethodAttributes(
			"addgamegenie",
			"Adds the specified game genie code. If an NES game is not currently loaded or the code is not a valid game genie code, this will have no effect"
		)]
		public void AddGameGenie(string code)
		{
			if (Global.Emulator.SystemId == "NES")
			{
				var decoder = new NESGameGenieDecoder(code);
				var watch = Watch.GenerateWatch(
					Global.Emulator.AsMemoryDomains().MemoryDomains["System Bus"],
					decoder.Address,
					Watch.WatchSize.Byte,
					Watch.DisplayType.Hex,
					code,
					false);

				Global.CheatList.Add(new Cheat(
					watch,
					decoder.Value,
					decoder.Compare));
			}
		}

		private static QuickNES AsQuickNES { get { return Global.Emulator as QuickNES; } }
		private static NES AsNES { get { return Global.Emulator as NES; } }

		[LuaMethodAttributes(
			"getallowmorethaneightsprites",
			"Gets the NES setting 'Allow more than 8 sprites per scanline' value"
		)]
		public static bool GetAllowMoreThanEightSprites()
		{
			if (AsQuickNES != null)
			{
				return AsQuickNES.GetSettings().NumSprites != 8;
			}

			return AsNES.GetSettings().AllowMoreThanEightSprites;
		}

		[LuaMethodAttributes(
			"getbottomscanline",
			"Gets the current value for the bottom scanline value"
		)]
		public static int GetBottomScanline(bool pal = false)
		{
			if (AsQuickNES != null)
			{
				return AsQuickNES.GetSettings().ClipTopAndBottom ? 231 : 239;
			}

			return pal
				? AsNES.GetSettings().PAL_BottomLine
				: AsNES.GetSettings().NTSC_BottomLine;
		}

		[LuaMethodAttributes(
			"getclipleftandright",
			"Gets the current value for the Clip Left and Right sides option"
		)]
		public static bool GetClipLeftAndRight()
		{
			if (AsQuickNES != null)
			{
				return AsQuickNES.GetSettings().ClipLeftAndRight;
			}

			return AsNES.GetSettings().ClipLeftAndRight;
		}

		[LuaMethodAttributes(
			"getdispbackground",
			"Indicates whether or not the bg layer is being displayed"
		)]
		public static bool GetDisplayBackground()
		{
			if (AsQuickNES != null)
			{
				return true;
			}

			return AsNES.GetSettings().DispBackground;
		}

		[LuaMethodAttributes(
			"getdispsprites",
			"Indicates whether or not sprites are being displayed"
		)]
		public static bool GetDisplaySprites()
		{
			if (AsQuickNES != null)
			{
				return true;
			}

			return AsNES.GetSettings().DispSprites;
		}

		[LuaMethodAttributes(
			"gettopscanline",
			"Gets the current value for the top scanline value"
		)]
		public static int GetTopScanline(bool pal = false)
		{
			if (AsQuickNES != null)
			{
				return AsQuickNES.GetSettings().ClipTopAndBottom ? 8 : 0;
			}

			return pal
				? AsNES.GetSettings().PAL_TopLine
				: AsNES.GetSettings().NTSC_TopLine;
		}

		[LuaMethodAttributes(
			"removegamegenie",
			"Removes the specified game genie code. If an NES game is not currently loaded or the code is not a valid game genie code, this will have no effect"
		)]
		public void RemoveGameGenie(string code)
		{
			if (Global.Emulator.SystemId == "NES")
			{
				var decoder = new NESGameGenieDecoder(code);
				Global.CheatList.RemoveRange(
					Global.CheatList.Where(x => x.Address == decoder.Address));
			}
		}

		[LuaMethodAttributes(
			"setallowmorethaneightsprites",
			"Sets the NES setting 'Allow more than 8 sprites per scanline'"
		)]
		public static void SetAllowMoreThanEightSprites(bool allow)
		{
			if (Global.Emulator is NES)
			{
				var s = AsNES.GetSettings();
				s.AllowMoreThanEightSprites = allow;
				AsNES.PutSettings(s);
			}
			else if (Global.Emulator is QuickNES)
			{
				var s = AsQuickNES.GetSettings();
				s.NumSprites = allow ? 64 : 8;
				AsQuickNES.PutSettings(s);
				
			}
		}

		[LuaMethodAttributes(
			"setclipleftandright",
			"Sets the Clip Left and Right sides option"
		)]
		public static void SetClipLeftAndRight(bool leftandright)
		{
			if (Global.Emulator is NES)
			{
				var s = AsNES.GetSettings();
				s.ClipLeftAndRight = leftandright;
				AsNES.PutSettings(s);
			}
			else if (Global.Emulator is QuickNES)
			{
				var s = AsQuickNES.GetSettings();
				s.ClipLeftAndRight = leftandright;
				AsQuickNES.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setdispbackground",
			"Sets whether or not the background layer will be displayed"
		)]
		public static void SetDisplayBackground(bool show)
		{
			if (Global.Emulator is NES)
			{
				var s = AsNES.GetSettings();
				s.DispBackground = show;
				AsNES.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setdispsprites",
			"Sets whether or not sprites will be displayed"
		)]
		public static void SetDisplaySprites(bool show)
		{
			if (Global.Emulator is NES)
			{
				var s = AsNES.GetSettings();
				s.DispSprites = show;
				AsNES.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setscanlines",
			"sets the top and bottom scanlines to be drawn (same values as in the graphics options dialog). Top must be in the range of 0 to 127, bottom must be between 128 and 239. Not supported in the Quick Nes core"
		)]
		public static void SetScanlines(int top, int bottom, bool pal = false)
		{
			if (Global.Emulator is NES)
			{
				if (top > 127)
				{
					top = 127;
				}
				else if (top < 0)
				{
					top = 0;
				}

				if (bottom > 239)
				{
					bottom = 239;
				}
				else if (bottom < 128)
				{
					bottom = 128;
				}

				var s = AsNES.GetSettings();

				if (pal)
				{
					s.PAL_TopLine = top;
					s.PAL_BottomLine = bottom;
				}
				else
				{
					s.NTSC_TopLine = top;
					s.NTSC_BottomLine = bottom;
				}

				AsNES.PutSettings(s);
			}
		}
	}
}
