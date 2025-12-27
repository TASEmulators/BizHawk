using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.DiscSystem;

using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

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
			// http://www.codeproject.com/Articles/310675/AppDomain-AssemblyResolve-Event-Tips
			// in case assembly resolution fails, such as if we moved them into the dll subdirectory, this event handler can reroute to them
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// Windows needs extra considerations for the dll directory
				// we can skip all this on non-Windows platforms
				return;
			}

			try
			{
				// before we load anything from the dll dir, whack the MOTW from everything in that directory (that's a dll)
				// otherwise, some people will have crashes at boot-up due to .net security disliking MOTW.
				// some people are getting MOTW through a combination of browser used to download bizhawk, and program used to dearchive it
				static void RemoveMOTW(string path) => DeleteFileW($"{path}:Zone.Identifier");
				var dllDir = Path.Combine(AppContext.BaseDirectory, "dll");
				var todo = new Queue<DirectoryInfo>([ new DirectoryInfo(dllDir) ]);
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
				WmImports.ChangeWindowMessageFilter(WM_DROPFILES, CHANGE_WINDOW_MESSAGE_FILTER_FLAGS.MSGFLT_ADD);
				WmImports.ChangeWindowMessageFilter(WM_COPYDATA, CHANGE_WINDOW_MESSAGE_FILTER_FLAGS.MSGFLT_ADD);
				WmImports.ChangeWindowMessageFilter(WM_COPYGLOBALDATA, CHANGE_WINDOW_MESSAGE_FILTER_FLAGS.MSGFLT_ADD);

				// this will look in subdirectory "dll" to load pinvoked stuff
				var dllDir = Path.Combine(AppContext.BaseDirectory, "dll");

				// windows prohibits a semicolon for SetDllDirectoryW, although such paths are fully valid otherwise
				// presumingly windows internally has ; used as a path separator, like with PATH
				// or perhaps this is just some legacy junk windows keeps around for backwards compatibility reasons
				// we can possibly workaround this by using the "short path name" rather (but this isn't guaranteed to exist)
				const string SEMICOLON_IN_DIR_MSG =
					"DiscoHawk requires no semicolons within its base directory! DiscoHawk will now close.";

				if (dllDir.ContainsOrdinal(';'))
				{
					var dllShortPathLen = Win32Imports.GetShortPathNameW(dllDir);
					if (dllShortPathLen == 0)
					{
						MessageBox.Show(SEMICOLON_IN_DIR_MSG);
						return;
					}

					var dllShortPathBuffer = new char[dllShortPathLen];
					dllShortPathLen = Win32Imports.GetShortPathNameW(dllDir, dllShortPathBuffer);
					if (dllShortPathLen == 0)
					{
						MessageBox.Show(SEMICOLON_IN_DIR_MSG);
						return;
					}

					dllDir = dllShortPathBuffer.AsSpan(start: 0, length: (int) dllShortPathLen).ToString();
					if (dllDir.ContainsOrdinal(';'))
					{
						MessageBox.Show(SEMICOLON_IN_DIR_MSG);
						return;
					}
				}

				if (!Win32Imports.SetDllDirectoryW(dllDir))
				{
					MessageBox.Show(
						$"SetDllDirectoryW failed with error code {Marshal.GetLastWin32Error()}, this is fatal. DiscoHawk will now close.");
					return;
				}
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
