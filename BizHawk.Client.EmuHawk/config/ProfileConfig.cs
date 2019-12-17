using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Atari.Atari2600;

namespace BizHawk.Client.EmuHawk
{
	public partial class ProfileConfig : Form
	{
		private readonly MainForm _mainForm;
		private readonly IEmulator _emulator;
		private readonly Config _config;

		public ProfileConfig(
			MainForm mainForm,
			IEmulator emulator,
			Config config)
		{
			_mainForm = mainForm;
			_emulator = emulator;
			_config = config;
			InitializeComponent();
		}

		private void ProfileConfig_Load(object sender, EventArgs e)
		{
			if (!VersionInfo.DeveloperBuild)
			{
				ProfileSelectComboBox.Items.Remove("Custom Profile");
			}

			switch (_config.SelectedProfile)
			{
				default:
				case Config.ClientProfile.Custom: // For now
				case Config.ClientProfile.Casual:
					ProfileSelectComboBox.SelectedItem = "Casual Gaming";
					break;
				case Config.ClientProfile.Longplay:
					ProfileSelectComboBox.SelectedItem = "Longplays";
					break;
				case Config.ClientProfile.Tas:
					ProfileSelectComboBox.SelectedItem = "Tool-assisted Speedruns";
					break;
				case Config.ClientProfile.N64Tas:
					ProfileSelectComboBox.SelectedItem = "N64 Tool-assisted Speedruns";
					break;
			}

			AutoCheckForUpdates.Checked = _config.Update_AutoCheckEnabled;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			switch (ProfileSelectComboBox.SelectedItem.ToString())
			{
				default:
				case "Custom Profile": // For now
				case "Casual Gaming":
					_config.SelectedProfile = Config.ClientProfile.Casual;
					break;
				case "Longplays":
					_config.SelectedProfile = Config.ClientProfile.Longplay;
					break;
				case "Tool-assisted Speedruns":
					_config.SelectedProfile = Config.ClientProfile.Tas;
					break;
				case "N64 Tool-assisted Speedruns":
					_config.SelectedProfile = Config.ClientProfile.N64Tas;
					break;
			}

			if (_config.SelectedProfile == Config.ClientProfile.Casual)
			{
				DisplayProfileSettingBoxes(false);
				_config.NoLowResLargeScreenshotWithStates = false;
				_config.SaveScreenshotWithStates = false;
				_config.AllowUD_LR = false;
				_config.BackupSavestates = false;

				_config.SaveStateCompressionLevelNormal = 0;
				_config.RewindEnabledLarge = false;
				_config.RewindEnabledMedium = false;
				_config.RewindEnabledSmall = true;
				_config.SkipLagFrame = false;

				// N64
				var n64Settings = GetSyncSettings<N64, N64SyncSettings>();
				n64Settings.Rsp = N64SyncSettings.RspType.Rsp_Hle;
				n64Settings.Core = N64SyncSettings.CoreType.Interpret;
				_config.N64UseCircularAnalogConstraint = true;
				PutSyncSettings<N64>(n64Settings);

				// SNES
				_config.SNES_InSnes9x = true;

				// Genesis
				var genesisSettings = GetSyncSettings<GPGX, GPGX.GPGXSyncSettings>();
				genesisSettings.Region = LibGPGX.Region.Autodetect;
				PutSyncSettings<GPGX>(genesisSettings);

				// SMS
				var smsSettings = GetSyncSettings<SMS, SMS.SMSSyncSettings>();
				smsSettings.UseBIOS = true;
				PutSyncSettings<SMS>(smsSettings);

				// Coleco
				var colecoSettings = GetSyncSettings<ColecoVision, ColecoVision.ColecoSyncSettings>();
				colecoSettings.SkipBiosIntro = false;
				PutSyncSettings<ColecoVision>(colecoSettings);

				// A2600
				var a2600Settings = GetSyncSettings<Atari2600, Atari2600.A2600SyncSettings>();
				a2600Settings.FastScBios = true;
				a2600Settings.LeftDifficulty = false;
				a2600Settings.RightDifficulty = false;
				PutSyncSettings<Atari2600>(a2600Settings);

				// NES
				_config.NES_InQuickNES = true;
			}
			else if (_config.SelectedProfile == Config.ClientProfile.Longplay)
			{
				DisplayProfileSettingBoxes(false);
				_config.NoLowResLargeScreenshotWithStates = false;
				_config.SaveScreenshotWithStates = false;
				_config.AllowUD_LR = false;
				_config.BackupSavestates = false;
				_config.SkipLagFrame = false;
				_config.SaveStateCompressionLevelNormal = 5;

				_config.RewindEnabledLarge = false;
				_config.RewindEnabledMedium = false;
				_config.RewindEnabledSmall = true;

				// N64
				var n64Settings = GetSyncSettings<N64, N64SyncSettings>();
				n64Settings.Core = N64SyncSettings.CoreType.Pure_Interpret;
				_config.N64UseCircularAnalogConstraint = true;
				PutSyncSettings<N64>(n64Settings);

				// SNES
				_config.SNES_InSnes9x = false;

				// Genesis
				var genesisSettings = GetSyncSettings<GPGX, GPGX.GPGXSyncSettings>();
				genesisSettings.Region = LibGPGX.Region.Autodetect;
				PutSyncSettings<GPGX>(genesisSettings);

				// SMS
				var smsSettings = GetSyncSettings<SMS, SMS.SMSSyncSettings>();
				smsSettings.UseBIOS = true;
				PutSyncSettings<SMS>(smsSettings);

				// Coleco
				var colecoSettings = GetSyncSettings<ColecoVision, ColecoVision.ColecoSyncSettings>();
				colecoSettings.SkipBiosIntro = false;
				PutSyncSettings<ColecoVision>(colecoSettings);

				// A2600
				var a2600Settings = GetSyncSettings<Atari2600, Atari2600.A2600SyncSettings>();
				a2600Settings.FastScBios = false;
				a2600Settings.LeftDifficulty = true;
				a2600Settings.RightDifficulty = true;
				PutSyncSettings<Atari2600>(a2600Settings);

				// NES
				_config.NES_InQuickNES = true;
			}
			else if (_config.SelectedProfile == Config.ClientProfile.Tas)
			{
				DisplayProfileSettingBoxes(false);

				// General
				_config.NoLowResLargeScreenshotWithStates = false;
				_config.SaveScreenshotWithStates = true;
				_config.AllowUD_LR = true;
				_config.BackupSavestates = true;
				_config.SkipLagFrame = false;
				_config.SaveStateCompressionLevelNormal = 5;

				// Rewind
				_config.RewindEnabledLarge = false;
				_config.RewindEnabledMedium = false;
				_config.RewindEnabledSmall = false;

				// N64
				var n64Settings = GetSyncSettings<N64, N64SyncSettings>();
				n64Settings.Core = N64SyncSettings.CoreType.Pure_Interpret;
				_config.N64UseCircularAnalogConstraint = false;
				PutSyncSettings<N64>(n64Settings);

				// SNES
				_config.SNES_InSnes9x = false;

				// Genesis
				var genesisSettings = GetSyncSettings<GPGX, GPGX.GPGXSyncSettings>();
				genesisSettings.Region = LibGPGX.Region.Autodetect;
				PutSyncSettings<GPGX>(genesisSettings);

				// SMS
				var smsSettings = GetSyncSettings<SMS, SMS.SMSSyncSettings>();
				smsSettings.UseBIOS = true;
				PutSyncSettings<SMS>(smsSettings);

				// Coleco
				var colecoSettings = GetSyncSettings<ColecoVision, ColecoVision.ColecoSyncSettings>();
				colecoSettings.SkipBiosIntro = true;
				PutSyncSettings<ColecoVision>(colecoSettings);

				// A2600
				var a2600Settings = GetSyncSettings<Atari2600, Atari2600.A2600SyncSettings>();
				a2600Settings.FastScBios = false;
				a2600Settings.LeftDifficulty = true;
				a2600Settings.RightDifficulty = true;
				PutSyncSettings<Atari2600>(a2600Settings);

				// NES
				_config.NES_InQuickNES = true;
			}
			else if (_config.SelectedProfile == Config.ClientProfile.N64Tas)
			{
				DisplayProfileSettingBoxes(false);

				// General
				_config.NoLowResLargeScreenshotWithStates = false;
				_config.SaveScreenshotWithStates = true;
				_config.AllowUD_LR = true;
				_config.BackupSavestates = false;
				_config.SkipLagFrame = true;
				_config.SaveStateCompressionLevelNormal = 0;

				// Rewind
				_config.RewindEnabledLarge = false;
				_config.RewindEnabledMedium = false;
				_config.RewindEnabledSmall = false;

				// N64
				var n64Settings = GetSyncSettings<N64, N64SyncSettings>();
				n64Settings.Rsp = N64SyncSettings.RspType.Rsp_Hle;
				n64Settings.Core = N64SyncSettings.CoreType.Pure_Interpret;
				_config.N64UseCircularAnalogConstraint = false;
				PutSyncSettings<N64>(n64Settings);

				// SNES
				_config.SNES_InSnes9x = false;

				// Genesis
				var genesisSettings = GetSyncSettings<GPGX, GPGX.GPGXSyncSettings>();
				genesisSettings.Region = LibGPGX.Region.Autodetect;
				PutSyncSettings<GPGX>(genesisSettings);

				// SMS
				var smsSettings = GetSyncSettings<SMS, SMS.SMSSyncSettings>();
				smsSettings.UseBIOS = true;
				PutSyncSettings<SMS>(smsSettings);

				// Coleco
				var colecoSettings = GetSyncSettings<ColecoVision, ColecoVision.ColecoSyncSettings>();
				colecoSettings.SkipBiosIntro = true;
				PutSyncSettings<ColecoVision>(colecoSettings);

				// A2600
				var a2600Settings = GetSyncSettings<Atari2600, Atari2600.A2600SyncSettings>();
				a2600Settings.FastScBios = false;
				a2600Settings.LeftDifficulty = true;
				a2600Settings.RightDifficulty = true;
				PutSyncSettings<Atari2600>(a2600Settings);

				// NES
				_config.NES_InQuickNES = true;
			}
			else if (_config.SelectedProfile == Config.ClientProfile.Custom)
			{
				// Disabled for now
				////DisplayProfileSettingBoxes(true);
			}

			bool oldUpdateAutoCheckEnabled = _config.Update_AutoCheckEnabled;
			_config.Update_AutoCheckEnabled = AutoCheckForUpdates.Checked;
			if (_config.Update_AutoCheckEnabled != oldUpdateAutoCheckEnabled)
			{
				if (!_config.Update_AutoCheckEnabled)
				{
					UpdateChecker.ResetHistory();
				}

				UpdateChecker.BeginCheck(); // Call even if auto checking is disabled to trigger event (it won't actually check)
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void DisplayProfileSettingBoxes(bool cProfile)
		{
			if (cProfile)
			{
				ProfileDialogHelpTexBox.Location = new Point(217, 12);
				ProfileDialogHelpTexBox.Size = new Size(165, 126);
				SaveScreenshotStatesCheckBox.Visible = true;
				SaveLargeScreenshotStatesCheckBox.Visible = true;
				AllowUDLRCheckBox.Visible = true;
				CustomProfileOptionsLabel.Visible = true;
			}
			else
			{
				ProfileDialogHelpTexBox.Location = new Point(184, 12);
				ProfileDialogHelpTexBox.Size = new Size(198, 126);
				ProfileDialogHelpTexBox.Text = "Options: \r\nCasual Gaming - All about performance! \r\n\nTool-Assisted Speedruns - Maximum Accuracy! \r\n\nLongplays - Stability is the key!";
				SaveScreenshotStatesCheckBox.Visible = false;
				SaveLargeScreenshotStatesCheckBox.Visible = false;
				AllowUDLRCheckBox.Visible = false;
				CustomProfileOptionsLabel.Visible = false;
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

		private TSetting GetSyncSettings<TEmulator, TSetting>()
			where TSetting : class, new()
			where TEmulator : IEmulator
		{
			// should we complain if we get a successful object from the config file, but it is the wrong type?
			object fromCore = null;
			var settable = new SettingsAdapter(_emulator);
			if (settable.HasSyncSettings)
			{
				fromCore = settable.GetSyncSettings();
			}

			return fromCore as TSetting
				?? _config.GetCoreSyncSettings<TEmulator>() as TSetting
				?? new TSetting(); // guaranteed to give sensible defaults
		}

		private void PutSyncSettings<TEmulator>(object o)
			where TEmulator : IEmulator
		{
			if (_emulator is TEmulator)
			{
				_mainForm.PutCoreSyncSettings(o);
			}
			else
			{
				_config.PutCoreSyncSettings<TEmulator>(o);
			}
		}
	}
}
