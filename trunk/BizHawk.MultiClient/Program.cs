using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
#if WINDOWS
using SlimDX.Direct3D9;
using SlimDX.DirectSound;
using Microsoft.VisualBasic.ApplicationServices;
#endif

#pragma warning disable 618

namespace BizHawk.MultiClient
{
	static class Program
	{
		static Program()
		{
			//http://www.codeproject.com/Articles/310675/AppDomain-AssemblyResolve-Event-Tips
#if WINDOWS
			// this will look in subdirectory "dll" to load pinvoked stuff
			string dllDir = System.IO.Path.Combine(PathManager.GetExeDirectoryAbsolute(), "dll");
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
			if (!MainForm.INTERIM)
			{
				var thisversion = typeof(Program).Assembly.GetName().Version;
				var utilversion = Assembly.LoadWithPartialName("Bizhawk.Util").GetName().Version;
				var emulversion = Assembly.LoadWithPartialName("Bizhawk.Emulation").GetName().Version;

				if (thisversion != utilversion || thisversion != emulversion)
				{
					MessageBox.Show("Conflicting revisions found!  Don't mix .dll versions!");
					return;
				}
			}

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Global.Config = ConfigService.Load<Config>(PathManager.DefaultIniPath, new Config());
			Global.Config.ResolveDefaults();

#if WINDOWS
			try { Global.DSound = SoundEnumeration.Create(); }
			catch
			{
				MessageBox.Show("Couldn't initialize DirectSound! Things may go poorly for you. Try changing your sound driver to 41khz instead of 48khz in mmsys.cpl.", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			try { Global.Direct3D = new Direct3D(); }
			catch
			{
				//fallback to GDI rendering
				if (!Global.Config.DisplayGDI)
					DisplayDirect3DError();
			}
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
					catch (ObjectDisposedException ex)
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
						mf.ProgramRunLoop();
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
				MessageBox.Show(message);
			}
#if WINDOWS
			finally
			{
				if (Global.DSound != null && Global.DSound.Disposed == false)
					Global.DSound.Dispose();
				if (Global.Direct3D != null && Global.Direct3D.Disposed == false)
					Global.Direct3D.Dispose();
				GamePad.CloseAll();
			}
#endif
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

		//for debugging purposes, this is provided. when we're satisfied everyone understands whats going on, we'll get rid of this
		[DllImportAttribute("kernel32.dll", EntryPoint = "CreateFileW")]
		public static extern System.IntPtr CreateFileW([InAttribute()] [MarshalAsAttribute(UnmanagedType.LPWStr)] string lpFileName, int dwDesiredAccess, int dwShareMode, [InAttribute()] int lpSecurityAttributes, int dwCreationDisposition, int dwFlagsAndAttributes, [InAttribute()] int hTemplateFile);
		static void ApplyMOTW(string path)
		{
			int generic_write = 0x40000000;
			int file_share_write = 2;
			int create_always = 2;
			var adsHandle = CreateFileW(path + ":Zone.Identifier", generic_write, file_share_write, 0, create_always, 0, 0);
			using (var sfh = new Microsoft.Win32.SafeHandles.SafeFileHandle(adsHandle, true))
			{
				var adsStream = new System.IO.FileStream(sfh, FileAccess.Write);
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
				string directory = Path.Combine(PathManager.GetExeDirectoryAbsolute(), "dll");
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

		public static void DisplayDirect3DError()
		{
			MessageBox.Show("Failure to initialize Direct3D, reverting to GDI+ display method. Change the option in Config > GUI or install DirectX web update.", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
#endif
	}
}
