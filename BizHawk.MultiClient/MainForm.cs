using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using BizHawk.Core;
using BizHawk.DiscSystem;
using BizHawk.Emulation;
using BizHawk.Emulation.Computers.Commodore64;
using BizHawk.Emulation.Consoles.Calculator;
using BizHawk.Emulation.Consoles.Coleco;
using BizHawk.Emulation.Consoles.GB;
using BizHawk.Emulation.Consoles.Intellivision;
using BizHawk.Emulation.Consoles.Nintendo;
using BizHawk.Emulation.Consoles.Nintendo.GBA;
using BizHawk.Emulation.Consoles.Nintendo.N64;
using BizHawk.Emulation.Consoles.Nintendo.SNES;
using BizHawk.Emulation.Consoles.Sega;
using BizHawk.Emulation.Consoles.TurboGrafx;

using BizHawk.Common;
using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	public partial class MainForm : Form
	{
		public static bool INTERIM = true;
		public const string EMUVERSION = "Version " + VersionInfo.MAINVERSION;
		public const string RELEASEDATE = "August 22, 2013";
		public string CurrentlyOpenRom;
		public bool PauseAVI = false;
		public bool PressFrameAdvance = false;
		public bool PressRewind = false;
		public bool FastForward = false;
		public bool TurboFastForward = false;
		public bool RestoreReadWriteOnStop = false;
		public bool UpdateFrame = false;
		public bool NeedsReboot = false;

		public bool RewindActive = true;

		private Control renderTarget;
		private RetainedViewportPanel retainedPanel;
		private readonly SaveSlotManager StateSlots = new SaveSlotManager();
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

		private RamSearch _ramsearch;

		private HexEditor _hexeditor;
		private TraceLogger _tracelogger;
		private SNESGraphicsDebugger _snesgraphicsdebugger;
		private NESNameTableViewer _nesnametableview;
		private NESPPU _nesppu;
		private NESDebugger _nesdebugger;
		private GBtools.GBGPUView _gbgpuview;
		private GBAtools.GBAGPUView _gbagpuview;
		private PCEBGViewer _pcebgviewer;
		private Cheats _cheats;
		private ToolBox _toolbox;
		private TI83KeyPad _ti83pad;
		private TAStudio _tastudio;
		private VirtualPadForm _vpad;
		private NESGameGenie _ngg;
		private SNESGameGenie _sgg;
		private GBGameGenie _gbgg;
		private GenGameGenie _gengg;
		private NESSoundConfig _nessound;

		//TODO: this is a lazy way to refactor things, but works for now.  The point is to not have these objects created until needed, without refactoring a lot of code
		public RamSearch RamSearch1 { get { if (_ramsearch == null) _ramsearch = new RamSearch(); return _ramsearch; } set { _ramsearch = value; } }
		public HexEditor HexEditor1 { get { if (_hexeditor == null) _hexeditor = new HexEditor(); return _hexeditor; } set { _hexeditor = value; } }
		public TraceLogger TraceLogger1 { get { if (_tracelogger == null) _tracelogger = new TraceLogger(); return _tracelogger; } set { _tracelogger = value; } }
		public SNESGraphicsDebugger SNESGraphicsDebugger1 { get { if (_snesgraphicsdebugger == null) _snesgraphicsdebugger = new SNESGraphicsDebugger(); return _snesgraphicsdebugger; } set { _snesgraphicsdebugger = value; } }
		public NESNameTableViewer NESNameTableViewer1 { get { return _nesnametableview ?? (_nesnametableview = new NESNameTableViewer()); } set { _nesnametableview = value; } }
		public NESPPU NESPPU1 { get { return _nesppu ?? (_nesppu = new NESPPU()); } set { _nesppu = value; } }
		public NESDebugger NESDebug1 { get { if (_nesdebugger == null) _nesdebugger = new NESDebugger(); return _nesdebugger; } set { _nesdebugger = value; } }
		public GBtools.GBGPUView GBGPUView1 { get { if (_gbgpuview == null) _gbgpuview = new GBtools.GBGPUView(); return _gbgpuview; } set { _gbgpuview = value; } }
		public GBAtools.GBAGPUView GBAGPUView1 { get { if (_gbagpuview == null) _gbagpuview = new GBAtools.GBAGPUView(); return _gbagpuview; } set { _gbagpuview = value; } }
		public PCEBGViewer PCEBGViewer1 { get { if (_pcebgviewer == null) _pcebgviewer = new PCEBGViewer(); return _pcebgviewer; } set { _pcebgviewer = value; } }
		public ToolBox ToolBox1 { get { if (_toolbox == null) _toolbox = new ToolBox(); return _toolbox; } set { _toolbox = value; } }
		public TI83KeyPad TI83KeyPad1 { get { if (_ti83pad == null) _ti83pad = new TI83KeyPad(); return _ti83pad; } set { _ti83pad = value; } }
		public TAStudio TAStudio1 { get { if (_tastudio == null) _tastudio = new TAStudio(); return _tastudio; } set { _tastudio = value; } }
		public VirtualPadForm VirtualPadForm1 { get { if (_vpad == null) _vpad = new VirtualPadForm(); return _vpad; } set { _vpad = value; } }
		public NESGameGenie NESgg { get { if (_ngg == null) _ngg = new NESGameGenie(); return _ngg; } set { _ngg = value; } }
		public SNESGameGenie SNESgg { get { if (_sgg == null) _sgg = new SNESGameGenie(); return _sgg; } set { _sgg = value; } }
		public GBGameGenie GBgg { get { if (_gbgg == null) _gbgg = new GBGameGenie(); return _gbgg; } set { _gbgg = value; } }
		public GenGameGenie Gengg { get { if (_gengg == null) _gengg = new GenGameGenie(); return _gengg; } set { _gengg = value; } }
		public NESSoundConfig NesSound { get { if (_nessound == null) _nessound = new NESSoundConfig(); return _nessound; } set { _nessound = value; } }

		//TODO: eventually start doing this, rather than tools attempting to talk to tools
		public void Cheats_UpdateValues() { if (_cheats != null) { _cheats.UpdateValues(); } }
		public void Cheats_Restart()
		{
			if (_cheats != null) _cheats.Restart();
			else Global.CheatList.NewList(GenerateDefaultCheatFilename());
			ToolHelpers.UpdateCheatRelatedTools();
		}

		public string GenerateDefaultCheatFilename()
		{
			PathEntry pathEntry = Global.Config.PathEntries[Global.Emulator.SystemId, "Cheats"];
			if (pathEntry == null)
			{
				pathEntry = Global.Config.PathEntries[Global.Emulator.SystemId, "Base"];
			}
			string path = PathManager.MakeAbsolutePath(pathEntry.Path, Global.Emulator.SystemId);

			var f = new FileInfo(path);
			if (f.Directory != null && f.Directory.Exists == false)
			{
				f.Directory.Create();
			}

			return Path.Combine(path, PathManager.FilesystemSafeName(Global.Game) + ".cht");
		}

#if WINDOWS
		private LuaConsole _luaconsole = null;
		public LuaConsole LuaConsole1 { get { if (_luaconsole == null) _luaconsole = new LuaConsole(); return _luaconsole; } set { _luaconsole = value; } }
#endif

		/// <summary>
		/// number of frames to autodump
		/// </summary>
		int autoDumpLength;
		bool autoCloseOnDump = false;

		static MainForm()
		{
			//if this isnt here, then our assemblyresolving hacks wont work due to the check for MainForm.INTERIM
			//its.. weird. dont ask.
		}

		public MainForm(string[] args)
		{
			GlobalWinF.MainForm = this;
			Global.FirmwareManager = new FirmwareManager();
			Global.MovieSession = new MovieSession
			{
				Movie = new Movie(GlobalWinF.MainForm.GetEmuVersion()),
				ClearSRAMCallback = ClearSaveRAM,
				MessageCallback = GlobalWinF.OSD.AddMessage,
				AskYesNoCallback = StateErrorAskUser
			};
			MainWait = new AutoResetEvent(false);
			Icon = Properties.Resources.logo;
			InitializeComponent();
			Global.Game = GameInfo.GetNullGame();
			if (Global.Config.ShowLogWindow)
			{
				ShowConsole();
				DisplayLogWindowMenuItem.Checked = true;
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
				Global.CheatList.SaveOnClose();
				CloseGame();
				Global.MovieSession.Movie.Stop();
				GlobalWinF.Tools.Close();
				SaveConfig();
			};

			ResizeBegin += (o, e) =>
			{
				if (GlobalWinF.Sound != null) GlobalWinF.Sound.StopSound();
			};

			ResizeEnd += (o, e) =>
			{
				if (GlobalWinF.RenderPanel != null) GlobalWinF.RenderPanel.Resized = true;
				if (GlobalWinF.Sound != null) GlobalWinF.Sound.StartSound();
			};

			Input.Initialize();
			InitControls();
			Global.CoreComm = new CoreComm();
			CoreFileProvider.SyncCoreCommInputSignals();
			Global.Emulator = new NullEmulator(Global.CoreComm);
			Global.ActiveController = Global.NullControls;
			Global.AutoFireController = Global.AutofireNullControls;
			GlobalWinF.AutofireStickyXORAdapter.SetOnOffPatternFromConfig();
#if WINDOWS
			GlobalWinF.Sound = new Sound(Handle, GlobalWinF.DSound);
#else
			Global.Sound = new Sound();
#endif
			GlobalWinF.Sound.StartSound();
			RewireInputChain();
			GlobalWinF.Tools = new ToolManager();
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
				else if (arg.StartsWith("--dump-close"))
					autoCloseOnDump = true;
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
			else if (Global.Config.RecentRoms.AutoLoad && !Global.Config.RecentRoms.Empty)
			{
				LoadRomFromRecent(Global.Config.RecentRoms[0]);
			}

			if (cmdMovie != null)
			{
				if (Global.Game == null)
				{
					OpenROM();
				}
				else
				{
					Movie m = new Movie(cmdMovie, GlobalWinF.MainForm.GetEmuVersion());
					Global.ReadOnly = true;
					// if user is dumping and didnt supply dump length, make it as long as the loaded movie
					if (autoDumpLength == 0)
					{
						autoDumpLength = m.RawFrames;
					}
					StartNewMovie(m, false);
					Global.Config.RecentMovies.Add(cmdMovie);
				}
			}
			else if (Global.Config.RecentMovies.AutoLoad && !Global.Config.RecentMovies.Empty)
			{
				if (Global.Game == null)
				{
					OpenROM();
				}
				else
				{
					Movie m = new Movie(Global.Config.RecentMovies[0], GlobalWinF.MainForm.GetEmuVersion());
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

			if (Global.Config.RecentWatches.AutoLoad)
			{
				LoadRamWatch(!Global.Config.DisplayRamWatch);
			}
			if (Global.Config.RecentSearches.AutoLoad)
			{
				LoadRamSearch();
			}
			if (Global.Config.AutoLoadHexEditor)
			{
				LoadHexEditor();
			}
			if (Global.Config.RecentCheats.AutoLoad)
			{
				LoadCheatsWindow();
			}
			if (Global.Config.AutoLoadNESPPU && Global.Emulator is NES)
			{
				LoadNESPPU();
			}
			if (Global.Config.AutoLoadNESNameTable && Global.Emulator is NES)
			{
				LoadNESNameTable();
			}
			if (Global.Config.AutoLoadNESDebugger && Global.Emulator is NES)
			{
				LoadNESDebugger();
			}
			if (Global.Config.NESGGAutoload && Global.Emulator is NES)
			{
				LoadGameGenieEC();
			}
			if (Global.Config.AutoLoadGBGPUView && Global.Emulator is Gameboy)
			{
				LoadGBGPUView();
			}
			if (Global.Config.AutoloadTAStudio)
			{
				LoadTAStudio();
			}
			if (Global.Config.AutoloadVirtualPad)
			{
				LoadVirtualPads();
			}
			if (Global.Config.AutoLoadLuaConsole)
			{
				OpenLuaConsole();
			}
			if (Global.Config.PCEBGViewerAutoload && Global.Emulator is PCEngine)
			{
				LoadPCEBGViewer();
			}
			if (Global.Config.AutoLoadSNESGraphicsDebugger && Global.Emulator is LibsnesCore)
			{
				LoadSNESGraphicsDebugger();
			}
			if (Global.Config.TraceLoggerAutoLoad)
			{
				if (Global.CoreComm.CpuTraceAvailable)
				{
					LoadTraceLogger();
				}
			}

			if (Global.Config.MainWndx >= 0 && Global.Config.MainWndy >= 0 && Global.Config.SaveWindowPosition)
			{
				Location = new Point(Global.Config.MainWndx, Global.Config.MainWndy);
			}

			if (Global.Config.DisplayStatusBar == false)
			{
				MainStatusBar.Visible = false;
			}
			else
			{
				DisplayStatusBarMenuItem.Checked = true;
			}

			if (Global.Config.StartPaused)
			{
				PauseEmulator();
			}

			if (!INTERIM)
			{
				NESDebuggerMenuItem.Enabled = false;
				//luaConsoleToolStripMenuItem.Enabled = false;
			}

			// start dumping, if appropriate
			if (cmdDumpType != null && cmdDumpName != null)
			{
				RecordAVI(cmdDumpType, cmdDumpName);
			}

			UpdateStatusSlots();

			renderTarget.Paint += (o, e) =>
			{
				GlobalWinF.DisplayManager.NeedsToPaint = true;
			};
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (GlobalWinF.DisplayManager != null) GlobalWinF.DisplayManager.Dispose();
			GlobalWinF.DisplayManager = null;

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
			var cfp = new CoreFileProvider();
			target.CoreFileProvider = cfp;
			cfp.FirmwareManager = Global.FirmwareManager;

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

			target.PSX_FirmwaresPath = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPath, null); // PathManager.MakeAbsolutePath(Global.Config.PathPSXFirmwares, "PSX");

			target.C64_FirmwaresPath = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPath, null); // PathManager.MakeAbsolutePath(Global.Config.PathC64Firmwares, "C64");

			target.SNES_FirmwaresPath = PathManager.MakeAbsolutePath(Global.Config.PathEntries.FirmwaresPath, null); // PathManager.MakeAbsolutePath(Global.Config.PathSNESFirmwares, "SNES");
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

		void SyncPresentationMode()
		{
			GlobalWinF.DisplayManager.Suspend();

#if WINDOWS
			bool gdi = Global.Config.DisplayGDI || GlobalWinF.Direct3D == null;
#endif
			if (renderTarget != null)
			{
				renderTarget.Dispose();
				Controls.Remove(renderTarget);
			}

			if (retainedPanel != null) retainedPanel.Dispose();
			if (GlobalWinF.RenderPanel != null) GlobalWinF.RenderPanel.Dispose();

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
				GlobalWinF.RenderPanel = new SysdrawingRenderPanel(retainedPanel);
				retainedPanel.ActivateThreaded();
#if WINDOWS
			}
			else
			{
				try
				{
					var d3dPanel = new Direct3DRenderPanel(GlobalWinF.Direct3D, renderTarget);
					d3dPanel.CreateDevice();
					GlobalWinF.RenderPanel = d3dPanel;
				}
				catch
				{
					Program.DisplayDirect3DError();
					GlobalWinF.Direct3D.Dispose();
					GlobalWinF.Direct3D = null;
					SyncPresentationMode();
				}
			}
#endif

			GlobalWinF.DisplayManager.Resume();
		}

		void SyncThrottle()
		{
			bool fastforward = GlobalWinF.ClientControls["Fast Forward"] || FastForward;
			bool superfastforward = GlobalWinF.ClientControls["Turbo"];
			Global.ForceNoThrottle = unthrottled || fastforward;

			// realtime throttle is never going to be so exact that using a double here is wrong
			throttle.SetCoreFps(Global.Emulator.CoreComm.VsyncRate);

			throttle.signal_paused = EmulatorPaused || Global.Emulator is NullEmulator;
			throttle.signal_unthrottle = unthrottled || superfastforward;

			if (fastforward)
			{
				throttle.SetSpeedPercent(Global.Config.SpeedPercentAlternate);
			}
			else
			{
				throttle.SetSpeedPercent(Global.Config.SpeedPercent);
			}
			
		}

		void SetSpeedPercentAlternate(int value)
		{
			Global.Config.SpeedPercentAlternate = value;
			SyncThrottle();
			GlobalWinF.OSD.AddMessage("Alternate Speed: " + value + "%");
		}

		void SetSpeedPercent(int value)
		{
			Global.Config.SpeedPercent = value;
			SyncThrottle();
			GlobalWinF.OSD.AddMessage("Speed: " + value + "%");
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
				GlobalWinF.ClientControls.LatchFromPhysical(GlobalWinF.HotkeyCoalescer);
				Global.ActiveController.LatchFromPhysical(GlobalWinF.ControllerInputCoalescer);

				Global.ActiveController.OR_FromLogical(Global.ClickyVirtualPadController);
				Global.AutoFireController.LatchFromPhysical(GlobalWinF.ControllerInputCoalescer);

				if (GlobalWinF.ClientControls["Autohold"])
				{
					Global.StickyXORAdapter.MassToggleStickyState(Global.ActiveController.PressedButtons);
					GlobalWinF.AutofireStickyXORAdapter.MassToggleStickyState(Global.AutoFireController.PressedButtons);
				}
				else if (GlobalWinF.ClientControls["Autofire"])
				{
					GlobalWinF.AutofireStickyXORAdapter.MassToggleStickyState(Global.ActiveController.PressedButtons);
				}

				//if (!EmulatorPaused)
				//Global.ClickyVirtualPadController.FrameTick();

#if WINDOWS
				LuaConsole1.ResumeScripts(false);
#endif

				StepRunLoop_Core();
				//if(!IsNullEmulator())
				StepRunLoop_Throttle();

				if (GlobalWinF.DisplayManager.NeedsToPaint) { Render(); }

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
				PauseStatusButton.Image = Properties.Resources.Pause;
				PauseStatusButton.Visible = true;
				PauseStatusButton.ToolTipText = "Emulator Paused";
			}
			else
			{
				PauseStatusButton.Image = Properties.Resources.Blank;
				PauseStatusButton.Visible = false;
				PauseStatusButton.ToolTipText = String.Empty;
			}
		}

		public void TogglePause()
		{
			EmulatorPaused ^= true;
			SetPauseStatusbarIcon();
		}

		private void LoadRomFromRecent(string rom)
		{
			if (!LoadRom(rom))
			{
				ToolHelpers.HandleLoadError(Global.Config.RecentRoms, rom);
			}
		}

		private void LoadMoviesFromRecent(string path)
		{
			Movie m = new Movie(path, GetEmuVersion());

			if (!m.Loaded)
			{
				ToolHelpers.HandleLoadError(Global.Config.RecentMovies, path);
			}
			else
			{
				Global.ReadOnly = true;
				StartNewMovie(m, false);
			}
		}

		private void InitControls()
		{
			var controls = new Controller(
				new ControllerDefinition
					{
					Name = "Emulator Frontend Controls",
					BoolButtons = Global.Config.HotkeyBindings.Select(x => x.DisplayName).ToList()
				});

			foreach (var b in Global.Config.HotkeyBindings)
			{
				controls.BindMulti(b.DisplayName, b.Bindings);
			}

			GlobalWinF.ClientControls = controls;
			Global.NullControls = new Controller(NullEmulator.NullController);
			Global.AutofireNullControls = new AutofireController(NullEmulator.NullController);

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

		public bool IsNullEmulator()
		{
			return Global.Emulator is NullEmulator;
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

			TI83SubMenu.Visible = false;
			NESSubMenu.Visible = false;
			PCESubMenu.Visible = false;
			SMSSubMenu.Visible = false;
			GBSubMenu.Visible = false;
			GBASubMenu.Visible = false;
			AtariSubMenu.Visible = false;
			SNESSubMenu.Visible = false;
			ColecoSubMenu.Visible = false;
			N64SubMenu.Visible = false;
			SaturnSubMenu.Visible = false;

			switch (system)
			{
				default:
				case "GEN":
					break;
				case "NULL":
					N64SubMenu.Visible = true;
					break;
				case "TI83":
					TI83SubMenu.Visible = true;
					break;
				case "NES":
					NESSubMenu.Visible = true;
					NESSpecialMenuControls();
					break;
				case "PCE":
				case "PCECD":
				case "SGX":
					PCESubMenu.Visible = true;
					break;
				case "SMS":
					SMSSubMenu.Text = "SMS";
					SMSSubMenu.Visible = true;
					break;
				case "SG":
					SMSSubMenu.Text = "SG";
					SMSSubMenu.Visible = true;
					break;
				case "GG":
					SMSSubMenu.Text = "GG";
					SMSSubMenu.Visible = true;
					break;
				case "GB":
				case "GBC":
					GBSubMenu.Visible = true;
					break;
				case "GBA":
					GBASubMenu.Visible = true;
					break;
				case "A26":
					AtariSubMenu.Visible = true;
					break;
				case "SNES":
				case "SGB":
					if ((Global.Emulator as LibsnesCore).IsSGB)
						SNESSubMenu.Text = "&SGB";
					else
						SNESSubMenu.Text = "&SNES";
					SNESSubMenu.Visible = true;
					break;
				case "Coleco":
					ColecoSubMenu.Visible = true;
					break;
				case "N64":
					N64SubMenu.Visible = true;
					break;
				case "SAT":
					SaturnSubMenu.Visible = true;
					break;
			}
		}

		void NESSpeicalMenuAdd(string name, string button, string msg)
		{
			NESSpecialControlsMenuItem.Visible = true;
			NESSpecialControlsMenuItem.DropDownItems.Add(name, null, delegate
			{
				if (Global.Emulator.ControllerDefinition.BoolButtons.Contains(button))
				{
					if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
					{
						Global.ClickyVirtualPadController.Click(button);
						GlobalWinF.OSD.AddMessage(msg);
					}
				}
			});
		}

		void NESSpecialMenuControls()
		{
			// ugly and hacky
			NESSpecialControlsMenuItem.Visible = false;
			NESSpecialControlsMenuItem.DropDownItems.Clear();
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

		static Controller BindToDefinition(ControllerDefinition def, Dictionary<string, Dictionary<string, string>> allbinds, Dictionary<string, Dictionary<string, Config.AnalogBind>> analogbinds)
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
			Dictionary<string, Config.AnalogBind> abinds;
			if (analogbinds.TryGetValue(def.Name, out abinds))
			{
				foreach (string cbutton in def.FloatControls)
				{
					Config.AnalogBind bind;
					if (abinds.TryGetValue(cbutton, out bind))
					{
						ret.BindFloat(cbutton, bind);
					}
				}
			}
			return ret;
		}

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

			Global.ActiveController = BindToDefinition(def, Global.Config.AllTrollers, Global.Config.AllTrollersAnalog);
			Global.AutoFireController = BindToDefinitionAF(def, Global.Config.AllTrollersAutoFire);

			// allow propogating controls that are in the current controller definition but not in the prebaked one
			// these two lines shouldn't be required anymore under the new system?
			Global.ActiveController.ForceType(new ControllerDefinition(Global.Emulator.ControllerDefinition));
			Global.ClickyVirtualPadController.Type = new ControllerDefinition(Global.Emulator.ControllerDefinition);
			RewireInputChain();
		}

		void RewireInputChain()
		{
			GlobalWinF.ControllerInputCoalescer = new ControllerInputCoalescer { Type = Global.ActiveController.Type };

			GlobalWinF.OrControllerAdapter.Source = Global.ActiveController;
			GlobalWinF.OrControllerAdapter.SourceOr = Global.AutoFireController;
			GlobalWinF.UD_LR_ControllerAdapter.Source = GlobalWinF.OrControllerAdapter;

			Global.StickyXORAdapter.Source = GlobalWinF.UD_LR_ControllerAdapter;
			GlobalWinF.AutofireStickyXORAdapter.Source = Global.StickyXORAdapter;

			Global.MultitrackRewiringControllerAdapter.Source = GlobalWinF.AutofireStickyXORAdapter;
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
									string biosPath = Global.FirmwareManager.Request("SAT", "J");
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
									string biosPath = Global.FirmwareManager.Request("PCECD", "Bios");
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
						try
						{
							var XMLG = XmlGame.Create(file); // if load fails, are we supposed to retry as a bsnes XML????????
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
						catch(Exception ex)
						{
							System.Windows.Forms.MessageBox.Show(ex.ToString(), "XMLGame Load Error");
						}
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

									((CoreFileProvider)nextComm.CoreFileProvider).SubfileDirectory = Path.GetDirectoryName(path.Replace("|", "")); //Dirty hack to get around archive filenames (since we are just getting the directory path, it is safe to mangle the filename

									var snes = new LibsnesCore(nextComm);
									nextEmulator = snes;
									byte[] romData = isXml ? null : rom.FileData;
									byte[] xmlData = isXml ? rom.FileData : null;
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
									//TODO - move into nes core
									string biosPath = nextComm.CoreFileProvider.PathFirmware("NES", "Bios_FDS");
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

									NES nes = new NES(nextComm, game, rom.FileData, bios, Global.MovieSession.Movie.Header.BoardProperties)
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
									string sgbromPath = Global.FirmwareManager.Request("SNES", "Rom_SGB");
									byte[] sgbrom;
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
								string colbiosPath = Global.FirmwareManager.Request("Coleco", "Bios");
								FileInfo colfile = colbiosPath != null ? new FileInfo(colbiosPath) : null;
								if (colfile == null || !colfile.Exists)
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
									string eromPath = Global.FirmwareManager.Request("INTV", "EROM");
									if (!File.Exists(eromPath))
										throw new InvalidOperationException("Specified EROM path does not exist:\n\n" + eromPath);
									intv.LoadExecutiveRom(eromPath);
									string gromPath = Global.FirmwareManager.Request("INTV", "GROM");
									if (!File.Exists(gromPath))
										throw new InvalidOperationException("Specified GROM path does not exist:\n\n" + gromPath);
									intv.LoadGraphicsRom(gromPath);
									nextEmulator = intv;
								}
								break;
							case "A78":
								string ntsc_biospath = Global.FirmwareManager.Request("A78", "Bios_NTSC");
								string pal_biospath = Global.FirmwareManager.Request("A78", "Bios_PAL");
								string hsbiospath = Global.FirmwareManager.Request("A78", "Bios_HSC");

								FileInfo ntscfile = ntsc_biospath != null ? new FileInfo(ntsc_biospath) : null;
								FileInfo palfile = pal_biospath != null ? new FileInfo(pal_biospath) : null;
								FileInfo hsfile = hsbiospath != null ? new FileInfo(hsbiospath) : null;

								byte[] NTSC_BIOS7800 = null;
								byte[] PAL_BIOS7800 = null;
								byte[] HighScoreBIOS = null;
								if (ntscfile == null || !ntscfile.Exists)
								{
									MessageBox.Show("Unable to find the required Atari 7800 BIOS file - \n" + ntsc_biospath + "\nIf the selected game requires it, it may crash", "Unable to load BIOS", MessageBoxButtons.OK, MessageBoxIcon.Error);
									//throw new Exception();
								}
								else
								{
									NTSC_BIOS7800 = File.ReadAllBytes(ntsc_biospath);
								}

								if (palfile == null || !palfile.Exists)
								{
									MessageBox.Show("Unable to find the required Atari 7800 BIOS file - \n" + pal_biospath + "\nIf the selected game requires it, it may crash", "Unable to load BIOS", MessageBoxButtons.OK, MessageBoxIcon.Error);
									//throw new Exception();
								}
								else
								{
									PAL_BIOS7800 = File.ReadAllBytes(pal_biospath);
								}

								if (hsfile == null || !hsfile.Exists)
								{
									MessageBox.Show("Unable to find the required Atari 7800 BIOS file - \n" + hsbiospath + "\nIf the selected game requires it, it may crash", "Unable to load BIOS", MessageBoxButtons.OK, MessageBoxIcon.Error);
									//throw new Exception();
								}
								else
								{
									HighScoreBIOS = File.ReadAllBytes(hsbiospath);
								}

								string gamedbpath = Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "EMU7800.csv");
								try
								{
									var a78 = new Atari7800(nextComm, game, rom.RomData, NTSC_BIOS7800, PAL_BIOS7800, HighScoreBIOS, gamedbpath);
									nextEmulator = a78;
								}
								catch (InvalidDataException ex)
								{
									MessageBox.Show(ex.Message, "Region specific bios missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
									return false;
								}
								break;
							case "C64":
								C64 c64 = new C64(nextComm, game, rom.RomData, rom.Extension);
								c64.HardReset();
								nextEmulator = c64;
								break;
							case "GBA":
								if (INTERIM)
								{
									string gbabiospath = Global.FirmwareManager.Request("GBA", "Bios");
									byte[] gbabios;

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
								Global.Game = game;
								VideoPluginSettings video_settings = N64GenerateVideoSettings(game, hasmovie);
								int SaveType = 0;
								if (game.OptionValue("SaveType") == "EEPROM_16K")
								{
									SaveType = 1;
								}
								nextEmulator = new N64(nextComm, game, rom.RomData, video_settings, SaveType);
								break;
						}
					}

					if (nextEmulator == null)
						throw new Exception("No core could load the rom.");
				}
				catch (Exception ex)
				{
					MessageBox.Show("Exception during loadgame:\n\n" + ex);
					return false;
				}

				if (nextEmulator == null) throw new Exception("No core could load the rom.");

				CloseGame();
				Global.Emulator.Dispose();
				Global.Emulator = nextEmulator;
				Global.CoreComm = nextComm;
				Global.Game = game;
				CoreFileProvider.SyncCoreCommInputSignals();
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

				if (Global.Emulator.BoardName != null)
				{
					Console.WriteLine("Core reported BoardID: \"{0}\"", Global.Emulator.BoardName);
				}

				//restarts the lua console if a different rom is loaded.
				//im not really a fan of how this is done..
				if (Global.Config.RecentRoms.Empty || Global.Config.RecentRoms[0] != file.CanonicalFullPath)
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

				GlobalWinF.Tools.Restart();

				if (_ramsearch != null) RamSearch1.Restart();
				
				if (_hexeditor != null) HexEditor1.Restart();
				if (_nesppu != null) NESPPU1.Restart();
				if (_nesnametableview != null) NESNameTableViewer1.Restart();
				if (_nesdebugger != null) NESDebug1.Restart();
				if (_gbgpuview != null) GBGPUView1.Restart();
				if (_gbagpuview != null) GBAGPUView1.Restart();
				if (_pcebgviewer != null) PCEBGViewer1.Restart();
				if (_ti83pad != null) TI83KeyPad1.Restart();
				if (_tastudio != null) TAStudio1.Restart();
				if (_vpad != null) VirtualPadForm1.Restart();
				Cheats_Restart();
				if (_toolbox != null) ToolBox1.Restart();
				if (_tracelogger != null) TraceLogger1.Restart();

				if (Global.Config.LoadCheatFileByGame)
				{
					if (Global.CheatList.AttemptToLoadCheatFile())
					{
						ToolHelpers.UpdateCheatRelatedTools();
						GlobalWinF.OSD.AddMessage("Cheats file loaded");
					}
				}

				Cheats_UpdateValues();

				CurrentlyOpenRom = file.CanonicalFullPath;
				HandlePlatformMenus();
				StateSlots.Clear();
				UpdateStatusSlots();
				UpdateDumpIcon();

				CaptureRewindState();

				Global.StickyXORAdapter.ClearStickies();
				Global.StickyXORAdapter.ClearStickyFloats();
				GlobalWinF.AutofireStickyXORAdapter.ClearStickies();

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
				GlobalWinF.Sound.SetAsyncInputPin(DumpProxy);
			}
			else if (Global.Config.SoundThrottle)
			{
				// for sound throttle, use sync mode
				Global.Emulator.EndAsyncSound();
				GlobalWinF.Sound.SetSyncInputPin(Global.Emulator.SyncSoundProvider);
			}
			else
			{
				// for vsync\clock throttle modes, use async
				if (!Global.Emulator.StartAsyncSound())
				{
					// if the core doesn't support async mode, use a standard vecna wrapper
					GlobalWinF.Sound.SetAsyncInputPin(new Emulation.Sound.MetaspuAsync(Global.Emulator.SyncSoundProvider, Emulation.Sound.ESynchMethod.ESynchMethod_V));
				}
				else
				{
					GlobalWinF.Sound.SetAsyncInputPin(Global.Emulator.SoundProvider);
				}
			}
		}

		private void UpdateDumpIcon()
		{
			DumpStatusButton.Image = Properties.Resources.Blank;
			DumpStatusButton.ToolTipText = "";

			if (Global.Emulator == null) return;
			if (Global.Game == null) return;

			var status = Global.Game.Status;
			string annotation;
			if (status == RomStatus.BadDump)
			{
				DumpStatusButton.Image = Properties.Resources.ExclamationRed;
				annotation = "Warning: Bad ROM Dump";
			}
			else if (status == RomStatus.Overdump)
			{
				DumpStatusButton.Image = Properties.Resources.ExclamationRed;
				annotation = "Warning: Overdump";
			}
			else if (status == RomStatus.NotInDatabase)
			{
				DumpStatusButton.Image = Properties.Resources.RetroQuestion;
				annotation = "Warning: Unknown ROM";
			}
			else if (status == RomStatus.TranslatedRom)
			{
				DumpStatusButton.Image = Properties.Resources.Translation;
				annotation = "Translated ROM";
			}
			else if (status == RomStatus.Homebrew)
			{
				DumpStatusButton.Image = Properties.Resources.HomeBrew;
				annotation = "Homebrew ROM";
			}
			else if (Global.Game.Status == RomStatus.Hack)
			{
				DumpStatusButton.Image = Properties.Resources.Hack;
				annotation = "Hacked ROM";
			}
			else if (Global.Game.Status == RomStatus.Unknown)
			{
				DumpStatusButton.Image = Properties.Resources.Hack;
				annotation = "Warning: ROM of Unknown Character";
			}
			else
			{
				DumpStatusButton.Image = Properties.Resources.GreenCheck;
				annotation = "Verified good dump";
			}
			if (!string.IsNullOrEmpty(Global.Emulator.CoreComm.RomStatusAnnotation))
				annotation = Global.Emulator.CoreComm.RomStatusAnnotation;

			DumpStatusButton.ToolTipText = annotation;
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

		void SelectSlot(int num)
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
				//if (ActiveForm is HotkeyWindow) return true;
				if (ActiveForm is HotkeyConfig) return true;
				//if (ActiveForm is ControllerConfig) return true;
				if (ActiveForm is ControllerConfig) return true;
				if (ActiveForm is TAStudio) return true;
				if (ActiveForm is VirtualPadForm) return true;
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
				var triggers = GlobalWinF.ClientControls.SearchBindings(ie.LogicalButton.ToString());
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
						GlobalWinF.ControllerInputCoalescer.Receive(ie);

						handled = false;
						if (ie.EventType == Input.InputEventType.Press)
						{
							handled = triggers.Aggregate(handled, (current, trigger) => current | CheckHotkey(trigger));
						}

						//hotkeys which arent handled as actions get coalesced as pollable virtual client buttons
						if (!handled)
						{
							GlobalWinF.HotkeyCoalescer.Receive(ie);
						}
						break;
					case 1: //Input overrides Hokeys
						GlobalWinF.ControllerInputCoalescer.Receive(ie);
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
								GlobalWinF.HotkeyCoalescer.Receive(ie);
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
							GlobalWinF.HotkeyCoalescer.Receive(ie);
							GlobalWinF.ControllerInputCoalescer.Receive(ie);
						}
						break;
				}

			} //foreach event

			// also handle floats
			GlobalWinF.ControllerInputCoalescer.AcceptNewFloats(Input.Instance.GetFloats());
		}

		private void ClearAutohold()
		{
			Global.StickyXORAdapter.ClearStickies();
			GlobalWinF.AutofireStickyXORAdapter.ClearStickies();
			VirtualPadForm1.ClearVirtualPadHolds();
			GlobalWinF.OSD.AddMessage("Autohold keys cleared");
		}

		bool CheckHotkey(string trigger)
		{
			//todo - could have these in a table somehow ?
			switch (trigger)
			{
				default:
					return false;
				case "Pause": TogglePause(); break;
				case "Toggle Throttle":
					unthrottled ^= true;
					GlobalWinF.OSD.AddMessage("Unthrottled: " + unthrottled);
					break;
				case "Soft Reset": SoftReset(); break;
				case "Hard Reset": HardReset(); break;
				case "Quick Load":
					if (!IsNullEmulator())
						LoadState("QuickSave" + Global.Config.SaveSlot.ToString());
					break;
				case "Quick Save":
					if (!IsNullEmulator())
						SaveState("QuickSave" + Global.Config.SaveSlot.ToString());
					break;
				case "Clear Autohold": ClearAutohold(); break;
				case "Screenshot": TakeScreenshot(); break;
				case "Full Screen": ToggleFullscreen(); break;
				case "Open ROM": OpenROM(); break;
				case "Close ROM": CloseROM(); break;
				case "Display FPS": ToggleFPS(); break;
				case "Frame Counter": ToggleFrameCounter(); break;
				case "Lag Counter": ToggleLagCounter(); break;
				case "Input Display": ToggleInputDisplay(); break;
				case "Toggle BG Input": ToggleBackgroundInput(); break;
				case "Toggle Menu": ShowHideMenu(); break;
				case "Volume Up": VolumeUp(); break;
				case "Volume Down": VolumeDown(); break;
				case "Record A/V": RecordAVI(); break;
				case "Stop A/V": StopAVI(); break;
				case "Larger Window": IncreaseWindowSize(); break;
				case "Smaller Window": DecreaseWIndowSize(); break;
				case "Increase Speed": IncreaseSpeed(); break;
				case "Decrease Speed": DecreaseSpeed(); break;
				case "Reboot Core":
					bool autoSaveState = Global.Config.AutoSavestates;
					Global.Config.AutoSavestates = false;
					LoadRom(CurrentlyOpenRom);
					Global.Config.AutoSavestates = autoSaveState;
					break;

				case "Save State 0": SaveState("QuickSave0"); break;
				case "Save State 1": SaveState("QuickSave1"); break;
				case "Save State 2": SaveState("QuickSave2"); break;
				case "Save State 3": SaveState("QuickSave3"); break;
				case "Save State 4": SaveState("QuickSave4"); break;
				case "Save State 5": SaveState("QuickSave5"); break;
				case "Save State 6": SaveState("QuickSave6"); break;
				case "Save State 7": SaveState("QuickSave7"); break;
				case "Save State 8": SaveState("QuickSave8"); break;
				case "Save State 9": SaveState("QuickSave9"); break;
				case "Load State 0": LoadState("QuickSave0"); break;
				case "Load State 1": LoadState("QuickSave1"); break;
				case "Load State 2": LoadState("QuickSave2"); break;
				case "Load State 3": LoadState("QuickSave3"); break;
				case "Load State 4": LoadState("QuickSave4"); break;
				case "Load State 5": LoadState("QuickSave5"); break;
				case "Load State 6": LoadState("QuickSave6"); break;
				case "Load State 7": LoadState("QuickSave7"); break;
				case "Load State 8": LoadState("QuickSave8"); break;
				case "Load State 9": LoadState("QuickSave9"); break;
				case "Select State 0": SelectSlot(0); break;
				case "Select State 1": SelectSlot(1); break;
				case "Select State 2": SelectSlot(2); break;
				case "Select State 3": SelectSlot(3); break;
				case "Select State 4": SelectSlot(4); break;
				case "Select State 5": SelectSlot(5); break;
				case "Select State 6": SelectSlot(6); break;
				case "Select State 7": SelectSlot(7); break;
				case "Select State 8": SelectSlot(8); break;
				case "Select State 9": SelectSlot(9); break;
				case "Save Named State": SaveStateAs(); break;
				case "Load Named State": LoadStateAs(); break;
				case "Previous Slot": PreviousSlot(); break;
				case "Next Slot": NextSlot(); break;


				case "Toggle read-only": ToggleReadOnly(); break;
				case "Play Movie": LoadPlayMovieDialog(); break;
				case "Record Movie": LoadRecordMovieDialog(); break;
				case "Stop Movie": StopMovie(); break;
				case "Play from beginning": RestartMovie(); break;
				case "Save Movie": SaveMovie(); break;
				case "Toggle MultiTrack":
					if (Global.MovieSession.Movie.IsActive)
					{

						if (Global.Config.VBAStyleMovieLoadState)
						{
							GlobalWinF.OSD.AddMessage("Multi-track can not be used in Full Movie Loadstates mode");
						}
						else
						{
							Global.MovieSession.MultiTrack.IsActive = !Global.MovieSession.MultiTrack.IsActive;
							if (Global.MovieSession.MultiTrack.IsActive)
							{
								GlobalWinF.OSD.AddMessage("MultiTrack Enabled");
								GlobalWinF.OSD.MT = "Recording None";
							}
							else
							{
								GlobalWinF.OSD.AddMessage("MultiTrack Disabled");
							}
							Global.MovieSession.MultiTrack.RecordAll = false;
							Global.MovieSession.MultiTrack.CurrentPlayer = 0;
						}
					}
					else
					{
						GlobalWinF.OSD.AddMessage("MultiTrack cannot be enabled while not recording.");
					}
					GlobalWinF.DisplayManager.NeedsToPaint = true;
					break;
				case "MT Select All":
					Global.MovieSession.MultiTrack.CurrentPlayer = 0;
					Global.MovieSession.MultiTrack.RecordAll = true;
					GlobalWinF.OSD.MT = "Recording All";
					GlobalWinF.DisplayManager.NeedsToPaint = true;
					break;
				case "MT Select None":
					Global.MovieSession.MultiTrack.CurrentPlayer = 0;
					Global.MovieSession.MultiTrack.RecordAll = false;
					GlobalWinF.OSD.MT = "Recording None";
					GlobalWinF.DisplayManager.NeedsToPaint = true;
					break;
				case "MT Increment Player":
					Global.MovieSession.MultiTrack.CurrentPlayer++;
					Global.MovieSession.MultiTrack.RecordAll = false;
					if (Global.MovieSession.MultiTrack.CurrentPlayer > 5) //TODO: Replace with console's maximum or current maximum players??!
					{
						Global.MovieSession.MultiTrack.CurrentPlayer = 1;
					}
					GlobalWinF.OSD.MT = "Recording Player " + Global.MovieSession.MultiTrack.CurrentPlayer.ToString();
					GlobalWinF.DisplayManager.NeedsToPaint = true;
					break;
				case "MT Decrement Player":
					Global.MovieSession.MultiTrack.CurrentPlayer--;
					Global.MovieSession.MultiTrack.RecordAll = false;
					if (Global.MovieSession.MultiTrack.CurrentPlayer < 1)
					{
						Global.MovieSession.MultiTrack.CurrentPlayer = 5;//TODO: Replace with console's maximum or current maximum players??!
					}
					GlobalWinF.OSD.MT = "Recording Player " + Global.MovieSession.MultiTrack.CurrentPlayer.ToString();
					GlobalWinF.DisplayManager.NeedsToPaint = true;
					break;
				case "Movie Poke": ToggleModePokeMode(); break;

				case "Ram Watch": LoadRamWatch(true); break;
				case "Ram Search": LoadRamSearch(); break;
				case "Hex Editor": LoadHexEditor(); break;
				case "Trace Logger": LoadTraceLogger(); break;
				case "Lua Console": OpenLuaConsole(); break;
				case "Cheats": LoadCheatsWindow(); break;
				case "TAStudio": LoadTAStudio(); break;
				case "ToolBox": LoadToolBox(); break;
				case "Virtual Pad": LoadVirtualPads(); break;

				case "Do Search": RamSearch_DoSearch(); break;
				case "New Search": RamSearch_NewSearch(); break;
				case "Previous Compare To": RamSearch_PreviousCompareTo(); break;
				case "Next Compare To": RamSearch_NextCompareTo(); break;
				case "Previous Operator": RamSearch_PreviousOperator(); break;
				case "Next Operator": RamSearch_NextOperator(); break;

				case "Toggle BG 1": SNES_ToggleBG1(); break;
				case "Toggle BG 2": SNES_ToggleBG2(); break;
				case "Toggle BG 3": SNES_ToggleBG3(); break;
				case "Toggle BG 4": SNES_ToggleBG4(); break;
				case "Toggle OBJ 1": SNES_ToggleOBJ1(); break;
				case "Toggle OBJ 2": SNES_ToggleOBJ2(); break;
				case "Toggle OBJ 3": SNES_ToggleOBJ3(); break;
				case "Toggle OBJ 4": SNES_ToggleOBJ4(); break;


				case "Y Up Small": VirtualPadForm1.BumpAnalogValue(null, Global.Config.Analog_SmallChange); break;
				case "Y Up Large": VirtualPadForm1.BumpAnalogValue(null, Global.Config.Analog_LargeChange); break;
				case "Y Down Small": VirtualPadForm1.BumpAnalogValue(null, -(Global.Config.Analog_SmallChange)); break;
				case "Y Down Large": VirtualPadForm1.BumpAnalogValue(null, -(Global.Config.Analog_LargeChange)); break;

				case "X Up Small": VirtualPadForm1.BumpAnalogValue(Global.Config.Analog_SmallChange, null); break;
				case "X Up Large": VirtualPadForm1.BumpAnalogValue(Global.Config.Analog_LargeChange, null); break;
				case "X Down Small": VirtualPadForm1.BumpAnalogValue(-(Global.Config.Analog_SmallChange), null); break;
				case "X Down Large": VirtualPadForm1.BumpAnalogValue(-(Global.Config.Analog_LargeChange), null); break;
			}

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
				runFrame = true;
			}

			if (GlobalWinF.ClientControls["Frame Advance"] || PressFrameAdvance)
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
			if (RewindActive && (GlobalWinF.ClientControls["Rewind"] || PressRewind))
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
				bool ff = GlobalWinF.ClientControls["Fast Forward"] || GlobalWinF.ClientControls["Turbo"];
				bool fff = GlobalWinF.ClientControls["Turbo"];
				bool updateFpsString = (runloop_last_ff != ff);
				runloop_last_ff = ff;

				if (!fff)
				{
					UpdateToolsBefore();
				}

				Global.ClickyVirtualPadController.FrameTick();

				runloop_fps++;
				//client input-related duties
				GlobalWinF.OSD.ClearGUIText();

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
					GlobalWinF.OSD.FPS = fps_string;
				}

				if (!suppressCaptureRewind && RewindActive) CaptureRewindState();

				if (!runloop_frameadvance) genSound = true;
				else if (!Global.Config.MuteFrameAdvance)
					genSound = true;

				Global.MovieSession.HandleMovieOnFrameLoop(GlobalWinF.ClientControls["ClearFrame"]);

				coreskipaudio = GlobalWinF.ClientControls["Turbo"] && CurrAviWriter == null;
				//=======================================
				Global.CheatList.Pulse();
				Global.Emulator.FrameAdvance(!throttle.skipnextframe || CurrAviWriter != null, !coreskipaudio);
				GlobalWinF.DisplayManager.NeedsToPaint = true;
				Global.CheatList.Pulse();
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

			if (GlobalWinF.ClientControls["Rewind"] || PressRewind)
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
				GlobalWinF.Sound.UpdateSound();
			}
			else
				GlobalWinF.Sound.UpdateSilence();
		}

		/// <summary>
		/// Update all tools that are frame dependent like Ram Search before processing
		/// </summary>
		public void UpdateToolsBefore(bool fromLua = false)
		{
#if WINDOWS
			if (_luaconsole != null)
			{
				if (!fromLua) LuaConsole1.StartLuaDrawing();
				LuaConsole1.LuaImp.CallFrameBeforeEvent();
			}
#endif

			GlobalWinF.Tools.UpdateBefore();

			if (_nesnametableview != null) NESNameTableViewer1.UpdateValues();
			if (_nesppu != null) NESPPU1.UpdateValues();
			if (_pcebgviewer != null) PCEBGViewer1.UpdateValues();
			if (_gbgpuview != null) GBGPUView1.UpdateValues();
			if (_gbagpuview != null) GBAGPUView1.UpdateValues();
		}

		public void UpdateToolsLoadstate()
		{
			if (_snesgraphicsdebugger != null) SNESGraphicsDebugger1.UpdateToolsLoadstate();
		}

		/// <summary>
		/// Update all tools that are frame dependent like Ram Search after processing
		/// </summary>
		public void UpdateToolsAfter(bool fromLua = false)
		{
#if WINDOWS
			if (_luaconsole != null && !fromLua)
			{
				LuaConsole1.ResumeScripts(true);
			}

#endif
			GlobalWinF.Tools.UpdateAfter();
			if (_ramsearch != null) RamSearch1.UpdateValues();
			if (_hexeditor != null) HexEditor1.UpdateValues();
			//The other tool updates are earlier, TAStudio needs to be later so it can display the latest
			//frame of execution in its list view.

			if (_tastudio != null) TAStudio1.UpdateValues();
			if (_vpad != null) VirtualPadForm1.UpdateValues();
			if (_snesgraphicsdebugger != null) SNESGraphicsDebugger1.UpdateToolsAfter();
			if (_tracelogger != null) TraceLogger1.UpdateValues();
			HandleToggleLight();
#if WINDOWS
			if (_luaconsole != null)
			{
				LuaConsole1.LuaImp.CallFrameAfterEvent();
				if (!fromLua)
				{
					GlobalWinF.DisplayManager.PreFrameUpdateLuaSource();
					LuaConsole1.EndLuaDrawing();
				}
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

					// make opaque
					col |= unchecked((int)0xff000000);

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
			GlobalWinF.OSD.AddMessage("Screenshot saved to clipboard.");
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
			GlobalWinF.OSD.AddMessage(fi.Name + " saved.");
		}

		public void SaveState(string name)
		{
			if (IsNullEmulator())
			{
				return;
			}

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
			LuaConsole1.LuaImp.CallSaveStateEvent(name);
		}

		public void SaveStateFile(string filename, string name, bool fromLua)
		{
			SavestateManager.SaveStateFile(filename, name);

			GlobalWinF.OSD.AddMessage("Saved state: " + name);

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
			if (file.Directory != null && file.Directory.Exists == false)
				file.Directory.Create();

			GlobalWinF.Sound.StopSound();
			var result = sfd.ShowDialog();
			GlobalWinF.Sound.StartSound();

			if (result != DialogResult.OK)
				return;

			SaveStateFile(sfd.FileName, sfd.FileName, false);
		}

		public void LoadStateFile(string path, string name, bool fromLua = false)
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;

			if (SavestateManager.LoadStateFile(path, name))
			{
				SetMainformMovieInfo();
				GlobalWinF.OSD.ClearGUIText();
				UpdateToolsBefore(fromLua);
				UpdateToolsAfter(fromLua);
				UpdateToolsLoadstate();
				GlobalWinF.OSD.AddMessage("Loaded state: " + name);
				LuaConsole1.LuaImp.CallLoadStateEvent(name);
			}
			else
			{
				GlobalWinF.OSD.AddMessage("Loadstate error!");
			}
		}

		public void LoadState(string name, bool fromLua = false)
		{
			if (IsNullEmulator())
			{
				return;
			}

			string path = PathManager.SaveStatePrefix(Global.Game) + "." + name + ".State";
			if (File.Exists(path) == false)
			{
				GlobalWinF.OSD.AddMessage("Unable to load " + name + ".State");
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

			GlobalWinF.Sound.StopSound();
			var result = ofd.ShowDialog();
			GlobalWinF.Sound.StartSound();

			if (result != DialogResult.OK)
				return;

			if (File.Exists(ofd.FileName) == false)
				return;

			LoadStateFile(ofd.FileName, Path.GetFileName(ofd.FileName));
		}

		private void SaveSlotSelectedMessage()
		{
			GlobalWinF.OSD.AddMessage("Slot " + Global.Config.SaveSlot + " selected.");
		}

		public void LoadRamSearch()
		{
			if (!RamSearch1.IsHandleCreated || RamSearch1.IsDisposed)
			{
				RamSearch1 = new RamSearch();
				RamSearch1.Show();
			}
			else
			{
				RamSearch1.Focus();
			}
		}

		private void RamSearch_DoSearch()
		{
			if (!RamSearch1.IsHandleCreated || RamSearch1.IsDisposed)
			{
				return;
			}
			else
			{
				RamSearch1.DoSearch();
			}
		}

		private void RamSearch_NewSearch()
		{
			if (!RamSearch1.IsHandleCreated || RamSearch1.IsDisposed)
			{
				return;
			}
			else
			{
				RamSearch1.NewSearch();
			}
		}

		private void RamSearch_NextCompareTo()
		{
			if (!RamSearch1.IsHandleCreated || RamSearch1.IsDisposed)
			{
				return;
			}
			else
			{
				RamSearch1.NextCompareTo();
			}
		}

		private void RamSearch_PreviousCompareTo()
		{
			if (!RamSearch1.IsHandleCreated || RamSearch1.IsDisposed)
			{
				return;
			}
			else
			{
				RamSearch1.NextCompareTo(reverse: true);
			}
		}

		private void RamSearch_NextOperator()
		{
			if (!RamSearch1.IsHandleCreated || RamSearch1.IsDisposed)
			{
				return;
			}
			else
			{
				RamSearch1.NextOperator();
			}
		}

		private void RamSearch_PreviousOperator()
		{
			if (!RamSearch1.IsHandleCreated || RamSearch1.IsDisposed)
			{
				return;
			}
			else
			{
				RamSearch1.NextOperator(reverse: true);
			}
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
			else if ((Global.Emulator.SystemId == "GB") || (Global.Game.System == "GG"))
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
			if (Global.Emulator.CoreComm.CpuTraceAvailable)
			{
				if (!TraceLogger1.IsHandleCreated || TraceLogger1.IsDisposed)
				{
					TraceLogger1 = new TraceLogger();
					TraceLogger1.Show();
				}
				else
				{
					TraceLogger1.Focus();
				}
			}
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
			if (_cheats == null)
			{
				_cheats = new Cheats();
			}

			if (!_cheats.IsHandleCreated || _cheats.IsDisposed)
			{
				_cheats = new Cheats();
				_cheats.Show();
			}
			else
			{
				_cheats.Focus();
			}
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
			else if (PluginToUse == "Glide64mk2")
			{
				Global.Config.Glide64mk2Plugin.FillPerGameHacks(game);
				video_settings.Parameters = Global.Config.Glide64mk2Plugin.GetPluginSettings();
			}

			if (hasmovie && Global.MovieSession.Movie.Header.HeaderParams[MovieHeader.PLATFORM] == "N64" && Global.MovieSession.Movie.Header.HeaderParams.ContainsKey(MovieHeader.VIDEOPLUGIN))
			{
				List<string> settings = new List<string>(video_settings.Parameters.Keys);
				foreach (string setting in settings)
				{
					if (Global.MovieSession.Movie.Header.HeaderParams.ContainsKey(setting))
					{
						string Value = Global.MovieSession.Movie.Header.HeaderParams[setting];
						if (video_settings.Parameters[setting] is bool)
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
						else if (video_settings.Parameters[setting] is int)
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

			GlobalWinF.DisplayManager.UpdateSource(Global.Emulator.VideoProvider);
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
				GlobalWinF.RenderPanel.Resized = true;

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
				MainStatusBar.Visible = false;
				PerformLayout();
				GlobalWinF.RenderPanel.Resized = true;
				InFullscreen = true;
			}
			else
			{
				FormBorderStyle = FormBorderStyle.Sizable;
				WindowState = FormWindowState.Normal;
				MainMenuStrip.Visible = true;
				MainStatusBar.Visible = Global.Config.DisplayStatusBar;
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
			Message m = new Message { WParam = new IntPtr(0xF100), LParam = new IntPtr(lparam), Msg = 0x0112, HWnd = Handle };
			base.WndProc(ref m);
		}

		//sends an alt+mnemonic combination
		void SendAltKeyChar(char c)
		{
			typeof(ToolStrip).InvokeMember("ProcessMnemonicInternal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Instance, null, MainformMenu, new object[] { c });
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
			var ofd = new OpenFileDialog { InitialDirectory = PathManager.GetRomsPath(Global.Emulator.SystemId) };
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
					"Rom Files", "*.nes;*.fds;*.sms;*.gg;*.sg;*.gb;*.gbc;*.pce;*.sgx;*.bin;*.smd;*.gen;*.md;*.smc;*.sfc;*.a26;*.a78;*.col;*.rom;*.cue;*.sgb;*.z64;*.v64;*.n64;*.xml;%ARCH%",
					"Disc Images", "*.cue",
					"NES", "*.nes;*.fds;%ARCH%",
					"Super NES", "*.smc;*.sfc;*.xml;%ARCH%",
					"Nintendo 64", "*.z64;*.v64;*.n64",
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

			GlobalWinF.Sound.StopSound();
			var result = ofd.ShowDialog();
			GlobalWinF.Sound.StartSound();
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

		private void CloseGame(bool clearSRAM = false)
		{
			if (Global.Config.AutoSavestates && Global.Emulator is NullEmulator == false)
			{
				SaveState("Auto");
			}

			if (clearSRAM)
			{
				string path = PathManager.SaveRamPath(Global.Game);
				if (File.Exists(path))
				{
					File.Delete(path);
					GlobalWinF.OSD.AddMessage("SRAM cleared.");
				}
			}
			else if (Global.Emulator.SaveRamModified)
			{
				SaveRam();
			}

			StopAVI();
			Global.Emulator.Dispose();
			Global.CoreComm = new CoreComm();
			CoreFileProvider.SyncCoreCommInputSignals();
			Global.Emulator = new NullEmulator(Global.CoreComm);
			Global.ActiveController = Global.NullControls;
			Global.AutoFireController = Global.AutofireNullControls;
			Global.MovieSession.Movie.Stop();
			NeedsReboot = false;
			SetRebootIconStatus();
		}

		public void CloseROM(bool clearSRAM = false)
		{
			CloseGame(clearSRAM);
			Global.CoreComm = new CoreComm();
			CoreFileProvider.SyncCoreCommInputSignals();
			Global.Emulator = new NullEmulator(Global.CoreComm);
			Global.Game = GameInfo.GetNullGame();

			GlobalWinF.Tools.Restart();

			RewireSound();
			ResetRewindBuffer();
			RamSearch1.Restart();
			HexEditor1.Restart();
			NESPPU1.Restart();
			NESNameTableViewer1.Restart();
			NESDebug1.Restart();
			GBGPUView1.Restart();
			GBAGPUView1.Restart();
			PCEBGViewer1.Restart();
			TI83KeyPad1.Restart();
			Cheats_Restart();
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
			CloseForm(RamSearch1);
			CloseForm(HexEditor1);
			CloseForm(NESNameTableViewer1);
			CloseForm(NESPPU1);
			CloseForm(NESDebug1);
			CloseForm(GBGPUView1);
			CloseForm(GBAGPUView1);
			CloseForm(PCEBGViewer1);
			CloseForm(_cheats);
			CloseForm(TI83KeyPad1);
			CloseForm(TAStudio1); Global.MovieSession.EditorMode = false;
			CloseForm(TraceLogger1);
			CloseForm(VirtualPadForm1);
#if WINDOWS
			CloseForm(LuaConsole1);
#endif
		}

		private void CloseForm(Form form)
		{
			if (form != null && form.IsHandleCreated) form.Close();
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
				Global.ReadOnly ^= true;
				if (Global.ReadOnly)
				{
					GlobalWinF.OSD.AddMessage("Movie read-only mode");
				}
				else
				{
					GlobalWinF.OSD.AddMessage("Movie read+write mode");
				}
			}
			else
			{
				GlobalWinF.OSD.AddMessage("No movie active");
			}

		}

		public void LoadTAStudio()
		{
			if (!TAStudio1.IsHandleCreated || TAStudio1.IsDisposed)
			{
				TAStudio1 = new TAStudio();
				Global.MovieSession.EditorMode = true;
				TAStudio1.Show();
			}
			else
			{
				TAStudio1.Focus();
			}
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
			GlobalWinF.Sound.ChangeVolume(Global.Config.SoundVolume);
			GlobalWinF.OSD.AddMessage("Volume " + Global.Config.SoundVolume.ToString());
		}

		private void VolumeDown()
		{
			Global.Config.SoundVolume -= 10;
			if (Global.Config.SoundVolume < 0)
				Global.Config.SoundVolume = 0;
			GlobalWinF.Sound.ChangeVolume(Global.Config.SoundVolume);
			GlobalWinF.OSD.AddMessage("Volume " + Global.Config.SoundVolume.ToString());
		}

		private void SoftReset()
		{
			//is it enough to run this for one frame? maybe..
			if (Global.Emulator.ControllerDefinition.BoolButtons.Contains("Reset"))
			{
				if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
				{
					Global.ClickyVirtualPadController.Click("Reset");
					GlobalWinF.OSD.AddMessage("Reset button pressed.");
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
					GlobalWinF.OSD.AddMessage("Power button pressed.");
				}
			}
		}

		public void UpdateStatusSlots()
		{
			StateSlots.Update();

			if (StateSlots.HasSlot(1))
			{
				Slot1StatusButton.ForeColor = Color.Black;
			}
			else
			{
				Slot1StatusButton.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(2))
			{
				Slot2StatusButton.ForeColor = Color.Black;
			}
			else
			{
				Slot2StatusButton.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(3))
			{
				Slot3StatusButton.ForeColor = Color.Black;
			}
			else
			{
				Slot3StatusButton.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(3))
			{
				Slot3StatusButton.ForeColor = Color.Black;
			}
			else
			{
				Slot3StatusButton.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(4))
			{
				Slot4StatusButton.ForeColor = Color.Black;
			}
			else
			{
				Slot4StatusButton.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(5))
			{
				Slot5StatusButton.ForeColor = Color.Black;
			}
			else
			{
				Slot5StatusButton.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(6))
			{
				Slot6StatusButton.ForeColor = Color.Black;
			}
			else
			{
				Slot6StatusButton.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(7))
			{
				Slot7StatusButton.ForeColor = Color.Black;
			}
			else
			{
				Slot7StatusButton.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(8))
			{
				Slot8StatusButton.ForeColor = Color.Black;
			}
			else
			{
				Slot8StatusButton.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(9))
			{
				Slot9StatusButton.ForeColor = Color.Black;
			}
			else
			{
				Slot9StatusButton.ForeColor = Color.Gray;
			}

			if (StateSlots.HasSlot(0))
			{
				MainStatusBar.ForeColor = Color.Black;
			}
			else
			{
				MainStatusBar.ForeColor = Color.Gray;
			}

			Slot1StatusButton.BackColor = SystemColors.Control;
			Slot2StatusButton.BackColor = SystemColors.Control;
			Slot3StatusButton.BackColor = SystemColors.Control;
			Slot4StatusButton.BackColor = SystemColors.Control;
			Slot5StatusButton.BackColor = SystemColors.Control;
			Slot6StatusButton.BackColor = SystemColors.Control;
			Slot7StatusButton.BackColor = SystemColors.Control;
			Slot8StatusButton.BackColor = SystemColors.Control;
			Slot9StatusButton.BackColor = SystemColors.Control;
			Slot0StatusButton.BackColor = SystemColors.Control;

			if (Global.Config.SaveSlot == 0) Slot0StatusButton.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 1) Slot1StatusButton.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 2) Slot2StatusButton.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 3) Slot3StatusButton.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 4) Slot4StatusButton.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 5) Slot5StatusButton.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 6) Slot6StatusButton.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 7) Slot7StatusButton.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 8) Slot8StatusButton.BackColor = SystemColors.ControlDark;
			if (Global.Config.SaveSlot == 9) Slot9StatusButton.BackColor = SystemColors.ControlDark;
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
				aw = VideoWriterChooserForm.DoVideoWriterChoserDlg(video_writers, GlobalWinF.MainForm, out avwriter_resizew, out avwriter_resizeh);
			}

			foreach (var w in video_writers)
			{
				if (w != aw)
					w.Dispose();
			}

			if (aw == null)
			{
				if (unattended)
					GlobalWinF.OSD.AddMessage(string.Format("Couldn't start video writer \"{0}\"", videowritername));
				else
					GlobalWinF.OSD.AddMessage("A/V capture canceled.");
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
					var token = aw.AcquireVideoCodecToken(GlobalWinF.MainForm);
					if (token == null)
					{
						GlobalWinF.OSD.AddMessage("A/V capture canceled.");
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
						sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.AVPath, null);
					}
					else
					{
						sfd.FileName = "NULL";
						sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.AVPath, null);
					}
					sfd.Filter = String.Format("{0} (*.{0})|*.{0}|All Files|*.*", aw.DesiredExtension());

					GlobalWinF.Sound.StopSound();
					var result = sfd.ShowDialog();
					GlobalWinF.Sound.StartSound();

					if (result == DialogResult.Cancel)
					{
						aw.Dispose();
						return;
					}
					aw.OpenFile(sfd.FileName);
				}

				//commit the avi writing last, in case there were any errors earlier
				CurrAviWriter = aw;
				GlobalWinF.OSD.AddMessage("A/V capture started");
				AVIStatusLabel.Image = Properties.Resources.AVI;
				AVIStatusLabel.ToolTipText = "A/V capture in progress";
				AVIStatusLabel.Visible = true;

			}
			catch
			{
				GlobalWinF.OSD.AddMessage("A/V capture failed!");
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
			GlobalWinF.OSD.AddMessage("A/V capture aborted");
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
			GlobalWinF.OSD.AddMessage("A/V capture stopped");
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
					{
						StopAVI();
						if (autoCloseOnDump)
						{
							exit = true;
						}
					}
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

		void ProcessMovieImport(string fn)
		{
			string d = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPath, null);
			string errorMsg;
			string warningMsg;
			Movie m = MovieImport.ImportFile(fn, GlobalWinF.MainForm.GetEmuVersion(), out errorMsg, out warningMsg);
			if (errorMsg.Length > 0)
				MessageBox.Show(errorMsg, "Conversion error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			if (warningMsg.Length > 0)
				GlobalWinF.OSD.AddMessage(warningMsg);
			else
				GlobalWinF.OSD.AddMessage(Path.GetFileName(fn) + " imported as " + "Movies\\" +
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


			GlobalWinF.DisplayManager.UpdateSourceEx(Global.Emulator.VideoProvider, captureosd_srp);

			Bitmap ret = (Bitmap)captureosd_rvp.GetBitmap().Clone();

			return ret;
		}

		private void ShowConsole()
		{
			LogConsole.ShowConsole();
			LogWindowAsConsoleMenuItem.Enabled = false;
		}

		private void HideConsole()
		{
			LogConsole.HideConsole();
			LogWindowAsConsoleMenuItem.Enabled = true;
		}

		public void notifyLogWindowClosing()
		{
			DisplayLogWindowMenuItem.Checked = false;
			LogWindowAsConsoleMenuItem.Enabled = true;
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
			Text = "BizHawk" + (INTERIM ? " (interim) " : "");

			//Hide Status bar icons
			PlayRecordStatusButton.Visible = false;
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
			GlobalWinF.OSD.AddMessage("Core reboot needed for this setting");
		}

		private void SaveMovie()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.WriteMovie();
				GlobalWinF.OSD.AddMessage(Global.MovieSession.Movie.Filename + " saved.");
			}
		}

		private void HandleToggleLight()
		{
			if (MainStatusBar.Visible)
			{
				if (Global.Emulator.CoreComm.UsesDriveLed)
				{
					if (!LedLightStatusLabel.Visible)
					{
						LedLightStatusLabel.Visible = true;
					}
					if (Global.Emulator.CoreComm.DriveLED)
					{
						LedLightStatusLabel.Image = Properties.Resources.LightOn;
					}
					else
					{
						LedLightStatusLabel.Image = Properties.Resources.LightOff;
					}
				}
				else
				{
					if (LedLightStatusLabel.Visible)
					{
						LedLightStatusLabel.Visible = false;
					}
				}
			}
		}

		private void UpdateKeyPriorityIcon()
		{
			switch (Global.Config.Input_Hotkey_OverrideOptions)
			{
				default:
				case 0:
					KeyPriorityStatusLabel.Image = Properties.Resources.Both;
					KeyPriorityStatusLabel.ToolTipText = "Key priority: Allow both hotkeys and controller buttons";
					break;
				case 1:
					KeyPriorityStatusLabel.Image = Properties.Resources.GameController;
					KeyPriorityStatusLabel.ToolTipText = "Key priority: Controller buttons will override hotkeys";
					break;
				case 2:
					KeyPriorityStatusLabel.Image = Properties.Resources.HotKeys;
					KeyPriorityStatusLabel.ToolTipText = "Key priority: Hotkeys will override controller buttons";
					break;
			}
		}

		private void ToggleModePokeMode()
		{
			Global.Config.MoviePlaybackPokeMode ^= true;
			if (Global.Config.MoviePlaybackPokeMode)
			{
				GlobalWinF.OSD.AddMessage("Movie Poke mode enabled");
			}
			else
			{
				GlobalWinF.OSD.AddMessage("Movie Poke mode disabled");
			}
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

		public void LoadRamWatch(bool load_dialog)
		{
			if (Global.Config.RecentWatches.AutoLoad && !Global.Config.RecentWatches.Empty)
			{
				GlobalWinF.Tools.RamWatch.LoadFileFromRecent(Global.Config.RecentWatches[0]);
			}
			if (load_dialog)
			{
				GlobalWinF.Tools.Load<RamWatch>();
			}
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

		public void ClickSpeedItem(int num)
		{
			if ((ModifierKeys & Keys.Control) != 0) SetSpeedPercentAlternate(num);
			else SetSpeedPercent(num);
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

		public void FrameSkipMessage()
		{
			GlobalWinF.OSD.AddMessage("Frameskipping set to " + Global.Config.FrameSkip.ToString());
		}

		public void UpdateCheatStatus()
		{
			if (Global.CheatList.ActiveCount > 0)
			{
				CheatStatusButton.ToolTipText = "Cheats are currently active";
				CheatStatusButton.Image = Properties.Resources.Freeze;
				CheatStatusButton.Visible = true;
			}
			else
			{
				CheatStatusButton.ToolTipText = "";
				CheatStatusButton.Image = Properties.Resources.Blank;
				CheatStatusButton.Visible = false;
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

				CoreFileProvider.SyncCoreCommInputSignals();
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
				CoreFileProvider.SyncCoreCommInputSignals();
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
				CoreFileProvider.SyncCoreCommInputSignals();
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
				CoreFileProvider.SyncCoreCommInputSignals();
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
				CoreFileProvider.SyncCoreCommInputSignals();
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
				CoreFileProvider.SyncCoreCommInputSignals();
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
				CoreFileProvider.SyncCoreCommInputSignals();
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
				CoreFileProvider.SyncCoreCommInputSignals();
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

		public void RebootCore()
		{
			LoadRom(CurrentlyOpenRom);
		}

		public void ForcePaint()
		{
			GlobalWinF.DisplayManager.NeedsToPaint = true;
		}

		public bool StateErrorAskUser(string title, string message)
		{
			var result = MessageBox.Show(message,
				title,
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question
			);

			return result == DialogResult.Yes;
		}
	}
}
