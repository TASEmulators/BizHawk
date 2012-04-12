using System;
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
		static void Main(string[] args)
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
					var mf = new MainForm(args);
					var title = mf.Text;
					mf.Text = title;
                    Application.Run(mf);
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
			}
#endif
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
                Application.Run(mf);
			}
		}

		public static void DisplayDirect3DError()
		{
			MessageBox.Show("Failure to initialize Direct3D, reverting to GDI+ display method. Change the option in Config > GUI or install DirectX web update.", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
#endif
	}
}
