using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class ControllerConfig : Form
	{
		//TODO: autoab

		public ControllerConfig()
		{
			InitializeComponent();
		}

		private void ControllerConfig_Load(object sender, EventArgs e)
		{
			AllowLR.Checked = Global.Config.AllowUD_LR;

			NESController1Panel.LoadSettings(Global.Config.NESController[0]);
			NESController2Panel.LoadSettings(Global.Config.NESController[1]);
			NESController3Panel.LoadSettings(Global.Config.NESController[2]);
			NESController4Panel.LoadSettings(Global.Config.NESController[3]);
			NESConsoleButtons.LoadSettings(Global.Config.NESConsoleButtons);
			NESAutofire1Panel.LoadSettings(Global.Config.NESAutoController[0]);
			NESAutofire2Panel.LoadSettings(Global.Config.NESAutoController[1]);
			NESAutofire3Panel.LoadSettings(Global.Config.NESAutoController[2]);
			NESAutofire4Panel.LoadSettings(Global.Config.NESAutoController[3]);

			SNESController1Panel.Spacing = 25;
			SNESController2Panel.Spacing = 25;
			SNESController3Panel.Spacing = 25;
			SNESController4Panel.Spacing = 25;
			SNESAutofire1Panel.Spacing = 25;
			SNESAutofire2Panel.Spacing = 25;
			SNESAutofire3Panel.Spacing = 25;
			SNESAutofire4Panel.Spacing = 25;
			SNESController1Panel.LoadSettings(Global.Config.SNESController[0]);
			SNESController2Panel.LoadSettings(Global.Config.SNESController[1]);
			SNESController3Panel.LoadSettings(Global.Config.SNESController[2]);
			SNESController4Panel.LoadSettings(Global.Config.SNESController[3]);
			SNESConsoleButtons.LoadSettings(Global.Config.SNESConsoleButtons);
			SNESAutofire1Panel.LoadSettings(Global.Config.SNESAutoController[0]);
			SNESAutofire2Panel.LoadSettings(Global.Config.SNESAutoController[1]);
			SNESAutofire3Panel.LoadSettings(Global.Config.SNESAutoController[2]);
			SNESAutofire4Panel.LoadSettings(Global.Config.SNESAutoController[3]);

			GBController1Panel.LoadSettings(Global.Config.GBController[0]);
			GBAutofire1Panel.LoadSettings(Global.Config.GBAutoController[0]);

			GBAController1Panel.LoadSettings(Global.Config.GBAController[0]);
			GBAAutofire1Panel.LoadSettings(Global.Config.GBAAutoController[0]);

			GenesisController1Panel.LoadSettings(Global.Config.GenesisController[0]);
			GenesisAutofire1Panel.LoadSettings(Global.Config.GenesisAutoController[0]);
			GenesisConsoleButtons.LoadSettings(Global.Config.GenesisConsoleButtons);

			SMSController1Panel.LoadSettings(Global.Config.SMSController[0]);
			SMSController2Panel.LoadSettings(Global.Config.SMSController[1]);
			SMSConsoleButtons.LoadSettings(Global.Config.SMSConsoleButtons);
			SMSAutofire1Panel.LoadSettings(Global.Config.SMSAutoController[0]);
			SMSAutofire2Panel.LoadSettings(Global.Config.SMSAutoController[1]);

			PCEController1Panel.LoadSettings(Global.Config.PCEController[0]);
			PCEController2Panel.LoadSettings(Global.Config.PCEController[1]);
			PCEController3Panel.LoadSettings(Global.Config.PCEController[2]);
			PCEController4Panel.LoadSettings(Global.Config.PCEController[3]);
			PCEController5Panel.LoadSettings(Global.Config.PCEController[4]);
			PCEAutofire1Panel.LoadSettings(Global.Config.PCEAutoController[0]);
			PCEAutofire2Panel.LoadSettings(Global.Config.PCEAutoController[1]);
			PCEAutofire3Panel.LoadSettings(Global.Config.PCEAutoController[2]);
			PCEAutofire4Panel.LoadSettings(Global.Config.PCEAutoController[3]);
			PCEAutofire5Panel.LoadSettings(Global.Config.PCEAutoController[4]);

			Atari2600Controller1Panel.LoadSettings(Global.Config.Atari2600Controller[0]);
			Atari2600Controller2Panel.LoadSettings(Global.Config.Atari2600Controller[1]);
			Atari2600ConsoleButtons.LoadSettings(Global.Config.Atari2600ConsoleButtons[0]);
			Atari2600Autofire1Panel.LoadSettings(Global.Config.Atari2600AutoController[0]);
			Atari2600Autofire2Panel.LoadSettings(Global.Config.Atari2600AutoController[1]);

			Atari7800Controller1Panel.LoadSettings(Global.Config.Atari7800Controller[0]);
			Atari7800Controller2Panel.LoadSettings(Global.Config.Atari7800Controller[1]);
			Atari7800ConsoleButtons.LoadSettings(Global.Config.Atari7800ConsoleButtons[0]);
			Atari7800Autofire1Panel.LoadSettings(Global.Config.Atari7800AutoController[0]);
			Atari7800Autofire2Panel.LoadSettings(Global.Config.Atari7800AutoController[1]);

			TI83ControllerPanel.Spacing = 24;
			TI83ControllerPanel.InputSize = 110;
			TI83ControllerPanel.LabelPadding = 5;
			TI83ControllerPanel.ColumnWidth = 170;
			TI83ControllerPanel.LabelWidth = 50;
			TI83ControllerPanel.LoadSettings(Global.Config.TI83Controller[0]);

			C64Controller1Panel.LoadSettings(Global.Config.C64Joysticks[0]);
			C64Controller2Panel.LoadSettings(Global.Config.C64Joysticks[1]);
			C64Autofire1Panel.LoadSettings(Global.Config.C64AutoJoysticks[0]);
			C64Autofire2Panel.LoadSettings(Global.Config.C64AutoJoysticks[1]);

			C64KeyboardPanel.Spacing = 23;
			C64KeyboardPanel.InputSize = 70;
			C64KeyboardPanel.LabelPadding = 4;
			C64KeyboardPanel.ColumnWidth = 130;
			C64KeyboardPanel.LabelWidth = 55;
			C64KeyboardPanel.LoadSettings(Global.Config.C64Keyboard);

			COLController1Panel.InputSize = 110;
			COLController1Panel.LabelWidth = 50;
			COLController1Panel.ColumnWidth = 170;
			COLController1Panel.LoadSettings(Global.Config.ColecoController[0]);

			COLAutofire1Panel.InputSize = 110;
			COLAutofire1Panel.LabelWidth = 50;
			COLAutofire1Panel.ColumnWidth = 170;
			COLAutofire1Panel.LoadSettings(Global.Config.ColecoAutoController[0]);

			COLController2Panel.InputSize = 110;
			COLController2Panel.LabelWidth = 50;
			COLController2Panel.ColumnWidth = 170;
			COLController2Panel.LoadSettings(Global.Config.ColecoController[1]);

			COLAutofire2Panel.InputSize = 110;
			COLAutofire2Panel.LabelWidth = 50;
			COLAutofire2Panel.ColumnWidth = 170;
			COLAutofire2Panel.LoadSettings(Global.Config.ColecoAutoController[1]);


			INTVController1Panel.InputSize = 110;
			INTVController1Panel.LabelWidth = 50;
			INTVController1Panel.ColumnWidth = 170;
			INTVController1Panel.LoadSettings(Global.Config.IntellivisionController[0]);

			INTVAutofire1Panel.InputSize = 110;
			INTVAutofire1Panel.LabelWidth = 50;
			INTVAutofire1Panel.ColumnWidth = 170;
			INTVAutofire1Panel.LoadSettings(Global.Config.IntellivisionAutoController[0]);

			INTVController2Panel.InputSize = 110;
			INTVController2Panel.LabelWidth = 50;
			INTVController2Panel.ColumnWidth = 170;
			INTVController2Panel.LoadSettings(Global.Config.IntellivisionController[1]);

			INTVAutofire2Panel.InputSize = 110;
			INTVAutofire2Panel.LabelWidth = 50;
			INTVAutofire2Panel.ColumnWidth = 170;
			INTVAutofire2Panel.LoadSettings(Global.Config.IntellivisionAutoController[1]);

			SetTabByPlatform();

			if (!Global.MainForm.INTERIM)
			{
				PlatformTabControl.Controls.Remove(tabPageC64);
				PlatformTabControl.Controls.Remove(tabPageGBA);
				PlatformTabControl.Controls.Remove(tabPageINTV);
			}

			AutoTab.Checked = Global.Config.InputConfigAutoTab;
			SetAutoTab();
		}

		private void SetTabByPlatform()
		{
			switch (Global.Emulator.SystemId)
			{
				case "NES":
				case "FDS":
					PlatformTabControl.SelectTab(tabPageNES);
					break;
				case "SNES":
				case "SGB": //TODO: I think it never reports this, so this line could/should be removed
					PlatformTabControl.SelectTab(tabPageSNES);
					break;
				case "GB":
				case "GBC":
					PlatformTabControl.SelectTab(tabPageGameboy);
					break;
				case "GBA":
					PlatformTabControl.SelectTab(tabPageGBA);
					break;
				case "GEN":
					PlatformTabControl.SelectTab(tabPageGenesis);
					break;
				case "SMS":
				case "GG":
				case "SG":
					PlatformTabControl.SelectTab(tabPageSMS);
					break;
				case "PCE":
				case "SGX":
				case "PCECD":
					PlatformTabControl.SelectTab(tabPagePCE);
					break;
				case "A26":
					PlatformTabControl.SelectTab(tabPageAtari2600);
					break;
				case "A78":
					PlatformTabControl.SelectTab(tabPageAtari7800);
					break;
				case "C64":
					PlatformTabControl.SelectTab(tabPageC64);
					break;
				case "Coleco":
					PlatformTabControl.SelectTab(tabPageColeco);
					break;
				case "INTV":
					PlatformTabControl.SelectTab(tabPageINTV);
					break;
				case "TI83":
					PlatformTabControl.SelectTab(tabPageTI83);
					break;
			}
		}

		protected override void OnShown(EventArgs e)
		{
			//Input.Instance.EnableIgnoreModifiers = true;
			base.OnShown(e);
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			//Input.Instance.EnableIgnoreModifiers = false;
		}

		private void SetAutoTab()
		{
			bool setting = AutoTab.Checked;
			foreach (Control control1 in PlatformTabControl.TabPages)
			{
				if (control1 is TabPage)
				{
					foreach (Control control2 in control1.Controls)
					{
						if (control2 is ControllerConfigPanel)
						{
							(control2 as ControllerConfigPanel).SetAutoTab(setting);
						}
						else if (control2 is TabControl)
						{
							foreach (Control control3 in (control2 as TabControl).TabPages)
							{
								if (control3 is TabPage)
								{
									foreach (Control control4 in control3.Controls)
									{
										if (control4 is ControllerConfigPanel)
										{
											(control4 as ControllerConfigPanel).SetAutoTab(setting);
										}
									}
								}
								else if (control3 is ControllerConfigPanel)
								{
									(control3 as ControllerConfigPanel).SetAutoTab(setting);
								}
							}
						}
					}
				}
				else if (control1 is ControllerConfigPanel)
				{
					(control1 as ControllerConfigPanel).SetAutoTab(setting);
				}
			}
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Global.Config.AllowUD_LR = AllowLR.Checked;

			foreach (Control control1 in PlatformTabControl.TabPages)
			{
				if (control1 is TabPage)
				{
					foreach (Control control2 in control1.Controls)
					{
						if (control2 is ControllerConfigPanel)
						{
							(control2 as ControllerConfigPanel).Save();
						}
						else if (control2 is TabControl)
						{
							foreach (Control control3 in (control2 as TabControl).TabPages)
							{
								if (control3 is TabPage)
								{
									foreach (Control control4 in control3.Controls)
									{
										if (control4 is ControllerConfigPanel)
										{
											(control4 as ControllerConfigPanel).Save();
										}
									}
								}
								else if (control3 is ControllerConfigPanel)
								{
									(control3 as ControllerConfigPanel).Save();
								}
							}
						}
					}
				}
				else
				{
					if (control1 is ControllerConfigPanel)
					{
						(control1 as ControllerConfigPanel).Save();
					}
				}
			}

			Global.OSD.AddMessage("Controller settings saved");
			this.DialogResult = DialogResult.OK;
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Global.OSD.AddMessage("Controller config aborted");
			Close();
		}

		private void AutoTab_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.HotkeyConfigAutoTab = AutoTab.Checked;
			SetAutoTab();
		}
	}
}
