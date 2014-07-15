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
using BizHawk.Emulation.Cores.Sega.Saturn;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Atari.Atari2600;

namespace BizHawk.Client.EmuHawk
{
	public partial class ProfileConfig : Form
	{
		// TODO:
		// Save the profile selected to Global.Config (Casual is the default)
		// Default the dropdown to the current profile selected instead of empty

		public ProfileConfig()
		{
			InitializeComponent();
		}

		private void ProfileConfig_Load(object sender, EventArgs e)
		{
			if (!VersionInfo.DeveloperBuild)
			{
				ProfileSelectComboBox.Items.Remove("Custom Profile");
			}
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			if (ProfileSelectComboBox.SelectedIndex == 0) // Casual Gaming
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
				var n64Settings = GetSyncSettings<N64, N64SyncSettings>();
				n64Settings.RspType = N64SyncSettings.RSPTYPE.Rsp_Hle;
				n64Settings.CoreType = N64SyncSettings.CORETYPE.Dynarec;
				Global.Config.N64UseCircularAnalogConstraint = true;
				PutSyncSettings<N64>(n64Settings);

				// SNES
				var snesSettings = GetSyncSettings<LibsnesCore, LibsnesCore.SnesSyncSettings>();
				snesSettings.Profile = "Performance";
				PutSyncSettings<LibsnesCore>(snesSettings);

				//Saturn
				var saturnSettings = GetSyncSettings<Yabause, Yabause.SaturnSyncSettings>();
				saturnSettings.SkipBios = false;
				PutSyncSettings<Yabause>(saturnSettings);

				//Genesis
				var genesisSettings = GetSyncSettings<GPGX, GPGX.GPGXSyncSettings>();
				genesisSettings.Region = LibGPGX.Region.Autodetect;
				PutSyncSettings<GPGX>(genesisSettings);

				//SMS
				var smsSettings = GetSyncSettings<SMS, SMS.SMSSyncSettings>();
				smsSettings.UseBIOS = false;
				PutSyncSettings<SMS>(smsSettings);

				//Coleco
				var colecoSettings = GetSyncSettings<ColecoVision, ColecoVision.ColecoSyncSettings>();
				colecoSettings.SkipBiosIntro = false;
				PutSyncSettings<ColecoVision>(colecoSettings);

				//A2600
				var a2600Settings = GetSyncSettings<Atari2600, Atari2600.A2600SyncSettings>();
				a2600Settings.FastScBios = true;
				a2600Settings.LeftDifficulty = false;
				a2600Settings.RightDifficulty = false;
				PutSyncSettings<Atari2600>(a2600Settings);

				// NES
				Global.Config.NES_InQuickNES = true;
			}
			else if (ProfileSelectComboBox.SelectedIndex == 2) // Long Plays
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
				var n64Settings = GetSyncSettings<N64, N64SyncSettings>();
				n64Settings.RspType = N64SyncSettings.RSPTYPE.Rsp_Z64_hlevideo;
				n64Settings.CoreType = N64SyncSettings.CORETYPE.Pure_Interpret;
				Global.Config.N64UseCircularAnalogConstraint = true;
				PutSyncSettings<N64>(n64Settings);

				// SNES
				var snesSettings = GetSyncSettings<LibsnesCore, LibsnesCore.SnesSyncSettings>();
				snesSettings.Profile = "Compatibility";
				PutSyncSettings<LibsnesCore>(snesSettings);

				// Saturn
				var saturnSettings = GetSyncSettings<Yabause, Yabause.SaturnSyncSettings>();
				saturnSettings.SkipBios = false;
				PutSyncSettings<Yabause>(saturnSettings);

				// Genesis
				var genesisSettings = GetSyncSettings<GPGX, GPGX.GPGXSyncSettings>();
				genesisSettings.Region = LibGPGX.Region.Autodetect;
				PutSyncSettings<GPGX>(genesisSettings);

				// SMS
				var smsSettings = GetSyncSettings<SMS, SMS.SMSSyncSettings>();
				smsSettings.UseBIOS = false;
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
				Global.Config.NES_InQuickNES = true;
			}
			else if (ProfileSelectComboBox.SelectedIndex == 1) // TAS
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
				var n64Settings = GetSyncSettings<N64, N64SyncSettings>();
				n64Settings.RspType = N64SyncSettings.RSPTYPE.Rsp_Z64_hlevideo;
				n64Settings.CoreType = N64SyncSettings.CORETYPE.Pure_Interpret;
				Global.Config.N64UseCircularAnalogConstraint = false;
				PutSyncSettings<N64>(n64Settings);

				// SNES
				var snesSettings = GetSyncSettings<LibsnesCore, LibsnesCore.SnesSyncSettings>();
				snesSettings.Profile = "Compatibility";
				PutSyncSettings<LibsnesCore>(snesSettings);

				// Saturn
				var saturnSettings = GetSyncSettings<Yabause, Yabause.SaturnSyncSettings>();
				saturnSettings.SkipBios = true;
				PutSyncSettings<Yabause>(saturnSettings);

				// Genesis
				var genesisSettings = GetSyncSettings<GPGX, GPGX.GPGXSyncSettings>();
				genesisSettings.Region = LibGPGX.Region.Autodetect;
				PutSyncSettings<GPGX>(genesisSettings);

