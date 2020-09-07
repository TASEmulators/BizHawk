using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Microsoft.VisualBasic.ApplicationServices;

using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;

using OSTC = EXE_PROJECT.OSTailoredCode;

namespace BizHawk.Client.EmuHawk
{
	internal static class Program
	{
		static Program()
		{
			//this needs to be done before the warnings/errors show up
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			if (OSTC.IsUnixHost)
			{
				// for Unix, skip everything else and just wire up the event handler
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
				return;
			}

			static void CheckLib(string dllToLoad, string desc)
			{
				var p = OSTC.LinkedLibManager.LoadOrZero(dllToLoad);
				if (p == IntPtr.Zero)
				{
					using (var box = new ExceptionBox($"EmuHawk needs {desc} in order to run! See the readme for more info. (EmuHawk will now close.)")) box.ShowDialog();
					Process.GetCurrentProcess().Kill();
					return;
				}
				OSTC.LinkedLibManager.FreeByPtr(p);
			}
			CheckLib("vcruntime140_1.dll", "Microsoft Visual C++ Redistributable for Visual Studio 2015, 2017 and 2019 (x64)");
			CheckLib("msvcr100.dll", "Microsoft Visual C++ 2010 SP1 Runtime (x64)"); // for Mupen64Plus, and some others

			// this will look in subdirectory "dll" to load pinvoked stuff
			var dllDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
			SetDllDirectory(dllDir);

			//in case assembly resolution fails, such as if we moved them into the dll subdiretory, this event handler can reroute to them
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			//but before we even try doing that, whack the MOTW from everything in that directory (that's a dll)
			//otherwise, some people will have crashes at boot-up due to .net security disliking MOTW.
			//some people are getting MOTW through a combination of browser used to download bizhawk, and program used to dearchive it
			//We need to do it here too... otherwise people get exceptions when externaltools we distribute try to startup
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

		[STAThread]
		private static int Main(string[] args)
		{
			var exitCode = SubMain(args);
			if (OSTC.IsUnixHost)
			{
				Console.WriteLine("BizHawk has completed its shutdown routines, killing process...");
				Process.GetCurrentProcess().Kill();
			}
			return exitCode;
		}

		//NoInlining should keep this code from getting jammed into Main() which would create dependencies on types which havent been setup by the resolver yet... or something like that
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		private static int SubMain(string[] args)
		{
			// this check has to be done VERY early.  i stepped through a debug build with wrong .dll versions purposely used,
			// and there was a TypeLoadException before the first line of SubMain was reached (some static ColorType init?)
			// zero 25-dec-2012 - only do for public builds. its annoying during development
			// and don't bother when installed from a package manager i.e. not Windows --yoshi
			// commenting this out until I get it generated properly --yoshi
//			if (!VersionInfo.DeveloperBuild && !OSTC.IsUnixHost)
//			{
//				var thisversion = typeof(Program).Assembly.GetName().Version;
//				var utilversion = Assembly.Load(new AssemblyName("BizHawk.Client.Common")).GetName().Version;
//				var emulversion = Assembly.Load(new AssemblyName("BizHawk.Emulation.Cores")).GetName().Version;
//
//				if (thisversion != utilversion || thisversion != emulversion)
//				{
//					MessageBox.Show("Conflicting revisions found!  Don't mix .dll versions!");
//					return -1;
//				}
//			}

			TempFileManager.Start();

			HawkFile.DearchivalMethod = SharpCompressDearchivalMethod.Instance;

			string cmdConfigFile = ArgParser.GetCmdConfigFile(args);
			if (cmdConfigFile != null) Config.SetDefaultIniPath(cmdConfigFile);

			try
			{
				if (!VersionInfo.DeveloperBuild && !ConfigService.IsFromSameVersion(Config.DefaultIniPath, out var msg))
				{
					new MsgBox(msg, "Mismatched version in config file", MessageBoxIcon.Warning).ShowDialog();
				}
				GlobalWin.Config = ConfigService.Load<Config>(Config.DefaultIniPath);
			}
			catch (Exception e)
			{
				new ExceptionBox(string.Join("\n",
					"It appears your config file (config.ini) is corrupted; an exception was thrown while loading it.",
					"On closing this warning, EmuHawk will delete your config file and generate a new one. You can go make a backup now if you'd like to look into diffs.",
					"The caught exception was:",
					e.ToString()
				)).ShowDialog();
				File.Delete(Config.DefaultIniPath);
				GlobalWin.Config = ConfigService.Load<Config>(Config.DefaultIniPath);
			}

			GlobalWin.Config.ResolveDefaults();
			FFmpegService.FFmpegPath = Path.Combine(PathUtils.DllDirectoryPath, OSTC.IsUnixHost ? "ffmpeg" : "ffmpeg.exe");

			StringLogUtil.DefaultToDisk = GlobalWin.Config.Movies.MoviesOnDisk;

			// super hacky! this needs to be done first. still not worth the trouble to make this system fully proper
			if (Array.Exists(args, arg => arg.StartsWith("--gdi", StringComparison.InvariantCultureIgnoreCase)))
			{
				GlobalWin.Config.DispMethod = EDispMethod.GdiPlus;
			}

			// create IGL context. we do this whether or not the user has selected OpenGL, so that we can run opengl-based emulator cores
			GlobalWin.IGL_GL = new IGL_TK(2, 0, false);

			// setup the GL context manager, needed for coping with multiple opengl cores vs opengl display method
			GLManager.CreateInstance();
			GlobalWin.GLManager = GLManager.Instance;

			//now create the "GL" context for the display method. we can reuse the IGL_TK context if opengl display method is chosen
		REDO_DISPMETHOD:
			if (GlobalWin.Config.DispMethod == EDispMethod.GdiPlus)
			{
				GlobalWin.GL = new IGL_GdiPlus();
			}
			else if (OSTailoredCode.CurrentOS == OSTailoredCode.DistinctOS.Windows && GlobalWin.Config.DispMethod == EDispMethod.SlimDX9)
			{
				try
				{
					GlobalWin.GL = new IGL_SlimDX9();
				}
				catch(Exception ex)
				{
					new ExceptionBox(new Exception("Initialization of Direct3d 9 Display Method failed; falling back to GDI+", ex)).ShowDialog();

					// fallback
					GlobalWin.Config.DispMethod = EDispMethod.GdiPlus;
					goto REDO_DISPMETHOD;
				}
			}
			else
			{
				GlobalWin.GL = GlobalWin.IGL_GL;

				// check the opengl version and don't even try to boot this crap up if its too old
				if (GlobalWin.IGL_GL.Version < 200)
				{
					// fallback
					GlobalWin.Config.DispMethod = EDispMethod.GdiPlus;
					goto REDO_DISPMETHOD;
				}
			}

			// try creating a GUI Renderer. If that doesn't succeed. we fallback
			try
			{
				using (GlobalWin.GL.CreateRenderer()) { }
			}
			catch(Exception ex)
			{
				new ExceptionBox(new Exception("Initialization of Display Method failed; falling back to GDI+", ex)).ShowDialog();

				//fallback
				GlobalWin.Config.DispMethod = EDispMethod.GdiPlus;
				goto REDO_DISPMETHOD;
			}

			if (!OSTC.IsUnixHost)
			{
				//WHY do we have to do this? some intel graphics drivers (ig7icd64.dll 10.18.10.3304 on an unknown chip on win8.1) are calling SetDllDirectory() for the process, which ruins stuff.
				//The relevant initialization happened just before in "create IGL context".
				//It isn't clear whether we need the earlier SetDllDirectory(), but I think we do.
				//note: this is pasted instead of being put in a static method due to this initialization code being sensitive to things like that, and not wanting to cause it to break
				//pasting should be safe (not affecting the jit order of things)
				var dllDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
				SetDllDirectory(dllDir);
			}

			try
			{
				if (!OSTC.IsUnixHost && GlobalWin.Config.SingleInstanceMode)
				{
					try
					{
						InitAndRunSingleInstance(args);
					}
					catch (ObjectDisposedException)
					{
						// Eat it, MainForm disposed itself and Run attempts to dispose of itself.  Eventually we would want to figure out a way to prevent that, but in the meantime it is harmless, so just eat the error
					}
				}
				else
				{
					var mf = new MainForm(args);
//					var title = mf.Text;
					mf.Show();
//					mf.Text = title;
					try
					{
						GlobalWin.ExitCode = mf.ProgramRunLoop();
						if (!mf.IsDisposed)
							mf.Dispose();
					}
					catch (Exception e) when (GlobalWin.MovieSession.Movie.IsActive() && !(Debugger.IsAttached || VersionInfo.DeveloperBuild))
					{
						var result = MessageBox.Show(
							"EmuHawk has thrown a fatal exception and is about to close.\nA movie has been detected. Would you like to try to save?\n(Note: Depending on what caused this error, this may or may not succeed)",
							$"Fatal error: {e.GetType().Name}",
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Exclamation
						);
						if (result == DialogResult.Yes)
						{
							GlobalWin.MovieSession.Movie.Save();
						}
					}
				}
			}
			catch (Exception e) when (!Debugger.IsAttached)
			{
				new ExceptionBox(e).ShowDialog();
			}
			finally
			{
				GlobalWin.Sound?.Dispose();
				GlobalWin.Sound = null;
				GlobalWin.GL.Dispose();
				Input.Instance.Adapter.DeInitAll();
			}

			//cleanup:
			//cleanup IGL stuff so we can get better refcounts when exiting process, for debugging
			//DOESNT WORK FOR SOME REASON
			//GlobalWin.IGL_GL = new IGL_TK();
			//GLManager.Instance.Dispose();
			//if (GlobalWin.IGL_GL != GlobalWin.GL)
			//  GlobalWin.GL.Dispose();
			//((IDisposable)GlobalWin.IGL_GL).Dispose();

			//return 0 assuming things have gone well, non-zero values could be used as error codes or for scripting purposes
			return GlobalWin.ExitCode;
		} //SubMain

		//declared here instead of a more usual place to avoid dependencies on the more usual place

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern uint SetDllDirectory(string lpPathName);

		[DllImport("kernel32.dll", EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		private static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)]string lpFileName);

