using System;
using MonoMac.AppKit;
using BizHawk.MultiClient;

namespace MonoMacWrapper
{
	class Program
	{
		public static void Main (string [] args)
		{
			BizHawk.HawkUIFactory.OpenDialogClass = typeof(MacOpenFileDialog);
			NSApplication.Init();
			//NSApplication.Main(args);
			//Application.EnableVisualStyles();
			//Application.SetCompatibleTextRenderingDefault(false);
			Global.Config = ConfigService.Load<Config>(PathManager.DefaultIniPath, new Config());
			try
			{
					var mf = new BizHawk.MultiClient.MainForm(args);
					var title = mf.Text;
					mf.Show();
					mf.Text = title;
					mf.ProgramRunLoop();
			}
			catch (Exception e)
			{
				NSAlert nsa = new NSAlert();
				nsa.MessageText = e.ToString();
				nsa.RunModal();
			}
		}
	}
}

