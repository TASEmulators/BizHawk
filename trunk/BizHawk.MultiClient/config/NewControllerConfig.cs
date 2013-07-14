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

		const int MAXPLAYERS = 8;

		private NewControllerConfig()
		{
			InitializeComponent();
		}

		static void LoadToPanel(Control dest, ControllerDefinition def, Dictionary<string, Dictionary<string, string>> settingsblock)
		{
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

			if (settings.Keys.Count == 0)
				return;

			// split the list of all settings into buckets by player number
			List<string>[] buckets = new List<string>[MAXPLAYERS + 1];
			for (int i = 0; i < buckets.Length; i++)
				buckets[i] = new List<string>();

			foreach (string button in settings.Keys)
			{
				int i;
				for (i = 1; i <= MAXPLAYERS; i++)
				{
					if (button.StartsWith("P" + i))
						break;
				}
				if (i > MAXPLAYERS) // couldn't find
					i = 0;
				buckets[i].Add(button);
			}

			if (buckets[0].Count == settings.Keys.Count)
			{
				// everything went into bucket 0, so make no tabs at all
				var cp = new ControllerConfigPanel();
				cp.Dock = DockStyle.Fill;
				dest.Controls.Add(cp);
				cp.LoadSettings(settings, null, dest.Width, dest.Height);
			}
			else
			{
				// create multiple player tabs
				var tt = new TabControl();
				tt.Dock = DockStyle.Fill;
				dest.Controls.Add(tt);
				int pageidx = 0;
				for (int i = 1; i <= MAXPLAYERS; i++)
				{
					if (buckets[i].Count > 0)
					{
						tt.TabPages.Add("Player " + i);

						var cp = new ControllerConfigPanel();
						cp.Dock = DockStyle.Fill;
						tt.TabPages[pageidx].Controls.Add(cp);
						cp.LoadSettings(settings, buckets[i], tt.Width, tt.Height);
						pageidx++;
					}
				}
				if (buckets[0].Count > 0)
				{
					tt.TabPages.Add("Console");
					var cp = new ControllerConfigPanel();
					cp.Dock = DockStyle.Fill;
					tt.TabPages[pageidx].Controls.Add(cp);
					cp.LoadSettings(settings, buckets[0], tt.Width, tt.Height);
					pageidx++;
				}
			}
		}

		public NewControllerConfig(ControllerDefinition def)
			: this()
		{
			SuspendLayout();
			LoadToPanel(tabPage1, def, Global.Config.AllTrollers);
			LoadToPanel(tabPage2, def, Global.Config.AllTrollersAutoFire);

			Text = def.Name + " Configuration";
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

		// lazy methods, but they're not called often and actually
		// tracking all of the ControllerConfigPanels wouldn't be simpler
		static void SetAutoTab(Control c, bool value)
		{
			if (c is ControllerConfigPanel)
				(c as ControllerConfigPanel).SetAutoTab(value);
			else if (c.HasChildren)
				foreach (Control cc in c.Controls)
					SetAutoTab(cc, value);
		}

		static void Save(Control c)
		{
			if (c is ControllerConfigPanel)
				(c as ControllerConfigPanel).Save();
			else if (c.HasChildren)
				foreach (Control cc in c.Controls)
					Save(cc);
		}



		private void checkBoxAutoTab_CheckedChanged(object sender, EventArgs e)
		{
			SetAutoTab(this, checkBoxAutoTab.Checked);
		}

		private void checkBoxUDLR_CheckedChanged(object sender, EventArgs e)
		{
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			Global.Config.AllowUD_LR = checkBoxUDLR.Checked;
			Global.Config.InputConfigAutoTab = checkBoxAutoTab.Checked;

			Save(this);

			Global.OSD.AddMessage("Controller settings saved");
			DialogResult = DialogResult.OK;
			Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			Global.OSD.AddMessage("Controller config aborted");
			Close();
		}

		private void NewControllerConfig_Load(object sender, EventArgs e)
		{

		}
	}
}
