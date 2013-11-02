using System;
using System.Linq;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.Client.Common
{
	public class NESLuaLibrary : LuaLibraryBase
	{
		public NESLuaLibrary(Action updateCallback = null)
			: base()
		{
			UpdateCallback = updateCallback;
		}

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

		private Action UpdateCallback;

		private void Update()
		{
			if (UpdateCallback != null)
			{
				UpdateCallback();
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
				Update();
			}
		}

		public static bool nes_getallowmorethaneightsprites()
		{
			return Global.Config.NESAllowMoreThanEightSprites;
		}

		public static int nes_getbottomscanline(bool pal = false)
		{
			if (pal)
			{
				return Global.Config.PAL_NESBottomLine;
			}
			else
			{
				return Global.Config.NTSC_NESBottomLine;
			}
		}

		public static bool nes_getclipleftandright()
		{
			return Global.Config.NESClipLeftAndRight;
		}

		public static bool nes_getdispbackground()
		{
			return Global.Config.NESDispBackground;
		}

		public static bool nes_getdispsprites()
		{
			return Global.Config.NESDispSprites;
		}

		public static int nes_gettopscanline(bool pal = false)
		{
			if (pal)
			{
				return Global.Config.PAL_NESTopLine;
			}
			else
			{
				return Global.Config.NTSC_NESTopLine;
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
				Update();
			}
		}

		public static void nes_setallowmorethaneightsprites(bool allow)
		{
			Global.Config.NESAllowMoreThanEightSprites = allow;
			if (Global.Emulator is NES)
			{
				(Global.Emulator as NES).CoreComm.NES_UnlimitedSprites = allow;
			}
		}

		public static void nes_setclipleftandright(bool leftandright)
		{
			Global.Config.NESClipLeftAndRight = leftandright;
			if (Global.Emulator is NES)
			{
				(Global.Emulator as NES).SetClipLeftAndRight(leftandright);
			}
		}

		public static void nes_setdispbackground(bool show)
		{
			Global.Config.NESDispBackground = show;
			CoreFileProvider.SyncCoreCommInputSignals();
		}

		public static void nes_setdispsprites(bool show)
		{
			Global.Config.NESDispSprites = show;
			CoreFileProvider.SyncCoreCommInputSignals();
		}

		public static void nes_setscanlines(object top, object bottom, bool pal = false)
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

			if (pal)
			{
				Global.Config.PAL_NESTopLine = first;
				Global.Config.PAL_NESBottomLine = last;
			}
			else
			{
				Global.Config.NTSC_NESTopLine = first;
				Global.Config.NTSC_NESBottomLine = last;
			}

			if (Global.Emulator is NES)
			{
				if (pal)
				{
					(Global.Emulator as NES).PAL_FirstDrawLine = first;
					(Global.Emulator as NES).PAL_LastDrawLine = last;
				}
				else
				{
					(Global.Emulator as NES).NTSC_FirstDrawLine = first;
					(Global.Emulator as NES).NTSC_LastDrawLine = last;
				}
			}
		}
	}
}
