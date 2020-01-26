using System;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

namespace BizHawk.Client.EmuHawk
{
	public partial class SmsGraphicsConfig : Form
	{
		private readonly MainForm _mainForm;
		private readonly SMS.SmsSettings _settings;
		public SmsGraphicsConfig(
			MainForm mainForm,
			SMS.SmsSettings settings)
		{
			_mainForm = mainForm;
			_settings = settings;
			InitializeComponent();
		}

		private void SMSGraphicsConfig_Load(object sender, EventArgs e)
		{
			DispOBJ.Checked = _settings.DispOBJ;
			DispBG.Checked = _settings.DispBG;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			_settings.DispOBJ = DispOBJ.Checked;
			_settings.DispBG = DispBG.Checked;
			_mainForm.PutCoreSettings(_settings);
			Close();
		}
	}
}
