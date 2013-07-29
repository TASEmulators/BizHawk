using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BizHawk.MultiClient.config.ControllerConfig;

namespace BizHawk.MultiClient.config
{
	public partial class NewControllerConfig : Form
	{
		static readonly Dictionary<string, Bitmap> ControllerImages = new Dictionary<string, Bitmap>();
		static NewControllerConfig()
		{
			ControllerImages.Add("NES Controller", Properties.Resources.NES_Controller);
            ControllerImages.Add("SNES Controller", Properties.Resources.SNES_Controller);
            ControllerImages.Add("Nintento 64 Controller", Properties.Resources.N64);
            ControllerImages.Add("Gameboy Controller", Properties.Resources.GBController);
            ControllerImages.Add("GBA Controller", Properties.Resources.GBA_Controller);
            ControllerImages.Add("Dual Gameboy Controller", Properties.Resources.GBController);

            ControllerImages.Add("SMS Controller", Properties.Resources.SMSController);
            ControllerImages.Add("Genesis 3-Button Controller", Properties.Resources.GENController);
            ControllerImages.Add("Saturn Controller", Properties.Resources.SaturnController);

            ControllerImages.Add("Intellivision Controller", Properties.Resources.IntVController);
            ControllerImages.Add("ColecoVision Basic Controller", Properties.Resources.colecovisioncontroller);
            ControllerImages.Add("Atari 2600 Basic Controller", Properties.Resources.atari_controller);
            ControllerImages.Add("Atari 7800 ProLine Joystick Controller", Properties.Resources.A78Joystick);

            ControllerImages.Add("PC Engine Controller", Properties.Resources.PCEngineController);
			ControllerImages.Add("Commodore 64 Controller", Properties.Resources.C64Joystick);
            ControllerImages.Add("TI83 Controller", Properties.Resources.TI83_Controller);

            //ControllerImages.Add("PSP Controller", Properties.Resources); //TODO
		}

		const int MAXPLAYERS = 8;
		string ControllerType;

		private NewControllerConfig()
		{
			InitializeComponent();
		}

		delegate Control PanelCreator<T>(Dictionary<string, T> settings, List<string> buttons, Size size);

		Control CreateNormalPanel(Dictionary<string, string> settings, List<string> buttons, Size size)
		{
			var cp = new ControllerConfigPanel {Dock = DockStyle.Fill};
			cp.LoadSettings(settings, checkBoxAutoTab.Checked, buttons, size.Width, size.Height);
			return cp;
		}

		Control CreateAnalogPanel(Dictionary<string, Config.AnalogBind> settings, List<string> buttons, Size size)
		{
			var acp = new AnalogBindPanel(settings, buttons) { Dock = DockStyle.Fill };
			return acp;
		}

		static void LoadToPanel<T>(Control dest, string ControllerName, IEnumerable<string> ControllerButtons, Dictionary<string, Dictionary<string, T>> settingsblock, T defaultvalue, PanelCreator<T> createpanel)
		{
			Dictionary<string, T> settings;
			if (!settingsblock.TryGetValue(ControllerName, out settings))
			{
				settings = new Dictionary<string, T>();
				settingsblock[ControllerName] = settings;
			}
			// check to make sure that the settings object has all of the appropriate boolbuttons
			foreach (string button in ControllerButtons)
			{
				if (!settings.Keys.Contains(button))
					settings[button] = defaultvalue;
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
				dest.Controls.Add(createpanel(settings, null, dest.Size));
			}
			else
			{
				// create multiple player tabs
				var tt = new TabControl {Dock = DockStyle.Fill};
				dest.Controls.Add(tt);
				int pageidx = 0;
				for (int i = 1; i <= MAXPLAYERS; i++)
				{
					if (buckets[i].Count > 0)
					{
						tt.TabPages.Add("Player " + i);
						tt.TabPages[pageidx].Controls.Add(createpanel(settings, buckets[i], tt.Size));
						pageidx++;
					}
				}
				if (buckets[0].Count > 0)
				{
					if (Global.Emulator.SystemId == "C64") //This is a kludge, if there starts to be more exceptions to this pattern, we will need a more robust solution
					{
						tt.TabPages.Add("Keyboard");
					}
					else
					{
						tt.TabPages.Add("Console");
					}
					tt.TabPages[pageidx].Controls.Add(createpanel(settings, buckets[0], tt.Size));
					pageidx++;
				}
			}
		}

