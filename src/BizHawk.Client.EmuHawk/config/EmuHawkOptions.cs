using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class EmuHawkOptions : Form
	{
		private readonly Action _autoFlushSaveRamTimerBumpCallback;

		private readonly Config _config;

		public EmuHawkOptions(Config config, Action autoFlushSaveRamTimerBumpCallback)
		{
			_autoFlushSaveRamTimerBumpCallback = autoFlushSaveRamTimerBumpCallback;
			_config = config;
			InitializeComponent();
		}

		public int AutosaveSaveRAMSeconds
		{
			get
			{
				if (AutosaveSRAMradioButton1.Checked)
				{
					return 5;
				}

				if (AutosaveSRAMradioButton2.Checked)
				{
					return 5 * 60;
				}

				return (int)AutosaveSRAMtextBox.Value;
			}
			set
			{
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
			RunInBackgroundCheckbox.Checked = _config.RunInBackground;
			AcceptBackgroundInputCheckbox.Checked = _config.AcceptBackgroundInput;
			AcceptBackgroundInputControllerOnlyCheckBox.Checked = _config.AcceptBackgroundInputControllerOnly;
			HandleAlternateKeyboardLayoutsCheckBox.Checked = _config.HandleAlternateKeyboardLayouts;
			NeverAskSaveCheckbox.Checked = _config.SuppressAskSave;
			cbMergeLAndRModifierKeys.Checked = _config.MergeLAndRModifierKeys;
			SingleInstanceModeCheckbox.Checked = _config.SingleInstanceMode;
			SingleInstanceModeCheckbox.Enabled = !OSTailoredCode.IsUnixHost;

			BackupSRamCheckbox.Checked = _config.BackupSaveram;
			AutosaveSRAMCheckbox.Checked = _config.AutosaveSaveRAM;
			groupBox2.Enabled = AutosaveSRAMCheckbox.Checked;
			AutosaveSaveRAMSeconds = _config.FlushSaveRamFrames / 60;
			FrameAdvSkipLagCheckbox.Checked = _config.SkipLagFrame;
			LuaDuringTurboCheckbox.Checked = _config.RunLuaDuringTurbo;
			cbMoviesOnDisk.Checked = _config.Movies.MoviesOnDisk;
			cbSkipWaterboxIntegrityChecks.Checked = _config.SkipWaterboxIntegrityChecks;
			NoMixedKeyPriorityCheckBox.Checked = _config.NoMixedInputHokeyOverride;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			if (cbMergeLAndRModifierKeys.Checked != _config.MergeLAndRModifierKeys)
			{
				var merging = cbMergeLAndRModifierKeys.Checked;
				var result = MessageBox.Show(
					this,
					text: $"Would you like to replace {(merging ? "LShift and RShift with Shift" : "Shift with LShift")},\nand the same for the other modifier keys,\nin existing keybinds for hotkeys and all systems' gamepads?",
					caption: "Rewrite keybinds now?",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question);
				if (result is DialogResult.Cancel) return;
				if (result is DialogResult.Yes) _config.ReplaceKeysInBindings(merging ? Input.ModifierKeyPreMap : Input.ModifierKeyInvPreMap);
			}

			_config.StartFullscreen = StartFullScreenCheckbox.Checked;
			_config.StartPaused = StartPausedCheckbox.Checked;
			_config.PauseWhenMenuActivated = PauseWhenMenuActivatedCheckbox.Checked;
			_config.ShowContextMenu = EnableContextMenuCheckbox.Checked;
			_config.RunInBackground = RunInBackgroundCheckbox.Checked;
			_config.AcceptBackgroundInput = AcceptBackgroundInputCheckbox.Checked;
			_config.AcceptBackgroundInputControllerOnly = AcceptBackgroundInputControllerOnlyCheckBox.Checked;
			_config.HandleAlternateKeyboardLayouts = HandleAlternateKeyboardLayoutsCheckBox.Checked;
			_config.SuppressAskSave = NeverAskSaveCheckbox.Checked;
			_config.MergeLAndRModifierKeys = cbMergeLAndRModifierKeys.Checked;
			_config.SingleInstanceMode = SingleInstanceModeCheckbox.Checked;

			_config.BackupSaveram = BackupSRamCheckbox.Checked;
			_config.AutosaveSaveRAM = AutosaveSRAMCheckbox.Checked;
			_config.FlushSaveRamFrames = AutosaveSaveRAMSeconds * 60;
			_autoFlushSaveRamTimerBumpCallback();

			_config.SkipLagFrame = FrameAdvSkipLagCheckbox.Checked;
			_config.RunLuaDuringTurbo = LuaDuringTurboCheckbox.Checked;
			_config.Movies.MoviesOnDisk = cbMoviesOnDisk.Checked;
			_config.SkipWaterboxIntegrityChecks = cbSkipWaterboxIntegrityChecks.Checked;
			_config.NoMixedInputHokeyOverride = NoMixedKeyPriorityCheckBox.Checked;

			Close();
			DialogResult = DialogResult.OK;
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			Close();
			DialogResult = DialogResult.Cancel;
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
