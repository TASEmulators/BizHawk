using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using NLua;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("A library for performing SQLite operations.")]
	public sealed class SqlLuaLibrary : LuaLibraryBase
	{
		public SqlLuaLibrary(Lua lua)
			: base(lua) { }

		public SqlLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "SQL";

		SQLiteConnection _mDBConnection;

		[LuaMethodExample("local stSQLcre = SQL.createdatabase( \"eg_db\" );")]
		[LuaMethod("createdatabase", "Creates a SQLite Database. Name should end with .db")]
		public string CreateDatabase(string name)
		{
			try
			{
				SQLiteConnection.CreateFile(name);
				return "Database Created Successfully";
			}
			catch (SQLiteException sqlEx)
			{
				return sqlEx.Message;
			}
		}


		[LuaMethodExample("local stSQLope = SQL.opendatabase( \"eg_db\" );")]
		[LuaMethod("opendatabase", "Opens a SQLite database. Name should end with .db")]
		public string OpenDatabase(string name)
		{
			try
			{
				var connBuilder = new SQLiteConnectionStringBuilder
				{
					DataSource = name
					, Version = 3 // SQLite version 
					, JournalMode = SQLiteJournalModeEnum.Wal // Allows for reads and writes to happen at the same time
					, DefaultIsolationLevel = System.Data.IsolationLevel.ReadCommitted // This only helps make the database lock left. May be pointless now
					, SyncMode = SynchronizationModes.Off // This shortens the delay for do synchronous calls.
				};

				_mDBConnection = new SQLiteConnection(connBuilder.ToString());
				_mDBConnection.Open();
				_mDBConnection.Close();
				return "Database Opened Successfully";
			}
			catch (SQLiteException sqlEx)
			{
				return sqlEx.Message;
			}
		}

		[LuaMethodExample("local stSQLwri = SQL.writecommand( \"CREATE TABLE eg_tab ( eg_tab_id integer PRIMARY KEY, eg_tab_row_name text NOT NULL ); INSERT INTO eg_tab ( eg_tab_id, eg_tab_row_name ) VALUES ( 1, 'Example table row' );\" );")]
		[LuaMethod("writecommand", "Runs a SQLite write command which includes CREATE,INSERT, UPDATE. " +
			"Ex: create TABLE rewards (ID integer  PRIMARY KEY, action VARCHAR(20)) ")]
		public string WriteCommand(string query = "")
		{
			if (query == "")
			{
				return "query is empty";
			}
			try
			{
				_mDBConnection.Open();
				string sql = query;
				SQLiteCommand command = new SQLiteCommand(sql, _mDBConnection);
				command.ExecuteNonQuery();
				_mDBConnection.Close();

				return "Command ran successfully";

			}
			catch (NullReferenceException)
			{
				return "Database not open.";
			}
			catch (SQLiteException sqlEx)
			{
				_mDBConnection.Close();
				return sqlEx.Message;
			}
		}

		[LuaMethodExample("local obSQLrea = SQL.readcommand( \"SELECT * FROM eg_tab WHERE eg_tab_id = 1;\" );")]
		[LuaMethod("readcommand", "Run a SQLite read command which includes Select. Returns all rows into a LuaTable." +
			"Ex: select * from rewards")]
		public dynamic ReadCommand(string query = "")
		{
			if (query == "")
			{
				return "query is empty";
			}
			try
			{
				var table = Lua.NewTable();
				_mDBConnection.Open();
				string sql = $"PRAGMA read_uncommitted =1;{query}";
				var command = new SQLiteCommand(sql, _mDBConnection);
				SQLiteDataReader reader = command.ExecuteReader();
				bool rows = reader.HasRows;
				long rowCount = 0;
				var columns = new List<string>();
				for (int i = 0; i < reader.FieldCount; ++i) //Add all column names into list
				{
					columns.Add(reader.GetName(i));
				}
				while (reader.Read())
				{
					for (int i = 0; i < reader.FieldCount; ++i)
					{
						table[$"{columns[i]} {rowCount}"] = reader.GetValue(i);
					}
					rowCount += 1;
				}
				reader.Close();
				_mDBConnection.Close();
				if (rows == false)
				{
					return "No rows found";
				}

				return table;

			}
			catch (NullReferenceException)
			{
				return "Database not opened.";
			}
			catch (SQLiteException sqlEx)
			{
				_mDBConnection.Close();
				return sqlEx.Message;
			}
		}
	}
}
