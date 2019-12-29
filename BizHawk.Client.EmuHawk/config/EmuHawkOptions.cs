using System;
using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class EmuHawkOptions : Form
	{
		private readonly MainForm _mainForm;
		private readonly Config _config;

		public EmuHawkOptions(MainForm mainForm, Config config)
		{
			_mainForm = mainForm;
			_config = config;
			InitializeComponent();
		}

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

		private void GuiOptions_Load(object sender, EventArgs e)
		{
			StartFullScreenCheckbox.Checked = _config.StartFullscreen;
			StartPausedCheckbox.Checked = _config.StartPaused;
			PauseWhenMenuActivatedCheckbox.Checked = _config.PauseWhenMenuActivated;
			EnableContextMenuCheckbox.Checked = _config.ShowContextMenu;
			SaveWindowPositionCheckbox.Checked = _config.SaveWindowPosition;
			RunInBackgroundCheckbox.Checked = _config.RunInBackground;
			AcceptBackgroundInputCheckbox.Checked = _config.AcceptBackgroundInput;
			AcceptBackgroundInputControllerOnlyCheckBox.Checked = _config.AcceptBackgroundInputControllerOnly;
			HandleAlternateKeyboardLayoutsCheckBox.Checked = _config.HandleAlternateKeyboardLayouts;
			NeverAskSaveCheckbox.Checked = _config.SuppressAskSave;
			SingleInstanceModeCheckbox.Checked = _config.SingleInstanceMode;

			BackupSRamCheckbox.Checked = _config.BackupSaveram;
			AutosaveSRAMCheckbox.Checked = _config.AutosaveSaveRAM;
			groupBox2.Enabled = AutosaveSRAMCheckbox.Checked;
			AutosaveSaveRAMSeconds = _config.FlushSaveRamFrames / 60;
			FrameAdvSkipLagCheckbox.Checked = _config.SkipLagFrame;
			LuaDuringTurboCheckbox.Checked = _config.RunLuaDuringTurbo;
			cbMoviesOnDisk.Checked = _config.MoviesOnDisk;
			cbMoviesInAWE.Checked = _config.MoviesInAWE;

			switch (_config.LuaEngine)
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
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			_config.StartFullscreen = StartFullScreenCheckbox.Checked;
			_config.StartPaused = StartPausedCheckbox.Checked;
			_config.PauseWhenMenuActivated = PauseWhenMenuActivatedCheckbox.Checked;
			_config.ShowContextMenu = EnableContextMenuCheckbox.Checked;
			_config.SaveWindowPosition = SaveWindowPositionCheckbox.Checked;
			_config.RunInBackground = RunInBackgroundCheckbox.Checked;
			_config.AcceptBackgroundInput = AcceptBackgroundInputCheckbox.Checked;
			_config.AcceptBackgroundInputControllerOnly = AcceptBackgroundInputControllerOnlyCheckBox.Checked;
			_config.HandleAlternateKeyboardLayouts = HandleAlternateKeyboardLayoutsCheckBox.Checked;
			_config.SuppressAskSave = NeverAskSaveCheckbox.Checked;
			_config.SingleInstanceMode = SingleInstanceModeCheckbox.Checked;

			_config.BackupSaveram = BackupSRamCheckbox.Checked;
			_config.AutosaveSaveRAM = AutosaveSRAMCheckbox.Checked;
			_config.FlushSaveRamFrames = AutosaveSaveRAMSeconds * 60;
			if (_mainForm.AutoFlushSaveRamIn > _config.FlushSaveRamFrames)
				_mainForm.AutoFlushSaveRamIn = _config.FlushSaveRamFrames;
			_config.SkipLagFrame = FrameAdvSkipLagCheckbox.Checked;
			_config.RunLuaDuringTurbo = LuaDuringTurboCheckbox.Checked;
			_config.MoviesOnDisk = cbMoviesOnDisk.Checked;
			_config.MoviesInAWE = cbMoviesInAWE.Checked;

			var prevLuaEngine = _config.LuaEngine;
			if (LuaInterfaceRadio.Checked) _config.LuaEngine = Config.ELuaEngine.LuaPlusLuaInterface;
			else if (NLuaRadio.Checked) _config.LuaEngine = Config.ELuaEngine.NLuaPlusKopiLua;

			Close();
			DialogResult = DialogResult.OK;
			_mainForm.AddOnScreenMessage("Custom configurations saved.");
			if (prevLuaEngine != _config.LuaEngine)
			{
				_mainForm.AddOnScreenMessage("Restart emulator for Lua change to take effect");
			}
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			Close();
			DialogResult = DialogResult.Cancel;
			_mainForm.AddOnScreenMessage("Customizing aborted.");
		}

		private void AcceptBackgroundInputCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			AcceptBackgroundInputControllerOnlyCheckBox.Enabled = AcceptBackgroundInputCheckbox.Checked;
		}

		private void AutosaveSRAMCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			groupBox2.Enabled = AutosaveSRAMCheckbox.Checked;
		}

		private void AutosaveSRAMRadioButton3_CheckedChanged(object sender, EventArgs e)
		{
			AutosaveSRAMtextBox.Enabled = AutosaveSRAMradioButton3.Checked;
		}
	}
}
