using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.Common;

using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.SNES;

namespace BizHawk.Client.EmuHawk
{
	public partial class ProfileConfig : Form
	{
		// TODO:
		// Save the profile selected to Global.Config (Casual is the default)
		// Default the dropdown to the current profile selected instead of empty
		// Put save logic in the Ok button click, not the profile selected change event!

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
				//Save to config.ini
			}
			
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
				Global.Config.BackupSavestates = false;

				Global.Config.RewindEnabledLarge = false;
				Global.Config.RewindEnabledMedium = false;
				Global.Config.RewindEnabledSmall = true;

				// N64
				var n64Settings = GetN64SyncSettings();
				n64Settings.RspType = N64SyncSettings.RSPTYPE.Rsp_Hle;
				n64Settings.CoreType = N64SyncSettings.CORETYPE.Dynarec;
				Global.Config.N64UseCircularAnalogConstraint = true;
				PutN64SyncSettings(n64Settings);

				// SNES
				var snesSettings = GetSnesSyncSettings();
				snesSettings.Profile = "Performance";
				PutSnesSyncSettings(snesSettings);

			}
			else if (ProfileSelectComboBox.SelectedIndex == 2) //Long Plays
			{
				DisplayProfileSettingBoxes(false);
				Global.Config.SaveLargeScreenshotWithStates = false;
				Global.Config.SaveScreenshotWithStates = false;
				Global.Config.AllowUD_LR = false;
				Global.Config.BackupSavestates = true;

				Global.Config.RewindEnabledLarge = false;
				Global.Config.RewindEnabledMedium = false;
				Global.Config.RewindEnabledSmall = true;

				// N64
				var n64Settings = GetN64SyncSettings();
				n64Settings.RspType = N64SyncSettings.RSPTYPE.Rsp_Z64_hlevideo;
				n64Settings.CoreType = N64SyncSettings.CORETYPE.Pure_Interpret;
				Global.Config.N64UseCircularAnalogConstraint = true;
				PutN64SyncSettings(n64Settings);

				// SNES
				var snesSettings = GetSnesSyncSettings();
				snesSettings.Profile = "Compatibility";
				PutSnesSyncSettings(snesSettings);
			}
			else if (ProfileSelectComboBox.SelectedIndex == 1) //TAS
			{
				DisplayProfileSettingBoxes(false);

				// General
				Global.Config.SaveLargeScreenshotWithStates = true;
				Global.Config.SaveScreenshotWithStates = true;
				Global.Config.AllowUD_LR = true;
				Global.Config.BackupSavestates = true;

				// Rewind
				Global.Config.RewindEnabledLarge = false;
				Global.Config.RewindEnabledMedium = false;
				Global.Config.RewindEnabledSmall = false;

				// N64
				var n64Settings = GetN64SyncSettings();
				n64Settings.RspType = N64SyncSettings.RSPTYPE.Rsp_Z64_hlevideo;
				n64Settings.CoreType = N64SyncSettings.CORETYPE.Pure_Interpret;
				Global.Config.N64UseCircularAnalogConstraint = false;
				PutN64SyncSettings(n64Settings);

				// SNES
				var snesSettings = GetSnesSyncSettings();
				snesSettings.Profile = "Compatibility";
				PutSnesSyncSettings(snesSettings);

			}
			else if (ProfileSelectComboBox.SelectedIndex == 3) //custom
			{
				//Disabled for now
				//DisplayProfileSettingBoxes(true);
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

		#region Core specific Config setting helpers

		// Unfortunatley there is not yet a generic way to match a sync setting object with its core implementation, so we need a bunch of these
		private static N64SyncSettings GetN64SyncSettings()
		{
			if (Global.Emulator is N64)
			{
				return (N64SyncSettings)Global.Emulator.GetSyncSettings();
			}
			else
			{
				return (N64SyncSettings)Global.Config.GetCoreSyncSettings<N64>();
			}
		}

		private static void PutN64SyncSettings(N64SyncSettings s)
		{
			if (Global.Emulator is N64)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(s);
			}
			else
			{
				Global.Config.PutCoreSyncSettings<N64>(s);
			}
		}

		private static LibsnesCore.SnesSyncSettings GetSnesSyncSettings()
		{
			if (Global.Emulator is LibsnesCore)
			{
				return (LibsnesCore.SnesSyncSettings)Global.Emulator.GetSyncSettings();
			}
			else
			{
				return (LibsnesCore.SnesSyncSettings)Global.Config.GetCoreSyncSettings<LibsnesCore>();
			}
		}

		private static void PutSnesSyncSettings(LibsnesCore.SnesSyncSettings s)
		{
			if (Global.Emulator is LibsnesCore)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(s);
			}
			else
			{
				Global.Config.PutCoreSyncSettings<LibsnesCore>(s);
			}
		}
		#endregion
	}
}
