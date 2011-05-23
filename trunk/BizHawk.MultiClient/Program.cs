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

			Global.Config = ConfigService.Load<Config>(PathManager.GetExePathAbsolute() + "\\config.ini");

            try { Global.DSound = new DirectSound(); }
            catch {
                MessageBox.Show("Couldn't initialize DirectSound!");
                return;
            }

			try { Global.Direct3D = new Direct3D(); }
			catch
			{
				//can fallback to GDI rendering
				Global.Config.ForceGDI = true;
			}

            try {
                if (Global.Config.SingleInstanceMode)
                {
                    SingleInstanceController controller = new SingleInstanceController(args);
                    controller.Run(args);
                }
                else
                {
                    var mf = new MainForm(args);
                    mf.Show();
                    mf.ProgramRunLoop();
                }
            } catch (Exception e) {
				MessageBox.Show(e.ToString(), "Oh, no, a terrible thing happened!\n\n" + e.ToString());
            } finally {
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
    }
}
