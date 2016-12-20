using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
#if WINDOWS
using SlimDX.DirectSound;
using Microsoft.VisualBasic.ApplicationServices;
#endif

using BizHawk.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.MultiHawk
{
	static class Program
	{
		static Program()
		{
			//http://www.codeproject.com/Articles/310675/AppDomain-AssemblyResolve-Event-Tips
#if WINDOWS
			// this will look in subdirectory "dll" to load pinvoked stuff
			string dllDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
			SetDllDirectory(dllDir);

			//but before we even try doing that, whack the MOTW from everything in that directory (thats a dll)
			//otherwise, some people will have crashes at boot-up due to .net security disliking MOTW.
			//some people are getting MOTW through a combination of browser used to download bizhawk, and program used to dearchive it
			WhackAllMOTW(dllDir);

			//in case assembly resolution fails, such as if we moved them into the dll subdiretory, this event handler can reroute to them
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
#endif
		}

		
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			SubMain(args);
		}


		//NoInlining should keep this code from getting jammed into Main() which would create dependencies on types which havent been setup by the resolver yet... or something like that
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		static void SubMain(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			string iniPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.ini");
			Global.Config = ConfigService.Load<Config>(iniPath);
			Global.Config.ResolveDefaults();
			HawkFile.ArchiveHandlerFactory = new SevenZipSharpArchiveHandler();

			//super hacky! this needs to be done first. still not worth the trouble to make this system fully proper
			for (int i = 0; i < args.Length; i++)
			{
				var arg = args[i].ToLower();
				if (arg.StartsWith("--gdi"))
				{
					Global.Config.DispMethod = Config.EDispMethod.GdiPlus;
				}
			}


			//WHY do we have to do this? some intel graphics drivers (ig7icd64.dll 10.18.10.3304 on an unknown chip on win8.1) are calling SetDllDirectory() for the process, which ruins stuff.
			//The relevant initialization happened just before in "create IGL context".
			//It isn't clear whether we need the earlier SetDllDirectory(), but I think we do.
			//note: this is pasted instead of being put in a static method due to this initialization code being sensitive to things like that, and not wanting to cause it to break
			//pasting should be safe (not affecting the jit order of things)
			string dllDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
			SetDllDirectory(dllDir);

			try
			{
					using (var mf = new Mainform(args))
					{
						var title = mf.Text;
						mf.Show();
						mf.Text = title;
						try
						{
							mf.ProgramRunLoop();
						}
						catch (Exception e)
						{
#if WINDOWS
							if (Global.MovieSession.Movie.IsActive)
							{
								var result = MessageBox.Show(
									"EmuHawk has thrown a fatal exception and is about to close.\nA movie has been detected. Would you like to try to save?\n(Note: Depending on what caused this error, this may or may not succeed)",
									"Fatal error: " + e.GetType().Name,
									MessageBoxButtons.YesNo,
									MessageBoxIcon.Exclamation
									);
								if (result == DialogResult.Yes)
								{
									Global.MovieSession.Movie.Save();
								}
							}
#endif
							throw;
						}
					}
			}
			catch (Exception e)
			{
				string message = e.ToString();
				if (e.InnerException != null)
				{
					message += "\n\nInner Exception:\n\n" + e.InnerException;
				}

				message += "\n\nStackTrace:\n" + e.StackTrace;
				MessageBox.Show(message);
			}
#if WINDOWS
			finally
			{
				GamePad.CloseAll();
			}
#endif
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
				string directory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
				string fname = Path.Combine(directory, dllname);
				if (!File.Exists(fname)) return null;
				//it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unamanged) assemblies can't load
				return Assembly.LoadFile(fname);
			}
		}

		//declared here instead of a more usual place to avoid dependencies on the more usual place
#if WINDOWS
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern uint SetDllDirectory(string lpPathName);

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
	}
}
