using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Client.DiscoHawk
{
	internal static class Program
	{
		// Declared here instead of a more usual place to avoid dependencies on the more usual place

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetDllDirectoryW(string lpPathName);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteFileW(string lpFileName);

		static Program()
		{
			// in case assembly resolution fails, such as if we moved them into the dll subdirectory, this event handler can reroute to them
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// Windows needs extra considerations for the dll directory
				// we can skip all this on non-Windows platforms
				return;
			}

			// http://www.codeproject.com/Articles/310675/AppDomain-AssemblyResolve-Event-Tips
			// this will look in subdirectory "dll" to load pinvoked stuff
			var dllDir = Path.Combine(AppContext.BaseDirectory, "dll");
			SetDllDirectoryW(dllDir);

			try
			{
				// but before we even try doing that, whack the MOTW from everything in that directory (that's a dll)
				// otherwise, some people will have crashes at boot-up due to .net security disliking MOTW.
				// some people are getting MOTW through a combination of browser used to download bizhawk, and program used to dearchive it
				static void RemoveMOTW(string path) => DeleteFileW($"{path}:Zone.Identifier");
				var todo = new Queue<DirectoryInfo>(new[] { new DirectoryInfo(dllDir) });
				while (todo.Count != 0)
				{
					var di = todo.Dequeue();
					foreach (var disub in di.GetDirectories()) todo.Enqueue(disub);
					foreach (var fi in di.GetFiles("*.dll")) RemoveMOTW(fi.FullName);
					foreach (var fi in di.GetFiles("*.exe")) RemoveMOTW(fi.FullName);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"MotW remover failed: {e}");
			}
		}

		[STAThread]
		private static void Main(string[] args)
		{
			if (!OSTailoredCode.IsUnixHost)
			{
				// MICROSOFT BROKE DRAG AND DROP IN WINDOWS 7. IT DOESN'T WORK ANYMORE
				// WELL, OBVIOUSLY IT DOES SOMETIMES. I DON'T REMEMBER THE DETAILS OR WHY WE HAD TO DO THIS SHIT
				const uint WM_DROPFILES = 0x0233;
				const uint WM_COPYDATA = 0x004A;
				const uint WM_COPYGLOBALDATA = 0x0049;
				WmImports.ChangeWindowMessageFilter(WM_DROPFILES, WmImports.ChangeWindowMessageFilterFlags.Add);
				WmImports.ChangeWindowMessageFilter(WM_COPYDATA, WmImports.ChangeWindowMessageFilterFlags.Add);
				WmImports.ChangeWindowMessageFilter(WM_COPYGLOBALDATA, WmImports.ChangeWindowMessageFilterFlags.Add);
			}

			// Do something for visuals, I guess
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			if (args.Length == 0)
			{
				using var dialog = new MainDiscoForm();
				dialog.ShowDialog();
			}
			else
			{
				DiscoHawkLogic.RunWithArgs(
					args,
					results =>
					{
						using var cr = new ComparisonResults { textBox1 = { Text = results } };
						cr.ShowDialog();
					});
			}
		}

		/// <remarks>http://www.codeproject.com/Articles/310675/AppDomain-AssemblyResolve-Event-Tips</remarks>
		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var requested = args.Name;

			lock (AppDomain.CurrentDomain)
			{
				var firstAsm = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), asm => asm.FullName == requested);
				if (firstAsm != null)
				{
					return firstAsm;
				}

				// load missing assemblies by trying to find them in the dll directory
				var dllname = $"{new AssemblyName(requested).Name}.dll";
				var directory = Path.Combine(AppContext.BaseDirectory, "dll");
				var fname = Path.Combine(directory, dllname);
				// it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unmanaged) assemblies can't load
				return File.Exists(fname) ? Assembly.LoadFile(fname) : null;
			}
		}
	}
}
