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
		public EmuHawkOptions()
		{
			InitializeComponent();
		}

		private void GuiOptions_Load(object sender, EventArgs e)
		{
			StartFullScreenCheckbox.Checked = Global.Config.StartFullscreen;
			StartPausedCheckbox.Checked = Global.Config.StartPaused;
			PauseWhenMenuActivatedCheckbox.Checked = Global.Config.PauseWhenMenuActivated;
			EnableContextMenuCheckbox.Checked = Global.Config.ShowContextMenu;
			SaveWindowPositionCheckbox.Checked = Global.Config.SaveWindowPosition;
			RunInBackgroundCheckbox.Checked = Global.Config.RunInBackground;
			AcceptBackgroundInputCheckbox.Checked = Global.Config.AcceptBackgroundInput;
			NeverAskSaveCheckbox.Checked = Global.Config.SupressAskSave;
			SingleInstanceModeCheckbox.Checked = Global.Config.SingleInstanceMode;

			BackupSRamCheckbox.Checked = Global.Config.BackupSaveram;
			FrameAdvSkipLagCheckbox.Checked = Global.Config.SkipLagFrame;
			LogWindowAsConsoleCheckbox.Checked = Global.Config.WIN32_CONSOLE;
			LuaDuringTurboCheckbox.Checked = Global.Config.RunLuaDuringTurbo;
			cbMoviesOnDisk.Checked = Global.Config.MoviesOnDisk;
			cbMoviesInAWE.Checked = Global.Config.MoviesInAWE;

			if (LogConsole.ConsoleVisible)
			{
				LogWindowAsConsoleCheckbox.Enabled = false;
				toolTip1.SetToolTip(
					LogWindowAsConsoleCheckbox,
					"This can not be changed while the log window is open. I know, it's annoying.");
			}
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			Global.Config.StartFullscreen = StartFullScreenCheckbox.Checked;
			Global.Config.StartPaused = StartPausedCheckbox.Checked;
			Global.Config.PauseWhenMenuActivated = PauseWhenMenuActivatedCheckbox.Checked;
			Global.Config.ShowContextMenu = EnableContextMenuCheckbox.Checked;
			Global.Config.SaveWindowPosition = SaveWindowPositionCheckbox.Checked;
			Global.Config.RunInBackground = RunInBackgroundCheckbox.Checked;
			Global.Config.AcceptBackgroundInput = AcceptBackgroundInputCheckbox.Checked;
			Global.Config.SupressAskSave = NeverAskSaveCheckbox.Checked;
			Global.Config.SingleInstanceMode = SingleInstanceModeCheckbox.Checked;

			Global.Config.BackupSaveram = BackupSRamCheckbox.Checked;
			Global.Config.SkipLagFrame = FrameAdvSkipLagCheckbox.Checked;
			Global.Config.WIN32_CONSOLE = LogWindowAsConsoleCheckbox.Checked;
			Global.Config.RunLuaDuringTurbo = LuaDuringTurboCheckbox.Checked;
			Global.Config.MoviesOnDisk = cbMoviesOnDisk.Checked;
			Global.Config.MoviesInAWE = cbMoviesInAWE.Checked;

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
