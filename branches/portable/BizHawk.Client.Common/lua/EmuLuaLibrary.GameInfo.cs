using System;
using LuaInterface;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed class GameInfoLuaLibrary : LuaLibraryBase
	{
		[RequiredService]
		public IEmulator Emulator { get; set; }

		public GameInfoLuaLibrary(Lua lua)
			: base(lua) { }

		public GameInfoLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name { get { return "gameinfo"; } }

		[LuaMethodAttributes(
			"getromname",
			"returns the path of the currently loaded rom, if a rom is loaded"
		)]
		public string GetRomName()
		{
			if (Global.Game != null)
			{
				return Global.Game.Name ?? string.Empty;
			}

			return string.Empty;
		}

		[LuaMethodAttributes(
			"getromhash",
			"returns the hash of the currently loaded rom, if a rom is loaded"
		)]
		public string GetRomHash()
		{
			if (Global.Game != null)
			{
				return Global.Game.Hash ?? string.Empty;
			}

			return string.Empty;
		}

		[LuaMethodAttributes(
			"indatabase",
			"returns whether or not the currently loaded rom is in the game database"
		)]
		public bool InDatabase()
		{
			if (Global.Game != null)
			{
				return !Global.Game.NotInDatabase;
			}

			return false;
		}

		[LuaMethodAttributes(
			"getstatus",
			"returns the game database status of the currently loaded rom. Statuses are for example: GoodDump, BadDump, Hack, Unknown, NotInDatabase"
		)]
		public string GetStatus()
		{
			if (Global.Game != null)
			{
				return Global.Game.Status.ToString() ?? string.Empty;
			}

			return string.Empty;
		}

		[LuaMethodAttributes(
			"isstatusbad",
			"returns the currently loaded rom's game database status is considered 'bad'"
		)]
		public bool IsStatusBad()
		{
			if (Global.Game != null)
			{
				return Global.Game.IsRomStatusBad();
			}

			return true;
		}

		[LuaMethodAttributes(
			"getboardtype",
			"returns identifying information about the 'mapper' or similar capability used for this game.  empty if no such useful distinction can be drawn"
		)]
		public string GetBoardType()
		{
			return Emulator.BoardName ?? "";
		}

		[LuaMethodAttributes(
			"getoptions",
			"returns the game options for the currently loaded rom. Options vary per platform"
		)]
		public LuaTable GetOptions()
		{
			var options = Lua.NewTable();

			if (Global.Game != null)
			{
				foreach (var option in Global.Game.GetOptionsDict())
				{
					options[option.Key] = option.Value;
				}
			}

			return options;
		}
	}
}
