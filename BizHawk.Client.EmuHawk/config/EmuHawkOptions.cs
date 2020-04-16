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

		public int AutosaveSaveRAMSeconds
		{
			get
			{
				if (cbAutoSaveRAMFreq5s.Checked)
				{
					return 5;
				}

				if (AutoSaveRAMFreq5min.Checked)
				{
					return 5 * 60;
				}

				return (int)nudAutoSaveRAMFreqCustom.Value;
			}
			set
			{
				switch (value)
				{
					case 5:
						cbAutoSaveRAMFreq5s.Checked = true;
						nudAutoSaveRAMFreqCustom.Enabled = false;
						break;
					case 5 * 60:
						AutoSaveRAMFreq5min.Checked = true;
						nudAutoSaveRAMFreqCustom.Enabled = false;
						break;
					default:
						rbAutoSaveRAMFreqCustom.Checked = true;
						nudAutoSaveRAMFreqCustom.Enabled = true;
						break;
				}

				nudAutoSaveRAMFreqCustom.Value = value;
			}
		}

		private void GuiOptions_Load(object sender, EventArgs e)
		{
			cbStartInFS.Checked = _config.StartFullscreen;
			cbStartPaused.Checked = _config.StartPaused;
			cbMenusPauseEmulation.Checked = _config.PauseWhenMenuActivated;
			cbEnableContextMenu.Checked = _config.ShowContextMenu;
			cbSaveWindowPosition.Checked = _config.SaveWindowPosition;
			cbNoFocusEmulate.Checked = _config.RunInBackground;
			cbNoFocusInput.Checked = _config.AcceptBackgroundInput;
			cbNoFocusInputGamepadOnly.Checked = _config.AcceptBackgroundInputControllerOnly;
			switch (_config.HostInputMethod)
			{
				case EHostInputMethod.OpenTK:
					rbInputMethodOpenTK.Checked = true;
					break;
				case EHostInputMethod.DirectInput:
					rbInputMethodDirectInput.Checked = true;
					break;
				default:
					throw new InvalidOperationException();
			}
			cbNonQWERTY.Checked = _config.HandleAlternateKeyboardLayouts;
			cbNeverAskForSave.Checked = _config.SuppressAskSave;
			cbSingleInstance.Checked = _config.SingleInstanceMode;

			cbBackupSaveRAM.Checked = _config.BackupSaveram;
			cbAutoSaveRAM.Checked = _config.AutosaveSaveRAM;
			grpAutoSaveRAM.Enabled = cbAutoSaveRAM.Checked;
			AutosaveSaveRAMSeconds = _config.FlushSaveRamFrames / 60;
			cbFrameAdvPastLag.Checked = _config.SkipLagFrame;
			cbRunLuaDuringTurbo.Checked = _config.RunLuaDuringTurbo;
			cbMoviesOnDisk.Checked = _config.MoviesOnDisk;
			cbMoviesInAWE.Checked = _config.MoviesInAwe;

			switch (_config.LuaEngine)
			{
				case ELuaEngine.LuaPlusLuaInterface:
					rbLuaInterface.Checked = true;
					break;
				case ELuaEngine.NLuaPlusKopiLua:
					rbKopiLua.Checked = true;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void btnDialogOK_Click(object sender, EventArgs e)
		{
			_config.StartFullscreen = cbStartInFS.Checked;
			_config.StartPaused = cbStartPaused.Checked;
			_config.PauseWhenMenuActivated = cbMenusPauseEmulation.Checked;
			_config.ShowContextMenu = cbEnableContextMenu.Checked;
			_config.SaveWindowPosition = cbSaveWindowPosition.Checked;
			_config.RunInBackground = cbNoFocusEmulate.Checked;
			_config.AcceptBackgroundInput = cbNoFocusInput.Checked;
			_config.AcceptBackgroundInputControllerOnly = cbNoFocusInputGamepadOnly.Checked;
			_config.HostInputMethod = grpInputMethod.Tracker.GetSelectionTagAs<EHostInputMethod>() ?? throw new InvalidOperationException();
			_config.HandleAlternateKeyboardLayouts = cbNonQWERTY.Checked;
			_config.SuppressAskSave = cbNeverAskForSave.Checked;
			_config.SingleInstanceMode = cbSingleInstance.Checked;

			_config.BackupSaveram = cbBackupSaveRAM.Checked;
			_config.AutosaveSaveRAM = cbAutoSaveRAM.Checked;
			_config.FlushSaveRamFrames = AutosaveSaveRAMSeconds * 60;
			if (_mainForm.AutoFlushSaveRamIn > _config.FlushSaveRamFrames)
			{
				_mainForm.AutoFlushSaveRamIn = _config.FlushSaveRamFrames;
			}

			_config.SkipLagFrame = cbFrameAdvPastLag.Checked;
			_config.RunLuaDuringTurbo = cbRunLuaDuringTurbo.Checked;
			_config.MoviesOnDisk = cbMoviesOnDisk.Checked;
			_config.MoviesInAwe = cbMoviesInAWE.Checked;

			var prevLuaEngine = _config.LuaEngine;
			_config.LuaEngine = grpLuaEngine.Tracker.GetSelectionTagAs<ELuaEngine>() ?? throw new InvalidOperationException();

			_mainForm.AddOnScreenMessage("Custom configurations saved.");
			if (prevLuaEngine != _config.LuaEngine)
			{
				_mainForm.AddOnScreenMessage("Restart emulator for Lua change to take effect");
			}

			Close();
			DialogResult = DialogResult.OK;
		}

		private void btnDialogCancel_Click(object sender, EventArgs e)
		{
			Close();
			DialogResult = DialogResult.Cancel;
			_mainForm.AddOnScreenMessage("Customizing aborted.");
		}

		private void cbNoFocusInput_CheckedChanged(object sender, EventArgs e)
		{
			cbNoFocusInputGamepadOnly.Enabled = cbNoFocusInput.Checked;
		}

		private void cbAutoSaveRAM_CheckedChanged(object sender, EventArgs e)
		{
			grpAutoSaveRAM.Enabled = cbAutoSaveRAM.Checked;
		}

		private void rbAutoSaveRAMFreqCustom_CheckedChanged(object sender, EventArgs e)
		{
			nudAutoSaveRAMFreqCustom.Enabled = rbAutoSaveRAMFreqCustom.Checked;
		}
	}
}
