using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace BizHawk.Client.Common
{
	public sealed class SQLiteApi : ISQLite
	{
		private SQLiteConnection _dbConnection;

		public string CreateDatabase(string name)
		{
			try
			{
				SQLiteConnection.CreateFile(name);
			}
			catch (SQLiteException sqlEx)
			{
				return sqlEx.Message;
			}
			return "Database Created Successfully";
		}

		public string ExecCommand(string query)
		{
			if (string.IsNullOrWhiteSpace(query)) return "query is empty";
			if (_dbConnection == null) return "Database not open.";
			string result;
			try
			{
				_dbConnection.Open();
				new SQLiteCommand(query, _dbConnection).ExecuteNonQuery();
				result = "Command ran successfully";
			}
			catch (SQLiteException sqlEx)
			{
				result = sqlEx.Message;
			}
			_dbConnection.Close();
			return result;
		}

		public dynamic ExecCommandWithResult(string query)
		{
			if (string.IsNullOrWhiteSpace(query)) return "query is empty";
			if (_dbConnection == null) return "Database not open.";
			dynamic result;
			try
			{
				_dbConnection.Open();
				using var command = new SQLiteCommand($"PRAGMA read_uncommitted =1;{query}", _dbConnection);
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
			catch (SQLiteException sqlEx)
			{
				result = sqlEx.Message;
			}
			_dbConnection.Close();
			return result;
		}

		public string OpenDatabase(string name)
		{
			try
			{
				_dbConnection = new SQLiteConnection(
					new SQLiteConnectionStringBuilder {
						DataSource = name,
						Version = 3,
						JournalMode = SQLiteJournalModeEnum.Wal, // Allows for reads and writes to happen at the same time
						DefaultIsolationLevel = IsolationLevel.ReadCommitted, // This only helps make the database lock left. May be pointless now
						SyncMode = SynchronizationModes.Off // This shortens the delay for do synchronous calls.
					}.ToString()
				);
				_dbConnection.Open();
			}
			catch (SQLiteException sqlEx)
			{
				return sqlEx.Message;
			}
			_dbConnection?.Close();
			return "Database Opened Successfully";
		}
	}
}
