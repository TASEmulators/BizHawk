using System;
using LuaInterface;

namespace BizHawk.Client.Common
{
	public class GameInfoLuaLibrary : LuaLibraryBase
	{
		public GameInfoLuaLibrary(Lua lua)
		{
			_lua = lua;
		}

		public override string Name { get { return "gameinfo"; } }

		private readonly Lua _lua;

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
			"getindatabase",
			"returns whether or not the currently loaded rom is in the game database"
		)]
		public bool GetInDatabase()
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
			"getisstatusbad",
			"returns the currently loaded rom's game database status is considered 'bad'"
		)]
		public bool GetIsStatusBad()
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
			return Global.Emulator.BoardName;
		}

		[LuaMethodAttributes(
			"getoptions",
			"returns the game options for the currently loaded rom. Options vary per platform"
		)]
		public LuaTable GetOptions()
		{
			var options = _lua.NewTable();

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
