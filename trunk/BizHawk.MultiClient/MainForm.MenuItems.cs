using System;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BizHawk.Core;
using BizHawk.Emulation.Consoles.Sega;
using BizHawk.Emulation.Consoles.TurboGrafx;
using BizHawk.Emulation.Consoles.Calculator;
using BizHawk.Emulation.Consoles.Gameboy;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
	partial class MainForm
	{
		private void rAMPokeToolStripMenuItem_Click(object sender, EventArgs e)
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
			var window = new BizHawk.MultiClient.tools.LuaWindow();
			window.Show();
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

		private void miSpeed50_Click(object sender, EventArgs e) { SetSpeedPercent(50); }
		private void miSpeed75_Click(object sender, EventArgs e) { SetSpeedPercent(75); }
		private void miSpeed100_Click(object sender, EventArgs e) { SetSpeedPercent(100); }
		private void miSpeed150_Click(object sender, EventArgs e) { SetSpeedPercent(150); }
		private void miSpeed200_Click(object sender, EventArgs e) { SetSpeedPercent(200); }

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
			RecordMovie r = new RecordMovie();
			r.ShowDialog();
		}

		private void playMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{
			//PlayMovie p = new PlayMovie();
			//p.ShowDialog();

            //Hacky testing
            InputLog.LoadMovie();
            InputLog.WriteMovie();
		}

		private void stopMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		private void playFromBeginningToolStripMenuItem_Click(object sender, EventArgs e)
		{

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
			SaveSlotSelectedMessage();
		}

		private void selectSlot2ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 2;
			SaveSlotSelectedMessage();
		}

		private void selectSlot3ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 3;
			SaveSlotSelectedMessage();
		}

		private void selectSlot4ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 4;
			SaveSlotSelectedMessage();
		}

		private void selectSlot5ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 5;
			SaveSlotSelectedMessage();
		}

		private void selectSlot6ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 6;
			SaveSlotSelectedMessage();
		}

		private void selectSlot7ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 7;
			SaveSlotSelectedMessage();
		}

		private void selectSlot8ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 8;
			SaveSlotSelectedMessage();
		}

		private void selectSlot9ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 9;
			SaveSlotSelectedMessage();
		}

		private void selectSlot10ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSlot = 0;
			SaveSlotSelectedMessage();
		}

		private void previousSlotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SaveSlot == 0) SaveSlot = 9;       //Wrap to end of slot list
			else if (SaveSlot > 9) SaveSlot = 9;   //Meh, just in case
			else SaveSlot--;
			SaveSlotSelectedMessage();
		}

		private void nextSlotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (SaveSlot >= 9) SaveSlot = 1;       //Wrap to beginning of slot list
			else SaveSlot++;
			SaveSlotSelectedMessage();
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
			CloseGame();
			Global.Emulator = new NullEmulator();
            RamSearch1.Restart();
			Text = "BizHawk";
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
			if (Global.Emulator.ControllerDefinition.BoolButtons.Contains("Reset"))
				Global.Emulator.Controller.ForceButton("Reset");
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
		}

		private void hotkeysToolStripMenuItem_Click(object sender, EventArgs e)
		{
			BizHawk.MultiClient.tools.HotkeyWindow h = new BizHawk.MultiClient.tools.HotkeyWindow();
			h.ShowDialog();
		}

		private void displayFPSToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisplayFPS ^= true;
		}

		private void displayFrameCounterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisplayFrameCounter ^= true;
		}

		private void displayInputToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisplayInput ^= true;
		}

		private void displayLagCounterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.DisplayLagCounter ^= true;
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
			var ofd = new OpenFileDialog();
			ofd.InitialDirectory = Global.Config.LastRomPath;
			ofd.Filter = "Rom Files|*.SMS;*.GG;*.SG;*.PCE;*.SGX;*.GB;*.BIN;*.SMD;*.ZIP;*.7z|Master System|*.SMS;*.GG;*.SG;*.ZIP;*.7z|PC Engine|*.PCE;*.SGX;*.ZIP;*.7z|Gameboy|*.GB;*.ZIP;*.7z|Archive Files|*.zip;*.7z|All Files|*.*";
			ofd.RestoreDirectory = true;

			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return;
			var file = new FileInfo(ofd.FileName);
			Global.Config.LastRomPath = file.DirectoryName;
			LoadRom(file.FullName);
		}


	}
}