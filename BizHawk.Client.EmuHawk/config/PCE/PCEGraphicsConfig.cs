using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.PCEngine;

namespace BizHawk.Client.EmuHawk
{
	public partial class PCEGraphicsConfig : Form
	{
		public PCEGraphicsConfig()
		{
			InitializeComponent();
		}

		private void PCEGraphicsConfig_Load(object sender, EventArgs e)
		{
			PCEngine.PCESettings s = ((PCEngine)Global.Emulator).GetSettings();

			DispOBJ1.Checked = s.ShowOBJ1;
			DispBG1.Checked = s.ShowBG1;
			DispOBJ2.Checked = s.ShowOBJ2;
			DispBG2.Checked = s.ShowBG2;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			var pce = (PCEngine)Global.Emulator;
			PCEngine.PCESettings s = pce.GetSettings();
			s.ShowOBJ1 = DispOBJ1.Checked;
			s.ShowBG1 = DispBG1.Checked;
			s.ShowOBJ2 = DispOBJ2.Checked;
			s.ShowBG2 = DispBG2.Checked;
			pce.PutSettings(s);
			Close();
		}
	}
}
