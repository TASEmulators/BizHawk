using System;
using NLua;

using BizHawk.Emulation.Common;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace BizHawk.Client.Common
{
	public sealed class GameInfoLuaLibrary : LuaLibraryBase
	{
		[OptionalService]
		private IBoardInfo BoardInfo { get; set; }

		public GameInfoLuaLibrary(Lua lua)
			: base(lua) { }

		public GameInfoLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "gameinfo";

		[LuaMethodExample("local stgamget = gameinfo.getromname( );")]
		[LuaMethod("getromname", "returns the name of the currently loaded rom, if a rom is loaded")]
		public string GetRomName()
		{
			return Global.Game?.Name ?? "";
		}

		[LuaMethodExample("local stgamget = gameinfo.getromhash( );")]
		[LuaMethod("getromhash", "returns the hash of the currently loaded rom, if a rom is loaded")]
		public string GetRomHash()
		{
			return Global.Game?.Hash ?? "";
		}

		[LuaMethodExample("if ( gameinfo.indatabase( ) ) then\r\n\tconsole.log( \"returns whether or not the currently loaded rom is in the game database\" );\r\nend;")]
		[LuaMethod("indatabase", "returns whether or not the currently loaded rom is in the game database")]
		public bool InDatabase()
		{
			if (Global.Game != null)
			{
				return !Global.Game.NotInDatabase;
			}

			return false;
		}

		[LuaMethodExample("local stgamget = gameinfo.getstatus( );")]
		[LuaMethod("getstatus", "returns the game database status of the currently loaded rom. Statuses are for example: GoodDump, BadDump, Hack, Unknown, NotInDatabase")]
		public string GetStatus()
		{
			return Global.Game?.Status.ToString();
		}

		[LuaMethodExample("if ( gameinfo.isstatusbad( ) ) then\r\n\tconsole.log( \"returns the currently loaded rom's game database status is considered 'bad'\" );\r\nend;")]
		[LuaMethod("isstatusbad", "returns the currently loaded rom's game database status is considered 'bad'")]
		public bool IsStatusBad()
		{
			return Global.Game?.IsRomStatusBad() ?? true;
		}

		[LuaMethodExample("local stgamget = gameinfo.getboardtype( );")]
		[LuaMethod("getboardtype", "returns identifying information about the 'mapper' or similar capability used for this game.  empty if no such useful distinction can be drawn")]
		public string GetBoardType()
		{
			return BoardInfo?.BoardName ?? "";
		}

		[LuaMethodExample("local nlgamget = gameinfo.getoptions( );")]
		[LuaMethod("getoptions", "returns the game options for the currently loaded rom. Options vary per platform")]
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
