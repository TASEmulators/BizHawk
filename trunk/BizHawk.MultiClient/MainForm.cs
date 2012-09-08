using System;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using BizHawk.Core;
using BizHawk.DiscSystem;
using BizHawk.Emulation.Consoles.Sega;
using BizHawk.Emulation.Consoles.TurboGrafx;
using BizHawk.Emulation.Consoles.Calculator;
using BizHawk.Emulation.Consoles.Gameboy;
using BizHawk.Emulation.Consoles.Nintendo;
using BizHawk.Emulation.Consoles.Nintendo.SNES;
using BizHawk.Emulation.Consoles.Coleco;
using BizHawk.MultiClient.tools;
using System.Collections.Generic;
using BizHawk.Emulation.Consoles.Intellivision;

namespace BizHawk.MultiClient
{

	public partial class MainForm : Form
	{
		public bool INTERIM = true;
		public const string EMUVERSION = "Version 1.0.6 interim";
		public const string RELEASEDATE = "June 17, 2012";
		private Control renderTarget;
		private RetainedViewportPanel retainedPanel;
		public string CurrentlyOpenRom;
		SavestateManager StateSlots = new SavestateManager();

		public bool PressFrameAdvance = false;
		public bool PressRewind = false;
		public bool FastForward = false;
		public bool TurboFastForward = false;
		public bool RestoreReadWriteOnStop = false;
		public bool UpdateFrame = false;

		//avi/wav state
		IVideoWriter CurrAviWriter = null;
		/// <summary>
		/// an audio proxy used for dumping
		/// </summary>
		Emulation.Sound.MetaspuSoundProvider DumpProxy = null;
		/// <summary>audio timekeeping for video dumping</summary>
		long SoundRemainder = 0;

		//runloop control
		bool exit;
		bool runloop_frameProgress;
		DateTime FrameAdvanceTimestamp = DateTime.MinValue;
		public bool EmulatorPaused;
		public EventWaitHandle MainWait;
		int runloop_fps;
		int runloop_last_fps;
		bool runloop_frameadvance;
		DateTime runloop_second;
		bool runloop_last_ff;

		Throttle throttle;
		bool unthrottled = false;

		//For handling automatic pausing when entering the menu
		private bool wasPaused = false;
		private bool didMenuPause = false;

		//tool dialogs
		public RamWatch RamWatch1 = new RamWatch();
		public RamSearch RamSearch1 = new RamSearch();
		public HexEditor HexEditor1 = new HexEditor();
		public SNESGraphicsDebugger SNESGraphicsDebugger1 = new SNESGraphicsDebugger();
		public NESNameTableViewer NESNameTableViewer1 = new NESNameTableViewer();
		public NESPPU NESPPU1 = new NESPPU();
		public NESDebugger NESDebug1 = new NESDebugger();
		public PCEBGViewer PCEBGViewer1 = new PCEBGViewer();
		public Cheats Cheats1 = new Cheats();
		public ToolBox ToolBox1 = new ToolBox();
		public TI83KeyPad TI83KeyPad1 = new TI83KeyPad();
		public TAStudio TAStudio1 = new TAStudio();
		public Debugger GBDebugger = new Debugger();
#if WINDOWS
		public LuaConsole LuaConsole1 = new LuaConsole();
#endif

		/// <summary>
		/// number of frames to autodump
		/// </summary>
		int autoDumpLength = 0;


