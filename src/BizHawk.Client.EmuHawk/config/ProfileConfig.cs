using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Atari.Atari2600;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

namespace BizHawk.Client.EmuHawk
{
	public partial class ProfileConfig : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private readonly IEmulator _emulator;
		private readonly Config _config;

		public ProfileConfig(
			IMainFormForConfig mainForm,
			IEmulator emulator,
			Config config)
		{
			_mainForm = mainForm;
			_emulator = emulator;
			_config = config;
			InitializeComponent();
			Icon = Properties.Resources.ProfileIcon;
		}

		private void ProfileConfig_Load(object sender, EventArgs e)
		{
			ProfileSelectComboBox.SelectedItem = _config.SelectedProfile switch
			{
				ClientProfile.Casual => "Casual Gaming",
				ClientProfile.Longplay => "Longplays",
				ClientProfile.Tas => "Tool-assisted Speedruns",
				ClientProfile.N64Tas => "N64 Tool-assisted Speedruns",
				_ => "Casual Gaming"
			};

			AutoCheckForUpdates.Checked = _config.UpdateAutoCheckEnabled;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			_config.SelectedProfile = ProfileSelectComboBox.SelectedItem.ToString() switch
			{
				"Longplays" => ClientProfile.Longplay,
				"Tool-assisted Speedruns" => ClientProfile.Tas,
				"N64 Tool-assisted Speedruns" => ClientProfile.N64Tas,
				_ => ClientProfile.Casual
			};

			SetCasual();

			switch (_config.SelectedProfile)
			{
				case ClientProfile.Longplay:
					SetLongPlay();
					break;
				case ClientProfile.Tas:
					SetTas();
					break;
				case ClientProfile.N64Tas:
					SetTas();
					SetN64Tas();
					break;
			}

			bool oldUpdateAutoCheckEnabled = _config.UpdateAutoCheckEnabled;
			_config.UpdateAutoCheckEnabled = AutoCheckForUpdates.Checked;
			if (_config.UpdateAutoCheckEnabled != oldUpdateAutoCheckEnabled)
			{
				UpdateChecker.GlobalConfig = _config;
				if (!_config.UpdateAutoCheckEnabled)
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

		private void SetCasual()
		{
			_config.Savestates.NoLowResLargeScreenshots = false;
			_config.Savestates.SaveScreenshot = false;
			_config.AllowUdlr = false;
			_config.Savestates.MakeBackups = false;

			_config.Savestates.CompressionLevelNormal = 0;
			_config.Rewind.Enabled = true;
			_config.SkipLagFrame = false;

			// N64
			var n64Settings = GetSyncSettings<N64, N64SyncSettings>();
			n64Settings.Rsp = N64SyncSettings.RspType.Rsp_Hle;
			n64Settings.Core = N64SyncSettings.CoreType.Interpret;
			_config.N64UseCircularAnalogConstraint = true;
			PutSyncSettings<N64>(n64Settings);

			// SNES
			_config.PreferredCores["SNES"] = CoreNames.Snes9X;

			// Genesis
			var genesisSettings = GetSyncSettings<GPGX, GPGX.GPGXSyncSettings>();
			genesisSettings.Region = LibGPGX.Region.Autodetect;
			PutSyncSettings<GPGX>(genesisSettings);

			// SMS
			var smsSettings = GetSyncSettings<SMS, SMS.SmsSyncSettings>();
			smsSettings.UseBios = false;
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
			_config.PreferredCores["NES"] = CoreNames.QuickNes;

			// GB
			_config.PreferredCores["GB"] = CoreNames.Gambatte;
			_config.PreferredCores["GBC"] = CoreNames.Gambatte;
			var s = GetSyncSettings<Gameboy, Gameboy.GambatteSyncSettings>();
			s.EnableBIOS = false;
			PutSyncSettings<Gameboy>(s);
		}

		private void SetLongPlay()
		{
			_config.Savestates.CompressionLevelNormal = 5;

			// SNES
			_config.PreferredCores["SNES"] = CoreNames.Bsnes;

			// SMS
			var smsSettings = GetSyncSettings<SMS, SMS.SmsSyncSettings>();
			smsSettings.UseBios = true;
			PutSyncSettings<SMS>(smsSettings);

			// A2600
			var a2600Settings = GetSyncSettings<Atari2600, Atari2600.A2600SyncSettings>();
			a2600Settings.FastScBios = false;
			a2600Settings.LeftDifficulty = true;
			a2600Settings.RightDifficulty = true;
			PutSyncSettings<Atari2600>(a2600Settings);

			// NES
			_config.PreferredCores["NES"] = CoreNames.NesHawk;

			// GB
			_config.PreferredCores["GB"] = CoreNames.Gambatte;
			_config.PreferredCores["GBC"] = CoreNames.Gambatte;
			var s = GetSyncSettings<Gameboy, Gameboy.GambatteSyncSettings>();
			s.EnableBIOS = true;
			PutSyncSettings<Gameboy>(s);
		}

		private void SetTas()
		{
			// General
			_config.Savestates.SaveScreenshot = true;
			_config.AllowUdlr = true;
			_config.Savestates.MakeBackups = true;
			_config.SkipLagFrame = false;
			_config.Savestates.CompressionLevelNormal = 5;

			// Rewind
			_config.Rewind.Enabled = false;

			// N64
			var n64Settings = GetSyncSettings<N64, N64SyncSettings>();
			n64Settings.Core = N64SyncSettings.CoreType.Pure_Interpret;
			_config.N64UseCircularAnalogConstraint = false;
			PutSyncSettings<N64>(n64Settings);

			// SNES
			_config.PreferredCores["SNES"] = CoreNames.Snes9X;

			// Genesis
			var genesisSettings = GetSyncSettings<GPGX, GPGX.GPGXSyncSettings>();
			genesisSettings.Region = LibGPGX.Region.Autodetect;
			PutSyncSettings<GPGX>(genesisSettings);

			// SMS
			var smsSettings = GetSyncSettings<SMS, SMS.SmsSyncSettings>();
			smsSettings.UseBios = true;
			PutSyncSettings<SMS>(smsSettings);

			// A2600
			var a2600Settings = GetSyncSettings<Atari2600, Atari2600.A2600SyncSettings>();
			a2600Settings.FastScBios = false;
			a2600Settings.LeftDifficulty = true;
			a2600Settings.RightDifficulty = true;
			PutSyncSettings<Atari2600>(a2600Settings);

			// NES
			_config.PreferredCores["NES"] = CoreNames.NesHawk;

			// GB
			_config.PreferredCores["GB"] = CoreNames.Gambatte;
			_config.PreferredCores["GBC"] = CoreNames.Gambatte;
			var s = GetSyncSettings<Gameboy, Gameboy.GambatteSyncSettings>();
			s.EnableBIOS = true;
			s.GBACGB = true;
			PutSyncSettings<Gameboy>(s);
			
		}

		private void SetN64Tas()
		{
			// General
			_config.Savestates.MakeBackups = false;
			_config.SkipLagFrame = true;
			_config.Savestates.CompressionLevelNormal = 0;
		}

		private TSetting GetSyncSettings<TEmulator, TSetting>()
			where TSetting : class, new()
			where TEmulator : IEmulator
		{
			object fromCore = null;
			var settable = new SettingsAdapter(_emulator);
			if (settable.HasSyncSettings)
			{
				fromCore = settable.GetSyncSettings();
			}

			return fromCore as TSetting
				?? _config.GetCoreSyncSettings<TEmulator, TSetting>()
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
