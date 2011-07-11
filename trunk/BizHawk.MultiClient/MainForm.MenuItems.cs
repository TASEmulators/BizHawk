using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Calculator;

namespace BizHawk.MultiClient
{
	partial class MainForm
	{
		private void recordAVIToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var sfd = new SaveFileDialog();
			//TODO adelikat (dunno how to do the paths correctly)
			sfd.FileName = Path.Combine(Global.Config.AVIPath, "game.avi");
			if (sfd.ShowDialog() == DialogResult.Cancel)
				return;

			//TODO - cores should be able to specify exact values for these instead of relying on this to calculate them
			int fps = (int)(Global.Emulator.CoreOutputComm.VsyncRate * 0x01000000);
			AviWriter aw = new AviWriter();
			aw.SetMovieParameters(fps, 0x01000000);
			aw.SetVideoParameters(Global.Emulator.VideoProvider.BufferWidth, Global.Emulator.VideoProvider.BufferHeight);
			aw.SetAudioParameters(44100, 2, 16);
			aw.OpenFile(sfd.FileName);
			var token = aw.AcquireVideoCodecToken(Global.MainForm.Handle);
			aw.SetVideoCodecToken(token);
			aw.OpenStreams();

			//commit the avi writing last, in case there were any errors earlier
			CurrAviWriter = aw;
		}