		public MainForm(string[] args)
		{
			Global.MovieSession = new MovieSession();
			Global.MovieSession.Movie = new Movie();
			MainWait = new AutoResetEvent(false);
			Icon = BizHawk.MultiClient.Properties.Resources.logo;
			InitializeComponent();
			Global.Game = GameInfo.GetNullGame();
			if (Global.Config.ShowLogWindow)
			{
				ShowConsole();
				//PsxApi.StdioFixes();
				displayLogWindowToolStripMenuItem.Checked = true;
			}

			throttle = new Throttle();

			DiscSystem.FFMpeg.FFMpegPath = PathManager.MakeProgramRelativePath(Global.Config.FFMpegPath);

			Global.CheatList = new CheatList();
			UpdateStatusSlots();

			//in order to allow late construction of this database, we hook up a delegate here to dearchive the data and provide it on demand
			//we could background thread this later instead if we wanted to be real clever
			NES.BootGodDB.GetDatabaseBytes = () =>
			{
				using (HawkFile NesCartFile = new HawkFile(Path.Combine(PathManager.GetExeDirectoryAbsolute(), "NesCarts.7z")).BindFirst())
					return Util.ReadAllBytes(NesCartFile.GetStream());
			};
			Global.MainForm = this;
			Global.CoreInputComm = new CoreInputComm();
			SyncCoreInputComm();

			Console.WriteLine("Scanning cores:");
			foreach (var ci in Introspection.GetCoreInfo())
			{
				Console.WriteLine("{0} - {1} ({2})", ci.FriendlyName, ci.Version, ci.ClassName);
			}

			Database.LoadDatabase(Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb.txt"));

			SyncPresentationMode();

			Load += (o, e) =>
			{
				AllowDrop = true;
				DragEnter += FormDragEnter;
				DragDrop += FormDragDrop;
			};

			Closing += (o, e) =>
			{
				Global.CheatList.SaveSettings();
				CloseGame();
				Global.MovieSession.Movie.Stop();
				CloseTools();
				SaveConfig();
			};

			ResizeBegin += (o, e) =>
			{
				if (Global.Sound != null) Global.Sound.StopSound();
			};

			ResizeEnd += (o, e) =>
			{
				if (Global.RenderPanel != null) Global.RenderPanel.Resized = true;
				if (Global.Sound != null) Global.Sound.StartSound();
			};

			Input.Initialize();
			InitControls();
			Global.Emulator = new NullEmulator();
			Global.ActiveController = Global.NullControls;
			Global.AutoFireController = Global.AutofireNullControls;
#if WINDOWS
			Global.Sound = new Sound(Handle, Global.DSound);
#else
			Global.Sound = new Sound();
#endif
			Global.Sound.StartSound();
			RewireInputChain();
			//TODO - replace this with some kind of standard dictionary-yielding parser in a separate component
			string cmdRom = null;
			string cmdLoadState = null;
			string cmdMovie = null;
			string cmdDumpType = null;
			string cmdDumpName = null;

			for (int i = 0; i < args.Length; i++)
			{
				//for some reason sometimes visual studio will pass this to us on the commandline. it makes no sense.
				if (args[i] == ">")
				{
					i++;
					string stdout = args[i];
					Console.SetOut(new StreamWriter(stdout));
					continue;
				}

				string arg = args[i].ToLower();
				if (arg.StartsWith("--load-slot="))
					cmdLoadState = arg.Substring(arg.IndexOf('=') + 1);
				else if (arg.StartsWith("--movie="))
					cmdMovie = arg.Substring(arg.IndexOf('=') + 1);
				else if (arg.StartsWith("--dump-type="))
					cmdDumpType = arg.Substring(arg.IndexOf('=') + 1);
				else if (arg.StartsWith("--dump-name="))
					cmdDumpName = arg.Substring(arg.IndexOf('=') + 1);
				else if (arg.StartsWith("--dump-length="))
					int.TryParse(arg.Substring(arg.IndexOf('=') + 1), out autoDumpLength);
				else
					cmdRom = arg;
			}

			if (cmdRom != null)
			{
				//Commandline should always override auto-load
				LoadRom(cmdRom);
				if (Global.Game == null)
				{
					MessageBox.Show("Failed to load " + cmdRom + " specified on commandline");
				}
			}
			else if (Global.Config.AutoLoadMostRecentRom && !Global.Config.RecentRoms.IsEmpty())
				LoadRomFromRecent(Global.Config.RecentRoms.GetRecentFileByPosition(0));

			if (cmdMovie != null)
			{
				if (Global.Game == null)
				{
					OpenROM();
				}
				else
				{
					Movie m = new Movie(cmdMovie);
					ReadOnly = true;
					// if user is dumping and didnt supply dump length, make it as long as the loaded movie
					if (autoDumpLength == 0)
					{
						autoDumpLength = m.Frames;
					}
					StartNewMovie(m, false);
					Global.Config.RecentMovies.Add(cmdMovie);
				}
			}
			else if (Global.Config.AutoLoadMostRecentMovie && !Global.Config.RecentMovies.IsEmpty())
			{
				if (Global.Game == null)
				{
					OpenROM();
				}
				else
				{
					Movie m = new Movie(Global.Config.RecentMovies.GetRecentFileByPosition(0));
					StartNewMovie(m, false);
				}
			}

			if (cmdLoadState != null && Global.Game != null)
			{
				LoadState("QuickSave" + cmdLoadState);
			}
			else if (Global.Config.AutoLoadLastSaveSlot && Global.Game != null)
			{
				LoadState("QuickSave" + Global.Config.SaveSlot.ToString());
			}

			if (Global.Config.AutoLoadRamWatch)
			{
				if (Global.Config.DisplayRamWatch)
				{
					LoadRamWatch(false);
				}
				else
				{
					LoadRamWatch(true);
				}
			}
			if (Global.Config.AutoLoadRamSearch)
				LoadRamSearch();
			if (Global.Config.AutoLoadHexEditor)
				LoadHexEditor();
			if (Global.Config.AutoLoadCheats)
				LoadCheatsWindow();
			if (Global.Config.AutoLoadNESPPU && Global.Emulator is NES)
				LoadNESPPU();
			if (Global.Config.AutoLoadNESNameTable && Global.Emulator is NES)
				LoadNESNameTable();
			if (Global.Config.AutoLoadNESDebugger && Global.Emulator is NES)
				LoadNESDebugger();
			if (Global.Config.NESGGAutoload && Global.Emulator is NES)
				LoadGameGenieEC();
			if (Global.Config.AutoloadTAStudio)
				LoadTAStudio();
			if (Global.Config.AutoLoadLuaConsole)
				OpenLuaConsole();
			if (Global.Config.PCEBGViewerAutoload && Global.Emulator is PCEngine)
				LoadPCEBGViewer();

			if (Global.Config.MainWndx >= 0 && Global.Config.MainWndy >= 0 && Global.Config.SaveWindowPosition)
				this.Location = new Point(Global.Config.MainWndx, Global.Config.MainWndy);

			if (Global.Config.DisplayStatusBar == false)
				StatusSlot0.Visible = false;
			else
				displayStatusBarToolStripMenuItem.Checked = true;

			if (Global.Config.StartPaused)
				PauseEmulator();

			if (!INTERIM)
			{
				debuggerToolStripMenuItem.Enabled = false;
				//luaConsoleToolStripMenuItem.Enabled = false;
			}

			// start dumping, if appropriate
			if (cmdDumpType != null && cmdDumpName != null)
			{
				RecordAVI(cmdDumpType, cmdDumpName);
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (Global.DisplayManager != null) Global.DisplayManager.Dispose();
			Global.DisplayManager = null;

			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		void SyncCoreInputComm()
		{
			Global.CoreInputComm.NES_BackdropColor = Global.Config.NESBackgroundColor;
			Global.CoreInputComm.NES_UnlimitedSprites = Global.Config.NESAllowMoreThanEightSprites;
			Global.CoreInputComm.NES_ShowBG = Global.Config.NESDispBackground;
			Global.CoreInputComm.NES_ShowOBJ = Global.Config.NESDispSprites;
			Global.CoreInputComm.PCE_ShowBG1 = Global.Config.PCEDispBG1;
			Global.CoreInputComm.PCE_ShowOBJ1 = Global.Config.PCEDispOBJ1;
			Global.CoreInputComm.PCE_ShowBG2 = Global.Config.PCEDispBG2;
			Global.CoreInputComm.PCE_ShowOBJ2 = Global.Config.PCEDispOBJ2;
			Global.CoreInputComm.SMS_ShowBG = Global.Config.SMSDispBG;
			Global.CoreInputComm.SMS_ShowOBJ = Global.Config.SMSDispOBJ;

			Global.CoreInputComm.SNES_ShowBG1_0 = Global.Config.SNES_ShowBG1_0;
			Global.CoreInputComm.SNES_ShowBG1_1 = Global.Config.SNES_ShowBG1_1;
			Global.CoreInputComm.SNES_ShowBG2_0 = Global.Config.SNES_ShowBG2_0;
			Global.CoreInputComm.SNES_ShowBG2_1 = Global.Config.SNES_ShowBG2_1;
			Global.CoreInputComm.SNES_ShowBG3_0 = Global.Config.SNES_ShowBG3_0;
			Global.CoreInputComm.SNES_ShowBG3_1 = Global.Config.SNES_ShowBG3_1;
			Global.CoreInputComm.SNES_ShowBG4_0 = Global.Config.SNES_ShowBG4_0;
			Global.CoreInputComm.SNES_ShowBG4_1 = Global.Config.SNES_ShowBG4_1;
			Global.CoreInputComm.SNES_ShowOBJ_0 = Global.Config.SNES_ShowOBJ_0;
			Global.CoreInputComm.SNES_ShowOBJ_1 = Global.Config.SNES_ShowOBJ_1;
			Global.CoreInputComm.SNES_ShowOBJ_2 = Global.Config.SNES_ShowOBJ_2;
			Global.CoreInputComm.SNES_ShowOBJ_3 = Global.Config.SNES_ShowOBJ_3;
		}

		void SyncPresentationMode()
		{
			Global.DisplayManager.Suspend();

#if WINDOWS
			bool gdi = Global.Config.DisplayGDI;

			if (Global.Direct3D == null)
				gdi = true;
#endif

			if (renderTarget != null)
			{
				renderTarget.Dispose();
				Controls.Remove(renderTarget);
			}

			if (retainedPanel != null) retainedPanel.Dispose();
			if (Global.RenderPanel != null) Global.RenderPanel.Dispose();

#if WINDOWS
			if (gdi)
#endif
				renderTarget = retainedPanel = new RetainedViewportPanel();
#if WINDOWS
			else renderTarget = new ViewportPanel();
#endif
			Controls.Add(renderTarget);
			Controls.SetChildIndex(renderTarget, 0);

			renderTarget.Dock = DockStyle.Fill;
			renderTarget.BackColor = Color.Black;

#if WINDOWS
			if (gdi)
			{
#endif
				Global.RenderPanel = new SysdrawingRenderPanel(retainedPanel);
				retainedPanel.ActivateThreaded();
#if WINDOWS
			}
			else
			{
				try
				{
					var d3dPanel = new Direct3DRenderPanel(Global.Direct3D, renderTarget);
					d3dPanel.CreateDevice();
					Global.RenderPanel = d3dPanel;
				}
				catch
				{
					Program.DisplayDirect3DError();
					Global.Direct3D.Dispose();
					Global.Direct3D = null;
					SyncPresentationMode();
				}
			}
#endif

			Global.DisplayManager.Resume();
		}

		void SyncThrottle()
		{
			bool fastforward = Global.ClientControls["Fast Forward"] || FastForward || Global.ClientControls["MaxTurbo"];
			Global.ForceNoVsync = unthrottled || fastforward;

			// realtime throttle is never going to be so exact that using a double here is wrong
			throttle.SetCoreFps(Global.Emulator.CoreOutputComm.VsyncRate);

			throttle.signal_paused = EmulatorPaused || Global.Emulator is NullEmulator;
			throttle.signal_unthrottle = unthrottled;
			if (fastforward)
				throttle.SetSpeedPercent(Global.Config.SpeedPercentAlternate);
			else
				throttle.SetSpeedPercent(Global.Config.SpeedPercent);
		}

		void SetSpeedPercentAlternate(int value)
		{
			Global.Config.SpeedPercentAlternate = value;
			SyncThrottle();
			Global.OSD.AddMessage("Alternate Speed: " + value + "%");
		}

		void SetSpeedPercent(int value)
		{
			Global.Config.SpeedPercent = value;
			SyncThrottle();
			Global.OSD.AddMessage("Speed: " + value + "%");
		}

		public void ProgramRunLoop()
		{
			CheckMessages();
			LogConsole.PositionConsole();

			for (; ; )
			{
				Input.Instance.Update();
				//handle events and dispatch as a hotkey action, or a hotkey button, or an input button
				ProcessInput();
				Global.ClientControls.LatchFromPhysical(Global.HotkeyCoalescer);
				Global.ActiveController.LatchFromPhysical(Global.ControllerInputCoalescer);
				Global.ActiveController.OR_FromLogical(Global.ClickyVirtualPadController);
				Global.AutoFireController.LatchFromPhysical(Global.ControllerInputCoalescer);
				Global.ClickyVirtualPadController.FrameTick();

#if WINDOWS
				LuaConsole1.ResumeScripts(false);
#endif

				StepRunLoop_Core();
				//if(!IsNullEmulator())
				StepRunLoop_Throttle();

				Render();

				CheckMessages();
				if (exit)
					break;
				Thread.Sleep(0);
			}

			Shutdown();
		}

		void Shutdown()
		{
			if (CurrAviWriter != null)
			{
				CurrAviWriter.CloseFile();
				CurrAviWriter = null;
			}
		}

		void CheckMessages()
		{
			Application.DoEvents();
			if (ActiveForm != null)
				ScreenSaver.ResetTimerPeriodically();
		}

		public void PauseEmulator()
		{
			EmulatorPaused = true;
			PauseStrip.Image = BizHawk.MultiClient.Properties.Resources.Pause;
		}

		public void UnpauseEmulator()
		{
			EmulatorPaused = false;
			PauseStrip.Image = BizHawk.MultiClient.Properties.Resources.Blank;
		}

		public void TogglePause()
		{
			EmulatorPaused ^= true;
			if (EmulatorPaused)
			{
				PauseStrip.Image = BizHawk.MultiClient.Properties.Resources.Pause;
			}
			else
			{
				PauseStrip.Image = BizHawk.MultiClient.Properties.Resources.Blank;
			}

		}

		private void LoadRomFromRecent(string rom)
		{
			bool r = LoadRom(rom);
			if (!r)
			{
				Global.Sound.StopSound();
				DialogResult result = MessageBox.Show("Could not open " + rom + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
				if (result == DialogResult.Yes)
				{
					Global.Config.RecentRoms.Remove(rom);
				}
				Global.Sound.StartSound();
			}
		}

		private void LoadMoviesFromRecent(string movie)
		{
			Movie m = new Movie(movie);

			if (!m.Loaded)
			{
				Global.Sound.StopSound();
				DialogResult result = MessageBox.Show("Could not open " + movie + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
				if (result == DialogResult.Yes)
				{
					Global.Config.RecentMovies.Remove(movie);
				}
				Global.Sound.StartSound();
			}
			else
			{
				ReadOnly = true;
				StartNewMovie(m, false);
			}
		}

		public static ControllerDefinition ClientControlsDef = new ControllerDefinition
		{
			Name = "Emulator Frontend Controls",
			BoolButtons = { "Fast Forward", "Rewind", "Hard Reset", "Mode Flip", "Quick Save State", "Quick Load State", "Save Named State", "Load Named State",
				"Emulator Pause", "Frame Advance", "Unthrottle", "MaxTurbo", "Screenshot", "Toggle Fullscreen", "SelectSlot0", "SelectSlot1", "SelectSlot2", "SelectSlot3", "SelectSlot4",
				"SelectSlot5", "SelectSlot6", "SelectSlot7", "SelectSlot8", "SelectSlot9", "SaveSlot0", "SaveSlot1", "SaveSlot2", "SaveSlot3", "SaveSlot4",
				"SaveSlot5","SaveSlot6","SaveSlot7","SaveSlot8","SaveSlot9","LoadSlot0","LoadSlot1","LoadSlot2","LoadSlot3","LoadSlot4","LoadSlot5","LoadSlot6",
				"LoadSlot7","LoadSlot8","LoadSlot9", "ToolBox", "Previous Slot", "Next Slot", "Ram Watch", "Ram Search", "Ram Poke", "Hex Editor",
				"Lua Console", "Cheats", "Open ROM", "Close ROM", "Display FPS", "Display FrameCounter", "Display LagCounter", "Display Input", "Toggle Read Only",
				"Play Movie", "Record Movie", "Stop Movie", "Play Beginning", "Volume Up", "Volume Down", "Toggle MultiTrack", "Record All", "Record None", "Increment Player",
				"Soft Reset", "Decrement Player", "Record AVI", "Stop AVI", "Toggle Menu", "Increase Speed", "Decrease Speed", "Toggle Background Input"}
		};

		private void InitControls()
		{
			var controls = new Controller(ClientControlsDef);
			controls.BindMulti("IncreaseWindowSize", Global.Config.IncreaseWindowSize);
			controls.BindMulti("DecreaseWindowSize", Global.Config.DecreaseWindowSize);
			controls.BindMulti("Fast Forward", Global.Config.FastForwardBinding);
			controls.BindMulti("Rewind", Global.Config.RewindBinding);
			controls.BindMulti("Hard Reset", Global.Config.HardResetBinding);
			controls.BindMulti("Emulator Pause", Global.Config.EmulatorPauseBinding);
			controls.BindMulti("Frame Advance", Global.Config.FrameAdvanceBinding);
			controls.BindMulti("Increase Speed", Global.Config.IncreaseSpeedBinding);
			controls.BindMulti("Decrease Speed", Global.Config.DecreaseSpeedBinding);
			controls.BindMulti("Toggle Background Input", Global.Config.ToggleBackgroundInput);
			controls.BindMulti("Unthrottle", Global.Config.TurboBinding);
			controls.BindMulti("MaxTurbo", Global.Config.MaxTurboBinding);
			controls.BindMulti("Screenshot", Global.Config.ScreenshotBinding);
			controls.BindMulti("Toggle Fullscreen", Global.Config.ToggleFullscreenBinding);
			controls.BindMulti("Quick Save State", Global.Config.QuickSave);
			controls.BindMulti("Quick Load State", Global.Config.QuickLoad);
			controls.BindMulti("SelectSlot0", Global.Config.SelectSlot0);
			controls.BindMulti("SelectSlot1", Global.Config.SelectSlot1);
			controls.BindMulti("SelectSlot2", Global.Config.SelectSlot2);
			controls.BindMulti("SelectSlot3", Global.Config.SelectSlot3);
			controls.BindMulti("SelectSlot4", Global.Config.SelectSlot4);
			controls.BindMulti("SelectSlot5", Global.Config.SelectSlot5);
			controls.BindMulti("SelectSlot6", Global.Config.SelectSlot6);
			controls.BindMulti("SelectSlot7", Global.Config.SelectSlot7);
			controls.BindMulti("SelectSlot8", Global.Config.SelectSlot8);
			controls.BindMulti("SelectSlot9", Global.Config.SelectSlot9);
			controls.BindMulti("SaveSlot0", Global.Config.SaveSlot0);
			controls.BindMulti("SaveSlot1", Global.Config.SaveSlot1);
			controls.BindMulti("SaveSlot2", Global.Config.SaveSlot2);
			controls.BindMulti("SaveSlot3", Global.Config.SaveSlot3);
			controls.BindMulti("SaveSlot4", Global.Config.SaveSlot4);
			controls.BindMulti("SaveSlot5", Global.Config.SaveSlot5);
			controls.BindMulti("SaveSlot6", Global.Config.SaveSlot6);
			controls.BindMulti("SaveSlot7", Global.Config.SaveSlot7);
			controls.BindMulti("SaveSlot8", Global.Config.SaveSlot8);
			controls.BindMulti("SaveSlot9", Global.Config.SaveSlot9);
			controls.BindMulti("LoadSlot0", Global.Config.LoadSlot0);
			controls.BindMulti("LoadSlot1", Global.Config.LoadSlot1);
			controls.BindMulti("LoadSlot2", Global.Config.LoadSlot2);
			controls.BindMulti("LoadSlot3", Global.Config.LoadSlot3);
			controls.BindMulti("LoadSlot4", Global.Config.LoadSlot4);
			controls.BindMulti("LoadSlot5", Global.Config.LoadSlot5);
			controls.BindMulti("LoadSlot6", Global.Config.LoadSlot6);
			controls.BindMulti("LoadSlot7", Global.Config.LoadSlot7);
			controls.BindMulti("LoadSlot8", Global.Config.LoadSlot8);
			controls.BindMulti("LoadSlot9", Global.Config.LoadSlot9);
			controls.BindMulti("ToolBox", Global.Config.ToolBox);
			controls.BindMulti("Save Named State", Global.Config.SaveNamedState);
			controls.BindMulti("Load Named State", Global.Config.LoadNamedState);
			controls.BindMulti("Previous Slot", Global.Config.PreviousSlot);
			controls.BindMulti("Next Slot", Global.Config.NextSlot);
			controls.BindMulti("Ram Watch", Global.Config.RamWatch);
			controls.BindMulti("TASTudio", Global.Config.TASTudio);
			controls.BindMulti("Ram Search", Global.Config.RamSearch);
			controls.BindMulti("Ram Poke", Global.Config.RamPoke);
			controls.BindMulti("Hex Editor", Global.Config.HexEditor);
			controls.BindMulti("Lua Console", Global.Config.LuaConsole);
			controls.BindMulti("Cheats", Global.Config.Cheats);
			controls.BindMulti("Open ROM", Global.Config.OpenROM);
			controls.BindMulti("Close ROM", Global.Config.CloseROM);
			controls.BindMulti("Display FPS", Global.Config.FPSBinding);
			controls.BindMulti("Display FrameCounter", Global.Config.FrameCounterBinding);
			controls.BindMulti("Display LagCounter", Global.Config.LagCounterBinding);
			controls.BindMulti("Display Input", Global.Config.InputDisplayBinding);
			controls.BindMulti("Toggle Read Only", Global.Config.ReadOnlyToggleBinding);
			controls.BindMulti("Play Movie", Global.Config.PlayMovieBinding);
			controls.BindMulti("Record Movie", Global.Config.RecordMovieBinding);
			controls.BindMulti("Stop Movie", Global.Config.StopMovieBinding);
			controls.BindMulti("Play Beginning", Global.Config.PlayBeginningBinding);
			controls.BindMulti("Volume Up", Global.Config.VolUpBinding);
			controls.BindMulti("Volume Down", Global.Config.VolDownBinding);
			controls.BindMulti("Toggle MultiTrack", Global.Config.ToggleMultiTrack);
			controls.BindMulti("Record All", Global.Config.MTRecordAll);
			controls.BindMulti("Record None", Global.Config.MTRecordNone);
			controls.BindMulti("Increment Player", Global.Config.MTIncrementPlayer);
			controls.BindMulti("Decrement Player", Global.Config.MTDecrementPlayer);
			controls.BindMulti("Soft Reset", Global.Config.SoftResetBinding);
			controls.BindMulti("Record AVI", Global.Config.AVIRecordBinding);
			controls.BindMulti("Stop AVI", Global.Config.AVIStopBinding);
			controls.BindMulti("Toggle Menu", Global.Config.ToggleMenuBinding);

			Global.ClientControls = controls;

			Global.NullControls = new Controller(NullEmulator.NullController);
			Global.AutofireNullControls = new AutofireController(NullEmulator.NullController);

			var smsControls = new Controller(SMS.SmsController);
			smsControls.BindMulti("Reset", Global.Config.SmsReset);
			smsControls.BindMulti("Pause", Global.Config.SmsPause);
			for (int i = 0; i < 2; i++)
			{
				smsControls.BindMulti(string.Format("P{0} Up", i + 1), Global.Config.SMSController[i].Up);
				smsControls.BindMulti(string.Format("P{0} Left", i + 1), Global.Config.SMSController[i].Left);
				smsControls.BindMulti(string.Format("P{0} Right", i + 1), Global.Config.SMSController[i].Right);
				smsControls.BindMulti(string.Format("P{0} Down", i + 1), Global.Config.SMSController[i].Down);
				smsControls.BindMulti(string.Format("P{0} B1", i + 1), Global.Config.SMSController[i].B1);
				smsControls.BindMulti(string.Format("P{0} B2", i + 1), Global.Config.SMSController[i].B2);
			}
			Global.SMSControls = smsControls;

			var asmsControls = new AutofireController(SMS.SmsController);
			asmsControls.Autofire = true;
			asmsControls.BindMulti("Reset", Global.Config.SmsReset);
			asmsControls.BindMulti("Pause", Global.Config.SmsPause);
			for (int i = 0; i < 2; i++)
			{
				asmsControls.BindMulti(string.Format("P{0} Up", i + 1), Global.Config.SMSAutoController[i].Up);
				asmsControls.BindMulti(string.Format("P{0} Left", i + 1), Global.Config.SMSAutoController[i].Left);
				asmsControls.BindMulti(string.Format("P{0} Right", i + 1), Global.Config.SMSAutoController[i].Right);
				asmsControls.BindMulti(string.Format("P{0} Down", i + 1), Global.Config.SMSAutoController[i].Down);
				asmsControls.BindMulti(string.Format("P{0} B1", i + 1), Global.Config.SMSAutoController[i].B1);
				asmsControls.BindMulti(string.Format("P{0} B2", i + 1), Global.Config.SMSAutoController[i].B2);
			}
			Global.AutofireSMSControls = asmsControls;

			var pceControls = new Controller(PCEngine.PCEngineController);
			for (int i = 0; i < 5; i++)
			{
				pceControls.BindMulti("P" + (i + 1) + " Up", Global.Config.PCEController[i].Up);
				pceControls.BindMulti("P" + (i + 1) + " Down", Global.Config.PCEController[i].Down);
				pceControls.BindMulti("P" + (i + 1) + " Left", Global.Config.PCEController[i].Left);
				pceControls.BindMulti("P" + (i + 1) + " Right", Global.Config.PCEController[i].Right);

				pceControls.BindMulti("P" + (i + 1) + " B2", Global.Config.PCEController[i].II);
				pceControls.BindMulti("P" + (i + 1) + " B1", Global.Config.PCEController[i].I);
				pceControls.BindMulti("P" + (i + 1) + " Select", Global.Config.PCEController[i].Select);
				pceControls.BindMulti("P" + (i + 1) + " Run", Global.Config.PCEController[i].Run);
			}
			Global.PCEControls = pceControls;

			var apceControls = new AutofireController(PCEngine.PCEngineController);
			apceControls.Autofire = true;
			for (int i = 0; i < 5; i++)
			{
				apceControls.BindMulti("P" + (i + 1) + " Up", Global.Config.PCEAutoController[i].Up);
				apceControls.BindMulti("P" + (i + 1) + " Down", Global.Config.PCEAutoController[i].Down);
				apceControls.BindMulti("P" + (i + 1) + " Left", Global.Config.PCEAutoController[i].Left);
				apceControls.BindMulti("P" + (i + 1) + " Right", Global.Config.PCEAutoController[i].Right);

				apceControls.BindMulti("P" + (i + 1) + " B2", Global.Config.PCEAutoController[i].II);
				apceControls.BindMulti("P" + (i + 1) + " B1", Global.Config.PCEAutoController[i].I);
				apceControls.BindMulti("P" + (i + 1) + " Select", Global.Config.PCEAutoController[i].Select);
				apceControls.BindMulti("P" + (i + 1) + " Run", Global.Config.PCEAutoController[i].Run);
			}
			Global.AutofirePCEControls = apceControls;

			var snesControls = new Controller(LibsnesCore.SNESController);

			for (int i = 0; i < 2 /*TODO*/; i++)
			{
				snesControls.BindMulti("P" + (i + 1) + " Up", Global.Config.SNESController[i].Up);
				snesControls.BindMulti("P" + (i + 1) + " Down", Global.Config.SNESController[i].Down);
				snesControls.BindMulti("P" + (i + 1) + " Left", Global.Config.SNESController[i].Left);
				snesControls.BindMulti("P" + (i + 1) + " Right", Global.Config.SNESController[i].Right);
				snesControls.BindMulti("P" + (i + 1) + " A", Global.Config.SNESController[i].A);
				snesControls.BindMulti("P" + (i + 1) + " B", Global.Config.SNESController[i].B);
				snesControls.BindMulti("P" + (i + 1) + " X", Global.Config.SNESController[i].X);
				snesControls.BindMulti("P" + (i + 1) + " Y", Global.Config.SNESController[i].Y);
				snesControls.BindMulti("P" + (i + 1) + " L", Global.Config.SNESController[i].L);
				snesControls.BindMulti("P" + (i + 1) + " R", Global.Config.SNESController[i].R);
				snesControls.BindMulti("P" + (i + 1) + " Select", Global.Config.NESController[i].Select);
				snesControls.BindMulti("P" + (i + 1) + " Start", Global.Config.NESController[i].Start);
			}
			Global.SNESControls = snesControls;

			var nesControls = new Controller(NES.NESController);

			for (int i = 0; i < 2 /*TODO*/; i++)
			{
				nesControls.BindMulti("P" + (i + 1) + " Up", Global.Config.NESController[i].Up);
				nesControls.BindMulti("P" + (i + 1) + " Down", Global.Config.NESController[i].Down);
				nesControls.BindMulti("P" + (i + 1) + " Left", Global.Config.NESController[i].Left);
				nesControls.BindMulti("P" + (i + 1) + " Right", Global.Config.NESController[i].Right);
				nesControls.BindMulti("P" + (i + 1) + " A", Global.Config.NESController[i].A);
				nesControls.BindMulti("P" + (i + 1) + " B", Global.Config.NESController[i].B);
				nesControls.BindMulti("P" + (i + 1) + " Select", Global.Config.NESController[i].Select);
				nesControls.BindMulti("P" + (i + 1) + " Start", Global.Config.NESController[i].Start);
			}
			Global.NESControls = nesControls;

			var anesControls = new AutofireController(NES.NESController);
			anesControls.Autofire = true;

			for (int i = 0; i < 2 /*TODO*/; i++)
			{
				anesControls.BindMulti("P" + (i + 1) + " Up", Global.Config.NESAutoController[i].Up);
				anesControls.BindMulti("P" + (i + 1) + " Down", Global.Config.NESAutoController[i].Down);
				anesControls.BindMulti("P" + (i + 1) + " Left", Global.Config.NESAutoController[i].Left);
				anesControls.BindMulti("P" + (i + 1) + " Right", Global.Config.NESAutoController[i].Right);
				anesControls.BindMulti("P" + (i + 1) + " A", Global.Config.NESAutoController[i].A);
				anesControls.BindMulti("P" + (i + 1) + " B", Global.Config.NESAutoController[i].B);
				anesControls.BindMulti("P" + (i + 1) + " Select", Global.Config.NESAutoController[i].Select);
				anesControls.BindMulti("P" + (i + 1) + " Start", Global.Config.NESAutoController[i].Start);
			}
			Global.AutofireNESControls = anesControls;

			var gbControls = new Controller(Gameboy.GbController);
			gbControls.BindMulti("Up", Global.Config.GBController[0].Up);
			gbControls.BindMulti("Down", Global.Config.GBController[0].Down);
			gbControls.BindMulti("Left", Global.Config.GBController[0].Left);
			gbControls.BindMulti("Right", Global.Config.GBController[0].Right);
			gbControls.BindMulti("A", Global.Config.GBController[0].A);
			gbControls.BindMulti("B", Global.Config.GBController[0].B);
			gbControls.BindMulti("Select", Global.Config.GBController[0].Select);
			gbControls.BindMulti("Start", Global.Config.GBController[0].Start);
			Global.GBControls = gbControls;

			var agbControls = new AutofireController(Gameboy.GbController);
			agbControls.Autofire = true;
			agbControls.BindMulti("Up", Global.Config.GBAutoController[0].Up);
			agbControls.BindMulti("Down", Global.Config.GBAutoController[0].Down);
			agbControls.BindMulti("Left", Global.Config.GBAutoController[0].Left);
			agbControls.BindMulti("Right", Global.Config.GBAutoController[0].Right);
			agbControls.BindMulti("A", Global.Config.GBAutoController[0].A);
			agbControls.BindMulti("B", Global.Config.GBAutoController[0].B);
			agbControls.BindMulti("Select", Global.Config.GBAutoController[0].Select);
			agbControls.BindMulti("Start", Global.Config.GBAutoController[0].Start);
			Global.AutofireGBControls = agbControls;

			var genControls = new Controller(Genesis.GenesisController);
			genControls.BindMulti("P1 Up", Global.Config.GenesisController[0].Up);
			genControls.BindMulti("P1 Left", Global.Config.GenesisController[0].Left);
			genControls.BindMulti("P1 Right", Global.Config.GenesisController[0].Right);
			genControls.BindMulti("P1 Down", Global.Config.GenesisController[0].Down);
			genControls.BindMulti("P1 A", Global.Config.GenesisController[0].A);
			genControls.BindMulti("P1 B", Global.Config.GenesisController[0].B);
			genControls.BindMulti("P1 C", Global.Config.GenesisController[0].C);
			genControls.BindMulti("P1 Start", Global.Config.GenesisController[0].Start);
			Global.GenControls = genControls;

			var agenControls = new AutofireController(Genesis.GenesisController);
			agenControls.BindMulti("P1 Up", Global.Config.GenesisAutoController[0].Up);
			agenControls.BindMulti("P1 Left", Global.Config.GenesisAutoController[0].Left);
			agenControls.BindMulti("P1 Right", Global.Config.GenesisAutoController[0].Right);
			agenControls.BindMulti("P1 Down", Global.Config.GenesisAutoController[0].Down);
			agenControls.BindMulti("P1 A", Global.Config.GenesisAutoController[0].A);
			agenControls.BindMulti("P1 B", Global.Config.GenesisAutoController[0].B);
			agenControls.BindMulti("P1 C", Global.Config.GenesisAutoController[0].C);
			agenControls.BindMulti("P1 Start", Global.Config.GenesisAutoController[0].Start);
			Global.AutofireGenControls = agenControls;

			var a2600Controls = new Controller(Atari2600.Atari2600ControllerDefinition);
			a2600Controls.BindMulti("P1 Up", Global.Config.Atari2600Controller[0].Up);
			a2600Controls.BindMulti("P1 Left", Global.Config.Atari2600Controller[0].Left);
			a2600Controls.BindMulti("P1 Right", Global.Config.Atari2600Controller[0].Right);
			a2600Controls.BindMulti("P1 Down", Global.Config.Atari2600Controller[0].Down);
			a2600Controls.BindMulti("P1 Button", Global.Config.Atari2600Controller[0].Button);
			a2600Controls.BindMulti("P2 Up", Global.Config.Atari2600Controller[1].Up);
			a2600Controls.BindMulti("P2 Left", Global.Config.Atari2600Controller[1].Left);
			a2600Controls.BindMulti("P2 Right", Global.Config.Atari2600Controller[1].Right);
			a2600Controls.BindMulti("P2 Down", Global.Config.Atari2600Controller[1].Down);
			a2600Controls.BindMulti("P2 Button", Global.Config.Atari2600Controller[1].Button);
			a2600Controls.BindMulti("Reset", Global.Config.Atari2600ConsoleButtons[0].Reset);
			a2600Controls.BindMulti("Select", Global.Config.Atari2600ConsoleButtons[0].Select);
			Global.Atari2600Controls = a2600Controls;

			var autofireA2600Controls = new AutofireController(Atari2600.Atari2600ControllerDefinition);
			autofireA2600Controls.BindMulti("P1 Up", Global.Config.Atari2600AutoController[0].Up);
			autofireA2600Controls.BindMulti("P1 Left", Global.Config.Atari2600AutoController[0].Left);
			autofireA2600Controls.BindMulti("P1 Right", Global.Config.Atari2600AutoController[0].Right);
			autofireA2600Controls.BindMulti("P1 Down", Global.Config.Atari2600AutoController[0].Down);
			autofireA2600Controls.BindMulti("P1 Button", Global.Config.Atari2600AutoController[0].Button);
			autofireA2600Controls.BindMulti("P2 Up", Global.Config.Atari2600AutoController[1].Up);
			autofireA2600Controls.BindMulti("P2 Left", Global.Config.Atari2600AutoController[1].Left);
			autofireA2600Controls.BindMulti("P2 Right", Global.Config.Atari2600AutoController[1].Right);
			autofireA2600Controls.BindMulti("P2 Down", Global.Config.Atari2600AutoController[1].Down);
			autofireA2600Controls.BindMulti("P2 Button", Global.Config.Atari2600Controller[1].Button);
			Global.AutofireAtari2600Controls = autofireA2600Controls;

			var colecoControls = new Controller(ColecoVision.ColecoVisionControllerDefinition);
			colecoControls.BindMulti("P1 Up", Global.Config.ColecoController.Up);
			colecoControls.BindMulti("P1 Left", Global.Config.ColecoController.Left);
			colecoControls.BindMulti("P1 Right", Global.Config.ColecoController.Right);
			colecoControls.BindMulti("P1 Down", Global.Config.ColecoController.Down);
			colecoControls.BindMulti("P1 L1", Global.Config.ColecoController.L1);
			colecoControls.BindMulti("P1 L2", Global.Config.ColecoController.L2);
			colecoControls.BindMulti("P1 R1", Global.Config.ColecoController.R1);
			colecoControls.BindMulti("P1 R2", Global.Config.ColecoController.R2);
			colecoControls.BindMulti("P1 Key1", Global.Config.ColecoController._1);
			colecoControls.BindMulti("P1 Key2", Global.Config.ColecoController._2);
			colecoControls.BindMulti("P1 Key3", Global.Config.ColecoController._3);
			colecoControls.BindMulti("P1 Key4", Global.Config.ColecoController._4);
			colecoControls.BindMulti("P1 Key5", Global.Config.ColecoController._5);
			colecoControls.BindMulti("P1 Key6", Global.Config.ColecoController._6);
			colecoControls.BindMulti("P1 Key7", Global.Config.ColecoController._7);
			colecoControls.BindMulti("P1 Key8", Global.Config.ColecoController._8);
			colecoControls.BindMulti("P1 Key9", Global.Config.ColecoController._9);
			colecoControls.BindMulti("P1 Star", Global.Config.ColecoController.Star);
			colecoControls.BindMulti("P1 Pound", Global.Config.ColecoController.Pound);
			Global.ColecoControls = colecoControls;

			var TI83Controls = new Controller(TI83.TI83Controller);
			TI83Controls.BindMulti("0", Global.Config.TI83Controller[0]._0);
			TI83Controls.BindMulti("1", Global.Config.TI83Controller[0]._1);
			TI83Controls.BindMulti("2", Global.Config.TI83Controller[0]._2);
			TI83Controls.BindMulti("3", Global.Config.TI83Controller[0]._3);
			TI83Controls.BindMulti("4", Global.Config.TI83Controller[0]._4);
			TI83Controls.BindMulti("5", Global.Config.TI83Controller[0]._5);
			TI83Controls.BindMulti("6", Global.Config.TI83Controller[0]._6);
			TI83Controls.BindMulti("7", Global.Config.TI83Controller[0]._7);
			TI83Controls.BindMulti("8", Global.Config.TI83Controller[0]._8);
			TI83Controls.BindMulti("9", Global.Config.TI83Controller[0]._9);
			TI83Controls.BindMulti("ON", Global.Config.TI83Controller[0].ON);
			TI83Controls.BindMulti("ENTER", Global.Config.TI83Controller[0].ENTER);
			TI83Controls.BindMulti("DOWN", Global.Config.TI83Controller[0].DOWN);
			TI83Controls.BindMulti("LEFT", Global.Config.TI83Controller[0].LEFT);
			TI83Controls.BindMulti("RIGHT", Global.Config.TI83Controller[0].RIGHT);
			TI83Controls.BindMulti("UP", Global.Config.TI83Controller[0].UP);
			TI83Controls.BindMulti("PLUS", Global.Config.TI83Controller[0].PLUS);
			TI83Controls.BindMulti("MINUS", Global.Config.TI83Controller[0].MINUS);
			TI83Controls.BindMulti("MULTIPLY", Global.Config.TI83Controller[0].MULTIPLY);
			TI83Controls.BindMulti("DIVIDE", Global.Config.TI83Controller[0].DIVIDE);
			TI83Controls.BindMulti("CLEAR", Global.Config.TI83Controller[0].CLEAR);
			TI83Controls.BindMulti("DOT", Global.Config.TI83Controller[0].DOT);
			TI83Controls.BindMulti("EXP", Global.Config.TI83Controller[0].EXP);
			TI83Controls.BindMulti("DASH", Global.Config.TI83Controller[0].DASH);
			TI83Controls.BindMulti("PARACLOSE", Global.Config.TI83Controller[0].DASH);
			TI83Controls.BindMulti("TAN", Global.Config.TI83Controller[0].TAN);
			TI83Controls.BindMulti("VARS", Global.Config.TI83Controller[0].VARS);
			TI83Controls.BindMulti("PARAOPEN", Global.Config.TI83Controller[0].PARAOPEN);
			TI83Controls.BindMulti("COS", Global.Config.TI83Controller[0].COS);
			TI83Controls.BindMulti("PRGM", Global.Config.TI83Controller[0].PRGM);
			TI83Controls.BindMulti("STAT", Global.Config.TI83Controller[0].STAT);
			TI83Controls.BindMulti("COMMA", Global.Config.TI83Controller[0].COMMA);
			TI83Controls.BindMulti("SIN", Global.Config.TI83Controller[0].SIN);
			TI83Controls.BindMulti("MATRIX", Global.Config.TI83Controller[0].MATRIX);
			TI83Controls.BindMulti("X", Global.Config.TI83Controller[0].X);
			TI83Controls.BindMulti("STO", Global.Config.TI83Controller[0].STO);
			TI83Controls.BindMulti("LN", Global.Config.TI83Controller[0].LN);
			TI83Controls.BindMulti("LOG", Global.Config.TI83Controller[0].LOG);
			TI83Controls.BindMulti("SQUARED", Global.Config.TI83Controller[0].SQUARED);
			TI83Controls.BindMulti("NEG1", Global.Config.TI83Controller[0].NEG1);
			TI83Controls.BindMulti("MATH", Global.Config.TI83Controller[0].MATH);
			TI83Controls.BindMulti("ALPHA", Global.Config.TI83Controller[0].ALPHA);
			TI83Controls.BindMulti("GRAPH", Global.Config.TI83Controller[0].GRAPH);
			TI83Controls.BindMulti("TRACE", Global.Config.TI83Controller[0].TRACE);
			TI83Controls.BindMulti("ZOOM", Global.Config.TI83Controller[0].ZOOM);
			TI83Controls.BindMulti("WINDOW", Global.Config.TI83Controller[0].WINDOW);
			TI83Controls.BindMulti("Y", Global.Config.TI83Controller[0].Y);
			TI83Controls.BindMulti("2ND", Global.Config.TI83Controller[0].SECOND);
			TI83Controls.BindMulti("MODE", Global.Config.TI83Controller[0].MODE);
			TI83Controls.BindMulti("DEL", Global.Config.TI83Controller[0].DEL);
			Global.TI83Controls = TI83Controls;
		}

		private static void FormDragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private bool IsValidMovieExtension(string ext)
		{
			if (ext.ToUpper() == "." + Global.Config.MovieExtension)
				return true;
			else if (ext.ToUpper() == ".TAS")
				return true;
			else if (ext.ToUpper() == ".BKM")
				return true;

			return false;
		}

		private void FormDragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);

			bool isLua = false;
			foreach (string path in filePaths)
			{
				if (Path.GetExtension(path).ToUpper() == ".LUA")
				{
					OpenLuaConsole();
					LuaConsole1.LoadLuaFile(path);
					isLua = true;
				}
			}
			if (isLua)
				return;

			if (Path.GetExtension(filePaths[0]).ToUpper() == ".LUASES")
			{
				OpenLuaConsole();
				LuaConsole1.LoadLuaSession(filePaths[0]);
			}

			else if (IsValidMovieExtension(Path.GetExtension(filePaths[0])))
			{
				Movie m = new Movie(filePaths[0]);
				StartNewMovie(m, false);

			}
			else if (Path.GetExtension(filePaths[0]).ToUpper() == ".STATE")
				LoadStateFile(filePaths[0], Path.GetFileName(filePaths[0]));
			else if (Path.GetExtension(filePaths[0]).ToUpper() == ".CHT")
			{
				LoadCheatsWindow();
				Cheats1.LoadCheatFile(filePaths[0], false);
				Cheats1.DisplayCheatsList();
			}
			else if (Path.GetExtension(filePaths[0]).ToUpper() == ".WCH")
			{
				LoadRamWatch(true);
				RamWatch1.LoadWatchFile(filePaths[0], false);
				RamWatch1.DisplayWatchList();
			}

			else if (MovieImport.IsValidMovieExtension(Path.GetExtension(filePaths[0])))
			{
				if (CurrentlyOpenRom == null)
					OpenROM();
				else
					LoadRom(CurrentlyOpenRom);

				string errorMsg = "";
				string warningMsg = "";
				Movie m = MovieImport.ImportFile(filePaths[0], out errorMsg, out warningMsg);
				if (errorMsg.Length > 0)
				{
					MessageBox.Show(errorMsg, "Conversion error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				else
				{
					StartNewMovie(m, false);
				}
				Global.OSD.AddMessage(warningMsg);
			}
			else
				LoadRom(filePaths[0]);
		}

		public bool IsNullEmulator()
		{
			if (Global.Emulator is NullEmulator)
				return true;
			else
				return false;
		}

		private string DisplayNameForSystem(string system)
		{
			string str = "";
			switch (system)
			{
				case "INTV": str += "Intellivision"; break;
				case "SG": str += "SG-1000"; break;
				case "SMS": str += "Sega Master System"; break;
				case "GG": str += "Game Gear"; break;
				case "PCECD": str += "TurboGrafx-16 (CD)"; break;
				case "PCE": str += "TurboGrafx-16"; break;
				case "SGX": str += "SuperGrafx"; break;
				case "GEN": str += "Genesis"; break;
				case "TI83": str += "TI-83"; break;
				case "NES": str += "NES"; break;
				case "SNES": str += "SNES"; break;
				case "GB": str += "Game Boy"; break;
				case "A26": str += "Atari 2600"; break;
			}

			if (INTERIM) str += " (interim)";
			return str;
		}

		public string GetMovieExtName()
		{
			string str = "", system = Global.Game.System, ext = GetAlternateExt();
			switch (system)
			{
				case "SG": str += "SG-1000"; break;
				case "SMS": str += "Sega Master System"; break;
				case "GG": str += "Game Gear"; break;
				case "PCECD": str += "TurboGrafx-16 (CD)"; break;
				case "PCE": str += "TurboGrafx-16"; break;
				case "SGX": str += "SuperGrafx"; break;
				case "GEN": str += "Genesis"; break;
				case "TI83": str += "TI-83"; break;
				case "NES": str += "NES"; break;
				case "GB": str += "Game Boy"; break;
			}
			return str + " Movie File (*" + ext + ")|*" + ext;
		}

		private string GetAlternateExt()
		{
			string str = ".", system = Global.Game.System;
			switch (system)
			{
				case "SG": str += "1000"; break;
				case "SMS": str += "sms"; break;
				case "GG": str += "gg"; break;
				case "PCECD": str += "pcecd"; break;
				case "PCE": str += "pce"; break;
				case "SGX": str += "sgx"; break;
				case "GEN": str += "gen"; break;
				case "TI83": str += "ti83"; break;
				case "NES": str += "nes"; break;
				case "GB": str += "gb"; break;
			}

			return str + "." + Global.Config.MovieExtension;
		}

		private void HandlePlatformMenus()
		{
			string system = "";
			if (Global.Game != null)
				system = Global.Game.System;

			tI83ToolStripMenuItem.Visible = false;
			NESToolStripMenuItem.Visible = false;
			pCEToolStripMenuItem.Visible = false;
			sMSToolStripMenuItem.Visible = false;
			gBToolStripMenuItem.Visible = false;
			atariToolStripMenuItem.Visible = false;
			switch (system)
			{

				case "TI83":
					tI83ToolStripMenuItem.Visible = true;
					break;
				case "NES":
					NESToolStripMenuItem.Visible = true;
					break;
				case "PCE":
				case "PCECD":
				case "SGX":
					pCEToolStripMenuItem.Visible = true;
					break;
				case "SMS":
				case "SG":
					sMSToolStripMenuItem.Visible = true;
					break;
				case "GB":
					gBToolStripMenuItem.Visible = true;
					break;
				case "A26":
					atariToolStripMenuItem.Visible = true;
					break;
				default:
					break;
			}
		}

		void SyncControls()
		{
			if (Global.Game == null) return;
			switch (Global.Game.System)
			{

				case "SG":
				case "SMS":
					Global.ActiveController = Global.SMSControls;
					Global.AutoFireController = Global.AutofireSMSControls;
					break;
				case "GG":
					Global.ActiveController = Global.SMSControls;
					Global.AutoFireController = Global.AutofireSMSControls;
					break;
				case "A26":
					Global.ActiveController = Global.Atari2600Controls;
					Global.AutoFireController = Global.AutofireAtari2600Controls;
					break;
				case "PCE":
				case "PCECD":
					Global.ActiveController = Global.PCEControls;
					Global.AutoFireController = Global.AutofirePCEControls;
					break;
				case "SGX":
					Global.ActiveController = Global.PCEControls;
					Global.AutoFireController = Global.AutofirePCEControls;
					break;
				case "GEN":
					Global.ActiveController = Global.GenControls;
					Global.AutoFireController = Global.AutofireGenControls;
					break;
				case "TI83":
					Global.ActiveController = Global.TI83Controls;
					break;
				case "NES":
					Global.ActiveController = Global.NESControls;
					Global.AutoFireController = Global.AutofireNESControls;
					break;
				case "SNES":
					Global.ActiveController = Global.SNESControls;
					break;
				case "GB":
					Global.ActiveController = Global.GBControls;
					Global.AutoFireController = Global.AutofireGBControls;
					break;
				case "COLV":
					Global.ActiveController = Global.ColecoControls;
					break;
				default:
					Global.ActiveController = Global.NullControls;
					break;
			}

			RewireInputChain();
		}

		void RewireInputChain()
		{
			Global.ControllerInputCoalescer = new InputCoalescer();

			Global.ControllerInputCoalescer.Type = Global.ActiveController.Type;

			Global.OrControllerAdapter.Source = Global.ActiveController;
			Global.OrControllerAdapter.SourceOr = Global.AutoFireController;
			Global.UD_LR_ControllerAdapter.Source = Global.OrControllerAdapter;

			Global.StickyXORAdapter.Source = Global.UD_LR_ControllerAdapter;

			Global.MultitrackRewiringControllerAdapter.Source = Global.StickyXORAdapter;
			Global.MovieInputSourceAdapter.Source = Global.MultitrackRewiringControllerAdapter;
			Global.ControllerOutput.Source = Global.MovieOutputHardpoint;

			Global.Emulator.Controller = Global.ControllerOutput;
			Global.MovieSession.MovieControllerAdapter.Type = Global.MovieInputSourceAdapter.Type;

			//connect the movie session before MovieOutputHardpoint if it is doing anything
			//otherwise connect the MovieInputSourceAdapter to it, effectively bypassing the movie session
			if (Global.MovieSession.Movie != null)
				Global.MovieOutputHardpoint.Source = Global.MovieSession.MovieControllerAdapter;
			else
				Global.MovieOutputHardpoint.Source = Global.MovieInputSourceAdapter;
		}

		public bool LoadRom(string path)
		{
			if (path == null) return false;
			using (var file = new HawkFile())
			{
				string[] romExtensions = new string[] { "SMS", "SMC", "SFC", "PCE", "SGX", "GG", "SG", "BIN", "GEN", "SMD", "GB", "NES", "ROM", "INT" };

				//lets not use this unless we need to
				//file.NonArchiveExtensions = romExtensions;
				file.Open(path);

				//if the provided file doesnt even exist, give up!
				if (!file.Exists) return false;

				//try binding normal rom extensions first
				if (!file.IsBound)
					file.BindSoleItemOf(romExtensions);

				//if we have an archive and need to bind something, then pop the dialog
				if (file.IsArchive && !file.IsBound)
				{
					var ac = new ArchiveChooser(file);
					if (ac.ShowDialog(this) == DialogResult.OK)
					{
						file.BindArchiveMember(ac.SelectedMemberIndex);
					}
					else return false;
				}

				IEmulator nextEmulator = null;
				RomGame rom = null;
				GameInfo game = null;

				try
				{
					//if (file.Extension.ToLower() == ".exe")
					//{
					//  PSX psx = new PSX();
					//  nextEmulator = psx;
					//  psx.LoadFile(file.CanonicalFullPath);
					//  game = new GameInfo();
					//  game.System = "PSX";
					//  game.Name = "xx";
					//  game.Hash = "xx";
					//}
					//else 
					if (file.Extension.ToLower() == ".iso")
					{
						//if (Global.PsxCoreLibrary.IsOpen)
						//{
						//  // sorry zero ;'( I leave de-RomGameifying this to you
						//  //PsxCore psx = new PsxCore(Global.PsxCoreLibrary);
						//  //nextEmulator = psx;
						//  //game = new RomGame();
						//  //var disc = Disc.FromIsoPath(path);
						//  //Global.DiscHopper.Clear();
						//  //Global.DiscHopper.Enqueue(disc);
						//  //Global.DiscHopper.Insert();
						//  //psx.SetDiscHopper(Global.DiscHopper);
						//}
					}
					else if (file.Extension.ToLower() == ".cue")
					{
						Disc disc = Disc.FromCuePath(path, new CueBinPrefs());
						var hash = disc.GetHash();
						game = Database.CheckDatabase(hash);
						if (game == null)
						{
							// Game was not found in DB. For now we're going to send it to the PCE-CD core.
							// In the future we need to do something smarter, possibly including simply asking the user
							// what system the game is for.

							game = new GameInfo();
							game.System = "PCECD";
							game.Name = Path.GetFileNameWithoutExtension(file.Name);
							game.Hash = hash;
						}

						switch (game.System)
						{
							case "PCE":
							case "PCECD":
								{
									string biosPath = PathManager.MakeAbsolutePath(Global.Config.PathPCEBios, "PCE");
									if (File.Exists(biosPath) == false)
									{
										MessageBox.Show("PCE-CD System Card not found. Please check the BIOS path in Config->Paths->PC Engine.");
										return false;
									}

									rom = new RomGame(new HawkFile(biosPath));

									if (rom.GameInfo.Status == RomStatus.BadDump)
										MessageBox.Show("The PCE-CD System Card you have selected is known to be a bad dump. This may cause problems playing PCE-CD games.\n\n" +
											"It is recommended that you find a good dump of the system card. Sorry to be the bearer of bad news!");

									else if (rom.GameInfo.NotInDatabase)
										MessageBox.Show("The PCE-CD System Card you have selected is not recognized in our database. That might mean it's a bad dump, or isn't the correct rom.");

									else if (rom.GameInfo["BIOS"] == false)
										MessageBox.Show("The PCE-CD System Card you have selected is not a BIOS image. You may have selected the wrong rom.");

									if (rom.GameInfo["SuperSysCard"])
										game.AddOption("SuperSysCard");
									if ((game["NeedSuperSysCard"]) && game["SuperSysCard"] == false)
										MessageBox.Show("This game requires a version 3.0 System card and won't run with the system card you've selected. Try selecting a 3.0 System Card in Config->Paths->PC Engine.");

									if (Global.Config.PceSpriteLimit) game.AddOption("ForceSpriteLimit");
									if (Global.Config.PceEqualizeVolume) game.AddOption("EqualizeVolumes");
									if (Global.Config.PceArcadeCardRewindHack) game.AddOption("ArcadeRewindHack");

									nextEmulator = new PCEngine(game, disc, rom.RomData);
									break;
								}
						}
					}
					else
					{
						rom = new RomGame(file);
						game = rom.GameInfo;

						switch (game.System)
						{
							case "SNES":
								nextEmulator = new LibsnesCore(rom.FileData);
								game.System = "SNES";
								break;
							case "SMS":
							case "SG":
								if (Global.Config.SmsEnableFM) game.AddOption("UseFM");
								if (Global.Config.SmsAllowOverlock) game.AddOption("AllowOverclock");
								if (Global.Config.SmsForceStereoSeparation) game.AddOption("ForceStereo");
								if (Global.Config.SmsSpriteLimit) game.AddOption("SpriteLimit");
								nextEmulator = new SMS(game, rom.RomData);
								break;
							case "GG":
								if (Global.Config.SmsAllowOverlock) game.AddOption("AllowOverclock");
								if (Global.Config.SmsSpriteLimit) game.AddOption("SpriteLimit");
								nextEmulator = new SMS(game, rom.RomData);
								break;
							case "A26":
								nextEmulator = new Atari2600(game, rom.FileData);
								((Atari2600)nextEmulator).SetBw(Global.Config.Atari2600_BW);
								((Atari2600)nextEmulator).SetP0Diff(Global.Config.Atari2600_LeftDifficulty);
								((Atari2600)nextEmulator).SetP1Diff(Global.Config.Atari2600_RightDifficulty);
								break;
							case "PCE":
							case "PCECD":
							case "SGX":
								if (Global.Config.PceSpriteLimit) game.AddOption("ForceSpriteLimit");
								nextEmulator = new PCEngine(game, rom.RomData);
								break;
							case "GEN":
								nextEmulator = new Genesis(game, rom.RomData);
								break;
							case "TI83":
								nextEmulator = new TI83(game, rom.RomData);
								if (Global.Config.TI83autoloadKeyPad)
									LoadTI83KeyPad();
								break;
							case "NES":
								{
									NES nes = new NES(game, rom.FileData);
									nes.SoundOn = Global.Config.SoundEnabled;
									nes.FirstDrawLine = Global.Config.NESTopLine;
									nes.LastDrawLine = Global.Config.NESBottomLine;
									nes.SetClipLeftAndRight(Global.Config.NESClipLeftAndRight);
									nextEmulator = nes;
									if (Global.Config.NESAutoLoadPalette && Global.Config.NESPaletteFile.Length > 0 &&
										HawkFile.ExistsAt(Global.Config.NESPaletteFile))
									{
										nes.SetPalette(
											NES.Palettes.Load_FCEUX_Palette(HawkFile.ReadAllBytes(Global.Config.NESPaletteFile)));
									}
								}
								break;
							case "GB":
								/*
								Gameboy gb = new Gameboy(game, rom.FileData, Global.Config.GameBoySkipBIOS);
								nextEmulator = gb;
								break;
								 */
								Emulation.Consoles.Gambatte.Gambatte gambatte = new Emulation.Consoles.Gambatte.Gambatte(rom.FileData);
								nextEmulator = gambatte;
								break;
							case "COLV":
								SMS c = new SMS(game, rom.RomData);//new ColecoVision(game, rom.FileData);
								nextEmulator = c;
								break;
							case "INTV":
								{
									Intellivision intv = new Intellivision(game, rom.RomData);
									string eromPath = PathManager.MakeAbsolutePath(Global.Config.PathINTVEROM, "INTV");
									if (!File.Exists(eromPath))
										throw new InvalidOperationException("Specified EROM path does not exist:\n\n" + eromPath);
									intv.LoadExecutiveRom(eromPath);
									string gromPath = PathManager.MakeAbsolutePath(Global.Config.PathINTVGROM, "INTV");
									if (!File.Exists(gromPath))
										throw new InvalidOperationException("Specified GROM path does not exist:\n\n" + gromPath);
									intv.LoadGraphicsRom(gromPath);
									nextEmulator = intv;
								}
								break;
						}
					}

					if (nextEmulator == null)
						throw new Exception();
					nextEmulator.CoreInputComm = Global.CoreInputComm;
				}
				catch (Exception ex)
				{
					MessageBox.Show("Exception during loadgame:\n\n" + ex.ToString());
					return false;
				}

				if (nextEmulator == null) throw new Exception();

				CloseGame();
				Global.Emulator.Dispose();
				Global.Emulator = nextEmulator;
				Global.Game = game;
				SyncControls();

				if (game.System == "NES")
				{
					NES nes = Global.Emulator as NES;
					Global.Game.Name = nes.GameName;
					Global.Game.Status = nes.RomStatus;
					SetNESSoundChannels();
				}

				Text = DisplayNameForSystem(game.System) + " - " + game.Name;
				ResetRewindBuffer();

				//restarts the lua console if a different rom is loaded.
				//im not really a fan of how this is done..
				if (Global.Config.RecentRoms.IsEmpty() || Global.Config.RecentRoms.GetRecentFileByPosition(0) != file.CanonicalFullPath)
				{
#if WINDOWS
					LuaConsole1.Restart();
#endif
				}

				Global.Config.RecentRoms.Add(file.CanonicalFullPath);
				if (File.Exists(PathManager.SaveRamPath(game)))
					LoadSaveRam();
				if (Global.Config.AutoSavestates)
					LoadState("Auto");

				////setup the throttle based on platform's specifications
				////(one day later for some systems we will need to modify it at runtime as the display mode changes)
				//{
				//    throttle.SetCoreFps(Global.Emulator.CoreOutputComm.VsyncRate);
				//    SyncThrottle();
				//}
				RamSearch1.Restart();
				RamWatch1.Restart();
				HexEditor1.Restart();
				NESPPU1.Restart();
				NESNameTableViewer1.Restart();
				NESDebug1.Restart();
				PCEBGViewer1.Restart();
				TI83KeyPad1.Restart();
				TAStudio1.Restart();
				Cheats1.Restart();
				ToolBox1.Restart();

				if (Global.Config.LoadCheatFileByGame)
				{
					if (Global.CheatList.AttemptLoadCheatFile())
					{
						Global.OSD.AddMessage("Cheats file loaded");
					}
				}
				Cheats1.UpdateValues();

				CurrentlyOpenRom = file.CanonicalFullPath;
				HandlePlatformMenus();
				StateSlots.Clear();
				UpdateStatusSlots();
				UpdateDumpIcon();

				CaptureRewindState();

				return true;
			}
		}

		private void UpdateDumpIcon()
		{
			DumpStatus.Image = BizHawk.MultiClient.Properties.Resources.Blank;
			DumpStatus.ToolTipText = "";

			if (Global.Emulator == null) return;
			if (Global.Game == null) return;

			var status = Global.Game.Status;
			string annotation = "";
			if (status == RomStatus.BadDump)
			{
				DumpStatus.Image = BizHawk.MultiClient.Properties.Resources.ExclamationRed;
				annotation = "Warning: Bad ROM Dump";
			}
			else if (status == RomStatus.Overdump)
			{
				DumpStatus.Image = BizHawk.MultiClient.Properties.Resources.ExclamationRed;
				annotation = "Warning: Overdump";
			}
			else if (status == RomStatus.NotInDatabase)
			{
				DumpStatus.Image = BizHawk.MultiClient.Properties.Resources.RetroQuestion;
				annotation = "Warning: Unknown ROM";
			}
			else if (status == RomStatus.TranslatedRom)
			{
				DumpStatus.Image = BizHawk.MultiClient.Properties.Resources.Translation;
				annotation = "Translated ROM";
			}
			else if (status == RomStatus.Homebrew)
			{
				DumpStatus.Image = BizHawk.MultiClient.Properties.Resources.HomeBrew;
				annotation = "Homebrew ROM";
			}
			else if (Global.Game.Status == RomStatus.Hack)
			{
				DumpStatus.Image = BizHawk.MultiClient.Properties.Resources.Hack;
				annotation = "Hacked ROM";
			}
			else if (Global.Game.Status == RomStatus.Unknown)
			{
				DumpStatus.Image = BizHawk.MultiClient.Properties.Resources.Hack;
				annotation = "Warning: ROM of Unknown Character";
			}
			else
			{
				DumpStatus.Image = BizHawk.MultiClient.Properties.Resources.GreenCheck;
				annotation = "Verified good dump";
			}
			if (!string.IsNullOrEmpty(Global.Emulator.CoreOutputComm.RomStatusAnnotation))
				annotation = Global.Emulator.CoreOutputComm.RomStatusAnnotation;

			DumpStatus.ToolTipText = annotation;
		}


		private void LoadSaveRam()
		{
			//zero says: this is sort of sketchy... but this is no time for rearchitecting
			try
			{
				var sram = new byte[Global.Emulator.ReadSaveRam.Length];
				using (var reader = new BinaryReader(new FileStream(PathManager.SaveRamPath(Global.Game), FileMode.Open, FileAccess.Read)))
					reader.Read(sram, 0, Global.Emulator.ReadSaveRam.Length);
				if (Global.Emulator is LibsnesCore)
					((LibsnesCore)Global.Emulator).StoreSaveRam(sram);
				else Array.Copy(sram, Global.Emulator.ReadSaveRam, Global.Emulator.ReadSaveRam.Length);
			}
			catch { }
		}

		private void CloseGame()
		{
			if (Global.Config.AutoSavestates && Global.Emulator is NullEmulator == false)
				SaveState("Auto");
			if (Global.Emulator.SaveRamModified)
				SaveRam();
			Global.Emulator.Dispose();
			Global.Emulator = new NullEmulator();
			Global.ActiveController = Global.NullControls;
			Global.AutoFireController = Global.AutofireNullControls;
			Global.MovieSession.Movie.Stop();
		}

		private static void SaveRam()
		{
			string path = PathManager.SaveRamPath(Global.Game);

			var f = new FileInfo(path);
			if (f.Directory.Exists == false)
				f.Directory.Create();

			var writer = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write));
			int len = Util.SaveRamBytesUsed(Global.Emulator.ReadSaveRam);
			writer.Write(Global.Emulator.ReadSaveRam, 0, len);
			writer.Close();
		}

		void OnSelectSlot(int num)
		{
			Global.Config.SaveSlot = num;
			SaveSlotSelectedMessage();
			UpdateStatusSlots();
		}

		/// <summary>
		/// Controls whether the app generates input events. should be turned off for most modal dialogs
		/// </summary>
		public bool AllowInput
		{
			get
			{
				//the main form gets input
				if (Form.ActiveForm == this) return true;

				//modals that need to capture input for binding purposes get input, of course
				if (Form.ActiveForm is InputConfig) return true;
				if (Form.ActiveForm is tools.HotkeyWindow) return true;

				//if no form is active on this process, then the background input setting applies
				if (Form.ActiveForm == null && Global.Config.AcceptBackgroundInput) return true;

				return false;
			}
		}

		public void ProcessInput()
		{
			for (; ; )
			{
				//loop through all available events
				var ie = Input.Instance.DequeueEvent();
				if (ie == null) { break; }

				//useful debugging:
				//Console.WriteLine(ie);

				//TODO - wonder what happens if we pop up something interactive as a response to one of these hotkeys? may need to purge further processing

				//look for client cntrol bindings for this key
				var triggers = Global.ClientControls.SearchBindings(ie.LogicalButton.ToString());
				if (triggers.Count == 0)
				{
					bool sys_hotkey = false;

					//maybe it is a system alt-key which hasnt been overridden
					if (ie.EventType == Input.InputEventType.Press)
					{
						if (ie.LogicalButton.Alt && ie.LogicalButton.Button.Length == 1)
						{
							char c = ie.LogicalButton.Button.ToLower()[0];
							if (c >= 'a' && c <= 'z' || c == ' ')
							{
								SendAltKeyChar(c);
								sys_hotkey = true;
							}
						}
						if (ie.LogicalButton.Alt && ie.LogicalButton.Button == "Space")
						{
							SendPlainAltKey(32);
							sys_hotkey = true;
						}
					}
					//ordinarily, an alt release with nothing else would move focus to the menubar. but that is sort of useless, and hard to implement exactly right.

					//no hotkeys or system keys bound this, so mutate it to an unmodified key and assign it for use as a game controller input
					//(we have a rule that says: modified events may be used for game controller inputs but not hotkeys)
					if (!sys_hotkey)
					{
						var mutated_ie = new Input.InputEvent();
						mutated_ie.EventType = ie.EventType;
						mutated_ie.LogicalButton = ie.LogicalButton;
						mutated_ie.LogicalButton.Modifiers = Input.ModifierKey.None;
						Global.ControllerInputCoalescer.Receive(mutated_ie);
					}
				}

				bool handled = false;
				if (ie.EventType == Input.InputEventType.Press)
				{
					foreach (var trigger in triggers)
					{
						handled |= CheckHotkey(trigger);
					}
				}

				//hotkeys which arent handled as actions get coalesced as pollable buttons
				if (!handled)
				{
					Global.HotkeyCoalescer.Receive(ie);
				}

			} //foreach event

		}

		bool CheckHotkey(string trigger)
		{
			//todo - could have these in a table somehow ?
			switch (trigger)
			{
				default:
					return false;
				case "IncreaseWindowSize":
					IncreaseWindowSize();
					break;
				case "DecreaseWindowSize":
					DecreaseWIndowSize();
					break;
				case "Record AVI":
					RecordAVI();
					break;
				case "Stop AVI":
					StopAVI();
					break;
				case "ToolBox":
					LoadToolBox();
					break;
				case "Increase Speed":
					IncreaseSpeed();
					break;
				case "Decrease Speed":
					DecreaseSpeed();
					break;
				case "Toggle Background Input":
					ToggleBackgroundInput();
					break;
				case "Quick Save State":
					if (!IsNullEmulator())
						SaveState("QuickSave" + Global.Config.SaveSlot.ToString());
					break;

				case "Quick Load State":
					if (!IsNullEmulator())
						LoadState("QuickSave" + Global.Config.SaveSlot.ToString());
					break;

				case "Unthrottle":
					unthrottled ^= true;
					Global.OSD.AddMessage("Unthrottled: " + unthrottled);
					break;

				case "Hard Reset":
					{
						bool autoSaveState = Global.Config.AutoSavestates;
						Global.Config.AutoSavestates = false;
						LoadRom(CurrentlyOpenRom);
						Global.Config.AutoSavestates = autoSaveState;
						break;
					}

				case "Screenshot":
					TakeScreenshot();
					break;

				case "SaveSlot0": if (!IsNullEmulator()) SaveState("QuickSave0"); break;
				case "SaveSlot1": if (!IsNullEmulator()) SaveState("QuickSave1"); break;
				case "SaveSlot2": if (!IsNullEmulator()) SaveState("QuickSave2"); break;
				case "SaveSlot3": if (!IsNullEmulator()) SaveState("QuickSave3"); break;
				case "SaveSlot4": if (!IsNullEmulator()) SaveState("QuickSave4"); break;
				case "SaveSlot5": if (!IsNullEmulator()) SaveState("QuickSave5"); break;
				case "SaveSlot6": if (!IsNullEmulator()) SaveState("QuickSave6"); break;
				case "SaveSlot7": if (!IsNullEmulator()) SaveState("QuickSave7"); break;
				case "SaveSlot8": if (!IsNullEmulator()) SaveState("QuickSave8"); break;
				case "SaveSlot9": if (!IsNullEmulator()) SaveState("QuickSave9"); break;
				case "LoadSlot0": if (!IsNullEmulator()) LoadState("QuickSave0"); break;
				case "LoadSlot1": if (!IsNullEmulator()) LoadState("QuickSave1"); break;
				case "LoadSlot2": if (!IsNullEmulator()) LoadState("QuickSave2"); break;
				case "LoadSlot3": if (!IsNullEmulator()) LoadState("QuickSave3"); break;
				case "LoadSlot4": if (!IsNullEmulator()) LoadState("QuickSave4"); break;
				case "LoadSlot5": if (!IsNullEmulator()) LoadState("QuickSave5"); break;
				case "LoadSlot6": if (!IsNullEmulator()) LoadState("QuickSave6"); break;
				case "LoadSlot7": if (!IsNullEmulator()) LoadState("QuickSave7"); break;
				case "LoadSlot8": if (!IsNullEmulator()) LoadState("QuickSave8"); break;
				case "LoadSlot9": if (!IsNullEmulator()) LoadState("QuickSave9"); break;
				case "SelectSlot0": OnSelectSlot(0); break;
				case "SelectSlot1": OnSelectSlot(1); break;
				case "SelectSlot2": OnSelectSlot(2); break;
				case "SelectSlot3": OnSelectSlot(3); break;
				case "SelectSlot4": OnSelectSlot(4); break;
				case "SelectSlot5": OnSelectSlot(5); break;
				case "SelectSlot6": OnSelectSlot(6); break;
				case "SelectSlot7": OnSelectSlot(7); break;
				case "SelectSlot8": OnSelectSlot(8); break;
				case "SelectSlot9": OnSelectSlot(9); break;

				case "Toggle Fullscreen": ToggleFullscreen(); break;
				case "Save Named State": SaveStateAs(); break;
				case "Load Named State": LoadStateAs(); break;
				case "Previous Slot": PreviousSlot(); break;
				case "Next Slot": NextSlot(); break;
				case "Ram Watch": LoadRamWatch(true); break;
				case "Ram Search": LoadRamSearch(); break;
				case "Ram Poke":
					{
						RamPoke r = new RamPoke();
						r.Show();
						break;
					}
				case "Hex Editor": LoadHexEditor(); break;
				case "Lua Console": OpenLuaConsole(); break;
				case "Cheats": LoadCheatsWindow(); break;
				case "TASTudio": LoadTAStudio(); break;
				case "Open ROM": OpenROM(); break;
				case "Close ROM": CloseROM(); break;
				case "Display FPS": ToggleFPS(); break;
				case "Display FrameCounter": ToggleFrameCounter(); break;
				case "Display LagCounter": ToggleLagCounter(); break;
				case "Display Input": ToggleInputDisplay(); break;
				case "Toggle Read Only": ToggleReadOnly(); break;
				case "Play Movie": PlayMovie(); break;
				case "Record Movie": RecordMovie(); break;
				case "Stop Movie": StopMovie(); break;
				case "Play Beginning": PlayMovieFromBeginning(); break;
				case "Volume Up": VolumeUp(); break;
				case "Volume Down": VolumeDown(); break;
				case "Soft Reset": SoftReset(); break;
				case "Toggle MultiTrack":
					{
						if (Global.MovieSession.Movie.IsActive)
						{
							Global.MovieSession.MultiTrack.IsActive = !Global.MovieSession.MultiTrack.IsActive;
							if (Global.MovieSession.MultiTrack.IsActive)
							{
								Global.OSD.AddMessage("MultiTrack Enabled");
								Global.OSD.MT = "Recording None";
							}
							else
								Global.OSD.AddMessage("MultiTrack Disabled");
							Global.MovieSession.MultiTrack.RecordAll = false;
							Global.MovieSession.MultiTrack.CurrentPlayer = 0;
						}
						else
						{
							Global.OSD.AddMessage("MultiTrack cannot be enabled while not recording.");
						}
						break;
					}
				case "Increment Player":
					{
						Global.MovieSession.MultiTrack.CurrentPlayer++;
						Global.MovieSession.MultiTrack.RecordAll = false;
						if (Global.MovieSession.MultiTrack.CurrentPlayer > 5) //TODO: Replace with console's maximum or current maximum players??!
						{
							Global.MovieSession.MultiTrack.CurrentPlayer = 1;
						}
						Global.OSD.MT = "Recording Player " + Global.MovieSession.MultiTrack.CurrentPlayer.ToString();
						break;
					}

				case "Decrement Player":
					{
						Global.MovieSession.MultiTrack.CurrentPlayer--;
						Global.MovieSession.MultiTrack.RecordAll = false;
						if (Global.MovieSession.MultiTrack.CurrentPlayer < 1)
						{
							Global.MovieSession.MultiTrack.CurrentPlayer = 5;//TODO: Replace with console's maximum or current maximum players??!
						}
						Global.OSD.MT = "Recording Player " + Global.MovieSession.MultiTrack.CurrentPlayer.ToString();
						break;
					}
				case "Record All":
					{
						Global.MovieSession.MultiTrack.CurrentPlayer = 0;
						Global.MovieSession.MultiTrack.RecordAll = true;
						Global.OSD.MT = "Recording All";
						break;
					}
				case "Record None":
					{
						Global.MovieSession.MultiTrack.CurrentPlayer = 0;
						Global.MovieSession.MultiTrack.RecordAll = false;
						Global.OSD.MT = "Recording None";
						break;
					}
				case "Emulator Pause":
					//used to be here: (the pause hotkey is ignored when we are frame advancing)
					TogglePause();
					break;
				case "Toggle Menu":
					ShowHideMenu();
					break;

			} //switch(trigger)

			return true;
		}

		void StepRunLoop_Throttle()
		{
			SyncThrottle();
			throttle.signal_frameAdvance = runloop_frameadvance;
			throttle.signal_continuousframeAdvancing = runloop_frameProgress;

			throttle.Step(true, -1);
		}

		void StepRunLoop_Core()
		{
			bool runFrame = false;
			runloop_frameadvance = false;
			DateTime now = DateTime.Now;
			bool suppressCaptureRewind = false;

			double frameAdvanceTimestampDelta = (now - FrameAdvanceTimestamp).TotalMilliseconds;
			bool frameProgressTimeElapsed = Global.Config.FrameProgressDelayMs < frameAdvanceTimestampDelta;

			if (Global.Config.SkipLagFrame && Global.Emulator.IsLagFrame && frameProgressTimeElapsed)
			{
				Global.Emulator.FrameAdvance(true);
			}

			if (Global.ClientControls["Frame Advance"] || PressFrameAdvance)
			{
				//handle the initial trigger of a frame advance
				if (FrameAdvanceTimestamp == DateTime.MinValue)
				{
					PauseEmulator();
					runFrame = true;
					runloop_frameadvance = true;
					FrameAdvanceTimestamp = now;
				}
				else
				{
					//handle the timed transition from countdown to FrameProgress
					if (frameProgressTimeElapsed)
					{
						runFrame = true;
						runloop_frameProgress = true;
						UnpauseEmulator();
					}
				}
			}
			else
			{
				//handle release of frame advance: do we need to deactivate FrameProgress?
				if (runloop_frameProgress)
				{
					runloop_frameProgress = false;
					PauseEmulator();
				}
				FrameAdvanceTimestamp = DateTime.MinValue;
			}

			if (!EmulatorPaused)
			{
				runFrame = true;
			}

			bool ReturnToRecording = Global.MovieSession.Movie.IsRecording;
			if (Global.Config.RewindEnabled && (Global.ClientControls["Rewind"] || PressRewind))
			{
				Rewind(1);
				suppressCaptureRewind = true;
				if (0 == RewindBuf.Count)
				{
					runFrame = false;
				}
				else
				{
					runFrame = true;
				}
				//we don't want to capture input when rewinding, even in record mode
				if (Global.MovieSession.Movie.IsRecording)
				{
					Global.MovieSession.Movie.SwitchToPlay();
				}
			}
			if (UpdateFrame == true)
			{
				runFrame = true;
				if (Global.MovieSession.Movie.IsRecording)
				{
					Global.MovieSession.Movie.SwitchToPlay();
				}
			}

			bool genSound = false;
			if (runFrame)
			{
				runloop_fps++;
				
				bool ff = Global.ClientControls["Fast Forward"] || Global.ClientControls["MaxTurbo"];
				bool fff = Global.ClientControls["MaxTurbo"];
				bool updateFpsString = (runloop_last_ff != ff);
				runloop_last_ff = ff;
				
				//client input-related duties
				Global.OSD.ClearGUIText();
				if (!fff)
				{
					UpdateToolsBefore();
				}

				if ((DateTime.Now - runloop_second).TotalSeconds > 1)
				{
					runloop_last_fps = runloop_fps;
					runloop_second = DateTime.Now;
					runloop_fps = 0;
					updateFpsString = true;
				}

				if (updateFpsString)
				{
					string fps_string = runloop_last_fps + " fps";
					if (fff)
					{
						fps_string += " >>>>";
					}
					else if (ff)
					{
						fps_string += " >>";
					}
					Global.OSD.FPS = fps_string;
				}

				if (!suppressCaptureRewind && Global.Config.RewindEnabled) CaptureRewindState();

				if (!runloop_frameadvance) genSound = true;
				else if (!Global.Config.MuteFrameAdvance)
					genSound = true;

				HandleMovieOnFrameLoop();

				//=======================================
				MemoryPulse.Pulse();
				Global.Emulator.FrameAdvance(!throttle.skipnextframe);
				MemoryPulse.Pulse();
				//=======================================
				if (CurrAviWriter != null)
				{
					long nsampnum = 44100 * (long)Global.Emulator.CoreOutputComm.VsyncDen + SoundRemainder;
					long nsamp = nsampnum / Global.Emulator.CoreOutputComm.VsyncNum;
					// exactly remember fractional parts of an audio sample
					SoundRemainder = nsampnum % Global.Emulator.CoreOutputComm.VsyncNum;

					short[] temp = new short[nsamp * 2];
					Global.Emulator.SoundProvider.GetSamples(temp);
					DumpProxy.buffer.enqueue_samples(temp, (int)nsamp);
					//DumpProxy.GetSamples(temp);
					//genSound = false;

					CurrAviWriter.AddFrame(Global.Emulator.VideoProvider);
					CurrAviWriter.AddSamples(temp);

					if (autoDumpLength > 0)
					{
						autoDumpLength--;
						if (autoDumpLength == 0) // finish
							StopAVI();
					}
				}

				if (Global.Emulator.IsLagFrame && Global.Config.AutofireLagFrames)
				{
					Global.AutoFireController.IncrementStarts();
				}

				PressFrameAdvance = false;
				if (!fff)
				{
					UpdateToolsAfter();
				}
			}

			if (Global.ClientControls["Rewind"] || PressRewind)
			{
				UpdateToolsAfter();
				if (ReturnToRecording)
				{
					Global.MovieSession.Movie.SwitchToRecord();
				}
				PressRewind = false;
			}
			if (true == UpdateFrame)
			{
				if (ReturnToRecording)
				{
					Global.MovieSession.Movie.SwitchToRecord();
				}
				UpdateFrame = false;
			}

			if (genSound)
			{
				// change audio path if dumping is occuring
				if (DumpProxy != null)
					Global.Sound.UpdateSound(DumpProxy);
				else
					Global.Sound.UpdateSound(Global.Emulator.SoundProvider);
			}
			else
				Global.Sound.UpdateSound(NullSound.SilenceProvider);
		}

		

		/// <summary>
		/// Update all tools that are frame dependent like Ram Search before processing
		/// </summary>
		public void UpdateToolsBefore()
		{
			LuaConsole1.LuaImp.FrameRegisterBefore();
#if WINDOWS
			LuaConsole1.ResumeScripts(true);
			Global.DisplayManager.PreFrameUpdateLuaSource();
#endif
			RamWatch1.UpdateValues();
			RamSearch1.UpdateValues();
			HexEditor1.UpdateValues();
			NESNameTableViewer1.UpdateValues();
			NESPPU1.UpdateValues();
			PCEBGViewer1.UpdateValues();
			GBDebugger.UpdateValues();
			SNESGraphicsDebugger1.UpdateValues();
		}

		/// <summary>
		/// Update all tools that are frame dependent like Ram Search after processing
		/// </summary>
		public void UpdateToolsAfter()
		{
			//The other tool updates are earlier, TAStudio needs to be later so it can display the latest
			//frame of execution in its list view.
			LuaConsole1.LuaImp.FrameRegisterAfter();
			TAStudio1.UpdateValues();
		}

		private unsafe Image MakeScreenshotImage()
		{
			var video = Global.Emulator.VideoProvider;
			var image = new Bitmap(video.BufferWidth, video.BufferHeight, PixelFormat.Format32bppArgb);

			//TODO - replace with BitmapBuffer
			var framebuf = video.GetVideoBuffer();
			var bmpdata = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int* ptr = (int*)bmpdata.Scan0.ToPointer();
			int stride = bmpdata.Stride / 4;
			for (int y = 0; y < video.BufferHeight; y++)
				for (int x = 0; x < video.BufferWidth; x++)
				{
					int col = framebuf[(y * video.BufferWidth) + x];

					if (Global.Emulator is TI83)
					{
						if (col == 0)
							col = Color.Black.ToArgb();
						else
							col = Color.White.ToArgb();
					}
					ptr[y * stride + x] = col;
				}
			image.UnlockBits(bmpdata);
			return image;
		}

		void TakeScreenshotToClipboard()
		{
			using (var img = MakeScreenshotImage())
			{
				System.Windows.Forms.Clipboard.SetImage(img);
			}
			Global.OSD.AddMessage("Screenshot saved to clipboard.");
		}

		private void TakeScreenshot()
		{
			string path = String.Format(PathManager.ScreenshotPrefix(Global.Game) + ".{0:yyyy-MM-dd HH.mm.ss}.png", DateTime.Now);
			TakeScreenshot(path);
			/*int frames = 120;
			int skip = 1;
			int speed = 1;
			bool reversable = true;
			string path = String.Format(PathManager.ScreenshotPrefix(Global.Game) + frames + "Frames-Skip=" + skip + "-Speed=" + speed + "-reversable=" + reversable + ".gif");
			makeAnimatedGif(frames, skip, speed, reversable, path);*/
			//Was using this code to test the animated gif functions
		}

		private void TakeScreenshot(string path)
		{
			var fi = new FileInfo(path);
			if (fi.Directory.Exists == false)
				fi.Directory.Create();
			using (var img = MakeScreenshotImage())
			{
				img.Save(fi.FullName, ImageFormat.Png);
			}
			Global.OSD.AddMessage(fi.Name + " saved.");
		}

		public void SaveState(string name)
		{
			string path = PathManager.SaveStatePrefix(Global.Game) + "." + name + ".State";

			var file = new FileInfo(path);
			if (file.Directory.Exists == false)
				file.Directory.Create();

			//Make backup first
			if (Global.Config.BackupSavestates && file.Exists == true)
			{
				string backup = path + ".bak";
				var backupFile = new FileInfo(backup);
				if (backupFile.Exists == true)
					backupFile.Delete();
				file.CopyTo(backup);
			}

			var writer = new StreamWriter(path);
			SaveStateFile(writer, name, false);
			LuaConsole1.LuaImp.SavestateRegisterSave(name);
		}

		public void SaveStateFile(StreamWriter writer, string name, bool fromLua)
		{
			Global.Emulator.SaveStateText(writer);
			HandleMovieSaveState(writer);
			if (Global.Config.SaveScreenshotWithStates)
			{
				writer.Write("Framebuffer ");
				Global.Emulator.VideoProvider.GetVideoBuffer().SaveAsHex(writer);
			}

			writer.Close();

			Global.OSD.AddMessage("Saved state: " + name);

			if (!fromLua)
			{

				UpdateStatusSlots();
			}
		}

		private void SaveStateAs()
		{
			if (IsNullEmulator()) return;
			var sfd = new SaveFileDialog();
			string path = PathManager.GetSaveStatePath(Global.Game);
			sfd.InitialDirectory = path;
			sfd.FileName = PathManager.SaveStatePrefix(Global.Game) + "." + "QuickSave0.State";
			var file = new FileInfo(path);
			if (file.Directory.Exists == false)
				file.Directory.Create();

			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();

			if (result != DialogResult.OK)
				return;

			var writer = new StreamWriter(sfd.FileName);
			SaveStateFile(writer, sfd.FileName, false);
		}

		public void LoadStateFile(string path, string name)
		{
			if (HandleMovieLoadState(path))
			{
				var reader = new StreamReader(path);
				Global.Emulator.LoadStateText(reader);

				while (true)
				{
					string str = reader.ReadLine();
					if (str == null) break;
					if (str.Trim() == "") continue;

					string[] args = str.Split(' ');
					if (args[0] == "Framebuffer")
					{
						Global.Emulator.VideoProvider.GetVideoBuffer().ReadFromHex(args[1]);
					}
				}

				reader.Close();
				Global.OSD.ClearGUIText();
				UpdateToolsBefore();
				UpdateToolsAfter();
				Global.OSD.AddMessage("Loaded state: " + name);
			}
			else
				Global.OSD.AddMessage("Loadstate error!");
		}

		public void LoadState(string name)
		{
			string path = PathManager.SaveStatePrefix(Global.Game) + "." + name + ".State";
			if (File.Exists(path) == false)
				return;

			LoadStateFile(path, name);
			LuaConsole1.LuaImp.SavestateRegisterLoad(name);
		}

		private void LoadStateAs()
		{
			if (IsNullEmulator()) return;
			var ofd = new OpenFileDialog();
			ofd.InitialDirectory = PathManager.GetSaveStatePath(Global.Game);
			ofd.Filter = "Save States (*.State)|*.State|All Files|*.*";
			ofd.RestoreDirectory = true;

			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();

			if (result != DialogResult.OK)
				return;

			if (File.Exists(ofd.FileName) == false)
				return;

			LoadStateFile(ofd.FileName, Path.GetFileName(ofd.FileName));
		}

		private void SaveSlotSelectedMessage()
		{
			Global.OSD.AddMessage("Slot " + Global.Config.SaveSlot + " selected.");
		}

		private void UpdateAutoLoadRecentRom()
		{
			if (Global.Config.AutoLoadMostRecentRom == true)
			{
				autoloadMostRecentToolStripMenuItem.Checked = false;
				Global.Config.AutoLoadMostRecentRom = false;
			}
			else
			{
				autoloadMostRecentToolStripMenuItem.Checked = true;
				Global.Config.AutoLoadMostRecentRom = true;
			}
		}

		private void UpdateAutoLoadRecentMovie()
		{
			if (Global.Config.AutoLoadMostRecentMovie == true)
			{
				autoloadMostRecentToolStripMenuItem1.Checked = false;
				Global.Config.AutoLoadMostRecentMovie = false;
			}
			else
			{
				autoloadMostRecentToolStripMenuItem1.Checked = true;
				Global.Config.AutoLoadMostRecentMovie = true;
			}
		}

		public void LoadRamSearch()
		{
			if (!RamSearch1.IsHandleCreated || RamSearch1.IsDisposed)
			{
				RamSearch1 = new RamSearch();
				RamSearch1.Show();
			}
			else
				RamSearch1.Focus();
		}

		public void LoadGameGenieEC()
		{
			NESGameGenie gg = new NESGameGenie();
			gg.Show();
		}

		public void LoadSNESGraphicsDebugger()
		{
			if (!SNESGraphicsDebugger1.IsHandleCreated || SNESGraphicsDebugger1.IsDisposed)
			{
				SNESGraphicsDebugger1 = new SNESGraphicsDebugger();
				SNESGraphicsDebugger1.Show();
			}
			else
				SNESGraphicsDebugger1.Focus();
		}

		public void LoadHexEditor()
		{
			if (!HexEditor1.IsHandleCreated || HexEditor1.IsDisposed)
			{
				HexEditor1 = new HexEditor();
				HexEditor1.Show();
			}
			else
				HexEditor1.Focus();
		}

		public void LoadToolBox()
		{
			if (!ToolBox1.IsHandleCreated || ToolBox1.IsDisposed)
			{
				ToolBox1 = new ToolBox();
				ToolBox1.Show();
			}
			else
				ToolBox1.Close();
		}

		public void LoadNESPPU()
		{
			if (!NESPPU1.IsHandleCreated || NESPPU1.IsDisposed)
			{
				NESPPU1 = new NESPPU();
				NESPPU1.Show();
			}
			else
				NESPPU1.Focus();
		}

		public void LoadNESNameTable()
		{
			if (!NESNameTableViewer1.IsHandleCreated || NESNameTableViewer1.IsDisposed)
			{
				NESNameTableViewer1 = new NESNameTableViewer();
				NESNameTableViewer1.Show();
			}
			else
				NESNameTableViewer1.Focus();
		}

		public void LoadNESDebugger()
		{
			if (!NESDebug1.IsHandleCreated || NESDebug1.IsDisposed)
			{
				NESDebug1 = new NESDebugger();
				NESDebug1.Show();
			}
			else
				NESDebug1.Focus();
		}

		public void LoadPCEBGViewer()
		{
			if (!PCEBGViewer1.IsHandleCreated || PCEBGViewer1.IsDisposed)
			{
				PCEBGViewer1 = new PCEBGViewer();
				PCEBGViewer1.Show();
			}
			else
				PCEBGViewer1.Focus();
		}

		public void LoadTI83KeyPad()
		{
			if (!TI83KeyPad1.IsHandleCreated || TI83KeyPad1.IsDisposed)
			{
				TI83KeyPad1 = new TI83KeyPad();
				TI83KeyPad1.Show();
			}
			else
				TI83KeyPad1.Focus();
		}

		public void LoadCheatsWindow()
		{
			if (!Cheats1.IsHandleCreated || Cheats1.IsDisposed)
			{
				Cheats1 = new Cheats();
				Cheats1.Show();
			}
			else
				Cheats1.Focus();
		}

		private int lastWidth = -1;
		private int lastHeight = -1;

		private void Render()
		{
			var video = Global.Emulator.VideoProvider;
			if (video.BufferHeight != lastHeight || video.BufferWidth != lastWidth)
			{
				lastWidth = video.BufferWidth;
				lastHeight = video.BufferHeight;
				FrameBufferResized();
			}

			Global.DisplayManager.UpdateSource(Global.Emulator.VideoProvider);
		}

		public void FrameBufferResized()
		{
			var video = Global.Emulator.VideoProvider;
			int zoom = Global.Config.TargetZoomFactor;
			var area = Screen.FromControl(this).WorkingArea;

			int borderWidth = Size.Width - renderTarget.Size.Width;
			int borderHeight = Size.Height - renderTarget.Size.Height;

			// start at target zoom and work way down until we find acceptable zoom
			for (; zoom >= 1; zoom--)
			{
				if ((((video.BufferWidth * zoom) + borderWidth) < area.Width) && (((video.BufferHeight * zoom) + borderHeight) < area.Height))
					break;
			}

			// Change size
			Size = new Size((video.BufferWidth * zoom) + borderWidth, (video.BufferHeight * zoom + borderHeight));
			PerformLayout();
			Global.RenderPanel.Resized = true;

			// Is window off the screen at this size?
			if (area.Contains(Bounds) == false)
			{
				if (Bounds.Right > area.Right) // Window is off the right edge
					Location = new Point(area.Right - Size.Width, Location.Y);

				if (Bounds.Bottom > area.Bottom) // Window is off the bottom edge
					Location = new Point(Location.X, area.Bottom - Size.Height);
			}
		}

		private bool InFullscreen = false;
		private Point WindowedLocation;

		public void ToggleFullscreen()
		{
			if (InFullscreen == false)
			{
				WindowedLocation = Location;
				FormBorderStyle = FormBorderStyle.None;
				WindowState = FormWindowState.Maximized;
				if (Global.Config.ShowMenuInFullscreen)
					MainMenuStrip.Visible = true;
				else
					MainMenuStrip.Visible = false;
				StatusSlot0.Visible = false;
				PerformLayout();
				Global.RenderPanel.Resized = true;
				InFullscreen = true;
			}
			else
			{
				FormBorderStyle = FormBorderStyle.FixedSingle;
				WindowState = FormWindowState.Normal;
				MainMenuStrip.Visible = true;
				StatusSlot0.Visible = Global.Config.DisplayStatusBar;
				Location = WindowedLocation;
				PerformLayout();
				FrameBufferResized();
				InFullscreen = false;
			}
		}

		//--alt key hacks

		protected override void WndProc(ref Message m)
		{
			//this is necessary to trap plain alt keypresses so that only our hotkey system gets them
			if (m.Msg == 0x0112) //WM_SYSCOMMAND
				if (m.WParam.ToInt32() == 0xF100) //SC_KEYMENU
					return;
			base.WndProc(ref m);
		}

		protected override bool ProcessDialogChar(char charCode)
		{
			//this is necessary to trap alt+char combinations so that only our hotkey system gets them
			if ((Control.ModifierKeys & Keys.Alt) != 0)
				return true;
			else return base.ProcessDialogChar(charCode);
		}

		//sends a simulation of a plain alt key keystroke
		void SendPlainAltKey(int lparam)
		{
			Message m = new Message();
			m.WParam = new IntPtr(0xF100); //SC_KEYMENU
			m.LParam = new IntPtr(lparam);
			m.Msg = 0x0112; //WM_SYSCOMMAND
			m.HWnd = Handle;
			base.WndProc(ref m);
		}

		//sends an alt+mnemonic combination
		void SendAltKeyChar(char c)
		{
			typeof(ToolStrip).InvokeMember("ProcessMnemonicInternal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Instance, null, menuStrip1, new object[] { c });
		}

		string FormatFilter(params string[] args)
		{
			var sb = new StringBuilder();
			if (args.Length % 2 != 0) throw new ArgumentException();
			int num = args.Length / 2;
			for (int i = 0; i < num; i++)
			{
				sb.AppendFormat("{0} ({1})|{1}", args[i * 2], args[i * 2 + 1]);
				if (i != num - 1) sb.Append('|');
			}
			string str = sb.ToString().Replace("%ARCH%", "*.zip;*.rar;*.7z");
			str = str.Replace(";", "; ");
			return str;
		}

		private void OpenROM()
		{
			var ofd = new OpenFileDialog();
			ofd.InitialDirectory = PathManager.GetRomsPath(Global.Emulator.SystemId);
			//"Rom Files|*.NES;*.SMS;*.GG;*.SG;*.PCE;*.SGX;*.GB;*.BIN;*.SMD;*.ROM;*.ZIP;*.7z|NES (*.NES)|*.NES|Master System|*.SMS;*.GG;*.SG;*.ZIP;*.7z|PC Engine|*.PCE;*.SGX;*.ZIP;*.7z|Gameboy|*.GB;*.ZIP;*.7z|TI-83|*.rom|Archive Files|*.zip;*.7z|Savestate|*.state|All Files|*.*";

			//adelikat: ugly design for this, I know
			if (INTERIM)
			{
				ofd.Filter = FormatFilter(
					"Rom Files", "*.nes;*.sms;*.gg;*.sg;*.pce;*.sgx;*.bin;*.smd;*.rom;*.a26;*.cue;*.exe;*.gb;*.gbc;*.gen;*.col;*.smc;*.sfc;%ARCH%",
					"Disc Images", "*.cue",
					"NES", "*.nes;%ARCH%",
					"Super NES", "*.smc;*.sfc;%ARCH%",
					"Master System", "*.sms;*.gg;*.sg;%ARCH%",
					"PC Engine", "*.pce;*.sgx;*.cue;%ARCH%",
					"TI-83", "*.rom;%ARCH%",
					"Archive Files", "%ARCH%",
					"Savestate", "*.state",
					"Atari 2600 (experimental)", "*.a26;*.bin;%ARCH%",
					"Genesis (experimental)", "*.gen;*.smd;*.bin;*.cue;%ARCH%",
					"Gameboy (experimental)", "*.gb;*.gbc;%ARCH%",
					"Colecovision (very experimental)", "*.col;%ARCH%",
                    "Intellivision (very experimental)", "*.int;*.bin;*.rom;%ARCH%",
					"PSX Executables (experimental)", "*.exe",
					"All Files", "*.*");
			}
			else
			{
				ofd.Filter = FormatFilter(
					"Rom Files", "*.nes;*.sms;*.gg;*.sg;*.pce;*.sgx;*.bin;*.smd;*.rom;*.cue;%ARCH%",
					"Disc Images", "*.cue",
					"NES", "*.nes;%ARCH%",
					"Super NES", "*.smc;*.sfc;%ARCH%",
					"Master System", "*.sms;*.gg;*.sg;%ARCH%",
					"PC Engine", "*.pce;*.sgx;*.cue;%ARCH%",
					"TI-83", "*.rom;%ARCH%",
					"Archive Files", "%ARCH%",
					"Savestate", "*.state",
					"Genesis (experimental)", "*.gen;*.smd;*.bin;*.cue;%ARCH%",
					"All Files", "*.*");
			}
			ofd.RestoreDirectory = false;

			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return;
			var file = new FileInfo(ofd.FileName);
			Global.Config.LastRomPath = file.DirectoryName;
			LoadRom(file.FullName);
		}

		public void CloseROM()
		{
			CloseGame();
			Global.Emulator = new NullEmulator();
			Global.Game = GameInfo.GetNullGame();
			MemoryPulse.Clear();
			RamSearch1.Restart();
			RamWatch1.Restart();
			HexEditor1.Restart();
			NESPPU1.Restart();
			NESNameTableViewer1.Restart();
			NESDebug1.Restart();
			PCEBGViewer1.Restart();
			TI83KeyPad1.Restart();
			Cheats1.Restart();
			GBDebugger.Restart();
			ToolBox1.Restart();
#if WINDOWS
			LuaConsole1.Restart();
#endif
			Text = "BizHawk" + (INTERIM ? " (interim) " : "");
			HandlePlatformMenus();
			StateSlots.Clear();
			UpdateDumpIcon();
		}

		private void SaveConfig()
		{
			if (Global.Config.SaveWindowPosition)
			{
				Global.Config.MainWndx = this.Location.X;
				Global.Config.MainWndy = this.Location.Y;
			}
			else
			{
				Global.Config.MainWndx = -1;
				Global.Config.MainWndy = -1;
			}

			if (Global.Config.ShowLogWindow) LogConsole.SaveConfigSettings();
			ConfigService.Save(PathManager.DefaultIniPath, Global.Config);
		}

		public void CloseTools()
		{
			CloseForm(RamWatch1);
			CloseForm(RamSearch1);
			CloseForm(HexEditor1);
			CloseForm(NESNameTableViewer1);
			CloseForm(NESPPU1);
			CloseForm(NESDebug1);
			CloseForm(PCEBGViewer1);
			CloseForm(Cheats1);
			CloseForm(TI83KeyPad1);
			CloseForm(TAStudio1);
#if WINDOWS
			CloseForm(LuaConsole1);
#endif
		}

		private void CloseForm(Form form)
		{
			if (form.IsHandleCreated) form.Close();
		}

		private void PreviousSlot()
		{
			if (Global.Config.SaveSlot == 0)
				Global.Config.SaveSlot = 9;		//Wrap to end of slot list
			else if (Global.Config.SaveSlot > 9)
				Global.Config.SaveSlot = 9;	//Meh, just in case
			else Global.Config.SaveSlot--;
			SaveSlotSelectedMessage();
			UpdateStatusSlots();
		}

		private void NextSlot()
		{
			if (Global.Config.SaveSlot >= 9)
				Global.Config.SaveSlot = 0;	//Wrap to beginning of slot list
			else if (Global.Config.SaveSlot < 0)
				Global.Config.SaveSlot = 0;	//Meh, just in case
			else Global.Config.SaveSlot++;
			SaveSlotSelectedMessage();
			UpdateStatusSlots();
		}

		private void ToggleFPS()
		{
			Global.Config.DisplayFPS ^= true;
		}

		private void ToggleFrameCounter()
		{
			Global.Config.DisplayFrameCounter ^= true;
		}

		private void ToggleLagCounter()
		{
			Global.Config.DisplayLagCounter ^= true;
		}

		private void ToggleInputDisplay()
		{
			Global.Config.DisplayInput ^= true;
		}

		public void ToggleReadOnly()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				ReadOnly ^= true;
				if (ReadOnly)
				{
					Global.OSD.AddMessage("Movie read-only mode");
				}
				else
				{
					Global.OSD.AddMessage("Movie read+write mode");
				}
			}
			else
			{
				Global.OSD.AddMessage("No movie active");
			}

		}

		public void SetReadOnly(bool read_only)
		{
			ReadOnly = read_only;
			if (ReadOnly)
				Global.OSD.AddMessage("Movie read-only mode");
			else
				Global.OSD.AddMessage("Movie read+write mode");
		}

		public void LoadRamWatch(bool load_dialog)
		{
			if (!RamWatch1.IsHandleCreated || RamWatch1.IsDisposed)
			{
				RamWatch1 = new RamWatch();
				if (Global.Config.AutoLoadRamWatch && Global.Config.RecentWatches.Length() > 0)
				{
					RamWatch1.LoadWatchFromRecent(Global.Config.RecentWatches.GetRecentFileByPosition(0));
				}
				if (load_dialog)
				{
					RamWatch1.Show();
				}
			}
			else
				RamWatch1.Focus();
		}

		public void LoadTAStudio()
		{
			if (!TAStudio1.IsHandleCreated || TAStudio1.IsDisposed)
			{
				TAStudio1 = new TAStudio();
				TAStudio1.Show();
			}
			else
				TAStudio1.Focus();
		}

		private void VolumeUp()
		{
			Global.Config.SoundVolume += 10;
			if (Global.Config.SoundVolume > 100)
				Global.Config.SoundVolume = 100;
			Global.Sound.ChangeVolume(Global.Config.SoundVolume);
			Global.OSD.AddMessage("Volume " + Global.Config.SoundVolume.ToString());
		}

		private void VolumeDown()
		{
			Global.Config.SoundVolume -= 10;
			if (Global.Config.SoundVolume < 0)
				Global.Config.SoundVolume = 0;
			Global.Sound.ChangeVolume(Global.Config.SoundVolume);
			Global.OSD.AddMessage("Volume " + Global.Config.SoundVolume.ToString());
		}

		private void SoftReset()
		{
			//is it enough to run this for one frame? maybe..
			if (Global.Emulator.ControllerDefinition.BoolButtons.Contains("Reset"))
			{
				Global.ClickyVirtualPadController.Click("Reset");
				if (!Global.MovieSession.Movie.IsActive && Global.Emulator is NES)
				{
					Global.Emulator.ResetFrameCounter();
				}
			}
		}

		public void UpdateStatusSlots()
		{
			StateSlots.Update();
			StatusSlot1.Enabled = StateSlots.HasSlot(1);
			StatusSlot2.Enabled = StateSlots.HasSlot(2);
			StatusSlot3.Enabled = StateSlots.HasSlot(3);
			StatusSlot4.Enabled = StateSlots.HasSlot(4);
			StatusSlot5.Enabled = StateSlots.HasSlot(5);
			StatusSlot6.Enabled = StateSlots.HasSlot(6);
			StatusSlot7.Enabled = StateSlots.HasSlot(7);
			StatusSlot8.Enabled = StateSlots.HasSlot(8);
			StatusSlot9.Enabled = StateSlots.HasSlot(9);
			StatusSlot10.Enabled = StateSlots.HasSlot(0);

			StatusSlot1.BackColor = SystemColors.Control;
			StatusSlot2.BackColor = SystemColors.Control;
			StatusSlot3.BackColor = SystemColors.Control;
			StatusSlot4.BackColor = SystemColors.Control;
			StatusSlot5.BackColor = SystemColors.Control;
			StatusSlot6.BackColor = SystemColors.Control;
			StatusSlot7.BackColor = SystemColors.Control;
			StatusSlot8.BackColor = SystemColors.Control;
			StatusSlot9.BackColor = SystemColors.Control;
			StatusSlot10.BackColor = SystemColors.Control;

			if (Global.Config.SaveSlot == 0) StatusSlot10.BackColor = SystemColors.ControlLightLight;
			if (Global.Config.SaveSlot == 1) StatusSlot1.BackColor = SystemColors.ControlLightLight;
			if (Global.Config.SaveSlot == 2) StatusSlot2.BackColor = SystemColors.ControlLightLight;
			if (Global.Config.SaveSlot == 3) StatusSlot3.BackColor = SystemColors.ControlLightLight;
			if (Global.Config.SaveSlot == 4) StatusSlot4.BackColor = SystemColors.ControlLightLight;
			if (Global.Config.SaveSlot == 5) StatusSlot5.BackColor = SystemColors.ControlLightLight;
			if (Global.Config.SaveSlot == 6) StatusSlot6.BackColor = SystemColors.ControlLightLight;
			if (Global.Config.SaveSlot == 7) StatusSlot7.BackColor = SystemColors.ControlLightLight;
			if (Global.Config.SaveSlot == 8) StatusSlot8.BackColor = SystemColors.ControlLightLight;
			if (Global.Config.SaveSlot == 9) StatusSlot9.BackColor = SystemColors.ControlLightLight;
		}

		/// <summary>
		/// start avi recording, unattended
		/// </summary>
		/// <param name="videowritername">match the short name of an ivideowriter</param>
		/// <param name="filename">filename to save to</param>
		public void RecordAVI(string videowritername, string filename)
		{
			_RecordAVI(videowritername, filename, true);
		}

		/// <summary>
		/// start avi recording, asking user for filename and options
		/// </summary>
		public void RecordAVI()
		{
			_RecordAVI(null, null, false);
		}

		/// <summary>
		/// start avi recording
		/// </summary>
		/// <param name="videowritername"></param>
		/// <param name="filename"></param>
		/// <param name="unattended"></param>
		private void _RecordAVI(string videowritername, string filename, bool unattended)
		{
			if (CurrAviWriter != null) return;

			// select IVideoWriter to use
			IVideoWriter aw = null;
			var writers = VideoWriterInventory.GetAllVideoWriters();

			if (unattended)
			{
				foreach (var w in writers)
				{
					if (w.ShortName() == videowritername)
					{
						aw = w;
						break;
					}
				}
			}
			else
			{
				aw = VideoWriterChooserForm.DoVideoWriterChoserDlg(writers, Global.MainForm);
			}

			foreach (var w in writers)
			{
				if (w != aw)
					w.Dispose();
			}

			if (aw == null)
			{
				if (unattended)
					Global.OSD.AddMessage(string.Format("Couldn't start video writer \"{0}\"", videowritername));
				else
					Global.OSD.AddMessage("A/V capture canceled.");
				return;
			}

			try
			{
				aw.SetMovieParameters(Global.Emulator.CoreOutputComm.VsyncNum, Global.Emulator.CoreOutputComm.VsyncDen);
				aw.SetVideoParameters(Global.Emulator.VideoProvider.BufferWidth, Global.Emulator.VideoProvider.BufferHeight);
				aw.SetAudioParameters(44100, 2, 16);

				// select codec token
				// do this before save dialog because ffmpeg won't know what extension it wants until it's been configured
				if (unattended)
				{
					aw.SetDefaultVideoCodecToken();
				}
				else
				{
					var token = aw.AcquireVideoCodecToken(Global.MainForm);
					if (token == null)
					{
						Global.OSD.AddMessage("A/V capture canceled.");
						aw.Dispose();
						return;
					}
					aw.SetVideoCodecToken(token);
				}

				// select file to save to
				if (unattended)
				{
					aw.OpenFile(filename);
				}
				else
				{
					var sfd = new SaveFileDialog();
					if (!(Global.Emulator is NullEmulator))
					{
						sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
						sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.AVIPath, "");
					}
					else
					{
						sfd.FileName = "NULL";
						sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.AVIPath, "");
					}
					sfd.Filter = String.Format("{0} (*.{0})|*.{0}|All Files|*.*", aw.DesiredExtension());

					Global.Sound.StopSound();
					var result = sfd.ShowDialog();
					Global.Sound.StartSound();

					if (result == DialogResult.Cancel)
					{
						aw.Dispose();
						return;
					}
					aw.OpenFile(sfd.FileName);
				}

				//commit the avi writing last, in case there were any errors earlier
				CurrAviWriter = aw;
				Global.OSD.AddMessage("A/V capture started");
				AVIStatusLabel.Image = BizHawk.MultiClient.Properties.Resources.AVI;
				AVIStatusLabel.ToolTipText = "A/V capture in progress";
			}
			catch
			{
				Global.OSD.AddMessage("A/V capture failed!");
				aw.Dispose();
				throw;
			}

			// buffersize here is entirely guess
			DumpProxy = new Emulation.Sound.MetaspuSoundProvider(Emulation.Sound.ESynchMethod.ESynchMethod_V);
			SoundRemainder = 0;
		}

