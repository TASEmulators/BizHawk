using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Calculator;
using BizHawk.Emulation.Consoles.GB;
using BizHawk.Emulation.Consoles.Nintendo.SNES;

namespace BizHawk.MultiClient
{
	partial class MainForm
	{
		private void openROMToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenROM();
		}

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
			string details = Global.Emulator.CoreComm.RomStatusDetails;
			if (string.IsNullOrEmpty(details)) return;
			Global.Sound.StopSound();
			LogWindow.ShowReport("Dump Status Report", details, this);
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
					Global.RenderPanel.Resized = true;
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
					Global.RenderPanel.Resized = true;
			}

		}

		private void miDisplayVsync_Click(object sender, EventArgs e)
		{
			Global.Config.VSyncThrottle ^= true;
			Global.RenderPanel.Resized = true;
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
				Global.RenderPanel.Resized = true;
		}

		public void LimitFrameRateMessage()
		{
			if (Global.Config.ClockThrottle)
			{
				Global.OSD.AddMessage("Framerate limiting on");
			}
			else
			{
				Global.OSD.AddMessage("Framerate limiting off");
			}
		}


		public void VsyncMessage()
		{
			if (Global.Config.VSyncThrottle)
			{
				Global.OSD.AddMessage("Display Vsync is set to on");
			}
			else
			{
				Global.OSD.AddMessage("Display Vsync is set to off");
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
				Global.OSD.AddMessage("Autominimizing set to on");
			}
			else
			{
				Global.OSD.AddMessage("Autominimizing set to off");
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
			Global.OSD.AddMessage("Frameskipping set to " + Global.Config.FrameSkip.ToString());
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
			LoadRamWatch(true);
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

		private void powerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			RebootCore();
		}

		void RebootCore()
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
			//ControllerConfig c = new ControllerConfig();
			config.NewControllerConfig c = new config.NewControllerConfig(Global.Emulator.ControllerDefinition);
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
			HotkeyWindow h = new HotkeyWindow();
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
			TakeScreenshotToClipboard();
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

		private void PPUViewerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadNESPPU();
		}

		private void enableRewindToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.RewindEnabled ^= true;
			RewindMessage();
		}

		public void RewindMessage()
		{
			if (Global.Config.RewindEnabled)
			{
				Global.OSD.AddMessage("Rewind enabled");
			}
			else
			{
				Global.OSD.AddMessage("Rewind disabled");
			}
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
			loadstate1toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot1;
			loadstate2toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot2;
			loadstate3toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot3;
			loadstate4toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot4;
			loadstate5toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot5;
			loadstate6toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot6;
			loadstate7toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot7;
			loadstate8toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot8;
			loadstate9toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot9;
			loadstate0toolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LoadSlot0;
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
			tAStudioToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.TASTudio;
			virtualPadToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.OpenVirtualPadBinding;

			if (Global.Emulator.CoreComm.CpuTraceAvailable)
			{
				traceLoggerToolStripMenuItem.Enabled = true;
			}
			else
			{
				traceLoggerToolStripMenuItem.Enabled = false;
			}
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
		}

		private void pathsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			new PathConfig().ShowDialog();
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

			if (Global.Config.RecentRoms.Empty)
			{
				var none = new ToolStripMenuItem {Enabled = false, Text = "None"};
				recentROMToolStripMenuItem.DropDownItems.Add(none);
			}
			else
			{
				for (int x = 0; x < Global.Config.RecentRoms.Count; x++)
				{
					string path = Global.Config.RecentRoms.GetRecentFileByPosition(x);
					var item = new ToolStripMenuItem {Text = path};
					item.Click += (o, ev) => LoadRomFromRecent(path);
					recentROMToolStripMenuItem.DropDownItems.Add(item);
				}
			}

			recentROMToolStripMenuItem.DropDownItems.Add("-");
			var clearitem = new ToolStripMenuItem {Text = "&Clear"};
			clearitem.Click += (o, ev) => Global.Config.RecentRoms.Clear();
			recentROMToolStripMenuItem.DropDownItems.Add(clearitem);
			var auto = new ToolStripMenuItem {Text = "&Autoload Most Recent"};
			auto.Click += (o, ev) => UpdateAutoLoadRecentRom();
			auto.Checked = Global.Config.AutoLoadMostRecentRom;
			recentROMToolStripMenuItem.DropDownItems.Add(auto);
		}

		private void recentToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			//Clear out recent Movies list
			//repopulate it with an up to date list

			recentToolStripMenuItem.DropDownItems.Clear();

			if (Global.Config.RecentMovies.Empty)
			{
				var none = new ToolStripMenuItem {Enabled = false, Text = "None"};
				recentToolStripMenuItem.DropDownItems.Add(none);
			}
			else
			{
				for (int x = 0; x < Global.Config.RecentMovies.Count; x++)
				{
					string path = Global.Config.RecentMovies.GetRecentFileByPosition(x);
					var item = new ToolStripMenuItem {Text = path};
					item.Click += (o, ev) => LoadMoviesFromRecent(path);
					recentToolStripMenuItem.DropDownItems.Add(item);
				}
			}

			recentToolStripMenuItem.DropDownItems.Add("-");

			var clearitem = new ToolStripMenuItem {Text = "&Clear"};
			clearitem.Click += (o, ev) => Global.Config.RecentMovies.Clear();
			recentToolStripMenuItem.DropDownItems.Add(clearitem);

			var auto = new ToolStripMenuItem {Text = "&Autoload Most Recent"};
			auto.Click += (o, ev) => UpdateAutoLoadRecentMovie();
			auto.Checked = Global.Config.AutoLoadMostRecentMovie;
			recentToolStripMenuItem.DropDownItems.Add(auto);
		}

		private void screenshotAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			string path = String.Format(PathManager.ScreenshotPrefix(Global.Game) + ".{0:yyyy-MM-dd HH.mm.ss}.png", DateTime.Now);

			SaveFileDialog sfd = new SaveFileDialog
				{
					InitialDirectory = Path.GetDirectoryName(path),
					FileName = Path.GetFileName(path),
					Filter = "PNG File (*.png)|*.png"
				};

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
			ToggleBackgroundInput();
		}

		public void ToggleBackgroundInput()
		{
			Global.Config.AcceptBackgroundInput ^= true;
			if (Global.Config.AcceptBackgroundInput)
			{
				Global.OSD.AddMessage("Background Input enabled");
			}
			else
			{
				Global.OSD.AddMessage("Background Input disabled");
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
				Point p = new Point(e.X, e.Y + menuStrip1.Height);
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
			LoadRomFromRecent(Global.Config.RecentRoms.GetRecentFileByPosition(0));
		}

		private void enableContextMenuToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.ShowContextMenu ^= true;
			if (Global.Config.ShowContextMenu)
			{
				Global.OSD.AddMessage("Context menu enabled");
			}
			else
			{
				Global.OSD.AddMessage("Context menu disabled");
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
			LoadMoviesFromRecent(Global.Config.RecentMovies.GetRecentFileByPosition(0));
		}

		private void AddSubtitleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SubtitleMaker s = new SubtitleMaker();
			s.DisableFrame();
			int index = -1;
			Subtitle sub = new Subtitle();
			for (int x = 0; x < Global.MovieSession.Movie.Subtitles.Count; x++)
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
				sub = new Subtitle {Frame = Global.Emulator.Frame};
			}
			s.sub = sub;

			if (s.ShowDialog() == DialogResult.OK)
			{
				if (index >= 0)
					Global.MovieSession.Movie.Subtitles.Remove(index);
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
				cmiShowMenu.Visible = true;
				if (MainMenuStrip.Visible)
					cmiShowMenu.Text = "Hide Menu";
				else
					cmiShowMenu.Text = "Show Menu";
			}
			else
			{
				cmiShowMenu.Visible = false;
			}

			if (Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie.HasChanges)
			{
				ContextMenuStopMovieNoSaving.Visible = true;
			}
			else
			{
				ContextMenuStopMovieNoSaving.Visible = false;
			}
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
			Global.Config.DisplaySubtitles ^= true;
		}

		private void aVIWAVToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			recordAVIToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.AVIRecordBinding;
			stopAVIToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.AVIStopBinding;
			captureOSDToolStripMenuItem.Checked = Global.Config.AVI_CaptureOSD;

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

		private void viewCommentsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				EditCommentsForm c = new EditCommentsForm {ReadOnly = ReadOnly};
				c.GetMovie(Global.MovieSession.Movie);
				c.ShowDialog();
			}
		}

		private void viewSubtitlesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				EditSubtitlesForm s = new EditSubtitlesForm {ReadOnly = ReadOnly};
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

		private void readonlyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			ToggleReadOnly();
		}

		private void movieToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (Global.MovieSession.MultiTrack.IsActive)
			{
				fullMovieLoadstatesToolStripMenuItem.Enabled = false;
			}
			else
			{
				fullMovieLoadstatesToolStripMenuItem.Enabled = true;
			}

			if (Global.MovieSession.Movie.IsActive)
			{
				stopMovieToolStripMenuItem.Enabled = true;
				playFromBeginningToolStripMenuItem.Enabled = true;
				saveMovieToolStripMenuItem.Enabled = true;
			}
			else
			{
				stopMovieToolStripMenuItem.Enabled = false;
				playFromBeginningToolStripMenuItem.Enabled = false;
				saveMovieToolStripMenuItem.Enabled = false;
			}

			readonlyToolStripMenuItem.Checked = ReadOnly;
			bindSavestatesToMoviesToolStripMenuItem.Checked = Global.Config.BindSavestatesToMovies;
			automaticallyBackupMoviesToolStripMenuItem.Checked = Global.Config.EnableBackupMovies;

			readonlyToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.ReadOnlyToggleBinding;
			recordMovieToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.RecordMovieBinding;
			playMovieToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.PlayMovieBinding;
			stopMovieToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.StopMovieBinding;
			playFromBeginningToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.PlayBeginningBinding;
			saveMovieToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SaveMovieBinding;
			fullMovieLoadstatesToolStripMenuItem.Checked = Global.Config.VBAStyleMovieLoadState;

			if (Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie.HasChanges)
			{
				stopMovieWithoutSavingToolStripMenuItem.Enabled = true;
			}
			else
			{
				stopMovieWithoutSavingToolStripMenuItem.Enabled = false;
			}
		}

		private void saveConfigToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveConfig();
			Global.OSD.AddMessage("Saved settings");
		}

		private void loadConfigToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config = ConfigService.Load(PathManager.DefaultIniPath, Global.Config);
			Global.OSD.AddMessage("Config file loaded");
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

			acceptBackgroundInputToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.ToggleBackgroundInput;
		}

		private void enableToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			enableRewindToolStripMenuItem.Checked = Global.Config.RewindEnabled;
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
				movieToolStripMenuItem.Enabled = false;
				AVIWAVToolStripMenuItem.Enabled = false;
				screenshotToolStripMenuItem.Enabled = false;
				closeROMToolStripMenuItem.Enabled = false;
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
				movieToolStripMenuItem.Enabled = true;
				AVIWAVToolStripMenuItem.Enabled = true;
				screenshotToolStripMenuItem.Enabled = true;
				closeROMToolStripMenuItem.Enabled = true;
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
			}

			autoloadMostRecentToolStripMenuItem.Checked = Global.Config.AutoLoadMostRecentRom;
			screenshotF12ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.ScreenshotBinding;
			openROMToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.OpenROM;
			closeROMToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.CloseROM;
		}

		private void emulationToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			rebootCoreToolStripMenuItem.Enabled = !IsNullEmulator();

			if (Global.Emulator.ControllerDefinition.BoolButtons.Contains("Reset") &&
					(!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished))
			{
				resetToolStripMenuItem.Enabled = true;
			}
			else
			{
				resetToolStripMenuItem.Enabled = false;
			}

			if (Global.Emulator.ControllerDefinition.BoolButtons.Contains("Power") &&
				(!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished))
			{
				hardResetToolStripMenuItem.Enabled = true;
			}
			else
			{
				hardResetToolStripMenuItem.Enabled = false;
			}

			pauseToolStripMenuItem.Checked = EmulatorPaused;
			if (didMenuPause)
			{
				pauseToolStripMenuItem.Checked = wasPaused;
			}

			pauseToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.EmulatorPauseBinding;
			rebootCoreToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.RebootCoreResetBinding;
			resetToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.SoftResetBinding;
			hardResetToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HardResetBinding;
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
				Global.OSD.AddMessage("Backup savestates enabled");
			}
			else
			{
				Global.OSD.AddMessage("Backup savestates disabled");
			}
		}

		void autoSavestatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoSavestates ^= true;
			if (Global.Config.AutoSavestates)
			{
				Global.OSD.AddMessage("AutoSavestates enabled");
			}
			else
			{
				Global.OSD.AddMessage("AutoSavestates disabled");
			}
		}

		void screenshotWithSavestatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SaveScreenshotWithStates ^= true;
			if (Global.Config.SaveScreenshotWithStates)
			{
				Global.OSD.AddMessage("Screenshots will be saved in savestates");
			}
			else
			{
				Global.OSD.AddMessage("Screenshots will not be saved in savestates");
			}
		}

		private void undoSavestateToolStripMenuItem_Click(object sender, EventArgs e)
		{
			string path = PathManager.SaveStatePrefix(Global.Game) + "." + "QuickSave" + Global.Config.SaveSlot + ".State";
			SwapBackupSavestate(path);
			Global.OSD.AddMessage("Save slot " + Global.Config.SaveSlot.ToString() + " restored.");
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
			if (Global.CheatList.HasActiveCheats)
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
					Global.OSD.AddMessage("Setting to Black and White Switch to On");
				else
					Global.OSD.AddMessage("Setting to Black and White Switch to Off");
			}
		}

		private void p0DifficultyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is Atari2600)
			{
				Global.Config.Atari2600_LeftDifficulty ^= true;
				((Atari2600)Global.Emulator).SetP0Diff(Global.Config.Atari2600_BW);
				if (Global.Config.Atari2600_LeftDifficulty)
					Global.OSD.AddMessage("Setting Left Difficulty to B");
				else
					Global.OSD.AddMessage("Setting Left Difficulty to A");
			}
		}

		private void rightDifficultyToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is Atari2600)
			{
				Global.Config.Atari2600_RightDifficulty ^= true;
				((Atari2600)Global.Emulator).SetP1Diff(Global.Config.Atari2600_BW);
				if (Global.Config.Atari2600_RightDifficulty)
					Global.OSD.AddMessage("Setting Right Difficulty to B");
				else
					Global.OSD.AddMessage("Setting Right Difficulty to A");
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
					Global.OSD.AddMessage("BG 1 Layer On");
				}
				else
				{
					Global.OSD.AddMessage("BG 1 Layer Off");
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
					Global.OSD.AddMessage("BG 2 Layer On");
				}
				else
				{
					Global.OSD.AddMessage("BG 2 Layer Off");
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
					Global.OSD.AddMessage("BG 3 Layer On");
				}
				else
				{
					Global.OSD.AddMessage("BG 3 Layer Off");
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
					Global.OSD.AddMessage("BG 4 Layer On");
				}
				else
				{
					Global.OSD.AddMessage("BG 4 Layer Off");
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
					Global.OSD.AddMessage("OBJ 1 Layer On");
				}
				else
				{
					Global.OSD.AddMessage("OBJ 1 Layer Off");
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
					Global.OSD.AddMessage("OBJ 2 Layer On");
				}
				else
				{
					Global.OSD.AddMessage("OBJ 2 Layer Off");
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
					Global.OSD.AddMessage("OBJ 3 Layer On");
				}
				else
				{
					Global.OSD.AddMessage("OBJ 3 Layer Off");
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
					Global.OSD.AddMessage("OBJ 4 Layer On");
				}
				else
				{
					Global.OSD.AddMessage("OBJ 4 Layer Off");
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
		}

		private void captureOSDToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AVI_CaptureOSD ^= true;
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

		private void importMovieToolStripMenuItem_Click(object sender, EventArgs e)
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

			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return;

			foreach (string fn in ofd.FileNames)
			{
				ProcessMovieImport(fn);
			}
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
				Movie m = new Movie(filePaths[0]);
				StartNewMovie(m, false);

			}
			else if (ext.ToUpper() == ".STATE")
			{
				LoadStateFile(filePaths[0], Path.GetFileName(filePaths[0]));
			}
			else if (ext.ToUpper() == ".CHT")
			{
				LoadCheatsWindow();
				Cheats1.LoadCheatFile(filePaths[0], false);
				Cheats1.DisplayCheatsList();
			}
			else if (ext.ToUpper() == ".WCH")
			{
				LoadRamWatch(true);
				RamWatch1.LoadWatchFile(filePaths[0], false);
				RamWatch1.DisplayWatchList();
			}

			else if (MovieImport.IsValidMovieExtension(Path.GetExtension(filePaths[0])))
			{
				//tries to open a legacy movie format as if it were a BKM, by importing it

				if (CurrentlyOpenRom == null)
					OpenROM();
				else
					LoadRom(CurrentlyOpenRom);

				string errorMsg;
				string warningMsg;
				Movie m = MovieImport.ImportFile(filePaths[0], out errorMsg, out warningMsg);
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
				Global.OSD.AddMessage(warningMsg);
			}
			else
				LoadRom(filePaths[0]);
		}

		private void toolStripMenuItem6_Click(object sender, EventArgs e)
		{
			StopMovie(true);
		}

		private void stopMovieWithoutSavingToolStripMenuItem_Click(object sender, EventArgs e)
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
			Global.Sound.StopSound();
			using (var dlg = new GBtools.DualGBXMLCreator())
			{
				dlg.ShowDialog(this);
			}
			Global.Sound.StartSound();
		}

		private void tempN64PluginControlToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var result = new N64VideoPluginconfig().ShowDialog();
			if (result == DialogResult.OK)
			{
				Global.OSD.AddMessage("Plugin settings saved");
			}
			else
			{
				Global.OSD.AddMessage("Plugin settings aborted");
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
			using (var dlg = new SATTools.SaturnPrefs())
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

		private void captureOSDToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			Global.Config.Screenshot_CaptureOSD ^= true;
		}

		private void screenshotToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
		{
			captureOSDToolStripMenuItem1.Checked = Global.Config.Screenshot_CaptureOSD;
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
			Global.RenderPanel.Resized = true;
		}

		private void backupSaveramToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.BackupSaveram ^= true;
			if (Global.Config.BackupSaveram)
			{
				Global.OSD.AddMessage("Backup saveram enabled");
			}
			else
			{
				Global.OSD.AddMessage("Backup saveram disabled");
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
			Global.CoreComm.GG_ShowClippedRegions = Global.Config.GGShowClippedRegions;
		}

		private void highlightActiveDisplayRegionToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.GGHighlightActiveDisplayRegion ^= true;
			Global.CoreComm.GG_HighlightActiveDisplayRegion = Global.Config.GGHighlightActiveDisplayRegion;
		}

		private void saveMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveMovie();
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

		private void fullMovieLoadstatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VBAStyleMovieLoadState ^= true;
		}
	}
}
