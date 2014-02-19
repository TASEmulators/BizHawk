using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Client.EmuHawk.config.NES;

namespace BizHawk.Client.EmuHawk
{
	partial class MainForm
	{
		#region File Menu

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			OpenRomMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Open ROM"].Bindings;
			CloseRomMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Close ROM"].Bindings;

			MovieSubMenu.Enabled =
					AVSubMenu.Enabled =
					ScreenshotSubMenu.Enabled =
					CloseRomMenuItem.Enabled =
					!(Global.Emulator is NullEmulator);
		}

		private void RecentRomMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			RecentRomSubMenu.DropDownItems.Clear();
			RecentRomSubMenu.DropDownItems.AddRange(
				ToolHelpers.GenerateRecentMenu(Global.Config.RecentRoms, LoadRomFromRecent)
			);
			RecentRomSubMenu.DropDownItems.Add(
				ToolHelpers.GenerateAutoLoadItem(Global.Config.RecentRoms)
			);
		}

		private void SaveStateSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveState0MenuItem.Font = new Font(
				SaveState0MenuItem.Font.FontFamily,
				SaveState0MenuItem.Font.Size,
				 _stateSlots.HasSlot(0) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState1MenuItem.Font = new Font(
				SaveState1MenuItem.Font.FontFamily,
				SaveState1MenuItem.Font.Size,
				_stateSlots.HasSlot(1) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState2MenuItem.Font = new Font(
				SaveState2MenuItem.Font.FontFamily,
				SaveState2MenuItem.Font.Size,
				_stateSlots.HasSlot(2) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState3MenuItem.Font = new Font(
				SaveState3MenuItem.Font.FontFamily,
				SaveState3MenuItem.Font.Size,
				_stateSlots.HasSlot(3) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState4MenuItem.Font = new Font(
				SaveState4MenuItem.Font.FontFamily,
				SaveState4MenuItem.Font.Size,
				_stateSlots.HasSlot(4) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState5MenuItem.Font = new Font(
				SaveState5MenuItem.Font.FontFamily,
				SaveState5MenuItem.Font.Size,
				_stateSlots.HasSlot(5) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState6MenuItem.Font = new Font(
				SaveState6MenuItem.Font.FontFamily,
				SaveState6MenuItem.Font.Size,
				_stateSlots.HasSlot(6) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState7MenuItem.Font = new Font(
				SaveState7MenuItem.Font.FontFamily,
				SaveState7MenuItem.Font.Size,
				_stateSlots.HasSlot(7) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState8MenuItem.Font = new Font(
				SaveState8MenuItem.Font.FontFamily,
				SaveState8MenuItem.Font.Size,
				_stateSlots.HasSlot(8) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState9MenuItem.Font = new Font(
				SaveState9MenuItem.Font.FontFamily,
				SaveState9MenuItem.Font.Size,
				_stateSlots.HasSlot(9) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

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

			SaveNamedStateMenuItem.Enabled =
				SaveState1MenuItem.Enabled =
				SaveState2MenuItem.Enabled =
				SaveState3MenuItem.Enabled =
				SaveState4MenuItem.Enabled =
				SaveState5MenuItem.Enabled =
				SaveState6MenuItem.Enabled =
				SaveState7MenuItem.Enabled =
				SaveState8MenuItem.Enabled =
				SaveState9MenuItem.Enabled =
				SaveState0MenuItem.Enabled =
				!(Global.Emulator is NullEmulator);
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

			LoadNamedStateMenuItem.Enabled = !(Global.Emulator is NullEmulator);
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

			SaveToCurrentSlotMenuItem.Enabled = LoadCurrentSlotMenuItem.Enabled = !(Global.Emulator is NullEmulator);

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

		private void MovieSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			FullMovieLoadstatesMenuItem.Enabled = !Global.MovieSession.MultiTrack.IsActive;
			StopMovieWithoutSavingMenuItem.Enabled = Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie.Changes;
			StopMovieMenuItem.Enabled
				= PlayFromBeginningMenuItem.Enabled
				= SaveMovieMenuItem.Enabled
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
			RecentMenuItem.DropDownItems.Clear();
			RecentMenuItem.DropDownItems.AddRange(
				ToolHelpers.GenerateRecentMenu(Global.Config.RecentMovies, LoadMoviesFromRecent)
			);
			RecentMenuItem.DropDownItems.Add(
				ToolHelpers.GenerateAutoLoadItem(Global.Config.RecentMovies)
			);
		}

		private void AVSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecordAVMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Record A/V"].Bindings;
			StopAVIMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Stop A/V"].Bindings;
			CaptureOSDMenuItem.Checked = Global.Config.AVI_CaptureOSD;

			if (_currAviWriter == null)
			{
				RecordAVMenuItem.Enabled = true;
				StopAVIMenuItem.Enabled = false;
			}
			else
			{
				RecordAVMenuItem.Enabled = false;
				StopAVIMenuItem.Enabled = true;
			}
		}

		private void ScreenshotSubMenu_DropDownOpening(object sender, EventArgs e)
		{
			ScreenshotCaptureOSDMenuItem1.Checked = Global.Config.Screenshot_CaptureOSD;
			ScreenshotMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Screenshot"].Bindings;
		}

		private void OpenRomMenuItem_Click(object sender, EventArgs e)
		{
			OpenRom();
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
			SaveQuickSave("QuickSave" + Global.Config.SaveSlot);
		}

		private void LoadCurrentSlotMenuItem_Click(object sender, EventArgs e)
		{
			LoadQuickSave("QuickSave" + Global.Config.SaveSlot);
		}

		private void ReadonlyMenuItem_Click(object sender, EventArgs e)
		{
			ToggleReadOnly();
		}

		private void RecordMovieMenuItem_Click(object sender, EventArgs e)
		{
			LoadRecordMovieDialog();
		}

		private void PlayMovieMenuItem_Click(object sender, EventArgs e)
		{
			LoadPlayMovieDialog();
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
			var ofd = new OpenFileDialog
			{
				InitialDirectory = PathManager.GetRomsPath(Global.Emulator.SystemId),
				Multiselect = true,
				Filter = FormatFilter(
					"Movie Files", "*.fm2;*.mc2;*.mcm;*.mmv;*.gmv;*.vbm;*.lsmv;*.fcm;*.fmv;*.vmv;*.nmv;*.smv;*.zmv;",
					"FCEUX", "*.fm2",
					"PCEjin/Mednafen", "*.mc2;*.mcm",
					"Dega", "*.mmv",
					"Gens", "*.gmv",
					"Visual Boy Advance", "*.vbm",
					"LSNES", "*.lsmv",
					"FCEU", "*.fcm",
					"Famtasia", "*.fmv",
					"VirtuaNES", "*.vmv",
					"Nintendulator", "*.nmv",
					"Snes9x", "*.smv",
					"ZSNES", "*.zmv",
					"All Files", "*.*"),
				RestoreDirectory = false
			};

			var result = ofd.ShowHawkDialog();
			if (result == DialogResult.OK)
			{
				foreach (var fn in ofd.FileNames)
				{
					ProcessMovieImport(fn);
				}
			}
		}

		private void SaveMovieMenuItem_Click(object sender, EventArgs e)
		{
			SaveMovie();
		}

		private void StopMovieWithoutSavingMenuItem_Click(object sender, EventArgs e)
		{
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

		private void RecordAVMenuItem_Click(object sender, EventArgs e)
		{
			this.RecordAv();
		}

		private void StopAVMenuItem_Click(object sender, EventArgs e)
		{
			this.StopAv();
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
			var path = String.Format(PathManager.ScreenshotPrefix(Global.Game) + ".{0:yyyy-MM-dd HH.mm.ss}.png", DateTime.Now);

			var sfd = new SaveFileDialog
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

		#endregion

		#region Emulation Menu

		private void emulationToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			RebootCoreMenuItem.Enabled = !(Global.Emulator is NullEmulator);

			PauseMenuItem.Checked = _didMenuPause ? _wasPaused : EmulatorPaused;

			SoftResetMenuItem.Enabled = Global.Emulator.ControllerDefinition.BoolButtons.Contains("Reset") &&
					(!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished);

			HardResetMenuItem.Enabled = Global.Emulator.ControllerDefinition.BoolButtons.Contains("Power") &&
				(!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished);

			PauseMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Pause"].Bindings;
			RebootCoreMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Reboot Core"].Bindings;
			SoftResetMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Soft Reset"].Bindings;
			HardResetMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Hard Reset"].Bindings;
		}

		private void PauseMenuItem_Click(object sender, EventArgs e)
		{
			if (EmulatorPaused)
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

			DisplayStatusBarMenuItem.Checked = Global.Config.DisplayStatusBar;
			DisplayLogWindowMenuItem.Checked = Global.Config.ShowLogWindow;
		}

		private void WindowSizeSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			x1MenuItem.Checked =
				x2MenuItem.Checked =
				x3MenuItem.Checked =
				x4MenuItem.Checked =
				x5MenuItem.Checked = false;

			switch (Global.Config.TargetZoomFactor)
			{
				case 1: x1MenuItem.Checked = true; break;
				case 2: x2MenuItem.Checked = true; break;
				case 3: x3MenuItem.Checked = true; break;
				case 4: x4MenuItem.Checked = true; break;
				case 5: x5MenuItem.Checked = true; break;
				case 10: mzMenuItem.Checked = true; break;
			}
		}

		private void WindowSize_Click(object sender, EventArgs e)
		{
			if (sender == x1MenuItem) Global.Config.TargetZoomFactor = 1;
			if (sender == x2MenuItem) Global.Config.TargetZoomFactor = 2;
			if (sender == x3MenuItem) Global.Config.TargetZoomFactor = 3;
			if (sender == x4MenuItem) Global.Config.TargetZoomFactor = 4;
			if (sender == x5MenuItem) Global.Config.TargetZoomFactor = 5;
			if (sender == mzMenuItem) Global.Config.TargetZoomFactor = 10;

			FrameBufferResized();
		}

		private void SwitchToFullscreenMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFullscreen();
		}

		private void DisplayFPSMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			ToggleFPS();
		}

		private void DisplayFrameCounterMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			ToggleFrameCounter();
		}

		private void DisplayLagCounterMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			ToggleLagCounter();
		}

		private void DisplayInputMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			ToggleInputDisplay();
		}

		private void DisplayRerecordsMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			Global.Config.DisplayRerecordCount ^= true;
		}

		private void DisplaySubtitlesMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			Global.Config.DisplaySubtitles ^= true;
		}

		private void DisplayStatusBarMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisplayStatusBar ^= true;

			if (!_inFullscreen)
			{
				MainStatusBar.Visible = Global.Config.DisplayStatusBar;
				PerformLayout();
				FrameBufferResized();
			}
		}

		private void DisplayLogWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.ShowLogWindow ^= true;

			if (Global.Config.ShowLogWindow)
			{
				ShowConsole();
			}
			else
			{
				HideConsole();
			}
		}

		#endregion

		#region Config

		private void ConfigSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ControllersMenuItem.Enabled = !(Global.Emulator is NullEmulator);

			toolStripSeparator8.Visible =
				coreSelectionToolStripMenuItem.Visible =
				VersionInfo.INTERIM;
		}

		private void EnableMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			EnableContextMenuMenuItem.Checked = Global.Config.ShowContextMenu;
			BackupSavestatesMenuItem.Checked = Global.Config.BackupSavestates;
			AutoSavestatesMenuItem.Checked = Global.Config.AutoSavestates;
			SaveScreenshotInSavestatesMenuItem.Checked = Global.Config.SaveScreenshotWithStates;
			FrameAdvanceSkipLagMenuItem.Checked = Global.Config.SkipLagFrame;
			BackupSaveramMenuItem.Checked = Global.Config.BackupSaveram;
		}

		private void GuiSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			PauseWhenMenuActivatedMenuItem.Checked = Global.Config.PauseWhenMenuActivated;
			StartPausedMenuItem.Checked = Global.Config.StartPaused;
			SaveWindowPositionMenuItem.Checked = Global.Config.SaveWindowPosition;
			SuppressGuiLayerMenuItem.Checked = Global.Config.SuppressGui;
			ShowMenuInFullScreenMenuItem.Checked = Global.Config.ShowMenuInFullscreen;
			RunInBackgroundMenuItem.Checked = Global.Config.RunInBackground;
			BackgroundInputMenuItem.Checked = Global.Config.AcceptBackgroundInput;
			SingleInstanceModeMenuItem.Checked = Global.Config.SingleInstanceMode;
			LogWindowAsConsoleMenuItem.Checked = Global.Config.WIN32_CONSOLE;
			DontAskToSaveChangesMenuItem.Checked = Global.Config.SupressAskSave;

			BackgroundInputMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle BG Input"].Bindings;
		}

		private void FrameSkipMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			MinimizeSkippingMenuItem.Checked = Global.Config.AutoMinimizeSkipping;
			ClickThrottleMenuItem.Checked = Global.Config.ClockThrottle;
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
			if (!MinimizeSkippingMenuItem.Enabled) MinimizeSkippingMenuItem.Checked = true;
			AudioThrottleMenuItem.Enabled = Global.Config.SoundEnabled;
			AudioThrottleMenuItem.Checked = Global.Config.SoundThrottle;
			VsyncEnabledMenuItem.Checked = Global.Config.VSync;

			Speed100MenuItem.Checked = Global.Config.SpeedPercent == 100;
			Speed100MenuItem.Image = (Global.Config.SpeedPercentAlternate == 100) ? Properties.Resources.FastForward : null;
			Speed150MenuItem.Checked = Global.Config.SpeedPercent == 150;
			Speed150MenuItem.Image = (Global.Config.SpeedPercentAlternate == 150) ? Properties.Resources.FastForward : null;
			Speed200MenuItem.Checked = Global.Config.SpeedPercent == 200;
			Speed200MenuItem.Image = (Global.Config.SpeedPercentAlternate == 200) ? Properties.Resources.FastForward : null;
			Speed75MenuItem.Checked = Global.Config.SpeedPercent == 75;
			Speed75MenuItem.Image = (Global.Config.SpeedPercentAlternate == 75) ? Properties.Resources.FastForward : null;
			Speed50MenuItem.Checked = Global.Config.SpeedPercent == 50;
			Speed50MenuItem.Image = (Global.Config.SpeedPercentAlternate == 50) ? Properties.Resources.FastForward : null;
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

		private void SavestateTypeMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			SavestateTypeDefaultMenuItem.Checked = false;
			SavestateBinaryMenuItem.Checked = false;
			SavestateTextMenuItem.Checked = false;
			switch (Global.Config.SaveStateType)
			{
				case Config.SaveStateTypeE.Binary: SavestateBinaryMenuItem.Checked = true; break;
				case Config.SaveStateTypeE.Text: SavestateTextMenuItem.Checked = true; break;
				case Config.SaveStateTypeE.Default: SavestateTypeDefaultMenuItem.Checked = true; break;
			}
		}

		private void ControllersMenuItem_Click(object sender, EventArgs e)
		{
			var controller = new ControllerConfig(Global.Emulator.ControllerDefinition);
			if (controller.ShowDialog() == DialogResult.OK)
			{
				InitControls();
				InputManager.SyncControls();
			}
		}

		private void HotkeysMenuItem_Click(object sender, EventArgs e)
		{
			var hotkeys = new HotkeyConfig();
			if (hotkeys.ShowDialog() == DialogResult.OK)
			{
				InitControls();
				InputManager.SyncControls();
			}
		}

		private void MessagesMenuItem_Click(object sender, EventArgs e)
		{
			new MessageConfig().ShowDialog();
		}

		private void PathsMenuItem_Click(object sender, EventArgs e)
		{
			new PathConfig().ShowDialog();
		}

		private void SoundMenuItem_Click(object sender, EventArgs e)
		{
			var sound = new SoundConfig();
			if (sound.ShowDialog() == DialogResult.OK)
			{
				RewireSound();
			}
		}

		private void AutofireMenuItem_Click(object sender, EventArgs e)
		{
			new AutofireConfig().ShowDialog();
		}

		private void RewindOptionsMenuItem_Click(object sender, EventArgs e)
		{
			new RewindConfig().ShowDialog();
		}

		private void FirmwaresMenuItem_Click(object sender, EventArgs e)
		{
			new FirmwaresConfig().Show();
		}

		private void EnableContextMenuMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.ShowContextMenu ^= true;
			GlobalWin.OSD.AddMessage(Global.Config.ShowContextMenu ? "Context menu enabled" : "Context menu disabled");
		}

		private void BackupSavestatesMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.BackupSavestates ^= true;
			GlobalWin.OSD.AddMessage(Global.Config.BackupSavestates ? "Backup savestates enabled" : "Backup savestates disabled");
		}

		private void AutoSavestatesMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoSavestates ^= true;
			GlobalWin.OSD.AddMessage(Global.Config.AutoSavestates ? "AutoSavestates enabled" : "AutoSavestates disabled");
		}

		private void ScreenshotWithSavestatesMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveScreenshotWithStates ^= true;
			GlobalWin.OSD.AddMessage(Global.Config.SaveScreenshotWithStates
										 ? "Screenshots will be saved in savestates"
										 : "Screenshots will not be saved in savestates");
		}

		private void frameAdvanceSkipLagFramesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SkipLagFrame ^= true;
		}

		private void BackupSaveramMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.BackupSaveram ^= true;
			GlobalWin.OSD.AddMessage(Global.Config.BackupSaveram ? "Backup saveram enabled" : "Backup saveram disabled");
		}

		private void PauseWhenMenuActivatedMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PauseWhenMenuActivated ^= true;
		}

		private void StartPausedMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.StartPaused ^= true;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveWindowPosition ^= true;
		}

		private void UseGDIMenuItem_Click(object sender, EventArgs e)
		{
			//TODO GL - this concept is gone
			//Global.Config.DisplayGDI ^= true;
			//SyncPresentationMode();
		}

		private void SuppressGuiLayerMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SuppressGui ^= true;
		}

		private void ShowMenuInFullScreenMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.ShowMenuInFullscreen ^= true;

			//make sure this gets applied immediately
			if (_inFullscreen)
			{
				MainMenuStrip.Visible = Global.Config.ShowMenuInFullscreen;
		}
		}

		private void RunInBackgroundMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RunInBackground ^= true;
		}

		private void BackgroundInputMenuItem_Click(object sender, EventArgs e)
		{
			ToggleBackgroundInput();
		}

		private void SingleInstanceModeMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SingleInstanceMode ^= true;
			MessageBox.Show("BizHawk must be restarted for this setting to take effect.", "Reboot Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void DontAskToSaveChangesMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SupressAskSave ^= true;
		}

		private void LogWindowAsConsoleMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.WIN32_CONSOLE ^= true;
		}

		private void ClickThrottleMenuItem_Click(object sender, EventArgs e)
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
					GlobalWin.PresentationPanel.Resized = true;
				}
			}
			LimitFrameRateMessage();
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
					GlobalWin.PresentationPanel.Resized = true;
				}
			}
		}

		private void VsyncThrottleMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VSyncThrottle ^= true;
			GlobalWin.PresentationPanel.Resized = true;
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
			VsyncMessage();
		}

		private void VsyncEnabledMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VSync ^= true;
			if (!Global.Config.VSyncThrottle) // when vsync throttle is on, vsync is forced to on, so no change to make here
			{
				GlobalWin.PresentationPanel.Resized = true;
			}
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

		private void SavestateTypeDefaultMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveStateType = Config.SaveStateTypeE.Default;
		}

		private void SavestateBinaryMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveStateType = Config.SaveStateTypeE.Binary;
		}

		private void SavestateTextMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveStateType = Config.SaveStateTypeE.Text;
		}


		private void SaveConfigMenuItem_Click(object sender, EventArgs e)
		{
			SaveConfig();
			GlobalWin.OSD.AddMessage("Saved settings");
		}

		private void LoadConfigMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config = ConfigService.Load<Config>(PathManager.DefaultIniPath);
			Global.Config.ResolveDefaults();
			GlobalWin.OSD.AddMessage("Config file loaded");
		}

		#endregion

		#region Tools

		private void toolsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			ToolBoxMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["ToolBox"].Bindings;
			RamWatchMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Ram Watch"].Bindings;
			RamSearchMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Ram Search"].Bindings;
			HexEditorMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Hex Editor"].Bindings;
			LuaConsoleMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Lua Console"].Bindings;
			CheatsMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Cheats"].Bindings;
			TAStudioMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["TAStudio"].Bindings;
			VirtualPadMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Virtual Pad"].Bindings;
			TraceLoggerMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Trace Logger"].Bindings;
			TraceLoggerMenuItem.Enabled = Global.Emulator.CoreComm.CpuTraceAvailable;
			CheatsMenuItem.Enabled =
				HexEditorMenuItem.Enabled =
				RamSearchMenuItem.Enabled =
				RamWatchMenuItem.Enabled =
				TAStudioMenuItem.Enabled =
				VirtualPadMenuItem.Enabled =
				!(Global.Emulator is NullEmulator);
			batchRunnerToolStripMenuItem.Visible = VersionInfo.INTERIM;
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
			GlobalWin.Tools.Load<RamSearch>();
		}

		private void HexEditorMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<HexEditor>();
		}

		private void TraceLoggerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadTraceLogger();
		}

		private void TAStudioMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<TAStudio>();
		}

		private void VirtualPadMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<VirtualPadForm>();
		}

		private void CheatsMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<Cheats>();
		}

		private void LuaConsoleMenuItem_Click(object sender, EventArgs e)
		{
			OpenLuaConsole();
		}

		private void CreateDualGbXmlMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Sound.StopSound();
			using (var dlg = new DualGBXMLCreator())
			{
				dlg.ShowDialog(this);
			}
			GlobalWin.Sound.StartSound();
		}

		#endregion

		#region NES

		private void NESSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			FDSControlsMenuItem.Enabled = Global.Emulator.BoardName == "FDS";

			NESDebuggerMenuItem.Visible =
			MovieSettingsMenuItem.Visible =
				VersionInfo.INTERIM;

			NESDebuggerMenuItem.Enabled =
				NESPPUViewerMenuItem.Enabled =
				NESNametableViewerMenuItem.Enabled =
				NESSoundChannelsMenuItem.Enabled =
				MovieSettingsMenuItem.Enabled =
				Global.Emulator is NES;
		}

		private void FdsControlsMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			FdsEjectDiskMenuItem.Enabled = Global.Emulator.BoardName == "FDS";

			while (FDSControlsMenuItem.DropDownItems.Count > 1)
			{
				FDSControlsMenuItem.DropDownItems.RemoveAt(1);
			}

			for (int i = 0; i < 16; i++)
			{
				var str = "FDS Insert " + i;
				if (Global.Emulator.ControllerDefinition.BoolButtons.Contains(str))
				{
					FdsInsertDiskMenuAdd("Insert Disk " + i, str, "FDS Disk " + i + " inserted.");
				}
			}
		}

		private void NESDebuggerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NESDebugger>();
		}

		private void NESPPUViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NesPPU>();
		}

		private void NESNametableViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NESNameTableViewer>();
		}

		private void NESGameGenieCodesMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadGameGenieEc();
		}

		private void NESGraphicSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is NES)
				new NESGraphicsConfig().ShowDialog(this);
			else if (Global.Emulator is QuickNES)
				new QuickNesConfig().ShowDialog(this);
		}

		private void NESSoundChannelsMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<NESSoundConfig>();
		}

		private void FdsEjectDiskMenuItem_Click(object sender, EventArgs e)
		{
			if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
			{
				Global.ClickyVirtualPadController.Click("FDS Eject");
				GlobalWin.OSD.AddMessage("FDS disk ejected.");
			}
		}

		private void MovieSettingsMenuItem_Click(object sender, EventArgs e)
		{
			using (var dlg = new NESSyncSettingsForm())
			{
				dlg.ShowDialog(this);
			}
		}


		#endregion

		#region PCE

		private void PCESubMenu_DropDownOpened(object sender, EventArgs e)
		{
			var s = (PCEngine.PCESettings)Global.Emulator.GetSettings();

			PCEAlwaysPerformSpriteLimitMenuItem.Checked = s.SpriteLimit;
			PCEAlwaysEqualizeVolumesMenuItem.Checked = s.EqualizeVolume;
			PCEArcadeCardRewindEnableMenuItem.Checked = s.ArcadeCardRewindHack;
		}

		private void PCEBGViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<PceBgViewer>();
		}

		private void PCEAlwaysPerformSpriteLimitMenuItem_Click(object sender, EventArgs e)
		{
			var s = (PCEngine.PCESettings)Global.Emulator.GetSettings();
			s.SpriteLimit ^= true;
			PutCoreSettings(s);
		}

		private void PCEAlwaysEqualizeVolumesMenuItem_Click(object sender, EventArgs e)
		{
			var s = (PCEngine.PCESettings)Global.Emulator.GetSettings();
			s.EqualizeVolume ^= true;
			PutCoreSettings(s);
		}

		private void PCEArcadeCardRewindEnableMenuItem_Click(object sender, EventArgs e)
		{
			var s = (PCEngine.PCESettings)Global.Emulator.GetSettings();
			s.ArcadeCardRewindHack ^= true;
			PutCoreSettings(s);
		}

		private void PCEGraphicsSettingsMenuItem_Click(object sender, EventArgs e)
		{
			new PCEGraphicsConfig().ShowDialog();
		}

		#endregion

		#region SMS

		private void SMSSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			var s = (SMS.SMSSettings)Global.Emulator.GetSettings();
			var ss = (SMS.SMSSyncSettings)Global.Emulator.GetSyncSettings();
			SMSEnableFMChipMenuItem.Checked = ss.EnableFM;
			SMSOverclockMenuItem.Checked = ss.AllowOverlock;
			SMSForceStereoMenuItem.Checked = s.ForceStereoSeparation;
			SMSSpriteLimitMenuItem.Checked = s.SpriteLimit;
			ShowClippedRegionsMenuItem.Checked = s.ShowClippedRegions;
			HighlightActiveDisplayRegionMenuItem.Checked = s.HighlightActiveDisplayRegion;

			SMSEnableFMChipMenuItem.Visible =
				SMSOverclockMenuItem.Visible =
				SMSForceStereoMenuItem.Visible =
				Global.Game.System != "GG";

			ShowClippedRegionsMenuItem.Visible =
				HighlightActiveDisplayRegionMenuItem.Visible =
				GGGameGenieMenuItem.Visible =
				Global.Game.System == "GG";
		}

		private void SMSEnableFMChipMenuItem_Click(object sender, EventArgs e)
		{
			var ss = (SMS.SMSSyncSettings)Global.Emulator.GetSyncSettings();
			ss.EnableFM ^= true;
			PutCoreSyncSettings(ss);
		}

		private void SMSOverclockMenuItem_Click(object sender, EventArgs e)
		{
			var ss = (SMS.SMSSyncSettings)Global.Emulator.GetSyncSettings();
			ss.AllowOverlock ^= true;
			PutCoreSyncSettings(ss);
		}

		private void SMSForceStereoMenuItem_Click(object sender, EventArgs e)
		{
			var s = (SMS.SMSSettings)Global.Emulator.GetSettings();
			s.ForceStereoSeparation ^= true;
			PutCoreSettings(s);
		}

		private void SMSSpriteLimitMenuItem_Click(object sender, EventArgs e)
		{
			var s = (SMS.SMSSettings)Global.Emulator.GetSettings();
			s.SpriteLimit ^= true;
			PutCoreSettings(s);
		}

		private void ShowClippedRegionsMenuItem_Click(object sender, EventArgs e)
		{
			var s = (SMS.SMSSettings)Global.Emulator.GetSettings();
			s.ShowClippedRegions ^= true;
			PutCoreSettings(s);
		}

		private void HighlightActiveDisplayRegionMenuItem_Click(object sender, EventArgs e)
		{
			var s = (SMS.SMSSettings)Global.Emulator.GetSettings();
			s.HighlightActiveDisplayRegion ^= true;
			PutCoreSettings(s);
		}

		private void SMSGraphicsSettingsMenuItem_Click(object sender, EventArgs e)
		{
			new SMSGraphicsConfig().ShowDialog();
		}

		private void GGGameGenieMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadGameGenieEc();
		}

		#endregion

		#region TI83

		private void TI83SubMenu_DropDownOpened(object sender, EventArgs e)
		{
			AutoloadKeypadMenuItem.Checked = Global.Config.TI83autoloadKeyPad;
		}

		private void KeypadMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is TI83)
			{
				GlobalWin.Tools.Load<TI83KeyPad>();
			}
		}

		private void AutoloadKeypadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TI83autoloadKeyPad ^= true;
		}

		private void LoadTIFileMenuItem_Click(object sender, EventArgs e)
		{
			var OFD = new OpenFileDialog();

			if (OFD.ShowDialog() == DialogResult.OK)
			{
				try
				{
					(Global.Emulator as TI83).LinkPort.SendFileToCalc(File.OpenRead(OFD.FileName), true);
				}
				catch (IOException ex)
				{
					var Message = String.Format("Invalid file format. Reason: {0} \nForce transfer? This may cause the calculator to crash.", ex.Message);

					if (MessageBox.Show(Message, "Upload Failed", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
					{
						(Global.Emulator as TI83).LinkPort.SendFileToCalc(File.OpenRead(OFD.FileName), false);
					}
				}
			}
		}

		#endregion

		#region Atari

		private void AtariSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Atari 2600 Settings");
		}

		#endregion

		#region GB

		private void GBSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			var s = (Gameboy.GambatteSyncSettings)Global.Emulator.GetSyncSettings();

			GBForceDMGMenuItem.Checked = s.ForceDMG;
			GBAInCGBModeMenuItem.Checked = s.GBACGB;
			GBMulticartCompatibilityMenuItem.Checked = s.MulticartCompat;
			LoadGBInSGBMenuItem.Checked = Global.Config.GB_AsSGB;
		}

		private void GBForceDMGMenuItem_Click(object sender, EventArgs e)
		{
			var s = (Gameboy.GambatteSyncSettings)Global.Emulator.GetSyncSettings();
			s.ForceDMG ^= true;
			PutCoreSyncSettings(s);
		}

		private void GBAInCGBModeMenuItem_Click(object sender, EventArgs e)
		{
			var s = (Gameboy.GambatteSyncSettings)Global.Emulator.GetSyncSettings();
			s.GBACGB ^= true;
			PutCoreSyncSettings(s);
		}

		private void GBMulticartCompatibilityMenuItem_Click(object sender, EventArgs e)
		{
			var s = (Gameboy.GambatteSyncSettings)Global.Emulator.GetSyncSettings();
			s.MulticartCompat ^= true;
			PutCoreSyncSettings(s);
		}

		private void GBPaletteConfigMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is Gameboy)
			{
				var gb = Global.Emulator as Gameboy;
				if (gb.IsCGBMode())
					CGBColorChooserForm.DoCGBColorChooserFormDialog(this);
				else
					ColorChooserForm.DoColorChooserFormDialog(this);
			}
		}

		private void LoadGBInSGBMenuItem_Click(object sender, EventArgs e)
		{
			SnesGBInSGBMenuItem_Click(sender, e);
		}

		private void GBGPUViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GBGPUView>();
		}

		private void GBGameGenieMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.LoadGameGenieEc();
		}

		#endregion

		#region GBA

		private void GbaGpuViewerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<GBAGPUView>();
		}

		#endregion

		#region SNES

		private void SNESSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			if ((Global.Emulator as LibsnesCore).IsSGB)
			{
				SnesGBInSGBMenuItem.Visible = true;
				SnesGBInSGBMenuItem.Checked = Global.Config.GB_AsSGB;
			}
			else
			{
				SnesGBInSGBMenuItem.Visible = false;
			}
		}

		private void SNESDisplayMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();

			SnesBg1MenuItem.Checked = s.ShowBG1_1;
			SnesBg2MenuItem.Checked = s.ShowBG2_1;
			SnesBg3MenuItem.Checked = s.ShowBG3_1;
			SnesBg4MenuItem.Checked = s.ShowBG4_1;

			SnesObj1MenuItem.Checked = s.ShowOBJ_0;
			SnesObj2MenuItem.Checked = s.ShowOBJ_1;
			SnesObj3MenuItem.Checked = s.ShowOBJ_2;
			SnesObj4MenuItem.Checked = s.ShowOBJ_3;

			SnesBg1MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle BG 1"].Bindings;
			SnesBg2MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle BG 2"].Bindings;
			SnesBg3MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle BG 3"].Bindings;
			SnesBg4MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle BG 4"].Bindings;

			SnesObj1MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle OBJ 1"].Bindings;
			SnesObj2MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle OBJ 2"].Bindings;
			SnesObj3MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle OBJ 3"].Bindings;
			SnesObj4MenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle OBJ 4"].Bindings;
		}

		private void SnesBg1MenuItem_Click(object sender, EventArgs e)
		{
			SNES_ToggleBG1();
		}

		private void SnesBg2MenuItem_Click(object sender, EventArgs e)
		{
			SNES_ToggleBG2();
		}

		private void SnesBg3MenuItem_Click(object sender, EventArgs e)
		{
			SNES_ToggleBG3();
		}

		private void SnesBg4MenuItem_Click(object sender, EventArgs e)
		{
			SNES_ToggleBG4();
		}

		private void SnesObj1MenuItem_Click(object sender, EventArgs e)
		{
			this.SNES_ToggleObj1();
		}

		private void SnesObj2MenuItem_Click(object sender, EventArgs e)
		{
			this.SNES_ToggleObj2();
		}

		private void SnesObj3MenuItem_Click(object sender, EventArgs e)
		{
			SNES_ToggleOBJ3();
		}

		private void SnesObj4MenuItem_Click(object sender, EventArgs e)
		{
			SNES_ToggleOBJ4();
		}

		private void SnesGfxDebuggerMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<SNESGraphicsDebugger>();
		}

		private void SnesGBInSGBMenuItem_Click(object sender, EventArgs e)
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

		#endregion

		#region Coleco

		private void ColecoSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			var ss = (ColecoVision.ColecoSyncSettings)Global.Emulator.GetSyncSettings();
			ColecoSkipBiosMenuItem.Checked = ss.SkipBiosIntro;
		}

		private void ColecoSkipBiosMenuItem_Click(object sender, EventArgs e)
		{
			var ss = (ColecoVision.ColecoSyncSettings)Global.Emulator.GetSyncSettings();
			ss.SkipBiosIntro ^= true;
			PutCoreSyncSettings(ss);
		}

		#endregion

		#region N64

		private void N64PluginSettingsMenuItem_Click(object sender, EventArgs e)
		{
			if (new N64VideoPluginconfig().ShowDialog() == DialogResult.OK)
			{
				GlobalWin.MainForm.FlagNeedsReboot();
				GlobalWin.OSD.AddMessage("Plugin settings saved");
			}
			else
			{
				GlobalWin.OSD.AddMessage("Plugin settings aborted");
			}
		}

		#endregion

		#region Saturn

		private void SaturnPreferencesMenuItem_Click(object sender, EventArgs e)
		{
			using (var dlg = new SaturnPrefs())
			{
				dlg.ShowDialog(this);
			}
		}

		#endregion

		#region DGB

		private void DGBsettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			BizHawk.Client.EmuHawk.config.GB.DGBPrefs.DoDGBPrefsDialog(this);
		}

		#endregion

		#region GEN

		private void GenesisSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Genesis Settings");
		}

		#endregion

		#region Help

		private void OnlineHelpMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/BizHawk.html");
		}

		private void ForumsMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/forum/viewforum.php?f=64");
		}

		private void AboutMenuItem_Click(object sender, EventArgs e)
		{
			if (VersionInfo.INTERIM)
			{
				new AboutBox().ShowDialog();
			}
			else
			{
				new BizBox().ShowDialog();
			}
		}

		#endregion

		#region Context Menu

		private void MainFormContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			_wasPaused = EmulatorPaused;
			_didMenuPause = true;
			PauseEmulator();

			OpenRomContextMenuItem.Visible = (Global.Emulator is NullEmulator) || _inFullscreen;

			ShowMenuContextMenuItem.Visible =
				ShowMenuContextMenuSeparator.Visible =
				_inFullscreen;

			LoadLastRomContextMenuItem.Visible =
				(Global.Emulator is NullEmulator);

			StopAVContextMenuItem.Visible = _currAviWriter != null;

			ContextSeparator_AfterMovie.Visible =
				ContextSeparator_AfterUndo.Visible =
				ScreenshotContextMenuItem.Visible =
				CloseRomContextMenuItem.Visible =
				UndoSavestateContextMenuItem.Visible =
				!(Global.Emulator is NullEmulator);

			RecordMovieContextMenuItem.Visible =
				PlayMovieContextMenuItem.Visible =
				LoadLastMovieContextMenuItem.Visible =
				!(Global.Emulator is NullEmulator) && !Global.MovieSession.Movie.IsActive;

			RestartMovieContextMenuItem.Visible =
				StopMovieContextMenuItem.Visible =
				ViewSubtitlesContextMenuItem.Visible =
				ViewCommentsContextMenuItem.Visible =
				SaveMovieContextMenuItem.Visible =
				Global.MovieSession.Movie.IsActive;

			BackupMovieContextMenuItem.Visible = Global.MovieSession.Movie is Movie && Global.MovieSession.Movie.IsActive;

			StopNoSaveContextMenuItem.Visible = Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie.Changes;

			AddSubtitleContextMenuItem.Visible = !(Global.Emulator is NullEmulator) && Global.MovieSession.Movie.IsActive && !Global.MovieSession.ReadOnly;

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

			var file = new FileInfo(
				PathManager.SaveStatePrefix(Global.Game) +
				".QuickSave" +
				Global.Config.SaveSlot +
				".State.bak"
			);

			if (file.Exists)
			{
				UndoSavestateContextMenuItem.Enabled = true;
				if (_stateSlots.IsRedo(Global.Config.SaveSlot))
				{
					UndoSavestateContextMenuItem.Text = "Redo Save to slot " + Global.Config.SaveSlot;
					UndoSavestateContextMenuItem.Image = Properties.Resources.redo;
				}
				else
				{
					UndoSavestateContextMenuItem.Text = "Undo Save to slot " + Global.Config.SaveSlot;
					UndoSavestateContextMenuItem.Image = Properties.Resources.undo;
				}
			}
			else
			{
				UndoSavestateContextMenuItem.Enabled = false;
				UndoSavestateContextMenuItem.Text = "Undo Savestate";
				UndoSavestateContextMenuItem.Image = Properties.Resources.undo;
			}

			if (_inFullscreen)
			{
				ShowMenuContextMenuItem.Text = MainMenuStrip.Visible ? "Hide Menu" : "Show Menu";
			}
		}

		private void MainFormContextMenu_Closing(object sender, ToolStripDropDownClosingEventArgs e)
		{
			if (!_wasPaused)
			{
				UnpauseEmulator();
			}
		}

		private void LoadLastRomContextMenuItem_Click(object sender, EventArgs e)
		{
			LoadRomFromRecent(Global.Config.RecentRoms[0]);
		}

		private void LoadLastMovieContextMenuItem_Click(object sender, EventArgs e)
		{
			LoadMoviesFromRecent(Global.Config.RecentMovies[0]);
		}

		private void BackupMovieContextMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie is Movie)
			{
				GlobalWin.OSD.AddMessage("Backup movie saved.");
				(Global.MovieSession.Movie as Movie).SaveBackup();
			}
		}

		private void ViewSubtitlesContextMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				var form = new EditSubtitlesForm { ReadOnly = Global.MovieSession.ReadOnly };
				form.GetMovie(Global.MovieSession.Movie);
				form.ShowDialog();
			}
		}

		private void AddSubtitleContextMenuItem_Click(object sender, EventArgs e)
		{
			//TODO: rethink this?
			var subForm = new SubtitleMaker();
			subForm.DisableFrame();

			int index = -1;
			var sub = new Subtitle();
			for (int x = 0; x < Global.MovieSession.Movie.Header.Subtitles.Count; x++)
			{
				sub = Global.MovieSession.Movie.Header.Subtitles[x];
				if (Global.Emulator.Frame == sub.Frame)
				{
					index = x;
					break;
				}
			}

			if (index < 0)
			{
				sub = new Subtitle { Frame = Global.Emulator.Frame };
			}

			subForm.Sub = sub;

			if (subForm.ShowDialog() == DialogResult.OK)
			{
				if (index >= 0)
				{
					Global.MovieSession.Movie.Header.Subtitles.RemoveAt(index);
				}

				Global.MovieSession.Movie.Header.Subtitles.Add(subForm.Sub);
			}
		}

		private void ViewCommentsContextMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				var form = new EditCommentsForm();
				form.GetMovie(Global.MovieSession.Movie);
				form.ShowDialog();
			}
		}

		private void UndoSavestateContextMenuItem_Click(object sender, EventArgs e)
		{
			_stateSlots.SwapBackupSavestate(
				PathManager.SaveStatePrefix(Global.Game) +
				".QuickSave" +
				Global.Config.SaveSlot +
				".State"
			);

			GlobalWin.OSD.AddMessage("Save slot " + Global.Config.SaveSlot + " restored.");
		}

		private void ClearSRAMContextMenuItem_Click(object sender, EventArgs e)
		{
			CloseRom(clearSram: true);
		}

		private void ShowMenuContextMenuItem_Click(object sender, EventArgs e)
		{
			MainMenuStrip.Visible ^= true;
		}

		#endregion

		#region Status Bar

		private void DumpStatusButton_Click(object sender, EventArgs e)
		{
			string details = Global.Emulator.CoreComm.RomStatusDetails;
			if (!String.IsNullOrEmpty(details))
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
					LoadQuickSave("QuickSave" + slot);
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				SaveQuickSave("QuickSave" + slot);
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

		private void MainForm_Enter(object sender, EventArgs e)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
		}

		public void MainForm_MouseClick(object sender, MouseEventArgs e)
		{
			if (Global.Config.ShowContextMenu && e.Button == MouseButtons.Right)
			{
				MainFormContextMenu.Show(
					PointToScreen(new Point(e.X, e.Y + MainformMenu.Height))
				);
			}
		}

		private void MainForm_Resize(object sender, EventArgs e)
		{
			if(GlobalWin.PresentationPanel != null)
				GlobalWin.PresentationPanel.Resized = true;
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			HandlePlatformMenus();
		}

		protected override void OnClosed(EventArgs e)
		{
			_exit = true;
			base.OnClosed(e);
		}

		private void MainformMenu_Leave(object sender, EventArgs e)
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
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
			GlobalWin.DisplayManager.NeedsToPaint = true;
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
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			var isLua = false;
			foreach (var path in filePaths)
			{
				var extension = Path.GetExtension(path);
				if (extension != null && extension.ToUpper() == ".LUA")
				{
					OpenLuaConsole();
					if (GlobalWin.Tools.Has<LuaConsole>())
					{
						GlobalWin.Tools.LuaConsole.LoadLuaFile(path);
					}
					isLua = true;
				}
			}
			if (isLua)
			{
				return;
			}

			var ext = Path.GetExtension(filePaths[0]) ?? String.Empty;
			if (ext.ToUpper() == ".LUASES")
			{
				OpenLuaConsole();
				if (GlobalWin.Tools.Has<LuaConsole>())
				{
					GlobalWin.Tools.LuaConsole.LoadLuaSession(filePaths[0]);
				}
			}
			else if (MovieSession.IsValidMovieExtension(ext))
			{
				StartNewMovie(new Movie(filePaths[0]), false);
			}
			else if (ext.ToUpper() == ".STATE")
			{
				LoadState(filePaths[0], Path.GetFileName(filePaths[0]));
			}
			else if (ext.ToUpper() == ".CHT")
			{
				Global.CheatList.Load(filePaths[0], false);
				GlobalWin.Tools.Load<Cheats>();
			}
			else if (ext.ToUpper() == ".WCH")
			{
				GlobalWin.Tools.LoadRamWatch(true);
				(GlobalWin.Tools.Get<RamWatch>() as RamWatch).LoadWatchFile(new FileInfo(filePaths[0]), false);
			}

			else if (MovieImport.IsValidMovieExtension(Path.GetExtension(filePaths[0])))
			{
				//tries to open a legacy movie format as if it were a BKM, by importing it
				if (CurrentlyOpenRom == null)
				{
					OpenRom();
				}
				else
				{
					LoadRom(CurrentlyOpenRom);
				}

				string errorMsg;
				string warningMsg;
				var movie = MovieImport.ImportFile(filePaths[0], out errorMsg, out warningMsg);
				if (!String.IsNullOrEmpty(errorMsg))
				{
					MessageBox.Show(errorMsg, "Conversion error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					//fix movie extension to something palatable for these purposes. 
					//for instance, something which doesnt clobber movies you already may have had.
					//i'm evenly torn between this, and a file in %TEMP%, but since we dont really have a way to clean up this tempfile, i choose this:
					movie.Filename += ".autoimported." + Global.Config.MovieExtension;
					movie.Save();
					StartNewMovie(movie, false);
				}
				GlobalWin.OSD.AddMessage(warningMsg);
			}
			else
			{
				LoadRom(filePaths[0]);
			}
		}

		#endregion
	}
}