		public void StopAVI()
		{
			if (CurrAviWriter == null)
			{
				DumpProxy = null;
				return;
			}
			CurrAviWriter.CloseFile();
			CurrAviWriter.Dispose();
			CurrAviWriter = null;
			Global.OSD.AddMessage("AVI capture stopped");
			AVIStatusLabel.Image = BizHawk.MultiClient.Properties.Resources.Blank;
			AVIStatusLabel.ToolTipText = "";
			DumpProxy = null; // return to normal sound output
			SoundRemainder = 0;
		}

		private void SwapBackupSavestate(string path)
		{
			//Takes the .state and .bak files and swaps them
			var state = new FileInfo(path);
			var backup = new FileInfo(path + ".bak");
			var temp = new FileInfo(path + ".bak.tmp");

			if (state.Exists == false) return;
			if (backup.Exists == false) return;
			if (temp.Exists == true) temp.Delete();

			backup.CopyTo(path + ".bak.tmp");
			backup.Delete();
			state.CopyTo(path + ".bak");
			state.Delete();
			temp.CopyTo(path);
			temp.Delete();

			StateSlots.ToggleRedo(Global.Config.SaveSlot);
		}

		private void ShowHideMenu()
		{
			MainMenuStrip.Visible ^= true;
		}

		public void OpenLuaConsole()
		{
#if WINDOWS
			if (!LuaConsole1.IsHandleCreated || LuaConsole1.IsDisposed)
			{
				LuaConsole1 = new LuaConsole();
				LuaConsole1.Show();
			}
			else
				LuaConsole1.Focus();
#else
			MessageBox.Show("Sorry, Lua is not supported on this platform.", "Lua not supported", MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
		}

		public void LoadGBDebugger()
		{
			if (Global.Emulator is Gameboy)
			{
				if (!GBDebugger.IsHandleCreated || GBDebugger.IsDisposed)
				{
					GBDebugger.LoadCore(Global.Emulator as Gameboy);
					GBDebugger.Show();
				}
				else
					GBDebugger.Focus();
			}
		}

		public void LoadRamPoke()
		{
			RamPoke r = new RamPoke();
			r.Show();
		}

		private void importMovieToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var ofd = new OpenFileDialog();
			ofd.InitialDirectory = PathManager.GetRomsPath(Global.Emulator.SystemId);
			ofd.Multiselect = true;
			ofd.Filter = FormatFilter(
				"Movie Files", "*.fm2;*.mc2;*.mcm;*.mmv;*.fcm;*.fmv;*.vmv;*.nmv;*.gmv;*.vbm;",
				"FCEUX", "*.fm2",
				"PCEjin/Mednafen", "*.mc2;*.mcm",
				"Dega", "*.mmv",
				"FCEU", "*.fcm",
				"Famtasia", "*.fmv",
				"VirtuaNES", "*.vmv",
				"Nintendulator", "*.nmv",
				"Gens", "*.gmv",
				"Visual Boy Advance", "*.vbm",
				"All Files", "*.*");

			ofd.RestoreDirectory = false;

			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return;

			foreach (string fn in ofd.FileNames)
			{
				var file = new FileInfo(fn);

				string errorMsg = "";
				string warningMsg = "";
				Movie m = MovieImport.ImportFile(fn, out errorMsg, out warningMsg);
				if (errorMsg.Length > 0)
					MessageBox.Show(errorMsg, "Conversion error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				if (warningMsg.Length > 0)
					Global.OSD.AddMessage(warningMsg);
				else
					Global.OSD.AddMessage(Path.GetFileName(fn) + " imported as ." + Global.Config.MovieExtension);
			}
		}

		#region Animaged Gifs
		/// <summary>
		/// Creates Animated Gifs
		/// </summary>
		/// <param name="num_images">Total number of frames in the gif</param>
		/// <param name="frameskip">How many frames to skip per screenshot in the image.  
		/// A value of 5 means that frame 1002 will be an image and 1007 will be an image in the gif 
		/// A value of 1 means that frame 1001 will be an image and 1002 will be an image in the gif</param>
		/// <param name="gifSpeed">How quickly the animated gif will run.  A value of 1 or -1 = normal emulator speed.
		/// A value of 2 will double the speed of the gif.
		/// Input a negative value to slow down the speed of the gif.
		/// A value of -2 will be half speed</param>
		/// <param name="reversable">Flag for making the gif loop back and forth</param>
		/// <param name="filename">location to save the file</param>
		/// <returns>false if the parameters are incorrect, true if it completes</returns>
		public bool AnimatedGif(int num_images, int frameskip, int gifSpeed, bool reversable, String filename)
		{
			if (num_images < 1 || frameskip < 1 || gifSpeed == 0) return false;//Exits if settings are bad
			#region declare/insantiate variables
			List<Image> images = new List<Image>(); //Variable for holding all images for the gif animation
			Image tempImage; //Holding the image in case it doesn't end up being added to the animation
			// Such a scenario could be a frameskip setting of 2 and a gifSpeed setting of 3
			// This would result in 1 of every 3 images being requested getting skipped.
			// My math might be wrong at this hour, but you get the point!
			int speedTracker = 0; // To keep track of when to add another image to the list
			bool status = PressFrameAdvance;
			PressFrameAdvance = true;
			#endregion

			#region Get the Images for the File
			int totalFrames = (gifSpeed > 0 ? num_images : (num_images * (gifSpeed * -1)));
			images.Add(MakeScreenshotImage());
			while (images.Count < totalFrames)
			{
				tempImage = MakeScreenshotImage();
				if (gifSpeed < 0)
					for (speedTracker = 0; speedTracker > gifSpeed; speedTracker--)
						images.Add(tempImage); //If the speed of the animation is to be slowed down, then add that many copies
				//of the image to the list

				for (int j = 0; j < frameskip; j++)
				{
					StepRunLoop_Core();
					Global.Emulator.FrameAdvance(true); //Frame advance
					//Global.RenderPanel.Render(Global.Emulator.VideoProvider);

					if (gifSpeed > 0)
					{
						speedTracker++;//Advance the frame counter for adding to the List of Images
						if (speedTracker == Math.Max(gifSpeed, frameskip))
						{
							images.Add(tempImage);
							speedTracker = 0;
						}
					}
				}
			}
			#endregion
			PressFrameAdvance = status;

			/*
			 * The following code was obtained from here:
			 * http://social.msdn.microsoft.com/Forums/en-US/csharpgeneral/thread/0c4252c8-8274-449c-ad9b-e4f07a8f8cdd/
			 * Modified to work with the BizHawk Project
			 */
			#region make gif file
			byte[] GifAnimation = { 33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0 };
			MemoryStream MS = new MemoryStream();
			BinaryReader BR = new BinaryReader(MS);
			var fi = new FileInfo(filename);
			if (fi.Directory.Exists == false)
				fi.Directory.Create();
			BinaryWriter BW = new BinaryWriter(new FileStream(filename, FileMode.Create));
			images[0].Save(MS, ImageFormat.Gif);
			byte[] B = MS.ToArray();
			B[10] = (byte)(B[10] & 0X78); //No global color table.
			BW.Write(B, 0, 13);
			BW.Write(GifAnimation);
			WriteGifImg(B, BW);
			for (int I = 1; I < images.Count; I++)
			{
				MS.SetLength(0);
				images[I].Save(MS, ImageFormat.Gif);
				B = MS.ToArray();
				WriteGifImg(B, BW);
			}
			if (reversable)
			{
				for (int I = images.Count - 2; I >= 0; I--)//Start at (count - 2) because last image is already in place
				{
					MS.SetLength(0);
					images[I].Save(MS, ImageFormat.Gif);
					B = MS.ToArray();
					WriteGifImg(B, BW);
				}
			}
			BW.Write(B[B.Length - 1]);
			BW.Close();
			MS.Dispose();
			#endregion

			return true;
		}

		public void WriteGifImg(byte[] B, BinaryWriter BW)
		{
			byte[] Delay = { 0, 0 };
			B[785] = Delay[0];
			B[786] = Delay[1];
			B[798] = (byte)(B[798] | 0X87);
			BW.Write(B, 781, 18);
			BW.Write(B, 13, 768);
			BW.Write(B, 799, B.Length - 800);
		}

		#endregion

		private void animatedGIFConfigToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GifAnimator g = new GifAnimator();
			g.Show();
		}