		private ControllerDefinition the_definition;
		

		public NewControllerConfig(ControllerDefinition def)
			: this()
		{
			the_definition = def;
			ControllerType = def.Name;
			SuspendLayout();
			LoadPanels();

			Text = def.Name + " Configuration";
			checkBoxUDLR.Checked = Global.Config.AllowUD_LR;
			checkBoxAutoTab.Checked = Global.Config.InputConfigAutoTab;

			SetControllerPicture(def.Name);
			ResumeLayout();
		}

		private void LoadPanels()
		{
			LoadToPanel(tabPage1, the_definition.Name, the_definition.BoolButtons, Global.Config.AllTrollers, "", CreateNormalPanel);
			LoadToPanel(tabPage2, the_definition.Name, the_definition.BoolButtons, Global.Config.AllTrollersAutoFire, "", CreateNormalPanel);
			LoadToPanel(tabPage3, the_definition.Name, the_definition.FloatControls, Global.Config.AllTrollersAnalog, new Config.AnalogBind("", 1.0f), CreateAnalogPanel);

			if (tabPage3.Controls.Count == 0)
			{
				tabControl1.TabPages.Remove(tabPage3);
			}
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
			else if (c is AnalogBindPanel)
				;// TODO
			else if (c.HasChildren)
				foreach (Control cc in c.Controls)
					SetAutoTab(cc, value);
		}

		static void Save(Control c)
		{
			if (c is ControllerConfigPanel)
				(c as ControllerConfigPanel).Save();
			else if (c is AnalogBindPanel)
				(c as AnalogBindPanel).Save();
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

		private static string ControlDefaultPath
		{
			get { return PathManager.MakeProgramRelativePath("defctrl.json"); }
		}

		private void buttonLoadDefaults_Click(object sender, EventArgs e)
		{
			RestoreDefaults();
			Close();
		}

		public void RestoreDefaults()
		{
			// this is not clever.  i'm going to replace it with something more clever

			ControlDefaults cd = new ControlDefaults();
			cd = ConfigService.Load(ControlDefaultPath, cd);
			Dictionary<string, string> settings;
			Dictionary<string, Config.AnalogBind> asettings;

			if (cd.AllTrollers.TryGetValue(ControllerType, out settings))
			{
				Global.Config.AllTrollers[ControllerType] = settings;
			}
			else
			{
				Global.Config.AllTrollers[ControllerType].Clear();
			}

			if (cd.AllTrollersAutoFire.TryGetValue(ControllerType, out settings))
			{
				Global.Config.AllTrollersAutoFire[ControllerType] = settings;
			}
			else
			{
				Global.Config.AllTrollersAutoFire[ControllerType].Clear();
			}

			if (cd.AllTrollersAnalog.TryGetValue(ControllerType, out asettings))
			{
				Global.Config.AllTrollersAnalog[ControllerType] = asettings;
			}
			else
			{
				Global.Config.AllTrollersAnalog[ControllerType].Clear();
			}
		}

		class ControlDefaults
		{
			public Dictionary<string, Dictionary<string, string>> AllTrollers = new Dictionary<string, Dictionary<string, string>>();
			public Dictionary<string, Dictionary<string, string>> AllTrollersAutoFire = new Dictionary<string, Dictionary<string, string>>();
			public Dictionary<string, Dictionary<string, Config.AnalogBind>> AllTrollersAnalog = new Dictionary<string, Dictionary<string, Config.AnalogBind>>();
		}

		public static void ConfigCheckAllControlDefaults(Config c)
		{
			if (c.AllTrollers.Count == 0 && c.AllTrollersAutoFire.Count == 0 && c.AllTrollersAnalog.Count == 0)
			{
				ControlDefaults cd = new ControlDefaults();
				cd = ConfigService.Load(ControlDefaultPath, cd);
				c.AllTrollers = cd.AllTrollers;
				c.AllTrollersAutoFire = cd.AllTrollersAutoFire;
				c.AllTrollersAnalog = cd.AllTrollersAnalog;
			}
		}
	}
}
