using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
#if WINDOWS
using SlimDX.Direct3D9;
using SlimDX.DirectSound;
using Microsoft.VisualBasic.ApplicationServices;
#endif

namespace BizHawk.MultiClient
{
	static class Program
	{
		[STAThread]
		unsafe static void Main(string[] args)
		{
#if WINDOWS
			// this will look in subdirectory "dll" to load pinvoked stuff
			SetDllDirectory(System.IO.Path.Combine(PathManager.GetExeDirectoryAbsolute(), "dll"));

			//in case assembly resolution fails, such as if we moved them into the dll subdiretory, this event handler can reroute to them
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
#endif

			SubMain(args);
		}

		static void SubMain(string[] args)
		{

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Global.Config = ConfigService.Load<Config>(PathManager.DefaultIniPath, new Config());

#if WINDOWS
			try { Global.DSound = new DirectSound(); }
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
					SingleInstanceController controller = new SingleInstanceController(args);
					controller.Run(args);
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
				MessageBox.Show(e.ToString());
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
#endif


		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			//load missing assemblies by trying to find them in the dll directory
			string dllname = new AssemblyName(args.Name).Name + ".dll";
			string directory = System.IO.Path.Combine(PathManager.GetExeDirectoryAbsolute(), "dll");
			string fname = Path.Combine(directory, dllname);
			if (!File.Exists(fname)) return null;
			//it is important that we use LoadFile here and not load from a byte array; otherwise mixed (managed/unamanged) assemblies can't load
			return Assembly.LoadFile(fname);
		}

#if WINDOWS
		public class SingleInstanceController : WindowsFormsApplicationBase
		{
			MainForm mf;
			string[] cmdArgs;
			public SingleInstanceController(string[] args)
			{
				cmdArgs = args;
				IsSingleInstance = true;
				StartupNextInstance += this_StartupNextInstance;

			}

			void this_StartupNextInstance(object sender, StartupNextInstanceEventArgs e)
			{
				mf.LoadRom(e.CommandLine[0]);
			}

			protected override void OnCreateMainForm()
			{
				MainForm = new RamWatch();

				mf = new MainForm(cmdArgs);
				MainForm = mf;
				mf.Show();
				mf.ProgramRunLoop();
			}
		}

		public static void DisplayDirect3DError()
		{
			MessageBox.Show("Failure to initialize Direct3D, reverting to GDI+ display method. Change the option in Config > GUI or install DirectX web update.", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
#endif
	}
}
