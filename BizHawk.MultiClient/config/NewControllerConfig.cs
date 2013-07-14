using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.MultiClient.config.ControllerConfig;

namespace BizHawk.MultiClient.config
{
	public partial class NewControllerConfig : Form
	{
		private NewControllerConfig()
		{
			InitializeComponent();
		}

		NewControllerConfigPanel normcontrls;
		NewControllerConfigPanel autofirecontrls;

		static void DoLoadSettings(NewControllerConfigPanel cp, ControllerDefinition def, Dictionary<string, Dictionary<string, string>> settingsblock)
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
			normcontrls = new NewControllerConfigPanel();
			normcontrls.Dock = DockStyle.Fill;
			tabPage1.Controls.Add(normcontrls);
			DoLoadSettings(normcontrls, def, Global.Config.AllTrollers);

			autofirecontrls = new NewControllerConfigPanel();
			autofirecontrls.Dock = DockStyle.Fill;
			tabPage2.Controls.Add(autofirecontrls);
			DoLoadSettings(autofirecontrls, def, Global.Config.AllTrollersAutoFire);

			label1.Text = "Currently Configuring: " + def.Name;
			checkBoxUDLR.Checked = Global.Config.AllowUD_LR;
			checkBoxAutoTab.Checked = Global.Config.InputConfigAutoTab;
			ResumeLayout();
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
