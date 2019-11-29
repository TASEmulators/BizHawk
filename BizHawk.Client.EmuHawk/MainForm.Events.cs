using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SubNESHawk;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;

using BizHawk.Client.Common;

using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Emulation.Cores.Computers.AppleII;
using BizHawk.Client.ApiHawk;
using BizHawk.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm
	{
		#region File Menu

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveStateSubMenu.Enabled =
				LoadStateSubMenu.Enabled =
				SaveSlotSubMenu.Enabled =
				Emulator.HasSavestates();

			OpenRomMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Open ROM"].Bindings;
			CloseRomMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Close ROM"].Bindings;

			MovieSubMenu.Enabled =
				CloseRomMenuItem.Enabled =
				!Emulator.IsNull();

			var hasSaveRam = Emulator.HasSaveRam();
			bool needBold = hasSaveRam && Emulator.AsSaveRam().SaveRamModified;

			SaveRAMSubMenu.Enabled = hasSaveRam;
			if (SaveRAMSubMenu.Font.Bold != needBold)
			{
				var font = new Font(SaveRAMSubMenu.Font, needBold ? FontStyle.Bold : FontStyle.Regular);
				SaveRAMSubMenu.Font = font;
			}

			AVSubMenu.Enabled =
			ScreenshotSubMenu.Enabled =
				Emulator.HasVideoProvider();
		}

		private void RecentRomMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			RecentRomSubMenu.DropDownItems.Clear();
			RecentRomSubMenu.DropDownItems.AddRange(
				Global.Config.RecentRoms.RecentMenu(LoadRomFromRecent, true, true));
		}

		private void SaveStateSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveState0MenuItem.Font = new Font(
				SaveState0MenuItem.Font.FontFamily,
				SaveState0MenuItem.Font.Size,
				 _stateSlots.HasSlot(0) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular);

			SaveState1MenuItem.Font = new Font(
				SaveState1MenuItem.Font.FontFamily,
				SaveState1MenuItem.Font.Size,
				_stateSlots.HasSlot(1) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular);

			SaveState2MenuItem.Font = new Font(
				SaveState2MenuItem.Font.FontFamily,
				SaveState2MenuItem.Font.Size,
				_stateSlots.HasSlot(2) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular);

			SaveState3MenuItem.Font = new Font(
				SaveState3MenuItem.Font.FontFamily,
				SaveState3MenuItem.Font.Size,
				_stateSlots.HasSlot(3) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular);

			SaveState4MenuItem.Font = new Font(
				SaveState4MenuItem.Font.FontFamily,
				SaveState4MenuItem.Font.Size,
				_stateSlots.HasSlot(4) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular);

			SaveState5MenuItem.Font = new Font(
				SaveState5MenuItem.Font.FontFamily,
				SaveState5MenuItem.Font.Size,
				_stateSlots.HasSlot(5) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular);

			SaveState6MenuItem.Font = new Font(
				SaveState6MenuItem.Font.FontFamily,
				SaveState6MenuItem.Font.Size,
				_stateSlots.HasSlot(6) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular);

			SaveState7MenuItem.Font = new Font(
				SaveState7MenuItem.Font.FontFamily,
				SaveState7MenuItem.Font.Size,
				_stateSlots.HasSlot(7) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular);

			SaveState8MenuItem.Font = new Font(
				SaveState8MenuItem.Font.FontFamily,
				SaveState8MenuItem.Font.Size,
				_stateSlots.HasSlot(8) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular);

			SaveState9MenuItem.Font = new Font(
				SaveState9MenuItem.Font.FontFamily,
				SaveState9MenuItem.Font.Size,
				_stateSlots.HasSlot(9) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular);

			SaveState1MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Save State 1"].Bindings;
			SaveState2MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Save State 2"].Bindings;
			SaveState3MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Save State 3"].Bindings;
			SaveState4MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Save State 4"].Bindings;
			SaveState5MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Save State 5"].Bindings;
			SaveState6MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Save State 6"].Bindings;
			SaveState7MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Save State 7"].Bindings;
			SaveState8MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Save State 8"].Bindings;
			SaveState9MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Save State 9"].Bindings;
			SaveState0MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Save State 0"].Bindings;
			SaveNamedStateMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Save Named State"].Bindings;
		}

		private void LoadStateSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			LoadState1MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Load State 1"].Bindings;
			LoadState2MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Load State 2"].Bindings;
			LoadState3MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Load State 3"].Bindings;
			LoadState4MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Load State 4"].Bindings;
			LoadState5MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Load State 5"].Bindings;
			LoadState6MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Load State 6"].Bindings;
			LoadState7MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Load State 7"].Bindings;
			LoadState8MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Load State 8"].Bindings;
			LoadState9MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Load State 9"].Bindings;
			LoadState0MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Load State 0"].Bindings;
			LoadNamedStateMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Load Named State"].Bindings;

			AutoloadLastSlotMenuItem.Checked = Global.Config.AutoLoadLastSaveSlot;

			LoadState1MenuItem.Enabled = _stateSlots.HasSlot(1);
			LoadState2MenuItem.Enabled = _stateSlots.HasSlot(2);
			LoadState3MenuItem.Enabled = _stateSlots.HasSlot(3);
			LoadState4MenuItem.Enabled = _stateSlots.HasSlot(4);
			LoadState5MenuItem.Enabled = _stateSlots.HasSlot(5);
			LoadState6MenuItem.Enabled = _stateSlots.HasSlot(6);
			LoadState7MenuItem.Enabled = _stateSlots.HasSlot(7);
			LoadState8MenuItem.Enabled = _stateSlots.HasSlot(8);
			LoadState9MenuItem.Enabled = _stateSlots.HasSlot(9);
			LoadState0MenuItem.Enabled = _stateSlots.HasSlot(0);
		}

		private void SaveSlotSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SelectSlot0MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Select State 0"].Bindings;
			SelectSlot1MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Select State 1"].Bindings;
			SelectSlot2MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Select State 2"].Bindings;
			SelectSlot3MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Select State 3"].Bindings;
			SelectSlot4MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Select State 4"].Bindings;
			SelectSlot5MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Select State 5"].Bindings;
			SelectSlot6MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Select State 6"].Bindings;
			SelectSlot7MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Select State 7"].Bindings;
			SelectSlot8MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Select State 8"].Bindings;
			SelectSlot9MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Select State 9"].Bindings;
			PreviousSlotMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Previous Slot"].Bindings;
			NextSlotMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Next Slot"].Bindings;
			SaveToCurrentSlotMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Quick Save"].Bindings;
			LoadCurrentSlotMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Quick Load"].Bindings;

			SelectSlot0MenuItem.Checked = false;
			SelectSlot1MenuItem.Checked = false;
			SelectSlot2MenuItem.Checked = false;
			SelectSlot3MenuItem.Checked = false;
			SelectSlot4MenuItem.Checked = false;
			SelectSlot5MenuItem.Checked = false;
			SelectSlot6MenuItem.Checked = false;
			SelectSlot7MenuItem.Checked = false;
			SelectSlot8MenuItem.Checked = false;
			SelectSlot9MenuItem.Checked = false;
			SelectSlot1MenuItem.Checked = false;

			switch (Global.Config.SaveSlot)
			{
				case 0:
					SelectSlot0MenuItem.Checked = true;
					break;
				case 1:
					SelectSlot1MenuItem.Checked = true;
					break;
				case 2:
					SelectSlot2MenuItem.Checked = true;
					break;
				case 3:
					SelectSlot3MenuItem.Checked = true;
					break;
				case 4:
					SelectSlot4MenuItem.Checked = true;
					break;
				case 5:
					SelectSlot5MenuItem.Checked = true;
					break;
				case 6:
					SelectSlot6MenuItem.Checked = true;
					break;
				case 7:
					SelectSlot7MenuItem.Checked = true;
					break;
				case 8:
					SelectSlot8MenuItem.Checked = true;
					break;
				case 9:
					SelectSlot9MenuItem.Checked = true;
					break;
			}
		}

		private void SaveRAMSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			FlushSaveRAMMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Flush SaveRAM"].Bindings;
		}

		private void MovieSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			FullMovieLoadstatesMenuItem.Enabled = !Global.MovieSession.MultiTrack.IsActive;
			StopMovieWithoutSavingMenuItem.Enabled = Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie.Changes;
			StopMovieMenuItem.Enabled
				= PlayFromBeginningMenuItem.Enabled
				= SaveMovieMenuItem.Enabled
				= SaveMovieAsMenuItem.Enabled
				= Global.MovieSession.Movie.IsActive;

			ReadonlyMenuItem.Checked = Global.MovieSession.ReadOnly;
			AutomaticallyBackupMoviesMenuItem.Checked = Global.Config.EnableBackupMovies;
			FullMovieLoadstatesMenuItem.Checked = Global.Config.VBAStyleMovieLoadState;

			ReadonlyMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle read-only"].Bindings;
			RecordMovieMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Record Movie"].Bindings;
			PlayMovieMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Play Movie"].Bindings;
			StopMovieMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Stop Movie"].Bindings;
			PlayFromBeginningMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Play from beginning"].Bindings;
			SaveMovieMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Save Movie"].Bindings;
		}

		private void RecentMovieSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentMovieSubMenu.DropDownItems.Clear();
			RecentMovieSubMenu.DropDownItems.AddRange(
				Global.Config.RecentMovies.RecentMenu(LoadMoviesFromRecent, true));
		}

		private void MovieEndSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			MovieEndFinishMenuItem.Checked = Global.Config.MovieEndAction == MovieEndAction.Finish;
			MovieEndRecordMenuItem.Checked = Global.Config.MovieEndAction == MovieEndAction.Record;
			MovieEndStopMenuItem.Checked = Global.Config.MovieEndAction == MovieEndAction.Stop;
			MovieEndPauseMenuItem.Checked = Global.Config.MovieEndAction == MovieEndAction.Pause;

			// Arguably an IControlMainForm property should be set here, but in reality only Tastudio is ever going to interfere with this logic
			MovieEndFinishMenuItem.Enabled =
			MovieEndRecordMenuItem.Enabled =
			MovieEndStopMenuItem.Enabled =
			MovieEndPauseMenuItem.Enabled =
				!GlobalWin.Tools.Has<TAStudio>();
		}

		private void AVSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ConfigAndRecordAVMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Record A/V"].Bindings;
			StopAVIMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Stop A/V"].Bindings;
			CaptureOSDMenuItem.Checked = Global.Config.AVI_CaptureOSD;

			RecordAVMenuItem.Enabled = !string.IsNullOrEmpty(Global.Config.VideoWriter) && _currAviWriter == null;

			if (_currAviWriter == null)
			{
				ConfigAndRecordAVMenuItem.Enabled = true;
				StopAVIMenuItem.Enabled = false;
			}
			else
			{
				ConfigAndRecordAVMenuItem.Enabled = false;
				StopAVIMenuItem.Enabled = true;
			}
		}

		private void ScreenshotSubMenu_DropDownOpening(object sender, EventArgs e)
		{
			ScreenshotCaptureOSDMenuItem1.Checked = Global.Config.Screenshot_CaptureOSD;
			ScreenshotMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Screenshot"].Bindings;
			ScreenshotClipboardMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["ScreenshotToClipboard"].Bindings;
			ScreenshotClientClipboardMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Screen Client to Clipboard"].Bindings;
		}

		private void OpenRomMenuItem_Click(object sender, EventArgs e)
		{
			AdvancedLoader = AdvancedRomLoaderType.None;
			OpenRom();
		}

		private void OpenAdvancedMenuItem_Click(object sender, EventArgs e)
		{
			using var oac = new OpenAdvancedChooser(this);
			if (oac.ShowHawkDialog() == DialogResult.Cancel)
			{
				return;
			}

			AdvancedLoader = oac.Result;

			if (AdvancedLoader == AdvancedRomLoaderType.LibretroLaunchNoGame)
			{
				var argsNoGame = new LoadRomArgs
				{
					OpenAdvanced = new OpenAdvanced_LibretroNoGame(Global.Config.LibretroCore)
				};
				LoadRom("", argsNoGame);
				return;
			}

			var args = new LoadRomArgs();

			var filter = RomFilter;

			if (AdvancedLoader == AdvancedRomLoaderType.LibretroLaunchGame)
			{
				args.OpenAdvanced = new OpenAdvanced_Libretro();
				filter = oac.SuggestedExtensionFilter;
			}
			else if (AdvancedLoader == AdvancedRomLoaderType.ClassicLaunchGame)
			{
				args.OpenAdvanced = new OpenAdvanced_OpenRom();
			}
			else if (AdvancedLoader == AdvancedRomLoaderType.MAMELaunchGame)
			{
				args.OpenAdvanced = new OpenAdvanced_MAME();
				filter = "MAME Arcade ROMs (*.zip)|*.zip";
			}
			else
			{
				throw new InvalidOperationException("Automatic Alpha Sanitizer");
			}

			/*************************/
			/* CLONE OF CODE FROM OpenRom (mostly) */
			using var ofd = new OpenFileDialog
			{
				InitialDirectory = PathManager.GetRomsPath(Emulator.SystemId),
				Filter = filter,
				RestoreDirectory = false,
				FilterIndex = _lastOpenRomFilter,
				Title = "Open Advanced"
			};

			var result = ofd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return;
			}

			var file = new FileInfo(ofd.FileName);
			Global.Config.LastRomPath = file.DirectoryName;
			_lastOpenRomFilter = ofd.FilterIndex;
			/*************************/

			LoadRom(file.FullName, args);
		}

		private void CloseRomMenuItem_Click(object sender, EventArgs e)
		{
			CloseRom();
		}

		private void Savestate1MenuItem_Click(object sender, EventArgs e) { SaveQuickSave("QuickSave1"); }
		private void Savestate2MenuItem_Click(object sender, EventArgs e) { SaveQuickSave("QuickSave2"); }
		private void Savestate3MenuItem_Click(object sender, EventArgs e) { SaveQuickSave("QuickSave3"); }
		private void Savestate4MenuItem_Click(object sender, EventArgs e) { SaveQuickSave("QuickSave4"); }
		private void Savestate5MenuItem_Click(object sender, EventArgs e) { SaveQuickSave("QuickSave5"); }
		private void Savestate6MenuItem_Click(object sender, EventArgs e) { SaveQuickSave("QuickSave6"); }
		private void Savestate7MenuItem_Click(object sender, EventArgs e) { SaveQuickSave("QuickSave7"); }
		private void Savestate8MenuItem_Click(object sender, EventArgs e) { SaveQuickSave("QuickSave8"); }
		private void Savestate9MenuItem_Click(object sender, EventArgs e) { SaveQuickSave("QuickSave9"); }
		private void Savestate0MenuItem_Click(object sender, EventArgs e) { SaveQuickSave("QuickSave0"); }

		private void SaveNamedStateMenuItem_Click(object sender, EventArgs e)
		{
			SaveStateAs();
		}

		private void Loadstate1MenuItem_Click(object sender, EventArgs e) { LoadQuickSave("QuickSave1"); }
		private void Loadstate2MenuItem_Click(object sender, EventArgs e) { LoadQuickSave("QuickSave2"); }
		private void Loadstate3MenuItem_Click(object sender, EventArgs e) { LoadQuickSave("QuickSave3"); }
		private void Loadstate4MenuItem_Click(object sender, EventArgs e) { LoadQuickSave("QuickSave4"); }
		private void Loadstate5MenuItem_Click(object sender, EventArgs e) { LoadQuickSave("QuickSave5"); }
		private void Loadstate6MenuItem_Click(object sender, EventArgs e) { LoadQuickSave("QuickSave6"); }
		private void Loadstate7MenuItem_Click(object sender, EventArgs e) { LoadQuickSave("QuickSave7"); }
		private void Loadstate8MenuItem_Click(object sender, EventArgs e) { LoadQuickSave("QuickSave8"); }
		private void Loadstate9MenuItem_Click(object sender, EventArgs e) { LoadQuickSave("QuickSave9"); }
		private void Loadstate0MenuItem_Click(object sender, EventArgs e) { LoadQuickSave("QuickSave0"); }

		private void LoadNamedStateMenuItem_Click(object sender, EventArgs e)
		{
			LoadStateAs();
		}

		private void AutoloadLastSlotMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoLoadLastSaveSlot ^= true;
		}

		private void SelectSlotMenuItems_Click(object sender, EventArgs e)
		{
			if (sender == SelectSlot0MenuItem) Global.Config.SaveSlot = 0;
			else if (sender == SelectSlot1MenuItem) Global.Config.SaveSlot = 1;
			else if (sender == SelectSlot2MenuItem) Global.Config.SaveSlot = 2;
			else if (sender == SelectSlot3MenuItem) Global.Config.SaveSlot = 3;
			else if (sender == SelectSlot4MenuItem) Global.Config.SaveSlot = 4;
			else if (sender == SelectSlot5MenuItem) Global.Config.SaveSlot = 5;
			else if (sender == SelectSlot6MenuItem) Global.Config.SaveSlot = 6;
			else if (sender == SelectSlot7MenuItem) Global.Config.SaveSlot = 7;
			else if (sender == SelectSlot8MenuItem) Global.Config.SaveSlot = 8;
			else if (sender == SelectSlot9MenuItem) Global.Config.SaveSlot = 9;

			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void PreviousSlotMenuItem_Click(object sender, EventArgs e)
		{
			PreviousSlot();
		}

		private void NextSlotMenuItem_Click(object sender, EventArgs e)
		{
			NextSlot();
		}

		private void SaveToCurrentSlotMenuItem_Click(object sender, EventArgs e)
		{
			SaveQuickSave($"QuickSave{Global.Config.SaveSlot}");
		}

		private void LoadCurrentSlotMenuItem_Click(object sender, EventArgs e)
		{
			LoadQuickSave($"QuickSave{Global.Config.SaveSlot}");
		}

		private void FlushSaveRAMMenuItem_Click(object sender, EventArgs e)
		{
			FlushSaveRAM();
		}

		private void ReadonlyMenuItem_Click(object sender, EventArgs e)
		{
			ToggleReadOnly();
		}

		private void RecordMovieMenuItem_Click(object sender, EventArgs e)
		{
			if (!Emulator.Attributes().Released)
			{
				var result = MessageBox.Show(
					this,
					"Thanks for using BizHawk!  The emulation core you have selected " +
					"is currently BETA-status.  We appreciate your help in testing BizHawk. " +
					"You can record a movie on this core if you'd like to, but expect to " +
					"encounter bugs and sync problems.  Continue?", "BizHawk", MessageBoxButtons.YesNo);

				if (result != DialogResult.Yes)
				{
					return;
				}
			}

			if (!EmuHawkUtil.EnsureCoreIsAccurate(Emulator))
			{
				// Inaccurate core but allow the user to continue anyway
			}

			using var form = new RecordMovie(Emulator);
			form.ShowDialog();
		}

		private void PlayMovieMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new PlayMovie();
			form.ShowDialog();
		}

		private void StopMovieMenuItem_Click(object sender, EventArgs e)
		{
			StopMovie();
		}

		private void PlayFromBeginningMenuItem_Click(object sender, EventArgs e)
		{
			RestartMovie();
		}

		private void ImportMovieMenuItem_Click(object sender, EventArgs e)
		{
			using var ofd = new OpenFileDialog
			{
				InitialDirectory = PathManager.GetRomsPath(Emulator.SystemId),
				Multiselect = true,
				Filter = ToFilter("Movie Files", MovieImport.AvailableImporters()),
				RestoreDirectory = false
			};

			var result = ofd.ShowHawkDialog();
			if (result == DialogResult.OK)
			{
				foreach (var fn in ofd.FileNames)
				{
					ProcessMovieImport(fn, false);
				}
			}
		}

		private void SaveMovieMenuItem_Click(object sender, EventArgs e)
		{
			SaveMovie();
		}

		private void SaveMovieAsMenuItem_Click(object sender, EventArgs e)
		{
			var filename = Global.MovieSession.Movie.Filename;
			if (string.IsNullOrWhiteSpace(filename))
			{
				filename = PathManager.FilesystemSafeName(Global.Game);
			}

			var file = ToolFormBase.SaveFileDialog(
				filename,
				PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null),
				"Movie Files",
				Global.MovieSession.Movie.PreferredExtension);

			if (file != null)
			{
				Global.MovieSession.Movie.Filename = file.FullName;
				Global.Config.RecentMovies.Add(Global.MovieSession.Movie.Filename);
				SaveMovie();
			}
		}

		private void StopMovieWithoutSavingMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.Config.EnableBackupMovies)
			{
				Global.MovieSession.Movie.SaveBackup();
			}

			StopMovie(saveChanges: false);
		}

		private void AutomaticMovieBackupMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.EnableBackupMovies ^= true;
		}

		private void FullMovieLoadstatesMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VBAStyleMovieLoadState ^= true;
		}

		private void MovieEndFinishMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.MovieEndAction = MovieEndAction.Finish;
		}

		private void MovieEndRecordMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.MovieEndAction = MovieEndAction.Record;
		}

		private void MovieEndStopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.MovieEndAction = MovieEndAction.Stop;
		}

		private void MovieEndPauseMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.MovieEndAction = MovieEndAction.Pause;
		}

		private void ConfigAndRecordAVMenuItem_Click(object sender, EventArgs e)
		{
			RecordAv();
		}

		private void RecordAVMenuItem_Click(object sender, EventArgs e)
		{
			RecordAv(null, null); // force unattended, but allow traditional setup
		}

		private void StopAVMenuItem_Click(object sender, EventArgs e)
		{
			StopAv();
		}

		private void SynclessRecordingMenuItem_Click(object sender, EventArgs e)
		{
			new SynclessRecordingTools().Run();
		}

		private void CaptureOSDMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AVI_CaptureOSD ^= true;
		}

		private void ScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			TakeScreenshot();
		}

		private void ScreenshotAsMenuItem_Click(object sender, EventArgs e)
		{
			var path = $"{PathManager.ScreenshotPrefix(Global.Game)}.{DateTime.Now:yyyy-MM-dd HH.mm.ss}.png";

			using var sfd = new SaveFileDialog
			{
				InitialDirectory = Path.GetDirectoryName(path),
				FileName = Path.GetFileName(path),
				Filter = "PNG File (*.png)|*.png"
			};

			var result = sfd.ShowHawkDialog();
			if (result == DialogResult.OK)
			{
				TakeScreenshot(sfd.FileName);
			}
		}

		private void ScreenshotClipboardMenuItem_Click(object sender, EventArgs e)
		{
			TakeScreenshotToClipboard();
		}

		private void ScreenshotClientClipboardMenuItem_Click(object sender, EventArgs e)
		{
			TakeScreenshotClientToClipboard();
		}

		private void ScreenshotCaptureOSDMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Screenshot_CaptureOSD ^= true;
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			if (GlobalWin.Tools.AskSave())
			{
				Close();
			}
		}

		public void CloseEmulator()
		{
			_exitRequestPending = true;
		}

		public void CloseEmulator(int exitCode)
		{
			_exitRequestPending = true;
			_exitCode = exitCode;
		}

		#endregion

		#region Emulation Menu

		private void EmulationMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			PauseMenuItem.Checked = _didMenuPause ? _wasPaused : EmulatorPaused;

			SoftResetMenuItem.Enabled = Emulator.ControllerDefinition.BoolButtons.Contains("Reset") &&
					(!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished);

			HardResetMenuItem.Enabled = Emulator.ControllerDefinition.BoolButtons.Contains("Power") &&
				(!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished);

			PauseMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Pause"].Bindings;
			RebootCoreMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Reboot Core"].Bindings;
			SoftResetMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Soft Reset"].Bindings;
			HardResetMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Hard Reset"].Bindings;
		}

		private void PauseMenuItem_Click(object sender, EventArgs e)
		{
			if (IsTurboSeeking || IsSeeking)
			{
				PauseOnFrame = null;
			}
			else if (EmulatorPaused)
			{
				UnpauseEmulator();
			}
			else
			{
				PauseEmulator();
			}
		}

		private void PowerMenuItem_Click(object sender, EventArgs e)
		{
			RebootCore();
		}

		private void SoftResetMenuItem_Click(object sender, EventArgs e)
		{
			SoftReset();
		}

		private void HardResetMenuItem_Click(object sender, EventArgs e)
		{
			HardReset();
		}

		#endregion

		#region View

		private void ViewSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DisplayFPSMenuItem.Checked = Global.Config.DisplayFPS;
			DisplayFrameCounterMenuItem.Checked = Global.Config.DisplayFrameCounter;
			DisplayLagCounterMenuItem.Checked = Global.Config.DisplayLagCounter;
			DisplayInputMenuItem.Checked = Global.Config.DisplayInput;
			DisplayRerecordCountMenuItem.Checked = Global.Config.DisplayRerecordCount;
			DisplaySubtitlesMenuItem.Checked = Global.Config.DisplaySubtitles;

			DisplayFPSMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Display FPS"].Bindings;
			DisplayFrameCounterMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Frame Counter"].Bindings;
			DisplayLagCounterMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Lag Counter"].Bindings;
			DisplayInputMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Input Display"].Bindings;
			SwitchToFullscreenMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Full Screen"].Bindings;

			DisplayStatusBarMenuItem.Checked = Global.Config.DispChrome_StatusBarWindowed;
			DisplayLogWindowMenuItem.Checked = Global.Config.ShowLogWindow;

			DisplayLagCounterMenuItem.Enabled = Emulator.CanPollInput();

			DisplayMessagesMenuItem.Checked = Global.Config.DisplayMessages;
		}

		private void WindowSizeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			x1MenuItem.Checked =
				x2MenuItem.Checked =
				x3MenuItem.Checked =
				x4MenuItem.Checked =
				x5MenuItem.Checked = false;

			switch (Global.Config.TargetZoomFactors[Emulator.SystemId])
			{
				case 1:
					x1MenuItem.Checked = true;
					break;
				case 2:
					x2MenuItem.Checked = true;
					break;
				case 3:
					x3MenuItem.Checked = true;
					break;
				case 4:
					x4MenuItem.Checked = true;
					break;
				case 5:
					x5MenuItem.Checked = true;
					break;
				case 10:
					mzMenuItem.Checked = true;
					break;
			}
		}

		private void WindowSize_Click(object sender, EventArgs e)
		{
			if (sender == x1MenuItem) Global.Config.TargetZoomFactors[Emulator.SystemId] = 1;
			if (sender == x2MenuItem) Global.Config.TargetZoomFactors[Emulator.SystemId] = 2;
			if (sender == x3MenuItem) Global.Config.TargetZoomFactors[Emulator.SystemId] = 3;
			if (sender == x4MenuItem) Global.Config.TargetZoomFactors[Emulator.SystemId] = 4;
			if (sender == x5MenuItem) Global.Config.TargetZoomFactors[Emulator.SystemId] = 5;
			if (sender == mzMenuItem) Global.Config.TargetZoomFactors[Emulator.SystemId] = 10;

			FrameBufferResized();
		}

		private void SwitchToFullscreenMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFullscreen();
		}

		private void DisplayFpsMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFps();
		}

		private void DisplayFrameCounterMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFrameCounter();
		}

		private void DisplayLagCounterMenuItem_Click(object sender, EventArgs e)
		{
			ToggleLagCounter();
		}

		private void DisplayInputMenuItem_Click(object sender, EventArgs e)
		{
			ToggleInputDisplay();
		}

		private void DisplayRerecordsMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisplayRerecordCount ^= true;
		}

		private void DisplaySubtitlesMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisplaySubtitles ^= true;
		}

		private void DisplayStatusBarMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DispChrome_StatusBarWindowed ^= true;
			SetStatusBar();
		}

		private void DisplayMessagesMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisplayMessages ^= true;
		}

		private void DisplayLogWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.ShowLogWindow ^= true;

			if (Global.Config.ShowLogWindow)
			{
				LogConsole.ShowConsole();
			}
			else
			{
				LogConsole.HideConsole();
			}
		}

		#endregion

		#region Config

		private void ConfigSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ControllersMenuItem.Enabled = Emulator.ControllerDefinition.Any();
			RewindOptionsMenuItem.Enabled = Emulator.HasSavestates();
		}

		private void FrameSkipMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			MinimizeSkippingMenuItem.Checked = Global.Config.AutoMinimizeSkipping;
			ClockThrottleMenuItem.Checked = Global.Config.ClockThrottle;
			VsyncThrottleMenuItem.Checked = Global.Config.VSyncThrottle;
			NeverSkipMenuItem.Checked = Global.Config.FrameSkip == 0;
			Frameskip1MenuItem.Checked = Global.Config.FrameSkip == 1;
			Frameskip2MenuItem.Checked = Global.Config.FrameSkip == 2;
			Frameskip3MenuItem.Checked = Global.Config.FrameSkip == 3;
			Frameskip4MenuItem.Checked = Global.Config.FrameSkip == 4;
			Frameskip5MenuItem.Checked = Global.Config.FrameSkip == 5;
			Frameskip6MenuItem.Checked = Global.Config.FrameSkip == 6;
			Frameskip7MenuItem.Checked = Global.Config.FrameSkip == 7;
			Frameskip8MenuItem.Checked = Global.Config.FrameSkip == 8;
			Frameskip9MenuItem.Checked = Global.Config.FrameSkip == 9;
			MinimizeSkippingMenuItem.Enabled = !NeverSkipMenuItem.Checked;
			if (!MinimizeSkippingMenuItem.Enabled)
			{
				MinimizeSkippingMenuItem.Checked = true;
			}

			AudioThrottleMenuItem.Enabled = Global.Config.SoundEnabled;
			AudioThrottleMenuItem.Checked = Global.Config.SoundThrottle;
			VsyncEnabledMenuItem.Checked = Global.Config.VSync;

			Speed100MenuItem.Checked = Global.Config.SpeedPercent == 100;
			Speed100MenuItem.Image = (Global.Config.SpeedPercentAlternate == 100) ? Properties.Resources.FastForward : null;
			Speed150MenuItem.Checked = Global.Config.SpeedPercent == 150;
			Speed150MenuItem.Image = (Global.Config.SpeedPercentAlternate == 150) ? Properties.Resources.FastForward : null;
			Speed400MenuItem.Checked = Global.Config.SpeedPercent == 400;
			Speed400MenuItem.Image = (Global.Config.SpeedPercentAlternate == 400) ? Properties.Resources.FastForward : null;
			Speed200MenuItem.Checked = Global.Config.SpeedPercent == 200;
			Speed200MenuItem.Image = (Global.Config.SpeedPercentAlternate == 200) ? Properties.Resources.FastForward : null;
			Speed75MenuItem.Checked = Global.Config.SpeedPercent == 75;
			Speed75MenuItem.Image = (Global.Config.SpeedPercentAlternate == 75) ? Properties.Resources.FastForward : null;
			Speed50MenuItem.Checked = Global.Config.SpeedPercent == 50;
			Speed50MenuItem.Image = (Global.Config.SpeedPercentAlternate == 50) ? Properties.Resources.FastForward : null;

			Speed50MenuItem.Enabled =
				Speed75MenuItem.Enabled =
				Speed100MenuItem.Enabled =
				Speed150MenuItem.Enabled =
				Speed200MenuItem.Enabled =
				Speed400MenuItem.Enabled =
				Global.Config.ClockThrottle;

			miUnthrottled.Checked = _unthrottled;
		}

		private void KeyPriorityMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			switch (Global.Config.Input_Hotkey_OverrideOptions)
			{
				default:
				case 0:
					BothHkAndControllerMenuItem.Checked = true;
					InputOverHkMenuItem.Checked = false;
					HkOverInputMenuItem.Checked = false;
					break;
				case 1:
					BothHkAndControllerMenuItem.Checked = false;
					InputOverHkMenuItem.Checked = true;
					HkOverInputMenuItem.Checked = false;
					break;
				case 2:
					BothHkAndControllerMenuItem.Checked = false;
					InputOverHkMenuItem.Checked = false;
					HkOverInputMenuItem.Checked = true;
					break;
			}
		}

		private void CoreToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			quickNESToolStripMenuItem.Checked = Global.Config.NES_InQuickNES;
			nesHawkToolStripMenuItem.Checked = !Global.Config.NES_InQuickNES;
		}

		private void ControllersMenuItem_Click(object sender, EventArgs e)
		{
			using var controller = new ControllerConfig(Emulator.ControllerDefinition);
			if (controller.ShowDialog() == DialogResult.OK)
			{
				InitControls();
				InputManager.SyncControls();
			}
		}

		private void HotkeysMenuItem_Click(object sender, EventArgs e)
		{
			using var hotkeyConfig = new HotkeyConfig();
			if (hotkeyConfig.ShowDialog() == DialogResult.OK)
			{
				InitControls();
				InputManager.SyncControls();
			}
		}

		private void FirmwaresMenuItem_Click(object sender, EventArgs e)
		{
			if (e is RomLoader.RomErrorArgs args)
			{
				using var configForm = new FirmwaresConfig(true, args.RomPath);
				var result = configForm.ShowDialog();
				args.Retry = result == DialogResult.Retry;
			}
			else
			{
				using var configForm = new FirmwaresConfig();
				configForm.ShowDialog();
			}
		}

		private void MessagesMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new MessageConfig();
			form.ShowDialog();
		}

		private void PathsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new PathConfig();
			form.ShowDialog();
		}

		private void SoundMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new SoundConfig();
			if (form.ShowDialog() == DialogResult.OK)
			{
				RewireSound();
			}
		}

		private void AutofireMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new AutofireConfig();
			form.ShowDialog();
		}

		private void RewindOptionsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new RewindConfig();
			form.ShowDialog();
		}

		private void FileExtensionsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new FileExtensionPreferences();
			form.ShowDialog();
		}

		private void CustomizeMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new EmuHawkOptions();
			form.ShowDialog();
		}

		private void ProfilesMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new ProfileConfig();
			if (form.ShowDialog() == DialogResult.OK)
			{
				GlobalWin.OSD.AddMessage("Profile settings saved");

				// We hide the FirstBoot items since the user setup a Profile
				// Is it a bad thing to do this constantly?
				Global.Config.FirstBoot = false;
				ProfileFirstBootLabel.Visible = false;
			}
			else
			{
				GlobalWin.OSD.AddMessage("Profile config aborted");
			}
		}

		private void ClockThrottleMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.ClockThrottle ^= true;
			if (Global.Config.ClockThrottle)
			{
				var old = Global.Config.SoundThrottle;
				Global.Config.SoundThrottle = false;
				if (old)
				{
					RewireSound();
				}

				old = Global.Config.VSyncThrottle;
				Global.Config.VSyncThrottle = false;
				if (old)
				{
					PresentationPanel.Resized = true;
				}
			}

			ThrottleMessage();
		}

		private void AudioThrottleMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SoundThrottle ^= true;
			RewireSound();
			if (Global.Config.SoundThrottle)
			{
				Global.Config.ClockThrottle = false;
				var old = Global.Config.VSyncThrottle;
				Global.Config.VSyncThrottle = false;
				if (old)
				{
					PresentationPanel.Resized = true;
				}
			}

			ThrottleMessage();
		}

		private void VsyncThrottleMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VSyncThrottle ^= true;
			PresentationPanel.Resized = true;
			if (Global.Config.VSyncThrottle)
			{
				Global.Config.ClockThrottle = false;
				var old = Global.Config.SoundThrottle;
				Global.Config.SoundThrottle = false;
				if (old)
				{
					RewireSound();
				}
			}

			if (!Global.Config.VSync)
			{
				Global.Config.VSync = true;
				VsyncMessage();
			}

			ThrottleMessage();
		}

		private void VsyncEnabledMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VSync ^= true;
			if (!Global.Config.VSyncThrottle) // when vsync throttle is on, vsync is forced to on, so no change to make here
			{
				PresentationPanel.Resized = true;
			}

			VsyncMessage();
		}

		private void UnthrottledMenuItem_Click(object sender, EventArgs e)
		{
			_unthrottled ^= true;
			ThrottleMessage();
		}

		private void MinimizeSkippingMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoMinimizeSkipping ^= true;
		}

		private void NeverSkipMenuItem_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 0; FrameSkipMessage(); }
		private void Frameskip1MenuItem_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 1; FrameSkipMessage(); }
		private void Frameskip2MenuItem_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 2; FrameSkipMessage(); }
		private void Frameskip3MenuItem_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 3; FrameSkipMessage(); }
		private void Frameskip4MenuItem_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 4; FrameSkipMessage(); }
		private void Frameskip5MenuItem_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 5; FrameSkipMessage(); }
		private void Frameskip6MenuItem_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 6; FrameSkipMessage(); }
		private void Frameskip7MenuItem_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 7; FrameSkipMessage(); }
		private void Frameskip8MenuItem_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 8; FrameSkipMessage(); }
		private void Frameskip9MenuItem_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 9; FrameSkipMessage(); }

		private void Speed50MenuItem_Click(object sender, EventArgs e) { ClickSpeedItem(50); }
		private void Speed75MenuItem_Click(object sender, EventArgs e) { ClickSpeedItem(75); }
		private void Speed100MenuItem_Click(object sender, EventArgs e) { ClickSpeedItem(100); }
		private void Speed150MenuItem_Click(object sender, EventArgs e) { ClickSpeedItem(150); }
		private void Speed200MenuItem_Click(object sender, EventArgs e) { ClickSpeedItem(200); }
		private void Speed400MenuItem_Click(object sender, EventArgs e) { ClickSpeedItem(400); }

		private void BothHkAndControllerMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Input_Hotkey_OverrideOptions = 0;
			UpdateKeyPriorityIcon();
		}

		private void InputOverHkMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Input_Hotkey_OverrideOptions = 1;
			UpdateKeyPriorityIcon();
		}

		private void HkOverInputMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Input_Hotkey_OverrideOptions = 2;
			UpdateKeyPriorityIcon();
		}

		private void CoresSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			GBInSGBMenuItem.Checked = Global.Config.GB_AsSGB;
			allowGameDBCoreOverridesToolStripMenuItem.Checked = Global.Config.CoreForcingViaGameDB;
		}

		private void NesCoreSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			QuicknesCoreMenuItem.Checked = Global.Config.NES_InQuickNES;
			NesCoreMenuItem.Checked = !Global.Config.NES_InQuickNES && !Global.Config.UseSubNESHawk;
			SubNesHawkMenuItem.Checked = Global.Config.UseSubNESHawk;
		}

		private void NesCorePick_Click(object sender, EventArgs e)
		{
			Global.Config.NES_InQuickNES ^= true;
			Global.Config.UseSubNESHawk = false;

			if (Emulator.SystemId == "NES")
			{
				FlagNeedsReboot();
			}
		}

		private void SubNesCorePick_Click(object sender, EventArgs e)
		{
			Global.Config.UseSubNESHawk = true;
			Global.Config.NES_InQuickNES = false;

			if (!Emulator.IsNull())
			{
				FlagNeedsReboot();
			}
		}

		private void CoreSNESSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			Coresnes9xMenuItem.Checked = Global.Config.SNES_InSnes9x;
			CorebsnesMenuItem.Checked = !Global.Config.SNES_InSnes9x;
		}

		private void CoreSnesToggle_Click(object sender, EventArgs e)
		{
			Global.Config.SNES_InSnes9x ^= true;

			if (Emulator.SystemId == "SNES")
			{
				FlagNeedsReboot();
			}
		}

		private void GbaCoreSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			VbaNextCoreMenuItem.Checked = !Global.Config.GBA_UsemGBA;
			MgbaCoreMenuItem.Checked = Global.Config.GBA_UsemGBA;
		}

		private void GbaCorePick_Click(object sender, EventArgs e)
		{
			Global.Config.GBA_UsemGBA ^= true;
			if (Emulator.SystemId == "GBA")
			{
				FlagNeedsReboot();
			}
		}

		private void SGBCoreSubmenu_DropDownOpened(object sender, EventArgs e)
		{
			SgbBsnesMenuItem.Checked = Global.Config.SGB_UseBsnes;
			SgbSameBoyMenuItem.Checked = !Global.Config.SGB_UseBsnes;
		}

		private void GBCoreSubmenu_DropDownOpened(object sender, EventArgs e)
		{
			GBGambatteMenuItem.Checked = !Global.Config.GB_UseGBHawk;
			GBGBHawkMenuItem.Checked = Global.Config.GB_UseGBHawk;
		}

		private void SgbCorePick_Click(object sender, EventArgs e)
		{
			Global.Config.SGB_UseBsnes ^= true;
			// TODO: only flag if one of these cores
			if (!Emulator.IsNull())
			{
				FlagNeedsReboot();
			}
		}

		private void GBCorePick_Click(object sender, EventArgs e)
		{
			Global.Config.GB_UseGBHawk ^= true;
			// TODO: only flag if one of these cores
			if (!Emulator.IsNull())
			{
				FlagNeedsReboot();
			}
		}

		private void GbInSgbMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GB_AsSGB ^= true;

			if (!Emulator.IsNull())
			{
				FlagNeedsReboot();
			}
		}

		private void AllowGameDBCoreOverridesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.CoreForcingViaGameDB ^= true;
		}

		private void N64VideoPluginSettingsMenuItem_Click(object sender, EventArgs e)
		{
			N64PluginSettingsMenuItem_Click(sender, e);
		}

		private void SetLibretroCoreMenuItem_Click(object sender, EventArgs e)
		{
			RunLibretroCoreChooser();
		}

		private void SaveConfigMenuItem_Click(object sender, EventArgs e)
		{
			SaveConfig();
			GlobalWin.OSD.AddMessage("Saved settings");
		}

		private void SaveConfigAsMenuItem_Click(object sender, EventArgs e)
		{
			var path = PathManager.DefaultIniPath;
			using var sfd = new SaveFileDialog
			{
				InitialDirectory = Path.GetDirectoryName(path),
				FileName = Path.GetFileName(path),
				Filter = "Config File (*.ini)|*.ini"
			};

			var result = sfd.ShowHawkDialog();
			if (result == DialogResult.OK)
			{
				SaveConfig(sfd.FileName);
				GlobalWin.OSD.AddMessage("Copied settings");
			}
		}

		private void LoadConfigMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config = ConfigService.Load<Config>(PathManager.DefaultIniPath);
			Global.Config.ResolveDefaults();
			InitControls(); // rebind hotkeys
			GlobalWin.OSD.AddMessage($"Config file loaded: {PathManager.DefaultIniPath}");
		}

		private void LoadConfigFromMenuItem_Click(object sender, EventArgs e)
		{
			var path = PathManager.DefaultIniPath;
			using var ofd = new OpenFileDialog
			{
				InitialDirectory = Path.GetDirectoryName(path),
				FileName = Path.GetFileName(path),
				Filter = "Config File (*.ini)|*.ini"
			};

			var result = ofd.ShowHawkDialog();
			if (result == DialogResult.OK)
			{
				Global.Config = ConfigService.Load<Config>(ofd.FileName);
				Global.Config.ResolveDefaults();
				InitControls(); // rebind hotkeys
				GlobalWin.OSD.AddMessage($"Config file loaded: {ofd.FileName}");
			}
		}

		#endregion

		#region Tools

		private void ToolsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ToolBoxMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["ToolBox"].Bindings;
			RamWatchMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["RAM Watch"].Bindings;
			RamSearchMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["RAM Search"].Bindings;
			HexEditorMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Hex Editor"].Bindings;
			LuaConsoleMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Lua Console"].Bindings;
			CheatsMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Cheats"].Bindings;
			TAStudioMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["TAStudio"].Bindings;
			VirtualPadMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Virtual Pad"].Bindings;
			TraceLoggerMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Trace Logger"].Bindings;
			TraceLoggerMenuItem.Enabled = GlobalWin.Tools.IsAvailable<TraceLogger>();
			CodeDataLoggerMenuItem.Enabled = GlobalWin.Tools.IsAvailable<CDL>();

			TAStudioMenuItem.Enabled = GlobalWin.Tools.IsAvailable<TAStudio>();

			CheatsMenuItem.Enabled = GlobalWin.Tools.IsAvailable<Cheats>();
			HexEditorMenuItem.Enabled = GlobalWin.Tools.IsAvailable<HexEditor>();
			RamSearchMenuItem.Enabled = GlobalWin.Tools.IsAvailable<RamSearch>();
			RamWatchMenuItem.Enabled = GlobalWin.Tools.IsAvailable<RamWatch>();

			DebuggerMenuItem.Enabled = GlobalWin.Tools.IsAvailable<GenericDebugger>();

			batchRunnerToolStripMenuItem.Visible = VersionInfo.DeveloperBuild;

			BasicBotMenuItem.Enabled = GlobalWin.Tools.IsAvailable<BasicBot>();

			gameSharkConverterToolStripMenuItem.Enabled = GlobalWin.Tools.IsAvailable<GameShark>();
		}

		private void ExternalToolToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			externalToolToolStripMenuItem.DropDownItems.Clear();

			foreach (ToolStripMenuItem item in ExternalToolManager.ToolStripMenu)
			{
				if (item.Enabled)
				{
					item.Click += delegate
					{
						GlobalWin.Tools.Load<IExternalToolForm>((string)item.Tag);
					};
				}
				else
				{
					item.Image = Properties.Resources.ExclamationRed;
				}

				externalToolToolStripMenuItem.DropDownItems.Add(item);
			}

			if (externalToolToolStripMenuItem.DropDownItems.Count == 0)
			{
				externalToolToolStripMenuItem.DropDownItems.Add("None");
			}
		}

		private void ToolBoxMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<ToolBox>();
		}

		private void RamWatchMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadRamWatch(true);
		}

		private void RamSearchMenuItem_Click(object sender, EventArgs e)
		{
			var ramSearch = GlobalWin.Tools.Load<RamSearch>();
			if (OSTailoredCode.IsUnixHost)
			{
				// this is apparently needed for weird mono-forms-on-different-thread issues
				// don't do .Show() within Load<T>() for RamSearch - instead put an instance of it here on MainForm, then show here
				// the mono winforms implementation is.... weird and buggy
				ramSearch.Show();
			}
		}

		private void LuaConsoleMenuItem_Click(object sender, EventArgs e)
		{
			OpenLuaConsole();
		}

		private void TAStudioMenuItem_Click(object sender, EventArgs e)
		{
			if (!Emulator.CanPollInput())
			{
				MessageBox.Show("Current core does not support input polling. TAStudio can't be used.");
				return;
			}

			GlobalWin.Tools.Load<TAStudio>();
		}

		private void HexEditorMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<HexEditor>();
		}

		private void TraceLoggerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<TraceLogger>();
		}

		private void DebuggerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GenericDebugger>();
		}

		private void CodeDataLoggerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<CDL>();
		}

		private void MacroToolMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<MacroInputTool>();
		}

		private void VirtualPadMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<VirtualpadTool>();
		}

		private void BasicBotMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<BasicBot>();
		}

		private void CheatsMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<Cheats>();
		}

		private void CheatCodeConverterMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GameShark>();
		}

		private void MultidiskBundlerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<MultiDiskBundler>();
		}

		private void BatchRunnerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new BatchRun();
			form.ShowDialog();
		}

		#endregion

		#region NES

		private void QuickNesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.NES_InQuickNES = true;
			FlagNeedsReboot();
		}

		private void NesHawkToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.NES_InQuickNES = false;
			FlagNeedsReboot();
		}

		private void NESSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			var boardName = Emulator.HasBoardInfo() ? Emulator.AsBoardInfo().BoardName : null;
			FDSControlsMenuItem.Enabled = boardName == "FDS";

			VSControlsMenuItem.Enabled =
			VSSettingsMenuItem.Enabled =
				Emulator is NES nes && nes.IsVS;

			NESSoundChannelsMenuItem.Enabled = GlobalWin.Tools.IsAvailable<NESSoundConfig>();
			MovieSettingsMenuItem.Enabled = Emulator is NES && !Global.MovieSession.Movie.IsActive;

			NesControllerSettingsMenuItem.Enabled = GlobalWin.Tools.IsAvailable<NesControllerSettings>()
				&& !Global.MovieSession.Movie.IsActive;

			barcodeReaderToolStripMenuItem.Enabled = ServiceInjector.IsAvailable(Emulator.ServiceProvider, typeof(BarcodeEntry));

			musicRipperToolStripMenuItem.Enabled = GlobalWin.Tools.IsAvailable<NESMusicRipper>();
		}

		private void FdsControlsMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			var boardName = Emulator.HasBoardInfo() ? Emulator.AsBoardInfo().BoardName : null;
			FdsEjectDiskMenuItem.Enabled = boardName == "FDS";

			while (FDSControlsMenuItem.DropDownItems.Count > 1)
			{
				FDSControlsMenuItem.DropDownItems.RemoveAt(1);
			}

			for (int i = 0; i < 16; i++)
			{
				var str = $"FDS Insert {i}";
				if (Emulator.ControllerDefinition.BoolButtons.Contains(str))
				{
					FdsInsertDiskMenuAdd($"Insert Disk {i}", str, $"FDS Disk {i} inserted.");
				}
			}
		}

		private void NesPPUViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NesPPU>();
		}

		private void NESNametableViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NESNameTableViewer>();
		}

		private void MusicRipperMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NESMusicRipper>();
		}

		private void NESGameGenieCodesMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadGameGenieEc();
		}

		private void NESGraphicSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is NES)
			{
				using var form = new NESGraphicsConfig();
				form.ShowDialog(this);
			}
			else if (Emulator is SubNESHawk)
			{
				using var form = new NESGraphicsConfig();
				form.ShowDialog(this);
			}
			else if (Emulator is QuickNES)
			{
				using var form = new QuickNesConfig();
				form.ShowDialog(this);
			}
		}

		private void NESSoundChannelsMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NESSoundConfig>();
		}

		private void VsSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is NES nes && nes.IsVS)
			{
				using var form = new NesVsSettings();
				form.ShowHawkDialog();
			}
		}

		private void FdsEjectDiskMenuItem_Click(object sender, EventArgs e)
		{
			if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
			{
				Global.ClickyVirtualPadController.Click("FDS Eject");
				GlobalWin.OSD.AddMessage("FDS disk ejected.");
			}
		}

		private void VsInsertCoinP1MenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is NES nes && nes.IsVS)
			{
				if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
				{
					Global.ClickyVirtualPadController.Click("Insert Coin P1");
					GlobalWin.OSD.AddMessage("P1 Coin Inserted");
				}
			}
		}

		private void VsInsertCoinP2MenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is NES nes && nes.IsVS)
			{
				if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
				{
					Global.ClickyVirtualPadController.Click("Insert Coin P2");
					GlobalWin.OSD.AddMessage("P2 Coin Inserted");
				}
			}
		}

		private void VsServiceSwitchMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is NES nes && nes.IsVS)
			{
				if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
				{
					Global.ClickyVirtualPadController.Click("Service Switch");
					GlobalWin.OSD.AddMessage("Service Switch Pressed");
				}
			}
		}

		private void NesControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is NES)
			{
				using var form = new NesControllerSettings();
				form.ShowDialog();
			}
			else if (Emulator is SubNESHawk)
			{
				using var form = new NesControllerSettings();
				form.ShowDialog();
			}
			else if (Emulator is QuickNES)
			{
				GenericCoreConfig.DoDialog(this, "QuickNES Controller Settings", true, false);
			}
		}

		private void MovieSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using (var dlg = new NESSyncSettingsForm())
			{
				dlg.ShowDialog(this);
			}
		}

		private void BarcodeReaderMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<BarcodeEntry>();
		}

		#endregion

		#region PCE

		private void PCESubMenu_DropDownOpened(object sender, EventArgs e)
		{
			var s = ((PCEngine)Emulator).GetSettings();

			PceControllerSettingsMenuItem.Enabled = !Global.MovieSession.Movie.IsActive;

			PCEAlwaysPerformSpriteLimitMenuItem.Checked = s.SpriteLimit;
			PCEAlwaysEqualizeVolumesMenuItem.Checked = s.EqualizeVolume;
			PCEArcadeCardRewindEnableMenuItem.Checked = s.ArcadeCardRewindHack;
		}

		private void PceControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using (var dlg = new PCEControllerConfig())
			{
				dlg.ShowDialog();
			}
		}

		private void PceGraphicsSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new PCEGraphicsConfig();
			form.ShowDialog();
		}

		private void PceBgViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<PceBgViewer>();
		}

		private void PceTileViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<PCETileViewer>();
		}

		private void PceSoundDebuggerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<PCESoundDebugger>();
		}

		private void PCEAlwaysPerformSpriteLimitMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((PCEngine)Emulator).GetSettings();
			s.SpriteLimit ^= true;
			PutCoreSettings(s);
		}

		private void PCEAlwaysEqualizeVolumesMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((PCEngine)Emulator).GetSettings();
			s.EqualizeVolume ^= true;
			PutCoreSettings(s);
		}

		private void PCEArcadeCardRewindEnableMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((PCEngine)Emulator).GetSettings();
			s.ArcadeCardRewindHack ^= true;
			PutCoreSettings(s);
		}

		#endregion

		#region SMS

		private void SMSSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			var s = ((SMS)Emulator).GetSettings();
			var ss = ((SMS)Emulator).GetSyncSettings();
			SMSregionExportToolStripMenuItem.Checked = ss.ConsoleRegion == "Export";
			SMSregionJapanToolStripMenuItem.Checked = ss.ConsoleRegion == "Japan";
			SMSregionKoreaToolStripMenuItem.Checked = ss.ConsoleRegion == "Korea";
			SMSregionAutoToolStripMenuItem.Checked = ss.ConsoleRegion == "Auto";
			SMSdisplayNtscToolStripMenuItem.Checked = ss.DisplayType == "NTSC";
			SMSdisplayPalToolStripMenuItem.Checked = ss.DisplayType == "PAL";
			SMSdisplayAutoToolStripMenuItem.Checked = ss.DisplayType == "Auto";
			SMSControllerStandardToolStripMenuItem.Checked = ss.ControllerType == "Standard";
			SMSControllerPaddleToolStripMenuItem.Checked = ss.ControllerType == "Paddle";
			SMSControllerLightPhaserToolStripMenuItem.Checked = ss.ControllerType == "Light Phaser";
			SMSControllerSportsPadToolStripMenuItem.Checked = ss.ControllerType == "Sports Pad";
			SMSControllerKeyboardToolStripMenuItem.Checked = ss.ControllerType == "Keyboard";
			SMSenableBIOSToolStripMenuItem.Checked = ss.UseBIOS;
			SMSEnableFMChipMenuItem.Checked = ss.EnableFM;
			SMSOverclockMenuItem.Checked = ss.AllowOverlock;
			SMSForceStereoMenuItem.Checked = s.ForceStereoSeparation;
			SMSSpriteLimitMenuItem.Checked = s.SpriteLimit;
			SMSDisplayOverscanMenuItem.Checked = s.DisplayOverscan;
			SMSFix3DGameDisplayToolStripMenuItem.Checked = s.Fix3D;
			ShowClippedRegionsMenuItem.Checked = s.ShowClippedRegions;
			HighlightActiveDisplayRegionMenuItem.Checked = s.HighlightActiveDisplayRegion;

			SMSEnableFMChipMenuItem.Visible =
				SMSFix3DGameDisplayToolStripMenuItem.Visible =
				SMSenableBIOSToolStripMenuItem.Visible =
				Global.Game.System == "SMS";

			SMSDisplayOverscanMenuItem.Visible =
				Global.Game.System == "SMS" || Global.Game.System == "SG";

			SMSOverclockMenuItem.Visible =
				SMSForceStereoMenuItem.Visible =
				SMSdisplayToolStripMenuItem.Visible =
				Global.Game.System != "GG";

			ShowClippedRegionsMenuItem.Visible =
				HighlightActiveDisplayRegionMenuItem.Visible =
				GGGameGenieMenuItem.Visible =
				Global.Game.System == "GG";

			SMSOverclockMenuItem.Visible =
				SMSVDPViewerToolStripMenuItem.Visible =
				toolStripSeparator24.Visible =
				Global.Game.System != "SG";
		}

		private void SMS_RegionExport_Click(object sender, EventArgs e)
		{
			var ss = ((SMS)Emulator).GetSyncSettings();
			ss.ConsoleRegion = "Export";
			PutCoreSyncSettings(ss);
		}

		private void SMS_RegionJapan_Click(object sender, EventArgs e)
		{
			var ss = ((SMS)Emulator).GetSyncSettings();
			ss.ConsoleRegion = "Japan";
			PutCoreSyncSettings(ss);
		}

		private void SMS_RegionKorea_Click(object sender, EventArgs e)
		{
			var ss = ((SMS)Emulator).GetSyncSettings();
			ss.ConsoleRegion = "Korea";
			PutCoreSyncSettings(ss);
		}

		private void SMS_RegionAuto_Click(object sender, EventArgs e)
		{
			var ss = ((SMS)Emulator).GetSyncSettings();
			ss.ConsoleRegion = "Auto";
			PutCoreSyncSettings(ss);
		}

		private void SMS_DisplayNTSC_Click(object sender, EventArgs e)
		{
			var ss = ((SMS)Emulator).GetSyncSettings();
			ss.DisplayType = "NTSC";
			PutCoreSyncSettings(ss);
		}

		private void SMS_DisplayPAL_Click(object sender, EventArgs e)
		{
			var ss = ((SMS)Emulator).GetSyncSettings();
			ss.DisplayType = "PAL";
			PutCoreSyncSettings(ss);
		}

		private void SMS_DisplayAuto_Click(object sender, EventArgs e)
		{
			var ss = ((SMS)Emulator).GetSyncSettings();
			ss.DisplayType = "Auto";
			PutCoreSyncSettings(ss);
		}

		private void SmsBiosMenuItem_Click(object sender, EventArgs e)
		{
			var ss = ((SMS)Emulator).GetSyncSettings();
			ss.UseBIOS ^= true;
			PutCoreSyncSettings(ss);
		}

		private void SmsEnableFmChipMenuItem_Click(object sender, EventArgs e)
		{
			var ss = ((SMS)Emulator).GetSyncSettings();
			ss.EnableFM ^= true;
			PutCoreSyncSettings(ss);
		}

		private void SMSOverclockMenuItem_Click(object sender, EventArgs e)
		{
			var ss = ((SMS)Emulator).GetSyncSettings();
			ss.AllowOverlock ^= true;
			PutCoreSyncSettings(ss);
		}

		private void SMSForceStereoMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((SMS)Emulator).GetSettings();
			s.ForceStereoSeparation ^= true;
			PutCoreSettings(s);
		}

		private void SMSSpriteLimitMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((SMS)Emulator).GetSettings();
			s.SpriteLimit ^= true;
			PutCoreSettings(s);
		}

		private void SMSDisplayOverscanMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((SMS)Emulator).GetSettings();
			s.DisplayOverscan ^= true;
			PutCoreSettings(s);
		}

		private void SMSFix3DDisplayMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((SMS)Emulator).GetSettings();
			s.Fix3D ^= true;
			PutCoreSettings(s);
		}

		private void ShowClippedRegionsMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((SMS)Emulator).GetSettings();
			s.ShowClippedRegions ^= true;
			PutCoreSettings(s);
		}

		private void HighlightActiveDisplayRegionMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((SMS)Emulator).GetSettings();
			s.HighlightActiveDisplayRegion ^= true;
			PutCoreSettings(s);
		}

		private void SMSGraphicsSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new SMSGraphicsConfig();
			form.ShowDialog();
		}

		private void GGGameGenieMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadGameGenieEc();
		}

		private void SmsVdpViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<SmsVDPViewer>();
		}

		private void SMSControllerStandardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((SMS)Emulator).GetSyncSettings();
			s.ControllerType = "Standard";
			PutCoreSyncSettings(s);
		}

		private void SMSControllerPaddleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((SMS)Emulator).GetSyncSettings();
			s.ControllerType = "Paddle";
			PutCoreSyncSettings(s);
		}

		private void SMSControllerLightPhaserToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((SMS)Emulator).GetSyncSettings();
			s.ControllerType = "Light Phaser";
			PutCoreSyncSettings(s);
		}

		private void SMSControllerSportsPadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((SMS)Emulator).GetSyncSettings();
			s.ControllerType = "Sports Pad";
			PutCoreSyncSettings(s);
		}

		private void SMSControllerKeyboardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var s = ((SMS)Emulator).GetSyncSettings();
			s.ControllerType = "Keyboard";
			PutCoreSyncSettings(s);
		}

		#endregion

		#region TI83

		private void TI83SubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadKeypadMenuItem.Checked = Global.Config.TI83autoloadKeyPad;
		}

		private void KeypadMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<TI83KeyPad>();
		}

		private void AutoloadKeypadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TI83autoloadKeyPad ^= true;
		}

		private void LoadTIFileMenuItem_Click(object sender, EventArgs e)
		{
			var ti83 = (TI83)Emulator;
			using var ofd = new OpenFileDialog
			{
				InitialDirectory = PathManager.GetRomsPath(Emulator.SystemId),
				Filter = "TI-83 Program Files (*.83p,*.8xp)|*.83P;*.8xp|All Files|*.*",
				RestoreDirectory = true
			};

			if (ofd.ShowDialog() == DialogResult.OK)
			{
				try
				{
					ti83.LinkPort.SendFileToCalc(File.OpenRead(ofd.FileName), true);
				}
				catch (IOException ex)
				{
					var message = $"Invalid file format. Reason: {ex.Message} \nForce transfer? This may cause the calculator to crash.";

					if (MessageBox.Show(message, "Upload Failed", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes)
					{
						ti83.LinkPort.SendFileToCalc(File.OpenRead(ofd.FileName), false);
					}
				}
			}
		}

		private void TI83PaletteMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new TI83PaletteConfig();
			GlobalWin.OSD.AddMessage(form.ShowDialog() == DialogResult.OK
				? "Palette settings saved"
				: "Palette config aborted");
		}

		#endregion

		#region Atari

		private void AtariSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Atari 2600 Settings");
		}

		#endregion

		#region Atari7800

		private void A7800SubMenu_DropDownOpened(object sender, EventArgs e)
		{
			A7800ControllerSettingsMenuItem.Enabled = !Global.MovieSession.Movie.IsActive;
			A7800FilterSettingsMenuItem.Enabled = !Global.MovieSession.Movie.IsActive;
		}

		private void A7800ControllerSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new A7800ControllerSettings();
			form.ShowDialog();
		}

		private void A7800FilterSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new A7800FilterSettings();
			form.ShowDialog();
		}

		#endregion

		#region GB

		private void GBSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			LoadGBInSGBMenuItem.Checked = Global.Config.GB_AsSGB;
		}

		private void GBCoreSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is Gameboy)
			{
				GBPrefs.DoGBPrefsDialog(this);
			}
			else // SameBoy
			{
				GenericCoreConfig.DoDialog(this, "Gameboy Settings");
			}
		}

		private void LoadGbInSgbMenuItem_Click(object sender, EventArgs e)
		{
			SnesGbInSgbMenuItem_Click(sender, e);
		}

		private void GbGpuViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GBGPUView>();
		}

		private void GBGameGenieMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadGameGenieEc();
		}

		private void GBPrinterViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GBPrinterView>();
		}

		#endregion

		#region GBA

		private void GBACoreSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Gameboy Advance Settings");
		}

		private void GbaGpuViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GBAGPUView>();
		}

		private void UsemGBAMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GBA_UsemGBA = true;
			FlagNeedsReboot();
		}

		private void UseVbaNextMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GBA_UsemGBA = false;
			FlagNeedsReboot();
		}

		private void GBACoreSelectionSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			GBAmGBAMenuItem.Checked = Global.Config.GBA_UsemGBA;
			GBAVBANextMenuItem.Checked = !Global.Config.GBA_UsemGBA;
		}

		#endregion

		#region PSX

		private void PSXSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			PSXControllerSettingsMenuItem.Enabled = !Global.MovieSession.Movie.IsActive;
		}

		private void PSXControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new PSXControllerConfigNew();
			form.ShowDialog();
		}

		private void PSXOptionsMenuItem_Click(object sender, EventArgs e)
		{
			var result = PSXOptions.DoSettingsDialog(this);
			if (result == DialogResult.OK)
			{
				FrameBufferResized();
			}
		}

		private void PSXDiscControlsMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<VirtualpadTool>().ScrollToPadSchema("Console");
		}

		private void PSXHashDiscsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new PSXHashDiscs();
			form.ShowDialog();
		}

		#endregion

		#region SNES

		private void SNESSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			if (((LibsnesCore)Emulator).IsSGB)
			{
				SnesGBInSGBMenuItem.Visible = true;
				SnesGBInSGBMenuItem.Checked = Global.Config.GB_AsSGB;
			}
			else
			{
				SnesGBInSGBMenuItem.Visible = false;
			}

			SNESControllerConfigurationMenuItem.Enabled = !Global.MovieSession.Movie.IsActive;
		}

		private void SNESControllerConfigurationMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new SNESControllerSettings();
			form.ShowDialog();
		}

		private void SnesGfxDebuggerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<SNESGraphicsDebugger>();
		}

		private void SnesGbInSgbMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GB_AsSGB ^= true;
			FlagNeedsReboot();
		}

		private void SnesGameGenieMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadGameGenieEc();
		}

		private void SnesOptionsMenuItem_Click(object sender, EventArgs e)
		{
			SNESOptions.DoSettingsDialog(this);
		}

		private void Snes9xSettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Snes9x Settings");
		}

		#endregion

		#region Coleco

		private void ColecoSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			var ss = ((ColecoVision)Emulator).GetSyncSettings();
			ColecoSkipBiosMenuItem.Checked = ss.SkipBiosIntro;
			ColecoUseSGMMenuItem.Checked = ss.UseSGM;
			ColecoControllerSettingsMenuItem.Enabled = !Global.MovieSession.Movie.IsActive;
		}

		private void ColecoSkipBiosMenuItem_Click(object sender, EventArgs e)
		{
			var ss = ((ColecoVision)Emulator).GetSyncSettings();
			ss.SkipBiosIntro ^= true;
			PutCoreSyncSettings(ss);
		}

		private void ColecoUseSGMMenuItem_Click(object sender, EventArgs e)
		{
			var ss = ((ColecoVision)Emulator).GetSyncSettings();
			ss.UseSGM ^= true;
			PutCoreSyncSettings(ss);
		}

		private void ColecoControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new ColecoControllerSettings();
			form.ShowDialog();
		}

		#endregion

		#region N64

		private void N64SubMenu_DropDownOpened(object sender, EventArgs e)
		{
			N64PluginSettingsMenuItem.Enabled =
				N64ControllerSettingsMenuItem.Enabled =
				N64ExpansionSlotMenuItem.Enabled =
				!Global.MovieSession.Movie.IsActive;

			N64CircularAnalogRangeMenuItem.Checked = Global.Config.N64UseCircularAnalogConstraint;

			var s = ((N64)Emulator).GetSettings();
			MupenStyleLagMenuItem.Checked = s.UseMupenStyleLag;

			N64ExpansionSlotMenuItem.Checked = ((N64)Emulator).UsingExpansionSlot;
			N64ExpansionSlotMenuItem.Enabled = !((N64)Emulator).IsOverridingUserExpansionSlotSetting;
		}

		private void N64PluginSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new N64VideoPluginconfig();
			if (form.ShowDialog() == DialogResult.OK)
			{
				if (Emulator.IsNull())
				{
					GlobalWin.OSD.AddMessage("Plugin settings saved");
				}
				else
				{
					// Do nothing, Reboot is being flagged already if they changed anything
				}
			}
			else
			{
				GlobalWin.OSD.AddMessage("Plugin settings aborted");
			}
		}

		private void N64ControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new N64ControllersSetup();
			if (form.ShowDialog() == DialogResult.OK)
			{
				FlagNeedsReboot();
				GlobalWin.OSD.AddMessage("Controller settings saved but a core reboot is required");
			}
			else
			{
				GlobalWin.OSD.AddMessage("Controller settings aborted");
			}
		}

		private void N64CircularAnalogRangeMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.N64UseCircularAnalogConstraint ^= true;
		}

		private void MupenStyleLagMenuItem_Click(object sender, EventArgs e)
		{
			var n64 = (N64)Emulator;
			var s = n64.GetSettings();
			s.UseMupenStyleLag ^= true;
			n64.PutSettings(s);
		}

		private void N64ExpansionSlotMenuItem_Click(object sender, EventArgs e)
		{
			var n64 = (N64)Emulator;
			var ss = n64.GetSyncSettings();
			ss.DisableExpansionSlot ^= true;
			n64.PutSyncSettings(ss);
			FlagNeedsReboot();
		}

		#endregion

		#region Saturn

		private void SaturnPreferencesMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Saturn Settings");
		}

		#endregion

		#region DGB

		private void DgbSettingsMenuItem_Click(object sender, EventArgs e)
		{
			DGBPrefs.DoDGBPrefsDialog(this);
		}

		private void DgbHawkSettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Gameboy Settings");
		}

		#endregion

		#region GB3x

		private void GB3xSettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Gameboy Settings");
		}

		#endregion

		#region GB4x

		private void GB4xSettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Gameboy Settings");
		}

		#endregion

		#region GGL

		private void GGLSettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Game Gear Settings");
		}

		#endregion

		#region Vectrex

		private void VectrexSettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Vectrex Settings");
		}

		#endregion

		#region O2Hawk

		private void O2HawkSettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Odyssey Settings");
		}

		#endregion

		#region GEN

		private void GenVdpViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GenVDPViewer>();
		}

		private void GenesisSettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Genesis Settings");
		}

		private void GenesisGameGenieEcDc_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GenGameGenie>();
		}

		#endregion

		#region Wondersawn

		private void WonderSwanSettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "WonderSwan Settings");
		}

		#endregion

		#region Apple II

		private void AppleIISettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Apple II Settings");
		}

		private void AppleSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is AppleII)
			{
				AppleDisksSubMenu.Enabled = ((AppleII)Emulator).DiskCount > 1;
			}
		}

		private void AppleDisksSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AppleDisksSubMenu.DropDownItems.Clear();

			if (Emulator is AppleII appleII)
			{
				for (int i = 0; i < appleII.DiskCount; i++)
				{
					var menuItem = new ToolStripMenuItem
					{
						Name = $"Disk{i + 1}",
						Text = $"Disk{i + 1}",
						Checked = appleII.CurrentDisk == i
					};

					int dummy = i;
					menuItem.Click += (o, ev) =>
					{
						appleII.SetDisk(dummy);
					};

					AppleDisksSubMenu.DropDownItems.Add(menuItem);
				}
			}
		}

		#endregion

		#region C64

		private void C64SubMenu_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is C64)
			{
				C64DisksSubMenu.Enabled = ((C64)Emulator).DiskCount > 1;
			}
		}

		private void C64DisksSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			C64DisksSubMenu.DropDownItems.Clear();

			if (Emulator is C64 c64)
			{
				for (int i = 0; i < c64.DiskCount; i++)
				{
					var menuItem = new ToolStripMenuItem
					{
						Name = $"Disk{i + 1}",
						Text = $"Disk{i + 1}",
						Checked = c64.CurrentDisk == i
					};

					int dummy = i;
					menuItem.Click += (o, ev) =>
					{
						c64.SetDisk(dummy);
					};

					C64DisksSubMenu.DropDownItems.Add(menuItem);
				}
			}
		}

		private void C64SettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "C64 Settings");
		}

		#endregion

		#region Intv

		private void IntVSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			IntVControllerSettingsMenuItem.Enabled = !Global.MovieSession.Movie.IsActive;
		}

		private void IntVControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new IntvControllerSettings();
			form.ShowDialog();
		}

		#endregion

		#region VirtualBoy
		private void VirtualBoySettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "VirtualBoy Settings");
		}

		#endregion

		#region NeoGeoPocket

		private void NeoGeoSettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "NeoPop Settings");
		}

		#endregion

		#region PC-FX

		private void PCFXSettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "PC-FX Settings");
		}

		#endregion

		#region ZXSpectrum

		private void ZXSpectrumControllerConfigurationMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new ZXSpectrumJoystickSettings();
			form.ShowDialog();
		}

		private void ZXSpectrumCoreEmulationSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new ZXSpectrumCoreEmulationSettings();
			form.ShowDialog();
		}

		private void ZXSpectrumNonSyncSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new ZXSpectrumNonSyncSettings();
			form.ShowDialog();
		}

		private void ZXSpectrumAudioSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new ZXSpectrumAudioSettings();
			form.ShowDialog();
		}

		private void ZXSpectrumPokeMemoryMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new ZXSpectrumPokeMemory();
			form.ShowDialog();
		}

		private void ZXSpectrumMediaMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is ZXSpectrum)
			{
				ZXSpectrumTapesSubMenu.Enabled = ((ZXSpectrum)Emulator)._tapeInfo.Count > 0;
				ZXSpectrumDisksSubMenu.Enabled = ((ZXSpectrum)Emulator)._diskInfo.Count > 0;
			}
		}

		private void ZXSpectrumTapesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ZXSpectrumTapesSubMenu.DropDownItems.Clear();

			List<ToolStripMenuItem> items = new List<ToolStripMenuItem>();

			if (Emulator is ZXSpectrum speccy)
			{
				var tapeMediaIndex = speccy._machine.TapeMediaIndex;

				for (int i = 0; i < speccy._tapeInfo.Count; i++)
				{
					string name = speccy._tapeInfo[i].Name;

					var menuItem = new ToolStripMenuItem
					{
						Name = $"{i}_{name}",
						Text = $"{i}: {name}",
						Checked = tapeMediaIndex == i
					};

					int dummy = i;
					menuItem.Click += (o, ev) =>
					{
						speccy._machine.TapeMediaIndex = dummy;
					};

					items.Add(menuItem);
				}
			}

			ZXSpectrumTapesSubMenu.DropDownItems.AddRange(items.ToArray());
		}

		private void ZXSpectrumDisksSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ZXSpectrumDisksSubMenu.DropDownItems.Clear();

			var items = new List<ToolStripMenuItem>();

			if (Emulator is ZXSpectrum speccy)
			{
				var diskMediaIndex = speccy._machine.DiskMediaIndex;

				for (int i = 0; i < speccy._diskInfo.Count; i++)
				{
					string name = speccy._diskInfo[i].Name;

					var menuItem = new ToolStripMenuItem
					{
						Name = $"{i}_{name}",
						Text = $"{i}: {name}",
						Checked = diskMediaIndex == i
					};

					int dummy = i;
					menuItem.Click += (o, ev) =>
					{
						speccy._machine.DiskMediaIndex = dummy;
					};

					items.Add(menuItem);
				}
			}

			ZXSpectrumDisksSubMenu.DropDownItems.AddRange(items.ToArray());
		}

		private void ZXSpectrumExportSnapshotMenuItemMenuItem_Click(object sender, EventArgs e)
		{
			using var zxSnapExpDialog = new SaveFileDialog
			{
				RestoreDirectory = true
				, Title = "EXPERIMENTAL - Export 3rd party snapshot formats"
				, DefaultExt = "szx"
				, Filter = "ZX-State files (*.szx)|*.szx"
				, SupportMultiDottedExtensions = true
			};

			try
			{
				var res = zxSnapExpDialog.ShowDialog();
				if (res == DialogResult.OK)
				{
					var speccy = (ZXSpectrum)Emulator;
					var snap = speccy.GetSZXSnapshot();
					File.WriteAllBytes(zxSnapExpDialog.FileName, snap);
				}
			}
			catch (Exception ex)
			{
				var ee = ex;
			}
		}

		#endregion

		#region AmstradCPC

		private void amstradCPCCoreEmulationSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new AmstradCPCCoreEmulationSettings();
			form.ShowDialog();
		}

		private void AmstradCPCAudioSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new AmstradCPCAudioSettings();
			form.ShowDialog();
		}

		private void AmstradCPCPokeMemoryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new AmstradCPCPokeMemory();
			form.ShowDialog();
		}

		private void AmstradCPCMediaToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is AmstradCPC)
			{
				AmstradCPCTapesSubMenu.Enabled = ((AmstradCPC)Emulator)._tapeInfo.Count > 0;
				AmstradCPCDisksSubMenu.Enabled = ((AmstradCPC)Emulator)._diskInfo.Count > 0;
			}
		}

		private void AmstradCPCTapesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AmstradCPCTapesSubMenu.DropDownItems.Clear();

			var items = new List<ToolStripMenuItem>();

			if (Emulator is AmstradCPC ams)
			{
				var tapeMediaIndex = ams._machine.TapeMediaIndex;

				for (int i = 0; i < ams._tapeInfo.Count; i++)
				{
					string name = ams._tapeInfo[i].Name;

					var menuItem = new ToolStripMenuItem
					{
						Name = $"{i}_{name}",
						Text = $"{i}: {name}",
						Checked = tapeMediaIndex == i
					};

					int dummy = i;
					menuItem.Click += (o, ev) =>
					{
						ams._machine.TapeMediaIndex = dummy;
					};

					items.Add(menuItem);
				}
			}

			AmstradCPCTapesSubMenu.DropDownItems.AddRange(items.ToArray());
		}

		private void AmstradCPCDisksSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AmstradCPCDisksSubMenu.DropDownItems.Clear();

			var items = new List<ToolStripMenuItem>();

			if (Emulator is AmstradCPC ams)
			{
				var diskMediaIndex = ams._machine.DiskMediaIndex;

				for (int i = 0; i < ams._diskInfo.Count; i++)
				{
					string name = ams._diskInfo[i].Name;

					var menuItem = new ToolStripMenuItem
					{
						Name = $"{i}_{name}",
						Text = $"{i}: {name}",
						Checked = diskMediaIndex == i
					};

					int dummy = i;
					menuItem.Click += (o, ev) =>
					{
						ams._machine.DiskMediaIndex = dummy;
					};

					items.Add(menuItem);
				}
			}

			AmstradCPCDisksSubMenu.DropDownItems.AddRange(items.ToArray());
		}

		private void AmstradCPCNonSyncSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new AmstradCPCNonSyncSettings();
			form.ShowDialog();
		}

		#endregion

		#region Help

		private void HelpSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			FeaturesMenuItem.Visible = VersionInfo.DeveloperBuild;
		}

		private void OnlineHelpMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/BizHawk.html");
		}

		private void ForumsMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/forum/viewforum.php?f=64");
		}

		private void FeaturesMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<CoreFeatureAnalysis>();
		}

		private void AboutMenuItem_Click(object sender, EventArgs e)
		{
			if (VersionInfo.DeveloperBuild)
			{
				using var form = new AboutBox();
				form.ShowDialog();
			}
			else
			{
				using var form = new BizBox();
				form.ShowDialog();
			}
		}

		#endregion

		#region Context Menu

		private void MainFormContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			_wasPaused = EmulatorPaused;
			_didMenuPause = true;
			PauseEmulator();

			OpenRomContextMenuItem.Visible = Emulator.IsNull() || _inFullscreen;

			bool showMenuVisible = _inFullscreen;
			if (!MainMenuStrip.Visible)
			{
				showMenuVisible = true; // need to always be able to restore this as an emergency measure
			}

			if (_argParser._chromeless)
			{
				showMenuVisible = true; // I decided this was always possible in chrome-less mode, we'll see what they think
			}

			ShowMenuContextMenuItem.Visible =
				ShowMenuContextMenuSeparator.Visible =
				showMenuVisible;

			LoadLastRomContextMenuItem.Visible = Emulator.IsNull();

			StopAVContextMenuItem.Visible = _currAviWriter != null;

			ContextSeparator_AfterMovie.Visible =
				ContextSeparator_AfterUndo.Visible =
				ScreenshotContextMenuItem.Visible =
				CloseRomContextMenuItem.Visible =
				UndoSavestateContextMenuItem.Visible =
				!Emulator.IsNull();

			RecordMovieContextMenuItem.Visible =
				PlayMovieContextMenuItem.Visible =
				LoadLastMovieContextMenuItem.Visible =
				!Emulator.IsNull() && !Global.MovieSession.Movie.IsActive;

			RestartMovieContextMenuItem.Visible =
				StopMovieContextMenuItem.Visible =
				ViewSubtitlesContextMenuItem.Visible =
				ViewCommentsContextMenuItem.Visible =
				SaveMovieContextMenuItem.Visible =
				SaveMovieAsContextMenuItem.Visible =
				Global.MovieSession.Movie.IsActive;

			BackupMovieContextMenuItem.Visible = Global.MovieSession.Movie.IsActive;

			StopNoSaveContextMenuItem.Visible = Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie.Changes;

			AddSubtitleContextMenuItem.Visible = !Emulator.IsNull() && Global.MovieSession.Movie.IsActive && !Global.MovieSession.ReadOnly;

			ConfigContextMenuItem.Visible = _inFullscreen;

			ClearSRAMContextMenuItem.Visible = File.Exists(PathManager.SaveRamPath(Global.Game));

			ContextSeparator_AfterROM.Visible = OpenRomContextMenuItem.Visible || LoadLastRomContextMenuItem.Visible;

			LoadLastRomContextMenuItem.Enabled = !Global.Config.RecentRoms.Empty;
			LoadLastMovieContextMenuItem.Enabled = !Global.Config.RecentMovies.Empty;

			if (Global.MovieSession.Movie.IsActive)
			{
				if (Global.MovieSession.ReadOnly)
				{
					ViewSubtitlesContextMenuItem.Text = "View Subtitles";
					ViewCommentsContextMenuItem.Text = "View Comments";
				}
				else
				{
					ViewSubtitlesContextMenuItem.Text = "Edit Subtitles";
					ViewCommentsContextMenuItem.Text = "Edit Comments";
				}
			}

			var file = new FileInfo($"{PathManager.SaveStatePrefix(Global.Game)}.QuickSave{Global.Config.SaveSlot}.State.bak");

			if (file.Exists)
			{
				UndoSavestateContextMenuItem.Enabled = true;
				if (_stateSlots.IsRedo(Global.Config.SaveSlot))
				{
					UndoSavestateContextMenuItem.Text = $"Redo Save to slot {Global.Config.SaveSlot}";
					UndoSavestateContextMenuItem.Image = Properties.Resources.redo;
				}
				else
				{
					UndoSavestateContextMenuItem.Text = $"Undo Save to slot {Global.Config.SaveSlot}";
					UndoSavestateContextMenuItem.Image = Properties.Resources.undo;
				}
			}
			else
			{
				UndoSavestateContextMenuItem.Enabled = false;
				UndoSavestateContextMenuItem.Text = "Undo Savestate";
				UndoSavestateContextMenuItem.Image = Properties.Resources.undo;
			}

			ShowMenuContextMenuItem.Text = MainMenuStrip.Visible ? "Hide Menu" : "Show Menu";
		}

		private void MainFormContextMenu_Closing(object sender, ToolStripDropDownClosingEventArgs e)
		{
			if (!_wasPaused)
			{
				UnpauseEmulator();
			}
		}

		private void SavestateTypeContextSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SavestateTypeDefaultContextMenuItem.Checked = false;
			SavestateBinaryContextMenuItem.Checked = false;
			SavestateTextContextMenuItem.Checked = false;
			switch (Global.Config.SaveStateType)
			{
				case Config.SaveStateTypeE.Binary:
					SavestateBinaryContextMenuItem.Checked = true;
					break;
				case Config.SaveStateTypeE.Text:
					SavestateTextContextMenuItem.Checked = true;
					break;
				case Config.SaveStateTypeE.Default:
					SavestateTypeDefaultContextMenuItem.Checked = true;
					break;
			}
		}

		private void DisplayConfigMenuItem_Click(object sender, EventArgs e)
		{
			using var window = new DisplayConfigLite();
			var result = window.ShowDialog();
			if (result == DialogResult.OK)
			{
				FrameBufferResized();
				SynchChrome();
				if (window.NeedReset)
				{
					GlobalWin.OSD.AddMessage("Restart program for changed settings");
				}
			}
		}

		private void LoadLastRomContextMenuItem_Click(object sender, EventArgs e)
		{
			LoadRomFromRecent(Global.Config.RecentRoms.MostRecent);
		}

		private void LoadLastMovieContextMenuItem_Click(object sender, EventArgs e)
		{
			LoadMoviesFromRecent(Global.Config.RecentMovies.MostRecent);
		}

		private void BackupMovieContextMenuItem_Click(object sender, EventArgs e)
		{
			Global.MovieSession.Movie.SaveBackup();
			GlobalWin.OSD.AddMessage("Backup movie saved.");
		}

		private void ViewSubtitlesContextMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				using var form = new EditSubtitlesForm { ReadOnly = Global.MovieSession.ReadOnly };
				form.GetMovie(Global.MovieSession.Movie);
				form.ShowDialog();
			}
		}

		private void AddSubtitleContextMenuItem_Click(object sender, EventArgs e)
		{
			// TODO: rethink this?
			var subForm = new SubtitleMaker();
			subForm.DisableFrame();

			int index = -1;
			var sub = new Subtitle();
			for (int i = 0; i < Global.MovieSession.Movie.Subtitles.Count; i++)
			{
				sub = Global.MovieSession.Movie.Subtitles[i];
				if (Emulator.Frame == sub.Frame)
				{
					index = i;
					break;
				}
			}

			if (index < 0)
			{
				sub = new Subtitle { Frame = Emulator.Frame };
			}

			subForm.Sub = sub;

			if (subForm.ShowDialog() == DialogResult.OK)
			{
				if (index >= 0)
				{
					Global.MovieSession.Movie.Subtitles.RemoveAt(index);
				}

				Global.MovieSession.Movie.Subtitles.Add(subForm.Sub);
			}
		}

		private void ViewCommentsContextMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				using var form = new EditCommentsForm();
				form.GetMovie(Global.MovieSession.Movie);
				form.ShowDialog();
			}
		}

		private void UndoSavestateContextMenuItem_Click(object sender, EventArgs e)
		{
			_stateSlots.SwapBackupSavestate($"{PathManager.SaveStatePrefix(Global.Game)}.QuickSave{Global.Config.SaveSlot}.State");

			GlobalWin.OSD.AddMessage($"Save slot {Global.Config.SaveSlot} restored.");
		}

		private void ClearSramContextMenuItem_Click(object sender, EventArgs e)
		{
			CloseRom(clearSram: true);
		}

		private void ShowMenuContextMenuItem_Click(object sender, EventArgs e)
		{
			MainMenuStrip.Visible ^= true;
			FrameBufferResized();
		}

		#endregion

		#region Status Bar

		private void DumpStatusButton_Click(object sender, EventArgs e)
		{
			string details = Emulator.CoreComm.RomStatusDetails;
			if (!string.IsNullOrEmpty(details))
			{
				GlobalWin.Sound.StopSound();
				LogWindow.ShowReport("Dump Status Report", details, this);
				GlobalWin.Sound.StartSound();
			}
		}

		private void SlotStatusButtons_MouseUp(object sender, MouseEventArgs e)
		{
			int slot = 0;
			if (sender == Slot1StatusButton) slot = 1;
			if (sender == Slot2StatusButton) slot = 2;
			if (sender == Slot3StatusButton) slot = 3;
			if (sender == Slot4StatusButton) slot = 4;
			if (sender == Slot5StatusButton) slot = 5;
			if (sender == Slot6StatusButton) slot = 6;
			if (sender == Slot7StatusButton) slot = 7;
			if (sender == Slot8StatusButton) slot = 8;
			if (sender == Slot9StatusButton) slot = 9;
			if (sender == Slot0StatusButton) slot = 0;

			if (e.Button == MouseButtons.Left)
			{
				if (_stateSlots.HasSlot(slot))
				{
					LoadQuickSave($"QuickSave{slot}");
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				SaveQuickSave($"QuickSave{slot}");
			}
		}

		private void KeyPriorityStatusLabel_Click(object sender, EventArgs e)
		{
			switch (Global.Config.Input_Hotkey_OverrideOptions)
			{
				default:
				case 0:
					Global.Config.Input_Hotkey_OverrideOptions = 1;
					break;
				case 1:
					Global.Config.Input_Hotkey_OverrideOptions = 2;
					break;
				case 2:
					Global.Config.Input_Hotkey_OverrideOptions = 0;
					break;
			}

			UpdateKeyPriorityIcon();
		}

		private void FreezeStatus_Click(object sender, EventArgs e)
		{
			if (CheatStatusButton.Visible)
			{
				GlobalWin.Tools.Load<Cheats>();
			}
		}

		private void ProfileFirstBootLabel_Click(object sender, EventArgs e)
		{
			// We do not check if the user is actually setting a profile here.
			// This is intentional.
			using var profileForm = new ProfileConfig();
			profileForm.ShowDialog();
			Global.Config.FirstBoot = false;
			ProfileFirstBootLabel.Visible = false;
		}

		private void LinkConnectStatusBarButton_Click(object sender, EventArgs e)
		{
			// toggle Link status (only outside of a movie session)
			if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
			{
				Emulator.AsLinkable().LinkConnected ^= true;
				Console.WriteLine("Cable connect status to {0}", Emulator.AsLinkable().LinkConnected);
			}
		}

		private void UpdateNotification_Click(object sender, EventArgs e)
		{
			GlobalWin.Sound.StopSound();
			DialogResult result = MessageBox.Show(this,
				$"Version {Global.Config.Update_LatestVersion} is now available. Would you like to open the BizHawk homepage?\r\n\r\nClick \"No\" to hide the update notification for this version.",
				"New Version Available", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			GlobalWin.Sound.StartSound();

			if (result == DialogResult.Yes)
			{
				System.Threading.ThreadPool.QueueUserWorkItem(s =>
				{
					using (System.Diagnostics.Process.Start(VersionInfo.HomePage))
					{
					}
				});
			}
			else if (result == DialogResult.No)
			{
				UpdateChecker.IgnoreNewVersion();
				UpdateChecker.BeginCheck(skipCheck: true); // Trigger event to hide new version notification
			}
		}

		#endregion

		#region Form Events

		private void MainForm_Activated(object sender, EventArgs e)
		{
			if (!Global.Config.RunInBackground)
			{
				if (!_wasPaused)
				{
					UnpauseEmulator();
				}

				_wasPaused = false;
			}
		}

		private void MainForm_Deactivate(object sender, EventArgs e)
		{
			if (!Global.Config.RunInBackground)
			{
				if (EmulatorPaused)
				{
					_wasPaused = true;
				}

				PauseEmulator();
			}
		}

		private void TimerMouseIdle_Tick(object sender, EventArgs e)
		{
			if (_inFullscreen && Global.Config.DispChrome_Fullscreen_AutohideMouse)
			{
				AutohideCursor(true);
			}
		}

		private void MainForm_Enter(object sender, EventArgs e)
		{
			AutohideCursor(false);
		}

		public void MainForm_MouseWheel(object sender, MouseEventArgs e)
		{
			MouseWheelTracker += e.Delta;
		}

		public void MainForm_MouseMove(object sender, MouseEventArgs e)
		{
			AutohideCursor(false);
		}

		public void MainForm_MouseClick(object sender, MouseEventArgs e)
		{
			AutohideCursor(false);
			if (Global.Config.ShowContextMenu && e.Button == MouseButtons.Right)
			{
				MainFormContextMenu.Show(
					PointToScreen(new Point(e.X, e.Y + MainformMenu.Height)));
			}
		}

		private void MainForm_Resize(object sender, EventArgs e)
		{
			PresentationPanel.Resized = true;
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			if (Emulator is TI83 && Global.Config.TI83autoloadKeyPad)
			{
				GlobalWin.Tools.Load<TI83KeyPad>();
			}

			GlobalWin.Tools.AutoLoad();

			if (Global.Config.RecentWatches.AutoLoad)
			{
				GlobalWin.Tools.LoadRamWatch(!Global.Config.DisplayRamWatch);
			}

			if (Global.Config.RecentCheats.AutoLoad)
			{
				GlobalWin.Tools.Load<Cheats>();
			}

			HandlePlatformMenus();
		}

		protected override void OnClosed(EventArgs e)
		{
			_windowClosedAndSafeToExitProcess = true;
			base.OnClosed(e);
		}

		private void MainformMenu_Leave(object sender, EventArgs e)
		{
		}

		private void MainformMenu_MenuActivate(object sender, EventArgs e)
		{
			HandlePlatformMenus();
			if (Global.Config.PauseWhenMenuActivated)
			{
				_wasPaused = EmulatorPaused;
				_didMenuPause = true;
				PauseEmulator();
			}
		}

		private void MainformMenu_MenuDeactivate(object sender, EventArgs e)
		{
			if (!_wasPaused)
			{
				UnpauseEmulator();
			}
		}

		private static void FormDragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void FormDragDrop(object sender, DragEventArgs e)
		{
			try
			{
				FormDragDrop_internal(e);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Exception on drag and drop:\n{ex}");
			}
		}

		#endregion
	}
}
