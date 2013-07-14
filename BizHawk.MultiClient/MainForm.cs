using System;
using System.Linq;
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
using BizHawk.Emulation.Consoles.Nintendo;
using BizHawk.Emulation.Consoles.Nintendo.SNES;
using BizHawk.Emulation.Consoles.Coleco;
using System.Collections.Generic;
using BizHawk.Emulation.Consoles.Intellivision;
using BizHawk.Emulation.Consoles.GB;
using BizHawk.Emulation.Consoles.Nintendo.GBA;
using BizHawk.Emulation.Computers.Commodore64;
using BizHawk.Emulation;
using BizHawk.Emulation.Consoles.Nintendo.N64;

namespace BizHawk.MultiClient
{

	public partial class MainForm : Form
	{
		public static bool INTERIM = true;
		public const string EMUVERSION = "Version " + VersionInfo.MAINVERSION;
		public const string RELEASEDATE = "March 23, 2013";
		public string CurrentlyOpenRom;
		public bool PauseAVI = false;
		public bool PressFrameAdvance = false;
		public bool PressRewind = false;
		public bool FastForward = false;
		public bool TurboFastForward = false;
		public bool RestoreReadWriteOnStop = false;
		public bool UpdateFrame = false;
		public bool NeedsReboot = false;

		private Control renderTarget;
		private RetainedViewportPanel retainedPanel;
		private readonly SavestateManager StateSlots = new SavestateManager();
		private readonly Dictionary<string, string> SNES_prepared = new Dictionary<string, string>();

		//avi/wav state
		IVideoWriter CurrAviWriter;
		ISoundProvider AviSoundInput;
		/// <summary>
		/// an audio proxy used for dumping
		/// </summary>
		Emulation.Sound.MetaspuSoundProvider DumpProxy;
		/// <summary>audio timekeeping for video dumping</summary>
		private long SoundRemainder;
		private int avwriter_resizew;
		private int avwriter_resizeh;

		//runloop control
		public bool EmulatorPaused { get; private set; }
		public EventWaitHandle MainWait;

		private bool exit;
		private bool runloop_frameProgress;
		private DateTime FrameAdvanceTimestamp = DateTime.MinValue;
		private int runloop_fps;
		private int runloop_last_fps;
		private bool runloop_frameadvance;
		private DateTime runloop_second;
		private bool runloop_last_ff;

		private readonly Throttle throttle;
		private bool unthrottled;

		//For handling automatic pausing when entering the menu
		private bool wasPaused;
		private bool didMenuPause;

		private bool InFullscreen;
		private Point _windowed_location;

		//tool dialogs
		public RamWatch RamWatch1 = new RamWatch();
		public RamSearch RamSearch1 = new RamSearch();
		public HexEditor HexEditor1 = new HexEditor();
		public TraceLogger TraceLogger1 = new TraceLogger();
		public SNESGraphicsDebugger SNESGraphicsDebugger1 = new SNESGraphicsDebugger();
		public NESNameTableViewer NESNameTableViewer1 = new NESNameTableViewer();
		public NESPPU NESPPU1 = new NESPPU();
		public NESDebugger NESDebug1 = new NESDebugger();
		public GBtools.GBGPUView GBGPUView1 = new GBtools.GBGPUView();
		public GBAtools.GBAGPUView GBAGPUView1 = new GBAtools.GBAGPUView();
		public PCEBGViewer PCEBGViewer1 = new PCEBGViewer();
		public Cheats Cheats1 = new Cheats();
		public ToolBox ToolBox1 = new ToolBox();
		public TI83KeyPad TI83KeyPad1 = new TI83KeyPad();
		public TAStudio TAStudio1 = new TAStudio();
		public VirtualPadForm VirtualPadForm1 = new VirtualPadForm();
		public NESGameGenie NESgg = new NESGameGenie();
		public SNESGameGenie SNESgg = new SNESGameGenie();
		public GBGameGenie GBgg = new GBGameGenie();
		public GenGameGenie Gengg = new GenGameGenie();
		public NESSoundConfig NesSound = new NESSoundConfig();
#if WINDOWS
		public LuaConsole LuaConsole1 = new LuaConsole();
#endif

		/// <summary>
		/// number of frames to autodump
		/// </summary>
		int autoDumpLength;

		static MainForm()
		{
			//if this isnt here, then our assemblyresolving hacks wont work due to the check for MainForm.INTERIM
			//its.. weird. dont ask.
		}

		public MainForm(string[] args)
		{
			Global.MovieSession = new MovieSession {Movie = new Movie()};
			MainWait = new AutoResetEvent(false);
			Icon = Properties.Resources.logo;
			InitializeComponent();
			Global.Game = GameInfo.GetNullGame();
			if (Global.Config.ShowLogWindow)
			{
				ShowConsole();
				//PsxApi.StdioFixes();
				displayLogWindowToolStripMenuItem.Checked = true;
			}

			throttle = new Throttle();

			FFMpeg.FFMpegPath = PathManager.MakeProgramRelativePath(Global.Config.FFMpegPath);

			Global.CheatList = new CheatList();
			UpdateStatusSlots();
			UpdateKeyPriorityIcon();

			//in order to allow late construction of this database, we hook up a delegate here to dearchive the data and provide it on demand
			//we could background thread this later instead if we wanted to be real clever
			NES.BootGodDB.GetDatabaseBytes = () =>
			{
				using (HawkFile NesCartFile = new HawkFile(Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "NesCarts.7z")).BindFirst())
					return Util.ReadAllBytes(NesCartFile.GetStream());
			};
			Global.MainForm = this;
			//Global.CoreComm = new CoreComm();
			//SyncCoreCommInputSignals();

