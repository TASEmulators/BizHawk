using System;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualBasic.ApplicationServices;

using BizHawk.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	static class Program
	{
		static Program()
		{
			//this needs to be done before the warnings/errors show up
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			var libLoader = EXE_PROJECT.PlatformLinkedLibSingleton.LinkedLibManager;

			//http://www.codeproject.com/Articles/310675/AppDomain-AssemblyResolve-Event-Tips

			//try loading libraries we know we'll need
			//something in the winforms, etc. code below will cause .net to popup a missing msvcr100.dll in case that one's missing
			//but oddly it lets us proceed and we'll then catch it here
			var libExt = EXE_PROJECT.PlatformLinkedLibSingleton.RunningOnUnix ? ".dll.so" : ".dll";
			var d3dx9 = libLoader.LoadPlatformSpecific($"d3dx9_43{libExt}");
			var vc2015 = libLoader.LoadPlatformSpecific($"vcruntime140{libExt}");
			var vc2012 = libLoader.LoadPlatformSpecific($"msvcr120{libExt}"); //TODO - check version?
			var vc2010 = libLoader.LoadPlatformSpecific($"msvcr100{libExt}"); //TODO - check version?
			var vc2010p = libLoader.LoadPlatformSpecific($"msvcp100{libExt}");
			bool fail = false, warn = false;
			warn |= d3dx9 == IntPtr.Zero;
			fail |= vc2015 == IntPtr.Zero;
			fail |= vc2010 == IntPtr.Zero;
			fail |= vc2012 == IntPtr.Zero;
			fail |= vc2010p == IntPtr.Zero;
			if (fail || warn)
			{
				var sw = new System.IO.StringWriter();
				sw.WriteLine("[ OK ] .Net 4.6.1 (You couldn't even get here without it)");
				sw.WriteLine("[{0}] Direct3d 9", d3dx9 == IntPtr.Zero ? "FAIL" : " OK ");
				sw.WriteLine("[{0}] Visual C++ 2010 SP1 Runtime", (vc2010 == IntPtr.Zero || vc2010p == IntPtr.Zero) ? "FAIL" : " OK ");
				sw.WriteLine("[{0}] Visual C++ 2012 Runtime", (vc2012 == IntPtr.Zero) ? "FAIL" : " OK ");
				sw.WriteLine("[{0}] Visual C++ 2015 Runtime", (vc2015 == IntPtr.Zero) ? "FAIL" : " OK ");
				var box = new BizHawk.Client.EmuHawk.CustomControls.PrereqsAlert(!fail);
				box.textBox1.Text = sw.ToString();
				box.ShowDialog();
				if (fail) System.Diagnostics.Process.GetCurrentProcess().Kill();
			}

			libLoader.FreePlatformSpecific(d3dx9);
			libLoader.FreePlatformSpecific(vc2015);
			libLoader.FreePlatformSpecific(vc2012);
			libLoader.FreePlatformSpecific(vc2010);
			libLoader.FreePlatformSpecific(vc2010p);

			if (!EXE_PROJECT.PlatformLinkedLibSingleton.RunningOnUnix)
			{
				// this will look in subdirectory "dll" to load pinvoked stuff
				string dllDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
				SetDllDirectory(dllDir);

				//in case assembly resolution fails, such as if we moved them into the dll subdiretory, this event handler can reroute to them
				AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

				//but before we even try doing that, whack the MOTW from everything in that directory (thats a dll)
				//otherwise, some people will have crashes at boot-up due to .net security disliking MOTW.
				//some people are getting MOTW through a combination of browser used to download bizhawk, and program used to dearchive it
				WhackAllMOTW(dllDir);

				//We need to do it here too... otherwise people get exceptions when externaltools we distribute try to startup
			}
			else
			{
				// on mono we skip all the SetDllDirectory stuff
				// just wire up the event handler
				AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
			}
		}

		[STAThread]
		static int Main(string[] args)
		{
			int sm = SubMain(args);
			// We have a problem on mono where the application (most of the time) does not terminate correctly
			// and we have to CTRL-C to kill the mono process
			// This forces mono to go away immediately
			if (EXE_PROJECT.PlatformLinkedLibSingleton.RunningOnUnix)
			{
				Console.WriteLine("BizHawk has completed it's shutdown routines");
				Console.WriteLine("Attempting to kill mono...");
				Process.GetCurrentProcess().Kill();
			}
			return sm;
		}

		//NoInlining should keep this code from getting jammed into Main() which would create dependencies on types which havent been setup by the resolver yet... or something like that
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		static int SubMain(string[] args)
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
					return -1;
				}
			}

			BizHawk.Common.TempFileManager.Start();			
			
			if (PlatformLinkedLibSingleton.RunningOnUnix)
				HawkFile.ArchiveHandlerFactory = new SharpCompressArchiveHandler();
			else
				HawkFile.ArchiveHandlerFactory = new SevenZipSharpArchiveHandler();

			// Uncomment for system-agnostic glory!
			//HawkFile.ArchiveHandlerFactory = new SharpCompressArchiveHandler();

			ArgParser argParser = new ArgParser();
			argParser.ParseArguments(args);
			if (argParser.cmdConfigFile != null) PathManager.SetDefaultIniPath(argParser.cmdConfigFile);

			try
			{
				Global.Config = ConfigService.Load<Config>(PathManager.DefaultIniPath);
			} catch (Exception e) {
				new ExceptionBox(e).ShowDialog();
				new ExceptionBox("Since your config file is corrupted, we're going to recreate it. Back it up before proceeding if you want to investigate further.").ShowDialog();
				File.Delete(PathManager.DefaultIniPath);
				Global.Config = ConfigService.Load<Config>(PathManager.DefaultIniPath);
			}

			Global.Config.ResolveDefaults();

			BizHawk.Client.Common.StringLogUtil.DefaultToDisk = Global.Config.MoviesOnDisk;
			BizHawk.Client.Common.StringLogUtil.DefaultToAWE = Global.Config.MoviesInAWE;

			// super hacky! this needs to be done first. still not worth the trouble to make this system fully proper
			if (Array.Exists(args, arg => arg.ToLower().StartsWith("--gdi")))
			{
				Global.Config.DispMethod = Config.EDispMethod.GdiPlus;
			}

			// create IGL context. we do this whether or not the user has selected OpenGL, so that we can run opengl-based emulator cores
			GlobalWin.IGL_GL = new Bizware.BizwareGL.Drivers.OpenTK.IGL_TK(2, 0, false);

			// setup the GL context manager, needed for coping with multiple opengl cores vs opengl display method
			GLManager.CreateInstance(GlobalWin.IGL_GL);
			GlobalWin.GLManager = GLManager.Instance;

			//now create the "GL" context for the display method. we can reuse the IGL_TK context if opengl display method is chosen

		REDO_DISPMETHOD:
			if (Global.Config.DispMethod == Config.EDispMethod.GdiPlus)
				GlobalWin.GL = new Bizware.BizwareGL.Drivers.GdiPlus.IGL_GdiPlus();
			else if (Global.Config.DispMethod == Config.EDispMethod.Vulkan)
			{
				try
				{
					GlobalWin.GL = new Bizware.BizwareGL.Drivers.Vulkan.IGL_Vulkan();
				}
				catch (Exception ex)
				{
					new ExceptionBox(new Exception("Something in Vulkan init failed, using GDI+ as fallback", ex))
						.ShowDialog();

					// fallback
					Global.Config.DispMethod = Config.EDispMethod.GdiPlus;
					goto REDO_DISPMETHOD;
				}
			}
			else if (Global.Config.DispMethod == Config.EDispMethod.SlimDX9 && !EXE_PROJECT.PlatformLinkedLibSingleton.RunningOnUnix)
			{
				try
				{
					GlobalWin.GL = new Bizware.BizwareGL.Drivers.SlimDX.IGL_SlimDX9();
				}
				catch(Exception ex)
				{
					new ExceptionBox(new Exception("Initialization of Direct3d 9 Display Method failed; falling back to GDI+", ex))
						.ShowDialog();

					// fallback
					Global.Config.DispMethod = Config.EDispMethod.GdiPlus;
					goto REDO_DISPMETHOD;
				}
			}
			else
			{
				GlobalWin.GL = GlobalWin.IGL_GL;

				// check the opengl version and dont even try to boot this crap up if its too old
				if (GlobalWin.IGL_GL.Version < 200)
				{
					// fallback
					Global.Config.DispMethod = Config.EDispMethod.GdiPlus;
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
				new ExceptionBox(new Exception("Initialization of Display Method failed; falling back to GDI+", ex))
					.ShowDialog();
				//fallback
				Global.Config.DispMethod = Config.EDispMethod.GdiPlus;
				goto REDO_DISPMETHOD;
			}

			if (!EXE_PROJECT.PlatformLinkedLibSingleton.RunningOnUnix)
			{
				//WHY do we have to do this? some intel graphics drivers (ig7icd64.dll 10.18.10.3304 on an unknown chip on win8.1) are calling SetDllDirectory() for the process, which ruins stuff.
				//The relevant initialization happened just before in "create IGL context".
				//It isn't clear whether we need the earlier SetDllDirectory(), but I think we do.
				//note: this is pasted instead of being put in a static method due to this initialization code being sensitive to things like that, and not wanting to cause it to break
				//pasting should be safe (not affecting the jit order of things)
				SetDllDirectory(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll"));
			}

			try
			{
				if (Global.Config.SingleInstanceMode)
				{
					try
					{
						new SingleInstanceController(args).RunWithArgs();
					}
					catch (ObjectDisposedException)
					{
						// Eat it, MainForm disposed itself and Run attempts to dispose of itself.  Eventually we would want to figure out a way to prevent that, but in the meantime it is harmless, so just eat the error
					}
				}
				else
				{
					using (var mf = new MainForm(args))
					{
						var title = mf.Text;
						mf.Show();
						mf.Text = title;
						try
						{
							GlobalWin.ExitCode = mf.ProgramRunLoop();
						}
						catch (Exception e) when (!Debugger.IsAttached && !VersionInfo.DeveloperBuild && Global.MovieSession.Movie.IsActive)
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
					}
				}
			}
			catch (Exception e) when (!Debugger.IsAttached)
			{
				new ExceptionBox(e).ShowDialog();
			}
			finally
			{
				if (GlobalWin.Sound != null)
				{
					GlobalWin.Sound.Dispose();
					GlobalWin.Sound = null;
				}
				GlobalWin.GL.Dispose();
				Input.Cleanup();
			}

			//cleanup:
			//cleanup IGL stuff so we can get better refcounts when exiting process, for debugging
			//DOESNT WORK FOR SOME REASON
			//GlobalWin.IGL_GL = new Bizware.BizwareGL.Drivers.OpenTK.IGL_TK();
			//GLManager.Instance.Dispose();
			//if (GlobalWin.IGL_GL != GlobalWin.GL)
			//  GlobalWin.GL.Dispose();
			//((IDisposable)GlobalWin.IGL_GL).Dispose();

			//return 0 assuming things have gone well, non-zero values could be used as error codes or for scripting purposes
			return GlobalWin.ExitCode;
		} //SubMain

		//declared here instead of a more usual place to avoid dependencies on the more usual place
#if WINDOWS
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern uint SetDllDirectory(string lpPathName);

		[DllImport("kernel32.dll", EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
		static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)]string lpFileName);

		public static void RemoveMOTW(string path)
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


		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			string requested = args.Name;

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
				//this method referencing Global.Config makes assemblies get loaded, which isnt smart from the assembly resolver.
				//so.. we're going to resort to something really bad.
				//avert your eyes.
				bool UseNLua = true;
				string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.ini");
				if (File.Exists(configPath) && (
					File.ReadAllLines(configPath)
						.FirstOrDefault(line => line.Contains("  \"UseNLua\": "))
						?.Contains("false")
						?? false))
				{
					UseNLua = false;
				}

				if (!UseNLua && !EXE_PROJECT.PlatformLinkedLibSingleton.RunningOnUnix)
				{
					// currently LuaInterface is not working/implemented on Mono so we always force NLua, otherwise:
					requested = "LuaInterface";
				}
			}

			lock (AppDomain.CurrentDomain)
			{
				var firstAsm = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), asm => asm.FullName == requested);
				if (firstAsm != null) return firstAsm;

				//load missing assemblies by trying to find them in the dll directory
				string dllname = new AssemblyName(requested).Name + ".dll";
				string directory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
				string simpleName = new AssemblyName(requested).Name;
				if (simpleName == "NLua" || simpleName == "KopiLua") directory = Path.Combine(directory, "nlua");
				string fname = Path.Combine(directory, dllname);
				if (!File.Exists(fname)) return null;

				//it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unamanged) assemblies can't load
				return Assembly.LoadFile(fname);
			}
		}

		public class SingleInstanceController : WindowsFormsApplicationBase
		{
			readonly string[] cmdArgs;
			public SingleInstanceController(string[] args)
			{
				cmdArgs = args;
				IsSingleInstance = true;
				StartupNextInstance += this_StartupNextInstance;
			}

			public void RunWithArgs()
			{
				Run(cmdArgs);
			}

			void this_StartupNextInstance(object sender, StartupNextInstanceEventArgs e)
			{
				if (e.CommandLine.Count >= 1)
					(MainForm as MainForm).LoadRom(e.CommandLine[0], new MainForm.LoadRomArgs() { OpenAdvanced = new OpenAdvanced_OpenRom() });
			}

			protected override void OnCreateMainForm()
			{
				MainForm = new MainForm(cmdArgs);
				var title = MainForm.Text;
				MainForm.Show();
				MainForm.Text = title;
				GlobalWin.ExitCode = (MainForm as MainForm).ProgramRunLoop();
			}
		}
	}
}
