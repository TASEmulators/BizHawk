using System.Linq;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.Common
{
	public class NESLuaLibrary : LuaLibraryBase
	{
		// TODO:  
		// perhaps with the new core config system, one could
		// automatically bring out all of the settings to a lua table, with names.  that
		// would be completely arbitrary and would remove the whole requirement for this mess
		public override string Name { get { return "nes"; } }
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"addgamegenie",
					"getallowmorethaneightsprites",
					"getbottomscanline",
					"getclipleftandright",
					"getdispbackground",
					"getdispsprites",
					"gettopscanline",
					"removegamegenie",
					"setallowmorethaneightsprites",
					"setclipleftandright",
					"setdispbackground",
					"setdispsprites",
					"setscanlines"
				};
			}
		}

		[LuaMethodAttributes(
			"addgamegenie",
			"TODO"
		)]
		public void AddGameGenie(string code)
		{
			if (Global.Emulator.SystemId == "NES")
			{
				var decoder = new NESGameGenieDecoder(code);
				var watch = Watch.GenerateWatch(
					Global.Emulator.MemoryDomains["System Bus"],
					decoder.Address,
					Watch.WatchSize.Byte,
					Watch.DisplayType.Hex,
					code,
					false
				);
				Global.CheatList.Add(new Cheat(
					watch,
					decoder.Value,
					decoder.Compare
				));
			}
		}

		[LuaMethodAttributes(
			"getallowmorethaneightsprites",
			"TODO"
		)]
		public static bool GetAllowMoreThanEightSprites()
		{
			return ((NES.NESSettings)Global.Emulator.GetSettings()).AllowMoreThanEightSprites;
		}

		[LuaMethodAttributes(
			"getbottomscanline",
			"TODO"
		)]
		public static int GetBottomScanline(bool pal = false)
		{
			if (pal)
			{
				return ((NES.NESSettings)Global.Emulator.GetSettings()).PAL_BottomLine;
			}
			else
			{
				return ((NES.NESSettings)Global.Emulator.GetSettings()).NTSC_BottomLine;
			}
		}

		[LuaMethodAttributes(
			"getclipleftandright",
			"TODO"
		)]
		public static bool GetClipLeftAndRight()
		{
			return ((NES.NESSettings)Global.Emulator.GetSettings()).ClipLeftAndRight;
		}

		[LuaMethodAttributes(
			"getdispbackground",
			"TODO"
		)]
		public static bool GetDisplayBackground()
		{
			return ((NES.NESSettings)Global.Emulator.GetSettings()).DispBackground;
		}

		[LuaMethodAttributes(
			"getdispsprites",
			"TODO"
		)]
		public static bool GetDisplaySprites()
		{
			return ((NES.NESSettings)Global.Emulator.GetSettings()).DispSprites;
		}

		[LuaMethodAttributes(
			"gettopscanline",
			"TODO"
		)]
		public static int GetTopScanline(bool pal = false)
		{
			if (pal)
			{
				return ((NES.NESSettings)Global.Emulator.GetSettings()).PAL_TopLine;
			}
			else
			{
				return ((NES.NESSettings)Global.Emulator.GetSettings()).NTSC_TopLine;
			}
		}

		[LuaMethodAttributes(
			"removegamegenie",
			"TODO"
		)]
		public void RemoveGameGenie(string code)
		{
			if (Global.Emulator.SystemId == "NES")
			{
				var decoder = new NESGameGenieDecoder(code);
				Global.CheatList.RemoveRange(
					Global.CheatList.Where(x => x.Address == decoder.Address)
				);
			}
		}

		[LuaMethodAttributes(
			"setallowmorethaneightsprites",
			"TODO"
		)]
		public static void SetAllowMoreThanEightSprites(bool allow)
		{
			if (Global.Emulator is NES)
			{
				var s = (NES.NESSettings)Global.Emulator.GetSettings();
				s.AllowMoreThanEightSprites = allow;
				Global.Emulator.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setclipleftandright",
			"TODO"
		)]
		public static void SetClipLeftAndRight(bool leftandright)
		{
			if (Global.Emulator is NES)
			{
				var s = (NES.NESSettings)Global.Emulator.GetSettings();
				s.ClipLeftAndRight = leftandright;
				Global.Emulator.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setdispbackground",
			"TODO"
		)]
		public static void SetDisplayBackground(bool show)
		{
			if (Global.Emulator is NES)
			{
				var s = (NES.NESSettings)Global.Emulator.GetSettings();
				s.DispBackground = show;
				Global.Emulator.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setdispsprites",
			"TODO"
		)]
		public static void SetDisplaySprites(bool show)
		{
			if (Global.Emulator is NES)
			{
				var s = (NES.NESSettings)Global.Emulator.GetSettings();
				s.DispSprites = show;
				Global.Emulator.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setscanlines",
			"TODO"
		)]
		public static void SetScanlines(object top, object bottom, bool pal = false)
		{
			if (Global.Emulator is NES)
			{
				var first = LuaInt(top);
				var last = LuaInt(bottom);
				if (first > 127)
				{
					first = 127;
				}
				else if (first < 0)
				{
					first = 0;
				}

				if (last > 239)
				{
					last = 239;
				}
				else if (last < 128)
				{
					last = 128;
				}

				var s = (NES.NESSettings)Global.Emulator.GetSettings();

				if (pal)
				{
					s.PAL_TopLine = first;
					s.PAL_BottomLine = last;
				}
				else
				{
					s.NTSC_TopLine = first;
					s.NTSC_BottomLine = last;
				}

				Global.Emulator.PutSettings(s);
			}
		}
	}
}