			Database.LoadDatabase(Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "gamedb.txt"));

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
			Global.CoreComm = new CoreComm();
			SyncCoreCommInputSignals();
			Global.Emulator = new NullEmulator(Global.CoreComm);
			Global.ActiveController = Global.NullControls;
			Global.AutoFireController = Global.AutofireNullControls;
			Global.AutofireStickyXORAdapter.SetOnOffPatternFromConfig();
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
			else if (Global.Config.AutoLoadMostRecentRom && !Global.Config.RecentRoms.IsEmpty)
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
						autoDumpLength = m.RawFrames;
					}
					StartNewMovie(m, false);
					Global.Config.RecentMovies.Add(cmdMovie);
				}
			}
			else if (Global.Config.AutoLoadMostRecentMovie && !Global.Config.RecentMovies.IsEmpty)
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
			if (Global.Config.AutoLoadGBGPUView && Global.Emulator is Gameboy)
				LoadGBGPUView();

			if (Global.Config.AutoloadTAStudio)
			{
				LoadTAStudio();
			}

			if (Global.Config.AutoloadVirtualPad)
			{
				LoadVirtualPads();
			}

			if (Global.Config.AutoLoadLuaConsole)
				OpenLuaConsole();
			if (Global.Config.PCEBGViewerAutoload && Global.Emulator is PCEngine)
				LoadPCEBGViewer();
			if (Global.Config.AutoLoadSNESGraphicsDebugger && Global.Emulator is LibsnesCore)
				LoadSNESGraphicsDebugger();
			if (Global.Config.TraceLoggerAutoLoad)
			{
				if (Global.CoreComm.CpuTraceAvailable)
				{
					LoadTraceLogger();
				}
			}

			if (Global.Config.MainWndx >= 0 && Global.Config.MainWndy >= 0 && Global.Config.SaveWindowPosition)
				Location = new Point(Global.Config.MainWndx, Global.Config.MainWndy);

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

			UpdateStatusSlots();
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

		//contains a mapping: profilename->exepath ; or null if the exe wasnt available
		string SNES_Prepare(string profile)
		{
			SNES_Check(profile);
			if (SNES_prepared[profile] == null)
			{
				throw new InvalidOperationException("Couldn't locate the executable for SNES emulation for profile: " + profile + ". Please make sure you're using a fresh dearchive of a BizHawk distribution.");
			}
			return SNES_prepared[profile];
		}
		void SNES_Check(string profile)
		{
			if (SNES_prepared.ContainsKey(profile)) return;

			const string bits = "32";

			//disabled til it works
			//if (Win32.Is64BitOperatingSystem)
			//  bits = "64";

			string exename = "libsneshawk-" + bits + "-" + profile.ToLower() + ".exe";

			string thisDir = PathManager.GetExeDirectoryAbsolute();
			string exePath = Path.Combine(thisDir, exename);
			if (!File.Exists(exePath))
				exePath = Path.Combine(Path.Combine(thisDir, "dll"), exename);

			if (!File.Exists(exePath))
				exePath = null;

			SNES_prepared[profile] = exePath;
		}

		public void SyncCoreCommInputSignals(CoreComm target)
		{
			target.NES_BackdropColor = Global.Config.NESBackgroundColor;
			target.NES_UnlimitedSprites = Global.Config.NESAllowMoreThanEightSprites;
			target.NES_ShowBG = Global.Config.NESDispBackground;
			target.NES_ShowOBJ = Global.Config.NESDispSprites;
			target.PCE_ShowBG1 = Global.Config.PCEDispBG1;
			target.PCE_ShowOBJ1 = Global.Config.PCEDispOBJ1;
			target.PCE_ShowBG2 = Global.Config.PCEDispBG2;
			target.PCE_ShowOBJ2 = Global.Config.PCEDispOBJ2;
			target.SMS_ShowBG = Global.Config.SMSDispBG;
			target.SMS_ShowOBJ = Global.Config.SMSDispOBJ;

			target.PSX_FirmwaresPath = PathManager.MakeAbsolutePath(Global.Config.FirmwaresPath); // PathManager.MakeAbsolutePath(Global.Config.PathPSXFirmwares, "PSX");

			target.C64_FirmwaresPath = PathManager.MakeAbsolutePath(Global.Config.FirmwaresPath); // PathManager.MakeAbsolutePath(Global.Config.PathC64Firmwares, "C64");

			target.SNES_FirmwaresPath = PathManager.MakeAbsolutePath(Global.Config.FirmwaresPath); // PathManager.MakeAbsolutePath(Global.Config.PathSNESFirmwares, "SNES");
			target.SNES_ShowBG1_0 = Global.Config.SNES_ShowBG1_0;
			target.SNES_ShowBG1_1 = Global.Config.SNES_ShowBG1_1;
			target.SNES_ShowBG2_0 = Global.Config.SNES_ShowBG2_0;
			target.SNES_ShowBG2_1 = Global.Config.SNES_ShowBG2_1;
			target.SNES_ShowBG3_0 = Global.Config.SNES_ShowBG3_0;
			target.SNES_ShowBG3_1 = Global.Config.SNES_ShowBG3_1;
			target.SNES_ShowBG4_0 = Global.Config.SNES_ShowBG4_0;
			target.SNES_ShowBG4_1 = Global.Config.SNES_ShowBG4_1;
			target.SNES_ShowOBJ_0 = Global.Config.SNES_ShowOBJ1;
			target.SNES_ShowOBJ_1 = Global.Config.SNES_ShowOBJ2;
			target.SNES_ShowOBJ_2 = Global.Config.SNES_ShowOBJ3;
			target.SNES_ShowOBJ_3 = Global.Config.SNES_ShowOBJ4;

			target.SNES_Profile = Global.Config.SNESProfile;
			target.SNES_UseRingBuffer = Global.Config.SNESUseRingBuffer;
			target.SNES_AlwaysDoubleSize = Global.Config.SNESAlwaysDoubleSize;

			target.GG_HighlightActiveDisplayRegion = Global.Config.GGHighlightActiveDisplayRegion;
			target.GG_ShowClippedRegions = Global.Config.GGShowClippedRegions;

			target.Atari2600_ShowBG = Global.Config.Atari2600_ShowBG;
			target.Atari2600_ShowPlayer1 = Global.Config.Atari2600_ShowPlayer1;
			target.Atari2600_ShowPlayer2 = Global.Config.Atari2600_ShowPlayer2;
			target.Atari2600_ShowMissle1 = Global.Config.Atari2600_ShowMissle1;
			target.Atari2600_ShowMissle2 = Global.Config.Atari2600_ShowMissle2;
			target.Atari2600_ShowBall = Global.Config.Atari2600_ShowBall;
			target.Atari2600_ShowPF = Global.Config.Atari2600_ShowPlayfield;
		}

		public void SyncCoreCommInputSignals()
		{
			SyncCoreCommInputSignals(Global.CoreComm);
		}

		void SyncPresentationMode()
		{
			Global.DisplayManager.Suspend();

#if WINDOWS
			bool gdi = Global.Config.DisplayGDI || Global.Direct3D == null;
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
			Global.ForceNoThrottle = unthrottled || fastforward;

			// realtime throttle is never going to be so exact that using a double here is wrong
			throttle.SetCoreFps(Global.Emulator.CoreComm.VsyncRate);

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

				if (Global.ClientControls["Autohold"])
				{
					Global.StickyXORAdapter.MassToggleStickyState(Global.ActiveController.PressedButtons);
					Global.AutofireStickyXORAdapter.MassToggleStickyState(Global.AutoFireController.PressedButtons);
				}
				else if (Global.ClientControls["AutoholdAutofire"])
				{
					Global.AutofireStickyXORAdapter.MassToggleStickyState(Global.ActiveController.PressedButtons);
				}

				//if (!EmulatorPaused)
					//Global.ClickyVirtualPadController.FrameTick();

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
			SetPauseStatusbarIcon();
		}

		public void UnpauseEmulator()
		{
			EmulatorPaused = false;
			SetPauseStatusbarIcon();
		}

		private void SetPauseStatusbarIcon()
		{
			if (EmulatorPaused)
			{
				PauseStrip.Image = Properties.Resources.Pause;
				PauseStrip.Visible = true;
				PauseStrip.ToolTipText = "Emulator Paused";
			}
			else
			{
				PauseStrip.Image = Properties.Resources.Blank;
				PauseStrip.Visible = false;
				PauseStrip.ToolTipText = "";
			}
		}

		public void TogglePause()
		{
			EmulatorPaused ^= true;
			SetPauseStatusbarIcon();
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
				"Soft Reset", "Decrement Player", "Record AVI/WAV", "Stop AVI/WAV", "Toggle Menu", "Increase Speed", "Decrease Speed", "Toggle Background Input",
				"Autohold", "Clear Autohold", "SNES Toggle BG 1", "SNES Toggle BG 2", "SNES Toggle BG 3", "SNES Toggle BG 4", "SNES Toggle OBJ 1", "SNES Toggle OBJ 2", "SNES Toggle OBJ 3",
				"SNES Toggle OBJ 4", "Reboot Core", "Save Movie", "Virtual Pad", "AutoholdAutofire", "MoviePokeToggle", "ClearFrame" }
		};

		private void InitControls()
		{
			var controls = new Controller(ClientControlsDef);

			controls.BindMulti("SNES Toggle BG 1", Global.Config.ToggleSNESBG1Binding);
			controls.BindMulti("SNES Toggle BG 2", Global.Config.ToggleSNESBG2Binding);
			controls.BindMulti("SNES Toggle BG 3", Global.Config.ToggleSNESBG3Binding);
			controls.BindMulti("SNES Toggle BG 4", Global.Config.ToggleSNESBG4Binding);

			controls.BindMulti("SNES Toggle OBJ 1", Global.Config.ToggleSNESOBJ1Binding);
			controls.BindMulti("SNES Toggle OBJ 2", Global.Config.ToggleSNESOBJ2Binding);
			controls.BindMulti("SNES Toggle OBJ 3", Global.Config.ToggleSNESOBJ3Binding);
			controls.BindMulti("SNES Toggle OBJ 4", Global.Config.ToggleSNESOBJ4Binding);
			controls.BindMulti("Save Movie", Global.Config.SaveMovieBinding);
			controls.BindMulti("IncreaseWindowSize", Global.Config.IncreaseWindowSize);
			controls.BindMulti("DecreaseWindowSize", Global.Config.DecreaseWindowSize);
			controls.BindMulti("Fast Forward", Global.Config.FastForwardBinding);
			controls.BindMulti("Rewind", Global.Config.RewindBinding);
			controls.BindMulti("Hard Reset", Global.Config.HardResetBinding);
			controls.BindMulti("Reboot Core", Global.Config.RebootCoreResetBinding);
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
			controls.BindMulti("Virtual Pad", Global.Config.OpenVirtualPadBinding);
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
			controls.BindMulti("Record AVI/WAV", Global.Config.AVIRecordBinding);
			controls.BindMulti("Stop AVI/WAV", Global.Config.AVIStopBinding);
			controls.BindMulti("Toggle Menu", Global.Config.ToggleMenuBinding);
			controls.BindMulti("Autohold", Global.Config.AutoholdBinding);
			controls.BindMulti("AutoholdAutofire", Global.Config.AutoholdAutofireBinding);
			controls.BindMulti("Clear Autohold", Global.Config.AutoholdClear);
			controls.BindMulti("MoviePokeToggle", Global.Config.MoviePlaybackPokeModeBinding);
			controls.BindMulti("ClearFrame", Global.Config.ClearFrameBinding);

			Global.ClientControls = controls;

			Global.NullControls = new Controller(NullEmulator.NullController);
			Global.AutofireNullControls = new AutofireController(NullEmulator.NullController);

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
				case "GBC": str += "Game Boy Color"; break;
				case "A26": str += "Atari 2600"; break;
				case "A78": str += "Atari 7800"; break;
				case "C64": str += "Commodore 64"; break;
				case "Coleco": str += "ColecoVision"; break;
				case "GBA": str += "Game Boy Advance"; break;
				case "N64": str += "Nintendo 64"; break;
				case "SAT": str += "Saturn"; break;
				case "DGB": str += "Game Boy Link"; break;
			}

			if (INTERIM) str += " (interim)";
			return str;
		}

		private void HandlePlatformMenus()
		{
			string system = "";

			if (Global.Game != null)
			{
				system = Global.Game.System;
			}

			tI83ToolStripMenuItem.Visible = false;
			NESToolStripMenuItem.Visible = false;
			pCEToolStripMenuItem.Visible = false;
			sMSToolStripMenuItem.Visible = false;
			gBToolStripMenuItem.Visible = false;
			gBAToolStripMenuItem.Visible = false;
			atariToolStripMenuItem.Visible = false;
			sNESToolStripMenuItem.Visible = false;
			colecoToolStripMenuItem.Visible = false;
			n64ToolStripMenuItem.Visible = false;
			saturnToolStripMenuItem.Visible = false;

			switch (system)
			{
				case "TI83":
					tI83ToolStripMenuItem.Visible = true;
					break;
				case "NES":
					NESToolStripMenuItem.Visible = true;
					NESSpeicalMenuControls();
					break;
				case "PCE":
				case "PCECD":
				case "SGX":
					pCEToolStripMenuItem.Visible = true;
					break;
				case "SMS":
					sMSToolStripMenuItem.Text = "SMS";
					sMSToolStripMenuItem.Visible = true;
					break;
				case "SG":
					sMSToolStripMenuItem.Text = "SG";
					sMSToolStripMenuItem.Visible = true;
					break;
				case "GG":
					sMSToolStripMenuItem.Text = "GG";
					sMSToolStripMenuItem.Visible = true;
					break;
				case "GB":
				case "GBC":
					gBToolStripMenuItem.Visible = true;
					break;
				case "GBA":
					gBAToolStripMenuItem.Visible = true;
					break;
				case "A26":
					atariToolStripMenuItem.Visible = true;
					break;
				case "SNES":
				case "SGB":
					if ((Global.Emulator as LibsnesCore).IsSGB)
						sNESToolStripMenuItem.Text = "&SGB";
					else
						sNESToolStripMenuItem.Text = "&SNES";
					sNESToolStripMenuItem.Visible = true;
					break;
				case "Coleco":
					colecoToolStripMenuItem.Visible = true;
					break;
				case "N64":
					n64ToolStripMenuItem.Visible = true;
					break;
				case "SAT":
					saturnToolStripMenuItem.Visible = true;
					break;
			}
		}

		void NESSpeicalMenuAdd(string name, string button, string msg)
		{
			nESSpeicalToolStripMenuItem.Visible = true;
			nESSpeicalToolStripMenuItem.DropDownItems.Add(name, null, delegate
				{
				if (Global.Emulator.ControllerDefinition.BoolButtons.Contains(button))
				{
					if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
					{
						Global.ClickyVirtualPadController.Click(button);
						Global.OSD.AddMessage(msg);
					}
				}

			}
			);
		}

		void NESSpeicalMenuControls()
		{
			// ugly and hacky
			nESSpeicalToolStripMenuItem.Visible = false;
			nESSpeicalToolStripMenuItem.DropDownItems.Clear();
			var ss = Global.Emulator.ControllerDefinition.BoolButtons;
			if (ss.Contains("FDS Eject"))
				NESSpeicalMenuAdd("Eject Disk", "FDS Eject", "FDS Disk Ejected.");
			for (int i = 0; i < 16; i++)
			{
				string s = "FDS Insert " + i;
				if (ss.Contains(s))
					NESSpeicalMenuAdd("Insert Disk " + i, s, "FDS Disk " + i + " inserted.");
			}
			if (ss.Contains("VS Coin 1"))
				NESSpeicalMenuAdd("Insert Coin 1", "VS Coin 1", "Coin 1 inserted.");
			if (ss.Contains("VS Coin 2"))
				NESSpeicalMenuAdd("Insert Coin 2", "VS Coin 2", "Coin 2 inserted.");
		}

		void SaturnSetPrefs(Emulation.Consoles.Sega.Saturn.Yabause e = null)
		{
			if (e == null)
				e = Global.Emulator as Emulation.Consoles.Sega.Saturn.Yabause;

			if (Global.Config.SaturnUseGL != e.GLMode)
			{
				// theoretically possible; not coded. meh.
				FlagNeedsReboot();
				return;
			}
			if (e.GLMode && Global.Config.SaturnUseGL)
			{
				if (Global.Config.SaturnDispFree)
					e.SetGLRes(0, Global.Config.SaturnGLW, Global.Config.SaturnGLH);
				else
					e.SetGLRes(Global.Config.SaturnDispFactor, 0, 0);
			}
		}

		static Controller BindToDefinition(ControllerDefinition def, Dictionary<string, Dictionary<string, string>> allbinds)
		{
			var ret = new Controller(def);
			Dictionary<string, string> binds;
			if (allbinds.TryGetValue(def.Name, out binds))
			{
				foreach (string cbutton in def.BoolButtons)
				{
					string bind;
					if (binds.TryGetValue(cbutton, out bind))
						ret.BindMulti(cbutton, bind);
				}
			}
			return ret;
		}
		// could merge these two methods...
		static AutofireController BindToDefinitionAF(ControllerDefinition def, Dictionary<string, Dictionary<string, string>> allbinds)
		{
			var ret = new AutofireController(def);
			Dictionary<string, string> binds;
			if (allbinds.TryGetValue(def.Name, out binds))
			{
				foreach (string cbutton in def.BoolButtons)
				{
					string bind;
					if (binds.TryGetValue(cbutton, out bind))
						ret.BindMulti(cbutton, bind);
				}
			}
			return ret;
		}


		void SyncControls()
		{
			var def = Global.Emulator.ControllerDefinition;

			Global.ActiveController = BindToDefinition(def, Global.Config.AllTrollers);
			Global.AutoFireController = BindToDefinitionAF(def, Global.Config.AllTrollersAutoFire);

			// allow propogating controls that are in the current controller definition but not in the prebaked one
			// these two lines shouldn't be required anymore under the new system?
			Global.ActiveController.ForceType(new ControllerDefinition(Global.Emulator.ControllerDefinition));
			Global.ClickyVirtualPadController.Type = new ControllerDefinition(Global.Emulator.ControllerDefinition);			
			RewireInputChain();
		}

		void RewireInputChain()
		{
			Global.ControllerInputCoalescer = new ControllerInputCoalescer {Type = Global.ActiveController.Type};

			Global.OrControllerAdapter.Source = Global.ActiveController;
			Global.OrControllerAdapter.SourceOr = Global.AutoFireController;
			Global.UD_LR_ControllerAdapter.Source = Global.OrControllerAdapter;

			Global.StickyXORAdapter.Source = Global.UD_LR_ControllerAdapter;
			Global.AutofireStickyXORAdapter.Source = Global.StickyXORAdapter;

			Global.MultitrackRewiringControllerAdapter.Source = Global.AutofireStickyXORAdapter;
			Global.ForceOffAdaptor.Source = Global.MultitrackRewiringControllerAdapter;

			Global.MovieInputSourceAdapter.Source = Global.ForceOffAdaptor;
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

		public bool LoadRom(string path, bool deterministicemulation = false, bool hasmovie = false)
		{
			if (path == null) return false;
			using (var file = new HawkFile())
			{
				string[] romExtensions = new[] { "SMS", "SMC", "SFC", "PCE", "SGX", "GG", "SG", "BIN", "GEN", "MD", "SMD", "GB", "NES", "FDS", "ROM", "INT", "GBC", "UNF", "A78", "CRT", "COL", "XML", "Z64", "V64", "N64" };

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
				CoreComm nextComm = new CoreComm();
				SyncCoreCommInputSignals(nextComm);

				try
				{
					string ext = file.Extension.ToLower();
					if (ext == ".iso" || ext == ".cue")
					{
						Disc disc;
						if (ext == ".iso")
							disc = Disc.FromIsoPath(path);
						else
							disc = Disc.FromCuePath(path, new CueBinPrefs());
						var hash = disc.GetHash();
						game = Database.CheckDatabase(hash);
						if (game == null)
						{
							// try to use our wizard methods
							game = new GameInfo { Name = Path.GetFileNameWithoutExtension(file.Name), Hash = hash };

							switch (disc.DetectDiscType())
							{
								case DiscType.SegaSaturn:
									game.System = "SAT";
									break;
								case DiscType.SonyPSP:
									game.System = "PSP";
									break;
								case DiscType.SonyPSX:
									game.System = "PSX";
									break;
								case DiscType.TurboCD:
								case DiscType.UnknownCDFS:
								case DiscType.UnknownFormat:
								default: // PCECD was bizhawk's first CD core, so this prevents regressions
									game.System = "PCECD";
									break;
							}
							
							/* probably dead code here
							if (Emulation.Consoles.PSX.Octoshock.CheckIsPSX(disc))
							{
								game = new GameInfo { System = "PSX", Name = Path.GetFileNameWithoutExtension(file.Name), Hash = hash };
								disc.Dispose();
							}
							*/
                            //else if (disc.DetectSegaSaturn())  // DetectSegaSaturn does not exist
                            //{
                            //    Console.WriteLine("Sega Saturn disc detected!");
                            //    game = new GameInfo { System = "SAT", Name = Path.GetFileNameWithoutExtension(file.Name), Hash = hash };
                            //}
						}

						switch (game.System)
						{
							case "SAT":
								{
									string biosPath = PathManager.StandardFirmwareName(Global.Config.FilenameSaturnBios);
									if (!File.Exists(biosPath))
									{
										MessageBox.Show("Saturn BIOS not found.  Please check firmware configurations.");
										return false;
									}
									var saturn = new Emulation.Consoles.Sega.Saturn.Yabause(nextComm, disc, File.ReadAllBytes(biosPath), Global.Config.SaturnUseGL);
									nextEmulator = saturn;
									SaturnSetPrefs(saturn);
								}
								break;
							case "PSP":
								{
									var psp = new Emulation.Consoles.Sony.PSP.PSP(nextComm, file.Name);
									nextEmulator = psp;
								}
								break;
							case "PSX":
								{
									var psx = new Emulation.Consoles.PSX.Octoshock(nextComm);
									nextEmulator = psx;
									psx.LoadCuePath(file.CanonicalFullPath);
									nextEmulator.CoreComm.RomStatusDetails = "PSX etc.";
								}
								break;
							case "PCE":
							case "PCECD":
								{
									string biosPath = PathManager.StandardFirmwareName(Global.Config.FilenamePCEBios); //PathManager.MakeAbsolutePath(Global.Config.PathPCEBios, "PCE");
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

									game.FirmwareHash = Util.BytesToHexString(System.Security.Cryptography.SHA1.Create().ComputeHash(rom.RomData));

									nextEmulator = new PCEngine(nextComm, game, disc, rom.RomData);
									break;
								}
						}
					}
					else if (file.Extension.ToLower() == ".xml")
					{
						var XMLG = XmlGame.Create(file);

						if (XMLG != null)
						{
							game = XMLG.GI;

							switch (game.System)
							{
								case "DGB":

									var L = Database.GetGameInfo(XMLG.Assets["LeftRom"], "left.gb");
									var R = Database.GetGameInfo(XMLG.Assets["RightRom"], "right.gb");

									if (Global.Config.GB_ForceDMG) L.AddOption("ForceDMG");
								    if (Global.Config.GB_GBACGB) L.AddOption("GBACGB");
								    if (Global.Config.GB_MulticartCompat) L.AddOption("MulitcartCompat");
									if (Global.Config.GB_ForceDMG) R.AddOption("ForceDMG");
								    if (Global.Config.GB_GBACGB) R.AddOption("GBACGB");
								    if (Global.Config.GB_MulticartCompat) R.AddOption("MulitcartCompat");

									GambatteLink gbl = new GambatteLink(nextComm, L, XMLG.Assets["LeftRom"], R, XMLG.Assets["RightRom"]);
								    nextEmulator = gbl;
								    // other stuff todo
									break;

								default:
									return false;
							}

						}
						// if load fails, are we supposed to retry as a bsnes XML????????
					}
					else // most extensions
					{
						rom = new RomGame(file);
						game = rom.GameInfo;

						bool isXml = false;

						// other xml has already been handled
						if (file.Extension.ToLower() == ".xml")
						{
							game.System = "SNES";
							isXml = true;
						}


					RETRY:
						switch (game.System)
						{
							case "SNES":
								{
									game.System = "SNES";
									nextComm.SNES_ExePath = SNES_Prepare(Global.Config.SNESProfile);

									//this isnt completely correct. might need to deal with the archive somehow.
									//once done, code should be factored out to be useful in other platforms as well
									//BUT!!! right now bsnes needs to open the file itself. lame.
									//nextComm.AcquireSubfile = (subpath) =>
									//  File.OpenRead(Path.Combine(Path.GetDirectoryName(path),subpath));
									nextComm.AcquireSubfilePath = (subpath) =>
									  Path.Combine(Path.GetDirectoryName(path),subpath);

									var snes = new LibsnesCore(nextComm);
									nextEmulator = snes;
									byte[] romData = isXml?null:rom.FileData;
									byte[] xmlData = isXml?rom.FileData:null;
									snes.Load(game, romData, null, deterministicemulation, xmlData);
								}
								break;
							case "SMS":
							case "SG":
								if (Global.Config.SmsEnableFM) game.AddOption("UseFM");
								if (Global.Config.SmsAllowOverlock) game.AddOption("AllowOverclock");
								if (Global.Config.SmsForceStereoSeparation) game.AddOption("ForceStereo");
								if (Global.Config.SmsSpriteLimit) game.AddOption("SpriteLimit");
								nextEmulator = new SMS(nextComm, game, rom.RomData);
								break;
							case "GG":
								if (Global.Config.SmsAllowOverlock) game.AddOption("AllowOverclock");
								if (Global.Config.SmsSpriteLimit) game.AddOption("SpriteLimit");
								nextEmulator = new SMS(nextComm, game, rom.RomData);
								break;
							case "A26":
								nextEmulator = new Atari2600(nextComm, game, rom.FileData);
								((Atari2600)nextEmulator).SetBw(Global.Config.Atari2600_BW);
								((Atari2600)nextEmulator).SetP0Diff(Global.Config.Atari2600_LeftDifficulty);
								((Atari2600)nextEmulator).SetP1Diff(Global.Config.Atari2600_RightDifficulty);
								break;
							case "PCE":
							case "PCECD":
							case "SGX":
								if (Global.Config.PceSpriteLimit) game.AddOption("ForceSpriteLimit");
								nextEmulator = new PCEngine(nextComm, game, rom.RomData);
								break;
							case "GEN":
								nextEmulator = new Genesis(nextComm, game, rom.RomData);
								break;
							case "TI83":
								nextEmulator = new TI83(nextComm, game, rom.RomData);
								if (Global.Config.TI83autoloadKeyPad)
									LoadTI83KeyPad();
								break;
							case "NES":
								{
									string biosPath = PathManager.StandardFirmwareName(Global.Config.FilenameFDSBios);
									byte[] bios = null;
									if (File.Exists(biosPath))
									{
										bios = File.ReadAllBytes(biosPath);
										// ines header + 24KB of garbage + actual bios + 8KB of garbage
										if (bios.Length == 40976)
										{
											MessageBox.Show(this, "Your FDS BIOS is a bad dump.  BizHawk will attempt to use it, but no guarantees!  You should find a new one.");
											byte[] tmp = new byte[8192];
											Buffer.BlockCopy(bios, 16 + 8192 * 3, tmp, 0, 8192);
											bios = tmp;
										}
									}

									NES nes = new NES(nextComm, game, rom.FileData, bios)
										{
											SoundOn = Global.Config.SoundEnabled,
											NTSC_FirstDrawLine = Global.Config.NTSC_NESTopLine,
											NTSC_LastDrawLine = Global.Config.NTSC_NESBottomLine,
											PAL_FirstDrawLine = Global.Config.PAL_NESTopLine
										};
									nes.NTSC_LastDrawLine = Global.Config.PAL_NESBottomLine;
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
							case "GBC":
									if (!Global.Config.GB_AsSGB)
									{
										if (Global.Config.GB_ForceDMG) game.AddOption("ForceDMG");
										if (Global.Config.GB_GBACGB) game.AddOption("GBACGB");
										if (Global.Config.GB_MulticartCompat) game.AddOption("MulitcartCompat");
										Gameboy gb = new Gameboy(nextComm, game, rom.FileData);
										nextEmulator = gb;
										if (gb.IsCGBMode())
										{
											gb.SetCGBColors(Global.Config.CGBColors);
										}
										else
										{
											try
											{
												using (StreamReader f = new StreamReader(Global.Config.GB_PaletteFile))
												{
													int[] colors = GBtools.ColorChooserForm.LoadPalFile(f);
													if (colors != null)
														gb.ChangeDMGColors(colors);
												}
											}
											catch { }
										}
									}
									else
									{
										// todo: get these bioses into a gamedb?? then we could demand different filenames for different regions?
										string sgbromPath = PathManager.StandardFirmwareName("sgb.sfc"); //Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathSNESFirmwares, "SNES"), "sgb.sfc");
										byte[] sgbrom = null;
										try
										{
											if (File.Exists(sgbromPath))
											{
												sgbrom = File.ReadAllBytes(sgbromPath);
											}
											else
											{
												MessageBox.Show("Couldn't open sgb.sfc from the configured SNES firmwares path, which is:\n\n" + sgbromPath + "\n\nPlease make sure it is available and try again.\n\nWe're going to disable SGB for now; please re-enable it when you've set up the file.");
												Global.Config.GB_AsSGB = false;
												game.System = "GB";
												goto RETRY;
											}
										}
										catch (Exception)
										{
											// failed to load SGB bios.  to avoid catch-22, disable SGB mode
											Global.Config.GB_AsSGB = false;
											throw;
										}
										if (sgbrom != null)
										{
											game.System = "SNES";
											game.AddOption("SGB");
											nextComm.SNES_ExePath = SNES_Prepare(Global.Config.SNESProfile);
											var snes = new LibsnesCore(nextComm);
											nextEmulator = snes;
											game.FirmwareHash = Util.BytesToHexString(System.Security.Cryptography.SHA1.Create().ComputeHash(sgbrom));
											snes.Load(game, rom.FileData, sgbrom, deterministicemulation, null);
										}
									}
								//}
								break;
							case "Coleco":
								string colbiosPath = PathManager.StandardFirmwareName(Global.Config.FilenameCOLBios);
								FileInfo colfile = new FileInfo(colbiosPath);
								if (!colfile.Exists)
								{
									MessageBox.Show("Unable to find the required ColecoVision BIOS file - \n" + colbiosPath, "Unable to load BIOS", MessageBoxButtons.OK, MessageBoxIcon.Error);
									throw new Exception();
								}
								else
								{
									ColecoVision c = new ColecoVision(nextComm, game, rom.RomData, colbiosPath, Global.Config.ColecoSkipBiosIntro);
									nextEmulator = c;
								}
								break;
							case "INTV":
								{
									Intellivision intv = new Intellivision(nextComm, game, rom.RomData);
									string eromPath = PathManager.StandardFirmwareName(Global.Config.FilenameINTVEROM);
									if (!File.Exists(eromPath))
										throw new InvalidOperationException("Specified EROM path does not exist:\n\n" + eromPath);
									intv.LoadExecutiveRom(eromPath);
									string gromPath = PathManager.StandardFirmwareName(Global.Config.FilenameINTVGROM);
									if (!File.Exists(gromPath))
										throw new InvalidOperationException("Specified GROM path does not exist:\n\n" + gromPath);
									intv.LoadGraphicsRom(gromPath);
									nextEmulator = intv;
								}
								break;
							case "A78":
								string ntsc_biospath = PathManager.StandardFirmwareName(Global.Config.FilenameA78NTSCBios);
								string pal_biospath = PathManager.StandardFirmwareName(Global.Config.FilenameA78PALBios);
								string hsbiospath = PathManager.StandardFirmwareName(Global.Config.FilenameA78HSCBios);

								FileInfo ntscfile = new FileInfo(ntsc_biospath);
								FileInfo palfile = new FileInfo(pal_biospath);
								FileInfo hsfile = new FileInfo(hsbiospath);

								byte[] NTSC_BIOS7800 = null;
								byte[] PAL_BIOS7800 = null;
								byte[] HighScoreBIOS = null;
								if (!ntscfile.Exists)
								{
									MessageBox.Show("Unable to find the required Atari 7800 BIOS file - \n" + ntsc_biospath + "\nIf the selected game requires it, it may crash", "Unable to load BIOS", MessageBoxButtons.OK, MessageBoxIcon.Error);
									//throw new Exception();
								}
								else
								{
									NTSC_BIOS7800 = File.ReadAllBytes(ntsc_biospath);
								}

								if (!palfile.Exists)
								{
									MessageBox.Show("Unable to find the required Atari 7800 BIOS file - \n" + pal_biospath + "\nIf the selected game requires it, it may crash", "Unable to load BIOS", MessageBoxButtons.OK, MessageBoxIcon.Error);
									//throw new Exception();
								}
								else
								{
									PAL_BIOS7800 = File.ReadAllBytes(pal_biospath);
								}

								if (!hsfile.Exists)
								{
									MessageBox.Show("Unable to find the required Atari 7800 BIOS file - \n" + hsbiospath + "\nIf the selected game requires it, it may crash", "Unable to load BIOS", MessageBoxButtons.OK, MessageBoxIcon.Error);
									//throw new Exception();
								}
								else
								{
									HighScoreBIOS = File.ReadAllBytes(hsbiospath);
								}

								string gamedbpath = Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "EMU7800.csv");
								var a78 = new Atari7800(nextComm, game, rom.RomData, NTSC_BIOS7800, PAL_BIOS7800, HighScoreBIOS, gamedbpath);
								nextEmulator = a78;
								break;
							case "C64":
								C64 c64 = new C64(nextComm, game, rom.RomData, rom.Extension);
								c64.HardReset();
								nextEmulator = c64;
								break;
							case "GBA":
								if (INTERIM)
								{
									string gbabiospath = PathManager.StandardFirmwareName(Global.Config.FilenameGBABIOS);
									byte[] gbabios = null;

									if (File.Exists(gbabiospath))
									{
										gbabios = File.ReadAllBytes(gbabiospath);
									}
									else
									{
										MessageBox.Show("Unable to find the required GBA BIOS file - \n" + gbabiospath, "Unable to load BIOS", MessageBoxButtons.OK, MessageBoxIcon.Error);
										throw new Exception();
									}
									GBA gba = new GBA(nextComm);
									//var gba = new GarboDev.GbaManager(nextComm);
									gba.Load(rom.RomData, gbabios);
									nextEmulator = gba;
								}
								break;
							case "N64":
								if (INTERIM)
								{
									Global.Game = game;
									VideoPluginSettings video_settings = N64GenerateVideoSettings(game, hasmovie);
									int SaveType = 0;
									if (game.OptionValue("SaveType") == "EEPROM_16K")
									{
										SaveType = 1;
									}
									nextEmulator = new N64(nextComm, game, rom.RomData, video_settings, SaveType);
								}
								break;
						}
					}

					if (nextEmulator == null)
						throw new Exception("No core could load the rom.");
				}
				catch (Exception ex)
				{
					MessageBox.Show("Exception during loadgame:\n\n" + ex.ToString());
					return false;
				}

				if (nextEmulator == null) throw new Exception("No core could load the rom.");

				CloseGame();
				Global.Emulator.Dispose();
				Global.Emulator = nextEmulator;
				Global.CoreComm = nextComm;
				Global.Game = game;
				SyncCoreCommInputSignals();
				SyncControls();

				if (nextEmulator is LibsnesCore)
				{
					var snes = nextEmulator as LibsnesCore;
					snes.SetPalette((SnesColors.ColorType)Enum.Parse(typeof(SnesColors.ColorType), Global.Config.SNESPalette, false));
				}

				if (game.System == "NES")
				{
					NES nes = Global.Emulator as NES;
					if (nes.GameName != null)
						Global.Game.Name = nes.GameName;
					Global.Game.Status = nes.RomStatus;
					SetNESSoundChannels();
				}

				Text = DisplayNameForSystem(game.System) + " - " + game.Name;
				ResetRewindBuffer();

				if (Global.Emulator.CoreComm.RomStatusDetails == null)
				{
					Global.Emulator.CoreComm.RomStatusDetails =
						string.Format("{0}\r\nSHA1:{1}\r\nMD5:{2}\r\n",
						game.Name,
						Util.BytesToHexString(System.Security.Cryptography.SHA1.Create().ComputeHash(rom.RomData)),
						Util.BytesToHexString(System.Security.Cryptography.MD5.Create().ComputeHash(rom.RomData)));
				}

				//restarts the lua console if a different rom is loaded.
				//im not really a fan of how this is done..
				if (Global.Config.RecentRoms.IsEmpty || Global.Config.RecentRoms.GetRecentFileByPosition(0) != file.CanonicalFullPath)
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
				//    throttle.SetCoreFps(Global.Emulator.CoreComm.VsyncRate);
				//    SyncThrottle();
				//}
				RamSearch1.Restart();
				RamWatch1.Restart();
				HexEditor1.Restart();
				NESPPU1.Restart();
				NESNameTableViewer1.Restart();
				NESDebug1.Restart();
				GBGPUView1.Restart();
				GBAGPUView1.Restart();
				PCEBGViewer1.Restart();
				TI83KeyPad1.Restart();
				TAStudio1.Restart();
				VirtualPadForm1.Restart();
				Cheats1.Restart();
				ToolBox1.Restart();
				TraceLogger1.Restart();

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

				Global.StickyXORAdapter.ClearStickies();
				Global.AutofireStickyXORAdapter.ClearStickies();

				RewireSound();

				return true;
			}
		}

		void RewireSound()
		{
			if (DumpProxy != null)
			{
				// we're video dumping, so async mode only and use the DumpProxy.
				// note that the avi dumper has already rewired the emulator itself in this case.
				Global.Sound.SetAsyncInputPin(DumpProxy);
			}
			else if (Global.Config.SoundThrottle)
			{
				// for sound throttle, use sync mode
				Global.Emulator.EndAsyncSound();
				Global.Sound.SetSyncInputPin(Global.Emulator.SyncSoundProvider);
			}
			else
			{
				// for vsync\clock throttle modes, use async
				if (!Global.Emulator.StartAsyncSound())
				{
					// if the core doesn't support async mode, use a standard vecna wrapper
					Global.Sound.SetAsyncInputPin(new Emulation.Sound.MetaspuAsync(Global.Emulator.SyncSoundProvider, Emulation.Sound.ESynchMethod.ESynchMethod_V));
				}
				else
				{
					Global.Sound.SetAsyncInputPin(Global.Emulator.SoundProvider);
				}
			}
		}

		private void UpdateDumpIcon()
		{
			DumpStatus.Image = Properties.Resources.Blank;
			DumpStatus.ToolTipText = "";

			if (Global.Emulator == null) return;
			if (Global.Game == null) return;

			var status = Global.Game.Status;
			string annotation;
			if (status == RomStatus.BadDump)
			{
				DumpStatus.Image = Properties.Resources.ExclamationRed;
				annotation = "Warning: Bad ROM Dump";
			}
			else if (status == RomStatus.Overdump)
			{
				DumpStatus.Image = Properties.Resources.ExclamationRed;
				annotation = "Warning: Overdump";
			}
			else if (status == RomStatus.NotInDatabase)
			{
				DumpStatus.Image = Properties.Resources.RetroQuestion;
				annotation = "Warning: Unknown ROM";
			}
			else if (status == RomStatus.TranslatedRom)
			{
				DumpStatus.Image = Properties.Resources.Translation;
				annotation = "Translated ROM";
			}
			else if (status == RomStatus.Homebrew)
			{
				DumpStatus.Image = Properties.Resources.HomeBrew;
				annotation = "Homebrew ROM";
			}
			else if (Global.Game.Status == RomStatus.Hack)
			{
				DumpStatus.Image = Properties.Resources.Hack;
				annotation = "Hacked ROM";
			}
			else if (Global.Game.Status == RomStatus.Unknown)
			{
				DumpStatus.Image = Properties.Resources.Hack;
				annotation = "Warning: ROM of Unknown Character";
			}
			else
			{
				DumpStatus.Image = Properties.Resources.GreenCheck;
				annotation = "Verified good dump";
			}
			if (!string.IsNullOrEmpty(Global.Emulator.CoreComm.RomStatusAnnotation))
				annotation = Global.Emulator.CoreComm.RomStatusAnnotation;

			DumpStatus.ToolTipText = annotation;
		}


		private void LoadSaveRam()
		{
			//zero says: this is sort of sketchy... but this is no time for rearchitecting
			try
			{
				byte[] sram;
				// GBA core might not know how big the saveram ought to be, so just send it the whole file
				if (Global.Emulator is GBA)
				{
					sram = File.ReadAllBytes(PathManager.SaveRamPath(Global.Game));
				}
				else
				{
					sram = new byte[Global.Emulator.ReadSaveRam().Length];
					using (var reader = new BinaryReader(new FileStream(PathManager.SaveRamPath(Global.Game), FileMode.Open, FileAccess.Read)))
					reader.Read(sram, 0, sram.Length);
				}
				Global.Emulator.StoreSaveRam(sram);
			}
			catch (IOException) { }
		}

		private static void SaveRam()
		{
			string path = PathManager.SaveRamPath(Global.Game);

			var f = new FileInfo(path);
			if (f.Directory != null && f.Directory.Exists == false)
				f.Directory.Create();

			//Make backup first
			if (Global.Config.BackupSaveram && f.Exists)
			{
				string backup = path + ".bak";
				var backupFile = new FileInfo(backup);
				if (backupFile.Exists)
					backupFile.Delete();
				f.CopyTo(backup);
			}


			var writer = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write));

			var saveram = Global.Emulator.ReadSaveRam();

			// this assumes that the default state of the core's sram is 0-filled, so don't do
			// int len = Util.SaveRamBytesUsed(saveram);
			int len = saveram.Length;
			writer.Write(saveram, 0, len);
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
				if (ActiveForm == this) return true;

				//modals that need to capture input for binding purposes get input, of course
				if (ActiveForm is HotkeyWindow) return true;
				//if (ActiveForm is ControllerConfig) return true;
				if (ActiveForm is config.NewControllerConfig) return true;
				if (ActiveForm is TAStudio) return true;
				//if no form is active on this process, then the background input setting applies
				if (ActiveForm == null && Global.Config.AcceptBackgroundInput) return true;

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

				//look for hotkey bindings for this key
				var triggers = Global.ClientControls.SearchBindings(ie.LogicalButton.ToString());
				if (triggers.Count == 0)
				{
					//bool sys_hotkey = false;

					//maybe it is a system alt-key which hasnt been overridden
					if (ie.EventType == Input.InputEventType.Press)
					{
						if (ie.LogicalButton.Alt && ie.LogicalButton.Button.Length == 1)
						{
							char c = ie.LogicalButton.Button.ToLower()[0];
							if (c >= 'a' && c <= 'z' || c == ' ')
							{
								SendAltKeyChar(c);
								//sys_hotkey = true;
							}
						}
						if (ie.LogicalButton.Alt && ie.LogicalButton.Button == "Space")
						{
							SendPlainAltKey(32);
							//sys_hotkey = true;
						}
					}
					//ordinarily, an alt release with nothing else would move focus to the menubar. but that is sort of useless, and hard to implement exactly right.

					//????????????
					//no hotkeys or system keys bound this, so mutate it to an unmodified key and assign it for use as a game controller input
					//(we have a rule that says: modified events may be used for game controller inputs but not hotkeys)
					//if (!sys_hotkey)
					//{
					//  var mutated_ie = new Input.InputEvent();
					//  mutated_ie.EventType = ie.EventType;
					//  mutated_ie.LogicalButton = ie.LogicalButton;
					//  mutated_ie.LogicalButton.Modifiers = Input.ModifierKey.None;
					//  Global.ControllerInputCoalescer.Receive(ie);
					//}
				}

				//zero 09-sep-2012 - all input is eligible for controller input. not sure why the above was done. 
				//maybe because it doesnt make sense to me to bind hotkeys and controller inputs to the same keystrokes
				
				//adelikat 02-dec-2012 - implemented options for how to handle controller vs hotkey conflicts.  This is primarily motivated by computer emulation and thus controller being nearly the entire keyboard
				bool handled;
				switch (Global.Config.Input_Hotkey_OverrideOptions)
				{
					default:
					case 0: //Both allowed
						Global.ControllerInputCoalescer.Receive(ie);

						handled = false;
						if (ie.EventType == Input.InputEventType.Press)
						{
							handled = triggers.Aggregate(handled, (current, trigger) => current | CheckHotkey(trigger));
						}

						//hotkeys which arent handled as actions get coalesced as pollable virtual client buttons
						if (!handled)
						{
							Global.HotkeyCoalescer.Receive(ie);
						}
						break;
					case 1: //Input overrides Hokeys
						Global.ControllerInputCoalescer.Receive(ie);
						bool inputisbound = Global.ActiveController.HasBinding(ie.LogicalButton.ToString());
						if (!inputisbound)
						{
							handled = false;
							if (ie.EventType == Input.InputEventType.Press)
							{
								handled = triggers.Aggregate(handled, (current, trigger) => current | CheckHotkey(trigger));
							}

							//hotkeys which arent handled as actions get coalesced as pollable virtual client buttons
							if (!handled)
							{
								Global.HotkeyCoalescer.Receive(ie);
							}
						}
						break;
					case 2: //Hotkeys override Input
						handled = false;
						if (ie.EventType == Input.InputEventType.Press)
						{
							handled = triggers.Aggregate(handled, (current, trigger) => current | CheckHotkey(trigger));
						}

						//hotkeys which arent handled as actions get coalesced as pollable virtual client buttons
						if (!handled)
						{
							Global.HotkeyCoalescer.Receive(ie);
							Global.ControllerInputCoalescer.Receive(ie);
						}
						break;
				}

			} //foreach event
		}

		private void ClearAutohold()
		{
			Global.StickyXORAdapter.ClearStickies();
			Global.AutofireStickyXORAdapter.ClearStickies();
			VirtualPadForm1.ClearVirtualPadHolds();
			Global.OSD.AddMessage("Autohold keys cleared");
		}

		bool CheckHotkey(string trigger)
		{
			//todo - could have these in a table somehow ?
			switch (trigger)
			{
				default:
					return false;

				case "SNES Toggle BG 1":
					SNES_ToggleBG1();
					break;
				case "SNES Toggle BG 2":
					SNES_ToggleBG2();
					break;
				case "SNES Toggle BG 3":
					SNES_ToggleBG3();
					break;
				case "SNES Toggle BG 4":
					SNES_ToggleBG4();
					break;
				case "SNES Toggle OBJ 1":
					SNES_ToggleOBJ1();
					break;
				case "SNES Toggle OBJ 2":
					SNES_ToggleOBJ2();
					break;
				case "SNES Toggle OBJ 3":
					SNES_ToggleOBJ3();
					break;
				case "SNES Toggle OBJ 4":
					SNES_ToggleOBJ4();
					break;
				case "Save Movie":
					SaveMovie();
					break;
				case "Clear Autohold":
					ClearAutohold();
					break;
				case "IncreaseWindowSize":
					IncreaseWindowSize();
					break;
				case "DecreaseWindowSize":
					DecreaseWIndowSize();
					break;
				case "Record AVI/WAV":
					RecordAVI();
					break;
				case "Stop AVI/WAV":
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

				case "Reboot Core":
					{
						bool autoSaveState = Global.Config.AutoSavestates;
						Global.Config.AutoSavestates = false;
						LoadRom(CurrentlyOpenRom);
						Global.Config.AutoSavestates = autoSaveState;
						break;
					}

				case "Hard Reset":
					HardReset();
					break;
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
				case "Virtual Pad": LoadVirtualPads(); break;
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
							
							if (Global.Config.VBAStyleMovieLoadState)
							{
								Global.OSD.AddMessage("Multi-track can not be used in Full Movie Loadstates mode");
							}
							else
							{
								Global.MovieSession.MultiTrack.IsActive = !Global.MovieSession.MultiTrack.IsActive;
								if (Global.MovieSession.MultiTrack.IsActive)
								{
									Global.OSD.AddMessage("MultiTrack Enabled");
									Global.OSD.MT = "Recording None";
								}
								else
								{
									Global.OSD.AddMessage("MultiTrack Disabled");
								}
								Global.MovieSession.MultiTrack.RecordAll = false;
								Global.MovieSession.MultiTrack.CurrentPlayer = 0;
							}
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
				case "MoviePokeToggle":
					ToggleModePokeMode();
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
			if (UpdateFrame)
			{
				runFrame = true;
				if (Global.MovieSession.Movie.IsRecording)
				{
					Global.MovieSession.Movie.SwitchToPlay();
				}
			}

			bool genSound = false;
			bool coreskipaudio = false;
			if (runFrame)
			{
				bool ff = Global.ClientControls["Fast Forward"] || Global.ClientControls["MaxTurbo"];
				bool fff = Global.ClientControls["MaxTurbo"];
				bool updateFpsString = (runloop_last_ff != ff);
				runloop_last_ff = ff;

				if (!fff)
				{
					UpdateToolsBefore();
				}

				Global.ClickyVirtualPadController.FrameTick();

				runloop_fps++;
				//client input-related duties
				Global.OSD.ClearGUIText();

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

				coreskipaudio = Global.ClientControls["MaxTurbo"] && CurrAviWriter == null;
				//=======================================
				MemoryPulse.Pulse();
				Global.Emulator.FrameAdvance(!throttle.skipnextframe || CurrAviWriter != null, !coreskipaudio);
				MemoryPulse.Pulse();
				//=======================================

				if (!PauseAVI)
				{
					AVIFrameAdvance();
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
			if (UpdateFrame)
			{
				if (ReturnToRecording)
				{
					Global.MovieSession.Movie.SwitchToRecord();
				}
				UpdateFrame = false;
			}

			if (genSound && !coreskipaudio)
			{
				Global.Sound.UpdateSound();
			}
			else
				Global.Sound.UpdateSilence();
		}

		/// <summary>
		/// Update all tools that are frame dependent like Ram Search before processing
		/// </summary>
		public void UpdateToolsBefore(bool fromLua = false)
		{
#if WINDOWS
			if (!fromLua)
				LuaConsole1.StartLuaDrawing();
			LuaConsole1.LuaImp.FrameRegisterBefore();

#endif
			NESNameTableViewer1.UpdateValues();
			NESPPU1.UpdateValues();
			PCEBGViewer1.UpdateValues();
			GBGPUView1.UpdateValues();
			GBAGPUView1.UpdateValues();
		}

		public void UpdateToolsLoadstate()
		{
			SNESGraphicsDebugger1.UpdateToolsLoadstate();
		}

		/// <summary>
		/// Update all tools that are frame dependent like Ram Search after processing
		/// </summary>
		public void UpdateToolsAfter(bool fromLua = false)
		{
#if WINDOWS
			if (!fromLua)
				LuaConsole1.ResumeScripts(true);

#endif
			RamWatch1.UpdateValues();
			RamSearch1.UpdateValues();
			HexEditor1.UpdateValues();
			//The other tool updates are earlier, TAStudio needs to be later so it can display the latest
			//frame of execution in its list view.

			TAStudio1.UpdateValues();
			VirtualPadForm1.UpdateValues();
			SNESGraphicsDebugger1.UpdateToolsAfter();
			TraceLogger1.UpdateValues();
			HandleToggleLight();
#if WINDOWS
			LuaConsole1.LuaImp.FrameRegisterAfter();
			if (!fromLua)
			{
				Global.DisplayManager.PreFrameUpdateLuaSource();
				LuaConsole1.EndLuaDrawing();
			}
#endif
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

		public void TakeScreenshotToClipboard()
		{
			using (var img = Global.Config.Screenshot_CaptureOSD ? CaptureOSD() : MakeScreenshotImage())
			{
				Clipboard.SetImage(img);
			}
			Global.OSD.AddMessage("Screenshot saved to clipboard.");
		}

		public void TakeScreenshot()
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

		public void TakeScreenshot(string path)
		{
			var fi = new FileInfo(path);
			if (fi.Directory != null && fi.Directory.Exists == false)
				fi.Directory.Create();
			using (var img = Global.Config.Screenshot_CaptureOSD ? CaptureOSD() : MakeScreenshotImage())
			{
				img.Save(fi.FullName, ImageFormat.Png);
			}
			Global.OSD.AddMessage(fi.Name + " saved.");
		}

		public void SaveState(string name)
		{
			string path = PathManager.SaveStatePrefix(Global.Game) + "." + name + ".State";

			var file = new FileInfo(path);
			if (file.Directory != null && file.Directory.Exists == false)
				file.Directory.Create();

			//Make backup first
			if (Global.Config.BackupSavestates && file.Exists)
			{
				string backup = path + ".bak";
				var backupFile = new FileInfo(backup);
				if (backupFile.Exists)
					backupFile.Delete();
				file.CopyTo(backup);
			}

			SaveStateFile(path, name, false);
			LuaConsole1.LuaImp.SavestateRegisterSave(name);
		}

		public void SaveStateFile(string filename, string name, bool fromLua)
		{
			// since movie mode requires input log, always save text in that case
			if (Global.MovieSession.Movie.IsActive ||
				Global.Config.SaveStateType == Config.SaveStateTypeE.Text ||
				(Global.Config.SaveStateType == Config.SaveStateTypeE.Default && !Global.Emulator.BinarySaveStatesPreferred))
			{
				var writer = new StreamWriter(filename);
				Global.Emulator.SaveStateText(writer);
				HandleMovieSaveState(writer);
				if (Global.Config.SaveScreenshotWithStates)
				{
					writer.Write("Framebuffer ");
					Global.Emulator.VideoProvider.GetVideoBuffer().SaveAsHex(writer);
				}
				writer.Close();
			}
			else
			{
				// binary savestate
				var writer = new BinaryWriter(new FileStream(filename, FileMode.Create));
				Global.Emulator.SaveStateBinary(writer);
				if (Global.Config.SaveScreenshotWithStates)
				{
					writer.Write("FRAMEBUFFA");
					var buff = Global.Emulator.VideoProvider.GetVideoBuffer();
					writer.Write(buff.Length);
					writer.Write(buff);
				}
				writer.Close();
			}
			Global.OSD.AddMessage("Saved state: " + name);

			if (!fromLua)
				UpdateStatusSlots();
		}

		private void SaveStateAs()
		{
			if (IsNullEmulator()) return;
			var sfd = new SaveFileDialog();
			string path = PathManager.GetSaveStatePath(Global.Game);
			sfd.InitialDirectory = path;
			sfd.FileName = PathManager.SaveStatePrefix(Global.Game) + "." + "QuickSave0.State";
			var file = new FileInfo(path);
			if (file.Directory != null && file.Directory.Exists == false)
				file.Directory.Create();

			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();

			if (result != DialogResult.OK)
				return;

			SaveStateFile(sfd.FileName, sfd.FileName, false);
		}

		public void LoadStateFile(string path, string name, bool fromLua = false)
		{
			if (!Global.MovieSession.Movie.IsActive)
			{
				// only when movies are not playing can we possibly load binary savestates
				bool binary = false;
				using (var s = new FileStream(path, FileMode.Open, FileAccess.Read))
				{
					int i;
					while ((i = s.ReadByte()) != -1)
					{
						// unicode support will need something better here
						if (i < 0x9 || (i > 0x7f))
						{
							binary = true;
							break;
						}
					}
				}

				if (binary)
				{
					using (var reader = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read)))
					{
						Global.Emulator.LoadStateBinary(reader);
						try
						{
							string s = reader.ReadString();
							if (s.Equals("FRAMEBUFFA"))
							{
								int len = reader.ReadInt32();
								var buff = Global.Emulator.VideoProvider.GetVideoBuffer();
								for (int i = 0; i < len; i++)
									buff[i] = reader.ReadInt32();
							}
						}
						catch { }
					}
					goto cleanup;
				}
				else
				{
					// fall through to text situation
				}
			}


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
			}
			else
				Global.OSD.AddMessage("Loadstate error!");

			cleanup:
			Global.OSD.ClearGUIText();
			UpdateToolsBefore(fromLua);
			UpdateToolsAfter(fromLua);
			UpdateToolsLoadstate();
			Global.OSD.AddMessage("Loaded state: " + name);
			LuaConsole1.LuaImp.SavestateRegisterLoad(name);
		}

		public void LoadState(string name, bool fromLua = false)
		{
			string path = PathManager.SaveStatePrefix(Global.Game) + "." + name + ".State";
			if (File.Exists(path) == false)
			{
				Global.OSD.AddMessage("Unable to load " + name + ".State");
				return;
			}

			LoadStateFile(path, name, fromLua);
		}

		private void LoadStateAs()
		{
			if (IsNullEmulator()) return;
			var ofd = new OpenFileDialog
				{
					InitialDirectory = PathManager.GetSaveStatePath(Global.Game),
					Filter = "Save States (*.State)|*.State|All Files|*.*",
					RestoreDirectory = true
				};

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
			if (Global.Config.AutoLoadMostRecentRom)
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
			if (Global.Config.AutoLoadMostRecentMovie)
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

		public void LoadNesSoundConfig()
		{
			if (Global.Emulator is NES)
			{
				if (!NesSound.IsHandleCreated || NesSound.IsDisposed)
				{
					NesSound = new NESSoundConfig();
					NesSound.Show();
				}
				else
					NesSound.Focus();
			}
		}

		public void LoadGameGenieEC()
		{

			if (Global.Emulator is NES)
			{
				if (!NESgg.IsHandleCreated || NESgg.IsDisposed)
				{
					NESgg = new NESGameGenie();
					NESgg.Show();
				}
				else
					NESgg.Focus();
			}
			else if (Global.Emulator is LibsnesCore)
			{
				if (!SNESgg.IsHandleCreated || SNESgg.IsDisposed)
				{
					SNESgg = new SNESGameGenie();
					SNESgg.Show();
				}
				else
					SNESgg.Focus();
			}
			else if ((Global.Emulator.SystemId == "GB")  || (Global.Game.System == "GG"))
			{
				if (!GBgg.IsHandleCreated || GBgg.IsDisposed)
				{
					GBgg = new GBGameGenie();
					GBgg.Show();
				}
				else
					GBgg.Focus();
			}
			else if (Global.Emulator is Genesis)
			{
				if (!Gengg.IsHandleCreated || Gengg.IsDisposed)
				{
					Gengg = new GenGameGenie();
					Gengg.Show();
				}
				else
					Gengg.Focus();
			}
		}

		public void LoadSNESGraphicsDebugger()
		{
			if (!SNESGraphicsDebugger1.IsHandleCreated || SNESGraphicsDebugger1.IsDisposed)
			{
				SNESGraphicsDebugger1 = new SNESGraphicsDebugger();
				SNESGraphicsDebugger1.UpdateToolsLoadstate();
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

		public void LoadTraceLogger()
		{
			if (!TraceLogger1.IsHandleCreated || TraceLogger1.IsDisposed)
			{
				TraceLogger1 = new TraceLogger();
				TraceLogger1.Show();
			}
			else
				TraceLogger1.Focus();
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

		public void LoadGBGPUView()
		{
			if (!GBGPUView1.IsHandleCreated || GBGPUView1.IsDisposed)
			{
				GBGPUView1 = new GBtools.GBGPUView();
				GBGPUView1.Show();
			}
			else
				GBGPUView1.Focus();
		}

		public void LoadGBAGPUView()
		{
			if (!GBAGPUView1.IsHandleCreated || GBAGPUView1.IsDisposed)
			{
				GBAGPUView1 = new GBAtools.GBAGPUView();
				GBAGPUView1.Show();
			}
			else
				GBAGPUView1.Focus();
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

		public VideoPluginSettings N64GenerateVideoSettings(GameInfo game, bool hasmovie)
		{
			string PluginToUse = "";

			if (hasmovie && Global.MovieSession.Movie.Header.HeaderParams[MovieHeader.PLATFORM] == "N64" && Global.MovieSession.Movie.Header.HeaderParams.ContainsKey(MovieHeader.VIDEOPLUGIN))
			{
				PluginToUse = Global.MovieSession.Movie.Header.HeaderParams[MovieHeader.VIDEOPLUGIN];
			}

			if (PluginToUse == "" || (PluginToUse != "Rice" && PluginToUse != "Glide64")) 
			{
				PluginToUse = Global.Config.N64VidPlugin;
			}

			VideoPluginSettings video_settings = new VideoPluginSettings(PluginToUse, Global.Config.N64VideoSizeX, Global.Config.N64VideoSizeY);

			if (PluginToUse == "Rice")
			{
				Global.Config.RicePlugin.FillPerGameHacks(game);
				video_settings.Parameters = Global.Config.RicePlugin.GetPluginSettings();
			}
			else if (PluginToUse == "Glide64")
			{
				Global.Config.GlidePlugin.FillPerGameHacks(game);
				video_settings.Parameters = Global.Config.GlidePlugin.GetPluginSettings();
			}
			
			if (hasmovie && Global.MovieSession.Movie.Header.HeaderParams[MovieHeader.PLATFORM] == "N64" && Global.MovieSession.Movie.Header.HeaderParams.ContainsKey(MovieHeader.VIDEOPLUGIN))
			{
				List<string> settings = new List<string>(video_settings.Parameters.Keys);
				foreach (string setting in settings)
				{
					if (Global.MovieSession.Movie.Header.HeaderParams.ContainsKey(setting))
					{
						string Value = Global.MovieSession.Movie.Header.HeaderParams[setting];
						if (video_settings.Parameters[setting].GetType() == typeof(bool))
						{
							try
							{
								video_settings.Parameters[setting] = bool.Parse(Value);
							}
							catch { }
							/*
							if (Value == "True")
							{
								video_settings.Parameters[setting] = true;
							}
							else if (Value == "False")
							{
								video_settings.Parameters[setting] = false;
							}*/
						}
						else if (video_settings.Parameters[setting].GetType() == typeof(int))
						{
							try
							{
								video_settings.Parameters[setting] = int.Parse(Value);
							}
							catch { }
						}
					}
				}
			}
			
			return video_settings;
		}

		public bool N64GetBoolFromDB(string parameter)
		{
			if (Global.Game.OptionPresent(parameter) && Global.Game.OptionValue(parameter) == "true")
				return true;
			else
				return false;
		}

		public int N64GetIntFromDB(string parameter, int defaultVal)
		{
			if (Global.Game.OptionPresent(parameter) && InputValidate.IsValidUnsignedNumber(Global.Game.OptionValue(parameter)))
				return int.Parse(Global.Game.OptionValue(parameter));
			else
				return defaultVal;
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
			// run this entire thing exactly twice, since the first resize may adjust the menu stacking
			for (int i = 0; i < 2; i++)
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
		}

		public void ToggleFullscreen()
		{
			if (InFullscreen == false)
			{
				_windowed_location = Location;
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
				FormBorderStyle = FormBorderStyle.Sizable;
				WindowState = FormWindowState.Normal;
				MainMenuStrip.Visible = true;
				StatusSlot0.Visible = Global.Config.DisplayStatusBar;
				Location = _windowed_location;
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
			if ((ModifierKeys & Keys.Alt) != 0)
				return true;
			else return base.ProcessDialogChar(charCode);
		}

		//sends a simulation of a plain alt key keystroke
		void SendPlainAltKey(int lparam)
		{
			Message m = new Message {WParam = new IntPtr(0xF100), LParam = new IntPtr(lparam), Msg = 0x0112, HWnd = Handle};
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

		int LastOpenRomFilter;
		private void OpenROM()
		{
			var ofd = new OpenFileDialog {InitialDirectory = PathManager.GetRomsPath(Global.Emulator.SystemId)};
			//"Rom Files|*.NES;*.SMS;*.GG;*.SG;*.PCE;*.SGX;*.GB;*.BIN;*.SMD;*.ROM;*.ZIP;*.7z|NES (*.NES)|*.NES|Master System|*.SMS;*.GG;*.SG;*.ZIP;*.7z|PC Engine|*.PCE;*.SGX;*.ZIP;*.7z|Gameboy|*.GB;*.ZIP;*.7z|TI-83|*.rom|Archive Files|*.zip;*.7z|Savestate|*.state|All Files|*.*";

			//adelikat: ugly design for this, I know
			if (INTERIM)
			{
				ofd.Filter = FormatFilter(
					"Rom Files", "*.nes;*.fds;*.sms;*.gg;*.sg;*.pce;*.sgx;*.bin;*.smd;*.rom;*.a26;*.a78;*.cue;*.exe;*.gb;*.gbc;*.gen;*.md;*.col;.int;*.smc;*.sfc;*.prg;*.d64;*.g64;*.crt;*.sgb;*.xml;*.z64;*.v64;*.n64;%ARCH%",
					"Music Files", "*.psf;*.sid",
					"Disc Images", "*.cue",
					"NES", "*.nes;*.fds;%ARCH%",
					"Super NES", "*.smc;*.sfc;*.xml;%ARCH%",
					"Master System", "*.sms;*.gg;*.sg;%ARCH%",
					"PC Engine", "*.pce;*.sgx;*.cue;%ARCH%",
					"TI-83", "*.rom;%ARCH%",
					"Archive Files", "%ARCH%",
					"Savestate", "*.state",
					"Atari 2600", "*.a26;*.bin;%ARCH%",
					"Atari 7800", "*.a78;*.bin;%ARCH%",
					"Genesis (experimental)", "*.gen;*.smd;*.bin;*.md;*.cue;%ARCH%",
					"Gameboy", "*.gb;*.gbc;*.sgb;%ARCH%",
					"Colecovision", "*.col;%ARCH%",
					"Intellivision (very experimental)", "*.int;*.bin;*.rom;%ARCH%",
					"PSX Executables (very experimental)", "*.exe",
					"PSF Playstation Sound File (very experimental)", "*.psf",
					"Commodore 64 (experimental)", "*.prg; *.d64, *.g64; *.crt;%ARCH%",
					"SID Commodore 64 Music File", "*.sid;%ARCH%",
					"Nintendo 64", "*.z64;*.v64;*.n64",
					"All Files", "*.*");
			}
			else
			{
				ofd.Filter = FormatFilter(
					"Rom Files", "*.nes;*.fds;*.sms;*.gg;*.sg;*.gb;*.gbc;*.pce;*.sgx;*.bin;*.smd;*.gen;*.md;*.smc;*.sfc;*.a26;*.a78;*.col;*.rom;*.cue;*.sgb;*.xml;%ARCH%",
					"Disc Images", "*.cue",
					"NES", "*.nes;*.fds;%ARCH%",
					"Super NES", "*.smc;*.sfc;*.xml;%ARCH%",
					"Gameboy", "*.gb;*.gbc;*.sgb;%ARCH%",
					"Master System", "*.sms;*.gg;*.sg;%ARCH%",
					"PC Engine", "*.pce;*.sgx;*.cue;%ARCH%",
					"Atari 2600", "*.a26;%ARCH%",
					"Atari 7800", "*.a78;%ARCH%",
					"Colecovision", "*.col;%ARCH%",
					"TI-83", "*.rom;%ARCH%",
					"Archive Files", "%ARCH%",
					"Savestate", "*.state",
					"Genesis (experimental)", "*.gen;*.md;*.smd;*.bin;*.cue;%ARCH%",
					
					"All Files", "*.*");
			}
			
			ofd.RestoreDirectory = false;
			ofd.FilterIndex = LastOpenRomFilter;
			
			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();
			if (result != DialogResult.OK)
				return;
			var file = new FileInfo(ofd.FileName);
			Global.Config.LastRomPath = file.DirectoryName;
			LastOpenRomFilter = ofd.FilterIndex;
			LoadRom(file.FullName);
		}

		//-------------------------------------------------------
		//whats the difference between these two methods??
		//its very tricky. rename to be more clear or combine them.

		private void CloseGame()
		{
			if (Global.Config.AutoSavestates && Global.Emulator is NullEmulator == false)
				SaveState("Auto");
			if (Global.Emulator.SaveRamModified)
				SaveRam();
			StopAVI();
			Global.Emulator.Dispose();
			Global.CoreComm = new CoreComm();
			SyncCoreCommInputSignals();
			Global.Emulator = new NullEmulator(Global.CoreComm);
			Global.ActiveController = Global.NullControls;
			Global.AutoFireController = Global.AutofireNullControls;
			Global.MovieSession.Movie.Stop();
			NeedsReboot = false;
			SetRebootIconStatus();
		}

		public void CloseROM()
		{
			CloseGame();
			Global.CoreComm = new CoreComm();
			SyncCoreCommInputSignals();
			Global.Emulator = new NullEmulator(Global.CoreComm);
			Global.Game = GameInfo.GetNullGame();
			MemoryPulse.Clear();
			RewireSound();
			ResetRewindBuffer();
			RamSearch1.Restart();
			RamWatch1.Restart();
			HexEditor1.Restart();
			NESPPU1.Restart();
			NESNameTableViewer1.Restart();
			NESDebug1.Restart();
			GBGPUView1.Restart();
			GBAGPUView1.Restart();
			PCEBGViewer1.Restart();
			TI83KeyPad1.Restart();
			Cheats1.Restart();
			ToolBox1.Restart();
#if WINDOWS
			LuaConsole1.Restart();
#endif
			Text = "BizHawk" + (INTERIM ? " (interim) " : "");
			HandlePlatformMenus();
			StateSlots.Clear();
			UpdateDumpIcon();
		}

		//-------------------------------------------------------

		private void SaveConfig()
		{
			if (Global.Config.SaveWindowPosition)
			{
				Global.Config.MainWndx = Location.X;
				Global.Config.MainWndy = Location.Y;
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
			CloseForm(GBGPUView1);
			CloseForm(GBAGPUView1);
			CloseForm(PCEBGViewer1);
			CloseForm(Cheats1);
			CloseForm(TI83KeyPad1);
			CloseForm(TAStudio1);
			CloseForm(TraceLogger1);
			CloseForm(VirtualPadForm1);
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
				if (Global.Config.AutoLoadRamWatch && Global.Config.RecentWatches.Count > 0)
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

		public void LoadVirtualPads()
		{
			if (!VirtualPadForm1.IsHandleCreated || VirtualPadForm1.IsDisposed)
			{
				VirtualPadForm1 = new VirtualPadForm();
				VirtualPadForm1.Show();
			}
			else
				VirtualPadForm1.Focus();
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
				if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
				{
					Global.ClickyVirtualPadController.Click("Reset");
					Global.OSD.AddMessage("Reset button pressed.");
				}
			}
		}

		private void HardReset()
		{
			//is it enough to run this for one frame? maybe..
			if (Global.Emulator.ControllerDefinition.BoolButtons.Contains("Power"))
			{
				if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
				{
					Global.ClickyVirtualPadController.Click("Power");
					Global.OSD.AddMessage("Power button pressed.");
				}
			}
		}

		public void UpdateStatusSlots()
		{
			StateSlots.Update();

			if (StateSlots.HasSlot(1))
			{
				StatusSlot1.ForeColor = Color.Black;
			}
			else
			{
				StatusSlot1.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(2))
			{
				StatusSlot2.ForeColor = Color.Black;
			}
			else
			{
				StatusSlot2.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(3))
			{
				StatusSlot3.ForeColor = Color.Black;
			}
			else
			{
				StatusSlot3.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(3))
			{
				StatusSlot3.ForeColor = Color.Black;
			}
			else
			{
				StatusSlot3.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(4))
			{
				StatusSlot4.ForeColor = Color.Black;
			}
			else
			{
				StatusSlot4.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(5))
			{
				StatusSlot5.ForeColor = Color.Black;
			}
			else
			{
				StatusSlot5.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(6))
			{
				StatusSlot6.ForeColor = Color.Black;
			}
			else
			{
				StatusSlot6.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(7))
			{
				StatusSlot7.ForeColor = Color.Black;
			}
			else
			{
				StatusSlot7.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(8))
			{
				StatusSlot8.ForeColor = Color.Black;
			}
			else
			{
				StatusSlot8.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(9))
			{
				StatusSlot9.ForeColor = Color.Black;
			}
			else
			{
				StatusSlot9.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(0))
			{
				StatusSlot0.ForeColor = Color.Black;
			}
			else
			{
				StatusSlot0.ForeColor = Color.Gray;
			}

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

			if (Global.Config.SaveSlot == 0) StatusSlot10.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 1) StatusSlot1.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 2) StatusSlot2.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 3) StatusSlot3.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 4) StatusSlot4.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 5) StatusSlot5.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 6) StatusSlot6.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 7) StatusSlot7.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 8) StatusSlot8.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 9) StatusSlot9.BackColor = SystemColors.ControlDark;
		}

		#region AVI Stuff

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

			var video_writers = writers as IVideoWriter[] ?? writers.ToArray();
			if (unattended)
			{
				foreach (var w in video_writers)
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
				aw = VideoWriterChooserForm.DoVideoWriterChoserDlg(video_writers, Global.MainForm, out avwriter_resizew, out avwriter_resizeh);
			}

			foreach (var w in video_writers)
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
				aw.SetMovieParameters(Global.Emulator.CoreComm.VsyncNum, Global.Emulator.CoreComm.VsyncDen);
				if (avwriter_resizew > 0 && avwriter_resizeh > 0)
					aw.SetVideoParameters(avwriter_resizew, avwriter_resizeh);
				else
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
						sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.AVIPath);
					}
					else
					{
						sfd.FileName = "NULL";
						sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.AVIPath);
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
				AVIStatusLabel.Image = Properties.Resources.AVI;
				AVIStatusLabel.ToolTipText = "A/V capture in progress";
				AVIStatusLabel.Visible = true;

			}
			catch
			{
				Global.OSD.AddMessage("A/V capture failed!");
				aw.Dispose();
				throw;
			}

			// do sound rewire.  the plan is to eventually have AVI writing support syncsound input, but it doesn't for the moment
			if (!Global.Emulator.StartAsyncSound())
				AviSoundInput = new Emulation.Sound.MetaspuAsync(Global.Emulator.SyncSoundProvider, Emulation.Sound.ESynchMethod.ESynchMethod_V);
			else
				AviSoundInput = Global.Emulator.SoundProvider;
			DumpProxy = new Emulation.Sound.MetaspuSoundProvider(Emulation.Sound.ESynchMethod.ESynchMethod_V);
			SoundRemainder = 0;
			RewireSound();
		}

		void AbortAVI()
		{
			if (CurrAviWriter == null)
			{
				DumpProxy = null;
				RewireSound();
				return;
			}
			CurrAviWriter.Dispose();
			CurrAviWriter = null;
			Global.OSD.AddMessage("A/V capture aborted");
			AVIStatusLabel.Image = Properties.Resources.Blank;
			AVIStatusLabel.ToolTipText = "";
			AVIStatusLabel.Visible = false;
			AviSoundInput = null;
			DumpProxy = null; // return to normal sound output
			SoundRemainder = 0;
			RewireSound();
		}

		public void StopAVI()
		{
			if (CurrAviWriter == null)
			{
				DumpProxy = null;
				RewireSound();
				return;
			}
			CurrAviWriter.CloseFile();
			CurrAviWriter.Dispose();
			CurrAviWriter = null;
			Global.OSD.AddMessage("A/V capture stopped");
			AVIStatusLabel.Image = Properties.Resources.Blank;
			AVIStatusLabel.ToolTipText = "";
			AVIStatusLabel.Visible = false;
			AviSoundInput = null;
			DumpProxy = null; // return to normal sound output
			SoundRemainder = 0;
			RewireSound();
		}

		void AVIFrameAdvance()
		{
			if (CurrAviWriter != null)
			{
				long nsampnum = 44100 * (long)Global.Emulator.CoreComm.VsyncDen + SoundRemainder;
				long nsamp = nsampnum / Global.Emulator.CoreComm.VsyncNum;
				// exactly remember fractional parts of an audio sample
				SoundRemainder = nsampnum % Global.Emulator.CoreComm.VsyncNum;

				short[] temp = new short[nsamp * 2];
				AviSoundInput.GetSamples(temp);
				DumpProxy.buffer.enqueue_samples(temp, (int)nsamp);

				try
				{
					IVideoProvider output;
					if (avwriter_resizew > 0 && avwriter_resizeh > 0)
					{
						Bitmap bmpin;
						if (Global.Config.AVI_CaptureOSD)
							bmpin = CaptureOSD();
						else
						{
							bmpin = new Bitmap(Global.Emulator.VideoProvider.BufferWidth, Global.Emulator.VideoProvider.BufferHeight, PixelFormat.Format32bppArgb);
							var lockdata = bmpin.LockBits(new Rectangle(0, 0, bmpin.Width, bmpin.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
							System.Runtime.InteropServices.Marshal.Copy(Global.Emulator.VideoProvider.GetVideoBuffer(), 0, lockdata.Scan0, bmpin.Width * bmpin.Height);
							bmpin.UnlockBits(lockdata);
						}
						Bitmap bmpout = new Bitmap(avwriter_resizew, avwriter_resizeh, PixelFormat.Format32bppArgb);
						using (Graphics g = Graphics.FromImage(bmpout))
							g.DrawImage(bmpin, new Rectangle(0, 0, bmpout.Width, bmpout.Height));
						bmpin.Dispose();
						output = new AVOut.BmpVideoProvder(bmpout);
					}
					else
					{
						if (Global.Config.AVI_CaptureOSD)
							output = new AVOut.BmpVideoProvder(CaptureOSD());
						else
							output = Global.Emulator.VideoProvider;
					}

					CurrAviWriter.AddFrame(output);
					if (output is AVOut.BmpVideoProvder)
						(output as AVOut.BmpVideoProvder).Dispose();

					CurrAviWriter.AddSamples(temp);
				}
				catch (Exception e)
				{
					MessageBox.Show("Video dumping died:\n\n" + e);
					AbortAVI();
				}

				if (autoDumpLength > 0)
				{
					autoDumpLength--;
					if (autoDumpLength == 0) // finish
						StopAVI();
				}
			}
		}

		#endregion

		private void SwapBackupSavestate(string path)
		{
			//Takes the .state and .bak files and swaps them
			var state = new FileInfo(path);
			var backup = new FileInfo(path + ".bak");
			var temp = new FileInfo(path + ".bak.tmp");

			if (state.Exists == false) return;
			if (backup.Exists == false) return;
			if (temp.Exists) temp.Delete();

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

		public void LoadRamPoke()
		{
			RamPoke r = new RamPoke();
			r.Show();
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

		void ProcessMovieImport(string fn)
		{
			string d = PathManager.MakeAbsolutePath(Global.Config.MoviesPath);
			string errorMsg;
			string warningMsg;
			Movie m = MovieImport.ImportFile(fn, out errorMsg, out warningMsg);
			if (errorMsg.Length > 0)
				MessageBox.Show(errorMsg, "Conversion error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			if (warningMsg.Length > 0)
				Global.OSD.AddMessage(warningMsg);
			else
				Global.OSD.AddMessage(Path.GetFileName(fn) + " imported as " + "Movies\\" +
										Path.GetFileName(fn) + "." + Global.Config.MovieExtension);
			if (!Directory.Exists(d))
				Directory.CreateDirectory(d);

			string outPath = d + "\\" + Path.GetFileName(fn) + "." + Global.Config.MovieExtension;
			m.WriteMovie(outPath);
		}

		// workaround for possible memory leak in SysdrawingRenderPanel
		RetainedViewportPanel captureosd_rvp;
		SysdrawingRenderPanel captureosd_srp;

		/// <summary>
		/// sort of like MakeScreenShot(), but with OSD and LUA captured as well.  slow and bad.
		/// </summary>
		Bitmap CaptureOSD()
		{
			// this code captures the emu display with OSD and lua composited onto it.
			// it's slow and a bit hackish; a better solution is to create a new
			// "dummy render" class that implements IRenderer, IBlitter, and possibly
			// IVideoProvider, and pass that to DisplayManager.UpdateSourceEx()

			if (captureosd_rvp == null)
			{
				captureosd_rvp = new RetainedViewportPanel();
				captureosd_srp = new SysdrawingRenderPanel(captureosd_rvp);
			}

			// this size can be different for showing off stretching or filters
			captureosd_rvp.Width = Global.Emulator.VideoProvider.BufferWidth;
			captureosd_rvp.Height = Global.Emulator.VideoProvider.BufferHeight;


			Global.DisplayManager.UpdateSourceEx(Global.Emulator.VideoProvider, captureosd_srp);

			Bitmap ret = (Bitmap)captureosd_rvp.GetBitmap().Clone();

			return ret;
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

			//Hide Status bar icons
			PlayRecordStatus.Visible = false;
			AVIStatusLabel.Visible = false;
			SetPauseStatusbarIcon();
			UpdateCheatStatus();
			SetRebootIconStatus();
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
			int newp;
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
			else newp = 1600;
			SetSpeedPercent(newp);
		}

		private void DecreaseSpeed()
		{
			int oldp = Global.Config.SpeedPercent;
			int newp;
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
			nes.SetSquare1(Global.Config.NESSquare1);
			nes.SetSquare2(Global.Config.NESSquare2);
			nes.SetTriangle(Global.Config.NESTriangle);
			nes.SetNoise(Global.Config.NESNoise);
			nes.SetDMC(Global.Config.NESDMC);
		}

		private void soundChannelsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LoadNesSoundConfig();
		}

		public void ClearSaveRAM()
		{
			//zero says: this is sort of sketchy... but this is no time for rearchitecting
			/*
			string saveRamPath = PathManager.SaveRamPath(Global.Game);
			var file = new FileInfo(saveRamPath);
			if (file.Exists) file.Delete();
			*/
			try
			{
				/*
				var sram = new byte[Global.Emulator.ReadSaveRam.Length];
				if (Global.Emulator is LibsnesCore)
					((LibsnesCore)Global.Emulator).StoreSaveRam(sram);
				else if (Global.Emulator is Gameboy)
					((Gameboy)Global.Emulator).ClearSaveRam();
				else
					Array.Copy(sram, Global.Emulator.ReadSaveRam, Global.Emulator.ReadSaveRam.Length);
				 */
				Global.Emulator.ClearSaveRam();
			}
			catch { }
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

		private void SetRebootIconStatus()
		{
			if (NeedsReboot)
			{
				RebootStatusBarIcon.Visible = true;
			}
			else
			{
				RebootStatusBarIcon.Visible = false;
			}
		}

		public void FlagNeedsReboot()
		{
			NeedsReboot = true;
			SetRebootIconStatus();
			Global.OSD.AddMessage("Core reboot needed for this setting");
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

		private void SaveMovie()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.WriteMovie();
				Global.OSD.AddMessage(Global.MovieSession.Movie.Filename + " saved.");
			}
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

		private void HandleToggleLight()
		{
			if (StatusSlot0.Visible)
			{
				if (Global.Emulator.CoreComm.UsesDriveLed)
				{
					if (!StatusBarLedLight.Visible)
					{
						StatusBarLedLight.Visible = true;
					}
					if (Global.Emulator.CoreComm.DriveLED)
					{
						StatusBarLedLight.Image = Properties.Resources.LightOn;
					}
					else
					{
						StatusBarLedLight.Image = Properties.Resources.LightOff;
					}
				}
				else
				{
					if (StatusBarLedLight.Visible)
					{
						StatusBarLedLight.Visible = false;
					}
				}
			}
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

		private void UpdateKeyPriorityIcon()
		{
			switch (Global.Config.Input_Hotkey_OverrideOptions)
			{
				default:
				case 0:
					KeyPriorityStatusBarLabel.Image = Properties.Resources.Both;
					KeyPriorityStatusBarLabel.ToolTipText = "Key priority: Allow both hotkeys and controller buttons";
					break;
				case 1:
					KeyPriorityStatusBarLabel.Image = Properties.Resources.GameController;
					KeyPriorityStatusBarLabel.ToolTipText = "Key priority: Controller buttons will override hotkeys";
					break;
				case 2:
					KeyPriorityStatusBarLabel.Image = Properties.Resources.HotKeys;
					KeyPriorityStatusBarLabel.ToolTipText = "Key priority: Hotkeys will override controller buttons";
					break;
			}
		}

		private void fullMovieLoadstatesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.VBAStyleMovieLoadState ^= true;
		}

		private void ToggleModePokeMode()
		{
			Global.Config.MoviePlaybackPokeMode ^= true;
			if (Global.Config.MoviePlaybackPokeMode)
			{
				Global.OSD.AddMessage("Movie Poke mode enabled");
			}
			else
			{
				Global.OSD.AddMessage("Movie Poke mode disabled");
			}
		}

		private void toolStripMenuItem6_Click(object sender, EventArgs e)
		{
			StopMovie(true);
		}

		private void stopMovieWithoutSavingToolStripMenuItem_Click(object sender, EventArgs e)
		{
			StopMovie(true);
		}

		public string GetEmuVersion()
		{
			if (INTERIM)
			{
				return "SVN " + SubWCRev.SVN_REV;
			}
			else
			{
				return EMUVERSION;
			}
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
			new N64VideoPluginconfig().ShowDialog();
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
				if (result == System.Windows.Forms.DialogResult.OK)
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
	}
}
