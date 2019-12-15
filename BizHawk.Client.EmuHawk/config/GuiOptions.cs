using System;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class EmuHawkOptions : Form
	{
		public int AutosaveSaveRAMSeconds {
			get {
				if (AutosaveSRAMradioButton1.Checked)
					return 5;
				if (AutosaveSRAMradioButton2.Checked)
					return 5 * 60;
				return (int)AutosaveSRAMtextBox.Value;
			}
			set {
				switch (value)
				{
					case 5:
						AutosaveSRAMradioButton1.Checked = true;
						AutosaveSRAMtextBox.Enabled = false;
						break;
					case 5 * 60:
						AutosaveSRAMradioButton2.Checked = true;
						AutosaveSRAMtextBox.Enabled = false;
						break;
					default:
						AutosaveSRAMradioButton3.Checked = true;
						AutosaveSRAMtextBox.Enabled = true;
						break;
				}
				AutosaveSRAMtextBox.Value = value;
			}
		}

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
			AcceptBackgroundInputControllerOnlyCheckBox.Checked = Global.Config.AcceptBackgroundInputControllerOnly;
			HandleAlternateKeyboardLayoutsCheckBox.Checked = Global.Config.HandleAlternateKeyboardLayouts;
			NeverAskSaveCheckbox.Checked = Global.Config.SuppressAskSave;
			SingleInstanceModeCheckbox.Checked = Global.Config.SingleInstanceMode;

			BackupSRamCheckbox.Checked = Global.Config.BackupSaveram;
			AutosaveSRAMCheckbox.Checked = Global.Config.AutosaveSaveRAM;
			groupBox2.Enabled = AutosaveSRAMCheckbox.Checked;
			AutosaveSaveRAMSeconds = Global.Config.FlushSaveRamFrames / 60;
			FrameAdvSkipLagCheckbox.Checked = Global.Config.SkipLagFrame;
			LogWindowAsConsoleCheckbox.Checked = Global.Config.WIN32_CONSOLE;
			LuaDuringTurboCheckbox.Checked = Global.Config.RunLuaDuringTurbo;
			cbMoviesOnDisk.Checked = Global.Config.MoviesOnDisk;
			cbMoviesInAWE.Checked = Global.Config.MoviesInAWE;

			switch (Global.Config.LuaEngine)
			{
				case Config.ELuaEngine.LuaPlusLuaInterface:
					LuaInterfaceRadio.Checked = true;
					break;
				case Config.ELuaEngine.NLuaPlusKopiLua:
					NLuaRadio.Checked = true;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

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
			Global.Config.AcceptBackgroundInputControllerOnly = AcceptBackgroundInputControllerOnlyCheckBox.Checked;
			Global.Config.HandleAlternateKeyboardLayouts = HandleAlternateKeyboardLayoutsCheckBox.Checked;
			Global.Config.SuppressAskSave = NeverAskSaveCheckbox.Checked;
			Global.Config.SingleInstanceMode = SingleInstanceModeCheckbox.Checked;

			Global.Config.BackupSaveram = BackupSRamCheckbox.Checked;
			Global.Config.AutosaveSaveRAM = AutosaveSRAMCheckbox.Checked;
			Global.Config.FlushSaveRamFrames = AutosaveSaveRAMSeconds * 60;
			if (GlobalWin.MainForm.AutoFlushSaveRamIn > Global.Config.FlushSaveRamFrames)
				GlobalWin.MainForm.AutoFlushSaveRamIn = Global.Config.FlushSaveRamFrames;
			Global.Config.SkipLagFrame = FrameAdvSkipLagCheckbox.Checked;
			Global.Config.WIN32_CONSOLE = LogWindowAsConsoleCheckbox.Checked;
			Global.Config.RunLuaDuringTurbo = LuaDuringTurboCheckbox.Checked;
			Global.Config.MoviesOnDisk = cbMoviesOnDisk.Checked;
			Global.Config.MoviesInAWE = cbMoviesInAWE.Checked;

			var prevLuaEngine = Global.Config.LuaEngine;
			if (LuaInterfaceRadio.Checked) Global.Config.LuaEngine = Config.ELuaEngine.LuaPlusLuaInterface;
			else if (NLuaRadio.Checked) Global.Config.LuaEngine = Config.ELuaEngine.NLuaPlusKopiLua;

			Close();
			DialogResult = DialogResult.OK;
			GlobalWin.OSD.AddMessage("Custom configurations saved.");
			if (prevLuaEngine != Global.Config.LuaEngine) GlobalWin.OSD.AddMessage("Restart emulator for Lua change to take effect");
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			Close();
			DialogResult = DialogResult.Cancel;
			GlobalWin.OSD.AddMessage("Customizing aborted.");
		}

		private void AcceptBackgroundInputCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			AcceptBackgroundInputControllerOnlyCheckBox.Enabled = AcceptBackgroundInputCheckbox.Checked;
		}

		private void AutosaveSRAMCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			groupBox2.Enabled = AutosaveSRAMCheckbox.Checked;
		}

		private void AutosaveSRAMradioButton3_CheckedChanged(object sender, EventArgs e)
		{
			AutosaveSRAMtextBox.Enabled = AutosaveSRAMradioButton3.Checked;
		}
	}
}
