using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Bizware.Audio;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.Cores.Atari.A7800Hawk;
using BizHawk.Emulation.Cores.Atari.Atari2600;
using BizHawk.Emulation.Cores.Atari.Jaguar;
using BizHawk.Emulation.Cores.Atari.Lynx;
using BizHawk.Emulation.Cores.Calculators.Emu83;
using BizHawk.Emulation.Cores.Calculators.TI83;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Computers.Amiga;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;
using BizHawk.Emulation.Cores.Computers.AppleII;
using BizHawk.Emulation.Cores.Computers.Commodore64;
using BizHawk.Emulation.Cores.Computers.MSX;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;
using BizHawk.Emulation.Cores.Computers.TIC80;
using BizHawk.Emulation.Cores.Consoles.Belogic;
using BizHawk.Emulation.Cores.Consoles.ChannelF;
using BizHawk.Emulation.Cores.Consoles.NEC.PCE;
using BizHawk.Emulation.Cores.Consoles.NEC.PCFX;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Faust;
using BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Consoles.Nintendo.VB;
using BizHawk.Emulation.Cores.Consoles.O2Hawk;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Consoles.Sega.PicoDrive;
using BizHawk.Emulation.Cores.Consoles.Sega.Saturn;
using BizHawk.Emulation.Cores.Consoles.SNK;
using BizHawk.Emulation.Cores.Consoles.Vectrex;
using BizHawk.Emulation.Cores.Intellivision;
using BizHawk.Emulation.Cores.Libretro;
using BizHawk.Emulation.Cores.Nintendo.BSNES;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.Sameboy;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Nintendo.SubGBHawk;
using BizHawk.Emulation.Cores.Nintendo.SubNESHawk;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.GGHawkLink;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.Cores.WonderSwan;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm
	{
		private static readonly FilesystemFilterSet MAMERomsFSFilterSet = new(new FilesystemFilter("MAME Arcade ROMs", new[] { "zip" }))
		{
			AppendAllFilesEntry = false,
		};

		private static readonly FilesystemFilterSet ScreenshotsFSFilterSet = new(FilesystemFilter.PNGs)
		{
			AppendAllFilesEntry = false,
		};

		private static readonly FilesystemFilterSet TI83ProgramFilesFSFilterSet = new(new FilesystemFilter("TI-83 Program Files", new[] { "83p", "8xp" }));

		private static readonly FilesystemFilterSet ZXStateFilesFSFilterSet = new(new FilesystemFilter("ZX-State files", new[] { "szx" }))
		{
			AppendAllFilesEntry = false,
		};

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveStateSubMenu.Enabled =
				LoadStateSubMenu.Enabled =
				SaveSlotSubMenu.Enabled =
				Emulator.HasSavestates();

			OpenRomMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Open ROM"];
			CloseRomMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Close ROM"];

			CloseRomMenuItem.Enabled = !Emulator.IsNull();

			var hasSaveRam = Emulator.HasSaveRam();
			bool needBold = hasSaveRam && Emulator.AsSaveRam().SaveRamModified;

			SaveRAMSubMenu.Enabled = hasSaveRam;
			SaveRAMSubMenu.SetStyle(needBold ? FontStyle.Bold : FontStyle.Regular);

			AVSubMenu.Enabled =
			ScreenshotSubMenu.Enabled =
				Emulator.HasVideoProvider();
		}

		private void RecentRomMenuItem_DropDownOpened(object sender, EventArgs e)
			=> RecentRomSubMenu.ReplaceDropDownItems(Config.RecentRoms.RecentMenu(this, LoadRomFromRecent, "ROM", romLoading: true));

		private bool HasSlot(int slot) => _stateSlots.HasSlot(Emulator, MovieSession.Movie, slot, SaveStatePrefix());

		private void SaveStateSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			void SetSlotFont(ToolStripMenuItemEx menu, int slot) => menu.SetStyle(
				HasSlot(slot) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular);

			SetSlotFont(SaveState1MenuItem, 1);
			SetSlotFont(SaveState2MenuItem, 2);
			SetSlotFont(SaveState3MenuItem, 3);
			SetSlotFont(SaveState4MenuItem, 4);
			SetSlotFont(SaveState5MenuItem, 5);
			SetSlotFont(SaveState6MenuItem, 6);
			SetSlotFont(SaveState7MenuItem, 7);
			SetSlotFont(SaveState8MenuItem, 8);
			SetSlotFont(SaveState9MenuItem, 9);
			SetSlotFont(SaveState0MenuItem, 10);

			AutosaveLastSlotMenuItem.Checked = Config.AutoSaveLastSaveSlot;

			SaveState1MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 1"];
			SaveState2MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 2"];
			SaveState3MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 3"];
			SaveState4MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 4"];
			SaveState5MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 5"];
			SaveState6MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 6"];
			SaveState7MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 7"];
			SaveState8MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 8"];
			SaveState9MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 9"];
			SaveState0MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save State 10"];
			SaveNamedStateMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save Named State"];
		}

		private void LoadStateSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			LoadState1MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 1"];
			LoadState2MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 2"];
			LoadState3MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 3"];
			LoadState4MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 4"];
			LoadState5MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 5"];
			LoadState6MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 6"];
			LoadState7MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 7"];
			LoadState8MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 8"];
			LoadState9MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 9"];
			LoadState0MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load State 10"];
			LoadNamedStateMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Load Named State"];

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
			LoadState0MenuItem.Enabled = HasSlot(10);
		}

		private void SaveSlotSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SelectSlot1MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 1"];
			SelectSlot2MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 2"];
			SelectSlot3MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 3"];
			SelectSlot4MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 4"];
			SelectSlot5MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 5"];
			SelectSlot6MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 6"];
			SelectSlot7MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 7"];
			SelectSlot8MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 8"];
			SelectSlot9MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 9"];
			SelectSlot0MenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Select State 10"];
			PreviousSlotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Previous Slot"];
			NextSlotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Next Slot"];
			SaveToCurrentSlotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Quick Save"];
			LoadCurrentSlotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Quick Load"];

			SelectSlot1MenuItem.Checked = Config.SaveSlot == 1;
			SelectSlot2MenuItem.Checked = Config.SaveSlot == 2;
			SelectSlot3MenuItem.Checked = Config.SaveSlot == 3;
			SelectSlot4MenuItem.Checked = Config.SaveSlot == 4;
			SelectSlot5MenuItem.Checked = Config.SaveSlot == 5;
			SelectSlot6MenuItem.Checked = Config.SaveSlot == 6;
			SelectSlot7MenuItem.Checked = Config.SaveSlot == 7;
			SelectSlot8MenuItem.Checked = Config.SaveSlot == 8;
			SelectSlot9MenuItem.Checked = Config.SaveSlot == 9;
			SelectSlot0MenuItem.Checked = Config.SaveSlot is 10;
		}

		private void SaveRamSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			FlushSaveRAMMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Flush SaveRAM"];
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

			ReadonlyMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Toggle read-only"];
			RecordMovieMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Record Movie"];
			PlayMovieMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Play Movie"];
			StopMovieMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Stop Movie"];
			PlayFromBeginningMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Play from beginning"];
			SaveMovieMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Save Movie"];

			PlayMovieMenuItem.Enabled
				= RecordMovieMenuItem.Enabled
				= RecentMovieSubMenu.Enabled
					= !Emulator.IsNull() && !Tools.IsLoaded<TAStudio>();

			// Record movie dialog should not be opened while in need of a reboot,
			// Otherwise the wrong sync settings could be set for the recording movie and cause crashes
			RecordMovieMenuItem.Enabled &= !RebootStatusBarIcon.Visible;

			PlayFromBeginningMenuItem.Enabled = MovieSession.Movie.IsActive() && !Tools.IsLoaded<TAStudio>();
		}

		private void RecentMovieSubMenu_DropDownOpened(object sender, EventArgs e)
			=> RecentMovieSubMenu.ReplaceDropDownItems(Config.RecentMovies.RecentMenu(this, LoadMoviesFromRecent, "Movie"));

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
			ConfigAndRecordAVMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Record A/V"];
			StopAVIMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Stop A/V"];
			CaptureOSDMenuItem.Checked = Config.AviCaptureOsd;
			CaptureLuaMenuItem.Checked = Config.AviCaptureLua || Config.AviCaptureOsd; // or with osd is for better compatibility with old config files

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
			ScreenshotMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Screenshot"];
			ScreenshotClipboardMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Screen Raw to Clipboard"];
			ScreenshotClientClipboardMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Screen Client to Clipboard"];
		}

		private void OpenRomMenuItem_Click(object sender, EventArgs e)
		{
			OpenRom();
		}

		private void OpenAdvancedMenuItem_Click(object sender, EventArgs e)
		{
			using var oac = new OpenAdvancedChooser(this, Config, CreateCoreComm, Game, RunLibretroCoreChooser);
			if (this.ShowDialogWithTempMute(oac) == DialogResult.Cancel) return;

			if (oac.Result == AdvancedRomLoaderType.LibretroLaunchNoGame)
			{
				var argsNoGame = new LoadRomArgs
				{
					OpenAdvanced = new OpenAdvanced_LibretroNoGame(Config.LibretroCore)
				};
				_ = LoadRom(string.Empty, argsNoGame);
				return;
			}

			var args = new LoadRomArgs();

			var filter = RomLoader.RomFilter;

			if (oac.Result == AdvancedRomLoaderType.LibretroLaunchGame)
			{
				args.OpenAdvanced = new OpenAdvanced_Libretro();
				filter = oac.SuggestedExtensionFilter!;
			}
			else if (oac.Result == AdvancedRomLoaderType.ClassicLaunchGame)
			{
				args.OpenAdvanced = new OpenAdvanced_OpenRom();
			}
			else if (oac.Result == AdvancedRomLoaderType.MameLaunchGame)
			{
				args.OpenAdvanced = new OpenAdvanced_MAME();
				filter = MAMERomsFSFilterSet;
			}
			else
			{
				throw new InvalidOperationException("Automatic Alpha Sanitizer");
			}
			var result = this.ShowFileOpenDialog(
				filter: filter,
				filterIndex: ref _lastOpenRomFilter,
				initDir: Config.PathEntries.RomAbsolutePath(Emulator.SystemId),
				windowTitle: "Open Advanced");
			if (result is null) return;
			FileInfo file = new(result);
			Config.PathEntries.LastRomPath = file.DirectoryName;
			_ = LoadRom(file.FullName, args);
		}

		private void CloseRomMenuItem_Click(object sender, EventArgs e)
		{
			Console.WriteLine($"Closing rom clicked Frame: {Emulator.Frame} Emulator: {Emulator.GetType().Name}");
			CloseRom();
			Console.WriteLine($"Closing rom clicked DONE Frame: {Emulator.Frame} Emulator: {Emulator.GetType().Name}");
		}

		private void QuickSavestateMenuItem_Click(object sender, EventArgs e)
			=> SaveQuickSave(int.Parse(((ToolStripMenuItem) sender).Text));

		private void SaveNamedStateMenuItem_Click(object sender, EventArgs e) => SaveStateAs();

		private void QuickLoadstateMenuItem_Click(object sender, EventArgs e)
			=> LoadQuickSave(int.Parse(((ToolStripMenuItem) sender).Text));

		private void LoadNamedStateMenuItem_Click(object sender, EventArgs e) => LoadStateAs();

		private void AutoloadLastSlotMenuItem_Click(object sender, EventArgs e)
		{
			Config.AutoLoadLastSaveSlot ^= true;
		}
		private void AutosaveLastSlotMenuItem_Click(object sender, EventArgs e)
		{
			Config.AutoSaveLastSaveSlot ^= true;
		}

		private void SelectSlotMenuItems_Click(object sender, EventArgs e)
		{
			if (sender == SelectSlot1MenuItem) Config.SaveSlot = 1;
			else if (sender == SelectSlot2MenuItem) Config.SaveSlot = 2;
			else if (sender == SelectSlot3MenuItem) Config.SaveSlot = 3;
			else if (sender == SelectSlot4MenuItem) Config.SaveSlot = 4;
			else if (sender == SelectSlot5MenuItem) Config.SaveSlot = 5;
			else if (sender == SelectSlot6MenuItem) Config.SaveSlot = 6;
			else if (sender == SelectSlot7MenuItem) Config.SaveSlot = 7;
			else if (sender == SelectSlot8MenuItem) Config.SaveSlot = 8;
			else if (sender == SelectSlot9MenuItem) Config.SaveSlot = 9;
			else if (sender == SelectSlot0MenuItem) Config.SaveSlot = 10;

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
			=> SavestateCurrentSlot();

		private void SavestateCurrentSlot()
			=> SaveQuickSave(Config.SaveSlot);

		private void LoadCurrentSlotMenuItem_Click(object sender, EventArgs e)
			=> LoadstateCurrentSlot();

		private bool LoadstateCurrentSlot()
			=> LoadQuickSave(Config.SaveSlot);

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
			if (Game.IsNullInstance()) return;
			if (!Emulator.Attributes().Released)
			{
				var result = this.ModalMessageBox2(
					"Thanks for using BizHawk!  The emulation core you have selected "
						+ "is currently BETA-status.  We appreciate your help in testing BizHawk. "
						+ "You can record a movie on this core if you'd like to, but expect to "
						+ "encounter bugs and sync problems.  Continue?",
					"BizHawk");

				if (!result)
				{
					return;
				}
			}

			// Nag user to user a more accurate core, but let them continue anyway
			EnsureCoreIsAccurate();

			using var form = new RecordMovie(this, Config, Game, Emulator, MovieSession, FirmwareManager);
			this.ShowDialogWithTempMute(form);
		}

		private string CanProvideFirmware(FirmwareID id, string hash)
			=> FirmwareManager.Resolve(
				Config.PathEntries,
				Config.FirmwareUserSpecifications,
				FirmwareDatabase.FirmwareRecords.First(fr => fr.ID == id),
//				exactFile: hash, //TODO re-scan FW dir for this file, then try autopatching
				forbidScan: true)?.Hash;

		private void PlayMovieMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new PlayMovie(this, Config, Game, Emulator, MovieSession, CanProvideFirmware);
			this.ShowDialogWithTempMute(form);
		}

		private void StopMovieMenuItem_Click(object sender, EventArgs e)
		{
			StopMovie();
		}

		private void PlayFromBeginningMenuItem_Click(object sender, EventArgs e)
			=> _ = RestartMovie();

		private void ImportMovieMenuItem_Click(object sender, EventArgs e)
		{
			var result = this.ShowFileMultiOpenDialog(
				discardCWDChange: false,
				filter: MovieImport.AvailableImporters,
				initDir: Config.PathEntries.RomAbsolutePath(Emulator.SystemId));
			if (result is not null) foreach (var fn in result) ProcessMovieImport(fn, false);
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
				currentFile: filename,
				path: Config.PathEntries.MovieAbsolutePath(),
				MovieSession.Movie.GetFSFilterSet(),
				this);

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
			if (OSTailoredCode.IsUnixHost)
			{
				using MsgBox dialog = new("Most of these options will cause crashes on Linux.", "A/V instability warning", MessageBoxIcon.Warning);
				this.ShowDialogWithTempMute(dialog);
			}
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

		private void CaptureOSDMenuItem_Click(object sender, EventArgs e)
		{
			bool c = ((ToolStripMenuItem)sender).Checked;
			Config.AviCaptureOsd = c;
			if (c) // Logic to capture OSD w/o Lua does not currently exist, so disallow that.
				Config.AviCaptureLua = true;
		}

		private void CaptureLuaMenuItem_Click(object sender, EventArgs e)
		{
			bool c = ((ToolStripMenuItem)sender).Checked;
			Config.AviCaptureLua = c;
			if (!c) // Logic to capture OSD w/o Lua does not currently exist, so disallow that.
				Config.AviCaptureOsd = false;
		}

		private void ScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			TakeScreenshot();
		}

		private void ScreenshotAsMenuItem_Click(object sender, EventArgs e)
		{
			var (dir, file) = $"{ScreenshotPrefix()}.{DateTime.Now:yyyy-MM-dd HH.mm.ss}.png".SplitPathToDirAndFile();
			var result = this.ShowFileSaveDialog(
				filter: ScreenshotsFSFilterSet,
				initDir: dir,
				initFileName: file);
			if (result is not null) TakeScreenshot(result);
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

		private void ScheduleShutdown()
			=> _exitRequestPending = true;

		public void CloseEmulator(int? exitCode = null)
		{
			ScheduleShutdown();
			if (exitCode != null) _exitCode = exitCode.Value;
		}

		private void EmulationMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			PauseMenuItem.Checked = _didMenuPause ? _wasPaused : EmulatorPaused;

			SoftResetMenuItem.Enabled = Emulator.ControllerDefinition.BoolButtons.Contains("Reset")
				&& !MovieSession.Movie.IsPlaying();

			HardResetMenuItem.Enabled = Emulator.ControllerDefinition.BoolButtons.Contains("Power")
				&& !MovieSession.Movie.IsPlaying();

			PauseMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Pause"];
			RebootCoreMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Reboot Core"];
			SoftResetMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Soft Reset"];
			HardResetMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Hard Reset"];
		}

		private void PauseMenuItem_Click(object sender, EventArgs e)
		{
			if (Config.PauseWhenMenuActivated && sender == PauseMenuItem)
			{
				const string ERR_MSG = nameof(PauseMenuItem_Click) + " ran before " + nameof(MaybeUnpauseFromMenuClosed) + "?";
				Debug.Assert(EmulatorPaused == _wasPaused, ERR_MSG);
				// fall through
			}
			TogglePause();
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

			DisplayFPSMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Display FPS"];
			DisplayFrameCounterMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Frame Counter"];
			DisplayLagCounterMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Lag Counter"];
			DisplayInputMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Input Display"];
			SwitchToFullscreenMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Full Screen"];

			DisplayStatusBarMenuItem.Checked = Config.DispChromeStatusBarWindowed;
			DisplayLogWindowMenuItem.Checked = Tools.IsLoaded<LogWindow>();

			DisplayLagCounterMenuItem.Enabled = Emulator.CanPollInput();

			DisplayMessagesMenuItem.Checked = Config.DisplayMessages;
		}

		private void WindowSizeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			var windowScale = Config.GetWindowScaleFor(Emulator.SystemId);
			foreach (ToolStripMenuItem item in WindowSizeSubMenu.DropDownItems)
			{
				item.Checked = (int) item.Tag == windowScale;
			}
		}

		private void WindowSize_Click(object sender, EventArgs e)
		{
			Config.SetWindowScaleFor(Emulator.SystemId, (int) ((ToolStripMenuItem) sender).Tag);
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

			miUnthrottled.Checked = Config.Unthrottled;
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
			using var controller = new ControllerConfig(this, Emulator, Config);
			if (!this.ShowDialogWithTempMute(controller).IsOk()) return;
			AddOnScreenMessage("Controller settings saved");

			InitControls();
			InputManager.SyncControls(Emulator, MovieSession, Config);
		}

		private void HotkeysMenuItem_Click(object sender, EventArgs e)
		{
			using var hotkeyConfig = new HotkeyConfig(Config);
			if (!this.ShowDialogWithTempMute(hotkeyConfig).IsOk()) return;
			AddOnScreenMessage("Hotkey settings saved");

			InitControls();
			InputManager.SyncControls(Emulator, MovieSession, Config);
		}

		private void OpenFWConfigRomLoadFailed(RomLoader.RomErrorArgs args)
		{
			using FirmwaresConfig configForm = new(
				this,
				FirmwareManager,
				Config.FirmwareUserSpecifications,
				Config.PathEntries,
				retryLoadRom: true,
				reloadRomPath: args.RomPath);
			args.Retry = this.ShowDialogWithTempMute(configForm) is DialogResult.Retry;
		}

		private void FirmwaresMenuItem_Click(object sender, EventArgs e)
		{
			using var configForm = new FirmwaresConfig(this, FirmwareManager, Config.FirmwareUserSpecifications, Config.PathEntries);
			this.ShowDialogWithTempMute(configForm);
		}

		private void MessagesMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new MessageConfig(Config);
			if (this.ShowDialogWithTempMute(form).IsOk()) AddOnScreenMessage("Message settings saved");
		}

		private void PathsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new PathConfig(Config.PathEntries, Game.System, newPath => MovieSession.BackupDirectory = newPath);
			if (this.ShowDialogWithTempMute(form).IsOk()) AddOnScreenMessage("Path settings saved");
		}

		private void SoundMenuItem_Click(object sender, EventArgs e)
		{
			static IEnumerable<string> GetDeviceNamesCallback(ESoundOutputMethod outputMethod) => outputMethod switch
			{
				ESoundOutputMethod.XAudio2 => XAudio2SoundOutput.GetDeviceNames(),
				ESoundOutputMethod.OpenAL => OpenALSoundOutput.GetDeviceNames(),
				_ => Enumerable.Empty<string>()
			};
			var oldOutputMethod = Config.SoundOutputMethod;
			var oldDevice = Config.SoundDevice;
			using var form = new SoundConfig(this, Config, GetDeviceNamesCallback);
			if (!this.ShowDialogWithTempMute(form).IsOk()) return;

			AddOnScreenMessage("Sound settings saved");
			if (Config.SoundOutputMethod == oldOutputMethod && Config.SoundDevice == oldDevice)
			{
				Sound.StopSound();
			}
			else
			{
				Sound.Dispose();
				Sound = new Sound(Config, () => Emulator.VsyncRate());
			}
			Sound.StartSound();
			RewireSound();
		}

		private void AutofireMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new AutofireConfig(Config, InputManager.AutoFireController, InputManager.AutofireStickyXorAdapter);
			if (this.ShowDialogWithTempMute(form).IsOk()) AddOnScreenMessage("Autofire settings saved");
		}

		private void RewindOptionsMenuItem_Click(object sender, EventArgs e)
		{
			if (!Emulator.HasSavestates()) return;
			using RewindConfig form = new(
				Config,
				PlatformFrameRates.GetFrameRate(Emulator.SystemId, Emulator.HasRegions() && Emulator.AsRegionable().Region is DisplayType.PAL), // why isn't there a helper for this
				Emulator.AsStatable(),
				CreateRewinder,
				() => this.Rewinder);
			if (this.ShowDialogWithTempMute(form).IsOk()) AddOnScreenMessage("Rewind and State settings saved");
		}

		private void FileExtensionsMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new FileExtensionPreferences(Config.PreferredPlatformsForExtensions);
			if (this.ShowDialogWithTempMute(form).IsOk()) AddOnScreenMessage("Rom Extension Preferences changed");
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
			using var form = new EmuHawkOptions(Config, BumpAutoFlushSaveRamTimer);
			if (!this.ShowDialogWithTempMute(form).IsOk()) return;
			AddOnScreenMessage("Custom configurations saved.");
		}

		private void ProfilesMenuItem_Click(object sender, EventArgs e)
		{
			using var form = new ProfileConfig(Config, this);
			if (!this.ShowDialogWithTempMute(form).IsOk()) return;
			AddOnScreenMessage("Profile settings saved");

			// We hide the FirstBoot items since the user setup a Profile
			// Is it a bad thing to do this constantly?
			Config.FirstBoot = false;
			ProfileFirstBootLabel.Visible = false;
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
					_presentationPanel.Resized = true;
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
					_presentationPanel.Resized = true;
				}
			}

			ThrottleMessage();
		}

		private void VsyncThrottleMenuItem_Click(object sender, EventArgs e)
		{
			Config.VSyncThrottle ^= true;
			_presentationPanel.Resized = true;
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
				_presentationPanel.Resized = true;
			}

			VsyncMessage();
		}

		private void UnthrottledMenuItem_Click(object sender, EventArgs e)
			=> ToggleUnthrottled();

		private void ToggleUnthrottled()
		{
			Config.Unthrottled ^= true;
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
			var (dir, file) = _getConfigPath().SplitPathToDirAndFile();
			var result = this.ShowFileSaveDialog(
				filter: ConfigFileFSFilterSet,
				initDir: dir,
				initFileName: file);
			if (result is not null)
			{
				SaveConfig(result);
				AddOnScreenMessage("Copied settings");
			}
		}

		private void LoadConfigMenuItem_Click(object sender, EventArgs e)
		{
			LoadConfigFile(_getConfigPath());
		}

		private void LoadConfigFromMenuItem_Click(object sender, EventArgs e)
		{
			var (dir, file) = _getConfigPath().SplitPathToDirAndFile();
			var result = this.ShowFileOpenDialog(filter: ConfigFileFSFilterSet, initDir: dir!, initFileName: file!);
			if (result is not null) LoadConfigFile(result);
		}

		private void ToolsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ToolBoxMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["ToolBox"];
			RamWatchMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["RAM Watch"];
			RamSearchMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["RAM Search"];
			HexEditorMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Hex Editor"];
			LuaConsoleMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Lua Console"];
			CheatsMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Cheats"];
			TAStudioMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["TAStudio"];
			VirtualPadMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Virtual Pad"];
			TraceLoggerMenuItem.ShortcutKeyDisplayString = Config.HotkeyBindings["Trace Logger"];
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
			ExternalToolMenuItem.DropDownItems.AddRange(ExtToolManager.ToolStripItems.ToArray());
			if (ExternalToolMenuItem.DropDownItems.Count == 0)
			{
				ExternalToolMenuItem.DropDownItems.Add(new ToolStripMenuItemEx { Enabled = false, Text = "(none)" });
			}
			if (Config.TrustedExtTools.Count is 0) return;

			ExternalToolMenuItem.DropDownItems.Add(new ToolStripSeparatorEx());
			ToolStripMenuItemEx forgetTrustedItem = new() { Text = "Forget Trusted Tools" };
			forgetTrustedItem.Click += (_, _) =>
			{
				if (this.ModalMessageBox2(
					caption: "Forget trusted ext. tools?",
					text: "This will cause the warning about running third-party code to show again for all the ext. tools you've previously loaded.\n" +
						"(If a tool has been loaded this session, the warning may not appear until EmuHawk is restarted.)",
					useOKCancel: true))
				{
					Config.TrustedExtTools.Clear();
				}
			};
			ExternalToolMenuItem.DropDownItems.Add(forgetTrustedItem);
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
				ShowMessageBox(owner: null, "Current core does not support input polling. TAStudio can't be used.");
				return;
			}
			const int DONT_PROMPT_BEFORE_FRAME = 2 * 60 * 60; // 2 min @ 60 fps
			if (MovieSession.Movie.NotActive() && Emulator.Frame > DONT_PROMPT_BEFORE_FRAME // if playing casually (not recording) AND played for enough frames (prompting always would be annoying)...
				&& !this.ModalMessageBox2("This will reload the rom without saving. Launch TAStudio anyway?", "Confirmation")) // ...AND user responds "No" to "Open TAStudio?", then cancel
			{
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
			using var form = new BatchRun(this, Config, CreateCoreComm);
			this.ShowDialogWithTempMute(form);
		}

		private void StartRetroAchievementsMenuItem_Click(object sender, EventArgs e)
		{
			OpenRetroAchievements();
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
			MovieSettingsMenuItem.Enabled = Emulator is NES or SubNESHawk && MovieSession.Movie.NotActive();

			NesControllerSettingsMenuItem.Enabled = Tools.IsAvailable<NesControllerSettings>() && MovieSession.Movie.NotActive();

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

			string button;
			for (int i = 0; Emulator.ControllerDefinition.BoolButtons.Contains(button = $"FDS Insert {i}"); i++)
			{
				var name = $"Disk {i / 2 + 1} Side {(char)(i % 2 + 'A')}";
				FdsInsertDiskMenuAdd($"Insert {name}", button, $"FDS {name} inserted.");
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

		private DialogResult OpenNesHawkGraphicsSettingsDialog(ISettingsAdapter settable)
		{
			using NESGraphicsConfig form = new(Config, this, settable);
			return this.ShowDialogWithTempMute(form);
		}

		private DialogResult OpenQuickNesGraphicsSettingsDialog(ISettingsAdapter settable)
		{
			using QuickNesConfig form = new(Config, DialogController, settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void NesGraphicSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				NES => OpenNesHawkGraphicsSettingsDialog(GetSettingsAdapterForLoadedCore<NES>()),
				SubNESHawk => OpenNesHawkGraphicsSettingsDialog(GetSettingsAdapterForLoadedCore<SubNESHawk>()),
				QuickNES => OpenQuickNesGraphicsSettingsDialog(GetSettingsAdapterForLoadedCore<QuickNES>()),
				_ => DialogResult.None
			};
		}

		private void NesSoundChannelsMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<NESSoundConfig>();
		}

		private DialogResult OpenNesHawkVSSettingsDialog(ISettingsAdapter settable)
		{
			using NesVsSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void VsSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				NES { IsVS: true } => OpenNesHawkVSSettingsDialog(GetSettingsAdapterForLoadedCore<NES>()),
				SubNESHawk { IsVs: true } => OpenNesHawkVSSettingsDialog(GetSettingsAdapterForLoadedCore<SubNESHawk>()),
				_ => DialogResult.None
			};
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

		private DialogResult OpenNesHawkGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using NesControllerSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private DialogResult OpenQuickNesGamepadSettingsDialog(ISettingsAdapter settable)
			=> GenericCoreConfig.DoDialogFor(
				this,
				settable,
				CoreNames.QuickNes + " Controller Settings",
				isMovieActive: MovieSession.Movie.IsActive(),
				ignoreSettings: true);

		private void NesControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				NES => OpenNesHawkGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<NES>()),
				SubNESHawk => OpenNesHawkGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<SubNESHawk>()),
				QuickNES => OpenQuickNesGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<QuickNES>()),
				_ => DialogResult.None
			};
		}

		private DialogResult OpenNesHawkAdvancedSettingsDialog(ISettingsAdapter settable, bool hasMapperProperties)
		{
			using NESSyncSettingsForm form = new(this, settable, hasMapperProperties: hasMapperProperties);
			return this.ShowDialogWithTempMute(form);
		}

		private void MovieSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				NES nesHawk => OpenNesHawkAdvancedSettingsDialog(GetSettingsAdapterForLoadedCore<NES>(), nesHawk.HasMapperProperties),
				SubNESHawk subNESHawk => OpenNesHawkAdvancedSettingsDialog(GetSettingsAdapterForLoadedCore<SubNESHawk>(), subNESHawk.HasMapperProperties),
				_ => DialogResult.None
			};
		}

		private void BarcodeReaderMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<BarcodeEntry>();
		}

		private void Ti83KeypadMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<TI83KeyPad>();
		}

		private void Ti83LoadTIFileMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is not TI83 ti83) return;
			var result = this.ShowFileOpenDialog(
				discardCWDChange: true,
				filter: TI83ProgramFilesFSFilterSet,
				initDir: Config.PathEntries.RomAbsolutePath(Emulator.SystemId));
			if (result is null) return;
			try
			{
				ti83.LinkPort.SendFileToCalc(File.OpenRead(result), true);
			}
			catch (IOException ex)
			{
				var message =
					$"Invalid file format. Reason: {ex.Message} \nForce transfer? This may cause the calculator to crash.";
				if (this.ShowMessageBox3(owner: null, message, "Upload Failed", EMsgBoxIcon.Question) == true)
				{
					ti83.LinkPort.SendFileToCalc(File.OpenRead(result), false);
				}
			}
		}

		private DialogResult OpenTI83PaletteSettingsDialog(ISettingsAdapter settable)
		{
			using TI83PaletteConfig form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void Ti83PaletteMenuItem_Click(object sender, EventArgs e)
		{
			var result = Emulator switch
			{
				Emu83 => OpenTI83PaletteSettingsDialog(GetSettingsAdapterForLoadedCore<Emu83>()),
				TI83 => OpenTI83PaletteSettingsDialog(GetSettingsAdapterForLoadedCore<TI83>()),
				_ => DialogResult.None
			};
			if (result.IsOk()) AddOnScreenMessage("Palette settings saved");
		}

		private void A7800SubMenu_DropDownOpened(object sender, EventArgs e)
		{
			A7800ControllerSettingsMenuItem.Enabled
				= A7800FilterSettingsMenuItem.Enabled
				= MovieSession.Movie.NotActive();
		}

		private DialogResult OpenA7800HawkGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using A7800ControllerSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void A7800ControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				A7800Hawk => OpenA7800HawkGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<A7800Hawk>()),
				_ => DialogResult.None
			};
		}

		private DialogResult OpenA7800HawkFilterSettingsDialog(ISettingsAdapter settable)
		{
			using A7800FilterSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void A7800FilterSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				A7800Hawk => OpenA7800HawkFilterSettingsDialog(GetSettingsAdapterForLoadedCore<A7800Hawk>()),
				_ => DialogResult.None
			};
		}

		private DialogResult OpenGambatteSettingsDialog(ISettingsAdapter settable)
			=> GBPrefs.DoGBPrefsDialog(Config, this, Game, MovieSession, settable);

		private DialogResult OpenGBHawkSettingsDialog()
			=> OpenGenericCoreConfigFor<GBHawk>(CoreNames.GbHawk + " Settings");

		private DialogResult OpenSameBoySettingsDialog()
			=> OpenGenericCoreConfigFor<Sameboy>(CoreNames.Sameboy + " Settings");

		private DialogResult OpenSubGBHawkSettingsDialog()
			=> OpenGenericCoreConfigFor<SubGBHawk>(CoreNames.SubGbHawk + " Settings");

		private void GbCoreSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				Gameboy => OpenGambatteSettingsDialog(GetSettingsAdapterForLoadedCore<Gameboy>()),
				GBHawk => OpenGBHawkSettingsDialog(),
				Sameboy => OpenSameBoySettingsDialog(),
				SubGBHawk => OpenSubGBHawkSettingsDialog(),
				_ => DialogResult.None
			};
		}

		private DialogResult OpenSameBoyPaletteSettingsDialog(ISettingsAdapter settable)
		{
			using SameBoyColorChooserForm form = new(Config, this, Game, settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void SameboyColorChooserMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				Sameboy => OpenSameBoyPaletteSettingsDialog(GetSettingsAdapterForLoadedCore<Sameboy>()),
				_ => DialogResult.None
			};
		}

		private void GbGpuViewerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<GbGpuView>();
		}

		private void GbPrinterViewerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<GBPrinterView>();
		}

		private void PsxSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			PSXControllerSettingsMenuItem.Enabled = MovieSession.Movie.NotActive();
		}

		private DialogResult OpenOctoshockGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using PSXControllerConfig form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void PsxControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				Octoshock => OpenOctoshockGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<Octoshock>()),
				_ => DialogResult.None
			};
		}

		private DialogResult OpenOctoshockSettingsDialog(ISettingsAdapter settable, OctoshockDll.eVidStandard vidStandard, Size vidSize)
			=> PSXOptions.DoSettingsDialog(Config, this, settable, vidStandard, vidSize);

		private void PsxOptionsMenuItem_Click(object sender, EventArgs e)
		{
			var result = Emulator switch
			{
				Octoshock octoshock => OpenOctoshockSettingsDialog(GetSettingsAdapterForLoadedCore<Octoshock>(), octoshock.SystemVidStandard, octoshock.CurrentVideoSize),
				_ => DialogResult.None
			};
			if (result.IsOk()) FrameBufferResized();
		}

		private void PsxDiscControlsMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<VirtualpadTool>().ScrollToPadSchema("Console");
		}

		private void PsxHashDiscsMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is not IRedumpDiscChecksumInfo psx) return;
			using PSXHashDiscs form = new() { _psx = psx };
			this.ShowDialogWithTempMute(form);
		}

		private void SnesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SNESControllerConfigurationMenuItem.Enabled = MovieSession.Movie.NotActive();
		}

		private DialogResult OpenOldBSNESGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using SNESControllerSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private DialogResult OpenBSNESGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using BSNESControllerSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void SNESControllerConfigurationMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				LibsnesCore => OpenOldBSNESGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<LibsnesCore>()),
				BsnesCore => OpenBSNESGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<BsnesCore>()),
				SubBsnesCore => OpenBSNESGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<SubBsnesCore>()),
				_ => DialogResult.None
			};
		}

		private void SnesGfxDebuggerMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<SNESGraphicsDebugger>();
		}

		private DialogResult OpenOldBSNESSettingsDialog(ISettingsAdapter settable)
			=> SNESOptions.DoSettingsDialog(this, settable);

		private DialogResult OpenBSNESSettingsDialog(ISettingsAdapter settable)
			=> BSNESOptions.DoSettingsDialog(this, settable);

		private void SnesOptionsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				LibsnesCore => OpenOldBSNESSettingsDialog(GetSettingsAdapterForLoadedCore<LibsnesCore>()),
				BsnesCore => OpenBSNESSettingsDialog(GetSettingsAdapterForLoadedCore<BsnesCore>()),
				SubBsnesCore => OpenBSNESSettingsDialog(GetSettingsAdapterForLoadedCore<SubBsnesCore>()),
				_ => DialogResult.None
			};
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

		private void ColecoHawkSetSkipBIOSIntro(bool newValue, ISettingsAdapter settable)
		{
			var ss = (ColecoVision.ColecoSyncSettings) settable.GetSyncSettings();
			ss.SkipBiosIntro = newValue;
			settable.PutCoreSyncSettings(ss);
		}

		private void ColecoSkipBiosMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is ColecoVision) ColecoHawkSetSkipBIOSIntro(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterForLoadedCore<ColecoVision>());
		}

		private void ColecoHawkSetSuperGameModule(bool newValue, ISettingsAdapter settable)
		{
			var ss = (ColecoVision.ColecoSyncSettings) settable.GetSyncSettings();
			ss.UseSGM = newValue;
			settable.PutCoreSyncSettings(ss);
		}

		private void ColecoUseSGMMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is ColecoVision) ColecoHawkSetSuperGameModule(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterForLoadedCore<ColecoVision>());
		}

		private DialogResult OpenColecoHawkGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using ColecoControllerSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void ColecoControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				ColecoVision => OpenColecoHawkGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<ColecoVision>()),
				_ => DialogResult.None
			};
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

		private DialogResult OpenMupen64PlusGraphicsSettingsDialog(ISettingsAdapter settable)
		{
			using N64VideoPluginConfig form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void N64PluginSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (OpenMupen64PlusGraphicsSettingsDialog(GetSettingsAdapterFor<N64>()).IsOk()
				&& Emulator is not N64) // If it's loaded, the reboot required message will appear
			{
				AddOnScreenMessage("Plugin settings saved");
			}
		}

		private DialogResult OpenMupen64PlusGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using N64ControllersSetup form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void N64ControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				N64 => OpenMupen64PlusGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<N64>()),
				_ => DialogResult.None
			};
		}

		private void N64CircularAnalogRangeMenuItem_Click(object sender, EventArgs e)
		{
			Config.N64UseCircularAnalogConstraint ^= true;
		}

		private static void Mupen64PlusSetMupenStyleLag(bool newValue, ISettingsAdapter settable)
		{
			var s = (N64Settings) settable.GetSettings();
			s.UseMupenStyleLag = newValue;
			settable.PutCoreSettings(s);
		}

		private void MupenStyleLagMenuItem_Click(object sender, EventArgs e)
			=> Mupen64PlusSetMupenStyleLag(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterForLoadedCore<N64>());

		private void Mupen64PlusSetUseExpansionSlot(bool newValue, ISettingsAdapter settable)
		{
			var ss = (N64SyncSettings) settable.GetSyncSettings();
			ss.DisableExpansionSlot = !newValue;
			settable.PutCoreSyncSettings(ss);
		}

		private void N64ExpansionSlotMenuItem_Click(object sender, EventArgs e)
		{
			if (Emulator is N64)
			{
				Mupen64PlusSetUseExpansionSlot(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterForLoadedCore<N64>());
				FlagNeedsReboot();
			}
		}

		private void Ares64SubMenu_DropDownOpened(object sender, EventArgs e)
			=> Ares64CircularAnalogRangeMenuItem.Checked = Config.N64UseCircularAnalogConstraint;

		private void Ares64SettingsMenuItem_Click(object sender, EventArgs e)
			=> OpenGenericCoreConfigFor<Ares64>(CoreNames.Ares64 + " Settings");

		private DialogResult OpenGambatteLinkSettingsDialog(ISettingsAdapter settable)
			=> GBLPrefs.DoGBLPrefsDialog(Config, this, Game, MovieSession, settable);

		private void GblSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				GambatteLink => OpenGambatteLinkSettingsDialog(GetSettingsAdapterForLoadedCore<GambatteLink>()),
				_ => DialogResult.None
			};
		}

		private DialogResult OpenGenericCoreConfigFor<T>(string title)
			where T : IEmulator
			=> GenericCoreConfig.DoDialogFor(this, GetSettingsAdapterFor<T>(), title, isMovieActive: MovieSession.Movie.IsActive());

		private void OpenGenericCoreConfig()
			=> GenericCoreConfig.DoDialog(Emulator, this, isMovieActive: MovieSession.Movie.IsActive());

		private void GenericCoreSettingsMenuItem_Click(object sender, EventArgs e)
			=> OpenGenericCoreConfig();

		private DialogResult OpenVirtuSettingsDialog()
			=> OpenGenericCoreConfigFor<AppleII>(CoreNames.Virtu + " Settings");

		private void AppleIISettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				AppleII => OpenVirtuSettingsDialog(),
				_ => DialogResult.None
			};
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

		private DialogResult OpenC64HawkSettingsDialog()
			=> OpenGenericCoreConfigFor<C64>(CoreNames.C64Hawk + " Settings");

		private void C64SettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				C64 => OpenC64HawkSettingsDialog(),
				_ => DialogResult.None
			};
		}

		private void IntVSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			IntVControllerSettingsMenuItem.Enabled = MovieSession.Movie.NotActive();
		}

		private DialogResult OpenIntelliHawkGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using IntvControllerSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void IntVControllerSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				Intellivision => OpenIntelliHawkGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<Intellivision>()),
				_ => DialogResult.None
			};
		}

		private DialogResult OpenZXHawkGamepadSettingsDialog(ISettingsAdapter settable)
		{
			using ZxSpectrumJoystickSettings form = new(this, settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void ZXSpectrumControllerConfigurationMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				ZXSpectrum => OpenZXHawkGamepadSettingsDialog(GetSettingsAdapterForLoadedCore<ZXSpectrum>()),
				_ => DialogResult.None
			};
		}

		private DialogResult OpenZXHawkSyncSettingsDialog(ISettingsAdapter settable)
		{
			using ZxSpectrumCoreEmulationSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void ZXSpectrumCoreEmulationSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				ZXSpectrum => OpenZXHawkSyncSettingsDialog(GetSettingsAdapterForLoadedCore<ZXSpectrum>()),
				_ => DialogResult.None
			};
		}

		private DialogResult OpenZXHawkSettingsDialog(ISettingsAdapter settable)
		{
			using ZxSpectrumNonSyncSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void ZXSpectrumNonSyncSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				ZXSpectrum => OpenZXHawkSettingsDialog(GetSettingsAdapterForLoadedCore<ZXSpectrum>()),
				_ => DialogResult.None
			};
		}

		private DialogResult OpenZXHawkAudioSettingsDialog(ISettingsAdapter settable)
		{
			using ZxSpectrumAudioSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void ZXSpectrumAudioSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				ZXSpectrum => OpenZXHawkAudioSettingsDialog(GetSettingsAdapterForLoadedCore<ZXSpectrum>()),
				_ => DialogResult.None
			};
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

			List<ToolStripItem> items = new();

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

			List<ToolStripItem> items = new();

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
			try
			{
				var result = this.ShowFileSaveDialog(
					discardCWDChange: true,
					fileExt: "szx",
//					SupportMultiDottedExtensions = true, // I think this should be enabled globally if we're going to do it --yoshi
					filter: ZXStateFilesFSFilterSet,
					initDir: Config.PathEntries.ToolsAbsolutePath());
				if (result is not null)
				{
					var speccy = (ZXSpectrum)Emulator;
					var snap = speccy.GetSZXSnapshot();
					File.WriteAllBytes(result, snap);
				}
			}
			catch (Exception)
			{
			}
		}

		private DialogResult OpenCPCHawkSyncSettingsDialog(ISettingsAdapter settable)
		{
			using AmstradCpcCoreEmulationSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void AmstradCpcCoreEmulationSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				AmstradCPC => OpenCPCHawkSyncSettingsDialog(GetSettingsAdapterForLoadedCore<AmstradCPC>()),
				_ => DialogResult.None
			};
		}

		private DialogResult OpenCPCHawkAudioSettingsDialog(ISettingsAdapter settable)
		{
			using AmstradCpcAudioSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void AmstradCpcAudioSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				AmstradCPC => OpenCPCHawkAudioSettingsDialog(GetSettingsAdapterForLoadedCore<AmstradCPC>()),
				_ => DialogResult.None
			};
		}

		private void AmstradCpcMediaMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (Emulator is AmstradCPC cpc)
			{
				AmstradCPCTapesSubMenu.Enabled = cpc._tapeInfo.Count > 0;
				AmstradCPCDisksSubMenu.Enabled = cpc._diskInfo.Count > 0;
			}
		}

		private void AmstradCpcTapesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AmstradCPCTapesSubMenu.DropDownItems.Clear();

			List<ToolStripItem> items = new();

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

			List<ToolStripItem> items = new();

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

		private DialogResult OpenCPCHawkSettingsDialog(ISettingsAdapter settable)
		{
			using AmstradCpcNonSyncSettings form = new(settable);
			return this.ShowDialogWithTempMute(form);
		}

		private void AmstradCpcNonSyncSettingsMenuItem_Click(object sender, EventArgs e)
		{
			_ = Emulator switch
			{
				AmstradCPC => OpenCPCHawkSettingsDialog(GetSettingsAdapterForLoadedCore<AmstradCPC>()),
				_ => DialogResult.None
			};
		}

		private void HelpSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			FeaturesMenuItem.Visible = VersionInfo.DeveloperBuild;
		}

		private void OnlineHelpMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("https://tasvideos.org/BizHawk");
		}

		private void ForumsMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("https://tasvideos.org/Forum/Subforum/64");
		}

		private void FeaturesMenuItem_Click(object sender, EventArgs e)
		{
			Tools.Load<CoreFeatureAnalysis>();
		}

		private void AboutMenuItem_Click(object sender, EventArgs e)
		{
			using BizBox form = new(() => Sound.PlayWavFile(Properties.Resources.GetNotHawkCallSFX(), atten: 1.0f));
			this.ShowDialogWithTempMute(form);
		}

		private void MainFormContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			MaybePauseFromMenuOpened();

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

			var file = new FileInfo($"{SaveStatePrefix()}.QuickSave{Config.SaveSlot % 10}.State.bak");

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
			=> MaybeUnpauseFromMenuClosed();

		private void DisplayConfigMenuItem_Click(object sender, EventArgs e)
		{
			using DisplayConfig window = new(Config, DialogController, GL);
			if (this.ShowDialogWithTempMute(window).IsOk())
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
			=> LoadMostRecentROM();

		private void LoadMostRecentROM()
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
			if (MovieSession.Movie.NotActive()) return;
			using EditSubtitlesForm form = new(this, MovieSession.Movie, Config.PathEntries, readOnly: MovieSession.ReadOnly);
			this.ShowDialogWithTempMute(form);
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
			if (!this.ShowDialogWithTempMute(subForm).IsOk()) return;

			if (index >= 0) MovieSession.Movie.Subtitles.RemoveAt(index);
			MovieSession.Movie.Subtitles.Add(subForm.Sub);
		}

		private void ViewCommentsContextMenuItem_Click(object sender, EventArgs e)
		{
			if (MovieSession.Movie.NotActive()) return;
			using EditCommentsForm form = new(MovieSession.Movie, MovieSession.ReadOnly);
			this.ShowDialogWithTempMute(form);
		}

		private void UndoSavestateContextMenuItem_Click(object sender, EventArgs e)
		{
			_stateSlots.SwapBackupSavestate(MovieSession.Movie, $"{SaveStatePrefix()}.QuickSave{Config.SaveSlot % 10}.State", Config.SaveSlot);
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

		private readonly ScreenshotForm _screenshotTooltip = new();

		private void SlotStatusButtons_MouseEnter(object/*?*/ sender, EventArgs e)
		{
			var slot = 10;
			if (sender == Slot1StatusButton) slot = 1;
			else if (sender == Slot2StatusButton) slot = 2;
			else if (sender == Slot3StatusButton) slot = 3;
			else if (sender == Slot4StatusButton) slot = 4;
			else if (sender == Slot5StatusButton) slot = 5;
			else if (sender == Slot6StatusButton) slot = 6;
			else if (sender == Slot7StatusButton) slot = 7;
			else if (sender == Slot8StatusButton) slot = 8;
			else if (sender == Slot9StatusButton) slot = 9;
			//TODO just put the slot number in Control.Tag already
			if (!(HasSlot(slot) && ReadScreenshotFromSavestate(slot: slot) is {} bb))
			{
				_screenshotTooltip.FadeOut();
				return;
			}
			var width = bb.Width;
			var height = bb.Height;
			var location = PointToScreen(MainStatusBar.Location);
			location.Offset(((e as MouseEventArgs)?.X ?? 50) - width/2, -height);
			_screenshotTooltip.UpdateValues(
				bb,
				captionText: string.Empty,
				location,
				width: width,
				height: height,
				Graphics.FromHwnd(Handle).MeasureString);
			_screenshotTooltip.FadeIn();
		}

		private void SlotStatusButtons_MouseLeave(object/*?*/ sender, EventArgs e)
			=> _screenshotTooltip.FadeOut();

		private void SlotStatusButtons_MouseUp(object sender, MouseEventArgs e)
		{
			var slot = 10;
			if (sender == Slot1StatusButton) slot = 1;
			if (sender == Slot2StatusButton) slot = 2;
			if (sender == Slot3StatusButton) slot = 3;
			if (sender == Slot4StatusButton) slot = 4;
			if (sender == Slot5StatusButton) slot = 5;
			if (sender == Slot6StatusButton) slot = 6;
			if (sender == Slot7StatusButton) slot = 7;
			if (sender == Slot8StatusButton) slot = 8;
			if (sender == Slot9StatusButton) slot = 9;
			if (sender == Slot0StatusButton) slot = 10;

			if (e.Button is MouseButtons.Right) SaveQuickSave(slot);
			else if (e.Button is MouseButtons.Left && HasSlot(slot)) _ = LoadQuickSave(slot);
		}

		private void KeyPriorityStatusLabel_Click(object sender, EventArgs e)
		{
			Config.InputHotkeyOverrideOptions = Config.InputHotkeyOverrideOptions switch
			{
				1 => 2,
				2 => Config.NoMixedInputHokeyOverride ? 1 : 0,
				_ => 1,
			};
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
			using var profileForm = new ProfileConfig(Config, this);
			this.ShowDialogWithTempMute(profileForm);
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
			var result = this.ModalMessageBox3(
				$"Version {Config.UpdateLatestVersion} is now available. Would you like to open the BizHawk homepage?\r\n\r\nClick \"No\" to hide the update notification for this version.",
				"New Version Available",
				EMsgBoxIcon.Question);
			Sound.StartSound();

			if (result == true)
			{
				System.Threading.ThreadPool.QueueUserWorkItem(s =>
				{
					using (System.Diagnostics.Process.Start(VersionInfo.HomePage))
					{
					}
				});
			}
			else if (result == false)
			{
				UpdateChecker.GlobalConfig = Config;
				UpdateChecker.IgnoreNewVersion();
				UpdateChecker.BeginCheck(skipCheck: true); // Trigger event to hide new version notification
			}
		}

		private void MainForm_Activated(object sender, EventArgs e)
		{
			if (!Config.RunInBackground) MaybeUnpauseFromMenuClosed();
		}

		private void MainForm_Deactivate(object sender, EventArgs e)
		{
			if (!Config.RunInBackground) MaybePauseFromMenuOpened();
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
			_presentationPanel.Resized = true;
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			if (Config.RecentWatches.AutoLoad)
			{
				Tools.LoadRamWatch(!Config.DisplayRamWatch);
			}

			if (Config.Cheats.Recent.AutoLoad)
			{
				Tools.Load<Cheats>();
			}

			Tools.AutoLoad();
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
			MaybePauseFromMenuOpened();
		}

		public void MaybePauseFromMenuOpened()
		{
			if (!Config.PauseWhenMenuActivated) return;
			_wasPaused = EmulatorPaused;
			PauseEmulator();
			_didMenuPause = true; // overwrites value set during PauseEmulator call
		}

		private void MainformMenu_MenuDeactivate(object sender, EventArgs e) => MaybeUnpauseFromMenuClosed();

		public void MaybeUnpauseFromMenuClosed()
		{
			if (_wasPaused || !Config.PauseWhenMenuActivated) return;
			UnpauseEmulator();
		}

		private static void FormDragEnter(object sender, DragEventArgs e)
		{
			e.Set(DragDropEffects.Copy);
		}

		private void FormDragDrop(object sender, DragEventArgs e)
			=> PathsFromDragDrop = (string[]) e.Data.GetData(DataFormats.FileDrop);

		private enum VSystemCategory : int
		{
			Consoles = 0,
			Handhelds = 1,
			PCs = 2,
			Other = 3,
		}

		private IReadOnlyCollection<ToolStripItem> CreateCoreSettingsSubmenus(bool includeDupes = false)
		{
			static ToolStripMenuItemEx CreateSettingsItem(string text, EventHandler onClick)
			{
				ToolStripMenuItemEx menuItem = new() { Text = text };
				menuItem.Click += onClick;
				return menuItem;
			}
			ToolStripMenuItemEx CreateGenericCoreConfigItem<T>(string coreName)
				where T : IEmulator
				=> CreateSettingsItem("Settings...", (_, _) => OpenGenericCoreConfigFor<T>($"{coreName} Settings"));
			ToolStripMenuItemEx CreateGenericNymaCoreConfigItem<T>(string coreName, Func<CoreComm, NymaCore.NymaSettingsInfo> getCachedSettingsInfo)
				where T : NymaCore
				=> CreateSettingsItem(
					"Settings...",
					(_, _) => GenericCoreConfig.DoNymaDialogFor(
						this,
						GetSettingsAdapterFor<T>(),
						$"{coreName} Settings",
						getCachedSettingsInfo(CreateCoreComm()),
						isMovieActive: MovieSession.Movie.IsActive()));
			ToolStripMenuItemEx CreateCoreSubmenu(VSystemCategory cat, string coreName, params ToolStripItem[] items)
			{
				ToolStripMenuItemEx submenu = new() { Tag = cat, Text = coreName };
				submenu.DropDownItems.AddRange(items);
				return submenu;
			}

			List<ToolStripItem> items = new();

			// A7800Hawk
			var a7800HawkGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenA7800HawkGamepadSettingsDialog(GetSettingsAdapterFor<A7800Hawk>()));
			var a7800HawkFilterSettingsItem = CreateSettingsItem("Filter Settings...", (_, _) => OpenA7800HawkFilterSettingsDialog(GetSettingsAdapterFor<A7800Hawk>()));
			var a7800HawkSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.A7800Hawk, a7800HawkGamepadSettingsItem, a7800HawkFilterSettingsItem);
			a7800HawkSubmenu.DropDownOpened += (_, _) => a7800HawkGamepadSettingsItem.Enabled = a7800HawkFilterSettingsItem.Enabled = MovieSession.Movie.NotActive() || Emulator is not A7800Hawk;
			items.Add(a7800HawkSubmenu);

			// Ares64
			var ares64AnalogConstraintItem = CreateSettingsItem("Circular Analog Range", N64CircularAnalogRangeMenuItem_Click);
			var ares64Submenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Ares64, CreateGenericCoreConfigItem<Ares64>(CoreNames.Ares64));
			ares64Submenu.DropDownOpened += (_, _) => ares64AnalogConstraintItem.Checked = Config.N64UseCircularAnalogConstraint;
			items.Add(ares64Submenu);

			// Atari2600Hawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Atari2600Hawk, CreateGenericCoreConfigItem<Atari2600>(CoreNames.Atari2600Hawk)));

			// BSNES
			var oldBSNESGamepadSettingsItem = CreateSettingsItem("Controller Configuration...", (_, _) => OpenOldBSNESGamepadSettingsDialog(GetSettingsAdapterFor<LibsnesCore>()));
			var oldBSNESSettingsItem = CreateSettingsItem("Options...", (_, _) => OpenOldBSNESSettingsDialog(GetSettingsAdapterFor<LibsnesCore>()));
			var oldBSNESSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Bsnes, oldBSNESGamepadSettingsItem, oldBSNESSettingsItem);
			oldBSNESSubmenu.DropDownOpened += (_, _) => oldBSNESGamepadSettingsItem.Enabled = MovieSession.Movie.NotActive() || Emulator is not LibsnesCore;
			items.Add(oldBSNESSubmenu);

			// BSNESv115+
			var bsnesGamepadSettingsItem = CreateSettingsItem("Controller Configuration...", (_, _) => OpenBSNESGamepadSettingsDialog(GetSettingsAdapterFor<BsnesCore>()));
			var bsnesSettingsItem = CreateSettingsItem("Options...", (_, _) => OpenBSNESSettingsDialog(GetSettingsAdapterFor<BsnesCore>()));
			var bsnesSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Bsnes115, bsnesGamepadSettingsItem, bsnesSettingsItem);
			bsnesSubmenu.DropDownOpened += (_, _) => bsnesGamepadSettingsItem.Enabled = MovieSession.Movie.NotActive() || Emulator is not BsnesCore;
			items.Add(bsnesSubmenu);

			// SubBSNESv115+
			var subBsnesGamepadSettingsItem = CreateSettingsItem("Controller Configuration...", (_, _) => OpenBSNESGamepadSettingsDialog(GetSettingsAdapterFor<SubBsnesCore>()));
			var subBsnesSettingsItem = CreateSettingsItem("Options...", (_, _) => OpenBSNESSettingsDialog(GetSettingsAdapterFor<SubBsnesCore>()));
			var subBsnesSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.SubBsnes115, subBsnesGamepadSettingsItem, subBsnesSettingsItem);
			subBsnesSubmenu.DropDownOpened += (_, _) => subBsnesGamepadSettingsItem.Enabled = MovieSession.Movie.NotActive() || Emulator is not SubBsnesCore;
			items.Add(subBsnesSubmenu);

			// C64Hawk
			items.Add(CreateCoreSubmenu(VSystemCategory.PCs, CoreNames.C64Hawk, CreateSettingsItem("Settings...", (_, _) => OpenC64HawkSettingsDialog())));

			// ChannelFHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.ChannelFHawk, CreateGenericCoreConfigItem<ChannelF>(CoreNames.ChannelFHawk)));

			// Encore
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.Encore, CreateGenericCoreConfigItem<Encore>(CoreNames.Encore)));

			// ColecoHawk
			var colecoHawkGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenColecoHawkGamepadSettingsDialog(GetSettingsAdapterFor<ColecoVision>()));
			var colecoHawkSkipBIOSItem = CreateSettingsItem("Skip BIOS intro (When Applicable)", (sender, _) => ColecoHawkSetSkipBIOSIntro(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterFor<ColecoVision>()));
			var colecoHawkUseSGMItem = CreateSettingsItem("Use the Super Game Module", (sender, _) => ColecoHawkSetSuperGameModule(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterFor<ColecoVision>()));
			var colecoHawkSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.ColecoHawk, colecoHawkGamepadSettingsItem, colecoHawkSkipBIOSItem, colecoHawkUseSGMItem);
			colecoHawkSubmenu.DropDownOpened += (_, _) =>
			{
				var ss = (ColecoVision.ColecoSyncSettings) GetSettingsAdapterFor<ColecoVision>().GetSyncSettings();
				colecoHawkGamepadSettingsItem.Enabled = MovieSession.Movie.NotActive() || Emulator is not ColecoVision;
				colecoHawkSkipBIOSItem.Checked = ss.SkipBiosIntro;
				colecoHawkUseSGMItem.Checked = ss.UseSGM;
			};
			items.Add(colecoHawkSubmenu);

			// CPCHawk
			items.Add(CreateCoreSubmenu(
				VSystemCategory.PCs,
				CoreNames.CPCHawk,
				CreateSettingsItem("Core Emulation Settings...", (_, _) => OpenCPCHawkSyncSettingsDialog(GetSettingsAdapterFor<AmstradCPC>())),
				CreateSettingsItem("Audio Settings...", (_, _) => OpenCPCHawkAudioSettingsDialog(GetSettingsAdapterFor<AmstradCPC>())),
				CreateSettingsItem("Non-Sync Settings...", (_, _) => OpenCPCHawkSettingsDialog(GetSettingsAdapterFor<AmstradCPC>()))));

			// Cygne
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.Cygne, CreateGenericCoreConfigItem<WonderSwan>(CoreNames.Cygne)));

			// Emu83
			items.Add(CreateCoreSubmenu(VSystemCategory.Other, CoreNames.Emu83, CreateSettingsItem("Palette...", (_, _) => OpenTI83PaletteSettingsDialog(GetSettingsAdapterFor<Emu83>()))));

			// Faust
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Faust, CreateGenericNymaCoreConfigItem<Faust>(CoreNames.Faust, Faust.CachedSettingsInfo)));

			// Gambatte
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.Gambatte, CreateSettingsItem("Settings...", (_, _) => OpenGambatteSettingsDialog(GetSettingsAdapterFor<Gameboy>()))));
			if (includeDupes) items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Gambatte, CreateSettingsItem("Settings...", (_, _) => OpenGambatteSettingsDialog(GetSettingsAdapterFor<Gameboy>()))));

			// GambatteLink
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.GambatteLink, CreateSettingsItem("Settings...", (_, _) => OpenGambatteLinkSettingsDialog(GetSettingsAdapterFor<GambatteLink>()))));

			// GBHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.GbHawk, CreateSettingsItem("Settings...", (_, _) => OpenGBHawkSettingsDialog())));

			// GBHawkLink
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.GBHawkLink, CreateGenericCoreConfigItem<GBHawkLink>(CoreNames.GBHawkLink)));

			// GBHawkLink3x
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.GBHawkLink3x, CreateGenericCoreConfigItem<GBHawkLink3x>(CoreNames.GBHawkLink3x)));

			// GBHawkLink4x
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.GBHawkLink4x, CreateGenericCoreConfigItem<GBHawkLink4x>(CoreNames.GBHawkLink4x)));

			// GGHawkLink
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.GGHawkLink, CreateGenericCoreConfigItem<GGHawkLink>(CoreNames.GGHawkLink)));

			// Genplus-gx
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Gpgx, CreateGenericCoreConfigItem<GPGX>(CoreNames.Gpgx)));

			// Handy
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.Handy, CreateGenericCoreConfigItem<Lynx>(CoreNames.Handy))); // as Handy doesn't implement `ISettable<,>`, this opens an empty `GenericCoreConfig`, which is dumb, but matches the existing behaviour

			// HyperNyma
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.HyperNyma, CreateGenericNymaCoreConfigItem<HyperNyma>(CoreNames.HyperNyma, HyperNyma.CachedSettingsInfo)));

			// IntelliHawk
			var intelliHawkGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenIntelliHawkGamepadSettingsDialog(GetSettingsAdapterFor<Intellivision>()));
			var intelliHawkSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.IntelliHawk, intelliHawkGamepadSettingsItem);
			intelliHawkSubmenu.DropDownOpened += (_, _) => intelliHawkGamepadSettingsItem.Enabled = MovieSession.Movie.NotActive() || Emulator is not Intellivision;
			items.Add(intelliHawkSubmenu);

			// Libretro
			items.Add(CreateCoreSubmenu(
				VSystemCategory.Other,
				CoreNames.Libretro,
				CreateGenericCoreConfigItem<LibretroHost>(CoreNames.Libretro))); // as Libretro doesn't implement `ISettable<,>`, this opens an empty `GenericCoreConfig`, which is dumb, but matches the existing behaviour

			// MAME
			var mameSettingsItem = CreateSettingsItem("Settings...", (_, _) => OpenGenericCoreConfig());
			var mameSubmenu = CreateCoreSubmenu(VSystemCategory.Other, CoreNames.MAME, mameSettingsItem);
			mameSubmenu.DropDownOpened += (_, _) => mameSettingsItem.Enabled = Emulator is MAME;
			items.Add(mameSubmenu);

			// melonDS
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.MelonDS, CreateGenericCoreConfigItem<NDS>(CoreNames.MelonDS)));

			// mGBA
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.Mgba, CreateGenericCoreConfigItem<MGBAHawk>(CoreNames.Mgba)));

			// MSXHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.PCs, CoreNames.MSXHawk, CreateGenericCoreConfigItem<MSX>(CoreNames.MSXHawk)));

			// Mupen64Plus
			var mupen64PlusGraphicsSettingsItem = CreateSettingsItem("Video Plugins...", N64PluginSettingsMenuItem_Click);
			var mupen64PlusGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenMupen64PlusGamepadSettingsDialog(GetSettingsAdapterFor<N64>()));
			var mupen64PlusAnalogConstraintItem = CreateSettingsItem("Circular Analog Range", N64CircularAnalogRangeMenuItem_Click);
			var mupen64PlusMupenStyleLagFramesItem = CreateSettingsItem("Mupen Style Lag Frames", (sender, _) => Mupen64PlusSetMupenStyleLag(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterFor<N64>()));
			var mupen64PlusUseExpansionSlotItem = CreateSettingsItem("Use Expansion Slot", (sender, _) => Mupen64PlusSetUseExpansionSlot(!((ToolStripMenuItem) sender).Checked, GetSettingsAdapterFor<N64>()));
			var mupen64PlusSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Mupen64Plus, mupen64PlusGraphicsSettingsItem, mupen64PlusGamepadSettingsItem, mupen64PlusAnalogConstraintItem, mupen64PlusMupenStyleLagFramesItem, mupen64PlusUseExpansionSlotItem);
			mupen64PlusSubmenu.DropDownOpened += (_, _) =>
			{
				var settable = GetSettingsAdapterFor<N64>();
				var s = (N64Settings) settable.GetSettings();
				var isMovieActive = MovieSession.Movie.IsActive();
				var mupen64Plus = Emulator as N64;
				var loadedCoreIsMupen64Plus = mupen64Plus is not null;
				mupen64PlusGraphicsSettingsItem.Enabled = !loadedCoreIsMupen64Plus || !isMovieActive;
				mupen64PlusGamepadSettingsItem.Enabled = !loadedCoreIsMupen64Plus || !isMovieActive;
				mupen64PlusAnalogConstraintItem.Checked = Config.N64UseCircularAnalogConstraint;
				mupen64PlusMupenStyleLagFramesItem.Checked = s.UseMupenStyleLag;
				if (loadedCoreIsMupen64Plus)
				{
					mupen64PlusUseExpansionSlotItem.Checked = mupen64Plus.UsingExpansionSlot;
					mupen64PlusUseExpansionSlotItem.Enabled = !mupen64Plus.IsOverridingUserExpansionSlotSetting;
				}
				else
				{
					mupen64PlusUseExpansionSlotItem.Checked = !((N64SyncSettings) settable.GetSyncSettings()).DisableExpansionSlot;
					mupen64PlusUseExpansionSlotItem.Enabled = true;
				}
			};
			items.Add(mupen64PlusSubmenu);

			// NeoPop
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.NeoPop, CreateGenericNymaCoreConfigItem<NeoGeoPort>(CoreNames.NeoPop, NeoGeoPort.CachedSettingsInfo)));

			// NesHawk
			var nesHawkGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenNesHawkGamepadSettingsDialog(GetSettingsAdapterFor<NES>()));
			var nesHawkVSSettingsItem = CreateSettingsItem("VS Settings...", (_, _) => OpenNesHawkVSSettingsDialog(GetSettingsAdapterFor<NES>()));
			var nesHawkAdvancedSettingsItem = CreateSettingsItem("Advanced Settings...", (_, _) => OpenNesHawkAdvancedSettingsDialog(GetSettingsAdapterFor<NES>(), Emulator is not NES nesHawk || nesHawk.HasMapperProperties));
			var nesHawkSubmenu = CreateCoreSubmenu(
				VSystemCategory.Consoles,
				CoreNames.NesHawk,
				nesHawkGamepadSettingsItem,
				CreateSettingsItem("Graphics Settings...", (_, _) => OpenNesHawkGraphicsSettingsDialog(GetSettingsAdapterFor<NES>())),
				nesHawkVSSettingsItem,
				nesHawkAdvancedSettingsItem);
			nesHawkSubmenu.DropDownOpened += (_, _) =>
			{
				var nesHawk = Emulator as NES;
				var canEditSyncSettings = nesHawk is null || MovieSession.Movie.NotActive();
				nesHawkGamepadSettingsItem.Enabled = canEditSyncSettings && Tools.IsAvailable<NesControllerSettings>();
				nesHawkVSSettingsItem.Enabled = nesHawk?.IsVS is null or true;
				nesHawkAdvancedSettingsItem.Enabled = canEditSyncSettings;
			};
			items.Add(nesHawkSubmenu);

			// Nymashock
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Nymashock, CreateGenericNymaCoreConfigItem<Nymashock>(CoreNames.Nymashock, Nymashock.CachedSettingsInfo)));

			// O2Hawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.O2Hawk, CreateGenericCoreConfigItem<O2Hawk>(CoreNames.O2Hawk)));

			// Octoshock
			var octoshockGamepadSettingsItem = CreateSettingsItem("Controller / Memcard Settings...", (_, _) => OpenOctoshockGamepadSettingsDialog(GetSettingsAdapterFor<Octoshock>()));
			var octoshockSettingsItem = CreateSettingsItem("Options...", PsxOptionsMenuItem_Click);
			// using init buffer sizes here (in practice, they don't matter here, but might as well)
			var octoshockNTSCSettingsItem = CreateSettingsItem("Options (as NTSC)...", (_, _) => OpenOctoshockSettingsDialog(GetSettingsAdapterFor<Octoshock>(), OctoshockDll.eVidStandard.NTSC, new(280, 240)));
			var octoshockPALSettingsItem = CreateSettingsItem("Options (as PAL)...", (_, _) => OpenOctoshockSettingsDialog(GetSettingsAdapterFor<Octoshock>(), OctoshockDll.eVidStandard.PAL, new(280, 288)));
			var octoshockSubmenu = CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Octoshock, octoshockGamepadSettingsItem, octoshockSettingsItem, octoshockNTSCSettingsItem, octoshockPALSettingsItem);
			octoshockSubmenu.DropDownOpened += (_, _) =>
			{
				var loadedCoreIsOctoshock = Emulator is Octoshock;
				octoshockGamepadSettingsItem.Enabled = !loadedCoreIsOctoshock || MovieSession.Movie.NotActive();
				octoshockSettingsItem.Visible = loadedCoreIsOctoshock;
				octoshockNTSCSettingsItem.Visible = octoshockPALSettingsItem.Visible = !loadedCoreIsOctoshock;
			};
			items.Add(octoshockSubmenu);

			// PCEHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.PceHawk, CreateGenericCoreConfigItem<PCEngine>(CoreNames.PceHawk)));

			// PicoDrive
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.PicoDrive, CreateGenericCoreConfigItem<PicoDrive>(CoreNames.PicoDrive)));

			// PUAE
			items.Add(CreateCoreSubmenu(VSystemCategory.PCs, CoreNames.PUAE, CreateGenericCoreConfigItem<PUAE>(CoreNames.PUAE)));

			// QuickNes
			var quickNesGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenQuickNesGamepadSettingsDialog(GetSettingsAdapterFor<QuickNES>()));
			var quickNesSubmenu = CreateCoreSubmenu(
				VSystemCategory.Consoles,
				CoreNames.QuickNes,
				quickNesGamepadSettingsItem,
				CreateSettingsItem("Graphics Settings...", (_, _) => OpenQuickNesGraphicsSettingsDialog(GetSettingsAdapterFor<QuickNES>())));
			quickNesSubmenu.DropDownOpened += (_, _) => quickNesGamepadSettingsItem.Enabled = (MovieSession.Movie.NotActive() || Emulator is not QuickNES) && Tools.IsAvailable<NesControllerSettings>();
			items.Add(quickNesSubmenu);

			// SameBoy
			items.Add(CreateCoreSubmenu(
				VSystemCategory.Handhelds,
				CoreNames.Sameboy,
				CreateSettingsItem("Settings...", (_, _) => OpenSameBoySettingsDialog()),
				CreateSettingsItem("Choose Custom Palette...", (_, _) => OpenSameBoyPaletteSettingsDialog(GetSettingsAdapterFor<Sameboy>()))));

			// Saturnus
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Saturnus, CreateGenericNymaCoreConfigItem<Saturnus>(CoreNames.Saturnus, Saturnus.CachedSettingsInfo)));

			// SMSHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.SMSHawk, CreateGenericCoreConfigItem<SMS>(CoreNames.SMSHawk)));
			if (includeDupes) items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.SMSHawk, CreateGenericCoreConfigItem<SMS>(CoreNames.SMSHawk)));

			// Snes9x
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Snes9X, CreateGenericCoreConfigItem<Snes9x>(CoreNames.Snes9X)));

			// SubGBHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Handhelds, CoreNames.SubGbHawk, CreateSettingsItem("Settings...", (_, _) => OpenSubGBHawkSettingsDialog())));

			// SubNESHawk
			var subNESHawkGamepadSettingsItem = CreateSettingsItem("Controller Settings...", (_, _) => OpenNesHawkGamepadSettingsDialog(GetSettingsAdapterFor<SubNESHawk>()));
			var subNESHawkVSSettingsItem = CreateSettingsItem("VS Settings...", (_, _) => OpenNesHawkVSSettingsDialog(GetSettingsAdapterFor<SubNESHawk>()));
			var subNESHawkAdvancedSettingsItem = CreateSettingsItem("Advanced Settings...", (_, _) => OpenNesHawkAdvancedSettingsDialog(GetSettingsAdapterFor<SubNESHawk>(), Emulator is not SubNESHawk subNESHawk || subNESHawk.HasMapperProperties));
			var subNESHawkSubmenu = CreateCoreSubmenu(
				VSystemCategory.Consoles,
				CoreNames.SubNesHawk,
				subNESHawkGamepadSettingsItem,
				CreateSettingsItem("Graphics Settings...", (_, _) => OpenNesHawkGraphicsSettingsDialog(GetSettingsAdapterFor<SubNESHawk>())),
				subNESHawkVSSettingsItem,
				subNESHawkAdvancedSettingsItem);
			subNESHawkSubmenu.DropDownOpened += (_, _) =>
			{
				var subNESHawk = Emulator as SubNESHawk;
				var canEditSyncSettings = subNESHawk is null || MovieSession.Movie.NotActive();
				subNESHawkGamepadSettingsItem.Enabled = canEditSyncSettings && Tools.IsAvailable<NesControllerSettings>();
				subNESHawkVSSettingsItem.Enabled = subNESHawk?.IsVs is null or true;
				subNESHawkAdvancedSettingsItem.Enabled = canEditSyncSettings;
			};
			items.Add(subNESHawkSubmenu);

			// TI83Hawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Other, CoreNames.TI83Hawk, CreateSettingsItem("Palette...", (_, _) => OpenTI83PaletteSettingsDialog(GetSettingsAdapterFor<TI83>()))));

			// TIC80
			items.Add(CreateCoreSubmenu(VSystemCategory.PCs, CoreNames.TIC80, CreateGenericCoreConfigItem<TIC80>(CoreNames.TIC80)));

			// T. S. T.
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.TST, CreateGenericNymaCoreConfigItem<Tst>(CoreNames.TST, Tst.CachedSettingsInfo)));

			// TurboNyma
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.TurboNyma, CreateGenericNymaCoreConfigItem<TurboNyma>(CoreNames.TurboNyma, TurboNyma.CachedSettingsInfo)));

			// uzem
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.Uzem, CreateGenericCoreConfigItem<Uzem>(CoreNames.Uzem))); // as uzem doesn't implement `ISettable<,>`, this opens an empty `GenericCoreConfig`, which is dumb, but matches the existing behaviour

			// VectrexHawk
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.VectrexHawk, CreateGenericCoreConfigItem<VectrexHawk>(CoreNames.VectrexHawk)));

			// Virtu
			items.Add(CreateCoreSubmenu(VSystemCategory.PCs, CoreNames.Virtu, CreateSettingsItem("Settings...", (_, _) => OpenVirtuSettingsDialog())));

			// Virtual Boyee
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.VirtualBoyee, CreateGenericNymaCoreConfigItem<VirtualBoyee>(CoreNames.VirtualBoyee, VirtualBoyee.CachedSettingsInfo)));

			// Virtual Jaguar
			items.Add(CreateCoreSubmenu(VSystemCategory.Consoles, CoreNames.VirtualJaguar, CreateGenericCoreConfigItem<VirtualJaguar>(CoreNames.VirtualJaguar)));

			// ZXHawk
			items.Add(CreateCoreSubmenu(
				VSystemCategory.PCs,
				CoreNames.ZXHawk,
				CreateSettingsItem("Core Emulation Settings...", (_, _) => OpenZXHawkSyncSettingsDialog(GetSettingsAdapterFor<ZXSpectrum>())),
				CreateSettingsItem("Joystick Configuration...", (_, _) => OpenZXHawkGamepadSettingsDialog(GetSettingsAdapterFor<ZXSpectrum>())),
				CreateSettingsItem("Audio Settings...", (_, _) => OpenZXHawkAudioSettingsDialog(GetSettingsAdapterFor<ZXSpectrum>())),
				CreateSettingsItem("Non-Sync Settings...", (_, _) => OpenZXHawkSettingsDialog(GetSettingsAdapterFor<ZXSpectrum>()))));

			return items;
		}
	}
}
