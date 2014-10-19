using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

namespace BizHawk.Client.EmuHawk
{
	public partial class SMSGraphicsConfig : Form
	{

		public SMSGraphicsConfig()
		{
			InitializeComponent();
		}

		private void SMSGraphicsConfig_Load(object sender, EventArgs e)
		{
			var s = ((SMS)Global.Emulator).GetSettings();
			DispOBJ.Checked = s.DispOBJ;
			DispBG.Checked = s.DispBG;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			var s = ((SMS)Global.Emulator).GetSettings();
			s.DispOBJ = DispOBJ.Checked;
			s.DispBG = DispBG.Checked;
			GlobalWin.MainForm.PutCoreSettings(s);
			Close();
		}
	}
}
