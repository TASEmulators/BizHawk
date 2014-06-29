using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class ProfileConfig : Form
	{
		public ProfileConfig()
		{
			InitializeComponent();
		}

		private void ProfileConfig_Load(object sender, EventArgs e)
		{
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			/* Saving logic goes here */
			if (ProfileSelectComboBox.SelectedIndex == 3)
			{
				//If custom profile, check all the checkboxes
			}
			//Save to config.ini
			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void ProfileSelectComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (ProfileSelectComboBox.SelectedIndex == 0) //Casual Gaming
			{
				DisplayProfileSettingBoxes(false);
				Global.Config.SaveLargeScreenshotWithStates = false;
				Global.Config.SaveScreenshotWithStates = false;
				Global.Config.AllowUD_LR = false;
			}
			else if (ProfileSelectComboBox.SelectedIndex == 1) //TAS
			{
				DisplayProfileSettingBoxes(false);
				Global.Config.SaveLargeScreenshotWithStates = true;
				Global.Config.SaveScreenshotWithStates = true;
				Global.Config.AllowUD_LR = true;
			}
			else if (ProfileSelectComboBox.SelectedIndex == 2) //Long Plays
			{
				DisplayProfileSettingBoxes(false);
				Global.Config.SaveLargeScreenshotWithStates = false;
				Global.Config.SaveScreenshotWithStates = false;
				Global.Config.AllowUD_LR = false;
			}
			else if (ProfileSelectComboBox.SelectedIndex == 3) //custom
			{
				DisplayProfileSettingBoxes(true);
			}
		}

		private void DisplayProfileSettingBoxes(bool cProfile)
		{
			if (cProfile == true)
			{
				ProfileDialogHelpTexBox.Location = new Point(217, 12);
				ProfileDialogHelpTexBox.Size = new Size(165, 126);
				SaveScreenshotStatesCheckBox.Visible = true;
				SaveLargeScreenshotStatesCheckBox.Visible = true;
				AllowUDLRCheckBox.Visible = true;
				GeneralOptionsLabel.Visible = true;
			}
			else if (cProfile == false)
			{
				ProfileDialogHelpTexBox.Location = new Point(184, 12);
				ProfileDialogHelpTexBox.Size = new Size(198, 126);
				ProfileDialogHelpTexBox.Text = "Options: \r\nCasual Gaming - All about performance! \r\n\nTool-Assisted Speedruns - Maximum Accuracy! \r\n\nLongplays - Stability is the key!";
				SaveScreenshotStatesCheckBox.Visible = false;
				SaveLargeScreenshotStatesCheckBox.Visible = false;
				AllowUDLRCheckBox.Visible = false;
				GeneralOptionsLabel.Visible = false;
			}
		}

		private void SaveScreenshotStatesCheckBox_MouseHover(object sender, EventArgs e)
		{
			ProfileDialogHelpTexBox.Text = "Save Screenshot with Savestates: \r\n * Required for TASing \r\n * Not Recommended for \r\n   Longplays or Casual Gaming";
		}
		private void SaveLargeScreenshotStatesCheckBox_MouseHover(object sender, EventArgs e)
		{
			ProfileDialogHelpTexBox.Text = "Save Large Screenshot With States: \r\n * Required for TASing \r\n * Not Recommended for \r\n   Longplays or Casual Gaming";
		}
		private void AllowUDLRCheckBox_MouseHover(object sender, EventArgs e)
		{
			ProfileDialogHelpTexBox.Text = "All Up+Down or Left+Right: \r\n * Useful for TASing \r\n * Unchecked for Casual Gaming \r\n * Unknown for longplays";
		}
	}
}
