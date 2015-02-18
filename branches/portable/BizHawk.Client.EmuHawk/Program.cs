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

namespace BizHawk.Client.EmuHawk
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

		[STAThread]
		static void Main(string[] args)
		{
			SubMain(args);
		}

		//NoInlining should keep this code from getting jammed into Main() which would create dependencies on types which havent been setup by the resolver yet... or something like that
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		static void SubMain(string[] args)
		{
			// this check has to be done VERY early.  i stepped through a debug build with wrong .dll versions purposely used,
			// and there was a TypeLoadException before the first line of SubMain was reached (some static ColorType init?)
			// zero 25-dec-2012 - only do for public builds. its annoying during development
			if (!VersionInfo.DeveloperBuild)
			{
				var thisversion = typeof(Program).Assembly.GetName().Version;
				var utilversion = Assembly.Load(new AssemblyName("Bizhawk.Client.Common")).GetName().Version;
				var emulversion = Assembly.Load(new AssemblyName("Bizhawk.Emulation.Cores")).GetName().Version;

				if (thisversion != utilversion || thisversion != emulversion)
				{
					MessageBox.Show("Conflicting revisions found!  Don't mix .dll versions!");
					return;
				}
			}

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

			//create IGL context. we do this whether or not the user has selected OpenGL, so that we can run opengl-based emulator cores
			GlobalWin.IGL_GL = new Bizware.BizwareGL.Drivers.OpenTK.IGL_TK();

			//setup the GL context manager, needed for coping with multiple opengl cores vs opengl display method
			GlobalWin.GLManager = new GLManager();
			GlobalWin.CR_GL = GlobalWin.GLManager.GetContextForIGL(GlobalWin.GL);

			//now create the "GL" context for the display method. we can reuse the IGL_TK context if opengl display method is chosen
REDO_DISPMETHOD:
			if (Global.Config.DispMethod == Config.EDispMethod.GdiPlus)
				GlobalWin.GL = new Bizware.BizwareGL.Drivers.GdiPlus.IGL_GdiPlus();
#if WINDOWS
			else if (Global.Config.DispMethod == Config.EDispMethod.SlimDX9)
			GlobalWin.GL = new Bizware.BizwareGL.Drivers.SlimDX.IGL_SlimDX9();
#endif
			else
			{
				GlobalWin.GL = GlobalWin.IGL_GL;
				//check the opengl version and dont even try to boot this crap up if its too old
				int version = GlobalWin.IGL_GL.Version;
				if (version < 200)
				{
					//fallback
					Global.Config.DispMethod = Config.EDispMethod.GdiPlus;
					goto REDO_DISPMETHOD;
				}
			}

			//WHY do we have to do this? some intel graphics drivers (ig7icd64.dll 10.18.10.3304 on an unknown chip on win8.1) are calling SetDllDirectory() for the process, which ruins stuff.
			//The relevant initialization happened just before in "create IGL context".
			//It isn't clear whether we need the earlier SetDllDirectory(), but I think we do.
			//note: this is pasted instead of being put in a static method due to this initialization code being sensitive to things like that, and not wanting to cause it to break
			//pasting should be safe (not affecting the jit order of things)
#if WINDOWS
			string dllDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
			SetDllDirectory(dllDir);
#endif

			try
			{
#if WINDOWS
				if (Global.Config.SingleInstanceMode)
				{
					try
					{
						new SingleInstanceController(args).Run(args);
					}
					catch (ObjectDisposedException)
					{
						/*Eat it, MainForm disposed itself and Run attempts to dispose of itself.  Eventually we would want to figure out a way to prevent that, but in the meantime it is harmless, so just eat the error*/
					}
				}
				else
				{
#endif
					using (var mf = new MainForm(args))
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
							if (!VersionInfo.DeveloperBuild && Global.MovieSession.Movie.IsActive)
							{
								var result = MessageBox.Show(
									"EmuHawk has thrown a fatal exception and is about to close.\nA movie has been detected. Would you like to try to save?\n(Note: Depending on what caused this error, this may or may succeed)",
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
#if WINDOWS
				}
#endif
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
				if (GlobalWin.Sound != null)
				{
					GlobalWin.Sound.Dispose();
					GlobalWin.Sound = null;
				}
				GlobalWin.GL.Dispose();
				GamePad.CloseAll();
			}
#endif
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

		//for debugging purposes, this is provided. when we're satisfied everyone understands whats going on, we'll get rid of this
		[DllImportAttribute("kernel32.dll", EntryPoint = "CreateFileW")]
		public static extern IntPtr CreateFileW([InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] string lpFileName, int dwDesiredAccess, int dwShareMode, [InAttribute()] int lpSecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, [InAttribute()] int hTemplateFile);
		static void ApplyMOTW(string path)
		{
			int generic_write = 0x40000000;
			int file_share_write = 2;
			int create_always = 2;
			var adsHandle = CreateFileW(path + ":Zone.Identifier", generic_write, file_share_write, 0, create_always, 0, 0);
			using (var sfh = new Microsoft.Win32.SafeHandles.SafeFileHandle(adsHandle, true))
			{
				var adsStream = new FileStream(sfh, FileAccess.Write);
				StreamWriter sw = new StreamWriter(adsStream);
				sw.Write("[ZoneTransfer]\r\nZoneId=3");
				sw.Flush();
				adsStream.Close();
			}
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

#if WINDOWS
		public class SingleInstanceController : WindowsFormsApplicationBase
		{
			readonly string[] cmdArgs;
			public SingleInstanceController(string[] args)
			{
				cmdArgs = args;
				IsSingleInstance = true;
				StartupNextInstance += this_StartupNextInstance;
			}

			void this_StartupNextInstance(object sender, StartupNextInstanceEventArgs e)
			{
				if (e.CommandLine.Count >= 1)
					(MainForm as MainForm).LoadRom(e.CommandLine[0]);
			}

			protected override void OnCreateMainForm()
			{
				MainForm = new MainForm(cmdArgs);
				var title = MainForm.Text;
				MainForm.Show();
				MainForm.Text = title;
				(MainForm as MainForm).ProgramRunLoop();
			} 
		}


#endif
	}
}
