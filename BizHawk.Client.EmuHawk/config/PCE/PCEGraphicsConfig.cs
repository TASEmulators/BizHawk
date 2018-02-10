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
			NTSC_FirstLineNumeric.Value = s.Top_Line;
			NTSC_LastLineNumeric.Value = s.Bottom_Line;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			var pce = (PCEngine)Global.Emulator;
			PCEngine.PCESettings s = pce.GetSettings();
			s.ShowOBJ1 = DispOBJ1.Checked;
			s.ShowBG1 = DispBG1.Checked;
			s.ShowOBJ2 = DispOBJ2.Checked;
			s.ShowBG2 = DispBG2.Checked;
			s.Top_Line = (int)NTSC_FirstLineNumeric.Value;
			s.Bottom_Line = (int)NTSC_LastLineNumeric.Value;
			pce.PutSettings(s);
			Close();
		}

		private void BtnAreaStandard_Click(object sender, EventArgs e)
		{
			NTSC_FirstLineNumeric.Value = 18;
			NTSC_LastLineNumeric.Value = 252;
		}

		private void BtnAreaFull_Click(object sender, EventArgs e)
		{
			NTSC_FirstLineNumeric.Value = 0;
			NTSC_LastLineNumeric.Value = 262;
		}
	}
}
