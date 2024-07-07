using System.Collections.Generic;
using System.IO;

using Microsoft.Data.Sqlite;

namespace BizHawk.Client.Common
{
	public sealed class SQLiteApi : ISQLiteApi
	{
		static SQLiteApi()
		{
			SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
		}

		private SqliteConnection _dbConnection;

		public string CreateDatabase(string name)
		{
			try
			{
				File.Create(name).Dispose();
			}
			catch (Exception ex)
			{
				return ex.Message;
			}

			return "Database Created Successfully";
		}

		public string OpenDatabase(string name)
		{
			try
			{
				_dbConnection?.Dispose();
				_dbConnection = new(new SqliteConnectionStringBuilder { DataSource = name }.ToString());
				_dbConnection.Open();
				using var initCmds = new SqliteCommand(null, _dbConnection);
				// Allows for reads and writes to happen at the same time
				initCmds.CommandText = "PRAGMA journal_mode = 'wal'";
				initCmds.ExecuteNonQuery();
				// This shortens the delay for do synchronous calls
				initCmds.CommandText = "PRAGMA synchronous = 'off'";
				initCmds.ExecuteNonQuery();
			}
			catch (SqliteException sqlEx)
			{
				return sqlEx.Message;
			}
			_dbConnection?.Close();
			return "Database Opened Successfully";
		}

		public string WriteCommand(string query)
		{
			if (string.IsNullOrWhiteSpace(query)) return "query is empty";
			if (_dbConnection == null) return "Database not open.";
			string result;
			try
			{
				_dbConnection.Open();
				using var cmd = new SqliteCommand(query, _dbConnection);
				cmd.ExecuteNonQuery();
				result = "Command ran successfully";
			}
			catch (SqliteException sqlEx)
			{
				result = sqlEx.Message;
			}
			_dbConnection.Close();
			return result;
		}

		public object ReadCommand(string query)
		{
			if (string.IsNullOrWhiteSpace(query)) return "query is empty";
			if (_dbConnection == null) return "Database not open.";
			object result;
			try
			{
				_dbConnection.Open();
				using var command = new SqliteCommand($"PRAGMA read_uncommitted =1;{query}", _dbConnection);
				using var reader = command.ExecuteReader();
				if (reader.HasRows)
				{
					var columns = new string[reader.FieldCount];
					for (int i = 0, l = reader.FieldCount; i < l; i++) columns[i] = reader.GetName(i);
					long rowCount = 0;
					var table = new Dictionary<string, object>();
					while (reader.Read())
					{
						for (int i = 0, l = reader.FieldCount; i < l; i++) table[$"{columns[i]} {rowCount}"] = reader.GetValue(i);
						rowCount++;
					}
					reader.Close();
					result = table;
				}
				else
				{
					result = "No rows found";
				}
			}
			catch (SqliteException sqlEx)
			{
				result = sqlEx.Message;
			}
			_dbConnection.Close();
			return result;
		}
	}
}
