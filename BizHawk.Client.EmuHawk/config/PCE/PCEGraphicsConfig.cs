using System;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.PCEngine;

namespace BizHawk.Client.EmuHawk
{
	public partial class PCEGraphicsConfig : Form
	{
		private readonly MainForm _mainForm;
		private readonly PCEngine.PCESettings _settings;

		public PCEGraphicsConfig(
			MainForm mainForm,
			PCEngine.PCESettings settings)
		{
			_mainForm = mainForm;
			_settings = settings;
			InitializeComponent();
		}

		private void PCEGraphicsConfig_Load(object sender, EventArgs e)
		{
			DispOBJ1.Checked = _settings.ShowOBJ1;
			DispBG1.Checked = _settings.ShowBG1;
			DispOBJ2.Checked = _settings.ShowOBJ2;
			DispBG2.Checked = _settings.ShowBG2;
			NTSC_FirstLineNumeric.Value = _settings.Top_Line;
			NTSC_LastLineNumeric.Value = _settings.Bottom_Line;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			_settings.ShowOBJ1 = DispOBJ1.Checked;
			_settings.ShowBG1 = DispBG1.Checked;
			_settings.ShowOBJ2 = DispOBJ2.Checked;
			_settings.ShowBG2 = DispBG2.Checked;
			_settings.Top_Line = (int)NTSC_FirstLineNumeric.Value;
			_settings.Bottom_Line = (int)NTSC_LastLineNumeric.Value;
			_mainForm.PutCoreSettings(_settings);
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