		/// <remarks>http://www.codeproject.com/Articles/310675/AppDomain-AssemblyResolve-Event-Tips</remarks>
		private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var requested = args.Name;

			//mutate filename depending on selection of lua core. here's how it works
			//1. we build NLua to the output/dll/lua directory. that brings KopiLua with it
			//2. We reference it from there, but we tell it not to copy local; that way there's no NLua in the output/dll directory
			//3. When NLua assembly attempts to load, it can't find it
			//I. if LuaInterface is selected by the user, we switch to requesting that.
			//     (those DLLs are built into the output/DLL directory)
			//II. if NLua is selected by the user, we skip over this part;
			//    later, we look for NLua or KopiLua assembly names and redirect them to files located in the output/DLL/nlua directory
			if (new AssemblyName(requested).Name == "NLua")
			{
				//this method referencing GlobalWin.Config makes assemblies get loaded, which isnt smart from the assembly resolver.
				//so.. we're going to resort to something really bad.
				//avert your eyes.
				var configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.ini");
				if (!OSTC.IsUnixHost // LuaInterface is not currently working on Mono
					&& File.Exists(configPath)
					&& (Array.Find(File.ReadAllLines(configPath), line => line.Contains("  \"LuaEngine\": ")) ?? string.Empty)
						.Contains("0"))
				{
					requested = "LuaInterface";
				}
			}

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

		private class SingleInstanceController : WindowsFormsApplicationBase
		{
			private readonly string[] cmdArgs;

			public SingleInstanceController(string[] args)
			{
				cmdArgs = args;
				IsSingleInstance = true;
				StartupNextInstance += this_StartupNextInstance;
			}

			public void Run() => Run(cmdArgs);

			private void this_StartupNextInstance(object sender, StartupNextInstanceEventArgs e)
			{
				if (e.CommandLine.Count >= 1)
					((MainForm)MainForm).LoadRom(e.CommandLine[0], new LoadRomArgs { OpenAdvanced = new OpenAdvanced_OpenRom() });
			}

			protected override void OnCreateMainForm()
			{
				MainForm = new MainForm(cmdArgs);
				var title = MainForm.Text;
				MainForm.Show();
				MainForm.Text = title;
				GlobalWin.ExitCode = ((MainForm)MainForm).ProgramRunLoop();
			}
		}

		private static void InitAndRunSingleInstance(string[] args) => new SingleInstanceController(args).Run();
	}
}
