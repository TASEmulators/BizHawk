using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient.config
{
	public partial class NewControllerConfig : Form
	{
		static Dictionary<string, Bitmap> ControllerImages = new Dictionary<string, Bitmap>();
		static NewControllerConfig()
		{
			ControllerImages.Add("NES Controller", Properties.Resources.NES_Controller);
			ControllerImages.Add("Atari 7800 ProLine Joystick Controller", Properties.Resources.A78Joystick);
			ControllerImages.Add("SNES Controller", Properties.Resources.SNES_Controller);
			ControllerImages.Add("Commodore 64 Controller", Properties.Resources.C64Joystick);
			ControllerImages.Add("GBA Controller", Properties.Resources.GBA_Controller);
			ControllerImages.Add("Dual Gameboy Controller", Properties.Resources.GBController);
			ControllerImages.Add("Nintento 64 Controller", Properties.Resources.N64);
			ControllerImages.Add("Saturn Controller", Properties.Resources.SaturnController);
			//ControllerImages.Add("PSP Controller", Properties.Resources);
			ControllerImages.Add("PC Engine Controller", Properties.Resources.PCEngineController);
			ControllerImages.Add("Atari 2600 Basic Controller", Properties.Resources.atari_controller);
			ControllerImages.Add("Genesis 3-Button Controller", Properties.Resources.GENController);
			ControllerImages.Add("Gameboy Controller", Properties.Resources.GBController);
			ControllerImages.Add("SMS Controller", Properties.Resources.SMSController);
			ControllerImages.Add("TI83 Controller", Properties.Resources.TI83_Controller);
			//ControllerImages.Add(, Properties.Resources);
		}

		private NewControllerConfig()
		{
			InitializeComponent();
		}

		ControllerConfigPanel normcontrls;
		ControllerConfigPanel autofirecontrls;

		static void DoLoadSettings(ControllerConfigPanel cp, ControllerDefinition def, Dictionary<string, Dictionary<string, string>> settingsblock)
		{
			cp.Spacing = 24;
			cp.InputSize = 100;
			cp.LabelPadding = 5;
			cp.ColumnWidth = 170;
			cp.LabelWidth = 60;

			Dictionary<string, string> settings;
			if (!settingsblock.TryGetValue(def.Name, out settings))
			{
				settings = new Dictionary<string, string>();
				settingsblock[def.Name] = settings;
			}
			// check to make sure that the settings object has all of the appropriate boolbuttons
			foreach (string button in def.BoolButtons)
			{
				if (!settings.Keys.Contains(button))
					settings[button] = "";
			}
			cp.LoadSettings(settings);
		}

		public NewControllerConfig(ControllerDefinition def)
			: this()
		{
			SuspendLayout();
			normcontrls = new ControllerConfigPanel();
			normcontrls.Dock = DockStyle.Fill;
			tabPage1.Controls.Add(normcontrls);
			DoLoadSettings(normcontrls, def, Global.Config.AllTrollers);

			autofirecontrls = new ControllerConfigPanel();
			autofirecontrls.Dock = DockStyle.Fill;
			tabPage2.Controls.Add(autofirecontrls);
			DoLoadSettings(autofirecontrls, def, Global.Config.AllTrollersAutoFire);

			label1.Text = "Currently Configuring: " + def.Name;
			checkBoxUDLR.Checked = Global.Config.AllowUD_LR;
			checkBoxAutoTab.Checked = Global.Config.InputConfigAutoTab;

			SetControllerPicture(def.Name);
			ResumeLayout();
		}

		void SetControllerPicture(string ControlName)
		{
			Bitmap bmp;
			if (!ControllerImages.TryGetValue(ControlName, out bmp))
				bmp = Properties.Resources.Help;

			pictureBox1.Image = bmp;
			pictureBox1.Size = bmp.Size;
			tableLayoutPanel1.ColumnStyles[1].Width = bmp.Width;
		}

		private void checkBoxAutoTab_CheckedChanged(object sender, EventArgs e)
		{
			normcontrls.SetAutoTab(checkBoxAutoTab.Checked);
			autofirecontrls.SetAutoTab(checkBoxAutoTab.Checked);
		}

		private void checkBoxUDLR_CheckedChanged(object sender, EventArgs e)
		{
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			Global.Config.AllowUD_LR = checkBoxUDLR.Checked;
			Global.Config.InputConfigAutoTab = checkBoxAutoTab.Checked;

			normcontrls.Save();
			autofirecontrls.Save();

			Global.OSD.AddMessage("Controller settings saved");
			DialogResult = DialogResult.OK;
			Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			Global.OSD.AddMessage("Controller config aborted");
			Close();
		}
	}
}
