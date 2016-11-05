using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.EmuHawk
{
	public partial class NESVSSettings : Form
	{
		private NES.NESSyncSettings _settings;
		private NES _nes;

		public NESVSSettings()
		{
			InitializeComponent();
		}

		private void NESVSSettings_Load(object sender, EventArgs e)
		{
			_nes = Global.Emulator as NES;
			_settings = _nes.GetSyncSettings();

			Dipswitch1CheckBox.Checked = _settings.VSDipswitches.Dip_Switch_1;
			Dipswitch2CheckBox.Checked = _settings.VSDipswitches.Dip_Switch_2;
			Dipswitch3CheckBox.Checked = _settings.VSDipswitches.Dip_Switch_3;
			Dipswitch4CheckBox.Checked = _settings.VSDipswitches.Dip_Switch_4;
			Dipswitch5CheckBox.Checked = _settings.VSDipswitches.Dip_Switch_5;
			Dipswitch6CheckBox.Checked = _settings.VSDipswitches.Dip_Switch_6;
			Dipswitch7CheckBox.Checked = _settings.VSDipswitches.Dip_Switch_7;
			Dipswitch8CheckBox.Checked = _settings.VSDipswitches.Dip_Switch_8;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			_settings.VSDipswitches.Dip_Switch_1 = Dipswitch1CheckBox.Checked;
			_settings.VSDipswitches.Dip_Switch_2 = Dipswitch2CheckBox.Checked;
			_settings.VSDipswitches.Dip_Switch_3 = Dipswitch3CheckBox.Checked;
			_settings.VSDipswitches.Dip_Switch_4 = Dipswitch4CheckBox.Checked;
			_settings.VSDipswitches.Dip_Switch_5 = Dipswitch5CheckBox.Checked;
			_settings.VSDipswitches.Dip_Switch_6 = Dipswitch6CheckBox.Checked;
			_settings.VSDipswitches.Dip_Switch_7 = Dipswitch7CheckBox.Checked;
			_settings.VSDipswitches.Dip_Switch_8 = Dipswitch8CheckBox.Checked;

			var changes = _nes.PutSyncSettings(_settings);

			if (changes)
			{
				GlobalWin.MainForm.FlagNeedsReboot();
			}

			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
