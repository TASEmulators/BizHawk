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
	public partial class EmuHawkOptions : Form
	{
		// TODO: make sure these options are cleaned out of the mainform context menu
		// TODO: put this in the context menu
		public EmuHawkOptions()
		{
			InitializeComponent();
		}

		private void GuiOptions_Load(object sender, EventArgs e)
		{
			StartPausedCheckbox.Checked = Global.Config.StartPaused;
			PauseWhenMenuActivatedCheckbox.Checked = Global.Config.PauseWhenMenuActivated;
			EnableContextMenuCheckbox.Checked = Global.Config.ShowContextMenu;
			SaveWindowPositionCheckbox.Checked = Global.Config.SaveWindowPosition;
			ShowMenuInFullScreenCheckbox.Checked = Global.Config.ShowMenuInFullscreen;
			RunInBackgroundCheckbox.Checked = Global.Config.RunInBackground;
			AcceptBackgroundInputCheckbox.Checked = Global.Config.AcceptBackgroundInput;
			NeverAskSaveCheckbox.Checked = Global.Config.SupressAskSave;
			SingleInstanceModeCheckbox.Checked = Global.Config.SingleInstanceMode;
			LogWindowAsConsoleCheckbox.Checked = Global.Config.WIN32_CONSOLE;

			BackupSavestatesCheckbox.Checked = Global.Config.BackupSavestates;
			ScreenshotInStatesCheckbox.Checked = Global.Config.SaveScreenshotWithStates;
			BackupSRamCheckbox.Checked = Global.Config.BackupSaveram;
			FrameAdvSkipLagCheckbox.Checked = Global.Config.SkipLagFrame;

			if (LogConsole.ConsoleVisible)
			{
				LogWindowAsConsoleCheckbox.Enabled = false;
				toolTip1.SetToolTip(
					LogWindowAsConsoleCheckbox,
					"This can not be chaned while the log window is open");
			}
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			Global.Config.StartPaused = StartPausedCheckbox.Checked;
			Global.Config.PauseWhenMenuActivated = PauseWhenMenuActivatedCheckbox.Checked;
			Global.Config.ShowContextMenu = EnableContextMenuCheckbox.Checked;
			Global.Config.SaveWindowPosition = SaveWindowPositionCheckbox.Checked;
			Global.Config.ShowMenuInFullscreen = ShowMenuInFullScreenCheckbox.Checked;
			Global.Config.RunInBackground = RunInBackgroundCheckbox.Checked;
			Global.Config.AcceptBackgroundInput = AcceptBackgroundInputCheckbox.Checked;
			Global.Config.SupressAskSave = NeverAskSaveCheckbox.Checked;
			Global.Config.SingleInstanceMode = SingleInstanceModeCheckbox.Checked;
			Global.Config.WIN32_CONSOLE = LogWindowAsConsoleCheckbox.Checked;

			Global.Config.BackupSavestates = BackupSavestatesCheckbox.Checked;
			Global.Config.SaveScreenshotWithStates = ScreenshotInStatesCheckbox.Checked;
			Global.Config.BackupSaveram = BackupSRamCheckbox.Checked;
			Global.Config.SkipLagFrame = FrameAdvSkipLagCheckbox.Checked;

			// Make sure this gets applied immediately
			if (GlobalWin.MainForm.IsInFullscreen)
			{
				GlobalWin.MainForm.MainMenuStrip.Visible = Global.Config.ShowMenuInFullscreen;
			}

			Close();
			DialogResult = DialogResult.OK;
			GlobalWin.OSD.AddMessage("Custom configurations saved.");
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			Close();
			DialogResult = DialogResult.Cancel;
			GlobalWin.OSD.AddMessage("Customizing aborted.");
		}
	}
}
