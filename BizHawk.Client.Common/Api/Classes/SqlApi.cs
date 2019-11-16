using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace BizHawk.Client.Common
{
	public sealed class SqlApi : ISql
	{
		SQLiteConnection _dbConnection;

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

		public string OpenDatabase(string name)
		{
			try
			{
				var connBuilder = new SQLiteConnectionStringBuilder
				{
					DataSource = name,
					Version = 3,
					JournalMode = SQLiteJournalModeEnum.Wal,  // Allows for reads and writes to happen at the same time
					DefaultIsolationLevel = System.Data.IsolationLevel.ReadCommitted, // This only helps make the database lock left. May be pointless now
					SyncMode = SynchronizationModes.Off // This shortens the delay for do synchronous calls.
				};

				_dbConnection = new SQLiteConnection(connBuilder.ToString());
				_dbConnection.Open();
				_dbConnection.Close();
				return "Database Opened Successfully";
			}
			catch (SQLiteException sqlEx)
			{
				return sqlEx.Message;
			}
		}

		public string WriteCommand(string query = "")
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				return "query is empty";
			}

			try
			{
				_dbConnection.Open();
				var command = new SQLiteCommand(query, _dbConnection);
				command.ExecuteNonQuery();
				_dbConnection.Close();

				return "Command ran successfully";
			}
			catch (NullReferenceException)
			{
				return "Database not open.";
			}
			catch (SQLiteException sqlEx)
			{
				_dbConnection.Close();
				return sqlEx.Message;
			}
		}

		public dynamic ReadCommand(string query = "")
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				return "query is empty";
			}

			try
			{
				var table = new Dictionary<string, object>();
				_dbConnection.Open();
				string sql = $"PRAGMA read_uncommitted =1;{query}";
				using var command = new SQLiteCommand(sql, _dbConnection);
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
				_dbConnection.Close();
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
				_dbConnection.Close();
				return sqlEx.Message;
			}
		}
	}
}