				// SMS
				var smsSettings = GetSyncSettings<SMS, SMS.SMSSyncSettings>();
				smsSettings.UseBIOS = false;
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
				Global.Config.NES_InQuickNES = true;
			}
			else if (ProfileSelectComboBox.SelectedIndex == 3) //custom
			{
				//Disabled for now
				//DisplayProfileSettingBoxes(true);
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

		private static TSetting GetSyncSettings<TEmulator, TSetting>()
			where TSetting : class, new()
			where TEmulator : BizHawk.Emulation.Common.IEmulator
		{
			// should we complain if we get a successful object from the config file, but it is the wrong type?
			return Global.Emulator.GetSyncSettings() as TSetting
				?? Global.Config.GetCoreSyncSettings<TEmulator>() as TSetting
				?? new TSetting(); // guaranteed to give sensible defaults
		}

		private static TSetting GetSettings<TEmulator, TSetting>()
			where TSetting : class, new()
			where TEmulator : BizHawk.Emulation.Common.IEmulator
		{
			// should we complain if we get a successful object from the config file, but it is the wrong type?
			return Global.Emulator.GetSettings() as TSetting
				?? Global.Config.GetCoreSettings<TEmulator>() as TSetting
				?? new TSetting(); // guaranteed to give sensible defaults
		}

		private static void PutSettings<TEmulator>(object o)
			where TEmulator : BizHawk.Emulation.Common.IEmulator
		{
			if (Global.Emulator is TEmulator)
				Global.Emulator.PutSettings(o);
			else
				Global.Config.PutCoreSettings<TEmulator>(o);
		}

		private static void PutSyncSettings<TEmulator>(object o)
			where TEmulator : BizHawk.Emulation.Common.IEmulator
		{
			if (Global.Emulator is TEmulator)
				GlobalWin.MainForm.PutCoreSyncSettings(o);
			else
				Global.Config.PutCoreSyncSettings<TEmulator>(o);
		}

		/*
		// Unfortunatley there is not yet a generic way to match a sync setting object with its core implementation, so we need a bunch of these
		private static N64SyncSettings GetN64SyncSettings()
		{
			if (Global.Emulator is N64)
			{
				return (N64SyncSettings)Global.Emulator.GetSyncSettings();
			}
			else
			{
				return (N64SyncSettings)Global.Config.GetCoreSyncSettings<N64>()
					?? new N64SyncSettings();
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
				return (LibsnesCore.SnesSyncSettings)Global.Config.GetCoreSyncSettings<LibsnesCore>()
					?? new LibsnesCore.SnesSyncSettings() ;
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

		private static Yabause.SaturnSyncSettings GetSaturnSyncSettings()
		{
			if (Global.Emulator is Yabause)
			{
				return (Yabause.SaturnSyncSettings)Global.Emulator.GetSyncSettings();
			}
			else
			{
				return (Yabause.SaturnSyncSettings)Global.Config.GetCoreSyncSettings<Yabause>()
					?? new Yabause.SaturnSyncSettings();
			}
		}

		private static void PutSaturnSyncSettings(Yabause.SaturnSyncSettings s)
		{
			if (Global.Emulator is Yabause)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(s);
			}
			else
			{
				Global.Config.PutCoreSyncSettings<Yabause>(s);
			}
		}

		private static GPGX.GPGXSyncSettings GetGenesisSyncSettings()
		{
			if (Global.Emulator is GPGX)
			{
				return (GPGX.GPGXSyncSettings)Global.Emulator.GetSyncSettings();
			}
			else
			{
				return (GPGX.GPGXSyncSettings)Global.Config.GetCoreSyncSettings<GPGX>()
					?? new GPGX.GPGXSyncSettings();
			}
		}

		private static void PutGenesisSyncSettings(GPGX.GPGXSyncSettings s)
		{
			if (Global.Emulator is GPGX)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(s);
			}
			else
			{
				Global.Config.PutCoreSyncSettings<GPGX>(s);
			}
		}

		private static SMS.SMSSyncSettings GetSMSSyncSettings()
		{
			if (Global.Emulator is SMS)
			{
				return (SMS.SMSSyncSettings)Global.Emulator.GetSyncSettings();
			}
			else
			{
				return (SMS.SMSSyncSettings)Global.Config.GetCoreSyncSettings<SMS>()
					?? new SMS.SMSSyncSettings();
			}
		}

		private static void PutSMSSyncSettings(SMS.SMSSyncSettings s)
		{
			if (Global.Emulator is SMS)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(s);
			}
			else
			{
				Global.Config.PutCoreSyncSettings<SMS>(s);
			}
		}

		private static ColecoVision.ColecoSyncSettings GetColecoVisionSyncSettings()
		{
			if (Global.Emulator is ColecoVision)
			{
				return (ColecoVision.ColecoSyncSettings)Global.Emulator.GetSyncSettings();
			}
			else
			{
				return (ColecoVision.ColecoSyncSettings)Global.Config.GetCoreSyncSettings<ColecoVision>()
					?? new ColecoVision.ColecoSyncSettings();
			}
		}

		private static void PutColecoVisionSyncSettings(ColecoVision.ColecoSyncSettings s)
		{
			if (Global.Emulator is ColecoVision)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(s);
			}
			else
			{
				Global.Config.PutCoreSyncSettings<ColecoVision>(s);
			}
		}

		private static Atari2600.A2600SyncSettings GetA2600SyncSettings()
		{
			if (Global.Emulator is Atari2600)
			{
				return (Atari2600.A2600SyncSettings)Global.Emulator.GetSyncSettings();
			}
			else
			{
				return (Atari2600.A2600SyncSettings)Global.Config.GetCoreSyncSettings<Atari2600>()
					?? new Atari2600.A2600SyncSettings();
			}
		}

		private static void PutA2600SyncSettings(Atari2600.A2600SyncSettings s)
		{
			if (Global.Emulator is Atari2600)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(s);
			}
			else
			{
				Global.Config.PutCoreSyncSettings<Atari2600>(s);
			}
		}
		*/
		#endregion
	}
}
