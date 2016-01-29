using System;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Generic;
using Community.CsharpSqlite.SQLiteClient;
using System.IO;


namespace BizHawk.Client.DBMan
{
	internal static class Program
	{
		static Program()
		{
#if WINDOWS
			//http://www.codeproject.com/Articles/310675/AppDomain-AssemblyResolve-Event-Tips
			// this will look in subdirectory "dll" to load pinvoked stuff
			string dllDir = System.IO.Path.Combine(GetExeDirectoryAbsolute(), "dll");
			SetDllDirectory(dllDir);

			//but before we even try doing that, whack the MOTW from everything in that directory (thats a dll)
			//otherwise, some people will have crashes at boot-up due to .net security disliking MOTW.
			//some people are getting MOTW through a combination of browser used to download bizhawk, and program used to dearchive it
			WhackAllMOTW(dllDir);

			//in case assembly resolution fails, such as if we moved them into the dll subdiretory, this event handler can reroute to them
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
#endif
		}

		public static string GetExeDirectoryAbsolute()
		{
			var uri = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
			string module = uri.LocalPath + System.Web.HttpUtility.UrlDecode(uri.Fragment);
			return Path.GetDirectoryName(module);
		}

		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			lock (AppDomain.CurrentDomain)
			{
				var asms = AppDomain.CurrentDomain.GetAssemblies();
				foreach (var asm in asms)
					if (asm.FullName == args.Name)
						return asm;

				//load missing assemblies by trying to find them in the dll directory
				string dllname = new AssemblyName(args.Name).Name + ".dll";
				string directory = Path.Combine(GetExeDirectoryAbsolute(), "dll");
				string fname = Path.Combine(directory, dllname);
				if (!File.Exists(fname)) return null;
				//it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unamanged) assemblies can't load
				return Assembly.LoadFile(fname);
			}
		}

		//declared here instead of a more usual place to avoid dependencies on the more usual place
#if WINDOWS
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetDllDirectory(string lpPathName);

		[DllImport("kernel32.dll", EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)]string lpFileName);
		static void RemoveMOTW(string path)
		{
			DeleteFileW(path + ":Zone.Identifier");
		}

		static void WhackAllMOTW(string dllDir)
		{
			var todo = new Queue<DirectoryInfo>(new[] { new DirectoryInfo(dllDir) });
			while (todo.Count > 0)
			{
				var di = todo.Dequeue();
				foreach (var disub in di.GetDirectories()) todo.Enqueue(disub);
				foreach (var fi in di.GetFiles("*.dll"))
					RemoveMOTW(fi.FullName);
				foreach (var fi in di.GetFiles("*.exe"))
					RemoveMOTW(fi.FullName);
			}

		}
#endif

		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length > 0 && args[0] == "--dischash")
			{
				new DiscHash().Run(args.Skip(1).ToArray());
				return;
			}
			if (args.Length > 0 && args[0] == "--psxdb")
			{
				new PsxDBJob().Run(args.Skip(1).ToArray());
				return;
			}
			//if (args.Length > 0 && args[0] == "--disccmp")
			//{
			//  new DiscCmp().Run(args.Skip(1).ToArray());
			//  return;
			//}
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