		private void makeAnimatedGIFToolStripMenuItem_Click(object sender, EventArgs e)
		{
			makeAnimatedGif();
		}

		private void makeAnimatedGif()
		{
			string path = String.Format(PathManager.ScreenshotPrefix(Global.Game) + "AGIF.{0:yyyy-MM-dd HH.mm.ss}.gif", DateTime.Now);
			AnimatedGif(Global.Config.GifAnimatorNumFrames, Global.Config.GifAnimatorFrameSkip, Global.Config.GifAnimatorSpeed, Global.Config.GifAnimatorReversable, path);
		}
		private void makeAnimatedGif(string path)
		{
			AnimatedGif(Global.Config.GifAnimatorNumFrames, Global.Config.GifAnimatorFrameSkip, Global.Config.GifAnimatorSpeed, Global.Config.GifAnimatorReversable, path);
		}

		private void makeAnimatedGifAsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			string path = String.Format(PathManager.ScreenshotPrefix(Global.Game) + "AGIF.{0:yyyy-MM-dd HH.mm.ss}.gif", DateTime.Now);

			SaveFileDialog sfd = new SaveFileDialog();
			sfd.InitialDirectory = Path.GetDirectoryName(path);
			sfd.FileName = Path.GetFileName(path);
			sfd.Filter = "GIF File (*.gif)|*.gif";

			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return;
			makeAnimatedGif(sfd.FileName);
		}

