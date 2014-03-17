using System;
using System.Windows.Forms;
using Community.CsharpSqlite.SQLiteClient;

namespace BizHawk.Client.DBMan
{
	internal static class Program
	{
		[STAThread]
		static void Main()
		{
			try
			{
				InitDB();
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				Application.Run(new DBMan_MainForm());
			}
			catch (Exception e)
			{
				MessageBox.Show(e.ToString());
			}
			finally 
			{
				if (DB.Con != null) DB.Con.Dispose();
			}
		}

		static void InitDB()
		{
			DB.Con = new SqliteConnection();
			DB.Con.ConnectionString = @"Version=3,uri=file://gamedb/game.db";
			DB.Con.Open();
		}
	}
}
