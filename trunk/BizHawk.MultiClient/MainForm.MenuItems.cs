using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Calculator;
using BizHawk.Emulation.Consoles.GB;
using BizHawk.Emulation.Consoles.Nintendo.SNES;

using BizHawk.Client.Common;

namespace BizHawk.MultiClient
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
					!IsNullEmulator();
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
				 StateSlots.HasSlot(0) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState1MenuItem.Font = new Font(
				SaveState1MenuItem.Font.FontFamily,
				SaveState1MenuItem.Font.Size,
				StateSlots.HasSlot(1) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState2MenuItem.Font = new Font(
				SaveState2MenuItem.Font.FontFamily,
				SaveState2MenuItem.Font.Size,
				StateSlots.HasSlot(2) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState3MenuItem.Font = new Font(
				SaveState3MenuItem.Font.FontFamily,
				SaveState3MenuItem.Font.Size,
				StateSlots.HasSlot(3) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);
			
			SaveState4MenuItem.Font = new Font(
				SaveState4MenuItem.Font.FontFamily,
				SaveState4MenuItem.Font.Size,
				StateSlots.HasSlot(4) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);
			
			SaveState5MenuItem.Font = new Font(
				SaveState5MenuItem.Font.FontFamily,
				SaveState5MenuItem.Font.Size,
				StateSlots.HasSlot(5) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState6MenuItem.Font = new Font(
				SaveState6MenuItem.Font.FontFamily,
				SaveState6MenuItem.Font.Size,
				StateSlots.HasSlot(6) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState7MenuItem.Font = new Font(
				SaveState7MenuItem.Font.FontFamily,
				SaveState7MenuItem.Font.Size,
				StateSlots.HasSlot(7) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState8MenuItem.Font = new Font(
				SaveState8MenuItem.Font.FontFamily,
				SaveState8MenuItem.Font.Size,
				StateSlots.HasSlot(8) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
			);

			SaveState9MenuItem.Font = new Font(
				SaveState9MenuItem.Font.FontFamily,
				SaveState9MenuItem.Font.Size,
				StateSlots.HasSlot(9) ? (FontStyle.Italic | FontStyle.Bold) : FontStyle.Regular
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
				!IsNullEmulator();
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

			LoadNamedStateMenuItem.Enabled = !IsNullEmulator();
			LoadState1MenuItem.Enabled = !IsNullEmulator() && StateSlots.HasSlot(1);
			LoadState2MenuItem.Enabled = !IsNullEmulator() && StateSlots.HasSlot(2);
			LoadState3MenuItem.Enabled = !IsNullEmulator() && StateSlots.HasSlot(3);
			LoadState4MenuItem.Enabled = !IsNullEmulator() && StateSlots.HasSlot(4);
			LoadState5MenuItem.Enabled = !IsNullEmulator() && StateSlots.HasSlot(5);
			LoadState6MenuItem.Enabled = !IsNullEmulator() && StateSlots.HasSlot(6);
			LoadState7MenuItem.Enabled = !IsNullEmulator() && StateSlots.HasSlot(7);
			LoadState8MenuItem.Enabled = !IsNullEmulator() && StateSlots.HasSlot(8);
			LoadState9MenuItem.Enabled = !IsNullEmulator() && StateSlots.HasSlot(9);
			LoadState0MenuItem.Enabled = !IsNullEmulator() && StateSlots.HasSlot(0);
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

			SaveToCurrentSlotMenuItem.Enabled = LoadCurrentSlotMenuItem.Enabled = !IsNullEmulator();

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
			StopMovieWithoutSavingMenuItem.Enabled = Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie.HasChanges;
			StopMovieMenuItem.Enabled
				= PlayFromBeginningMenuItem.Enabled
				= SaveMovieMenuItem.Enabled
				= Global.MovieSession.Movie.IsActive;

			ReadonlyMenuItem.Checked = ReadOnly;
			BindSavestatesToMoviesMenuItem.Checked = Global.Config.BindSavestatesToMovies;
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

			if (CurrAviWriter == null)
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
			OpenROM();
		}

		private void CloseRomMenuItem_Click(object sender, EventArgs e)
		{
			CloseROM();
		}

		private void Savestate1MenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave1"); }
		private void Savestate2MenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave2"); }
		private void Savestate3MenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave3"); }
		private void Savestate4MenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave4"); }
		private void Savestate5MenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave5"); }
		private void Savestate6MenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave6"); }
		private void Savestate7MenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave7"); }
		private void Savestate8MenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave8"); }
		private void Savestate9MenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave9"); }
		private void Savestate0MenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave0"); }

		private void SaveNamedStateMenuItem_Click(object sender, EventArgs e)
		{
			SaveStateAs();
		}

		private void Loadstate1MenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave1"); }
		private void Loadstate2MenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave2"); }
		private void Loadstate3MenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave3"); }
		private void Loadstate4MenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave4"); }
		private void Loadstate5MenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave5"); }
		private void Loadstate6MenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave6"); }
		private void Loadstate7MenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave7"); }
		private void Loadstate8MenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave8"); }
		private void Loadstate9MenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave9"); }
		private void Loadstate0MenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave0"); }

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
			SaveState("QuickSave" + Global.Config.SaveSlot.ToString());
		}

		private void LoadCurrentSlotMenuItem_Click(object sender, EventArgs e)
		{
			LoadState("QuickSave" + Global.Config.SaveSlot.ToString());
		}

		private void ReadonlyMenuItem_Click(object sender, EventArgs e)
		{
			ToggleReadOnly();
		}

		private void RecordMovieMenuItem_Click(object sender, EventArgs e)
		{
			RecordMovie();
		}

		private void PlayMovieMenuItem_Click(object sender, EventArgs e)
		{
			PlayMovie();
		}

		private void StopMovieMenuItem_Click(object sender, EventArgs e)
		{
			StopMovie();
		}

		private void PlayFromBeginningMenuItem_Click(object sender, EventArgs e)
		{
			PlayMovieFromBeginning();
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

			GlobalWinF.Sound.StopSound();
			var result = ofd.ShowDialog();
			GlobalWinF.Sound.StartSound();
			if (result != DialogResult.OK)
			{
				return;
			}
			else
			{
				foreach (string fn in ofd.FileNames)
				{
					ProcessMovieImport(fn);
				}
			}
		}

		private void SaveMovieMenuItem_Click(object sender, EventArgs e)
		{
			SaveMovie();
		}

		private void stopMovieWithoutSaveMenuItem_Click(object sender, EventArgs e)
		{
			StopMovie(true);
		}

		private void BindSavestatesToMoviesMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.BindSavestatesToMovies ^= true;
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
			RecordAVI();
		}

		private void StopAVMenuItem_Click(object sender, EventArgs e)
		{
			StopAVI();
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
			string path = String.Format(PathManager.ScreenshotPrefix(Global.Game) + ".{0:yyyy-MM-dd HH.mm.ss}.png", DateTime.Now);

			SaveFileDialog sfd = new SaveFileDialog
			{
				InitialDirectory = Path.GetDirectoryName(path),
				FileName = Path.GetFileName(path),
				Filter = "PNG File (*.png)|*.png"
			};

			GlobalWinF.Sound.StopSound();
			var result = sfd.ShowDialog();
			GlobalWinF.Sound.StartSound();
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
			if (RamWatch1.AskSave())
			{
				Close();
			}
		}

		#endregion

		private void DumpStatus_Click(object sender, EventArgs e)
		{
			string details = Global.Emulator.CoreComm.RomStatusDetails;
			if (string.IsNullOrEmpty(details)) return;
			GlobalWinF.Sound.StopSound();
			LogWindow.ShowReport("Dump Status Report", details, this);
			GlobalWinF.Sound.StartSound();
		}

		private void saveWindowPositionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveWindowPosition ^= true;
		}

		private void startPausedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.StartPaused ^= true;
		}

		private void luaConsoleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenLuaConsole();
		}

		private void miLimitFramerate_Click(object sender, EventArgs e)
		{
			Global.Config.ClockThrottle ^= true;
			if (Global.Config.ClockThrottle)
			{
				bool old = Global.Config.SoundThrottle;
				Global.Config.SoundThrottle = false;
				if (old)
					RewireSound();
				old = Global.Config.VSyncThrottle;
				Global.Config.VSyncThrottle = false;
				if (old)
					GlobalWinF.RenderPanel.Resized = true;
			}
			LimitFrameRateMessage();
		}

		private void audioThrottleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SoundThrottle ^= true;
			RewireSound();
			if (Global.Config.SoundThrottle)
			{
				Global.Config.ClockThrottle = false;
				bool old = Global.Config.VSyncThrottle;
				Global.Config.VSyncThrottle = false;
				if (old)
					GlobalWinF.RenderPanel.Resized = true;
			}

		}

		private void miDisplayVsync_Click(object sender, EventArgs e)
		{
			Global.Config.VSyncThrottle ^= true;
			GlobalWinF.RenderPanel.Resized = true;
			if (Global.Config.VSyncThrottle)
			{
				Global.Config.ClockThrottle = false;
				bool old = Global.Config.SoundThrottle;
				Global.Config.SoundThrottle = false;
				if (old)
					RewireSound();
			}
			VsyncMessage();
		}

		private void vSyncEnabledToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VSync ^= true;
			if (!Global.Config.VSyncThrottle) // when vsync throttle is on, vsync is forced to on, so no change to make here
				GlobalWinF.RenderPanel.Resized = true;
		}

		public void LimitFrameRateMessage()
		{
			if (Global.Config.ClockThrottle)
			{
				GlobalWinF.OSD.AddMessage("Framerate limiting on");
			}
			else
			{
				GlobalWinF.OSD.AddMessage("Framerate limiting off");
			}
		}


		public void VsyncMessage()
		{
			if (Global.Config.VSyncThrottle)
			{
				GlobalWinF.OSD.AddMessage("Display Vsync is set to on");
			}
			else
			{
				GlobalWinF.OSD.AddMessage("Display Vsync is set to off");
			}
		}

		private void miAutoMinimizeSkipping_Click(object sender, EventArgs e)
		{
			Global.Config.AutoMinimizeSkipping ^= true;
		}

		public void MinimizeFrameskipMessage()
		{
			if (Global.Config.AutoMinimizeSkipping)
			{
				GlobalWinF.OSD.AddMessage("Autominimizing set to on");
			}
			else
			{
				GlobalWinF.OSD.AddMessage("Autominimizing set to off");
			}
		}

		private void miFrameskip0_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 0; FrameSkipMessage(); }
		private void miFrameskip1_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 1; FrameSkipMessage(); }
		private void miFrameskip2_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 2; FrameSkipMessage(); }
		private void miFrameskip3_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 3; FrameSkipMessage(); }
		private void miFrameskip4_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 4; FrameSkipMessage(); }
		private void miFrameskip5_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 5; FrameSkipMessage(); }
		private void miFrameskip6_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 6; FrameSkipMessage(); }
		private void miFrameskip7_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 7; FrameSkipMessage(); }
		private void miFrameskip8_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 8; FrameSkipMessage(); }
		private void miFrameskip9_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 9; FrameSkipMessage(); }

		public void FrameSkipMessage()
		{
			GlobalWinF.OSD.AddMessage("Frameskipping set to " + Global.Config.FrameSkip.ToString());
		}

		public void ClickSpeedItem(int num)
		{
			if ((ModifierKeys & Keys.Control) != 0) SetSpeedPercentAlternate(num);
			else SetSpeedPercent(num);
		}
		private void miSpeed50_Click(object sender, EventArgs e) { ClickSpeedItem(50); }
		private void miSpeed75_Click(object sender, EventArgs e) { ClickSpeedItem(75); }
		private void miSpeed100_Click(object sender, EventArgs e) { ClickSpeedItem(100); }
		private void miSpeed150_Click(object sender, EventArgs e) { ClickSpeedItem(150); }
		private void miSpeed200_Click(object sender, EventArgs e) { ClickSpeedItem(200); }

		private void pauseWhenMenuActivatedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PauseWhenMenuActivated ^= true;
		}

		private void soundToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenSoundConfigDialog();
		}

		private void OpenSoundConfigDialog()
		{
			SoundConfig s = new SoundConfig();
			var result = s.ShowDialog();
			if (result == DialogResult.OK)
				RewireSound();
		}

		private void zoomMenuItem_Click(object sender, EventArgs e)
		{
			if (sender == x1MenuItem) Global.Config.TargetZoomFactor = 1;
			if (sender == x2MenuItem) Global.Config.TargetZoomFactor = 2;
			if (sender == x3MenuItem) Global.Config.TargetZoomFactor = 3;
			if (sender == x4MenuItem) Global.Config.TargetZoomFactor = 4;
			if (sender == x5MenuItem) Global.Config.TargetZoomFactor = 5;
			if (sender == mzMenuItem) Global.Config.TargetZoomFactor = 10;

			x1MenuItem.Checked = Global.Config.TargetZoomFactor == 1;
			x2MenuItem.Checked = Global.Config.TargetZoomFactor == 2;
			x3MenuItem.Checked = Global.Config.TargetZoomFactor == 3;
			x4MenuItem.Checked = Global.Config.TargetZoomFactor == 4;
			x5MenuItem.Checked = Global.Config.TargetZoomFactor == 5;
			mzMenuItem.Checked = Global.Config.TargetZoomFactor == 10;

			FrameBufferResized();
		}

		private void DisplayFilterMenuItem_Click(object sender, EventArgs e)
		{
			if (sender == DisplayFilterNoneMenuItem) Global.Config.TargetDisplayFilter = 0;
			if (sender == x2SAIMenuItem) Global.Config.TargetDisplayFilter = 1;
			if (sender == SuperX2SAIMenuItem) Global.Config.TargetDisplayFilter = 2;
			if (sender == SuperEagleMenuItem) Global.Config.TargetDisplayFilter = 3;

			DisplayFilterNoneMenuItem.Checked = Global.Config.TargetDisplayFilter == 0;
			x2SAIMenuItem.Checked = Global.Config.TargetDisplayFilter == 1;
			SuperX2SAIMenuItem.Checked = Global.Config.TargetDisplayFilter == 2;
			SuperEagleMenuItem.Checked = Global.Config.TargetDisplayFilter == 3;
		}

		private void smsEnableFMChipToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SmsEnableFM ^= true;
			FlagNeedsReboot();
		}

		private void smsOverclockWhenKnownSafeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SmsAllowOverlock ^= true;
			FlagNeedsReboot();
		}

		private void smsForceStereoSeparationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SmsForceStereoSeparation ^= true;
			FlagNeedsReboot();
		}

		private void smsSpriteLimitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SmsSpriteLimit ^= true;
			FlagNeedsReboot();
		}

		private void pceAlwaysPerformSpriteLimitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PceSpriteLimit ^= true;
			FlagNeedsReboot();
		}

		private void pceAlwayEqualizeVolumesLimitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PceEqualizeVolume ^= true;
			FlagNeedsReboot();
		}

		private void pceArcadeCardRewindEnableHackToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.PceArcadeCardRewindHack ^= true;
			FlagNeedsReboot();
		}


		private void RAMWatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadRamWatch(true);
		}

		private void rAMSearchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadRamSearch();
		}

		private void powerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RebootCore();
		}

		public void RebootCore()
		{
			LoadRom(CurrentlyOpenRom);
		}

		private void resetToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SoftReset();
		}

		private void hardResetToolStripMenuItem_Click(object sender, EventArgs e)
		{
			HardReset();
		}

		private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (EmulatorPaused)
				UnpauseEmulator();
			else
				PauseEmulator();
		}

		private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/BizHawk.html");
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (INTERIM)
				new AboutBox().ShowDialog();
			else
				new BizBox().ShowDialog();
		}

		private void controllersToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenControllerConfig();
		}

		private void OpenControllerConfig()
		{
			ControllerConfig c = new ControllerConfig(Global.Emulator.ControllerDefinition);
			c.ShowDialog();
			if (c.DialogResult == DialogResult.OK)
			{
				InitControls();
				SyncControls();
			}
		}

		private void hotkeysToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenHotkeyDialog();
		}

		private void OpenHotkeyDialog()
		{
			HotkeyConfig h = new HotkeyConfig();
			h.ShowDialog();
			if (h.DialogResult == DialogResult.OK)
			{
				InitControls();
				SyncControls();
			}
		}

		private void displayFPSToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			ToggleFPS();
		}

		private void displayFrameCounterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			ToggleFrameCounter();
		}

		private void displayInputToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			ToggleInputDisplay();
		}

		private void displayLagCounterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			ToggleLagCounter();
		}

		private void forumsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/forum/viewforum.php?f=64");
		}

		private void PPUViewerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadNESPPU();
		}

		private void hexEditorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadHexEditor();
		}

		private void MainForm_Shown(object sender, EventArgs e)
		{
			HandlePlatformMenus();
		}

		private void gameGenieCodesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadGameGenieEC();
		}

		private void cheatsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadCheatsWindow();
		}

		private void forceGDIPPresentationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisplayGDI ^= true;
			SyncPresentationMode();
		}

		private void miSuppressGuiLayer_Click(object sender, EventArgs e)
		{
			Global.Config.SuppressGui ^= true;
		}

		private void debuggerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadNESDebugger();
		}

		private void nametableViewerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadNESNameTable();
		}

		private void toolBoxToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadToolBox();
		}

		private void toolsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			toolBoxToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["ToolBox"].Bindings;
			rAMWatchToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Ram Watch"].Bindings;
			rAMSearchToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Ram Search"].Bindings;
			hexEditorToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Hex Editor"].Bindings;
			luaConsoleToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Lua Console"].Bindings;
			cheatsToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Cheats"].Bindings;
			tAStudioToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["TAStudio"].Bindings;
			virtualPadToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Virtual Pad"].Bindings;
			traceLoggerToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Trace Logger"].Bindings;
			toolBoxToolStripMenuItem.Enabled = !ToolBox1.IsHandleCreated || ToolBox1.IsDisposed;
			traceLoggerToolStripMenuItem.Enabled = Global.Emulator.CoreComm.CpuTraceAvailable;

			cheatsToolStripMenuItem.Enabled = !(Global.Emulator is NullEmulator);
		}

		private void switchToFullscreenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFullscreen();
		}

		private void messagesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			new MessageConfig().ShowDialog();
		}

		private void autoloadVirtualKeyboardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!(Global.Emulator is TI83)) return;
			Global.Config.TI83autoloadKeyPad ^= true;
		}

		private void keypadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!(Global.Emulator is TI83))
				return;
			LoadTI83KeyPad();
		}

		private void tI83ToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			autoloadVirtualKeyboardToolStripMenuItem.Checked = Global.Config.TI83autoloadKeyPad;

			if (!MainForm.INTERIM) loadTIFileToolStripMenuItem.Visible = false;
		}

		private void pathsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			new PathConfig().ShowDialog();
		}

		private void displayRerecordCountToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			Global.Config.DisplayRerecordCount ^= true;
		}

		private void runInBackgroundToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RunInBackground ^= true;
		}

		private void acceptBackgroundInputToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleBackgroundInput();
		}

		public void ToggleBackgroundInput()
		{
			Global.Config.AcceptBackgroundInput ^= true;
			if (Global.Config.AcceptBackgroundInput)
			{
				GlobalWinF.OSD.AddMessage("Background Input enabled");
			}
			else
			{
				GlobalWinF.OSD.AddMessage("Background Input disabled");
			}
		}

		private void displayStatusBarToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisplayStatusBar ^= true;
			displayStatusBarToolStripMenuItem.Checked = Global.Config.DisplayStatusBar;
			if (!InFullscreen)
			{
				StatusSlot0.Visible = Global.Config.DisplayStatusBar;
				PerformLayout();
				FrameBufferResized();
			}
		}

		private void graphicsSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NESGraphicsConfig g = new NESGraphicsConfig();
			g.ShowDialog();
			SyncCoreCommInputSignals();
		}

		private void pceGraphicsSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PCEGraphicsConfig g = new PCEGraphicsConfig();
			g.ShowDialog();
			SyncCoreCommInputSignals();
		}

		private void smsGraphicsSettingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SMSGraphicsConfig g = new SMSGraphicsConfig();
			g.ShowDialog();
			SyncCoreCommInputSignals();
		}

		public void MainForm_MouseClick(object sender, MouseEventArgs e)
		{
			if (Global.Config.ShowContextMenu && e.Button == MouseButtons.Right)
			{
				Point p = new Point(e.X, e.Y + MainformMenu.Height);
				Point po = PointToScreen(p);
				contextMenuStrip1.Show(po);
			}
		}

		private void openRomToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			OpenROM();
		}

		private void loadLastROMToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadRomFromRecent(Global.Config.RecentRoms[0]);
		}

		private void enableContextMenuToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.ShowContextMenu ^= true;
			if (Global.Config.ShowContextMenu)
			{
				GlobalWinF.OSD.AddMessage("Context menu enabled");
			}
			else
			{
				GlobalWinF.OSD.AddMessage("Context menu disabled");
			}
		}

		private void recordMovieToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			RecordMovie();
		}

		private void playMovieToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			PlayMovie();
		}

		private void loadLastMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadMoviesFromRecent(Global.Config.RecentMovies[0]);
		}

		private void AddSubtitleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SubtitleMaker s = new SubtitleMaker();
			s.DisableFrame();
			int index = -1;
			Subtitle sub = new Subtitle();
			for (int x = 0; x < Global.MovieSession.Movie.Subtitles.Count; x++)
			{
				sub = Global.MovieSession.Movie.Subtitles[x];
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
			s.sub = sub;

			if (s.ShowDialog() == DialogResult.OK)
			{
				if (index >= 0)
					Global.MovieSession.Movie.Subtitles.RemoveAt(index);
				Global.MovieSession.Movie.Subtitles.AddSubtitle(s.sub);
			}
		}

		private void closeROMToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			CloseROM();
		}


		private void screenshotToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TakeScreenshotToClipboard();
		}

		private void restartMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PlayMovieFromBeginning();
		}

		private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			ClearSRAMContextSeparator.Visible =
				ClearSRAMContextMenuItem.Visible
				= File.Exists(PathManager.SaveRamPath(Global.Game));
			
			wasPaused = EmulatorPaused;
			didMenuPause = true;
			PauseEmulator();

			//TODO - MUST refactor this to hide all and then view a set depending on the state

			configToolStripMenuItem1.Visible = InFullscreen;

			if (IsNullEmulator())
			{
				cmiOpenRom.Visible = true;
				cmiLoadLastRom.Visible = true;
				toolStripSeparator_afterRomLoading.Visible = false;
				cmiRecordMovie.Visible = false;
				cmiPlayMovie.Visible = false;
				cmiRestartMovie.Visible = false;
				cmiStopMovie.Visible = false;
				cmiLoadLastMovie.Visible = false;
				cmiMakeMovieBackup.Visible = false;
				cmiViewSubtitles.Visible = false;
				cmiViewComments.Visible = false;
				toolStripSeparator_afterMovie.Visible = false;
				cmiAddSubtitle.Visible = false;
				cmiUndoSavestate.Visible = false;
				cmiSeparator20.Visible = false;
				cmiScreenshot.Visible = false;
				cmiScreenshotClipboard.Visible = false;
				cmiCloseRom.Visible = false;
				cmiShowMenu.Visible = false;
				ShowMenuContextMenuSeparator.Visible = false;
				saveMovieToolStripMenuItem1.Visible = false;
			}
			else
			{
				cmiOpenRom.Visible = InFullscreen;
				configToolStripMenuItem1.Visible = InFullscreen;

				cmiLoadLastRom.Visible = false;
				toolStripSeparator_afterRomLoading.Visible = false;

				if (Global.MovieSession.Movie.IsActive)
				{
					cmiRecordMovie.Visible = false;
					cmiPlayMovie.Visible = false;
					cmiRestartMovie.Visible = true;
					cmiStopMovie.Visible = true;
					cmiLoadLastMovie.Visible = false;
					cmiMakeMovieBackup.Visible = true;
					cmiViewSubtitles.Visible = true;
					cmiViewComments.Visible = true;
					saveMovieToolStripMenuItem1.Visible = true;
					toolStripSeparator_afterMovie.Visible = true;
					if (ReadOnly)
					{
						cmiViewSubtitles.Text = "View Subtitles";
						cmiViewComments.Text = "View Comments";
						cmiAddSubtitle.Visible = false;
					}
					else
					{
						cmiViewSubtitles.Text = "Edit Subtitles";
						cmiViewComments.Text = "Edit Comments";
						cmiAddSubtitle.Visible = true;
					}
				}
				else
				{
					cmiRecordMovie.Visible = true;
					cmiPlayMovie.Visible = true;
					cmiRestartMovie.Visible = false;
					cmiStopMovie.Visible = false;
					cmiLoadLastMovie.Visible = true;
					cmiMakeMovieBackup.Visible = false;
					cmiViewSubtitles.Visible = false;
					cmiViewComments.Visible = false;
					toolStripSeparator_afterMovie.Visible = true;
					cmiAddSubtitle.Visible = false;
					saveMovieToolStripMenuItem1.Visible = false;
				}

				cmiUndoSavestate.Visible = true;
				cmiSeparator20.Visible = true;
				cmiScreenshot.Visible = true;
				cmiScreenshotClipboard.Visible = true;
				cmiCloseRom.Visible = true;
			}

			cmiLoadLastRom.Enabled = !Global.Config.RecentRoms.Empty;
			cmiLoadLastMovie.Enabled = !Global.Config.RecentMovies.Empty;

			string path = PathManager.SaveStatePrefix(Global.Game) + "." + "QuickSave" + Global.Config.SaveSlot + ".State.bak";
			var file = new FileInfo(path);
			if (file.Exists)
			{
				if (StateSlots.IsRedo(Global.Config.SaveSlot))
				{
					cmiUndoSavestate.Enabled = true;
					cmiUndoSavestate.Text = "Redo Save to slot " + Global.Config.SaveSlot.ToString();
					cmiUndoSavestate.Image = Properties.Resources.redo;
				}
				else
				{
					cmiUndoSavestate.Enabled = true;
					cmiUndoSavestate.Text = "Undo Save to slot " + Global.Config.SaveSlot.ToString();
					cmiUndoSavestate.Image = Properties.Resources.undo;
				}
			}
			else
			{
				cmiUndoSavestate.Enabled = false;
				cmiUndoSavestate.Text = "Undo Savestate";
				cmiUndoSavestate.Image = Properties.Resources.undo;
			}

			if (InFullscreen)
			{
				ShowMenuContextMenuSeparator.Visible = cmiShowMenu.Visible = true;
				if (MainMenuStrip.Visible)
					cmiShowMenu.Text = "Hide Menu";
				else
					cmiShowMenu.Text = "Show Menu";
			}
			else
			{
				ShowMenuContextMenuSeparator.Visible = cmiShowMenu.Visible = false;
			}

			ContextMenuStopMovieNoSaving.Visible = Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie.HasChanges;
		}


		private void contextMenuStrip1_Closing(object sender, ToolStripDropDownClosingEventArgs e)
		{
			if (!wasPaused)
			{
				UnpauseEmulator();
			}
		}

		private void makeMovieBackupToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWinF.OSD.AddMessage("Backup movie saved.");
			Global.MovieSession.Movie.WriteBackup();
		}

		private void stopMovieToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			StopMovie();
		}

		private void displayLogWindowToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.ShowLogWindow ^= true;
			displayLogWindowToolStripMenuItem.Checked = Global.Config.ShowLogWindow;
			if (Global.Config.ShowLogWindow)
				ShowConsole();
			else
				HideConsole();
		}

		private void PauseStrip_Click(object sender, EventArgs e)
		{
			TogglePause();
		}

		private void displaySubtitlesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			Global.Config.DisplaySubtitles ^= true;
		}

		private void viewCommentsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				EditCommentsForm c = new EditCommentsForm { ReadOnly = ReadOnly };
				c.GetMovie(Global.MovieSession.Movie);
				c.ShowDialog();
			}
		}

		private void viewSubtitlesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				EditSubtitlesForm s = new EditSubtitlesForm { ReadOnly = ReadOnly };
				s.GetMovie(Global.MovieSession.Movie);
				s.ShowDialog();
			}
		}

		private void tAStudioToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadTAStudio();
		}

		private void singleInstanceModeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SingleInstanceMode ^= true;

			MessageBox.Show("BizHawk must be restarted for this setting to take effect.", "Reboot Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void MainForm_Deactivate(object sender, EventArgs e)
		{
			if (!Global.Config.RunInBackground)
			{
				if (EmulatorPaused)
				{
					wasPaused = true;
				}
				PauseEmulator();
			}
		}

		private void MainForm_Activated(object sender, EventArgs e)
		{
			if (!Global.Config.RunInBackground)
			{

				if (!wasPaused)
				{
					UnpauseEmulator();
				}
				wasPaused = false;
			}
		}

		private void saveConfigToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveConfig();
			GlobalWinF.OSD.AddMessage("Saved settings");
		}

		private void loadConfigToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config = ConfigService.Load(PathManager.DefaultIniPath, Global.Config);
			Global.Config.ResolveDefaults();
			GlobalWinF.OSD.AddMessage("Config file loaded");
		}

		private void frameSkipToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			miAutoMinimizeSkipping.Checked = Global.Config.AutoMinimizeSkipping;
			miLimitFramerate.Checked = Global.Config.ClockThrottle;
			miDisplayVsync.Checked = Global.Config.VSyncThrottle;
			miFrameskip0.Checked = Global.Config.FrameSkip == 0;
			miFrameskip1.Checked = Global.Config.FrameSkip == 1;
			miFrameskip2.Checked = Global.Config.FrameSkip == 2;
			miFrameskip3.Checked = Global.Config.FrameSkip == 3;
			miFrameskip4.Checked = Global.Config.FrameSkip == 4;
			miFrameskip5.Checked = Global.Config.FrameSkip == 5;
			miFrameskip6.Checked = Global.Config.FrameSkip == 6;
			miFrameskip7.Checked = Global.Config.FrameSkip == 7;
			miFrameskip8.Checked = Global.Config.FrameSkip == 8;
			miFrameskip9.Checked = Global.Config.FrameSkip == 9;
			miAutoMinimizeSkipping.Enabled = !miFrameskip0.Checked;
			if (!miAutoMinimizeSkipping.Enabled) miAutoMinimizeSkipping.Checked = true;
			audioThrottleToolStripMenuItem.Enabled = Global.Config.SoundEnabled;
			audioThrottleToolStripMenuItem.Checked = Global.Config.SoundThrottle;
			vSyncEnabledToolStripMenuItem.Checked = Global.Config.VSync;

			miSpeed100.Checked = Global.Config.SpeedPercent == 100;
			miSpeed100.Image = (Global.Config.SpeedPercentAlternate == 100) ? Properties.Resources.FastForward : null;
			miSpeed150.Checked = Global.Config.SpeedPercent == 150;
			miSpeed150.Image = (Global.Config.SpeedPercentAlternate == 150) ? Properties.Resources.FastForward : null;
			miSpeed200.Checked = Global.Config.SpeedPercent == 200;
			miSpeed200.Image = (Global.Config.SpeedPercentAlternate == 200) ? Properties.Resources.FastForward : null;
			miSpeed75.Checked = Global.Config.SpeedPercent == 75;
			miSpeed75.Image = (Global.Config.SpeedPercentAlternate == 75) ? Properties.Resources.FastForward : null;
			miSpeed50.Checked = Global.Config.SpeedPercent == 50;
			miSpeed50.Image = (Global.Config.SpeedPercentAlternate == 50) ? Properties.Resources.FastForward : null;
		}

		private void gUIToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			pauseWhenMenuActivatedToolStripMenuItem.Checked = Global.Config.PauseWhenMenuActivated;
			startPausedToolStripMenuItem.Checked = Global.Config.StartPaused;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.SaveWindowPosition;
			forceGDIPPresentationToolStripMenuItem.Checked = Global.Config.DisplayGDI;
			blurryToolStripMenuItem.Checked = Global.Config.DispBlurry;
			miSuppressGuiLayer.Checked = Global.Config.SuppressGui;
			showMenuInFullScreenToolStripMenuItem.Checked = Global.Config.ShowMenuInFullscreen;
			runInBackgroundToolStripMenuItem.Checked = Global.Config.RunInBackground;
			acceptBackgroundInputToolStripMenuItem.Checked = Global.Config.AcceptBackgroundInput;
			singleInstanceModeToolStripMenuItem.Checked = Global.Config.SingleInstanceMode;
			logWindowAsConsoleToolStripMenuItem.Checked = Global.Config.WIN32_CONSOLE;
			neverBeAskedToSaveChangesToolStripMenuItem.Checked = Global.Config.SupressAskSave;

			acceptBackgroundInputToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle BG Input"].Bindings;
		}

		private void enableToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			enableContextMenuToolStripMenuItem.Checked = Global.Config.ShowContextMenu;
			backupSavestatesToolStripMenuItem.Checked = Global.Config.BackupSavestates;
			autoSavestatesToolStripMenuItem.Checked = Global.Config.AutoSavestates;
			saveScreenshotWithSavestatesToolStripMenuItem.Checked = Global.Config.SaveScreenshotWithStates;
			frameAdvanceSkipLagFramesToolStripMenuItem.Checked = Global.Config.SkipLagFrame;
			backupSaveramToolStripMenuItem.Checked = Global.Config.BackupSaveram;
		}

		private void frameAdvanceSkipLagFramesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SkipLagFrame ^= true;
		}

		private void menuStrip1_MenuActivate(object sender, EventArgs e)
		{
			HandlePlatformMenus();
			if (Global.Config.PauseWhenMenuActivated)
			{
				if (EmulatorPaused)
					wasPaused = true;
				else
					wasPaused = false;
				didMenuPause = true;
				PauseEmulator();
			}
		}

		private void menuStrip1_MenuDeactivate(object sender, EventArgs e)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
			if (!wasPaused)
			{
				UnpauseEmulator();
			}
		}

		private void viewToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			displayFPSToolStripMenuItem.Checked = Global.Config.DisplayFPS;
			displayFrameCounterToolStripMenuItem.Checked = Global.Config.DisplayFrameCounter;
			displayLagCounterToolStripMenuItem.Checked = Global.Config.DisplayLagCounter;
			displayInputToolStripMenuItem.Checked = Global.Config.DisplayInput;
			displayRerecordCountToolStripMenuItem.Checked = Global.Config.DisplayRerecordCount;
			displaySubtitlesToolStripMenuItem.Checked = Global.Config.DisplaySubtitles;

			displayFPSToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Display FPS"].Bindings;
			displayFrameCounterToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Frame Counter"].Bindings;
			displayLagCounterToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Lag Counter"].Bindings;
			displayInputToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Input Display"].Bindings;
			switchToFullscreenToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Full Screen"].Bindings;

			x1MenuItem.Checked = false;
			x2MenuItem.Checked = false;
			x3MenuItem.Checked = false;
			x4MenuItem.Checked = false;
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

		private void emulationToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			rebootCoreToolStripMenuItem.Enabled = !IsNullEmulator();

			resetToolStripMenuItem.Enabled = Global.Emulator.ControllerDefinition.BoolButtons.Contains("Reset") &&
					(!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished);


			hardResetToolStripMenuItem.Enabled = Global.Emulator.ControllerDefinition.BoolButtons.Contains("Power") &&
				(!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished);

			pauseToolStripMenuItem.Checked = EmulatorPaused;
			if (didMenuPause)
			{
				pauseToolStripMenuItem.Checked = wasPaused;
			}

			pauseToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Pause"].Bindings;
			rebootCoreToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Reboot Core"].Bindings;
			resetToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Soft Reset"].Bindings;
			hardResetToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Hard Reset"].Bindings;
		}

		private void pCEToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			pceAlwaysPerformSpriteLimitToolStripMenuItem.Checked = Global.Config.PceSpriteLimit;
			pceAlwaysEqualizeVolumesToolStripMenuItem.Checked = Global.Config.PceEqualizeVolume;
			pceArcadeCardRewindEnableHackToolStripMenuItem.Checked = Global.Config.PceArcadeCardRewindHack;
		}

		private void sMSToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			smsEnableFMChipToolStripMenuItem.Checked = Global.Config.SmsEnableFM;
			smsOverclockWhenKnownSafeToolStripMenuItem.Checked = Global.Config.SmsAllowOverlock;
			smsForceStereoSeparationToolStripMenuItem.Checked = Global.Config.SmsForceStereoSeparation;
			smsSpriteLimitToolStripMenuItem.Checked = Global.Config.SmsSpriteLimit;
			showClippedRegionsToolStripMenuItem.Checked = Global.Config.GGShowClippedRegions;
			highlightActiveDisplayRegionToolStripMenuItem.Checked = Global.Config.GGHighlightActiveDisplayRegion;

			if (Global.Game.System == "GG")
			{
				smsEnableFMChipToolStripMenuItem.Visible = false;
				smsOverclockWhenKnownSafeToolStripMenuItem.Visible = false;
				smsForceStereoSeparationToolStripMenuItem.Visible = false;

				showClippedRegionsToolStripMenuItem.Visible = true;
				highlightActiveDisplayRegionToolStripMenuItem.Visible = true;
				GGgameGenieEncoderDecoderToolStripMenuItem.Visible = true;
			}
			else
			{
				smsEnableFMChipToolStripMenuItem.Visible = true;
				smsOverclockWhenKnownSafeToolStripMenuItem.Visible = true;
				smsForceStereoSeparationToolStripMenuItem.Visible = true;

				showClippedRegionsToolStripMenuItem.Visible = false;
				highlightActiveDisplayRegionToolStripMenuItem.Visible = false;
				GGgameGenieEncoderDecoderToolStripMenuItem.Visible = false;
			}

		}

		protected override void OnClosed(EventArgs e)
		{
			exit = true;
			base.OnClosed(e);
		}

		private void backupSavestatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.BackupSavestates ^= true;
			if (Global.Config.BackupSavestates)
			{
				GlobalWinF.OSD.AddMessage("Backup savestates enabled");
			}
			else
			{
				GlobalWinF.OSD.AddMessage("Backup savestates disabled");
			}
		}

		void autoSavestatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoSavestates ^= true;
			if (Global.Config.AutoSavestates)
			{
				GlobalWinF.OSD.AddMessage("AutoSavestates enabled");
			}
			else
			{
				GlobalWinF.OSD.AddMessage("AutoSavestates disabled");
			}
		}

		void screenshotWithSavestatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveScreenshotWithStates ^= true;
			if (Global.Config.SaveScreenshotWithStates)
			{
				GlobalWinF.OSD.AddMessage("Screenshots will be saved in savestates");
			}
			else
			{
				GlobalWinF.OSD.AddMessage("Screenshots will not be saved in savestates");
			}
		}

		private void undoSavestateToolStripMenuItem_Click(object sender, EventArgs e)
		{
			string path = PathManager.SaveStatePrefix(Global.Game) + "." + "QuickSave" + Global.Config.SaveSlot + ".State";
			SwapBackupSavestate(path);
			GlobalWinF.OSD.AddMessage("Save slot " + Global.Config.SaveSlot.ToString() + " restored.");
		}

		private void FreezeStatus_Click(object sender, EventArgs e)
		{
			if (CheatStatus.Visible)
			{
				LoadCheatsWindow();
			}
		}

		public void UpdateCheatStatus()
		{
			if (Global.CheatList.ActiveCount > 0)
			{
				CheatStatus.ToolTipText = "Cheats are currently active";
				CheatStatus.Image = Properties.Resources.Freeze;
				CheatStatus.Visible = true;
			}
			else
			{
				CheatStatus.ToolTipText = "";
				CheatStatus.Image = Properties.Resources.Blank;
				CheatStatus.Visible = false;
			}
		}

		private void autofireToolStripMenuItem_Click(object sender, EventArgs e)
		{
			new AutofireConfig().ShowDialog();
		}

		private void logWindowAsConsoleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.WIN32_CONSOLE ^= true;
		}

		private void showMenuInFullScreenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.ShowMenuInFullscreen ^= true;
		}

		private void showMenuToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ShowHideMenu();
		}

		private void justatestToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadPCEBGViewer();
		}

		private void bWToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is Atari2600)
			{
				Global.Config.Atari2600_BW ^= true;
				((Atari2600)Global.Emulator).SetBw(Global.Config.Atari2600_BW);
				if (Global.Config.Atari2600_BW)
					GlobalWinF.OSD.AddMessage("Setting to Black and White Switch to On");
				else
					GlobalWinF.OSD.AddMessage("Setting to Black and White Switch to Off");
			}
		}

		private void p0DifficultyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is Atari2600)
			{
				Global.Config.Atari2600_LeftDifficulty ^= true;
				((Atari2600)Global.Emulator).SetP0Diff(Global.Config.Atari2600_BW);
				if (Global.Config.Atari2600_LeftDifficulty)
					GlobalWinF.OSD.AddMessage("Setting Left Difficulty to B");
				else
					GlobalWinF.OSD.AddMessage("Setting Left Difficulty to A");
			}
		}

		private void rightDifficultyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is Atari2600)
			{
				Global.Config.Atari2600_RightDifficulty ^= true;
				((Atari2600)Global.Emulator).SetP1Diff(Global.Config.Atari2600_BW);
				if (Global.Config.Atari2600_RightDifficulty)
					GlobalWinF.OSD.AddMessage("Setting Right Difficulty to B");
				else
					GlobalWinF.OSD.AddMessage("Setting Right Difficulty to A");
			}
		}

		private void atariToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			bWToolStripMenuItem.Checked = Global.Config.Atari2600_BW;
			p0DifficultyToolStripMenuItem.Checked = Global.Config.Atari2600_LeftDifficulty;
			rightDifficultyToolStripMenuItem.Checked = Global.Config.Atari2600_RightDifficulty;

			showBGToolStripMenuItem.Checked = Global.Config.Atari2600_ShowBG;
			showPlayer1ToolStripMenuItem.Checked = Global.Config.Atari2600_ShowPlayer1;
			showPlayer2ToolStripMenuItem.Checked = Global.Config.Atari2600_ShowPlayer2;
			showMissle1ToolStripMenuItem.Checked = Global.Config.Atari2600_ShowMissle1;
			showMissle2ToolStripMenuItem.Checked = Global.Config.Atari2600_ShowMissle2;
			showBallToolStripMenuItem.Checked = Global.Config.Atari2600_ShowBall;
			showPlayfieldToolStripMenuItem.Checked = Global.Config.Atari2600_ShowPlayfield;
		}

		private void gBToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			forceDMGModeToolStripMenuItem.Checked = Global.Config.GB_ForceDMG;
			gBAInCGBModeToolStripMenuItem.Checked = Global.Config.GB_GBACGB;
			multicartCompatibilityToolStripMenuItem.Checked = Global.Config.GB_MulticartCompat;

			loadGBInSGBToolStripMenuItem1.Checked = Global.Config.GB_AsSGB;
		}

		private void graphicsDebuggerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadSNESGraphicsDebugger();
		}

		private void miSnesOptions_Click(object sender, EventArgs e)
		{
			var so = new SNESOptions
				{
					UseRingBuffer = Global.Config.SNESUseRingBuffer,
					AlwaysDoubleSize = Global.Config.SNESAlwaysDoubleSize,
					Profile = Global.Config.SNESProfile
				};
			if (so.ShowDialog() == DialogResult.OK)
			{
				bool reboot = Global.Config.SNESProfile != so.Profile;
				Global.Config.SNESProfile = so.Profile;
				Global.Config.SNESUseRingBuffer = so.UseRingBuffer;
				Global.Config.SNESAlwaysDoubleSize = so.AlwaysDoubleSize;
				if (reboot) FlagNeedsReboot();
				SyncCoreCommInputSignals();
			}
		}

		public void SNES_ToggleBG1(bool? setto = null)
		{
			if (Global.Emulator is LibsnesCore)
			{
				if (setto.HasValue)
				{
					Global.Config.SNES_ShowBG1_1 = Global.Config.SNES_ShowBG1_0 = setto.Value;
				}
				else
				{
					Global.Config.SNES_ShowBG1_1 = Global.Config.SNES_ShowBG1_0 ^= true;
				}

				SyncCoreCommInputSignals();
				if (Global.Config.SNES_ShowBG1_1)
				{
					GlobalWinF.OSD.AddMessage("BG 1 Layer On");
				}
				else
				{
					GlobalWinF.OSD.AddMessage("BG 1 Layer Off");
				}
			}
		}

		public void SNES_ToggleBG2(bool? setto = null)
		{
			if (Global.Emulator is LibsnesCore)
			{
				if (setto.HasValue)
				{
					Global.Config.SNES_ShowBG2_1 = Global.Config.SNES_ShowBG2_0 = setto.Value;
				}
				else
				{
					Global.Config.SNES_ShowBG2_1 = Global.Config.SNES_ShowBG2_0 ^= true;
				}
				SyncCoreCommInputSignals();
				if (Global.Config.SNES_ShowBG2_1)
				{
					GlobalWinF.OSD.AddMessage("BG 2 Layer On");
				}
				else
				{
					GlobalWinF.OSD.AddMessage("BG 2 Layer Off");
				}
			}
		}

		public void SNES_ToggleBG3(bool? setto = null)
		{
			if (Global.Emulator is LibsnesCore)
			{
				if (setto.HasValue)
				{
					Global.Config.SNES_ShowBG3_1 = Global.Config.SNES_ShowBG3_0 = setto.Value;
				}
				else
				{
					Global.Config.SNES_ShowBG3_1 = Global.Config.SNES_ShowBG3_0 ^= true;
				}
				SyncCoreCommInputSignals();
				if (Global.Config.SNES_ShowBG3_1)
				{
					GlobalWinF.OSD.AddMessage("BG 3 Layer On");
				}
				else
				{
					GlobalWinF.OSD.AddMessage("BG 3 Layer Off");
				}
			}
		}

		public void SNES_ToggleBG4(bool? setto = null)
		{
			if (Global.Emulator is LibsnesCore)
			{
				if (setto.HasValue)
				{
					Global.Config.SNES_ShowBG4_1 = Global.Config.SNES_ShowBG4_0 = setto.Value;
				}
				else
				{
					Global.Config.SNES_ShowBG4_1 = Global.Config.SNES_ShowBG4_0 ^= true;
				}
				SyncCoreCommInputSignals();
				if (Global.Config.SNES_ShowBG4_1)
				{
					GlobalWinF.OSD.AddMessage("BG 4 Layer On");
				}
				else
				{
					GlobalWinF.OSD.AddMessage("BG 4 Layer Off");
				}
			}
		}

		public void SNES_ToggleOBJ1(bool? setto = null)
		{
			if (Global.Emulator is LibsnesCore)
			{
				if (setto.HasValue)
				{
					Global.Config.SNES_ShowOBJ1 = setto.Value;
				}
				else
				{
					Global.Config.SNES_ShowOBJ1 ^= true;
				}
				SyncCoreCommInputSignals();
				if (Global.Config.SNES_ShowOBJ1)
				{
					GlobalWinF.OSD.AddMessage("OBJ 1 Layer On");
				}
				else
				{
					GlobalWinF.OSD.AddMessage("OBJ 1 Layer Off");
				}
			}
		}

		public void SNES_ToggleOBJ2(bool? setto = null)
		{
			if (Global.Emulator is LibsnesCore)
			{
				if (setto.HasValue)
				{
					Global.Config.SNES_ShowOBJ2 = setto.Value;
				}
				else
				{
					Global.Config.SNES_ShowOBJ2 ^= true;
				}
				SyncCoreCommInputSignals();
				if (Global.Config.SNES_ShowOBJ2)
				{
					GlobalWinF.OSD.AddMessage("OBJ 2 Layer On");
				}
				else
				{
					GlobalWinF.OSD.AddMessage("OBJ 2 Layer Off");
				}
			}
		}

		public void SNES_ToggleOBJ3(bool? setto = null)
		{
			if (Global.Emulator is LibsnesCore)
			{
				if (setto.HasValue)
				{
					Global.Config.SNES_ShowOBJ3 = setto.Value;
				}
				else
				{
					Global.Config.SNES_ShowOBJ3 ^= true;
				}
				SyncCoreCommInputSignals();
				if (Global.Config.SNES_ShowOBJ3)
				{
					GlobalWinF.OSD.AddMessage("OBJ 3 Layer On");
				}
				else
				{
					GlobalWinF.OSD.AddMessage("OBJ 3 Layer Off");
				}
			}
		}

		public void SNES_ToggleOBJ4(bool? setto = null)
		{
			if (Global.Emulator is LibsnesCore)
			{
				if (setto.HasValue)
				{
					Global.Config.SNES_ShowOBJ4 = setto.Value;
				}
				else
				{
					Global.Config.SNES_ShowOBJ4 ^= true;
				}
				SyncCoreCommInputSignals();
				if (Global.Config.SNES_ShowOBJ4)
				{
					GlobalWinF.OSD.AddMessage("OBJ 4 Layer On");
				}
				else
				{
					GlobalWinF.OSD.AddMessage("OBJ 4 Layer Off");
				}
			}
		}

		private void bG1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SNES_ToggleBG1();
		}

		private void bG1ToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			SNES_ToggleBG2();
		}

		private void bG2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SNES_ToggleBG3();
		}

		private void bG3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SNES_ToggleBG4();
		}

		private void oBJ0ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SNES_ToggleOBJ1();
		}

		private void oBJ1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SNES_ToggleOBJ2();
		}

		private void oBJ2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SNES_ToggleOBJ3();
		}

		private void oBJ3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SNES_ToggleOBJ4();
		}

		private void displayToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			bG0ToolStripMenuItem.Checked = Global.Config.SNES_ShowBG1_1;
			bG1ToolStripMenuItem.Checked = Global.Config.SNES_ShowBG2_1;
			bG2ToolStripMenuItem.Checked = Global.Config.SNES_ShowBG3_1;
			bG3ToolStripMenuItem.Checked = Global.Config.SNES_ShowBG4_1;

			oBJ0ToolStripMenuItem.Checked = Global.Config.SNES_ShowOBJ1;
			oBJ1ToolStripMenuItem.Checked = Global.Config.SNES_ShowOBJ2;
			oBJ2ToolStripMenuItem.Checked = Global.Config.SNES_ShowOBJ3;
			oBJ3ToolStripMenuItem.Checked = Global.Config.SNES_ShowOBJ4;

			bG0ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle BG 1"].Bindings;
			bG1ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle BG 2"].Bindings;
			bG2ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle BG 3"].Bindings;
			bG3ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle BG 4"].Bindings;

			oBJ0ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle OBJ 1"].Bindings;
			oBJ1ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle OBJ 2"].Bindings;
			oBJ2ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle OBJ 3"].Bindings;
			oBJ3ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Toggle OBJ 4"].Bindings;
		}

		private void forceDMGModeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GB_ForceDMG ^= true;
			FlagNeedsReboot();
		}

		private void gBAInCGBModeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GB_GBACGB ^= true;
			FlagNeedsReboot();
		}

		private void multicartCompatibilityToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GB_MulticartCompat ^= true;
			FlagNeedsReboot();
		}

		private void StatusSlot1_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (StateSlots.HasSlot(1))
				{
					LoadState("QuickSave1");
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				SaveState("QuickSave1");
			}
		}

		private void StatusSlot2_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (StateSlots.HasSlot(2))
				{
					LoadState("QuickSave2");
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				SaveState("QuickSave2");
			}
		}

		private void StatusSlot3_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (StateSlots.HasSlot(3))
				{
					LoadState("QuickSave3");
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				SaveState("QuickSave3");
			}
		}

		private void StatusSlot4_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (StateSlots.HasSlot(4))
				{
					LoadState("QuickSave4");
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				SaveState("QuickSave4");
			}
		}

		private void StatusSlot5_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (StateSlots.HasSlot(5))
				{
					LoadState("QuickSave5");
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				SaveState("QuickSave5");
			}
		}

		private void StatusSlot6_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (StateSlots.HasSlot(6))
				{
					LoadState("QuickSave6");
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				SaveState("QuickSave6");
			}
		}

		private void StatusSlot7_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (StateSlots.HasSlot(7))
				{
					LoadState("QuickSave7");
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				SaveState("QuickSave7");
			}
		}

		private void StatusSlot8_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (StateSlots.HasSlot(8))
				{
					LoadState("QuickSave8");
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				SaveState("QuickSave8");
			}
		}

		private void StatusSlot9_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (StateSlots.HasSlot(9))
				{
					LoadState("QuickSave9");
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				SaveState("QuickSave9");
			}
		}

		private void StatusSlot10_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (StateSlots.HasSlot(0))
				{
					LoadState("QuickSave0");
				}
			}
			else if (e.Button == MouseButtons.Right)
			{
				SaveState("QuickSave0");
			}
		}

		private static void FormDragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void FormDragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			bool isLua = false;
			foreach (string path in filePaths)
			{
				var extension = Path.GetExtension(path);
				if (extension != null && extension.ToUpper() == ".LUA")
				{
					OpenLuaConsole();
					LuaConsole1.LoadLuaFile(path);
					isLua = true;
				}
			}
			if (isLua)
				return;

			var ext = Path.GetExtension(filePaths[0]) ?? "";
			if (ext.ToUpper() == ".LUASES")
			{
				OpenLuaConsole();
				LuaConsole1.LoadLuaSession(filePaths[0]);
			}
			else if (IsValidMovieExtension(ext))
			{
				Movie m = new Movie(filePaths[0], GlobalWinF.MainForm.GetEmuVersion());
				StartNewMovie(m, false);

			}
			else if (ext.ToUpper() == ".STATE")
			{
				LoadStateFile(filePaths[0], Path.GetFileName(filePaths[0]));
			}
			else if (ext.ToUpper() == ".CHT")
			{
				Global.CheatList.Load(filePaths[0], false);
				LoadCheatsWindow();
				ToolHelpers.UpdateCheatRelatedTools();
			}
			else if (ext.ToUpper() == ".WCH")
			{
				LoadRamWatch(true);
				RamWatch1.LoadWatchFile(new FileInfo(filePaths[0]), false);
			}

			else if (MovieImport.IsValidMovieExtension(Path.GetExtension(filePaths[0])))
			{
				//tries to open a legacy movie format as if it were a BKM, by importing it

				if (CurrentlyOpenRom == null)
				{
					OpenROM();
				}
				else
				{
					LoadRom(CurrentlyOpenRom);
				}

				string errorMsg;
				string warningMsg;
				Movie m = MovieImport.ImportFile(filePaths[0], GlobalWinF.MainForm.GetEmuVersion(), out errorMsg, out warningMsg);
				if (errorMsg.Length > 0)
				{
					MessageBox.Show(errorMsg, "Conversion error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					//fix movie extension to something palatable for these purposes. 
					//for instance, something which doesnt clobber movies you already may have had.
					//i'm evenly torn between this, and a file in %TEMP%, but since we dont really have a way to clean up this tempfile, i choose this:
					m.Filename += ".autoimported." + Global.Config.MovieExtension;
					m.WriteMovie();
					StartNewMovie(m, false);
				}
				GlobalWinF.OSD.AddMessage(warningMsg);
			}
			else
				LoadRom(filePaths[0]);
		}

		private void toolStripMenuItem6_Click(object sender, EventArgs e)
		{
			StopMovie(true);
		}

		private void SNESgameGenieCodesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadGameGenieEC();
		}

		private void GBgameGenieCodesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadGameGenieEC();
		}

		private void GGgameGenieEncoderDecoderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadGameGenieEC();
		}

		private void createDualGBXMLToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWinF.Sound.StopSound();
			using (var dlg = new GBtools.DualGBXMLCreator())
			{
				dlg.ShowDialog(this);
			}
			GlobalWinF.Sound.StartSound();
		}

		private void tempN64PluginControlToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var result = new N64VideoPluginconfig().ShowDialog();
			if (result == DialogResult.OK)
			{
				GlobalWinF.OSD.AddMessage("Plugin settings saved");
			}
			else
			{
				GlobalWinF.OSD.AddMessage("Plugin settings aborted");
			}
		}

		private void savestateTypeToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			defaultToolStripMenuItem.Checked = false;
			binaryToolStripMenuItem.Checked = false;
			textToolStripMenuItem.Checked = false;
			switch (Global.Config.SaveStateType)
			{
				case Config.SaveStateTypeE.Binary: binaryToolStripMenuItem.Checked = true; break;
				case Config.SaveStateTypeE.Text: textToolStripMenuItem.Checked = true; break;
				case Config.SaveStateTypeE.Default: defaultToolStripMenuItem.Checked = true; break;
			}
		}

		private void defaultToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveStateType = Config.SaveStateTypeE.Default;
		}

		private void binaryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveStateType = Config.SaveStateTypeE.Binary;
		}

		private void textToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveStateType = Config.SaveStateTypeE.Text;
		}

		private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (var dlg = new SaturnPrefs())
			{
				var result = dlg.ShowDialog(this);
				if (result == DialogResult.OK)
				{
					SaturnSetPrefs();
				}
			}
		}

		private void controllersToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			OpenControllerConfig();
		}

		private void hotkeysToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			OpenHotkeyDialog();
		}

		private void messagesToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			new MessageConfig().ShowDialog();
		}

		private void pathsToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			new PathConfig().ShowDialog();
		}

		private void soundToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			OpenSoundConfigDialog();
		}

		private void autofireToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			new AutofireConfig().ShowDialog();
		}

		private void neverBeAskedToSaveChangesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SupressAskSave ^= true;
		}

		private void soundChannelsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadNesSoundConfig();
		}

		private void changeDMGPalettesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is Gameboy)
			{
				var g = Global.Emulator as Gameboy;
				if (g.IsCGBMode())
				{
					if (GBtools.CGBColorChooserForm.DoCGBColorChooserFormDialog(this))
					{
						g.SetCGBColors(Global.Config.CGBColors);
					}
				}
				else
				{
					GBtools.ColorChooserForm.DoColorChooserFormDialog(g.ChangeDMGColors, this);
				}
			}
		}

		private void sNESToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if ((Global.Emulator as LibsnesCore).IsSGB)
			{
				loadGBInSGBToolStripMenuItem.Visible = true;
				loadGBInSGBToolStripMenuItem.Checked = Global.Config.GB_AsSGB;
			}
			else
				loadGBInSGBToolStripMenuItem.Visible = false;
		}

		private void loadGBInSGBToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			loadGBInSGBToolStripMenuItem_Click(sender, e);
		}

		private void loadGBInSGBToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GB_AsSGB ^= true;
			FlagNeedsReboot();
		}

		private void MainForm_Resize(object sender, EventArgs e)
		{
			GlobalWinF.RenderPanel.Resized = true;
		}

		private void backupSaveramToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.BackupSaveram ^= true;
			if (Global.Config.BackupSaveram)
			{
				GlobalWinF.OSD.AddMessage("Backup saveram enabled");
			}
			else
			{
				GlobalWinF.OSD.AddMessage("Backup saveram disabled");
			}

		}

		private void toolStripStatusLabel2_Click(object sender, EventArgs e)
		{
			RebootCore();
		}

		private void traceLoggerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadTraceLogger();
		}

		private void blurryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DispBlurry ^= true;
		}

		private void showClippedRegionsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GGShowClippedRegions ^= true;
			GlobalWinF.CoreComm.GG_ShowClippedRegions = Global.Config.GGShowClippedRegions;
		}

		private void highlightActiveDisplayRegionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GGHighlightActiveDisplayRegion ^= true;
			GlobalWinF.CoreComm.GG_HighlightActiveDisplayRegion = Global.Config.GGHighlightActiveDisplayRegion;
		}

		private void saveMovieToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			SaveMovie();
		}

		private void virtualPadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadVirtualPads();
		}

		private void showBGToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Atari2600_ShowBG ^= true;
			SyncCoreCommInputSignals();
		}

		private void showPlayer1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Atari2600_ShowPlayer1 ^= true;
			SyncCoreCommInputSignals();
		}

		private void showPlayer2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Atari2600_ShowPlayer2 ^= true;
			SyncCoreCommInputSignals();
		}

		private void showMissle1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Atari2600_ShowMissle1 ^= true;
			SyncCoreCommInputSignals();
		}

		private void showMissle2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Atari2600_ShowMissle2 ^= true;
			SyncCoreCommInputSignals();
		}

		private void showBallToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Atari2600_ShowBall ^= true;
			SyncCoreCommInputSignals();
		}

		private void showPlayfieldToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Atari2600_ShowPlayfield ^= true;
			SyncCoreCommInputSignals();
		}

		private void gPUViewerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadGBGPUView();
		}

		private void miLimitFramerate_DropDownOpened(object sender, EventArgs e)
		{
		}

		private void skipBIOIntroToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.ColecoSkipBiosIntro ^= true;
			FlagNeedsReboot();
		}

		private void colecoToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			skipBIOSIntroToolStripMenuItem.Checked = Global.Config.ColecoSkipBiosIntro;
		}

		private void gPUViewToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadGBAGPUView();
		}

		private void bothHotkeysAndControllersToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Input_Hotkey_OverrideOptions = 0;
			UpdateKeyPriorityIcon();
		}

		private void inputOverridesHotkeysToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Input_Hotkey_OverrideOptions = 1;
			UpdateKeyPriorityIcon();
		}

		private void hotkeysOverrideInputToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.Input_Hotkey_OverrideOptions = 2;
			UpdateKeyPriorityIcon();
		}

		private void keyPriorityToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			switch (Global.Config.Input_Hotkey_OverrideOptions)
			{
				default:
				case 0:
					bothHotkeysAndControllersToolStripMenuItem.Checked = true;
					inputOverridesHotkeysToolStripMenuItem.Checked = false;
					hotkeysOverrideInputToolStripMenuItem.Checked = false;
					break;
				case 1:
					bothHotkeysAndControllersToolStripMenuItem.Checked = false;
					inputOverridesHotkeysToolStripMenuItem.Checked = true;
					hotkeysOverrideInputToolStripMenuItem.Checked = false;
					break;
				case 2:
					bothHotkeysAndControllersToolStripMenuItem.Checked = false;
					inputOverridesHotkeysToolStripMenuItem.Checked = false;
					hotkeysOverrideInputToolStripMenuItem.Checked = true;
					break;
			}
		}

		private void KeyPriorityStatusBarLabel_Click(object sender, EventArgs e)
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

		private void rewindOptionsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			new RewindConfig().ShowDialog();
		}

		private void loadTIFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenFileDialog OFD = new OpenFileDialog();

			if (OFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				try
				{
					(Global.Emulator as TI83).LinkPort.SendFileToCalc(File.OpenRead(OFD.FileName), true);
				}
				catch (IOException ex)
				{
					string Message = string.Format("Invalid file format. Reason: {0} \nForce transfer? This may cause the calculator to crash.", ex.Message);

					if (MessageBox.Show(Message, "Upload Failed", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
						(Global.Emulator as TI83).LinkPort.SendFileToCalc(File.OpenRead(OFD.FileName), false);
				}
			}
		}
	}
}
