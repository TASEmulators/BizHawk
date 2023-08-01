using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.Graphics;
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

			// quickly check if the user is running this as a 32 bit process somehow
			if (!Environment.Is64BitProcess)
			{
				using (var box = new ExceptionBox($"EmuHawk requires a 64 bit environment in order to run! EmuHawk will now close.")) box.ShowDialog();
				Process.GetCurrentProcess().Kill();
				return;
			}

			if (OSTC.IsUnixHost)
			{
				// for Unix, skip everything else and just wire up the event handler
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
				return;
			}

			foreach (var (dllToLoad, desc) in new[]
			{
				("vcruntime140_1.dll", "Microsoft Visual C++ Redistributable for Visual Studio 2015, 2017 and 2019 (x64)"),
				("msvcr100.dll", "Microsoft Visual C++ 2010 SP1 Runtime (x64)"), // for Mupen64Plus, and some others
			})
			{
				var p = OSTC.LinkedLibManager.LoadOrZero(dllToLoad);
				if (p != IntPtr.Zero)
				{
					OSTC.LinkedLibManager.FreeByPtr(p);
					continue;
				}
				// else it's missing or corrupted
				using (ExceptionBox box = new($"EmuHawk needs {desc} in order to run! See the readme on GitHub for more info. (EmuHawk will now close.) Internal error message: {OSTC.LinkedLibManager.GetErrorMessage()}"))
				{
					box.ShowDialog();
				}
				Process.GetCurrentProcess().Kill();
				return;
			}

			// this will look in subdirectory "dll" to load pinvoked stuff
			var dllDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
			_ = SetDllDirectory(dllDir);

			//in case assembly resolution fails, such as if we moved them into the dll subdiretory, this event handler can reroute to them
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			try
			{
				// but before we even try doing that, whack the MOTW from everything in that directory (that's a dll)
				// otherwise, some people will have crashes at boot-up due to .net security disliking MOTW.
				// some people are getting MOTW through a combination of browser used to download bizhawk, and program used to dearchive it
				// We need to do it here too... otherwise people get exceptions when externaltools we distribute try to startup
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
			var thisAsmVer = EmuHawk.ReflectionCache.AsmVersion;
			foreach (var asmVer in new[]
			{
				BizInvoke.ReflectionCache.AsmVersion,
				Bizware.BizwareGL.ReflectionCache.AsmVersion,
				Client.Common.ReflectionCache.AsmVersion,
				Common.ReflectionCache.AsmVersion,
				Emulation.Common.ReflectionCache.AsmVersion,
				Emulation.Cores.ReflectionCache.AsmVersion,
				Emulation.DiscSystem.ReflectionCache.AsmVersion,
				WinForms.Controls.ReflectionCache.AsmVersion,
			})
			{
				if (asmVer != thisAsmVer)
				{
					MessageBox.Show("One or more of the BizHawk.* assemblies have the wrong version!\n(Did you attempt to update by overwriting an existing install?)");
					return -1;
				}
			}

			typeof(Form).GetField(OSTC.IsUnixHost ? "default_icon" : "defaultIcon", BindingFlags.NonPublic | BindingFlags.Static)
				.SetValue(null, Properties.Resources.Logo);

			TempFileManager.Start();

			HawkFile.DearchivalMethod = SharpCompressDearchivalMethod.Instance;

			ParsedCLIFlags cliFlags = default;
			try
			{
				ArgParser.ParseArguments(out cliFlags, args);
			}
			catch (ArgParser.ArgParserException e)
			{
				new ExceptionBox(e.Message).ShowDialog();
			}

			var configPath = cliFlags.cmdConfigFile ?? Path.Combine(PathUtils.ExeDirectoryPath, "config.ini");

			Config initialConfig;
			try
			{
				if (!VersionInfo.DeveloperBuild && !ConfigService.IsFromSameVersion(configPath, out var msg))
				{
					new MsgBox(msg, "Mismatched version in config file", MessageBoxIcon.Warning).ShowDialog();
				}
				initialConfig = ConfigService.Load<Config>(configPath);
			}
			catch (Exception e)
			{
				new ExceptionBox(string.Join("\n",
					"It appears your config file (config.ini) is corrupted; an exception was thrown while loading it.",
					"On closing this warning, EmuHawk will delete your config file and generate a new one. You can go make a backup now if you'd like to look into diffs.",
					"The caught exception was:",
					e.ToString()
				)).ShowDialog();
				File.Delete(configPath);
				initialConfig = ConfigService.Load<Config>(configPath);
			}
			initialConfig.ResolveDefaults();
			if (initialConfig.SaveSlot is 0) initialConfig.SaveSlot = 10; //TODO remove after a while
			// initialConfig should really be globalConfig as it's mutable

			FFmpegService.FFmpegPath = Path.Combine(PathUtils.DataDirectoryPath, "dll", OSTC.IsUnixHost ? "ffmpeg" : "ffmpeg.exe");

			StringLogUtil.DefaultToDisk = initialConfig.Movies.MoviesOnDisk;

			var glInitCount = 0;

			IGL TryInitIGL(EDispMethod dispMethod)
			{
				glInitCount++;

				(EDispMethod Method, string Name) ChooseFallback()
					=> glInitCount switch
					{
						// try to fallback on the faster option on Windows
						// if we're on a Unix platform, there's only 1 fallback here...
						1 when OSTC.IsUnixHost => (EDispMethod.GdiPlus, "GDI+"),
						1 or 2 when !OSTC.IsUnixHost => dispMethod == EDispMethod.D3D9
							? (EDispMethod.OpenGL, "OpenGL")
							: (EDispMethod.D3D9, "Direct3D9"),
						_ => (EDispMethod.GdiPlus, "GDI+")
					};

				IGL CheckRenderer(IGL gl)
				{
					try
					{
						using (gl.CreateRenderer()) return gl;
					}
					catch (Exception ex)
					{
						var fallback = ChooseFallback();
						new ExceptionBox(new Exception($"Initialization of Display Method failed; falling back to {fallback.Name}", ex)).ShowDialog();
						return TryInitIGL(initialConfig.DispMethod = fallback.Method);
					}
				}

				switch (dispMethod)
				{
					case EDispMethod.D3D9:
						if (OSTC.IsUnixHost || OSTC.IsWine)
						{
							// possibly sharing config w/ Windows, assume the user wants the not-slow method (but don't change the config)
							return TryInitIGL(EDispMethod.OpenGL);
						}
						try
						{
							return CheckRenderer(new IGL_D3D9());
						}
						catch (Exception ex)
						{
							var fallback = ChooseFallback();
							new ExceptionBox(new Exception($"Initialization of Direct3D9 Display Method failed; falling back to {fallback.Name}", ex)).ShowDialog();
							return TryInitIGL(initialConfig.DispMethod = fallback.Method);
						}
					case EDispMethod.OpenGL:
						if (!IGL_OpenGL.Available)
						{
							// too old to use, need to fallback to something else
							var fallback = ChooseFallback();
							new ExceptionBox(new Exception($"Initialization of OpenGL Display Method failed; falling back to {fallback.Name}")).ShowDialog();
							return TryInitIGL(initialConfig.DispMethod = fallback.Method);
						}
						var igl = new IGL_OpenGL();
						// need to have a context active for checking renderer, will be disposed afterwards
						using (new SDL2OpenGLContext(OpenGLVersion.SupportsVersion(3, 0) ? 3 : 2, 0, false, false))
						{
							return CheckRenderer(igl);
						}
					default:
					case EDispMethod.GdiPlus:
						static GLControlWrapper_GdiPlus CreateGLControlWrapper(IGL_GdiPlus self) => new(self); // inlining as lambda causes crash, don't wanna know why --yoshi
						// if this fails, we're screwed
						return new IGL_GdiPlus(CreateGLControlWrapper);
				}
			}

			// super hacky! this needs to be done first. still not worth the trouble to make this system fully proper
			if (Array.Exists(args, arg => arg.StartsWith("--gdi", StringComparison.InvariantCultureIgnoreCase)))
			{
				initialConfig.DispMethod = EDispMethod.GdiPlus;
			}

			var workingGL = TryInitIGL(initialConfig.DispMethod);

			Sound globalSound = null;

			if (!OSTC.IsUnixHost)
			{
				//WHY do we have to do this? some intel graphics drivers (ig7icd64.dll 10.18.10.3304 on an unknown chip on win8.1) are calling SetDllDirectory() for the process, which ruins stuff.
				//The relevant initialization happened just before in "create IGL context".
				//It isn't clear whether we need the earlier SetDllDirectory(), but I think we do.
				//note: this is pasted instead of being put in a static method due to this initialization code being sensitive to things like that, and not wanting to cause it to break
				//pasting should be safe (not affecting the jit order of things)
				var dllDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
				_ = SetDllDirectory(dllDir);
			}

			if (OSTC.HostWindowsVersion is null || OSTC.HostWindowsVersion.Value.Version >= OSTC.WindowsVersion._10) // "windows isn't capable of being useful for non-administrators until windows 10" --zeromus
			{
				if (EmuHawkUtil.CLRHostHasElevatedPrivileges)
				{
					using MsgBox dialog = new(
						title: "This EmuHawk is privileged",
						message: $"EmuHawk detected it {(OSTC.IsUnixHost ? "is running as root (Superuser)" : "has Administrator privileges")}.\n"
							+ "This is a bad idea.",
						boxIcon: MessageBoxIcon.Warning);
					dialog.ShowDialog();
				}
				else
				{
					Util.DebugWriteLine("running as unprivileged user");
				}
			}

			var exitCode = 0;
			try
			{
				MainForm mf = new(
					cliFlags,
					workingGL,
					() => configPath,
					() => initialConfig,
					newSound => globalSound = newSound,
					args,
					out var movieSession,
					out var exitEarly);
				if (exitEarly)
				{
					//TODO also use this for ArgParser failure
					mf.Dispose();
					return 0;
				}
				mf.LoadGlobalConfigFromFile = iniPath =>
				{
					if (!VersionInfo.DeveloperBuild && !ConfigService.IsFromSameVersion(iniPath, out var msg))
					{
						new MsgBox(msg, "Mismatched version in config file", MessageBoxIcon.Warning).ShowDialog();
					}
					initialConfig = ConfigService.Load<Config>(iniPath);
					initialConfig.ResolveDefaults();
					mf.Config = initialConfig;
				};
//				var title = mf.Text;
				mf.Show();
//				mf.Text = title;
				try
				{
					exitCode = mf.ProgramRunLoop();
					if (!mf.IsDisposed)
						mf.Dispose();
				}
				catch (Exception e) when (movieSession.Movie.IsActive() && !(Debugger.IsAttached || VersionInfo.DeveloperBuild))
				{
					var result = MessageBox.Show(
						"EmuHawk has thrown a fatal exception and is about to close.\nA movie has been detected. Would you like to try to save?\n(Note: Depending on what caused this error, this may or may not succeed)",
						$"Fatal error: {e.GetType().Name}",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Exclamation
					);
					if (result == DialogResult.Yes)
					{
						movieSession.Movie.Save();
					}
				}
			}
			catch (Exception e) when (!Debugger.IsAttached)
			{
				new ExceptionBox(e).ShowDialog();
			}
			finally
			{
				globalSound?.Dispose();
				workingGL.Dispose();
				Input.Instance?.Adapter?.DeInitAll();
			}

			//return 0 assuming things have gone well, non-zero values could be used as error codes or for scripting purposes
			return exitCode;
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

			lock (AppDomain.CurrentDomain)
			{
				var firstAsm = Array.Find(AppDomain.CurrentDomain.GetAssemblies(), asm => asm.FullName == requested);
				if (firstAsm != null) return firstAsm;

				//load missing assemblies by trying to find them in the dll directory
				var dllname = $"{new AssemblyName(requested).Name}.dll";
				var directory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "dll");
				var simpleName = new AssemblyName(requested).Name;
				var fname = Path.Combine(directory, dllname);
				//it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unmanaged) assemblies can't load
				return File.Exists(fname) ? Assembly.LoadFile(fname) : null;
			}
		}
	}
}