		private void ShowConsole()
		{
			LogConsole.ShowConsole();
			logWindowAsConsoleToolStripMenuItem.Enabled = false;
		}

		private void HideConsole()
		{
			LogConsole.HideConsole();
			logWindowAsConsoleToolStripMenuItem.Enabled = true;
		}

		public void notifyLogWindowClosing()
		{
			displayLogWindowToolStripMenuItem.Checked = false;
			logWindowAsConsoleToolStripMenuItem.Enabled = true;
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			Text = "BizHawk" + (INTERIM ? " (interim) " : "");
		}

		private void IncreaseWindowSize()
		{
			switch (Global.Config.TargetZoomFactor)
			{
				case 1:
					Global.Config.TargetZoomFactor = 2;
					break;
				case 2:
					Global.Config.TargetZoomFactor = 3;
					break;
				case 3:
					Global.Config.TargetZoomFactor = 4;
					break;
				case 4:
					Global.Config.TargetZoomFactor = 5;
					break;
				case 5:
					Global.Config.TargetZoomFactor = 10;
					break;
				case 10:
					return;
			}
			FrameBufferResized();
		}

		private void DecreaseWIndowSize()
		{
			switch (Global.Config.TargetZoomFactor)
			{
				case 1:
					return;
				case 2:
					Global.Config.TargetZoomFactor = 1;
					break;
				case 3:
					Global.Config.TargetZoomFactor = 2;
					break;
				case 4:
					Global.Config.TargetZoomFactor = 3;
					break;
				case 5:
					Global.Config.TargetZoomFactor = 4;
					break;
				case 10:
					Global.Config.TargetZoomFactor = 5;
					return;
			}
			FrameBufferResized();
		}

		private void neverBeAskedToSaveChangesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.SupressAskSave ^= true;
		}

		private void IncreaseSpeed()
		{
			int oldp = Global.Config.SpeedPercent;
			int newp = 0;
			if (oldp < 3) newp = 3;
			else if (oldp < 6) newp = 6;
			else if (oldp < 12) newp = 12;
			else if (oldp < 25) newp = 25;
			else if (oldp < 50) newp = 50;
			else if (oldp < 75) newp = 75;
			else if (oldp < 100) newp = 100;
			else if (oldp < 150) newp = 150;
			else if (oldp < 200) newp = 200;
			else if (oldp < 400) newp = 400;
			else if (oldp < 800) newp = 800;
			else newp = 1000;
			SetSpeedPercent(newp);
		}

		private void DecreaseSpeed()
		{
			int oldp = Global.Config.SpeedPercent;
			int newp = 0;
			if (oldp > 800) newp = 800;
			else if (oldp > 400) newp = 400;
			else if (oldp > 200) newp = 200;
			else if (oldp > 150) newp = 150;
			else if (oldp > 100) newp = 100;
			else if (oldp > 75) newp = 75;
			else if (oldp > 50) newp = 50;
			else if (oldp > 25) newp = 25;
			else if (oldp > 12) newp = 12;
			else if (oldp > 6) newp = 6;
			else if (oldp > 3) newp = 3;
			else newp = 1;
			SetSpeedPercent(newp);
		}

		public void SetNESSoundChannels()
		{
			NES nes = Global.Emulator as NES;
			nes.SetSquare1(Global.Config.NESEnableSquare1);
			nes.SetSquare2(Global.Config.NESEnableSquare2);
			nes.SetTriangle(Global.Config.NESEnableTriangle);
			nes.SetNoise(Global.Config.NESEnableNoise);
			nes.SetDMC(Global.Config.NESEnableDMC);
		}

		private void soundChannelsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.Emulator is NES)
			{
				Global.Sound.StopSound();
				NESSoundConfig config = new NESSoundConfig();
				config.ShowDialog();
				Global.Sound.StartSound();
			}
		}

		public void ClearSaveRAM()
		{
			//zero says: this is sort of sketchy... but this is no time for rearchitecting
			string saveRamPath = PathManager.SaveRamPath(Global.Game);
			var file = new FileInfo(saveRamPath);
			if (file.Exists) file.Delete();

			try
			{
				var sram = new byte[Global.Emulator.ReadSaveRam.Length];
				if (Global.Emulator is LibsnesCore)
					((LibsnesCore)Global.Emulator).StoreSaveRam(sram);
				else Array.Copy(sram, Global.Emulator.ReadSaveRam, Global.Emulator.ReadSaveRam.Length);
			}
			catch { }
		}

	}
}
