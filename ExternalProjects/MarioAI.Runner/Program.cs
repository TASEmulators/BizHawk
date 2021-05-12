using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace MarioAI.Runner
{
    class Program
	{
		static Program()
		{
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
		}

		public Program()
		{
			var dllDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
			SetDllDirectory(dllDir);
		}

		static void RemoveMOTW(string path)
		{
			DeleteFileW($"{path}:Zone.Identifier");
		}

		static void Main(string[] args)
        {
			var dllDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll"); // Path.Combine(), "dll");
			
			SetDllDirectory(dllDir);


			//var todo = new Queue<DirectoryInfo>(new[] { new DirectoryInfo(dllDir) });
			//while (todo.Count != 0)
			//{
			//	var di = todo.Dequeue();
			//	foreach (var disub in di.GetDirectories()) todo.Enqueue(disub);
			//	foreach (var fi in di.GetFiles("*.dll")) RemoveMOTW(fi.FullName);
			//	foreach (var fi in di.GetFiles("*.exe")) RemoveMOTW(fi.FullName);
			//}

			N64Settings n64Settings = new N64Settings();

			N64SyncSettings n64SyncSettings = new N64SyncSettings()
			{
				Core = N64SyncSettings.CoreType.Interpret
			};

			GameInfo gameInfo = new GameInfo() { Region = "US", Status = RomStatus.GoodDump } ;

			var file = File.ReadAllBytes("C:\\temp\\mario.n64");

			N64 test = new N64(gameInfo, file, n64Settings, n64SyncSettings);
        }

		//declared here instead of a more usual place to avoid dependencies on the more usual place
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetDllDirectory(string lpPathName);

		[DllImport("kernel32.dll", EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		private static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var requested = args.Name;

			lock (AppDomain.CurrentDomain)
			{
				var firstAsm = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), asm => asm.FullName == requested);
				if (firstAsm != null) return firstAsm;

				//load missing assemblies by trying to find them in the dll directory
				var dllname = $"{new AssemblyName(requested).Name}.dll";
				var directory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
				var simpleName = new AssemblyName(requested).Name;
				if (simpleName == "NLua" || simpleName == "KopiLua") directory = Path.Combine(directory, "nlua");
				var fname = Path.Combine(directory, dllname);
				//it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unmanaged) assemblies can't load
				return File.Exists(fname) ? Assembly.LoadFile(fname) : null;
			}
		}
	}
}
