using System;
using System.Linq;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.Common
{
	public class NESLuaLibrary : LuaLibraryBase
	{
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

		public void nes_addgamegenie(string code)
		{
			if (Global.Emulator is NES)
			{
				var decoder = new NESGameGenieDecoder(code);
				Watch watch = Watch.GenerateWatch(
					Global.Emulator.MemoryDomains[1],
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

		// these methods are awkward.  perhaps with the new core config system, one could
		// automatically bring out all of the settings to a lua table, with names.  that
		// would be completely arbitrary and would remove the whole requirement for this mess

		public static bool nes_getallowmorethaneightsprites()
		{
			return ((NES.NESSettings)Global.Emulator.GetSettings()).AllowMoreThanEightSprites;
		}

		public static int nes_getbottomscanline(bool pal = false)
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

		public static bool nes_getclipleftandright()
		{
			return ((NES.NESSettings)Global.Emulator.GetSettings()).ClipLeftAndRight;
		}

		public static bool nes_getdispbackground()
		{
			return ((NES.NESSettings)Global.Emulator.GetSettings()).DispBackground;
		}

		public static bool nes_getdispsprites()
		{
			return ((NES.NESSettings)Global.Emulator.GetSettings()).DispSprites;
		}

		public static int nes_gettopscanline(bool pal = false)
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

		public void nes_removegamegenie(string code)
		{
			if (Global.Emulator is NES)
			{
				var decoder = new NESGameGenieDecoder(code);
				Global.CheatList.RemoveRange(
					Global.CheatList.Where(x => x.Address == decoder.Address)
				);
			}
		}

		public static void nes_setallowmorethaneightsprites(bool allow)
		{
			if (Global.Emulator is NES)
			{
				var s = (NES.NESSettings)Global.Emulator.GetSettings();
				s.AllowMoreThanEightSprites = allow;
				Global.Emulator.PutSettings(s);
			}
		}

		public static void nes_setclipleftandright(bool leftandright)
		{
			if (Global.Emulator is NES)
			{
				var s = (NES.NESSettings)Global.Emulator.GetSettings();
				s.ClipLeftAndRight = leftandright;
				Global.Emulator.PutSettings(s);
			}
		}

		// these seem to duplicate emu.setrenderplanes???
		public static void nes_setdispbackground(bool show)
		{
			if (Global.Emulator is NES)
			{
				var s = (NES.NESSettings)Global.Emulator.GetSettings();
				s.DispBackground = show;
				Global.Emulator.PutSettings(s);
			}
		}

		public static void nes_setdispsprites(bool show)
		{
			if (Global.Emulator is NES)
			{
				var s = (NES.NESSettings)Global.Emulator.GetSettings();
				s.DispSprites = show;
				Global.Emulator.PutSettings(s);
			}
		}

		public static void nes_setscanlines(object top, object bottom, bool pal = false)
		{
			if (Global.Emulator is NES)
			{
				int first = LuaInt(top);
				int last = LuaInt(bottom);
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
