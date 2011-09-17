using System;
using System.Windows.Forms;
using SlimDX.Direct3D9;
using SlimDX.DirectSound;
using Microsoft.VisualBasic.ApplicationServices;

namespace BizHawk.MultiClient
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Global.Config = ConfigService.Load<Config>(PathManager.DefaultIniPath);

			try { Global.DSound = new DirectSound(); }
			catch
			{
				MessageBox.Show("Couldn't initialize DirectSound!", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			try { Global.Direct3D = new Direct3D(); }
			catch
			{
				//fallback to GDI rendering
				if (!Global.Config.DisplayGDI)
					DisplayDirect3DError();
			}

			try
			{
				if (Global.Config.SingleInstanceMode)
				{
					SingleInstanceController controller = new SingleInstanceController(args);
					controller.Run(args);
				}
				else
				{
					var mf = new MainForm(args);
                    var title = mf.Text;
                    mf.Show();
                    mf.Text = title;
					mf.ProgramRunLoop();
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(e.ToString(), "Oh, no, a terrible thing happened!\n\n" + e.ToString());
			}
			finally
			{
				if (Global.DSound != null && Global.DSound.Disposed == false)
					Global.DSound.Dispose();
				if (Global.Direct3D != null && Global.Direct3D.Disposed == false)
					Global.Direct3D.Dispose();
			}

		}

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
	}
}
