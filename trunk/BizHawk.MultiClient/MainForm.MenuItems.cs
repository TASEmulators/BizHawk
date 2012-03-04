using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Calculator;
using BizHawk.Emulation.Consoles.Gameboy;

namespace BizHawk.MultiClient
{
	partial class MainForm
	{
		private void recordAVIToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RecordAVI();
		}

		private void stopAVIToolStripMenuItem_Click(object sender, EventArgs e)
		{
			StopAVI();
		}

		private void DumpStatus_Click(object sender, EventArgs e)
		{
			string details = Global.Emulator.CoreOutputComm.RomStatusDetails;
			if(string.IsNullOrEmpty(details)) return;
			var lw = new LogWindow();
			Global.Sound.StopSound();
			lw.ShowReport("Dump Status Report",details);
			Global.Sound.StartSound();
		}

		private void RAMPokeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadRamPoke();
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

        private void smsSpriteLimitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.SmsSpriteLimit ^= true;
        }

        private void pceAlwaysPerformSpriteLimitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.PceSpriteLimit ^= true;
        }

        private void pceAlwayEqualizeVolumesLimitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.PceEqualizeVolume ^= true;
        }

        private void pceArcadeCardRewindEnableHackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.PceArcadeCardRewindHack ^= true;
        }

        private void recordMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RecordMovie();
		}

		private void playMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PlayMovie();
		}

		private void stopMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{
			StopMovie();
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
			Global.Config.SaveSlot = 1;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveSlot = 2;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveSlot = 3;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot4ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveSlot = 4;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot5ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveSlot = 5;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot6ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveSlot = 6;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot7ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveSlot = 7;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot8ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveSlot = 8;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot9ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveSlot = 9;
			UpdateStatusSlots();
			SaveSlotSelectedMessage();
		}

		private void selectSlot10ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveSlot = 0;
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
			SaveState("QuickSave" + Global.Config.SaveSlot.ToString());
		}

		private void loadCurrentSlotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadState("QuickSave" + Global.Config.SaveSlot.ToString());
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

		private void forumsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/forum/viewforum.php?f=64");
		}

		private void screenshotClipboardToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (var img = MakeScreenshotImage())
			{
				System.Windows.Forms.Clipboard.SetImage(img);
			}
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
			Global.Config.DisplayGDI ^= true;
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

			autoLoadLastSlotToolStripMenuItem.Checked = Global.Config.AutoLoadLastSaveSlot;
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

		private void tI83ToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
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
			string path = String.Format(PathManager.ScreenshotPrefix(Global.Game) + ".{0:yyyy-MM-dd HH.mm.ss}.png", DateTime.Now);

			SaveFileDialog sfd = new SaveFileDialog();
			sfd.InitialDirectory = Path.GetDirectoryName(path);
			sfd.FileName = Path.GetFileName(path);
			sfd.Filter = "PNG File (*.png)|*.png";

			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return;
			TakeScreenshot(sfd.FileName);
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
			for (int x = 0; x < Global.MovieSession.Movie.Subtitles.Count(); x++)
			{
				sub = Global.MovieSession.Movie.Subtitles.GetSubtitleByIndex(x);
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
					Global.MovieSession.Movie.Subtitles.Remove(index);
				Global.MovieSession.Movie.Subtitles.AddSubtitle(s.sub);
			}
		}

		private void screenshotToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			TakeScreenshot();
		}

		private void closeROMToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			CloseROM();
		}


		private void restartMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{
			PlayMovieFromBeginning();
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
				contextMenuStrip1.Items[16].Visible = false;
			}
			else
			{
				contextMenuStrip1.Items[0].Visible = false;
				contextMenuStrip1.Items[1].Visible = false;
				contextMenuStrip1.Items[2].Visible = false;

				if (Global.MovieSession.Movie.Mode == MOVIEMODE.INACTIVE)
				{
					contextMenuStrip1.Items[3].Visible = true;
					contextMenuStrip1.Items[4].Visible = true;
					contextMenuStrip1.Items[5].Visible = false;
					contextMenuStrip1.Items[6].Visible = false;
					contextMenuStrip1.Items[7].Visible = true;
					contextMenuStrip1.Items[8].Visible = false;
					contextMenuStrip1.Items[9].Visible = false;
					contextMenuStrip1.Items[10].Visible = false;
					contextMenuStrip1.Items[11].Visible = true;
					contextMenuStrip1.Items[12].Visible = false;
				}
				else
				{
					contextMenuStrip1.Items[3].Visible = false;
					contextMenuStrip1.Items[4].Visible = false;
					contextMenuStrip1.Items[5].Visible = true;
					contextMenuStrip1.Items[6].Visible = true;
					contextMenuStrip1.Items[7].Visible = false;
					contextMenuStrip1.Items[8].Visible = true;
					contextMenuStrip1.Items[9].Visible = true;
					contextMenuStrip1.Items[10].Visible = true;
					contextMenuStrip1.Items[11].Visible = true;
					if (ReadOnly == true)
					{
						contextMenuStrip1.Items[9].Text = "View Subtitles";
						contextMenuStrip1.Items[10].Text = "View Comments";
						contextMenuStrip1.Items[12].Visible = false;
					}
					else
					{
						contextMenuStrip1.Items[9].Text = "Edit Subtitles";
						contextMenuStrip1.Items[10].Text = "Edit Comments";
						contextMenuStrip1.Items[12].Visible = true;
					}
				}

				contextMenuStrip1.Items[13].Visible = true;

				contextMenuStrip1.Items[14].Visible = true;
				contextMenuStrip1.Items[15].Visible = true;
				contextMenuStrip1.Items[16].Visible = true;
			}

			if (Global.Config.RecentRoms.Length() == 0)
				contextMenuStrip1.Items[1].Enabled = false;
			else
				contextMenuStrip1.Items[1].Enabled = true;

			if (Global.Config.RecentMovies.Length() == 0)
				contextMenuStrip1.Items[7].Enabled = false;
			else
				contextMenuStrip1.Items[7].Enabled = true;

			string path = PathManager.SaveStatePrefix(Global.Game) + "." + "QuickSave" + Global.Config.SaveSlot + ".State.bak";
			var file = new FileInfo(path);
			if (file.Exists == true)
			{
				if (StateSlots.IsRedo(Global.Config.SaveSlot))
				{
					contextMenuStrip1.Items[13].Enabled = true;
					contextMenuStrip1.Items[13].Text = "Redo Save to slot " + Global.Config.SaveSlot.ToString();
					contextMenuStrip1.Items[13].Image = BizHawk.MultiClient.Properties.Resources.redo;
				}
				else
				{
					contextMenuStrip1.Items[13].Enabled = true;
					contextMenuStrip1.Items[13].Text = "Undo Save to slot " + Global.Config.SaveSlot.ToString();
					contextMenuStrip1.Items[13].Image = BizHawk.MultiClient.Properties.Resources.undo;
				}
			}
			else
			{
				contextMenuStrip1.Items[13].Enabled = false;
				contextMenuStrip1.Items[13].Text = "Undo Savestate";
				contextMenuStrip1.Items[13].Image = BizHawk.MultiClient.Properties.Resources.undo;
			}

			if (InFullscreen == true)
			{
				contextMenuStrip1.Items[17].Visible = true;
				if (MainMenuStrip.Visible == true)
					contextMenuStrip1.Items[17].Text = "Hide Menu";
				else
					contextMenuStrip1.Items[17].Text = "Show Menu";
			}
			else
				contextMenuStrip1.Items[17].Visible = false;
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
			Global.MovieSession.Movie.WriteBackup();
		}

		private void automaticallyBackupMoviesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.EnableBackupMovies ^= true;
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
				LogConsole.ShowConsole();
			else
				LogConsole.HideConsole();
		}

		private void PauseStrip_Click(object sender, EventArgs e)
		{
			TogglePause();
		}

		private void displaySubtitlesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisplaySubtitles ^= true;
		}

		private void aVIWAVToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			recordAVIToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.AVIRecordBinding;
			stopAVIToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.AVIStopBinding;

			if (CurrAviWriter == null)
			{
				recordAVIToolStripMenuItem.Enabled = true;
				stopAVIToolStripMenuItem.Enabled = false;
			}
			else
			{
				recordAVIToolStripMenuItem.Enabled = false;
				stopAVIToolStripMenuItem.Enabled = true;
			}
		}

		private void StatusSlot1_Click(object sender, EventArgs e) { LoadState("QuickSave1"); }
		private void StatusSlot2_Click(object sender, EventArgs e) { LoadState("QuickSave2"); }
		private void StatusSlot3_Click(object sender, EventArgs e) { LoadState("QuickSave3"); }
		private void StatusSlot4_Click(object sender, EventArgs e) { LoadState("QuickSave4"); }
		private void StatusSlot5_Click(object sender, EventArgs e) { LoadState("QuickSave5"); }
		private void StatusSlot6_Click(object sender, EventArgs e) { LoadState("QuickSave6"); }
		private void StatusSlot7_Click(object sender, EventArgs e) { LoadState("QuickSave7"); }
		private void StatusSlot8_Click(object sender, EventArgs e) { LoadState("QuickSave8"); }
		private void StatusSlot9_Click(object sender, EventArgs e) { LoadState("QuickSave9"); }
		private void StatusSlot10_Click(object sender, EventArgs e) { LoadState("QuickSave0"); }

		private void viewCommentsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie.Mode == MOVIEMODE.INACTIVE) return;

			EditCommentsForm c = new EditCommentsForm();
			c.ReadOnly = ReadOnly;
			c.GetMovie(Global.MovieSession.Movie);
			c.ShowDialog();
		}

		private void viewSubtitlesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie.Mode == MOVIEMODE.INACTIVE) return;

			EditSubtitlesForm s = new EditSubtitlesForm();
			s.ReadOnly = ReadOnly;
			s.GetMovie(Global.MovieSession.Movie);
			s.ShowDialog();
		}

		private void debuggerToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			Global.MainForm.OpenGameboyDebugger();
		}

		private void tAStudioToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadTAStudio();
		}

		private void singleInstanceModeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SingleInstanceMode ^= true;
		}

		private void MainForm_Deactivate(object sender, EventArgs e)
		{
			if (!Global.Config.RunInBackground)
				PauseEmulator();
		}

		private void MainForm_Activated(object sender, EventArgs e)
		{
			if (!Global.Config.RunInBackground)
				UnpauseEmulator();
		}

		private void readonlyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleReadOnly();
		}

		private void movieToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie.Mode == MOVIEMODE.INACTIVE)
			{
				stopMovieToolStripMenuItem.Enabled = false;
				playFromBeginningToolStripMenuItem.Enabled = false;
			}
			else
			{
				stopMovieToolStripMenuItem.Enabled = true;
				playFromBeginningToolStripMenuItem.Enabled = true;
			}

			readonlyToolStripMenuItem.Checked = ReadOnly;
			bindSavestatesToMoviesToolStripMenuItem.Checked = Global.Config.BindSavestatesToMovies;
			automaticallyBackupMoviesToolStripMenuItem.Checked = Global.Config.EnableBackupMovies;

			readonlyToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.ReadOnlyToggleBinding;
			recordMovieToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.RecordMovieBinding;
			playMovieToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.PlayMovieBinding;
			stopMovieToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.StopMovieBinding;
			playFromBeginningToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.PlayBeginningBinding;
		}

		private void saveConfigToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveConfig();
			Global.RenderPanel.AddMessage("Saved settings");
		}

		private void loadConfigToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config = ConfigService.Load<Config>(PathManager.DefaultIniPath);
			Global.RenderPanel.AddMessage("Saved loaded");
		}

		private void frameSkipToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			miDisplayVsync.Checked = Global.Config.LimitFramerate;
			miAutoMinimizeSkipping.Checked = Global.Config.AutoMinimizeSkipping;
			miLimitFramerate.Checked = Global.Config.LimitFramerate;
			miDisplayVsync.Checked = Global.Config.DisplayVSync;
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
			miSpeed100.Checked = Global.Config.SpeedPercent == 100;
			miSpeed100.Image = (Global.Config.SpeedPercentAlternate == 100) ? BizHawk.MultiClient.Properties.Resources.FastForward : null;
			miSpeed150.Checked = Global.Config.SpeedPercent == 150;
			miSpeed150.Image = (Global.Config.SpeedPercentAlternate == 150) ? BizHawk.MultiClient.Properties.Resources.FastForward : null;
			miSpeed200.Checked = Global.Config.SpeedPercent == 200;
			miSpeed200.Image = (Global.Config.SpeedPercentAlternate == 200) ? BizHawk.MultiClient.Properties.Resources.FastForward : null;
			miSpeed75.Checked = Global.Config.SpeedPercent == 75;
			miSpeed75.Image = (Global.Config.SpeedPercentAlternate == 75) ? BizHawk.MultiClient.Properties.Resources.FastForward : null;
			miSpeed50.Checked = Global.Config.SpeedPercent == 50;
			miSpeed50.Image = (Global.Config.SpeedPercentAlternate == 50) ? BizHawk.MultiClient.Properties.Resources.FastForward : null;
			miAutoMinimizeSkipping.Enabled = !miFrameskip0.Checked;
			if (!miAutoMinimizeSkipping.Enabled) miAutoMinimizeSkipping.Checked = true;
		}

		private void gUIToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			runInBackgroundToolStripMenuItem.Checked = Global.Config.RunInBackground;
			pauseWhenMenuActivatedToolStripMenuItem.Checked = Global.Config.PauseWhenMenuActivated;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.SaveWindowPosition;
			startPausedToolStripMenuItem.Checked = Global.Config.StartPaused;
			enableRewindToolStripMenuItem.Checked = Global.Config.RewindEnabled;
			forceGDIPPresentationToolStripMenuItem.Checked = Global.Config.DisplayGDI;
			acceptBackgroundInputToolStripMenuItem.Checked = Global.Config.AcceptBackgroundInput;
			singleInstanceModeToolStripMenuItem.Checked = Global.Config.SingleInstanceMode;
			enableContextMenuToolStripMenuItem.Checked = Global.Config.ShowContextMenu;
			backupSavestatesToolStripMenuItem.Checked = Global.Config.BackupSavestates;
            autoSavestatesToolStripMenuItem.Checked = Global.Config.AutoSavestates;
			saveScreenshotWithSavestatesToolStripMenuItem.Checked = Global.Config.SaveScreenshotWithStates;
			logWindowAsConsoleToolStripMenuItem.Checked = Global.Config.WIN32_CONSOLE;
			showMenuInFullScreenToolStripMenuItem.Checked = Global.Config.ShowMenuInFullscreen;
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			Text = "BizHawk" + (INTERIM ? " (interim) " : "");

			//Hide platform specific menus until an appropriate ROM is loaded
			NESToolStripMenuItem.Visible = false;
			tI83ToolStripMenuItem.Visible = false;
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

			displayFPSToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.FPSBinding;
			displayFrameCounterToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.FrameCounterBinding;
			displayLagCounterToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LagCounterBinding;
			displayInputToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.InputDisplayBinding;

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

			switchToFullscreenToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.ToggleFullscreenBinding;
		}

		private void fileToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (IsNullEmulator())
			{
				closeROMToolStripMenuItem.Enabled = false;
				screenshotF12ToolStripMenuItem.Enabled = false;
				saveToCurrentSlotToolStripMenuItem.Enabled = false;
				loadCurrentSlotToolStripMenuItem.Enabled = false;
				loadNamedStateToolStripMenuItem.Enabled = false;
				saveNamedStateToolStripMenuItem.Enabled = false;
				savestate1toolStripMenuItem.Enabled = false;
				savestate2toolStripMenuItem.Enabled = false;
				savestate3toolStripMenuItem.Enabled = false;
				savestate4toolStripMenuItem.Enabled = false;
				savestate5toolStripMenuItem.Enabled = false;
				savestate6toolStripMenuItem.Enabled = false;
				savestate7toolStripMenuItem.Enabled = false;
				savestate8toolStripMenuItem.Enabled = false;
				savestate9toolStripMenuItem.Enabled = false;
				savestate0toolStripMenuItem.Enabled = false;
				loadstate1toolStripMenuItem.Enabled = false;
				loadstate2toolStripMenuItem.Enabled = false;
				loadstate3toolStripMenuItem.Enabled = false;
				loadstate4toolStripMenuItem.Enabled = false;
				loadstate5toolStripMenuItem.Enabled = false;
				loadstate6toolStripMenuItem.Enabled = false;
				loadstate7toolStripMenuItem.Enabled = false;
				loadstate8toolStripMenuItem.Enabled = false;
				loadstate9toolStripMenuItem.Enabled = false;
				loadstate0toolStripMenuItem.Enabled = false;
			}
			else
			{
				closeROMToolStripMenuItem.Enabled = true;
				screenshotF12ToolStripMenuItem.Enabled = true;
				saveToCurrentSlotToolStripMenuItem.Enabled = true;
				loadCurrentSlotToolStripMenuItem.Enabled = true;
				loadNamedStateToolStripMenuItem.Enabled = true;
				saveNamedStateToolStripMenuItem.Enabled = true;
				savestate1toolStripMenuItem.Enabled = true;
				savestate2toolStripMenuItem.Enabled = true;
				savestate3toolStripMenuItem.Enabled = true;
				savestate4toolStripMenuItem.Enabled = true;
				savestate5toolStripMenuItem.Enabled = true;
				savestate6toolStripMenuItem.Enabled = true;
				savestate7toolStripMenuItem.Enabled = true;
				savestate8toolStripMenuItem.Enabled = true;
				savestate9toolStripMenuItem.Enabled = true;
				savestate0toolStripMenuItem.Enabled = true;
				loadstate1toolStripMenuItem.Enabled = true;
				loadstate2toolStripMenuItem.Enabled = true;
				loadstate3toolStripMenuItem.Enabled = true;
				loadstate4toolStripMenuItem.Enabled = true;
				loadstate5toolStripMenuItem.Enabled = true;
				loadstate6toolStripMenuItem.Enabled = true;
				loadstate7toolStripMenuItem.Enabled = true;
				loadstate8toolStripMenuItem.Enabled = true;
				loadstate9toolStripMenuItem.Enabled = true;
				loadstate0toolStripMenuItem.Enabled = true;
			}

			selectSlot10ToolStripMenuItem.Checked = false;
			selectSlot1ToolStripMenuItem.Checked = false;
			selectSlot2ToolStripMenuItem.Checked = false;
			selectSlot3ToolStripMenuItem.Checked = false;
			selectSlot4ToolStripMenuItem.Checked = false;
			selectSlot5ToolStripMenuItem.Checked = false;
			selectSlot6ToolStripMenuItem.Checked = false;
			selectSlot7ToolStripMenuItem.Checked = false;
			selectSlot8ToolStripMenuItem.Checked = false;
			selectSlot9ToolStripMenuItem.Checked = false;
			selectSlot1ToolStripMenuItem.Checked = false;

			switch (Global.Config.SaveSlot)
			{
				case 0:
					selectSlot10ToolStripMenuItem.Checked = true;
					break;
				case 1:
					selectSlot1ToolStripMenuItem.Checked = true;
					break;
				case 2:
					selectSlot2ToolStripMenuItem.Checked = true;
					break;
				case 3:
					selectSlot3ToolStripMenuItem.Checked = true;
					break;
				case 4:
					selectSlot4ToolStripMenuItem.Checked = true;
					break;
				case 5:
					selectSlot5ToolStripMenuItem.Checked = true;
					break;
				case 6:
					selectSlot6ToolStripMenuItem.Checked = true;
					break;
				case 7:
					selectSlot7ToolStripMenuItem.Checked = true;
					break;
				case 8:
					selectSlot8ToolStripMenuItem.Checked = true;
					break;
				case 9:
					selectSlot9ToolStripMenuItem.Checked = true;
					break;
				default:
					break;
			}

			if (Global.Config.AutoLoadMostRecentRom == true)
				autoloadMostRecentToolStripMenuItem.Checked = true;
			else
				autoloadMostRecentToolStripMenuItem.Checked = false;

			screenshotF12ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.ScreenshotBinding;
			openROMToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.OpenROM;
			closeROMToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.CloseROM;
		}

		private void emulationToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			powerToolStripMenuItem.Enabled = !IsNullEmulator();
			resetToolStripMenuItem.Enabled = Global.Emulator.ControllerDefinition.BoolButtons.Contains("Reset");

			enableFMChipToolStripMenuItem.Checked = Global.Config.SmsEnableFM;
			overclockWhenKnownSafeToolStripMenuItem.Checked = Global.Config.SmsAllowOverlock;
			forceStereoSeparationToolStripMenuItem.Checked = Global.Config.SmsForceStereoSeparation;
            smsSpriteLimitToolStripMenuItem.Checked = Global.Config.SmsSpriteLimit;
            pceAlwaysPerformSpriteLimitToolStripMenuItem.Checked = Global.Config.PceSpriteLimit;
            pceAlwaysEqualizeVolumesToolStripMenuItem.Checked = Global.Config.PceEqualizeVolume;
            pceArcadeCardRewindEnableHackToolStripMenuItem.Checked = Global.Config.PceArcadeCardRewindHack;
			pauseToolStripMenuItem.Checked = EmulatorPaused;
			if (didMenuPause) pauseToolStripMenuItem.Checked = wasPaused;

			pauseToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.EmulatorPauseBinding;
			powerToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HardResetBinding;
			resetToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SoftResetBinding;
		}

		protected override void OnClosed(EventArgs e)
		{
			exit = true;
			base.OnClosed(e);
		}

		private void backupSavestatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.BackupSavestates ^= true;
		}

        void autoSavestatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoSavestates ^= true;
		}

		void screenshotWithSavestatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveScreenshotWithStates ^= true;
		}

		private void undoSavestateToolStripMenuItem_Click(object sender, EventArgs e)
		{
			string path = PathManager.SaveStatePrefix(Global.Game) + "." + "QuickSave" + Global.Config.SaveSlot + ".State";
			SwapBackupSavestate(path);
			Global.RenderPanel.AddMessage("Save slot " + Global.Config.SaveSlot.ToString() + " restored.");
		}

		private void FreezeStatus_Click(object sender, EventArgs e)
		{
			LoadCheatsWindow();
		}

		public void UpdateCheatStatus()
		{
			if (Global.CheatList.HasActiveCheat())
			{
				CheatStatus.ToolTipText = "Cheats are currently active";
				CheatStatus.Image = BizHawk.MultiClient.Properties.Resources.Freeze;
			}
			else
			{
				CheatStatus.ToolTipText = "";
				CheatStatus.Image = BizHawk.MultiClient.Properties.Resources.Blank;
			}
		}

		private void autofireToolStripMenuItem_Click(object sender, EventArgs e)
		{
			new AutofireConfig().ShowDialog();
		}

		private void autoLoadLastSlotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoLoadLastSaveSlot ^= true;
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
	}
}