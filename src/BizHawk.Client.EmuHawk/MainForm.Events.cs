using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Atari.A7800Hawk;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;
using BizHawk.Emulation.Cores.Computers.AppleII;
using BizHawk.Emulation.Cores.Computers.Commodore64;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Intellivision;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.SubNESHawk;
using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm
	{
		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveStateSubMenu.Enabled =
				LoadStateSubMenu.Enabled =
				SaveSlotSubMenu.Enabled =
				Emulator.HasSavestates();

			OpenRomMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Open ROM"].Bindings;
			CloseRomMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Close ROM"].Bindings;

			MovieSubMenu.Enabled =
				CloseRomMenuItem.Enabled =
				!Emulator.IsNull();

			var hasSaveRam = Emulator.HasSaveRam();
			bool needBold = hasSaveRam && Emulator.AsSaveRam().SaveRamModified;

			SaveRAMSubMenu.Enabled = hasSaveRam;
			SaveRAMSubMenu.SetStyle(needBold ? FontStyle.Bold : FontStyle.Regular);

			AVSubMenu.Enabled =
			ScreenshotSubMenu.Enabled =
				Emulator.HasVideoProvider();
		}

		private void RecentRomMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			RecentRomSubMenu.DropDownItems.Clear();
			RecentRomSubMenu.DropDownItems.AddRange(Config.RecentRoms.RecentMenu(LoadRomFromRecent, "ROM", romLoading: true));
		}

		private bool HasSlot(int slot) => _stateSlots.HasSlot(Emulator, MovieSession.Movie, slot, SaveStatePrefix());

		private void SaveStateSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			void SetSlotFont(ToolStripMenuItemEx menu, int slot) => menu.SetStyle(
				HasSlot(slot) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular);

			SetSlotFont(SaveState0MenuItem, 0);
			SetSlotFont(SaveState1MenuItem, 1);
			SetSlotFont(SaveState2MenuItem, 2);
			SetSlotFont(SaveState3MenuItem, 3);
			SetSlotFont(SaveState4MenuItem, 4);
			SetSlotFont(SaveState5MenuItem, 5);
			SetSlotFont(SaveState6MenuItem, 6);
			SetSlotFont(SaveState7MenuItem, 7);
			SetSlotFont(SaveState8MenuItem, 8);
			SetSlotFont(SaveState9MenuItem, 9);

			SaveState1MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 1"].Bindings;
			SaveState2MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 2"].Bindings;
			SaveState3MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 3"].Bindings;
			SaveState4MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 4"].Bindings;
			SaveState5MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 5"].Bindings;
			SaveState6MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 6"].Bindings;
			SaveState7MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 7"].Bindings;
			SaveState8MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 8"].Bindings;
			SaveState9MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 9"].Bindings;
			SaveState0MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 0"].Bindings;
			SaveNamedStateMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save Named State"].Bindings;
		}

		private void LoadStateSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			LoadState1MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 1"].Bindings;
			LoadState2MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 2"].Bindings;
			LoadState3MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 3"].Bindings;
			LoadState4MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 4"].Bindings;
			LoadState5MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 5"].Bindings;
			LoadState6MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 6"].Bindings;
			LoadState7MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 7"].Bindings;
			LoadState8MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 8"].Bindings;
			LoadState9MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 9"].Bindings;
			LoadState0MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 0"].Bindings;
			LoadNamedStateMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load Named State"].Bindings;

			AutoloadLastSlotMenuItem.Checked = Config.AutoLoadLastSaveSlot;

			LoadState1MenuItem.Enabled = HasSlot(1);
			LoadState2MenuItem.Enabled = HasSlot(2);
			LoadState3MenuItem.Enabled = HasSlot(3);
			LoadState4MenuItem.Enabled = HasSlot(4);
			LoadState5MenuItem.Enabled = HasSlot(5);
			LoadState6MenuItem.Enabled = HasSlot(6);
			LoadState7MenuItem.Enabled = HasSlot(7);
			LoadState8MenuItem.Enabled = HasSlot(8);
			LoadState9MenuItem.Enabled = HasSlot(9);
			LoadState0MenuItem.Enabled = HasSlot(0);
		}

		private void SaveSlotSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SelectSlot0MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 0"].Bindings;
			SelectSlot1MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 1"].Bindings;
			SelectSlot2MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 2"].Bindings;
			SelectSlot3MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 3"].Bindings;
			SelectSlot4MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 4"].Bindings;
			SelectSlot5MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 5"].Bindings;
			SelectSlot6MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 6"].Bindings;
			SelectSlot7MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 7"].Bindings;
			SelectSlot8MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 8"].Bindings;
			SelectSlot9MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 9"].Bindings;
			PreviousSlotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Previous Slot"].Bindings;
			NextSlotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Next Slot"].Bindings;
			SaveToCurrentSlotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Quick Save"].Bindings;
			LoadCurrentSlotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Quick Load"].Bindings;

			SelectSlot0MenuItem.Checked = Config.SaveSlot == 0;
			SelectSlot1MenuItem.Checked = Config.SaveSlot == 1;
			SelectSlot2MenuItem.Checked = Config.SaveSlot == 2;
			SelectSlot3MenuItem.Checked = Config.SaveSlot == 3;
			SelectSlot4MenuItem.Checked = Config.SaveSlot == 4;
			SelectSlot5MenuItem.Checked = Config.SaveSlot == 5;
			SelectSlot6MenuItem.Checked = Config.SaveSlot == 6;
			SelectSlot7MenuItem.Checked = Config.SaveSlot == 7;
			SelectSlot8MenuItem.Checked = Config.SaveSlot == 8;
			SelectSlot9MenuItem.Checked = Config.SaveSlot == 9;
		}

		private void SaveRamSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			FlushSaveRAMMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Flush SaveRAM"].Bindings;
		}

		private void MovieSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			StopMovieWithoutSavingMenuItem.Enabled = MovieSession.Movie.IsActive() && MovieSession.Movie.Changes;
			StopMovieMenuItem.Enabled
				= SaveMovieMenuItem.Enabled
				= SaveMovieAsMenuItem.Enabled
				= MovieSession.Movie.IsActive();

			ReadonlyMenuItem.Checked = MovieSession.ReadOnly;
			AutomaticallyBackupMoviesMenuItem.Checked = Config.Movies.EnableBackupMovies;
			FullMovieLoadstatesMenuItem.Checked = Config.Movies.VBAStyleMovieLoadState;

			ReadonlyMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Toggle read-only"].Bindings;
			RecordMovieMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Record Movie"].Bindings;
			PlayMovieMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Play Movie"].Bindings;
			StopMovieMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Stop Movie"].Bindings;
			PlayFromBeginningMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Play from beginning"].Bindings;
			SaveMovieMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save Movie"].Bindings;

			PlayMovieMenuItem.Enabled
				= ImportMoviesMenuItem.Enabled 
				= RecentMovieSubMenu.Enabled
					= !Tools.IsLoaded<TAStudio>();

			// Record movie dialog should not be opened while in need of a reboot,
			// Otherwise the wrong sync settings could be set for the recording movie and cause crashes
			RecordMovieMenuItem.Enabled = !Tools.IsLoaded<TAStudio>()
				&& RebootStatusBarIcon.Visible == false;

			PlayFromBeginningMenuItem.Enabled = MovieSession.Movie.IsActive() && !Tools.IsLoaded<TAStudio>();
		}

		private void RecentMovieSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentMovieSubMenu.DropDownItems.Clear();
			RecentMovieSubMenu.DropDownItems.AddRange(Config.RecentMovies.RecentMenu(LoadMoviesFromRecent, "Movie"));
		}

		private void MovieEndSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			MovieEndFinishMenuItem.Checked = Config.Movies.MovieEndAction == MovieEndAction.Finish;
			MovieEndRecordMenuItem.Checked = Config.Movies.MovieEndAction == MovieEndAction.Record;
			MovieEndStopMenuItem.Checked = Config.Movies.MovieEndAction == MovieEndAction.Stop;
			MovieEndPauseMenuItem.Checked = Config.Movies.MovieEndAction == MovieEndAction.Pause;

			// Arguably an IControlMainForm property should be set here, but in reality only Tastudio is ever going to interfere with this logic
			MovieEndFinishMenuItem.Enabled =
			MovieEndRecordMenuItem.Enabled =
			MovieEndStopMenuItem.Enabled =
			MovieEndPauseMenuItem.Enabled =
				!Tools.Has<TAStudio>();
		}

		private void AVSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ConfigAndRecordAVMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Record A/V"].Bindings;
			StopAVIMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Stop A/V"].Bindings;
			CaptureOSDMenuItem.Checked = Config.AviCaptureOsd;

			RecordAVMenuItem.Enabled = !string.IsNullOrEmpty(Config.VideoWriter) && _currAviWriter == null;

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
			ScreenshotCaptureOSDMenuItem1.Checked = Config.ScreenshotCaptureOsd;
			ScreenshotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Screenshot"].Bindings;
			ScreenshotClipboardMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["ScreenshotToClipboard"].Bindings;
			ScreenshotClientClipboardMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Screen Client to Clipboard"].Bindings;
		}

		private void OpenRomMenuItem_Click(object sender, EventArgs e)
		{
			OpenRom();
		}

		private void OpenAdvancedMenuItem_Click(object sender, EventArgs e)
		{
			using var oac = new OpenAdvancedChooser(Config, CreateCoreComm, Game, RunLibretroCoreChooser);
			if (oac.ShowHawkDialog() == DialogResult.Cancel)
			{
				return;
			}

			if (oac.Result == AdvancedRomLoaderType.LibretroLaunchNoGame)
			{
				var argsNoGame = new LoadRomArgs
				{
					OpenAdvanced = new OpenAdvanced_LibretroNoGame(Config.LibretroCore)
				};
				LoadRom("", argsNoGame);
				return;
			}

			var args = new LoadRomArgs();

			var filter = RomLoader.RomFilter;

			if (oac.Result == AdvancedRomLoaderType.LibretroLaunchGame)
			{
				args.OpenAdvanced = new OpenAdvanced_Libretro();
				filter = oac.SuggestedExtensionFilter;
			}
			else if (oac.Result == AdvancedRomLoaderType.ClassicLaunchGame)
			{
				args.OpenAdvanced = new OpenAdvanced_OpenRom();
			}
			else if (oac.Result == AdvancedRomLoaderType.MameLaunchGame)
			{
				args.OpenAdvanced = new OpenAdvanced_MAME();
				filter = new FilesystemFilter("MAME Arcade ROMs", new[] { "zip" }).ToString();
			}
			else
			{
				throw new InvalidOperationException("Automatic Alpha Sanitizer");
			}

			/*************************/
			/* CLONE OF CODE FROM OpenRom (mostly) */
			using var ofd = new OpenFileDialog
			{
				InitialDirectory = Config.PathEntries.RomAbsolutePath(Emulator.SystemId),
				Filter = filter,
				RestoreDirectory = false,
				FilterIndex = _lastOpenRomFilter,
				Title = "Open Advanced"
			};

			var result = ofd.ShowHawkDialog();
			if (!result.IsOk())
			{
				return;
			}

			var file = new FileInfo(ofd.FileName);
			Config.PathEntries.LastRomPath = file.DirectoryName;
			_lastOpenRomFilter = ofd.FilterIndex;
			/*************************/

			LoadRom(file.FullName, args);
		}

		private void CloseRomMenuItem_Click(object sender, EventArgs e)
		{
			Console.WriteLine($"Closing rom clicked Frame: {Emulator.Frame} Emulator: {Emulator.GetType().Name}");
			CloseRom();
			Console.WriteLine($"Closing rom clicked DONE Frame: {Emulator.Frame} Emulator: {Emulator.GetType().Name}");
		}

		private void Savestate1MenuItem_Click(object sender, EventArgs e) => SaveQuickSave("QuickSave1");
		private void Savestate2MenuItem_Click(object sender, EventArgs e) => SaveQuickSave("QuickSave2");
		private void Savestate3MenuItem_Click(object sender, EventArgs e) => SaveQuickSave("QuickSave3");
		private void Savestate4MenuItem_Click(object sender, EventArgs e) => SaveQuickSave("QuickSave4");
		private void Savestate5MenuItem_Click(object sender, EventArgs e) => SaveQuickSave("QuickSave5");
		private void Savestate6MenuItem_Click(object sender, EventArgs e) => SaveQuickSave("QuickSave6");
		private void Savestate7MenuItem_Click(object sender, EventArgs e) => SaveQuickSave("QuickSave7");
		private void Savestate8MenuItem_Click(object sender, EventArgs e) => SaveQuickSave("QuickSave8");
		private void Savestate9MenuItem_Click(object sender, EventArgs e) => SaveQuickSave("QuickSave9");
		private void Savestate0MenuItem_Click(object sender, EventArgs e) => SaveQuickSave("QuickSave0");

		private void SaveNamedStateMenuItem_Click(object sender, EventArgs e) => SaveStateAs();

		private void Loadstate1MenuItem_Click(object sender, EventArgs e) => LoadQuickSave("QuickSave1");
		private void Loadstate2MenuItem_Click(object sender, EventArgs e) => LoadQuickSave("QuickSave2");
		private void Loadstate3MenuItem_Click(object sender, EventArgs e) => LoadQuickSave("QuickSave3");
		private void Loadstate4MenuItem_Click(object sender, EventArgs e) => LoadQuickSave("QuickSave4");
		private void Loadstate5MenuItem_Click(object sender, EventArgs e) => LoadQuickSave("QuickSave5");
		private void Loadstate6MenuItem_Click(object sender, EventArgs e) => LoadQuickSave("QuickSave6");
		private void Loadstate7MenuItem_Click(object sender, EventArgs e) => LoadQuickSave("QuickSave7");
		private void Loadstate8MenuItem_Click(object sender, EventArgs e) => LoadQuickSave("QuickSave8");
		private void Loadstate9MenuItem_Click(object sender, EventArgs e) => LoadQuickSave("QuickSave9");
		private void Loadstate0MenuItem_Click(object sender, EventArgs e) => LoadQuickSave("QuickSave0");

		private void LoadNamedStateMenuItem_Click(object sender, EventArgs e) => LoadStateAs();

		private void AutoloadLastSlotMenuItem_Click(object sender, EventArgs e)
		{
			Config.AutoLoadLastSaveSlot ^= true;
		}

		private void SelectSlotMenuItems_Click(object sender, EventArgs e)
		{
			if (sender == SelectSlot0MenuItem) Config.SaveSlot = 0;
			else if (sender == SelectSlot1MenuItem) Config.SaveSlot = 1;
			else if (sender == SelectSlot2MenuItem) Config.SaveSlot = 2;
			else if (sender == SelectSlot3MenuItem) Config.SaveSlot = 3;
			else if (sender == SelectSlot4MenuItem) Config.SaveSlot = 4;
			else if (sender == SelectSlot5MenuItem) Config.SaveSlot = 5;
			else if (sender == SelectSlot6MenuItem) Config.SaveSlot = 6;
			else if (sender == SelectSlot7MenuItem) Config.SaveSlot = 7;
			else if (sender == SelectSlot8MenuItem) Config.SaveSlot = 8;
			else if (sender == SelectSlot9MenuItem) Config.SaveSlot = 9;

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
			SaveQuickSave($"QuickSave{Config.SaveSlot}");
		}

		private void LoadCurrentSlotMenuItem_Click(object sender, EventArgs e)
		{
			LoadQuickSave($"QuickSave{Config.SaveSlot}");
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

			// Nag user to user a more accurate core, but let them continue anyway
			EnsureCoreIsAccurate();

			using var form = new RecordMovie(this, Config, Game, Emulator, MovieSession, FirmwareManager);
			form.ShowDialog();
		}

		private void PlayMovieMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new PlayMovie(this, Config, Game, Emulator, MovieSession);
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
				InitialDirectory = Config.PathEntries.RomAbsolutePath(Emulator.SystemId),
				Multiselect = true,
				Filter = MovieImport.AvailableImporters.ToString("Movie Files"),
				RestoreDirectory = false
			};

			if (ofd.ShowHawkDialog().IsOk())
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
			var filename = MovieSession.Movie.Filename;
			if (string.IsNullOrWhiteSpace(filename))
			{
				filename = Game.FilesystemSafeName();
			}

			var file = ToolFormBase.SaveFileDialog(
				filename,
				Config.PathEntries.MovieAbsolutePath(),
				"Movie Files",
				MovieSession.Movie.PreferredExtension);

			if (file != null)
			{
				MovieSession.Movie.Filename = file.FullName;
				Config.RecentMovies.Add(MovieSession.Movie.Filename);
				SaveMovie();
			}
		}

		private void StopMovieWithoutSavingMenuItem_Click(object sender, EventArgs e)
		{
			if (Config.Movies.EnableBackupMovies)
			{
				MovieSession.Movie.SaveBackup();
			}

			StopMovie(saveChanges: false);
		}

		private void AutomaticMovieBackupMenuItem_Click(object sender, EventArgs e)
		{
			Config.Movies.EnableBackupMovies ^= true;
		}

		private void FullMovieLoadstatesMenuItem_Click(object sender, EventArgs e)
		{
			Config.Movies.VBAStyleMovieLoadState ^= true;
		}

		private void MovieEndFinishMenuItem_Click(object sender, EventArgs e)
		{
			Config.Movies.MovieEndAction = MovieEndAction.Finish;
		}

		private void MovieEndRecordMenuItem_Click(object sender, EventArgs e)
		{
			Config.Movies.MovieEndAction = MovieEndAction.Record;
		}

		private void MovieEndStopMenuItem_Click(object sender, EventArgs e)
		{
			Config.Movies.MovieEndAction = MovieEndAction.Stop;
		}

		private void MovieEndPauseMenuItem_Click(object sender, EventArgs e)
		{
			Config.Movies.MovieEndAction = MovieEndAction.Pause;
		}

		private void ConfigAndRecordAVMenuItem_Click(object sender, EventArgs e)
		{
			if (OSTailoredCode.IsUnixHost) new MsgBox("Most of these options will cause crashes on Linux.", "A/V instability warning", MessageBoxIcon.Warning).ShowDialog();
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
			new SynclessRecordingTools(Game, Config.PathEntries.AvAbsolutePath()).Run();
		}

		private void CaptureOSDMenuItem_Click(object sender, EventArgs e)
		{
			Config.AviCaptureOsd ^= true;
		}

		private void ScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			TakeScreenshot();
		}

		private void ScreenshotAsMenuItem_Click(object sender, EventArgs e)
		{
			var path = $"{ScreenshotPrefix()}.{DateTime.Now:yyyy-MM-dd HH.mm.ss}.png";

			using var sfd = new SaveFileDialog
			{
				InitialDirectory = Path.GetDirectoryName(path),
				FileName = Path.GetFileName(path),
				Filter = FilesystemFilter.PNGs.ToString()
			};

			if (sfd.ShowHawkDialog().IsOk())
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
			Config.ScreenshotCaptureOsd ^= true;
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			if (Tools.AskSave())
			{
				Close();
			}
		}

		public void CloseEmulator(int? exitCode = null)
		{
			_exitRequestPending = true;
			if (exitCode != null) _exitCode = exitCode.Value;
		}

		private void EmulationMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			PauseMenuItem.Checked = _didMenuPause ? _wasPaused : EmulatorPaused;

			SoftResetMenuItem.Enabled = Emulator.ControllerDefinition.BoolButtons.Contains("Reset")
				&& !MovieSession.Movie.IsPlaying();

			HardResetMenuItem.Enabled = Emulator.ControllerDefinition.BoolButtons.Contains("Power")
				&& !MovieSession.Movie.IsPlaying();

			PauseMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Pause"].Bindings;
			RebootCoreMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Reboot Core"].Bindings;
			SoftResetMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Soft Reset"].Bindings;
			HardResetMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Hard Reset"].Bindings;
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

		private void ViewSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DisplayFPSMenuItem.Checked = Config.DisplayFps;
			DisplayFrameCounterMenuItem.Checked = Config.DisplayFrameCounter;
			DisplayLagCounterMenuItem.Checked = Config.DisplayLagCounter;
			DisplayInputMenuItem.Checked = Config.DisplayInput;
			DisplayRerecordCountMenuItem.Checked = Config.DisplayRerecordCount;
			DisplaySubtitlesMenuItem.Checked = Config.DisplaySubtitles;

			DisplayFPSMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Display FPS"].Bindings;
			DisplayFrameCounterMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Frame Counter"].Bindings;
			DisplayLagCounterMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Lag Counter"].Bindings;
			DisplayInputMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Input Display"].Bindings;
			SwitchToFullscreenMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Full Screen"].Bindings;

			DisplayStatusBarMenuItem.Checked = Config.DispChromeStatusBarWindowed;
			DisplayLogWindowMenuItem.Checked = Tools.IsLoaded<LogWindow>();

			DisplayLagCounterMenuItem.Enabled = Emulator.CanPollInput();

			DisplayMessagesMenuItem.Checked = Config.DisplayMessages;
		}

		private void WindowSizeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			x1MenuItem.Checked =
				x2MenuItem.Checked =
				x3MenuItem.Checked =
				x4MenuItem.Checked =
				x5MenuItem.Checked = false;

			switch (Config.TargetZoomFactors[Emulator.SystemId])
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
			if (sender == x1MenuItem) Config.TargetZoomFactors[Emulator.SystemId] = 1;
			if (sender == x2MenuItem) Config.TargetZoomFactors[Emulator.SystemId] = 2;
			if (sender == x3MenuItem) Config.TargetZoomFactors[Emulator.SystemId] = 3;
			if (sender == x4MenuItem) Config.TargetZoomFactors[Emulator.SystemId] = 4;
			if (sender == x5MenuItem) Config.TargetZoomFactors[Emulator.SystemId] = 5;
			if (sender == mzMenuItem) Config.TargetZoomFactors[Emulator.SystemId] = 10;

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
			Config.DisplayRerecordCount ^= true;
		}

		private void DisplaySubtitlesMenuItem_Click(object sender, EventArgs e)
		{
			Config.DisplaySubtitles ^= true;
		}

		private void DisplayStatusBarMenuItem_Click(object sender, EventArgs e)
		{
			Config.DispChromeStatusBarWindowed ^= true;
			SetStatusBar();
		}

		private void DisplayMessagesMenuItem_Click(object sender, EventArgs e)
		{
			Config.DisplayMessages ^= true;
		}

		private void DisplayLogWindowMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<LogWindow>();
		}

		private void ConfigSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ControllersMenuItem.Enabled = Emulator.ControllerDefinition.Any();
			RewindOptionsMenuItem.Enabled = Emulator.HasSavestates();
		}

		private void FrameSkipMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			MinimizeSkippingMenuItem.Checked = Config.AutoMinimizeSkipping;
			ClockThrottleMenuItem.Checked = Config.ClockThrottle;
			VsyncThrottleMenuItem.Checked = Config.VSyncThrottle;
			NeverSkipMenuItem.Checked = Config.FrameSkip == 0;
			Frameskip1MenuItem.Checked = Config.FrameSkip == 1;
			Frameskip2MenuItem.Checked = Config.FrameSkip == 2;
			Frameskip3MenuItem.Checked = Config.FrameSkip == 3;
			Frameskip4MenuItem.Checked = Config.FrameSkip == 4;
			Frameskip5MenuItem.Checked = Config.FrameSkip == 5;
			Frameskip6MenuItem.Checked = Config.FrameSkip == 6;
			Frameskip7MenuItem.Checked = Config.FrameSkip == 7;
			Frameskip8MenuItem.Checked = Config.FrameSkip == 8;
			Frameskip9MenuItem.Checked = Config.FrameSkip == 9;
			MinimizeSkippingMenuItem.Enabled = !NeverSkipMenuItem.Checked;
			if (!MinimizeSkippingMenuItem.Enabled)
			{
				MinimizeSkippingMenuItem.Checked = true;
			}

			AudioThrottleMenuItem.Enabled = Config.SoundEnabled;
			AudioThrottleMenuItem.Checked = Config.SoundThrottle;
			VsyncEnabledMenuItem.Checked = Config.VSync;

			Speed100MenuItem.Checked = Config.SpeedPercent == 100;
			Speed100MenuItem.Image = (Config.SpeedPercentAlternate == 100) ? Properties.Resources.FastForward : null;
			Speed150MenuItem.Checked = Config.SpeedPercent == 150;
			Speed150MenuItem.Image = (Config.SpeedPercentAlternate == 150) ? Properties.Resources.FastForward : null;
			Speed400MenuItem.Checked = Config.SpeedPercent == 400;
			Speed400MenuItem.Image = (Config.SpeedPercentAlternate == 400) ? Properties.Resources.FastForward : null;
			Speed200MenuItem.Checked = Config.SpeedPercent == 200;
			Speed200MenuItem.Image = (Config.SpeedPercentAlternate == 200) ? Properties.Resources.FastForward : null;
			Speed75MenuItem.Checked = Config.SpeedPercent == 75;
			Speed75MenuItem.Image = (Config.SpeedPercentAlternate == 75) ? Properties.Resources.FastForward : null;
			Speed50MenuItem.Checked = Config.SpeedPercent == 50;
			Speed50MenuItem.Image = (Config.SpeedPercentAlternate == 50) ? Properties.Resources.FastForward : null;

			Speed50MenuItem.Enabled =
				Speed75MenuItem.Enabled =
				Speed100MenuItem.Enabled =
				Speed150MenuItem.Enabled =
				Speed200MenuItem.Enabled =
				Speed400MenuItem.Enabled =
				Config.ClockThrottle;

			miUnthrottled.Checked = _unthrottled;
		}

		private void KeyPriorityMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			BothHkAndControllerMenuItem.Checked = false;
			InputOverHkMenuItem.Checked = false;
			HkOverInputMenuItem.Checked = false;

			switch (Config.InputHotkeyOverrideOptions)
			{
				default:
				case 0:
					BothHkAndControllerMenuItem.Checked = true;
					break;
				case 1:
					InputOverHkMenuItem.Checked = true;
					break;
				case 2:
					HkOverInputMenuItem.Checked = true;
					break;
			}
		}

		private void ControllersMenuItem_Click(object sender, EventArgs e)
		{
			using var controller = new ControllerConfig(Emulator, Config);
			if (controller.ShowDialog().IsOk())
			{
				AddOnScreenMessage("Controller settings saved");
				InitControls();
				InputManager.SyncControls(Emulator, MovieSession, Config);
			}
			else
			{
				AddOnScreenMessage("Controller config aborted");
			}
		}

		private void HotkeysMenuItem_Click(object sender, EventArgs e)
		{
			using var hotkeyConfig = new HotkeyConfig(Config);
			if (hotkeyConfig.ShowDialog().IsOk())
			{
				AddOnScreenMessage("Hotkey settings saved");
				InitControls();
				InputManager.SyncControls(Emulator, MovieSession, Config);
			}
			else
			{
				AddOnScreenMessage("Hotkey config aborted");
			}
		}

		private void FirmwaresMenuItem_Click(object sender, EventArgs e)
		{
			if (e is RomLoader.RomErrorArgs args)
			{
				using var configForm = new FirmwaresConfig(this, Config, true, args.RomPath);
				var result = configForm.ShowDialog();
				args.Retry = result == DialogResult.Retry;
			}
			else
			{
				using var configForm = new FirmwaresConfig(this, Config);
				configForm.ShowDialog();
			}
		}

		private void MessagesMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new MessageConfig(Config);
			var result = form.ShowDialog();
			AddOnScreenMessage(result.IsOk()
				? "Message settings saved"
				: "Message config aborted");
		}

		private void PathsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new PathConfig(this, Config, Game.System);
			form.ShowDialog();
		}

		private void SoundMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new SoundConfig(Config) { Owner = this };
			if (form.ShowDialog().IsOk())
			{
				Sound.StartSound();
				AddOnScreenMessage("Sound settings saved");
				RewireSound();
			}
			else
			{
				AddOnScreenMessage("Sound config aborted");
			}
		}

		private void AutofireMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new AutofireConfig(Config, InputManager.AutoFireController, InputManager.AutofireStickyXorAdapter);
			var result = form.ShowDialog();
			AddOnScreenMessage(result.IsOk()
				? "Autofire settings saved"
				: "Autofire config aborted");
		}

		private void RewindOptionsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator.HasSavestates())
			{
				using var form = new RewindConfig(Config, CreateRewinder, () => this.Rewinder, Emulator.AsStatable());
				AddOnScreenMessage(form.ShowDialog().IsOk()
					? "Rewind and State settings saved"
					: "Rewind config aborted");
			}
		}

		private void FileExtensionsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new FileExtensionPreferences(Config.PreferredPlatformsForExtensions);
			var result = form.ShowDialog();
			AddOnScreenMessage(result.IsOk()
				? "Rom Extension Preferences changed"
				: "Rom Extension Preferences cancelled");
		}

		private void BumpAutoFlushSaveRamTimer()
		{
			if (AutoFlushSaveRamIn > Config.FlushSaveRamFrames)
			{
				AutoFlushSaveRamIn = Config.FlushSaveRamFrames;
			}
		}

		private void CustomizeMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new EmuHawkOptions(BumpAutoFlushSaveRamTimer, Config, this.AddOnScreenMessage);
			form.ShowDialog();
		}

		private void ProfilesMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new ProfileConfig(this, Emulator, Config);
			if (form.ShowDialog().IsOk())
			{
				AddOnScreenMessage("Profile settings saved");

				// We hide the FirstBoot items since the user setup a Profile
				// Is it a bad thing to do this constantly?
				Config.FirstBoot = false;
				ProfileFirstBootLabel.Visible = false;
			}
			else
			{
				AddOnScreenMessage("Profile config aborted");
			}
		}

		private void ClockThrottleMenuItem_Click(object sender, EventArgs e)
		{
			Config.ClockThrottle ^= true;
			if (Config.ClockThrottle)
			{
				var old = Config.SoundThrottle;
				Config.SoundThrottle = false;
				if (old)
				{
					RewireSound();
				}

				old = Config.VSyncThrottle;
				Config.VSyncThrottle = false;
				if (old)
				{
					PresentationPanel.Resized = true;
				}
			}

			ThrottleMessage();
		}

		private void AudioThrottleMenuItem_Click(object sender, EventArgs e)
		{
			Config.SoundThrottle ^= true;
			RewireSound();
			if (Config.SoundThrottle)
			{
				Config.ClockThrottle = false;
				var old = Config.VSyncThrottle;
				Config.VSyncThrottle = false;
				if (old)
				{
					PresentationPanel.Resized = true;
				}
			}

			ThrottleMessage();
		}

		private void VsyncThrottleMenuItem_Click(object sender, EventArgs e)
		{
			Config.VSyncThrottle ^= true;
			PresentationPanel.Resized = true;
			if (Config.VSyncThrottle)
			{
				Config.ClockThrottle = false;
				var old = Config.SoundThrottle;
				Config.SoundThrottle = false;
				if (old)
				{
					RewireSound();
				}
			}

			if (!Config.VSync)
			{
				Config.VSync = true;
				VsyncMessage();
			}

			ThrottleMessage();
		}

		private void VsyncEnabledMenuItem_Click(object sender, EventArgs e)
		{
			Config.VSync ^= true;
			if (!Config.VSyncThrottle) // when vsync throttle is on, vsync is forced to on, so no change to make here
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
			Config.AutoMinimizeSkipping ^= true;
		}

		private void NeverSkipMenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 0; FrameSkipMessage(); }
		private void Frameskip1MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 1; FrameSkipMessage(); }
		private void Frameskip2MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 2; FrameSkipMessage(); }
		private void Frameskip3MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 3; FrameSkipMessage(); }
		private void Frameskip4MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 4; FrameSkipMessage(); }
		private void Frameskip5MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 5; FrameSkipMessage(); }
		private void Frameskip6MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 6; FrameSkipMessage(); }
		private void Frameskip7MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 7; FrameSkipMessage(); }
		private void Frameskip8MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 8; FrameSkipMessage(); }
		private void Frameskip9MenuItem_Click(object sender, EventArgs e) { Config.FrameSkip = 9; FrameSkipMessage(); }

		private void Speed50MenuItem_Click(object sender, EventArgs e) => ClickSpeedItem(50);
		private void Speed75MenuItem_Click(object sender, EventArgs e) => ClickSpeedItem(75);
		private void Speed100MenuItem_Click(object sender, EventArgs e) => ClickSpeedItem(100);
		private void Speed150MenuItem_Click(object sender, EventArgs e) => ClickSpeedItem(150);
		private void Speed200MenuItem_Click(object sender, EventArgs e) => ClickSpeedItem(200);
		private void Speed400MenuItem_Click(object sender, EventArgs e) => ClickSpeedItem(400);

		private void BothHkAndControllerMenuItem_Click(object sender, EventArgs e)
		{
			Config.InputHotkeyOverrideOptions = 0;
			UpdateKeyPriorityIcon();
		}

		private void InputOverHkMenuItem_Click(object sender, EventArgs e)
		{
			Config.InputHotkeyOverrideOptions = 1;
			UpdateKeyPriorityIcon();
		}

		private void HkOverInputMenuItem_Click(object sender, EventArgs e)
		{
			Config.InputHotkeyOverrideOptions = 2;
			UpdateKeyPriorityIcon();
		}

		private void SaveConfigMenuItem_Click(object sender, EventArgs e)
		{
			SaveConfig();
			AddOnScreenMessage("Saved settings");
		}

		private void SaveConfigAsMenuItem_Click(object sender, EventArgs e)
		{
			var path = Config.DefaultIniPath;
			using var sfd = new SaveFileDialog
			{
				InitialDirectory = Path.GetDirectoryName(path),
				FileName = Path.GetFileName(path),
				Filter = ConfigFileFSFilterString
			};

			if (sfd.ShowHawkDialog().IsOk())
			{
				SaveConfig(sfd.FileName);
				AddOnScreenMessage("Copied settings");
			}
		}

		private void LoadConfigMenuItem_Click(object sender, EventArgs e)
		{
			LoadConfigFile(Config.DefaultIniPath);
		}

		private void LoadConfigFromMenuItem_Click(object sender, EventArgs e)
		{
			var path = Config.DefaultIniPath;
			using var ofd = new OpenFileDialog
			{
				InitialDirectory = Path.GetDirectoryName(path),
				FileName = Path.GetFileName(path),
				Filter = ConfigFileFSFilterString
			};

			if (ofd.ShowHawkDialog().IsOk())
			{
				LoadConfigFile(ofd.FileName);
			}
		}

		private void ToolsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ToolBoxMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["ToolBox"].Bindings;
			RamWatchMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["RAM Watch"].Bindings;
			RamSearchMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["RAM Search"].Bindings;
			HexEditorMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Hex Editor"].Bindings;
			LuaConsoleMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Lua Console"].Bindings;
			CheatsMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Cheats"].Bindings;
			TAStudioMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["TAStudio"].Bindings;
			VirtualPadMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Virtual Pad"].Bindings;
			TraceLoggerMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Trace Logger"].Bindings;
			TraceLoggerMenuItem.Enabled = Tools.IsAvailable<TraceLogger>();
			CodeDataLoggerMenuItem.Enabled = Tools.IsAvailable<CDL>();

			TAStudioMenuItem.Enabled = Tools.IsAvailable<TAStudio>();

			CheatsMenuItem.Enabled = Tools.IsAvailable<Cheats>();
			HexEditorMenuItem.Enabled = Tools.IsAvailable<HexEditor>();
			RamSearchMenuItem.Enabled = Tools.IsAvailable<RamSearch>();
			RamWatchMenuItem.Enabled = Tools.IsAvailable<RamWatch>();

			DebuggerMenuItem.Enabled = Tools.IsAvailable<GenericDebugger>();

			BatchRunnerMenuItem.Visible = VersionInfo.DeveloperBuild;

			BasicBotMenuItem.Enabled = Tools.IsAvailable<BasicBot>();

			GameSharkConverterMenuItem.Enabled = Tools.IsAvailable<GameShark>();
			MacroToolMenuItem.Enabled = MovieSession.Movie.IsActive() && Tools.IsAvailable<MacroInputTool>();
			VirtualPadMenuItem.Enabled = Emulator.ControllerDefinition.Any();
		}

		private void ExternalToolMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			ExternalToolMenuItem.DropDownItems.Clear();

			foreach (var item in ExternalToolManager.ToolStripMenu)
			{
				if (item.Tag is ValueTuple<string, string> tuple)
				{
					if (item.Enabled)
					{
						item.Click += (clickEventSender, clickEventArgs) => Tools.LoadExternalToolForm(tuple.Item1, tuple.Item2);
					}
				}
				else
				{
					item.Image = Properties.Resources.ExclamationRed;
				}

				ExternalToolMenuItem.DropDownItems.Add(item);
			}

			if (ExternalToolMenuItem.DropDownItems.Count == 0)
			{
				ExternalToolMenuItem.DropDownItems.Add("None");
			}
		}

		private void ToolBoxMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<ToolBox>();
		}

		private void RamWatchMenuItem_Click(object sender, EventArgs e)
		{
			Tools.LoadRamWatch(true);
		}

		private void RamSearchMenuItem_Click(object sender, EventArgs e) => Tools.Load<RamSearch>();

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

			Tools.Load<TAStudio>();
		}

		private void HexEditorMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<HexEditor>();
		}

		private void TraceLoggerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<TraceLogger>();
		}

		private void DebuggerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<GenericDebugger>();
		}

		private void CodeDataLoggerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<CDL>();
		}

		private void MacroToolMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<MacroInputTool>();
		}

		private void VirtualPadMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<VirtualpadTool>();
		}

		private void BasicBotMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<BasicBot>();
		}

		private void CheatsMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<Cheats>();
		}

		private void CheatCodeConverterMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<GameShark>();
		}

		private void MultidiskBundlerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<MultiDiskBundler>();
		}

		private void BatchRunnerMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new BatchRun(CreateCoreComm);
			form.ShowDialog();
		}

		private void NesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			var boardName = Emulator.HasBoardInfo() ? Emulator.AsBoardInfo().BoardName : null;
			FDSControlsMenuItem.Enabled = boardName == "FDS";

			VSControlsMenuItem.Enabled =
			VSSettingsMenuItem.Enabled =
				(Emulator is NES nes && nes.IsVS)
				|| (Emulator is SubNESHawk sub && sub.IsVs);

			NESSoundChannelsMenuItem.Enabled = Tools.IsAvailable<NESSoundConfig>();
			MovieSettingsMenuItem.Enabled = (Emulator is NES || Emulator is SubNESHawk)
				&& !MovieSession.Movie.IsActive();

			NesControllerSettingsMenuItem.Enabled = Tools.IsAvailable<NesControllerSettings>()
				&& !MovieSession.Movie.IsActive();

			BarcodeReaderMenuItem.Enabled = ServiceInjector.IsAvailable(Emulator.ServiceProvider, typeof(BarcodeEntry));

			MusicRipperMenuItem.Enabled = Tools.IsAvailable<NESMusicRipper>();
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

		private void NesPpuViewerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<NesPPU>();
		}

		private void NesNametableViewerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<NESNameTableViewer>();
		}

		private void MusicRipperMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<NESMusicRipper>();
		}

		private void NesGraphicSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is NES nes)
			{
				using var form = new NESGraphicsConfig(this, Config, nes.GetSettings().Clone());
				form.ShowDialog(this);
			}
			else if (Emulator is SubNESHawk sub)
			{
				using var form = new NESGraphicsConfig(this, Config, sub.GetSettings().Clone());
				form.ShowDialog(this);
			}
			else if (Emulator is QuickNES quickNes)
			{
				using var form = new QuickNesConfig(this, Config, quickNes.GetSettings().Clone());
				form.ShowDialog(this);
			}
		}

		private void NesSoundChannelsMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<NESSoundConfig>();
		}

		private void VsSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is NES nes && nes.IsVS)
			{
				using var form = new NesVsSettings(this, nes.GetSyncSettings().Clone());
				form.ShowHawkDialog();
			}
			else if (Emulator is SubNESHawk sub && sub.IsVs)
			{
				using var form = new NesVsSettings(this, sub.GetSyncSettings().Clone());
				form.ShowHawkDialog();
			}
		}

		private void FdsEjectDiskMenuItem_Click(object sender, EventArgs e)
		{
			if (!MovieSession.Movie.IsPlaying())
			{
				InputManager.ClickyVirtualPadController.Click("FDS Eject");
				AddOnScreenMessage("FDS disk ejected.");
			}
		}

		private void VsInsertCoinP1MenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is NES nes && nes.IsVS
			|| Emulator is SubNESHawk sub && sub.IsVs)
			{
				if (!MovieSession.Movie.IsPlaying())
				{
					InputManager.ClickyVirtualPadController.Click("Insert Coin P1");
					AddOnScreenMessage("P1 Coin Inserted");
				}
			}
		}

		private void VsInsertCoinP2MenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is NES nes && nes.IsVS
				|| Emulator is SubNESHawk sub && sub.IsVs)
			{
				if (!MovieSession.Movie.IsPlaying())
				{
					InputManager.ClickyVirtualPadController.Click("Insert Coin P2");
					AddOnScreenMessage("P2 Coin Inserted");
				}
			}
		}

		private void VsServiceSwitchMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is NES nes && nes.IsVS
				|| Emulator is SubNESHawk sub && sub.IsVs)
			{
				if (!MovieSession.Movie.IsPlaying())
				{
					InputManager.ClickyVirtualPadController.Click("Service Switch");
					AddOnScreenMessage("Service Switch Pressed");
				}
			}
		}

		private void NesControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is NES nes)
			{
				using var form = new NesControllerSettings(this, nes.GetSyncSettings().Clone());
				form.ShowDialog();
			}
			else if (Emulator is SubNESHawk sub)
			{
				using var form = new NesControllerSettings(this, sub.GetSyncSettings().Clone());
				form.ShowDialog();
			}
			else if (Emulator is QuickNES)
			{
				GenericCoreConfig.DoDialog(this, "QuickNES Controller Settings", true, false);
			}
		}

		private void MovieSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is NES nes)
			{
				using var dlg = new NESSyncSettingsForm(this, nes.GetSyncSettings().Clone(), nes.HasMapperProperties);
				dlg.ShowDialog(this);
			}
			else if (Emulator is SubNESHawk sub)
			{
				using var dlg = new NESSyncSettingsForm(this, sub.GetSyncSettings().Clone(), sub.HasMapperProperties);
				dlg.ShowDialog(this);
			}
			
		}

		private void BarcodeReaderMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<BarcodeEntry>();
		}

		private void Ti83SubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadKeypadMenuItem.Checked = Config.Ti83AutoloadKeyPad;
		}

		private void Ti83KeypadMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<TI83KeyPad>();
		}

		private void AutoloadKeypadMenuItem_Click(object sender, EventArgs e)
		{
			Config.Ti83AutoloadKeyPad ^= true;
		}

		private void Ti83LoadTIFileMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is TI83 ti83)
			{
				using var ofd = new OpenFileDialog
				{
					Filter = new FilesystemFilterSet(new FilesystemFilter("TI-83 Program Files", new[] { "83p", "8xp" })).ToString(),
					InitialDirectory = Config.PathEntries.RomAbsolutePath(Emulator.SystemId),
					RestoreDirectory = true
				};

				if (ofd.ShowDialog().IsOk())
				{
					try
					{
						ti83.LinkPort.SendFileToCalc(File.OpenRead(ofd.FileName), true);
					}
					catch (IOException ex)
					{
						var message =
							$"Invalid file format. Reason: {ex.Message} \nForce transfer? This may cause the calculator to crash.";

						if (MessageBox.Show(message, "Upload Failed", MessageBoxButtons.YesNoCancel,
							MessageBoxIcon.Question) == DialogResult.Yes)
						{
							ti83.LinkPort.SendFileToCalc(File.OpenRead(ofd.FileName), false);
						}
					}
				}
			}
		}

		private void Ti83PaletteMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is TI83 ti83)
			{
				using var form = new TI83PaletteConfig(this, ti83.GetSettings().Clone());
				AddOnScreenMessage(form.ShowDialog().IsOk()
					? "Palette settings saved"
					: "Palette config aborted");
			}
		}

		private void A7800SubMenu_DropDownOpened(object sender, EventArgs e)
		{
			A7800ControllerSettingsMenuItem.Enabled
				= A7800FilterSettingsMenuItem.Enabled
				= MovieSession.Movie.NotActive();
		}

		private void A7800ControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is A7800Hawk atari7800Hawk)
			{
				using var form = new A7800ControllerSettings(this, atari7800Hawk.GetSyncSettings().Clone());
				form.ShowDialog();
			}
		}

		private void A7800FilterSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is A7800Hawk atari7800Hawk)
			{
				using var form = new A7800FilterSettings(this, atari7800Hawk.GetSyncSettings().Clone());
				form.ShowDialog();
			}
		}

		private void GbCoreSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is Gameboy gb)
			{
				GBPrefs.DoGBPrefsDialog(this, gb);
			}
			else // SameBoy
			{
				GenericCoreConfig.DoDialog(this, "Gameboy Settings");
			}
		}

		private void GbGpuViewerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<GbGpuView>();
		}

		private void GbPrinterViewerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<GBPrinterView>();
		}

		private void NDSSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			NdsSyncSettingsMenuItem.Enabled = MovieSession.Movie.NotActive();
		}

		private void NDSSettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "NDS Settings", false, true);
			FrameBufferResized();
		}

		private void NDSSyncSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is MelonDS ds)
			{
				using var form = new NdsSettings(this, ds.GetSyncSettings().Clone());
				form.ShowDialog();
			}
		}

		private void PsxSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			PSXControllerSettingsMenuItem.Enabled = MovieSession.Movie.NotActive();
		}

		private void PsxControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is Octoshock psx)
			{
				using var form = new PSXControllerConfig(this, psx.GetSyncSettings().Clone());
				form.ShowDialog();
			}
		}

		private void PsxOptionsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is Octoshock psx)
			{
				if (PSXOptions.DoSettingsDialog(this, Config, psx).IsOk())
				{
					FrameBufferResized();
				}
			}
		}

		private void PsxDiscControlsMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<VirtualpadTool>().ScrollToPadSchema("Console");
		}

		private void PsxHashDiscsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is Octoshock psx)
			{
				using var form = new PSXHashDiscs(psx);
				form.ShowDialog();
			}
		}

		private void SnesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SNESControllerConfigurationMenuItem.Enabled = MovieSession.Movie.NotActive();
			SnesGfxDebuggerMenuItem.Enabled = true;
		}

		private void SNESControllerConfigurationMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is LibsnesCore bsnes)
			{
				using var form = new SNESControllerSettings(this, bsnes.GetSyncSettings().Clone());
				form.ShowDialog();
			}
		}

		private void SnesGfxDebuggerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<SNESGraphicsDebugger>();
		}

		private void SnesOptionsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is LibsnesCore bsnes)
			{
				SNESOptions.DoSettingsDialog(this, bsnes);
			}
		}

		private void ColecoSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is ColecoVision coleco)
			{
				var ss = coleco.GetSyncSettings();
				ColecoSkipBiosMenuItem.Checked = ss.SkipBiosIntro;
				ColecoUseSGMMenuItem.Checked = ss.UseSGM;
				ColecoControllerSettingsMenuItem.Enabled = MovieSession.Movie.NotActive();
			}
		}

		private void ColecoSkipBiosMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is ColecoVision coleco)
			{
				var ss = coleco.GetSyncSettings();
				ss.SkipBiosIntro ^= true;
				PutCoreSyncSettings(ss);
			}
		}

		private void ColecoUseSGMMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is ColecoVision coleco)
			{
				var ss = coleco.GetSyncSettings();
				ss.UseSGM ^= true;
				PutCoreSyncSettings(ss);
			}
		}

		private void ColecoControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is ColecoVision coleco)
			{
				using var form = new ColecoControllerSettings(this, coleco.GetSyncSettings().Clone());
				form.ShowDialog();
			}
		}

		private void N64SubMenu_DropDownOpened(object sender, EventArgs e)
		{
			N64PluginSettingsMenuItem.Enabled =
				N64ControllerSettingsMenuItem.Enabled =
				N64ExpansionSlotMenuItem.Enabled = 
				MovieSession.Movie.NotActive();

			N64CircularAnalogRangeMenuItem.Checked = Config.N64UseCircularAnalogConstraint;

			var s = ((N64)Emulator).GetSettings();
			MupenStyleLagMenuItem.Checked = s.UseMupenStyleLag;

			N64ExpansionSlotMenuItem.Checked = ((N64)Emulator).UsingExpansionSlot;
			N64ExpansionSlotMenuItem.Enabled = !((N64)Emulator).IsOverridingUserExpansionSlotSetting;
		}

		private void N64PluginSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new N64VideoPluginConfig(this, Config, Emulator);
			if (form.ShowDialog().IsOk())
			{
				if (Emulator.IsNull())
				{
					AddOnScreenMessage("Plugin settings saved");
				}
				else
				{
					// Do nothing, Reboot is being flagged already if they changed anything
				}
			}
			else
			{
				AddOnScreenMessage("Plugin settings aborted");
			}
		}

		private void N64ControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is N64 n64)
			{
				using var form = new N64ControllersSetup(this, n64.GetSyncSettings().Clone());
				form.ShowDialog();
			}
		}

		private void N64CircularAnalogRangeMenuItem_Click(object sender, EventArgs e)
		{
			Config.N64UseCircularAnalogConstraint ^= true;
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
			if (Emulator is N64 n64)
			{
				var ss = n64.GetSyncSettings();
				ss.DisableExpansionSlot ^= true;
				n64.PutSyncSettings(ss);
				FlagNeedsReboot();
			}
		}

		private void DgbSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is GambatteLink gambatte)
			{
				DGBPrefs.DoDGBPrefsDialog(this, gambatte);
			}
		}

		private void GenericCoreSettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, $"{Emulator.DisplayName()} Settings");
		}

		private void AppleIISettingsMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Apple II Settings");
		}

		private void AppleSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is AppleII a)
			{
				AppleDisksSubMenu.Enabled = a.DiskCount > 1;
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

		private void C64SubMenu_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is C64 c64)
			{
				C64DisksSubMenu.Enabled = c64.DiskCount > 1;
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

		private void IntVSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			IntVControllerSettingsMenuItem.Enabled = MovieSession.Movie.NotActive();
		}

		private void IntVControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is Intellivision intv)
			{
				using var form = new IntvControllerSettings(this, intv.GetSyncSettings().Clone());
				form.ShowDialog();
			}
		}

		private void ZXSpectrumControllerConfigurationMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is ZXSpectrum zxs)
			{
				using var form = new ZxSpectrumJoystickSettings(this, zxs.GetSyncSettings().Clone());
				form.ShowDialog();
			}
		}

		private void ZXSpectrumCoreEmulationSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is ZXSpectrum speccy)
			{
				using var form = new ZxSpectrumCoreEmulationSettings(this, speccy.GetSyncSettings().Clone());
				form.ShowDialog();
			}
		}

		private void ZXSpectrumNonSyncSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is ZXSpectrum speccy)
			{
				using var form = new ZxSpectrumNonSyncSettings(this, speccy.GetSettings().Clone());
				form.ShowDialog();
			}
		}

		private void ZXSpectrumAudioSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is ZXSpectrum speccy)
			{
				using var form = new ZxSpectrumAudioSettings(this, speccy.GetSettings().Clone());
				form.ShowDialog();
			}
		}

		private void ZXSpectrumPokeMemoryMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is ZXSpectrum speccy)
			{
				using var form = new ZxSpectrumPokeMemory(this, speccy);
				form.ShowDialog();
			}
		}

		private void ZXSpectrumMediaMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is ZXSpectrum speccy)
			{
				ZXSpectrumTapesSubMenu.Enabled = speccy._tapeInfo.Count > 0;
				ZXSpectrumDisksSubMenu.Enabled = speccy._diskInfo.Count > 0;
			}
		}

		private void ZXSpectrumTapesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ZXSpectrumTapesSubMenu.DropDownItems.Clear();

			var items = new List<ToolStripMenuItem>();

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
				DefaultExt = "szx",
				Filter = new FilesystemFilter("ZX-State files", new[] { "szx" }).ToString(),
				RestoreDirectory = true,
				SupportMultiDottedExtensions = true,
				Title = "EXPERIMENTAL - Export 3rd party snapshot formats"
			};

			try
			{
				if (zxSnapExpDialog.ShowDialog().IsOk())
				{
					var speccy = (ZXSpectrum)Emulator;
					var snap = speccy.GetSZXSnapshot();
					File.WriteAllBytes(zxSnapExpDialog.FileName, snap);
				}
			}
			catch (Exception)
			{
			}
		}

		private void AmstradCpcCoreEmulationSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is AmstradCPC cpc)
			{
				using var form = new AmstradCpcCoreEmulationSettings(this, cpc.GetSyncSettings().Clone());
				form.ShowDialog();
			}
		}

		private void AmstradCpcAudioSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is AmstradCPC cpc)
			{
				using var form = new AmstradCpcAudioSettings(this, cpc.GetSettings().Clone());
				form.ShowDialog();
			}
		}

		private void AmstradCpcPokeMemoryMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is AmstradCPC cpc)
			{
				using var form = new AmstradCpcPokeMemory(this, cpc);
				form.ShowDialog();
			}
		}

		private void AmstradCpcMediaMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is AmstradCPC)
			{
				AmstradCPCTapesSubMenu.Enabled = ((AmstradCPC)Emulator)._tapeInfo.Count > 0;
				AmstradCPCDisksSubMenu.Enabled = ((AmstradCPC)Emulator)._diskInfo.Count > 0;
			}
		}

		private void AmstradCpcTapesSubMenu_DropDownOpened(object sender, EventArgs e)
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

		private void AmstradCpcDisksSubMenu_DropDownOpened(object sender, EventArgs e)
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

		private void AmstradCpcNonSyncSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is AmstradCPC cpc)
			{
				using var form = new AmstradCpcNonSyncSettings(this, cpc.GetSettings().Clone());
				form.ShowDialog();
			}
			
		}

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
			Tools.Load<CoreFeatureAnalysis>();
		}

		private void AboutMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new BizBox();
			form.ShowDialog();
		}

		private void MainFormContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			_wasPaused = EmulatorPaused;
			_didMenuPause = true;
			PauseEmulator();

			OpenRomContextMenuItem.Visible = Emulator.IsNull() || _inFullscreen;

			bool showMenuVisible = _inFullscreen || !MainMenuStrip.Visible; // need to always be able to restore this as an emergency measure

			if (_argParser._chromeless)
			{
				showMenuVisible = true; // I decided this was always possible in chrome-less mode, we'll see what they think
			}

			var movieIsActive = MovieSession.Movie.IsActive();

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
				!Emulator.IsNull() && !movieIsActive;

			RestartMovieContextMenuItem.Visible =
				StopMovieContextMenuItem.Visible =
				ViewSubtitlesContextMenuItem.Visible =
				ViewCommentsContextMenuItem.Visible =
				SaveMovieContextMenuItem.Visible =
				SaveMovieAsContextMenuItem.Visible =
					movieIsActive;

			BackupMovieContextMenuItem.Visible = movieIsActive;

			StopNoSaveContextMenuItem.Visible = movieIsActive && MovieSession.Movie.Changes;

			AddSubtitleContextMenuItem.Visible = !Emulator.IsNull() && movieIsActive && !MovieSession.ReadOnly;

			ConfigContextMenuItem.Visible = _inFullscreen;

			ClearSRAMContextMenuItem.Visible = File.Exists(Config.PathEntries.SaveRamAbsolutePath(Game, MovieSession.Movie));

			ContextSeparator_AfterROM.Visible = OpenRomContextMenuItem.Visible || LoadLastRomContextMenuItem.Visible;

			LoadLastRomContextMenuItem.Enabled = !Config.RecentRoms.Empty;
			LoadLastMovieContextMenuItem.Enabled = !Config.RecentMovies.Empty;

			if (movieIsActive)
			{
				if (MovieSession.ReadOnly)
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

			var file = new FileInfo($"{SaveStatePrefix()}.QuickSave{Config.SaveSlot}.State.bak");

			if (file.Exists)
			{
				UndoSavestateContextMenuItem.Enabled = true;
				if (_stateSlots.IsRedo(MovieSession.Movie, Config.SaveSlot))
				{
					UndoSavestateContextMenuItem.Text = $"Redo Save to slot {Config.SaveSlot}";
					UndoSavestateContextMenuItem.Image = Properties.Resources.Redo;
				}
				else
				{
					UndoSavestateContextMenuItem.Text = $"Undo Save to slot {Config.SaveSlot}";
					UndoSavestateContextMenuItem.Image = Properties.Resources.Undo;
				}
			}
			else
			{
				UndoSavestateContextMenuItem.Enabled = false;
				UndoSavestateContextMenuItem.Text = "Undo Savestate";
				UndoSavestateContextMenuItem.Image = Properties.Resources.Undo;
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

		private void DisplayConfigMenuItem_Click(object sender, EventArgs e)
		{
			using var window = new DisplayConfig(Config);
			if (window.ShowDialog().IsOk())
			{
				DisplayManager.RefreshUserShader();
				FrameBufferResized();
				SynchChrome();
				if (window.NeedReset)
				{
					AddOnScreenMessage("Restart program for changed settings");
				}
			}
		}

		private void LoadLastRomContextMenuItem_Click(object sender, EventArgs e)
		{
			LoadRomFromRecent(Config.RecentRoms.MostRecent);
		}

		private void LoadLastMovieContextMenuItem_Click(object sender, EventArgs e)
		{
			LoadMoviesFromRecent(Config.RecentMovies.MostRecent);
		}

		private void BackupMovieContextMenuItem_Click(object sender, EventArgs e)
		{
			MovieSession.Movie.SaveBackup();
			AddOnScreenMessage("Backup movie saved.");
		}

		private void ViewSubtitlesContextMenuItem_Click(object sender, EventArgs e)
		{
			if (MovieSession.Movie.IsActive())
			{
				using var form = new EditSubtitlesForm(MovieSession.Movie, MovieSession.ReadOnly);
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
			for (int i = 0; i < MovieSession.Movie.Subtitles.Count; i++)
			{
				sub = MovieSession.Movie.Subtitles[i];
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

			if (subForm.ShowDialog().IsOk())
			{
				if (index >= 0)
				{
					MovieSession.Movie.Subtitles.RemoveAt(index);
				}

				MovieSession.Movie.Subtitles.Add(subForm.Sub);
			}
		}

		private void ViewCommentsContextMenuItem_Click(object sender, EventArgs e)
		{
			if (MovieSession.Movie.IsActive())
			{
				using var form = new EditCommentsForm(MovieSession.Movie, MovieSession.ReadOnly);
				form.ShowDialog();
			}
		}

		private void UndoSavestateContextMenuItem_Click(object sender, EventArgs e)
		{
			_stateSlots.SwapBackupSavestate(MovieSession.Movie, $"{SaveStatePrefix()}.QuickSave{Config.SaveSlot}.State", Config.SaveSlot);
			AddOnScreenMessage($"Save slot {Config.SaveSlot} restored.");
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

		private void DumpStatusButton_Click(object sender, EventArgs e)
		{
			string details = Emulator.RomDetails();
			if (string.IsNullOrWhiteSpace(details))
			{
				details = _defaultRomDetails;
			}

			if (!string.IsNullOrEmpty(details))
			{
				Tools.Load<LogWindow>();
				((LogWindow) Tools.Get<LogWindow>()).ShowReport("Dump Status Report", details);
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
				if (HasSlot(slot))
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
			switch (Config.InputHotkeyOverrideOptions)
			{
				default:
				case 0:
					Config.InputHotkeyOverrideOptions = 1;
					break;
				case 1:
					Config.InputHotkeyOverrideOptions = 2;
					break;
				case 2:
					Config.InputHotkeyOverrideOptions = 0;
					break;
			}

			UpdateKeyPriorityIcon();
		}

		private void FreezeStatus_Click(object sender, EventArgs e)
		{
			if (CheatStatusButton.Visible)
			{
				Tools.Load<Cheats>();
			}
		}

		private void ProfileFirstBootLabel_Click(object sender, EventArgs e)
		{
			// We do not check if the user is actually setting a profile here.
			// This is intentional.
			using var profileForm = new ProfileConfig(this, Emulator, Config);
			profileForm.ShowDialog();
			Config.FirstBoot = false;
			ProfileFirstBootLabel.Visible = false;
		}

		private void LinkConnectStatusBarButton_Click(object sender, EventArgs e)
		{
			// toggle Link status (only outside of a movie session)
			if (!MovieSession.Movie.IsPlaying())
			{
				Emulator.AsLinkable().LinkConnected ^= true;
				Console.WriteLine("Cable connect status to {0}", Emulator.AsLinkable().LinkConnected);
			}
		}

		private void UpdateNotification_Click(object sender, EventArgs e)
		{
			Sound.StopSound();
			DialogResult result = MessageBox.Show(this,
				$"Version {Config.UpdateLatestVersion} is now available. Would you like to open the BizHawk homepage?\r\n\r\nClick \"No\" to hide the update notification for this version.",
				"New Version Available", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			Sound.StartSound();

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

		private void MainForm_Activated(object sender, EventArgs e)
		{
			if (!Config.RunInBackground)
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
			if (!Config.RunInBackground)
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
			if (_inFullscreen && Config.DispChromeFullscreenAutohideMouse)
			{
				AutohideCursor(true);
			}
		}

		private void MainForm_Enter(object sender, EventArgs e)
		{
			AutohideCursor(false);
		}

		private void MainForm_Resize(object sender, EventArgs e)
		{
			PresentationPanel.Resized = true;
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			if (Emulator is TI83 && Config.Ti83AutoloadKeyPad)
			{
				Tools.Load<TI83KeyPad>();
			}

			Tools.AutoLoad();

			if (Config.RecentWatches.AutoLoad)
			{
				Tools.LoadRamWatch(!Config.DisplayRamWatch);
			}

			if (Config.Cheats.Recent.AutoLoad)
			{
				Tools.Load<Cheats>();
			}

			HandlePlatformMenus();
		}

		protected override void OnClosed(EventArgs e)
		{
			_windowClosedAndSafeToExitProcess = true;
			base.OnClosed(e);
		}

		private void MainformMenu_MenuActivate(object sender, EventArgs e)
		{
			HandlePlatformMenus();
			if (Config.PauseWhenMenuActivated)
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
			e.Set(DragDropEffects.Copy);
		}

		private void FormDragDrop(object sender, DragEventArgs e)
		{
			Sound.StopSound();
			try
			{
				FormDragDrop_internal(e);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Exception on drag and drop:\n{ex}");
			}
			finally
			{
				Sound.StartSound();
			}
		}
	}
}
