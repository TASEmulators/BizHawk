using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("A library for performing SQLite operations.")]
	public sealed class SQLiteLuaLibrary : LuaLibraryBase
	{
		public SQLiteLuaLibrary(IPlatformLuaLibEnv luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "SQL";

		[LuaMethodExample("local stSQLcre = SQL.createdatabase( \"eg_db\" );")]
		[LuaMethod("createdatabase", "Creates a SQLite Database. Name should end with .db")]
		[return: LuaArbitraryStringParam]
		public string CreateDatabase([LuaArbitraryStringParam] string name)
			=> UnFixString(APIs.SQLite.CreateDatabase(FixString(name)));

		[LuaMethodExample("local stSQLope = SQL.opendatabase( \"eg_db\" );")]
		[LuaMethod("opendatabase", "Opens a SQLite database. Name should end with .db")]
		[return: LuaArbitraryStringParam]
		public string OpenDatabase([LuaArbitraryStringParam] string name)
			=> UnFixString(APIs.SQLite.OpenDatabase(FixString(name)));

		[LuaMethodExample("local stSQLwri = SQL.writecommand( \"CREATE TABLE eg_tab ( eg_tab_id integer PRIMARY KEY, eg_tab_row_name text NOT NULL ); INSERT INTO eg_tab ( eg_tab_id, eg_tab_row_name ) VALUES ( 1, 'Example table row' );\" );")]
		[LuaMethod("writecommand", "Runs a SQLite write command which includes CREATE,INSERT, UPDATE. " +
			"Ex: create TABLE rewards (ID integer  PRIMARY KEY, action VARCHAR(20)) ")]
		[return: LuaArbitraryStringParam]
		public string WriteCommand([LuaArbitraryStringParam] string query = "")
			=> UnFixString(APIs.SQLite.WriteCommand(FixString(query)));

		[LuaMethodExample("local obSQLrea = SQL.readcommand( \"SELECT * FROM eg_tab WHERE eg_tab_id = 1;\" );")]
		[LuaMethod("readcommand", "Run a SQLite read command which includes Select. Returns all rows into a LuaTable." +
			"Ex: select * from rewards")]
		[return: LuaArbitraryStringParam]
		public object ReadCommand([LuaArbitraryStringParam] string query = "")
		{
			var result = APIs.SQLite.ReadCommand(FixString(query));
			return result switch
			{
				Dictionary<string, object> dict => _th.DictToTable(dict.ToDictionary(static kvp => UnFixString(kvp.Key), static kvp => kvp.Value)),
				string s => UnFixString(s),
				_ => result
			};
		}
	}
}
