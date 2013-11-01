using System.Linq;
using BizHawk.Client.Common;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
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
					"setscanlines",
				};
			}
		}

		public static void nes_addgamegenie(string code)
		{
			if (Global.Emulator is NES)
			{
				NESGameGenie gg = new NESGameGenie();
				gg.DecodeGameGenieCode(code);
				if (gg.Address.HasValue && gg.Value.HasValue)
				{
					Watch watch = Watch.GenerateWatch(
						Global.Emulator.MemoryDomains[1],
						gg.Address.Value,
						Watch.WatchSize.Byte,
						Watch.DisplayType.Hex,
						code,
						false
					);

					Global.CheatList.Add(new Cheat(
						watch,
						gg.Value.Value,
						gg.Compare
					));
				}

				ToolHelpers.UpdateCheatRelatedTools();
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

		public static void nes_removegamegenie(string code)
		{
			if (Global.Emulator is NES)
			{
				NESGameGenie gg = new NESGameGenie();
				gg.DecodeGameGenieCode(code);
				if (gg.Address.HasValue && gg.Value.HasValue)
				{
					var cheats = Global.CheatList.Where(x => x.Address == gg.Address);
					Global.CheatList.RemoveRange(cheats);
				}

				ToolHelpers.UpdateCheatRelatedTools();
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