		private void stopAVIToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CurrAviWriter.CloseFile();
			CurrAviWriter = null;
		}

		private void DumpStatus_Click(object sender, EventArgs e)
		{
			string details = Global.Emulator.CoreOutputComm.RomStatusDetails;
			if(string.IsNullOrEmpty(details)) return;
			var lw = new LogWindow();
			lw.ShowReport("Dump Status Report",details);
		}

		private void RAMPokeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RamPoke r = new RamPoke();
			r.Show();
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
			//var window = new BizHawk.MultiClient.tools.LuaWindow();
			//window.Show();
			LuaConsole l = new LuaConsole();
			l.Show();
		}

		private void miLimitFramerate_Click(object sender, EventArgs e)
		{
			Global.Config.LimitFramerate ^= true;
		}

		private void miDisplayVsync_Click(object sender, EventArgs e)
		{
			Global.Config.DisplayVSync ^= true;
			Global.RenderPanel.Resized = true;
		}

		private void miAutoMinimizeSkipping_Click(object sender, EventArgs e)
		{
			Global.Config.AutoMinimizeSkipping ^= true;
		}

		private void miFrameskip0_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 0; }
		private void miFrameskip1_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 1; }
		private void miFrameskip2_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 2; }
		private void miFrameskip3_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 3; }
		private void miFrameskip4_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 4; }
		private void miFrameskip5_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 5; }
		private void miFrameskip6_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 6; }
		private void miFrameskip7_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 7; }
		private void miFrameskip8_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 8; }
		private void miFrameskip9_Click(object sender, EventArgs e) { Global.Config.FrameSkip = 9; }

		void ClickSpeedItem(int num)
		{
			if ((Control.ModifierKeys & Keys.Control) != 0) SetSpeedPercentAlternate(num);
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
			SoundConfig s = new SoundConfig();
			s.ShowDialog();
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

		private void enableFMChipToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SmsEnableFM ^= true;
		}

		private void overclockWhenKnownSafeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SmsAllowOverlock ^= true;
		}

		private void forceStereoSeparationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SmsForceStereoSeparation ^= true;
		}

		private void recordMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RecordMovie();
		}

		private void playMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PlayMovie();
		}

		public void StopUserMovie()
		{
			string message = "Movie ";
			if (UserMovie.GetMovieMode() == MOVIEMODE.RECORD)
				message += "recording ";
			else if (UserMovie.GetMovieMode() == MOVIEMODE.PLAY
				|| UserMovie.GetMovieMode() == MOVIEMODE.FINISHED)
				message += "playback ";
			message += "stopped.";
			if (UserMovie.GetMovieMode() != MOVIEMODE.INACTIVE)
			{
				UserMovie.StopMovie();
				Global.MovieMode = false;
				Global.RenderPanel.AddMessage(message);
				SetMainformMovieInfo();
			}
		}

		public void StopInputLog()
		{
			if (InputLog.GetMovieMode() == MOVIEMODE.RECORD)
				InputLog.StopMovie();
		}

		private void stopMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{
			StopUserMovie();
		}

		private void playFromBeginningToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PlayMovieFromBeginning();
		}


		private void RAMWatchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadRamWatch();
		}

		private void rAMSearchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadRamSearch();
		}


		private void autoloadMostRecentToolStripMenuItem_Click(object sender, EventArgs e)
		{
			UpdateAutoLoadRecentRom();
		}

		private void clearToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RecentRoms.Clear();
		}


		private void selectSlot1ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 1;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 2;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 3;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot4ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 4;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot5ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 5;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot6ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 6;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot7ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 7;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot8ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 8;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot9ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 9;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot10ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 0;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void previousSlotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PreviousSlot();
		}

		private void nextSlotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			NextSlot();
		}

		private void saveToCurrentSlotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveState("QuickSave" + SaveSlot.ToString());
		}

		private void loadCurrentSlotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadState("QuickSave" + SaveSlot.ToString());
		}

		private void closeROMToolStripMenuItem_Click(object sender, EventArgs e)
		{
			CloseROM();
		}

		private void saveStateToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Sound.StopSound();

			var frm = new NameStateForm();
			frm.ShowDialog(this);

			if (frm.OK)
				SaveState(frm.Result);

			Global.Sound.StartSound();
		}

		private void powerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadRom(CurrentlyOpenRom);
		}

		private void resetToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SoftReset();
		}

		private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (EmulatorPaused == true)
				UnpauseEmulator();
			else
				PauseEmulator();
		}

		private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
		{

		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			new AboutBox().ShowDialog();
		}

		private void controllersToolStripMenuItem_Click(object sender, EventArgs e)
		{
			InputConfig i = new InputConfig();
			i.ShowDialog();
			//re-initialize controls in case anything was changed
			if (i.DialogResult == DialogResult.OK)
			{
				InitControls();
				SyncControls();
			}
		}

		private void hotkeysToolStripMenuItem_Click(object sender, EventArgs e)
		{
			BizHawk.MultiClient.tools.HotkeyWindow h = new BizHawk.MultiClient.tools.HotkeyWindow();
			h.ShowDialog();
			if (h.DialogResult == DialogResult.OK)
			{
				InitControls();
				SyncControls();
			}
		}

		private void displayFPSToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFPS();
		}

		private void displayFrameCounterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFrameCounter();
		}

		private void displayInputToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleInputDisplay();
		}

		private void displayLagCounterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleLagCounter();
		}

		private void screenshotF12ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			TakeScreenshot();
		}

		private void savestate1toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave1"); }
		private void savestate2toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave2"); }
		private void savestate3toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave3"); }
		private void savestate4toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave4"); }
		private void savestate5toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave5"); }
		private void savestate6toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave6"); }
		private void savestate7toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave7"); }
		private void savestate8toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave8"); }
		private void savestate9toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave9"); }
		private void savestate0toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave0"); }

		private void loadstate1toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave1"); }
		private void loadstate2toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave2"); }
		private void loadstate3toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave3"); }
		private void loadstate4toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave4"); }
		private void loadstate5toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave5"); }
		private void loadstate6toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave6"); }
		private void loadstate7toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave7"); }
		private void loadstate8toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave8"); }
		private void loadstate9toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave9"); }
		private void loadstate0toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave0"); }

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (RamWatch1.AskSave())
				Close();
		}

		private void openROMToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenROM();
		}

		private void PPUViewerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadNESPPU();
		}

		private void enableRewindToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RewindEnabled ^= true;
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
			Global.Config.ForceGDI ^= true;
			SyncPresentationMode();
		}

		private void debuggerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadNESDebugger();
		}

		private void saveStateToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			savestate1toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SaveSlot1;
			savestate2toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SaveSlot2;
			savestate3toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SaveSlot3;
			savestate4toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SaveSlot4;
			savestate5toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SaveSlot5;
			savestate6toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SaveSlot6;
			savestate7toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SaveSlot7;
			savestate8toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SaveSlot8;
			savestate9toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SaveSlot9;
			savestate0toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SaveSlot0;
			saveNamedStateToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SaveNamedState;
		}

		private void loadStateToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			loadstate1toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot0;
			loadstate2toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot1;
			loadstate3toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot2;
			loadstate4toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot3;
			loadstate5toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot4;
			loadstate6toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot5;
			loadstate7toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot6;
			loadstate8toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot7;
			loadstate9toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot8;
			loadstate0toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot9;
			loadNamedStateToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadNamedState;
		}

		private void nametableViewerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadNESNameTable();
		}

		private void saveNamedStateToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveStateAs();
		}

		private void loadNamedStateToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadStateAs();
		}

		private void toolBoxToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadToolBox();
		}

		private void toolsToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			toolBoxToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.ToolBox;
			if (!ToolBox1.IsHandleCreated || ToolBox1.IsDisposed)
				toolBoxToolStripMenuItem.Enabled = true;
			else
				toolBoxToolStripMenuItem.Enabled = false;

			rAMWatchToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.RamWatch;
			rAMSearchToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.RamSearch;
			rAMPokeToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.RamPoke;
			hexEditorToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HexEditor;
			luaConsoleToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LuaConsole;
			cheatsToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.Cheats;
		}

		private void saveSlotToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			selectSlot10ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SelectSlot0;
			selectSlot1ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SelectSlot1;
			selectSlot2ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SelectSlot2;
			selectSlot3ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SelectSlot3;
			selectSlot4ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SelectSlot4;
			selectSlot5ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SelectSlot5;
			selectSlot6ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SelectSlot6;
			selectSlot7ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SelectSlot7;
			selectSlot8ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SelectSlot8;
			selectSlot9ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SelectSlot9;
			previousSlotToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.PreviousSlot;
			nextSlotToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.NextSlot;
			saveToCurrentSlotToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.QuickSave;
			loadCurrentSlotToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.QuickLoad;
		}

		private void switchToFullscreenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleFullscreen();
		}

		private void messagesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MessageConfig m = new MessageConfig();
			m.ShowDialog();
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

		private void disableSaveslotKeysOnLoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!(Global.Emulator is TI83)) return;
			Global.Config.TI83disableSaveSlotKeys ^= true;
		}

		private void tI83ToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			disableSaveslotKeysOnLoToolStripMenuItem.Checked = Global.Config.TI83disableSaveSlotKeys;
			autoloadVirtualKeyboardToolStripMenuItem.Checked = Global.Config.TI83autoloadKeyPad;
		}

		private void pathsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PathConfig p = new PathConfig();
			p.ShowDialog();
		}

		private void displayRerecordCountToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisplayRerecordCount ^= true;
		}

		private void recentROMToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			//Clear out recent Roms list
			//repopulate it with an up to date list
			recentROMToolStripMenuItem.DropDownItems.Clear();

			if (Global.Config.RecentRoms.IsEmpty())
			{
				var none = new ToolStripMenuItem();
				none.Enabled = false;
				none.Text = "None";
				recentROMToolStripMenuItem.DropDownItems.Add(none);
			}
			else
			{
				for (int x = 0; x < Global.Config.RecentRoms.Length(); x++)
				{
					string path = Global.Config.RecentRoms.GetRecentFileByPosition(x);
					var item = new ToolStripMenuItem();
					item.Text = path;
					item.Click += (o, ev) => LoadRomFromRecent(path);
					recentROMToolStripMenuItem.DropDownItems.Add(item);
				}
			}

			recentROMToolStripMenuItem.DropDownItems.Add("-");

			var clearitem = new ToolStripMenuItem();
			clearitem.Text = "&Clear";
			clearitem.Click += (o, ev) => Global.Config.RecentRoms.Clear();
			recentROMToolStripMenuItem.DropDownItems.Add(clearitem);

			var auto = new ToolStripMenuItem();
			auto.Text = "&Autoload Most Recent";
			auto.Click += (o, ev) => UpdateAutoLoadRecentRom();
			if (Global.Config.AutoLoadMostRecentRom == true)
				auto.Checked = true;
			else
				auto.Checked = false;
			recentROMToolStripMenuItem.DropDownItems.Add(auto);
		}

		private void recentToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			//Clear out recent Movies list
			//repopulate it with an up to date list

			recentToolStripMenuItem.DropDownItems.Clear();

			if (Global.Config.RecentMovies.IsEmpty())
			{
				var none = new ToolStripMenuItem();
				none.Enabled = false;
				none.Text = "None";
				recentToolStripMenuItem.DropDownItems.Add(none);
			}
			else
			{
				for (int x = 0; x < Global.Config.RecentMovies.Length(); x++)
				{
					string path = Global.Config.RecentMovies.GetRecentFileByPosition(x);
					var item = new ToolStripMenuItem();
					item.Text = path;
					item.Click += (o, ev) => LoadMoviesFromRecent(path);
					recentToolStripMenuItem.DropDownItems.Add(item);
				}
			}

			recentToolStripMenuItem.DropDownItems.Add("-");

			var clearitem = new ToolStripMenuItem();
			clearitem.Text = "&Clear";
			clearitem.Click += (o, ev) => Global.Config.RecentMovies.Clear();
			recentToolStripMenuItem.DropDownItems.Add(clearitem);

			var auto = new ToolStripMenuItem();
			auto.Text = "&Autoload Most Recent";
			auto.Click += (o, ev) => UpdateAutoLoadRecentMovie();
			if (Global.Config.AutoLoadMostRecentMovie == true)
				auto.Checked = true;
			else
				auto.Checked = false;
			recentToolStripMenuItem.DropDownItems.Add(auto);
		}

		private void screenshotAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			string path = String.Format(Global.Game.ScreenshotPrefix + ".{0:yyyy-MM-dd HH.mm.ss}.png", DateTime.Now);

			SaveFileDialog sfd = new SaveFileDialog();
			sfd.InitialDirectory = Path.GetDirectoryName(path);
			sfd.FileName = Path.GetFileName(path);
			sfd.Filter = "PNG File (*.png)|*.png";

			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return;
			MakeScreenshot(sfd.FileName);
		}

		private void runInBackgroundToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RunInBackground ^= true;
		}

		private void bindSavestatesToMoviesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.BindSavestatesToMovies ^= true;
		}

		private void acceptBackgroundInputToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AcceptBackgroundInput ^= true;
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
			SyncCoreInputComm();
		}

		public void MainForm_MouseClick(object sender, MouseEventArgs e)
		{
			if (Global.Config.ShowContextMenu && e.Button == MouseButtons.Right)
			{
				Point p = new Point(e.X, e.Y + this.menuStrip1.Height);
				Point po = this.PointToScreen(p);
				contextMenuStrip1.Show(po);
			}
		}

		private void openRomToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			OpenROM();
		}

		private void loadLastROMToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadRomFromRecent(Global.Config.RecentRoms.GetRecentFileByPosition(0));
		}

		private void enableContextMenuToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.ShowContextMenu ^= true;
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
			LoadMoviesFromRecent(Global.Config.RecentMovies.GetRecentFileByPosition(0));
		}

		private void AddSubtitleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SubtitleMaker s = new SubtitleMaker();
			s.DisableFrame();
			int index = -1;
			Subtitle sub = new Subtitle();
			for (int x = 0; x < UserMovie.Subtitles.Count(); x++)
			{
				sub = UserMovie.Subtitles.GetSubtitleByIndex(x);
				if (Global.Emulator.Frame == sub.Frame)
				{
					index = x;
					break;
				}
			}
			if (index < 0)
			{
				sub = new Subtitle();
				sub.Frame = Global.Emulator.Frame;
			}
			s.sub = sub;

			if (s.ShowDialog() == DialogResult.OK)
			{
				if (index >= 0)
					UserMovie.Subtitles.Remove(index);
				UserMovie.Subtitles.AddSubtitle(s.sub);
			}
		}

		private void undoSavestateToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//TODO
		}

		private void screenshotToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			TakeScreenshot();
		}

		private void closeROMToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			CloseROM();
		}

		private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (EmulatorPaused)
				wasPaused = true;
			else
				wasPaused = false;
			didMenuPause = true;
			PauseEmulator();

			if (IsNullEmulator())
			{
				contextMenuStrip1.Items[0].Visible = true;
				contextMenuStrip1.Items[1].Visible = true;
				contextMenuStrip1.Items[2].Visible = false;
				contextMenuStrip1.Items[3].Visible = false;
				contextMenuStrip1.Items[4].Visible = false;
				contextMenuStrip1.Items[5].Visible = false;
				contextMenuStrip1.Items[6].Visible = false;
				contextMenuStrip1.Items[7].Visible = false;
				contextMenuStrip1.Items[8].Visible = false;
				contextMenuStrip1.Items[9].Visible = false;
				contextMenuStrip1.Items[10].Visible = false;
				contextMenuStrip1.Items[11].Visible = false;
				contextMenuStrip1.Items[12].Visible = false;
				contextMenuStrip1.Items[13].Visible = false;
				contextMenuStrip1.Items[14].Visible = false;
				contextMenuStrip1.Items[15].Visible = false;
			}
			else
			{
				contextMenuStrip1.Items[0].Visible = false;
				contextMenuStrip1.Items[1].Visible = false;
				contextMenuStrip1.Items[2].Visible = false;

				if (UserMovie.GetMovieMode() == MOVIEMODE.INACTIVE)
				{
					contextMenuStrip1.Items[3].Visible = true;
					contextMenuStrip1.Items[4].Visible = true;
					contextMenuStrip1.Items[5].Visible = false;
					contextMenuStrip1.Items[6].Visible = true;
					contextMenuStrip1.Items[7].Visible = false;
					contextMenuStrip1.Items[8].Visible = false;
					contextMenuStrip1.Items[9].Visible = false;
					contextMenuStrip1.Items[10].Visible = true;
				}
				else
				{
					contextMenuStrip1.Items[3].Visible = false;
					contextMenuStrip1.Items[4].Visible = false;
					contextMenuStrip1.Items[5].Visible = true;
					contextMenuStrip1.Items[6].Visible = false;
					contextMenuStrip1.Items[7].Visible = true;
					contextMenuStrip1.Items[8].Visible = true;
					contextMenuStrip1.Items[9].Visible = true;
					contextMenuStrip1.Items[10].Visible = true;
					if (ReadOnly == true)
					{
						contextMenuStrip1.Items[8].Text = "View Subtitles";
						contextMenuStrip1.Items[9].Text = "View Comments";
						contextMenuStrip1.Items[11].Visible = false;
					}
					else
					{
						contextMenuStrip1.Items[8].Text = "Edit Subtitles";
						contextMenuStrip1.Items[9].Text = "Edit Comments";
						contextMenuStrip1.Items[11].Visible = true;
					}
				}

				contextMenuStrip1.Items[12].Visible = true;

				contextMenuStrip1.Items[13].Visible = true;
				contextMenuStrip1.Items[14].Visible = true;
				contextMenuStrip1.Items[15].Visible = true;
			}

			if (Global.Config.RecentRoms.Length() == 0)
				contextMenuStrip1.Items[1].Enabled = false;
			else
				contextMenuStrip1.Items[1].Enabled = true;

			if (Global.Config.RecentMovies.Length() == 0)
				contextMenuStrip1.Items[6].Enabled = false;
			else
				contextMenuStrip1.Items[6].Enabled = true;


			//TODO:
			contextMenuStrip1.Items[12].Enabled = false;
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
			UserMovie.WriteBackup();
		}

		private void automaticallyBackupMoviesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.EnableBackupMovies ^= true;
		}

		private void stopMovieToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			StopUserMovie();
		}

        private void displayLogWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.ShowLogWindow ^= true;
            displayLogWindowToolStripMenuItem.Checked = Global.Config.ShowLogWindow;
            if (Global.Config.ShowLogWindow)
                LogConsole.ShowConsole();
            else
                LogConsole.HideConsole();
        }
	}
}