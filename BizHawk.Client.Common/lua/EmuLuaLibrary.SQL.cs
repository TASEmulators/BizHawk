using System;
using System.Collections;
using System.ComponentModel;
using System.Data.SQLite;
using NLua;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	[Description("A library for performing SQLite operations.")]
	public sealed class SQLLuaLibrary : LuaLibraryBase
	{
		public SQLLuaLibrary(Lua lua)
			: base(lua) { }

		public SQLLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "SQL";

		SQLiteConnection m_dbConnection;
		string connectionString;

		[LuaMethod("createdatabase","Creates a SQLite Database. Name should end with .db")]
		public string CreateDatabase(string name)
		{
			try
			{
				SQLiteConnection.CreateFile(name);
				return "Database Created Successfully";
			}
			catch (SQLiteException sqlEX)
			{
				return sqlEX.Message;
			}

		}


		[LuaMethod("opendatabase", "Opens a SQLite database. Name should end with .db")]
		public string OpenDatabase(string name)
		{
			try
			{
				SQLiteConnectionStringBuilder connBuilder = new SQLiteConnectionStringBuilder();
				connBuilder.DataSource = name;
				connBuilder.Version = 3; //SQLite version 
				connBuilder.JournalMode = SQLiteJournalModeEnum.Wal;  //Allows for reads and writes to happen at the same time
				connBuilder.DefaultIsolationLevel = System.Data.IsolationLevel.ReadCommitted;  //This only helps make the database lock left. May be pointless now
				connBuilder.SyncMode = SynchronizationModes.Off; //This shortens the delay for do synchronous calls.
				m_dbConnection = new SQLiteConnection(connBuilder.ToString());
				connectionString = connBuilder.ToString();
				m_dbConnection.Open();
				m_dbConnection.Close();
				return "Database Opened Successfully";
			}
			catch(SQLiteException sqlEX)
			{
				return sqlEX.Message;
			}
		}

		[LuaMethod("writecommand", "Runs a SQLite write command which includes CREATE,INSERT, UPDATE. " +
			"Ex: create TABLE rewards (ID integer  PRIMARY KEY, action VARCHAR(20)) ")]
		public string WriteCommand(string query="")
		{
			if (query == "")
			{
				return "query is empty";
			}
			try
			{
				m_dbConnection.Open();
				string sql = query;
				SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
				command.ExecuteNonQuery();
				m_dbConnection.Close();

				return "Command ran successfully";

			}
			catch (NullReferenceException nullEX)
			{
				return "Database not open.";
			}
			catch(SQLiteException sqlEX)
			{
				m_dbConnection.Close();
				return sqlEX.Message;
			}
		}

		[LuaMethod("readcommand", "Run a SQLite read command which includes Select. Returns all rows into a LuaTable." +
			"Ex: select * from rewards")]
		public dynamic ReadCommand(string query="")
		{
			if (query=="")
			{
				return "query is empty";
			}
			try
			{
				var table = Lua.NewTable();
				m_dbConnection.Open();
				string sql = "PRAGMA read_uncommitted =1;"+query;
				SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
				SQLiteDataReader reader = command.ExecuteReader();
				bool rows=reader.HasRows;
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
						table[columns[i]+" "+rowCount.ToString()] = reader.GetValue(i);
					}
					rowCount += 1;
				}
				reader.Close();
				m_dbConnection.Close();
				if (rows==false)
				{
					return "No rows found";
				}

				return table;

			}
			catch (NullReferenceException)
			{
				return "Database not opened.";
			}
			catch (SQLiteException sqlEX)
			{
				m_dbConnection.Close();
				return sqlEX.Message;
			}
		}

	}
}
