using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Common.IOExtensions;

using BizHawk.Client.Common;
using BizHawk.Bizware.BizwareGL;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink;

using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Client.EmuHawk.CoreExtensions;
using BizHawk.Client.ApiHawk;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common.Base_Implementations;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Consoles.SNK;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm : Form
	{
		#region Constructors and Initialization, and Tear down

		private void MainForm_Load(object sender, EventArgs e)
		{
			SetWindowText();

			// Hide Status bar icons and general StatusBar prep
			MainStatusBar.Padding = new Padding(MainStatusBar.Padding.Left, MainStatusBar.Padding.Top, MainStatusBar.Padding.Left, MainStatusBar.Padding.Bottom); // Workaround to remove extra padding on right
			PlayRecordStatusButton.Visible = false;
			AVIStatusLabel.Visible = false;
			SetPauseStatusBarIcon();
			Tools.UpdateCheatRelatedTools(null, null);
			RebootStatusBarIcon.Visible = false;
			UpdateNotification.Visible = false;
			_statusBarDiskLightOnImage = Properties.Resources.LightOn;
			_statusBarDiskLightOffImage = Properties.Resources.LightOff;
			_linkCableOn = Properties.Resources.connect_16x16;
			_linkCableOff = Properties.Resources.noconnect_16x16;
			UpdateCoreStatusBarButton();
			if (Config.FirstBoot)
			{
				ProfileFirstBootLabel.Visible = true;
			}

			HandleToggleLightAndLink();
			SetStatusBar();
			_stateSlots.Update(SaveStatePrefix());

			// New version notification
			UpdateChecker.CheckComplete += (s2, e2) =>
			{
				if (IsDisposed)
				{
					return;
				}

				this.BeginInvoke(() => { UpdateNotification.Visible = UpdateChecker.IsNewVersionAvailable; });
			};
			UpdateChecker.BeginCheck(); // Won't actually check unless enabled by user
		}

		static MainForm()
		{
			// If this isn't here, then our assembly resolving hacks wont work due to the check for MainForm.INTERIM
			// its.. weird. don't ask.
		}

		private CoreComm CreateCoreComm()
		{
			var cfp = new CoreFileProvider(
				ShowMessageCoreComm,
				Global.FirmwareManager,
				Config.PathEntries,
				Config.FirmwareUserSpecifications);
			return new CoreComm(ShowMessageCoreComm, NotifyCoreComm, cfp);
		}

		public MainForm(string[] args)
		{
			void SetImages()
			{
				OpenRomMenuItem.Image = Properties.Resources.OpenFile;
				RecentRomSubMenu.Image = Properties.Resources.Recent;
				CloseRomMenuItem.Image = Properties.Resources.Close;
				PreviousSlotMenuItem.Image = Properties.Resources.MoveLeft;
				NextSlotMenuItem.Image = Properties.Resources.MoveRight;
				ReadonlyMenuItem.Image = Properties.Resources.ReadOnly;
				RecentMovieSubMenu.Image = Properties.Resources.Recent;
				RecordMovieMenuItem.Image = Properties.Resources.RecordHS;
				PlayMovieMenuItem.Image = Properties.Resources.Play;
				StopMovieMenuItem.Image = Properties.Resources.Stop;
				PlayFromBeginningMenuItem.Image = Properties.Resources.restart;
				ImportMoviesMenuItem.Image = Properties.Resources.Import;
				SaveMovieMenuItem.Image = Properties.Resources.SaveAs;
				SaveMovieAsMenuItem.Image = Properties.Resources.SaveAs;
				StopMovieWithoutSavingMenuItem.Image = Properties.Resources.Stop;
				RecordAVMenuItem.Image = Properties.Resources.RecordHS;
				ConfigAndRecordAVMenuItem.Image = Properties.Resources.AVI;
				StopAVIMenuItem.Image = Properties.Resources.Stop;
				ScreenshotMenuItem.Image = Properties.Resources.camera;
				PauseMenuItem.Image = Properties.Resources.Pause;
				RebootCoreMenuItem.Image = Properties.Resources.reboot;
				SwitchToFullscreenMenuItem.Image = Properties.Resources.Fullscreen;
				ControllersMenuItem.Image = Properties.Resources.GameController;
				HotkeysMenuItem.Image = Properties.Resources.HotKeys;
				DisplayConfigMenuItem.Image = Properties.Resources.tvIcon;
				SoundMenuItem.Image = Properties.Resources.AudioHS;
				PathsMenuItem.Image = Properties.Resources.CopyFolderHS;
				FirmwaresMenuItem.Image = Properties.Resources.pcb;
				MessagesMenuItem.Image = Properties.Resources.MessageConfig;
				AutofireMenuItem.Image = Properties.Resources.Lightning;
				RewindOptionsMenuItem.Image = Properties.Resources.Previous;
				ProfilesMenuItem.Image = Properties.Resources.user_blue_small;
				N64VideoPluginSettingsMenuItem.Image = Properties.Resources.monitor;
				SaveConfigMenuItem.Image = Properties.Resources.Save;
				LoadConfigMenuItem.Image = Properties.Resources.LoadConfig;
				ToolBoxMenuItem.Image = Properties.Resources.ToolBox;
				RamWatchMenuItem.Image = Properties.Resources.watch;
				RamSearchMenuItem.Image = Properties.Resources.search;
				LuaConsoleMenuItem.Image = Properties.Resources.Lua;
				TAStudioMenuItem.Image = Properties.Resources.TAStudio;
				HexEditorMenuItem.Image = Properties.Resources.poke;
				TraceLoggerMenuItem.Image = Properties.Resources.pencil;
				DebuggerMenuItem.Image = Properties.Resources.Bug;
				CodeDataLoggerMenuItem.Image = Properties.Resources.cdlogger;
				VirtualPadMenuItem.Image = Properties.Resources.GameController;
				CheatsMenuItem.Image = Properties.Resources.Freeze;
				GameSharkConverterMenuItem.Image = Properties.Resources.Shark;
				MultiDiskBundlerFileMenuItem.Image = Properties.Resources.SaveConfig;
				NesControllerSettingsMenuItem.Image = Properties.Resources.GameController;
				NESGraphicSettingsMenuItem.Image = Properties.Resources.tvIcon;
				NESSoundChannelsMenuItem.Image = Properties.Resources.AudioHS;
				PceControllerSettingsMenuItem.Image = Properties.Resources.GameController;
				PCEGraphicsSettingsMenuItem.Image = Properties.Resources.tvIcon;
				KeypadMenuItem.Image = Properties.Resources.calculator;
				PSXControllerSettingsMenuItem.Image = Properties.Resources.GameController;
				SNESControllerConfigurationMenuItem.Image = Properties.Resources.GameController;
				SnesGfxDebuggerMenuItem.Image = Properties.Resources.Bug;
				ColecoControllerSettingsMenuItem.Image = Properties.Resources.GameController;
				N64PluginSettingsMenuItem.Image = Properties.Resources.monitor;
				N64ControllerSettingsMenuItem.Image = Properties.Resources.GameController;
				IntVControllerSettingsMenuItem.Image = Properties.Resources.GameController;
				OnlineHelpMenuItem.Image = Properties.Resources.Help;
				ForumsMenuItem.Image = Properties.Resources.TAStudio;
				FeaturesMenuItem.Image = Properties.Resources.kitchensink;
				AboutMenuItem.Image = Properties.Resources.CorpHawkSmall;
				DumpStatusButton.Image = Properties.Resources.Blank;
				PlayRecordStatusButton.Image = Properties.Resources.Blank;
				PauseStatusButton.Image = Properties.Resources.Blank;
				RebootStatusBarIcon.Image = Properties.Resources.reboot;
				AVIStatusLabel.Image = Properties.Resources.Blank;
				LedLightStatusLabel.Image = Properties.Resources.LightOff;
				KeyPriorityStatusLabel.Image = Properties.Resources.Both;
				CoreNameStatusBarButton.Image = Properties.Resources.CorpHawkSmall;
				ProfileFirstBootLabel.Image = Properties.Resources.user_blue_small;
				LinkConnectStatusBarButton.Image = Properties.Resources.connect_16x16;
				OpenRomContextMenuItem.Image = Properties.Resources.OpenFile;
				LoadLastRomContextMenuItem.Image = Properties.Resources.Recent;
				StopAVContextMenuItem.Image = Properties.Resources.Stop;
				RecordMovieContextMenuItem.Image = Properties.Resources.RecordHS;
				PlayMovieContextMenuItem.Image = Properties.Resources.Play;
				RestartMovieContextMenuItem.Image = Properties.Resources.restart;
				StopMovieContextMenuItem.Image = Properties.Resources.Stop;
				LoadLastMovieContextMenuItem.Image = Properties.Resources.Recent;
				StopNoSaveContextMenuItem.Image = Properties.Resources.Stop;
				SaveMovieContextMenuItem.Image = Properties.Resources.SaveAs;
				SaveMovieAsContextMenuItem.Image = Properties.Resources.SaveAs;
				UndoSavestateContextMenuItem.Image = Properties.Resources.undo;
				toolStripMenuItem6.Image = Properties.Resources.GameController;
				toolStripMenuItem7.Image = Properties.Resources.HotKeys;
				toolStripMenuItem8.Image = Properties.Resources.tvIcon;
				toolStripMenuItem9.Image = Properties.Resources.AudioHS;
				toolStripMenuItem10.Image = Properties.Resources.CopyFolderHS;
				toolStripMenuItem11.Image = Properties.Resources.pcb;
				toolStripMenuItem12.Image = Properties.Resources.MessageConfig;
				toolStripMenuItem13.Image = Properties.Resources.Lightning;
				toolStripMenuItem14.Image = Properties.Resources.Previous;
				toolStripMenuItem66.Image = Properties.Resources.Save;
				toolStripMenuItem67.Image = Properties.Resources.LoadConfig;
				ScreenshotContextMenuItem.Image = Properties.Resources.camera;
				CloseRomContextMenuItem.Image = Properties.Resources.Close;
			}

			GlobalWin.MainForm = this;
			Rewinder = new Rewinder
			{
				MessageCallback = AddOnScreenMessage
			};

			Global.ControllerInputCoalescer = new ControllerInputCoalescer();
			Global.FirmwareManager = new FirmwareManager();
			MovieSession = new MovieSession
			{
				Movie = MovieService.DefaultInstance,
				MovieControllerAdapter = MovieService.DefaultInstance.LogGeneratorInstance().MovieControllerAdapter,
				MessageCallback = AddOnScreenMessage,
				PopupCallback = ShowMessageCoreComm,
				AskYesNoCallback = StateErrorAskUser,
				PauseCallback = PauseEmulator,
				ModeChangedCallback = SetMainformMovieInfo
			};

			Icon = Properties.Resources.logo;
			InitializeComponent();
			SetImages();
			Game = GameInfo.NullInstance;

			_throttle = new Throttle();

			Emulator = new NullEmulator();
			GlobalWin.Tools = new ToolManager(this, Config, Emulator);
			CheatList.Changed += Tools.UpdateCheatRelatedTools;

			UpdateStatusSlots();
			UpdateKeyPriorityIcon();

			// In order to allow late construction of this database, we hook up a delegate here to dearchive the data and provide it on demand
			// we could background thread this later instead if we wanted to be real clever
			NES.BootGodDB.GetDatabaseBytes = () =>
			{
				string xmlPath = Path.Combine(PathUtils.GetExeDirectoryAbsolute(), "gamedb", "NesCarts.xml");
				string x7zPath = Path.Combine(PathUtils.GetExeDirectoryAbsolute(), "gamedb", "NesCarts.7z");
				bool loadXml = File.Exists(xmlPath);
				using var nesCartFile = new HawkFile(loadXml ? xmlPath : x7zPath);
				if (!loadXml)
				{
					nesCartFile.BindFirst();
				}

				return nesCartFile
					.GetStream()
					.ReadAllBytes();
			};
			try
			{
				_argParser.ParseArguments(args);
			}
			catch (ArgParserException e)
			{
				MessageBox.Show(e.Message);
			}

			Database.LoadDatabase(Path.Combine(PathUtils.GetExeDirectoryAbsolute(), "gamedb", "gamedb.txt"));

			// TODO GL - a lot of disorganized wiring-up here
			// installed separately on Unix (via package manager or from https://developer.nvidia.com/cg-toolkit-download), look in $PATH
			CGC.CGCBinPath = OSTailoredCode.IsUnixHost ? "cgc" : Path.Combine(PathUtils.GetDllDirectory(), "cgc.exe");
			PresentationPanel = new PresentationPanel(this, Config, GlobalWin.GL)
			{
				GraphicsControl = { MainWindow = true }
			};
			GlobalWin.DisplayManager = new DisplayManager(PresentationPanel);
			Controls.Add(PresentationPanel);
			Controls.SetChildIndex(PresentationPanel, 0);

			// TODO GL - move these event handlers somewhere less obnoxious line in the On* overrides
			Load += (o, e) =>
			{
				AllowDrop = true;
				DragEnter += FormDragEnter;
				DragDrop += FormDragDrop;
			};

			Closing += (o, e) =>
			{
				if (Tools.AskSave())
				{
					// zero 03-nov-2015 - close game after other steps. tools might need to unhook themselves from a core.
					MovieSession.Movie.Stop();
					Tools.Close();
					CloseGame();

					// does this need to be last for any particular reason? do tool dialogs persist settings when closing?
					SaveConfig();
				}
				else
				{
					e.Cancel = true;
				}
			};

			ResizeBegin += (o, e) =>
			{
				_inResizeLoop = true;
				Sound?.StopSound();
			};

			Resize += (o, e) =>
			{
				SetWindowText();
			};

			ResizeEnd += (o, e) =>
			{
				_inResizeLoop = false;
				SetWindowText();

				if (PresentationPanel != null)
				{
					PresentationPanel.Resized = true;
				}

				Sound?.StartSound();
			};

			Input.Initialize(this);
			InitControls();

			Global.ActiveController = new Controller(NullController.Instance.Definition);
			Global.AutoFireController = _autofireNullControls;
			Global.AutofireStickyXORAdapter.SetOnOffPatternFromConfig();
			try
			{
				GlobalWin.Sound = new Sound(Handle);
			}
			catch
			{
				string message = "Couldn't initialize sound device! Try changing the output method in Sound config.";
				if (Config.SoundOutputMethod == ESoundOutputMethod.DirectSound)
				{
					message = "Couldn't initialize DirectSound! Things may go poorly for you. Try changing your sound driver to 44.1khz instead of 48khz in mmsys.cpl.";
				}

				MessageBox.Show(message, "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				Config.SoundOutputMethod = ESoundOutputMethod.Dummy;
				GlobalWin.Sound = new Sound(Handle);
			}

			Sound.StartSound();
			InputManager.RewireInputChain();
			RewireSound();

			// Workaround for windows, location is -32000 when minimized, if they close it during this time, that's what gets saved
			if (Config.MainWndx == -32000)
			{
				Config.MainWndx = 0;
			}

			if (Config.MainWndy == -32000)
			{
				Config.MainWndy = 0;
			}

			if (Config.MainWndx != -1 && Config.MainWndy != -1 && Config.SaveWindowPosition)
			{
				Location = new Point(Config.MainWndx, Config.MainWndy);
			}

			if (_argParser.cmdRom != null)
			{
				// Commandline should always override auto-load
				var ioa = OpenAdvancedSerializer.ParseWithLegacy(_argParser.cmdRom);
				LoadRom(_argParser.cmdRom, new LoadRomArgs { OpenAdvanced = ioa });
				if (Game == null)
				{
					MessageBox.Show($"Failed to load {_argParser.cmdRom} specified on commandline");
				}
			}
			else if (Config.RecentRoms.AutoLoad && !Config.RecentRoms.Empty)
			{
				LoadRomFromRecent(Config.RecentRoms.MostRecent);
			}

			if (_argParser.audiosync.HasValue)
			{
				Config.VideoWriterAudioSync = _argParser.audiosync.Value;
			}

			if (_argParser.cmdMovie != null)
			{
				_suppressSyncSettingsWarning = true; // We don't want to be nagged if we are attempting to automate
				if (Game == null)
				{
					OpenRom();
				}

				// If user picked a game, then do the commandline logic
				if (!Game.IsNullInstance())
				{
					var movie = MovieService.Get(_argParser.cmdMovie);
					MovieSession.ReadOnly = true;

					// if user is dumping and didn't supply dump length, make it as long as the loaded movie
					if (_argParser._autoDumpLength == 0)
					{
						_argParser._autoDumpLength = movie.InputLogLength;
					}

					// Copy pasta from drag & drop
					if (MovieImport.IsValidMovieExtension(Path.GetExtension(_argParser.cmdMovie)))
					{
						ProcessMovieImport(_argParser.cmdMovie, true);
					}
					else
					{
						StartNewMovie(movie, false);
						Config.RecentMovies.Add(_argParser.cmdMovie);
					}

					_suppressSyncSettingsWarning = false;
				}
			}
			else if (Config.RecentMovies.AutoLoad && !Config.RecentMovies.Empty)
			{
				if (Game.IsNullInstance())
				{
					OpenRom();
				}

				// If user picked a game, then do the autoload logic
				if (!Game.IsNullInstance())
				{
					if (File.Exists(Config.RecentMovies.MostRecent))
					{
						StartNewMovie(MovieService.Get(Config.RecentMovies.MostRecent), false);
					}
					else
					{
						Config.RecentMovies.HandleLoadError(Config.RecentMovies.MostRecent);
					}
				}
			}

			if (_argParser.startFullscreen || Config.StartFullscreen)
			{
				_needsFullscreenOnLoad = true;
			}

			if (!Game.IsNullInstance())
			{
				if (_argParser.cmdLoadState != null)
				{
					LoadState(_argParser.cmdLoadState, Path.GetFileName(_argParser.cmdLoadState));
				}
				else if (_argParser.cmdLoadSlot != null)
				{
					LoadQuickSave($"QuickSave{_argParser.cmdLoadSlot}");
				}
				else if (Config.AutoLoadLastSaveSlot)
				{
					LoadQuickSave($"QuickSave{Config.SaveSlot}");
				}
			}

			//start Lua Console if requested in the command line arguments
			if (_argParser.luaConsole)
			{
				Tools.Load<LuaConsole>();
			}
			//load Lua Script if requested in the command line arguments
			if (_argParser.luaScript != null)
			{
				if (OSTailoredCode.IsUnixHost) Console.WriteLine($"The Lua environment can currently only be created on Windows, {_argParser.luaScript} will not be loaded.");
				else Tools.LuaConsole.LoadLuaFile(_argParser.luaScript);
			}

			SetStatusBar();

			if (Config.StartPaused)
			{
				PauseEmulator();
			}

			// start dumping, if appropriate
			if (_argParser.cmdDumpType != null && _argParser.cmdDumpName != null)
			{
				if (OSTailoredCode.IsUnixHost) Console.WriteLine("A/V dump requires Win32 API calls, ignored");
				else RecordAv(_argParser.cmdDumpType, _argParser.cmdDumpName);
			}

			SetMainformMovieInfo();

			SynchChrome();

			PresentationPanel.Control.Paint += (o, e) =>
			{
				// I would like to trigger a repaint here, but this isn't done yet
			};

			if (!OSTailoredCode.IsUnixHost && !Config.SkipOutdatedOsCheck)
			{
				static string GetRegValue(string key)
				{
					using var proc = OSTailoredCode.ConstructSubshell("REG", $@"QUERY ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion"" /V {key}");
					proc.Start();
					return proc.StandardOutput.ReadToEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)[1].Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)[2];
				}
				var winVer = float.Parse(GetRegValue("CurrentVersion"), NumberFormatInfo.InvariantInfo);
				if (winVer < 6.3f)
				{
					// less than is just easier than equals
					string message = ($"Quick reminder: Windows {(winVer < 6.2f ? winVer < 6.1f ? winVer < 6.0f ? "XP" : "Vista" : "7" : "8")} is no longer supported by Microsoft. EmuHawk will continue to work, but please get a new operating system for increased security (either Windows 8.1, Windows 10, or a GNU+Linux distro).");
				}
				else if (GetRegValue("ProductName").Contains("Windows 10"))
				{
					var win10version = int.Parse(GetRegValue("ReleaseId"));
					if (win10version < 1809)
					{
						string message = ($"Quick reminder: version {win10version} of Windows 10 is no longer supported by Microsoft. EmuHawk will continue to work, but please update to at least 1809 \"Redstone 5\" for increased security.");
					}
				}
				else
				{
					// 8.1: can't be bothered writing code for KB installed check, not that I have a Win8.1 machine to test on anyway, so it gets a free pass --yoshi
				}
			}
		}

		private readonly bool _suppressSyncSettingsWarning;

		public int ProgramRunLoop()
		{
			CheckMessages(); // can someone leave a note about why this is needed?

			// needs to be done late, after the log console snaps on top
			// fullscreen should snap on top even harder!
			if (_needsFullscreenOnLoad)
			{
				_needsFullscreenOnLoad = false;
				ToggleFullscreen();
			}
			
			// Simply exit the program if the version is asked for
			if (_argParser.printVersion)
			{
				// Print the version
				Console.WriteLine(VersionInfo.GetEmuVersion());
				// Return and leave
				return _exitCode;
			}

			// incantation required to get the program reliably on top of the console window
			// we might want it in ToggleFullscreen later, but here, it needs to happen regardless
			BringToFront();
			Activate();
			BringToFront();

			InitializeFpsData();

			for (; ; )
			{
				Input.Instance.Update();

				// handle events and dispatch as a hotkey action, or a hotkey button, or an input button
				ProcessInput();
				ClientControls.LatchFromPhysical(_hotkeyCoalescer);

				Global.ActiveController.LatchFromPhysical(Global.ControllerInputCoalescer);

				Global.ActiveController.ApplyAxisConstraints(
					(Emulator is N64 && Config.N64UseCircularAnalogConstraint) ? "Natural Circle" : null);

				Global.ActiveController.OR_FromLogical(ClickyVirtualPadController);
				AutoFireController.LatchFromPhysical(Global.ControllerInputCoalescer);

				if (ClientControls["Autohold"])
				{
					Global.StickyXORAdapter.MassToggleStickyState(Global.ActiveController.PressedButtons);
					Global.AutofireStickyXORAdapter.MassToggleStickyState(AutoFireController.PressedButtons);
				}
				else if (ClientControls["Autofire"])
				{
					Global.AutofireStickyXORAdapter.MassToggleStickyState(Global.ActiveController.PressedButtons);
				}

				// autohold/autofire must not be affected by the following inputs
				Global.ActiveController.Overrides(Global.ButtonOverrideAdapter);

				if (Tools.Has<LuaConsole>() && !SuppressLua)
				{
					Tools.LuaConsole.ResumeScripts(false);
				}

				StepRunLoop_Core();
				StepRunLoop_Throttle();

				Render();

				CheckMessages();

				if (_exitRequestPending)
				{
					_exitRequestPending = false;
					Close();
				}

				if (_windowClosedAndSafeToExitProcess)
				{
					break;
				}

				if (Config.DispSpeedupFeatures != 0)
				{
					Thread.Sleep(0);
				}
			}

			Shutdown();
			return _exitCode;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			// NOTE: this gets called twice sometimes. once by using() in Program.cs and once from winforms internals when the form is closed...
			if (DisplayManager != null)
			{
				DisplayManager.Dispose();
				GlobalWin.DisplayManager = null;
			}

			if (disposing)
			{
				components?.Dispose();
			}

			base.Dispose(disposing);
		}

		#endregion

		#region Pause

		private bool _emulatorPaused;
		public bool EmulatorPaused
		{
			get => _emulatorPaused;

			private set
			{
				if (_emulatorPaused && !value) // Unpausing
				{
					InitializeFpsData();
				}

				_emulatorPaused = value;
				OnPauseChanged?.Invoke(this, new PauseChangedEventArgs(_emulatorPaused));
			}
		}

		public delegate void PauseChangedEventHandler(object sender, PauseChangedEventArgs e);
		public event PauseChangedEventHandler OnPauseChanged;

		public class PauseChangedEventArgs : EventArgs
		{
			public PauseChangedEventArgs(bool paused)
			{
				Paused = paused;
			}

			public bool Paused { get; }
		}

		#endregion

		#region Properties

		public string CurrentlyOpenRom { get; private set; } // todo - delete me and use only args instead
		public LoadRomArgs CurrentlyOpenRomArgs { get; private set; }
		public bool PauseAvi { get; set; }
		public bool PressFrameAdvance { get; set; }
		public bool HoldFrameAdvance { get; set; } // necessary for tastudio > button
		public bool PressRewind { get; set; } // necessary for tastudio < button
		public bool FastForward { get; set; }

		/// <summary>
		/// Disables updates for video/audio, and enters "turbo" mode.
		/// Can be used to replicate Gens-rr's "latency compensation" that involves:
		/// <list type="bullet">
		/// <item><description>Saving a no-framebuffer state that is stored in RAM</description></item>
		/// <item><description>Emulating forth for some frames with updates disabled</description></item>
		/// <item><list type="bullet">
		/// <item><description>Optionally hacking in-game memory
		/// (like camera position, to show off-screen areas)</description></item>
		/// </list></item>
		/// <item><description>Updating the screen</description></item>
		/// <item><description>Loading the no-framebuffer state from RAM</description></item>
		/// </list>
		/// The most common use case is CamHack for Sonic games.
		/// Accessing this from Lua allows to keep internal code hacks to minimum.
		/// <list type="bullet">
		/// <item><description><see cref="EmuHawkLuaLibrary.InvisibleEmulation(bool)"/></description></item>
		/// <item><description><see cref="EmuHawkLuaLibrary.SeekFrame(int)"/></description></item>
		/// </list>
		/// </summary>
		public bool InvisibleEmulation { get; set; }

		// runloop won't exec lua
		public bool SuppressLua { get; set; }

		public long MouseWheelTracker { get; private set; }

		private int? _pauseOnFrame;
		public int? PauseOnFrame // If set, upon completion of this frame, the client wil pause
		{
			get => _pauseOnFrame;

			set
			{
				_pauseOnFrame = value;
				SetPauseStatusBarIcon();

				if (value == null) // TODO: make an Event handler instead, but the logic here is that after turbo seeking, tools will want to do a real update when the emulator finally pauses
				{
					bool skipScripts = !(Config.TurboSeek && !Config.RunLuaDuringTurbo && !SuppressLua);
					Tools.UpdateToolsBefore(skipScripts);
					Tools.UpdateToolsAfter(skipScripts);
				}
			}
		}

		public bool IsSeeking => PauseOnFrame.HasValue;
		private bool IsTurboSeeking => PauseOnFrame.HasValue && Config.TurboSeek;
		public bool IsTurboing => ClientControls["Turbo"] || IsTurboSeeking;

		#endregion

		#region Public Methods

		public void AddOnScreenMessage(string message)
		{
			GlobalWin.OSD.AddMessage(message);
		}

		public void ClearHolds()
		{
			Global.StickyXORAdapter.ClearStickies();
			Global.AutofireStickyXORAdapter.ClearStickies();

			if (Tools.Has<VirtualpadTool>())
			{
				Tools.VirtualPad.ClearVirtualPadHolds();
			}
		}

		public void FlagNeedsReboot()
		{
			RebootStatusBarIcon.Visible = true;
			AddOnScreenMessage("Core reboot needed for this setting");
		}

		/// <summary>
		/// Controls whether the app generates input events. should be turned off for most modal dialogs
		/// </summary>
		public Input.AllowInput AllowInput(bool yieldAlt)
		{
			// the main form gets input
			if (ActiveForm == this)
			{
				return Input.AllowInput.All;
			}

			// even more special logic for TAStudio:
			// TODO - implement by event filter in TAStudio
			if (ActiveForm is TAStudio maybeTAStudio)
			{
				if (yieldAlt || maybeTAStudio.IsInMenuLoop)
				{
					return Input.AllowInput.None;
				}
			}

			// modals that need to capture input for binding purposes get input, of course
			if (ActiveForm is HotkeyConfig
				|| ActiveForm is ControllerConfig
				|| ActiveForm is TAStudio
				|| ActiveForm is VirtualpadTool)
			{
				return Input.AllowInput.All;
			}

			// if no form is active on this process, then the background input setting applies
			if (ActiveForm == null && Config.AcceptBackgroundInput)
			{
				return Config.AcceptBackgroundInputControllerOnly ? Input.AllowInput.OnlyController : Input.AllowInput.All;
			}

			return Input.AllowInput.None;
		}

		// TODO: make these actual properties
		// This is a quick hack to reduce the dependency on Globals
		private IEmulator Emulator
		{
			get => Global.Emulator;

			set
			{
				Global.Emulator = value;
				_currentVideoProvider = Global.Emulator.AsVideoProviderOrDefault();
				_currentSoundProvider = Global.Emulator.AsSoundProviderOrDefault();
			}
		}

		private InputManager InputManager => Global.InputManager;

		private IVideoProvider _currentVideoProvider = NullVideo.Instance;

		private ISoundProvider _currentSoundProvider = new NullSound(44100 / 60); // Reasonable default until we have a core instance

		private Config Config
		{
			get => Global.Config;
			set => Global.Config = value;
		}

		private ToolManager Tools => GlobalWin.Tools;
		private DisplayManager DisplayManager => GlobalWin.DisplayManager;

		private IMovieSession MovieSession
		{
			get => Global. MovieSession;
			set => Global.MovieSession = value;
		}

		private GameInfo Game
		{
			get => Global.Game;
			set => Global.Game = value;
		}

		private Sound Sound => GlobalWin.Sound;
		private CheatCollection CheatList => Global.CheatList;
		private AutofireController AutoFireController => Global.AutoFireController;
		private AutoFireStickyXorAdapter AutofireStickyXORAdapter => Global.AutofireStickyXORAdapter;
		private ClickyVirtualPadController ClickyVirtualPadController => Global.ClickyVirtualPadController;

		private Rewinder Rewinder { get; }

		private FirmwareManager FirmwareManager => Global.FirmwareManager;
		private Controller ClientControls => Global.ClientControls;

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			Input.Instance.ControlInputFocus(this, Input.InputFocus.Mouse, true);
		}

		protected override void OnDeactivate(EventArgs e)
		{
			Input.Instance.ControlInputFocus(this, Input.InputFocus.Mouse, false);
			base.OnDeactivate(e);
		}

		private void ProcessInput()
		{
			var conInput = (ControllerInputCoalescer)Global.ControllerInputCoalescer;

			for (; ; )
			{
				// loop through all available events
				var ie = Input.Instance.DequeueEvent();
				if (ie == null)
				{
					break;
				}

				// useful debugging:
				// Console.WriteLine(ie);

				// TODO - wonder what happens if we pop up something interactive as a response to one of these hotkeys? may need to purge further processing

				// look for hotkey bindings for this key
				var triggers = ClientControls.SearchBindings(ie.LogicalButton.ToString());
				if (triggers.Count == 0)
				{
					// Maybe it is a system alt-key which hasn't been overridden
					if (ie.EventType == Input.InputEventType.Press)
					{
						if (ie.LogicalButton.Alt && ie.LogicalButton.Button.Length == 1)
						{
							var c = ie.LogicalButton.Button.ToLower()[0];
							if ((c >= 'a' && c <= 'z') || c == ' ')
							{
								SendAltKeyChar(c);
							}
						}

						if (ie.LogicalButton.Alt && ie.LogicalButton.Button == "Space")
						{
							SendPlainAltKey(32);
						}
					}

					// ordinarily, an alt release with nothing else would move focus to the MenuBar. but that is sort of useless, and hard to implement exactly right.
				}

				// zero 09-sep-2012 - all input is eligible for controller input. not sure why the above was done. 
				// maybe because it doesn't make sense to me to bind hotkeys and controller inputs to the same keystrokes

				bool handled;
				switch (Config.InputHotkeyOverrideOptions)
				{
					default:
					case 0: // Both allowed
						conInput.Receive(ie);

						handled = false;
						if (ie.EventType == Input.InputEventType.Press)
						{
							handled = triggers.Aggregate(handled, (current, trigger) => current | CheckHotkey(trigger));
						}

						// hotkeys which aren't handled as actions get coalesced as pollable virtual client buttons
						if (!handled)
						{
							_hotkeyCoalescer.Receive(ie);
						}

						break;
					case 1: // Input overrides Hotkeys
						conInput.Receive(ie);
						if (!Global.ActiveController.HasBinding(ie.LogicalButton.ToString()))
						{
							handled = false;
							if (ie.EventType == Input.InputEventType.Press)
							{
								handled = triggers.Aggregate(false, (current, trigger) => current | CheckHotkey(trigger));
							}

							// hotkeys which aren't handled as actions get coalesced as pollable virtual client buttons
							if (!handled)
							{
								_hotkeyCoalescer.Receive(ie);
							}
						}

						break;
					case 2: // Hotkeys override Input
						handled = false;
						if (ie.EventType == Input.InputEventType.Press)
						{
							handled = triggers.Aggregate(false, (current, trigger) => current | CheckHotkey(trigger));
						}

						// hotkeys which aren't handled as actions get coalesced as pollable virtual client buttons
						if (!handled)
						{
							_hotkeyCoalescer.Receive(ie);

							// Check for hotkeys that may not be handled through CheckHotkey() method, reject controller input mapped to these
							if (!triggers.Any(IsInternalHotkey))
							{
								conInput.Receive(ie);
							}
						}

						break;
				}
			} // foreach event

			// also handle floats
			conInput.AcceptNewFloats(Input.Instance.GetFloats().Select(o =>
			{
				// hackish
				if (o.Item1 == "WMouse X")
				{
					var p = DisplayManager.UntransformPoint(new Point((int)o.Item2, 0));
					float x = p.X / (float)_currentVideoProvider.BufferWidth;
					return new Tuple<string, float>("WMouse X", (x * 20000) - 10000);
				}

				if (o.Item1 == "WMouse Y")
				{
					var p = DisplayManager.UntransformPoint(new Point(0, (int)o.Item2));
					float y = p.Y / (float)_currentVideoProvider.BufferHeight;
					return new Tuple<string, float>("WMouse Y", (y * 20000) - 10000);
				}

				return o;
			}));
		}

		public void RebootCore()
		{
			if (CurrentlyOpenRomArgs == null) return;
			LoadRom(CurrentlyOpenRomArgs.OpenAdvanced.SimplePath, CurrentlyOpenRomArgs);
		}

		public void PauseEmulator()
		{
			EmulatorPaused = true;
			SetPauseStatusBarIcon();
		}

		public void UnpauseEmulator()
		{
			EmulatorPaused = false;
			SetPauseStatusBarIcon();
		}

		public void TogglePause()
		{
			EmulatorPaused ^= true;
			SetPauseStatusBarIcon();

			// TODO: have tastudio set a pause status change callback, or take control over pause
			if (Tools.Has<TAStudio>())
			{
				Tools.UpdateValues<TAStudio>();
			}
		}

		public void TakeScreenshotToClipboard()
		{
			using (var bb = Config.ScreenshotCaptureOsd ? CaptureOSD() : MakeScreenshotImage())
			{
				using var img = bb.ToSysdrawingBitmap();
				Clipboard.SetImage(img);
			}

			AddOnScreenMessage("Screenshot (raw) saved to clipboard.");
		}

		private void TakeScreenshotClientToClipboard()
		{
			using (var bb = DisplayManager.RenderOffscreen(_currentVideoProvider, Config.ScreenshotCaptureOsd))
			{
				using var img = bb.ToSysdrawingBitmap();
				Clipboard.SetImage(img);
			}

			AddOnScreenMessage("Screenshot (client) saved to clipboard.");
		}

		private string ScreenshotPrefix()
		{
			var screenPath = Config.PathEntries.ScreenshotAbsolutePathFor(Game.System);
			var name = Game.Name.FilesystemSafeName();
			return Path.Combine(screenPath, name);
		}

		public void TakeScreenshot()
		{
			var basename = $"{ScreenshotPrefix()}.{DateTime.Now:yyyy-MM-dd HH.mm.ss}";

			var fnameBare = $"{basename}.png";
			var fname = $"{basename} (0).png";

			// if the (0) filename exists, do nothing. we'll bump up the number later
			// if the bare filename exists, move it to (0)
			// otherwise, no related filename exists, and we can proceed with the bare filename
			if (!File.Exists(fname))
			{
				if (File.Exists(fnameBare)) File.Move(fnameBare, fname);
				else fname = fnameBare;
			}

			for (var seq = 0; File.Exists(fname); seq++)
				fname = $"{basename} ({seq}).png";

			TakeScreenshot(fname);
		}

		public void TakeScreenshot(string path)
		{
			var fi = new FileInfo(path);
			if (fi.Directory != null && !fi.Directory.Exists)
			{
				fi.Directory.Create();
			}

			using (var bb = Config.ScreenshotCaptureOsd ? CaptureOSD() : MakeScreenshotImage())
			{
				using var img = bb.ToSysdrawingBitmap();
				img.Save(fi.FullName, ImageFormat.Png);
			}

			AddOnScreenMessage($"{fi.Name} saved.");
		}

		public void FrameBufferResized()
		{
			// run this entire thing exactly twice, since the first resize may adjust the menu stacking
			for (int i = 0; i < 2; i++)
			{
				int zoom = Config.TargetZoomFactors[Emulator.SystemId];
				var area = Screen.FromControl(this).WorkingArea;

				int borderWidth = Size.Width - PresentationPanel.Control.Size.Width;
				int borderHeight = Size.Height - PresentationPanel.Control.Size.Height;

				// start at target zoom and work way down until we find acceptable zoom
				Size lastComputedSize = new Size(1, 1);
				for (; zoom >= 1; zoom--)
				{
					lastComputedSize = DisplayManager.CalculateClientSize(_currentVideoProvider, zoom);
					if (lastComputedSize.Width + borderWidth < area.Width
						&& lastComputedSize.Height + borderHeight < area.Height)
					{
						break;
					}
				}

				Console.WriteLine($"Selecting display size {lastComputedSize}");

				// Change size
				Size = new Size(lastComputedSize.Width + borderWidth, lastComputedSize.Height + borderHeight);
				PerformLayout();
				PresentationPanel.Resized = true;

				// Is window off the screen at this size?
				if (!area.Contains(Bounds))
				{
					if (Bounds.Right > area.Right) // Window is off the right edge
					{
						Location = new Point(area.Right - Size.Width, Location.Y);
					}

					if (Bounds.Bottom > area.Bottom) // Window is off the bottom edge
					{
						Location = new Point(Location.X, area.Bottom - Size.Height);
					}
				}
			}
		}

		private void SynchChrome()
		{
			if (_inFullscreen)
			{
				// TODO - maybe apply a hack tracked during fullscreen here to override it
				FormBorderStyle = FormBorderStyle.None;
				MainMenuStrip.Visible = Config.DispChromeMenuFullscreen && !_argParser._chromeless;
				MainStatusBar.Visible = Config.DispChromeStatusBarFullscreen && !_argParser._chromeless;
			}
			else
			{
				MainStatusBar.Visible = Config.DispChromeStatusBarWindowed && !_argParser._chromeless;
				MainMenuStrip.Visible = Config.DispChromeMenuWindowed && !_argParser._chromeless;
				MaximizeBox = MinimizeBox = Config.DispChromeCaptionWindowed && !_argParser._chromeless;
				if (Config.DispChromeFrameWindowed == 0 || _argParser._chromeless)
				{
					FormBorderStyle = FormBorderStyle.None;
				}
				else if (Config.DispChromeFrameWindowed == 1)
				{
					FormBorderStyle = FormBorderStyle.SizableToolWindow;
				}
				else if (Config.DispChromeFrameWindowed == 2)
				{
					FormBorderStyle = FormBorderStyle.Sizable;
				}
			}
		}

		public void ToggleFullscreen(bool allowSuppress = false)
		{
			AutohideCursor(false);

			// prohibit this operation if the current controls include LMouse
			if (allowSuppress)
			{
				if (Global.ActiveController.HasBinding("WMouse L"))
				{
					return;
				}
			}

			if (!_inFullscreen)
			{
				SuspendLayout();

				// Work around an AMD driver bug in >= vista:
				// It seems windows will activate opengl fullscreen mode when a GL control is occupying the exact space of a screen (0,0 and dimensions=screensize)
				// AMD cards manifest a problem under these circumstances, flickering other monitors. 
				// It isn't clear whether nvidia cards are failing to employ this optimization, or just not flickering.
				// (this could be determined with more work; other side affects of the fullscreen mode include: corrupted TaskBar, no modal boxes on top of GL control, no screenshots)
				// At any rate, we can solve this by adding a 1px black border around the GL control
				// Please note: It is important to do this before resizing things, otherwise momentarily a GL control without WS_BORDER will be at the magic dimensions and cause the flakeout
				if (!OSTailoredCode.IsUnixHost
					&& Config.DispFullscreenHacks
					&& Config.DispMethod == EDispMethod.OpenGL)
				{
					// ATTENTION: this causes the StatusBar to not work well, since the backcolor is now set to black instead of SystemColors.Control.
					// It seems that some StatusBar elements composite with the backcolor. 
					// Maybe we could add another control under the StatusBar. with a different backcolor
					Padding = new Padding(1);
					BackColor = Color.Black;

					// FUTURE WORK:
					// re-add this padding back into the display manager (so the image will get cut off a little but, but a few more resolutions will fully fit into the screen)
				}

				_windowedLocation = Location;

				_inFullscreen = true;
				SynchChrome();
				WindowState = FormWindowState.Maximized; // be sure to do this after setting the chrome, otherwise it wont work fully
				ResumeLayout();

				PresentationPanel.Resized = true;
			}
			else
			{
				SuspendLayout();

				WindowState = FormWindowState.Normal;

				if (!OSTailoredCode.IsUnixHost)
				{
					// do this even if DispFullscreenHacks aren't enabled, to restore it in case it changed underneath us or something
					Padding = new Padding(0);

					// it's important that we set the form color back to this, because the StatusBar icons blend onto the mainform, not onto the StatusBar--
					// so we need the StatusBar and mainform backdrop color to match
					BackColor = SystemColors.Control;
				}

				_inFullscreen = false;

				SynchChrome();
				Location = _windowedLocation;
				ResumeLayout();

				FrameBufferResized();
			}
		}

		private void OpenLuaConsole()
		{
			Tools.Load<LuaConsole>();
		}

		public void ClickSpeedItem(int num)
		{
			if ((ModifierKeys & Keys.Control) != 0)
			{
				SetSpeedPercentAlternate(num);
			}
			else
			{
				SetSpeedPercent(num);
			}
		}

		public void Unthrottle()
		{
			_unthrottled = true;
		}

		public void Throttle()
		{
			_unthrottled = false;
		}

		private void ThrottleMessage()
		{
			string type = ":(none)";
			if (Config.SoundThrottle)
			{
				type = ":Sound";
			}

			if (Config.VSyncThrottle)
			{
				type = $":Vsync{(Config.VSync ? "[ena]" : "[dis]")}";
			}

			if (Config.ClockThrottle)
			{
				type = ":Clock";
			}

			string throttled = _unthrottled ? "Unthrottled" : "Throttled";
			string msg = $"{throttled}{type} ";

			AddOnScreenMessage(msg);
		}

		public void FrameSkipMessage()
		{
			AddOnScreenMessage($"Frameskipping set to {Config.FrameSkip}");
		}

		public void UpdateCheatStatus()
		{
			if (CheatList.ActiveCount > 0)
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

		private void SNES_ToggleBg(int layer)
		{
			if (!(Emulator is LibsnesCore || Emulator is Snes9x) || !1.RangeTo(4).Contains(layer))
			{
				return;
			}

			bool result = false;
			if (Emulator is LibsnesCore bsnes)
			{
				var s = bsnes.GetSettings();
				switch (layer)
				{
					case 1:
						result = s.ShowBG1_0 = s.ShowBG1_1 ^= true;
						break;
					case 2:
						result = s.ShowBG2_0 = s.ShowBG2_1 ^= true;
						break;
					case 3:
						result = s.ShowBG3_0 = s.ShowBG3_1 ^= true;
						break;
					case 4:
						result = s.ShowBG4_0 = s.ShowBG4_1 ^= true;
						break;
				}

				bsnes.PutSettings(s);
			}
			else if (Emulator is Snes9x snes9X)
			{
				var s = snes9X.GetSettings();
				switch (layer)
				{
					case 1:
						result = s.ShowBg0 ^= true;
						break;
					case 2:
						result = s.ShowBg1 ^= true;
						break;
					case 3:
						result = s.ShowBg2 ^= true;
						break;
					case 4:
						result = s.ShowBg3 ^= true;
						break;
				}

				snes9X.PutSettings(s);
			}

			AddOnScreenMessage($"BG {layer} Layer {(result ? "On" : "Off")}");
		}

		private void SNES_ToggleObj(int layer)
		{
			if (!(Emulator is LibsnesCore || Emulator is Snes9x) || !1.RangeTo(4).Contains(layer))
			{
				return;
			}

			bool result = false;
			if (Emulator is LibsnesCore bsnes)
			{
				var s = bsnes.GetSettings();
				result = layer switch
				{
					1 => (s.ShowOBJ_0 ^= true),
					2 => (s.ShowOBJ_1 ^= true),
					3 => (s.ShowOBJ_2 ^= true),
					4 => (s.ShowOBJ_3 ^= true),
					_ => result
				};

				bsnes.PutSettings(s);
				AddOnScreenMessage($"Obj {layer} Layer {(result ? "On" : "Off")}");
			}
			else if (Emulator is Snes9x snes9X)
			{
				var s = snes9X.GetSettings();
				result = layer switch
				{
					1 => (s.ShowSprites0 ^= true),
					2 => (s.ShowSprites1 ^= true),
					3 => (s.ShowSprites2 ^= true),
					4 => (s.ShowSprites3 ^= true),
					_ => result
				};

				snes9X.PutSettings(s);
				AddOnScreenMessage($"Sprite {layer} Layer {(result ? "On" : "Off")}");
			}
		}

		public bool RunLibretroCoreChooser()
		{
			using var ofd = new OpenFileDialog();

			if (Config.LibretroCore != null)
			{
				ofd.FileName = Path.GetFileName(Config.LibretroCore);
				ofd.InitialDirectory = Path.GetDirectoryName(Config.LibretroCore);
			}
			else
			{
				ofd.InitialDirectory = Config.PathEntries.AbsolutePathForType("Libretro", "Cores");
				if (!Directory.Exists(ofd.InitialDirectory))
				{
					Directory.CreateDirectory(ofd.InitialDirectory);
				}
			}

			ofd.RestoreDirectory = true;
			ofd.Filter = new FilesystemFilter("Libretro Cores", new[] { "dll" }).ToString();

			if (ofd.ShowDialog() == DialogResult.Cancel)
			{
				return false;
			}

			Config.LibretroCore = ofd.FileName;

			return true;
		}

		#endregion

		#region Private variables

		private Size _lastVideoSize = new Size(-1, -1), _lastVirtualSize = new Size(-1, -1);
		private readonly SaveSlotManager _stateSlots = new SaveSlotManager();

		// AVI/WAV state
		private IVideoWriter _currAviWriter;

		private AutofireController _autofireNullControls;

		// Sound refactor TODO: we can enforce async mode here with a property that gets/sets this but does an async check
		private ISoundProvider _aviSoundInputAsync; // Note: This sound provider must be in async mode!

		private SimpleSyncSoundProvider _dumpProxy; // an audio proxy used for dumping
		private bool _dumpaudiosync; // set true to for experimental AV dumping
		private int _avwriterResizew;
		private int _avwriterResizeh;
		private bool _avwriterpad;

		private bool _windowClosedAndSafeToExitProcess;
		private int _exitCode;
		private bool _exitRequestPending;
		private bool _runloopFrameProgress;
		private long _frameAdvanceTimestamp;
		private long _frameRewindTimestamp;
		private bool _frameRewindWasPaused;
		private bool _runloopFrameAdvance;
		private bool _lastFastForwardingOrRewinding;
		private bool _inResizeLoop;

		private readonly double _fpsUpdatesPerSecond = 4.0;
		private readonly double _fpsSmoothing = 8.0;
		private double _lastFps;
		private int _framesSinceLastFpsUpdate;
		private long _timestampLastFpsUpdate;

		private readonly Throttle _throttle;
		private bool _unthrottled;

		// For handling automatic pausing when entering the menu
		private bool _wasPaused;
		private bool _didMenuPause;

		private bool _cursorHidden;
		private bool _inFullscreen;
		private Point _windowedLocation;
		private bool _needsFullscreenOnLoad;

		private int _lastOpenRomFilter;

		private readonly ArgParser _argParser = new ArgParser();

		// Resources
		private Bitmap _statusBarDiskLightOnImage;
		private Bitmap _statusBarDiskLightOffImage;
		private Bitmap _linkCableOn;
		private Bitmap _linkCableOff;

		// input state which has been destined for game controller inputs are coalesced here
		// public static ControllerInputCoalescer ControllerInputCoalescer = new ControllerInputCoalescer();
		// input state which has been destined for client hotkey consumption are colesced here
		private readonly InputCoalescer _hotkeyCoalescer = new InputCoalescer();

		public PresentationPanel PresentationPanel { get; }

		// countdown for saveram autoflushing
		public int AutoFlushSaveRamIn { get; set; }
		#endregion

		#region Private methods

		private void SetStatusBar()
		{
			if (!_inFullscreen)
			{
				MainStatusBar.Visible = Config.DispChromeStatusBarWindowed;
				PerformLayout();
				FrameBufferResized();
			}
		}

		public void SetWindowText()
		{
			string str = "";

			if (_inResizeLoop)
			{
				var size = PresentationPanel.NativeSize;
				float ar = (float)size.Width / size.Height;
				str += $"({size.Width}x{size.Height})={ar} - ";
			}

			// we need to display FPS somewhere, in this case
			if (Config.DispSpeedupFeatures == 0)
			{
				str += $"({_lastFps:0} fps) - ";
			}

			if (!string.IsNullOrEmpty(VersionInfo.CustomBuildString))
			{
				str += $"{VersionInfo.CustomBuildString} ";
			}

			str += Emulator.IsNull() ? "BizHawk" : Emulator.System().DisplayName;

			if (VersionInfo.DeveloperBuild)
			{
				str += " (interim)";
			}

			if (!Emulator.IsNull())
			{
				str += $" - {Game.Name}";

				if (MovieSession.Movie.IsActive())
				{
					str += $" - {Path.GetFileName(MovieSession.Movie.Filename)}";
				}
			}

			if (!Config.DispChromeCaptionWindowed || _argParser._chromeless)
			{
				str = "";
			}

			Text = str;
		}

		private void ClearAutohold()
		{
			ClearHolds();
			AddOnScreenMessage("Autohold keys cleared");
		}

		private void UpdateToolsLoadstate()
		{
			if (Tools.Has<SNESGraphicsDebugger>())
			{
				Tools.SNESGraphicsDebugger.UpdateToolsLoadstate();
			}
		}

		private void UpdateToolsAfter(bool fromLua = false)
		{
			Tools.UpdateToolsAfter(fromLua);
			HandleToggleLightAndLink();
		}

		public void UpdateDumpIcon()
		{
			DumpStatusButton.Image = Properties.Resources.Blank;
			DumpStatusButton.ToolTipText = "";

			if (Emulator.IsNull() || Game.IsNullInstance())
			{
				return;
			}

			var status = Game.Status;
			if (status == RomStatus.BadDump)
			{
				DumpStatusButton.Image = Properties.Resources.ExclamationRed;
				DumpStatusButton.ToolTipText = "Warning: Bad ROM Dump";
			}
			else if (status == RomStatus.Overdump)
			{
				DumpStatusButton.Image = Properties.Resources.ExclamationRed;
				DumpStatusButton.ToolTipText = "Warning: Overdump";
			}
			else if (status == RomStatus.NotInDatabase)
			{
				DumpStatusButton.Image = Properties.Resources.RetroQuestion;
				DumpStatusButton.ToolTipText = "Warning: Unknown ROM";
			}
			else if (status == RomStatus.TranslatedRom)
			{
				DumpStatusButton.Image = Properties.Resources.Translation;
				DumpStatusButton.ToolTipText = "Translated ROM";
			}
			else if (status == RomStatus.Homebrew)
			{
				DumpStatusButton.Image = Properties.Resources.HomeBrew;
				DumpStatusButton.ToolTipText = "Homebrew ROM";
			}
			else if (Game.Status == RomStatus.Hack)
			{
				DumpStatusButton.Image = Properties.Resources.Hack;
				DumpStatusButton.ToolTipText = "Hacked ROM";
			}
			else if (Game.Status == RomStatus.Unknown)
			{
				DumpStatusButton.Image = Properties.Resources.Hack;
				DumpStatusButton.ToolTipText = "Warning: ROM of Unknown Character";
			}
			else
			{
				DumpStatusButton.Image = Properties.Resources.GreenCheck;
				DumpStatusButton.ToolTipText = "Verified good dump";
			}

			if (_multiDiskMode)
			{
				DumpStatusButton.ToolTipText = "Multi-disk bundler";
				DumpStatusButton.Image = Properties.Resources.RetroQuestion;
			}
		}

		private bool _multiDiskMode;

		// Rom details as decided by MainForm, which shouldn't happen, the RomLoader or Core should be doing this
		// Better is to just keep the game and rom hashes as properties and then generate the rom info from this
		private string _defaultRomDetails = "";

		private void LoadSaveRam()
		{
			if (Emulator.HasSaveRam())
			{
				try // zero says: this is sort of sketchy... but this is no time for rearchitecting
				{
					var saveRamPath = Config.PathEntries.SaveRamAbsolutePath(Game, MovieSession.Movie.IsActive());
					if (Config.AutosaveSaveRAM)
					{
						var saveram = new FileInfo(saveRamPath);
						var autosave = new FileInfo(Config.PathEntries.AutoSaveRamAbsolutePath(Game, MovieSession.Movie.IsActive()));
						if (autosave.Exists && autosave.LastWriteTime > saveram.LastWriteTime)
						{
							AddOnScreenMessage("AutoSaveRAM is newer than last saved SaveRAM");
						}
					}

					byte[] sram;

					// GBA meteor core might not know how big the saveram ought to be, so just send it the whole file
					// GBA vba-next core will try to eat anything, regardless of size
					if (Emulator is VBANext || Emulator is MGBAHawk || Emulator is NeoGeoPort)
					{
						sram = File.ReadAllBytes(saveRamPath);
					}
					else
					{
						var oldRam = Emulator.AsSaveRam().CloneSaveRam();
						if (oldRam == null)
						{
							// we're eating this one now. The possible negative consequence is that a user could lose
							// their saveram and not know why
							// MessageBox.Show("Error: tried to load saveram, but core would not accept it?");
							return;
						}

						// why do we silently truncate\pad here instead of warning\erroring?
						sram = new byte[oldRam.Length];
						using var reader = new BinaryReader(new FileStream(saveRamPath, FileMode.Open, FileAccess.Read));
						reader.Read(sram, 0, sram.Length);
					}

					Emulator.AsSaveRam().StoreSaveRam(sram);
					AutoFlushSaveRamIn = Config.FlushSaveRamFrames;
				}
				catch (IOException)
				{
					AddOnScreenMessage("An error occurred while loading Sram");
				}
			}
		}

		public bool FlushSaveRAM(bool autosave = false)
		{
			if (Emulator.HasSaveRam())
			{
				bool movieIsActive = MovieSession.Movie.IsActive();
				string path;
				if (autosave)
				{
					path = Config.PathEntries.AutoSaveRamAbsolutePath(Game, movieIsActive);
					AutoFlushSaveRamIn = Config.FlushSaveRamFrames;
				}
				else
				{
					path =  Config.PathEntries.SaveRamAbsolutePath(Game, movieIsActive);
				}

				var file = new FileInfo(path);
				var newPath = $"{path}.new";
				var newFile = new FileInfo(newPath);
				var backupPath = $"{path}.bak";
				var backupFile = new FileInfo(backupPath);
				if (file.Directory != null && !file.Directory.Exists)
				{
					try
					{
						file.Directory.Create();
					}
					catch
					{
						AddOnScreenMessage($"Unable to flush SaveRAM to: {newFile.Directory}");
						return false;
					}
				}

				using (var writer = new BinaryWriter(new FileStream(newPath, FileMode.Create, FileAccess.Write)))
				{
					var saveram = Emulator.AsSaveRam().CloneSaveRam();
					if (saveram != null)
					{
						writer.Write(saveram, 0, saveram.Length);
					}
				}

				if (file.Exists)
				{
					if (Config.BackupSaveram)
					{
						if (backupFile.Exists)
						{
							backupFile.Delete();
						}

						file.MoveTo(backupPath);
					}
					else
					{
						file.Delete();
					}
				}

				newFile.MoveTo(path);
			}

			return true;
		}

		private void RewireSound()
		{
			if (_dumpProxy != null)
			{
				// we're video dumping, so async mode only and use the DumpProxy.
				// note that the avi dumper has already rewired the emulator itself in this case.
				Sound.SetInputPin(_dumpProxy);
			}
			else
			{
				bool useAsyncMode = _currentSoundProvider.CanProvideAsync && !Config.SoundThrottle;
				_currentSoundProvider.SetSyncMode(useAsyncMode ? SyncSoundMode.Async : SyncSoundMode.Sync);
				Sound.SetInputPin(_currentSoundProvider);
			}
		}

		private void HandlePlatformMenus()
		{
			var system = "";
			if (!Game.IsNullInstance())
			{
				system = Emulator.SystemId;
			}

			TI83SubMenu.Visible = false;
			NESSubMenu.Visible = false;
			PCESubMenu.Visible = false;
			SMSSubMenu.Visible = false;
			GBSubMenu.Visible = false;
			GBASubMenu.Visible = false;
			AtariSubMenu.Visible = false;
			A7800SubMenu.Visible = false;
			SNESSubMenu.Visible = false;
			PSXSubMenu.Visible = false;
			ColecoSubMenu.Visible = false;
			N64SubMenu.Visible = false;
			SaturnSubMenu.Visible = false;
			DGBSubMenu.Visible = false;
			DGBHawkSubMenu.Visible = false;
			GB3xSubMenu.Visible = false;
			GB4xSubMenu.Visible = false;
			GGLSubMenu.Visible = false;
			GenesisSubMenu.Visible = false;
			wonderSwanToolStripMenuItem.Visible = false;
			AppleSubMenu.Visible = false;
			C64SubMenu.Visible = false;
			IntvSubMenu.Visible = false;
			virtualBoyToolStripMenuItem.Visible = false;
			sNESToolStripMenuItem.Visible = false;
			neoGeoPocketToolStripMenuItem.Visible = false;
			pCFXToolStripMenuItem.Visible = false;
			zXSpectrumToolStripMenuItem.Visible = false;
			amstradCPCToolStripMenuItem.Visible = false;
			VectrexSubMenu.Visible = false;
			MSXSubMenu.Visible = false;
			O2HawkSubMenu.Visible = false;
			arcadeToolStripMenuItem.Visible = false;

			switch (system)
			{
				case "GEN":
					GenesisSubMenu.Visible = true;
					break;
				case "TI83":
					TI83SubMenu.Visible = true;
					break;
				case "NES":
					NESSubMenu.Visible = true;
					break;
				case "PCE":
				case "PCECD":
				case "SGX":
					PCESubMenu.Visible = true;
					break;
				case "SMS":
					SMSSubMenu.Text = "&SMS";
					SMSSubMenu.Visible = true;
					break;
				case "SG":
					SMSSubMenu.Text = "&SG";
					SMSSubMenu.Visible = true;
					break;
				case "GG":
					SMSSubMenu.Text = "&GG";
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
				case "A78":
					A7800SubMenu.Visible = true;
					break;
				case "PSX":
					PSXSubMenu.Visible = true;
					break;
				case "SNES":
				case "SGB":
					if (Emulator is LibsnesCore)
					{
						SNESSubMenu.Text = ((LibsnesCore)Emulator).IsSGB ? "&SGB" : "&SNES";
						SNESSubMenu.Visible = true;
					}
					else if (Emulator is Snes9x)
					{
						sNESToolStripMenuItem.Visible = true;
					}
					else if (Emulator is Sameboy)
					{
						GBSubMenu.Visible = true;
					}
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
				case "DGB":
					if (Emulator is GBHawkLink)
					{
						DGBHawkSubMenu.Visible = true;
					}
					else
					{
						DGBSubMenu.Visible = true;
					}
					break;
				case "WSWAN":
					wonderSwanToolStripMenuItem.Visible = true;
					break;
				case "AppleII":
					AppleSubMenu.Visible = true;
					break;
				case "C64":
					C64SubMenu.Visible = true;
					break;
				case "INTV":
					IntvSubMenu.Visible = true;
					break;
				case "VB":
					virtualBoyToolStripMenuItem.Visible = true;
					break;
				case "NGP":
					neoGeoPocketToolStripMenuItem.Visible = true;
					break;
				case "PCFX":
					pCFXToolStripMenuItem.Visible = true;
					break;
				case "ZXSpectrum":
					zXSpectrumToolStripMenuItem.Visible = true;
#if DEBUG
					ZXSpectrumExportSnapshotMenuItemMenuItem.Visible = true;
#else
					ZXSpectrumExportSnapshotMenuItemMenuItem.Visible = false;
#endif
					break;
				case "AmstradCPC":
					amstradCPCToolStripMenuItem.Visible = true;
					break;
				case "GGL":
					GGLSubMenu.Visible = true;
					break;
				case "VEC":
					VectrexSubMenu.Visible = true;
					break;
				case "MSX":
					MSXSubMenu.Visible = true;
					break;
				case "O2":
					O2HawkSubMenu.Visible = true;
					break;
				case "GB3x":
					GB3xSubMenu.Visible = true;
					break;
				case "GB4x":
					GB4xSubMenu.Visible = true;
					break;
				case "MAME":
					arcadeToolStripMenuItem.Visible = true;
					break;
			}
		}

		private void InitControls()
		{
			var controls = new Controller(
				new ControllerDefinition
				{
					Name = "Emulator Frontend Controls",
					BoolButtons = Config.HotkeyBindings.Select(x => x.DisplayName).ToList()
				});

			foreach (var b in Config.HotkeyBindings)
			{
				controls.BindMulti(b.DisplayName, b.Bindings);
			}

			Global.ClientControls = controls;
			_autofireNullControls = new AutofireController(NullController.Instance.Definition, Emulator);
		}

		private void LoadMoviesFromRecent(string path)
		{
			if (File.Exists(path))
			{
				var movie = MovieService.Get(path);
				MovieSession.ReadOnly = true;
				StartNewMovie(movie, false);
			}
			else
			{
				Config.RecentMovies.HandleLoadError(path);
			}
		}

		private void LoadRomFromRecent(string rom)
		{
			var ioa = OpenAdvancedSerializer.ParseWithLegacy(rom);

			var args = new LoadRomArgs
			{
				OpenAdvanced = ioa
			};

			// if(ioa is this or that) - for more complex behaviour
			string romPath = ioa.SimplePath;

			if (!LoadRom(romPath, args))
			{
				Config.RecentRoms.HandleLoadError(romPath, rom);
			}
		}

		private void SetPauseStatusBarIcon()
		{
			if (EmulatorPaused)
			{
				PauseStatusButton.Image = Properties.Resources.Pause;
				PauseStatusButton.Visible = true;
				PauseStatusButton.ToolTipText = "Emulator Paused";
			}
			else if (IsTurboSeeking)
			{
				PauseStatusButton.Image = Properties.Resources.Lightning;
				PauseStatusButton.Visible = true;
				// ReSharper disable once PossibleInvalidOperationException
				PauseStatusButton.ToolTipText = $"Emulator is turbo seeking to frame {PauseOnFrame.Value} click to stop seek";
			}
			else if (PauseOnFrame.HasValue)
			{
				PauseStatusButton.Image = Properties.Resources.YellowRight;
				PauseStatusButton.Visible = true;
				PauseStatusButton.ToolTipText = $"Emulator is playing to frame {PauseOnFrame.Value} click to stop seek";
			}
			else
			{
				PauseStatusButton.Image = Properties.Resources.Blank;
				PauseStatusButton.Visible = false;
				PauseStatusButton.ToolTipText = "";
			}
		}

		private void SyncThrottle()
		{
			// "unthrottled" = throttle was turned off with "Toggle Throttle" hotkey
			// "turbo" = throttle is off due to the "Turbo" hotkey being held
			// They are basically the same thing but one is a toggle and the other requires a
			// hotkey to be held. There is however slightly different behavior in that turbo
			// skips outputting the audio. There's also a third way which is when no throttle
			// method is selected, but the clock throttle determines that by itself and
			// everything appears normal here.
			var rewind = Rewinder.RewindActive && (ClientControls["Rewind"] || PressRewind);
			var fastForward = ClientControls["Fast Forward"] || FastForward;
			var turbo = IsTurboing;

			int speedPercent = fastForward ? Config.SpeedPercentAlternate : Config.SpeedPercent;

			if (rewind)
			{
				speedPercent = Math.Max(speedPercent * Config.Rewind.SpeedMultiplier / Rewinder.RewindFrequency, 5);
			}

			Global.DisableSecondaryThrottling = _unthrottled || turbo || fastForward || rewind;

			// realtime throttle is never going to be so exact that using a double here is wrong
			_throttle.SetCoreFps(Emulator.VsyncRate());
			_throttle.signal_paused = EmulatorPaused;
			_throttle.signal_unthrottle = _unthrottled || turbo;

			// zero 26-mar-2016 - vsync and vsync throttle here both is odd, but see comments elsewhere about triple buffering
			_throttle.signal_overrideSecondaryThrottle = (fastForward || rewind) && (Config.SoundThrottle || Config.VSyncThrottle || Config.VSync);
			_throttle.SetSpeedPercent(speedPercent);
		}

		private void SetSpeedPercentAlternate(int value)
		{
			Config.SpeedPercentAlternate = value;
			SyncThrottle();
			AddOnScreenMessage($"Alternate Speed: {value}%");
		}

		private void SetSpeedPercent(int value)
		{
			Config.SpeedPercent = value;
			SyncThrottle();
			AddOnScreenMessage($"Speed: {value}%");
		}

		private void Shutdown()
		{
			if (_currAviWriter != null)
			{
				_currAviWriter.CloseFile();
				_currAviWriter = null;
			}
		}

		private static void CheckMessages()
		{
			Application.DoEvents();
			if (ActiveForm != null)
			{
				ScreenSaver.ResetTimerPeriodically();
			}
		}

		private void AutohideCursor(bool hide)
		{
			if (hide && !_cursorHidden)
			{
				PresentationPanel.Control.Cursor = Properties.Resources.BlankCursor;
				_cursorHidden = true;
			}
			else if (!hide && _cursorHidden)
			{
				PresentationPanel.Control.Cursor = Cursors.Default;
				timerMouseIdle.Stop();
				timerMouseIdle.Start();
				_cursorHidden = false;
			}
		}

		public BitmapBuffer MakeScreenshotImage()
		{
			return DisplayManager.RenderVideoProvider(_currentVideoProvider);
		}

		private void SaveSlotSelectedMessage()
		{
			int slot = Config.SaveSlot;
			string emptyPart = HasSlot(slot) ? "" : " (empty)";
			string message = $"Slot {slot}{emptyPart} selected.";
			AddOnScreenMessage(message);
		}

		private void Render()
		{
			if (Config.DispSpeedupFeatures == 0)
			{
				return;
			}

			var video = _currentVideoProvider;
			Size currVideoSize = new Size(video.BufferWidth, video.BufferHeight);
			Size currVirtualSize = new Size(video.VirtualWidth, video.VirtualHeight);


			bool resizeFramebuffer = currVideoSize != _lastVideoSize || currVirtualSize != _lastVirtualSize;

			bool isZero = currVideoSize.Width == 0 || currVideoSize.Height == 0 || currVirtualSize.Width == 0 || currVirtualSize.Height == 0;

			//don't resize if the new size is 0 somehow; we'll wait until we have a sensible size
			if (isZero)
			{
				resizeFramebuffer = false;
			}

			if (resizeFramebuffer)
			{
				_lastVideoSize = currVideoSize;
				_lastVirtualSize = currVirtualSize;
				FrameBufferResized();
			}

			//rendering flakes out egregiously if we have a zero size
			//can we fix it later not to?
			if (isZero)
				DisplayManager.Blank();
			else
				DisplayManager.UpdateSource(video);
		}

		// sends a simulation of a plain alt key keystroke
		private void SendPlainAltKey(int lparam)
		{
			var m = new Message { WParam = new IntPtr(0xF100), LParam = new IntPtr(lparam), Msg = 0x0112, HWnd = Handle };
			base.WndProc(ref m);
		}

		// sends an alt+mnemonic combination
		private void SendAltKeyChar(char c)
		{
			switch (OSTailoredCode.CurrentOS)
			{
				case OSTailoredCode.DistinctOS.Linux:
				case OSTailoredCode.DistinctOS.macOS:
					// no mnemonics for you
					break;
				case OSTailoredCode.DistinctOS.Windows:
					//HACK
					var _ = typeof(ToolStrip).InvokeMember(
						"ProcessMnemonicInternal",
						System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Instance,
						null,
						MainformMenu,
						new object[] { c });
					break;
			}
		}

		/// <remarks>TODO add and handle <see cref="FilesystemFilter.LuaScripts"/> (you can drag-and-drop scripts and there are already non-rom things in this list, so why not?)</remarks>
		private static readonly FilesystemFilterSet RomFSFilterSet = new FilesystemFilterSet(
			new FilesystemFilter("Music Files", new string[0], devBuildExtraExts: new[] { "psf", "minipsf", "sid", "nsf" }),
			new FilesystemFilter("Disc Images", new[] { "cue", "ccd", "mds", "m3u" }),
			new FilesystemFilter("NES", new[] { "nes", "fds", "unf", "nsf" }, addArchiveExts: true),
			new FilesystemFilter("Super NES", new[] { "smc", "sfc", "xml" }, addArchiveExts: true),
			new FilesystemFilter("PlayStation", new[] { "cue", "ccd", "mds", "m3u" }),
			new FilesystemFilter("PSX Executables (experimental)", new string[0], devBuildExtraExts: new[] { "exe" }),
			new FilesystemFilter("PSF Playstation Sound File", new[] { "psf", "minipsf" }),
			new FilesystemFilter("Nintendo 64", new[] { "z64", "v64", "n64" }),
			new FilesystemFilter("Gameboy", new[] { "gb", "gbc", "sgb" }, addArchiveExts: true),
			new FilesystemFilter("Gameboy Advance", new[] { "gba" }, addArchiveExts: true),
			new FilesystemFilter("Master System", new[] { "sms", "gg", "sg" }, addArchiveExts: true),
			new FilesystemFilter("PC Engine", new[] { "pce", "sgx", "cue", "ccd", "mds" }, addArchiveExts: true),
			new FilesystemFilter("Atari 2600", new[] { "a26" }, devBuildExtraExts: new[] { "bin" }, addArchiveExts: true),
			new FilesystemFilter("Atari 7800", new[] { "a78" }, devBuildExtraExts: new[] { "bin" }, addArchiveExts: true),
			new FilesystemFilter("Atari Lynx", new[] { "lnx" }, addArchiveExts: true),
			new FilesystemFilter("ColecoVision", new[] { "col" }, addArchiveExts: true),
			new FilesystemFilter("IntelliVision", new[] { "int", "bin", "rom" }, addArchiveExts: true),
			new FilesystemFilter("TI-83", new[] { "rom" }, addArchiveExts: true),
			FilesystemFilter.Archives,
			new FilesystemFilter("Genesis", new[] { "gen", "md", "smd", "32x", "bin", "cue", "ccd" }, addArchiveExts: true),
			new FilesystemFilter("SID Commodore 64 Music File", new string[0], devBuildExtraExts: new[] { "sid" }, devBuildAddArchiveExts: true),
			new FilesystemFilter("WonderSwan", new[] { "ws", "wsc" }, addArchiveExts: true),
			new FilesystemFilter("Apple II", new[] { "dsk", "do", "po" }, addArchiveExts: true),
			new FilesystemFilter("Virtual Boy", new[] { "vb" }, addArchiveExts: true),
			new FilesystemFilter("Neo Geo Pocket", new[] { "ngp", "ngc" }, addArchiveExts: true),
			new FilesystemFilter("Commodore 64", new[] { "prg", "d64", "g64", "crt", "tap" }, addArchiveExts: true),
			new FilesystemFilter("Amstrad CPC", new string[0], devBuildExtraExts: new[] { "cdt", "dsk" }, devBuildAddArchiveExts: true),
			new FilesystemFilter("Sinclair ZX Spectrum", new[] { "tzx", "tap", "dsk", "pzx", "csw", "wav" }, addArchiveExts: true),
			new FilesystemFilter("Odyssey 2", new[] { "o2" }),
			FilesystemFilter.EmuHawkSaveStates
		);

		private static readonly IReadOnlyCollection<string> KnownRomExtensions = RomFSFilterSet.Filters
			.SelectMany(f => f.Extensions)
			.Distinct()
			.Except(FilesystemFilter.ArchiveExtensions.Concat(new[] { "State" }))
			.Select(s => $".{s.ToUpperInvariant()}") // this is what's expected at call-site
			.ToList();

		public static readonly string RomFilter = RomFSFilterSet.ToString("Everything");

		public static readonly string ConfigFileFSFilterString = new FilesystemFilter("Config File", new[] { "ini" }).ToString();

		private void OpenRom()
		{
			using var ofd = new OpenFileDialog
			{
				InitialDirectory = Config.PathEntries.RomAbsolutePath(Emulator.SystemId),
				Filter = RomFilter,
				RestoreDirectory = false,
				FilterIndex = _lastOpenRomFilter
			};

			var result = ofd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return;
			}

			var file = new FileInfo(ofd.FileName);
			_lastOpenRomFilter = ofd.FilterIndex;

			var lra = new LoadRomArgs { OpenAdvanced = new OpenAdvanced_OpenRom { Path = file.FullName } };
			LoadRom(file.FullName, lra);
		}

		private void CoreSyncSettings(object sender, RomLoader.SettingsLoadArgs e)
		{
			if (MovieSession.QueuedMovie != null)
			{
				if (!string.IsNullOrWhiteSpace(MovieSession.QueuedMovie.SyncSettingsJson))
				{
					e.Settings = ConfigService.LoadWithType(MovieSession.QueuedMovie.SyncSettingsJson);
				}
				else
				{
					e.Settings = Config.GetCoreSyncSettings(e.Core);

					// Only show this nag if the core actually has sync settings, not all cores do
					if (e.Settings != null && !_suppressSyncSettingsWarning)
					{
						MessageBox.Show(
						"No sync settings found, using currently configured settings for this core.",
						"No sync settings found",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);
					}
				}
			}
			else
			{
				e.Settings = Config.GetCoreSyncSettings(e.Core);
			}
		}

		private void CoreSettings(object sender, RomLoader.SettingsLoadArgs e)
		{
			e.Settings = Config.GetCoreSettings(e.Core);
		}

		/// <summary>
		/// send core settings to emu, setting reboot flag if needed
		/// </summary>
		public void PutCoreSettings(object o)
		{
			var settable = new SettingsAdapter(Emulator);
			if (settable.HasSettings && settable.PutSettings(o))
			{
				FlagNeedsReboot();
			}
		}

		// TODO: Get/Put settings/sync settings methods could become a service we instantiate and use and pass to other forms
		/// <summary>
		/// send core sync settings to emu, setting reboot flag if needed
		/// </summary>
		public void PutCoreSyncSettings(object o)
		{
			var settable = new SettingsAdapter(Emulator);
			if (MovieSession.Movie.IsActive())
			{
				AddOnScreenMessage("Attempt to change sync-relevant settings while recording BLOCKED.");
			}
			else if (settable.HasSyncSettings && settable.PutSyncSettings(o))
			{
				FlagNeedsReboot();
			}
		}

		private void SaveConfig(string path = "")
		{
			if (Config.SaveWindowPosition)
			{
				if (Config.MainWndx != -32000) // When minimized location is -32000, don't save this into the config file!
				{
					Config.MainWndx = Location.X;
				}

				if (Config.MainWndy != -32000)
				{
					Config.MainWndy = Location.Y;
				}
			}
			else
			{
				Config.MainWndx = -1;
				Config.MainWndy = -1;
			}

			if (string.IsNullOrEmpty(path))
			{
				path = Config.DefaultIniPath;
			}

			ConfigService.Save(path, Config);
		}

		private void ToggleFps()
		{
			Config.DisplayFps ^= true;
		}

		private void ToggleFrameCounter()
		{
			Config.DisplayFrameCounter ^= true;
		}

		private void ToggleLagCounter()
		{
			Config.DisplayLagCounter ^= true;
		}

		private void ToggleInputDisplay()
		{
			Config.DisplayInput ^= true;
		}

		public void ToggleSound()
		{
			Config.SoundEnabled ^= true;
			Sound.StopSound();
			Sound.StartSound();
		}

		private void VolumeUp()
		{
			Config.SoundVolume += 10;
			if (Config.SoundVolume > 100)
			{
				Config.SoundVolume = 100;
			}

			AddOnScreenMessage($"Volume {Config.SoundVolume}");
		}

		private void VolumeDown()
		{
			Config.SoundVolume -= 10;
			if (Config.SoundVolume < 0)
			{
				Config.SoundVolume = 0;
			}

			AddOnScreenMessage($"Volume {Config.SoundVolume}");
		}

		private void SoftReset()
		{
			// is it enough to run this for one frame? maybe..
			if (Emulator.ControllerDefinition.BoolButtons.Contains("Reset"))
			{
				if (MovieSession.Movie.Mode != MovieMode.Play)
				{
					ClickyVirtualPadController.Click("Reset");
					AddOnScreenMessage("Reset button pressed.");
				}
			}
		}

		private void HardReset()
		{
			// is it enough to run this for one frame? maybe..
			if (Emulator.ControllerDefinition.BoolButtons.Contains("Power"))
			{
				if (MovieSession.Movie.Mode != MovieMode.Play)
				{
					ClickyVirtualPadController.Click("Power");
					AddOnScreenMessage("Power button pressed.");
				}
			}
		}

		private Color SlotForeColor(int slot)
		{
			return HasSlot(slot)
				? Config.SaveSlot == slot
					? SystemColors.HighlightText
					: SystemColors.WindowText
				: SystemColors.GrayText;
		}

		private Color SlotBackColor(int slot)
		{
			return  Config.SaveSlot == slot
				? SystemColors.Highlight
				: SystemColors.Control;
		}

		public void UpdateStatusSlots()
		{
			_stateSlots.Update(SaveStatePrefix());

			Slot0StatusButton.ForeColor = SlotForeColor(0);
			Slot1StatusButton.ForeColor = SlotForeColor(1);
			Slot2StatusButton.ForeColor = SlotForeColor(2);
			Slot3StatusButton.ForeColor = SlotForeColor(3);
			Slot4StatusButton.ForeColor = SlotForeColor(4);
			Slot5StatusButton.ForeColor = SlotForeColor(5);
			Slot6StatusButton.ForeColor = SlotForeColor(6);
			Slot7StatusButton.ForeColor = SlotForeColor(7);
			Slot8StatusButton.ForeColor = SlotForeColor(8);
			Slot9StatusButton.ForeColor = SlotForeColor(9);

			Slot0StatusButton.BackColor = SlotBackColor(0);
			Slot1StatusButton.BackColor = SlotBackColor(1);
			Slot2StatusButton.BackColor = SlotBackColor(2);
			Slot3StatusButton.BackColor = SlotBackColor(3);
			Slot4StatusButton.BackColor = SlotBackColor(4);
			Slot5StatusButton.BackColor = SlotBackColor(5);
			Slot6StatusButton.BackColor = SlotBackColor(6);
			Slot7StatusButton.BackColor = SlotBackColor(7);
			Slot8StatusButton.BackColor = SlotBackColor(8);
			Slot9StatusButton.BackColor = SlotBackColor(9);

			SaveSlotsStatusLabel.Visible =
				Slot0StatusButton.Visible =
				Slot1StatusButton.Visible =
				Slot2StatusButton.Visible =
				Slot3StatusButton.Visible =
				Slot4StatusButton.Visible =
				Slot5StatusButton.Visible =
				Slot6StatusButton.Visible =
				Slot7StatusButton.Visible =
				Slot8StatusButton.Visible =
				Slot9StatusButton.Visible =
				Emulator.HasSavestates();
		}

		public BitmapBuffer CaptureOSD()
		{
			var bb = DisplayManager.RenderOffscreen(_currentVideoProvider, true);
			bb.DiscardAlpha();
			return bb;
		}

		private void IncreaseWindowSize()
		{
			switch (Config.TargetZoomFactors[Emulator.SystemId])
			{
				case 1:
					Config.TargetZoomFactors[Emulator.SystemId] = 2;
					break;
				case 2:
					Config.TargetZoomFactors[Emulator.SystemId] = 3;
					break;
				case 3:
					Config.TargetZoomFactors[Emulator.SystemId] = 4;
					break;
				case 4:
					Config.TargetZoomFactors[Emulator.SystemId] = 5;
					break;
				case 5:
					Config.TargetZoomFactors[Emulator.SystemId] = 10;
					break;
				case 10:
					return;
			}

			AddOnScreenMessage($"Screensize set to {Config.TargetZoomFactors[Emulator.SystemId]}x");
			FrameBufferResized();
		}

		private void DecreaseWindowSize()
		{
			switch (Config.TargetZoomFactors[Emulator.SystemId])
			{
				case 1:
					return;
				case 2:
					Config.TargetZoomFactors[Emulator.SystemId] = 1;
					break;
				case 3:
					Config.TargetZoomFactors[Emulator.SystemId] = 2;
					break;
				case 4:
					Config.TargetZoomFactors[Emulator.SystemId] = 3;
					break;
				case 5:
					Config.TargetZoomFactors[Emulator.SystemId] = 4;
					break;
				case 10:
					Config.TargetZoomFactors[Emulator.SystemId] = 5;
					return;
			}

			AddOnScreenMessage($"Screensize set to {Config.TargetZoomFactors[Emulator.SystemId]}x");
			FrameBufferResized();
		}

		private static readonly int[] SpeedPercents = { 1, 3, 6, 12, 25, 50, 75, 100, 150, 200, 300, 400, 800, 1600, 3200, 6400 };

		private void IncreaseSpeed()
		{
			if (!Config.ClockThrottle)
			{
				AddOnScreenMessage("Unable to change speed, please switch to clock throttle");
				return;
			}

			var oldPercent = Config.SpeedPercent;
			int newPercent;

			int i = 0;
			do
			{
				i++;
				newPercent = SpeedPercents[i];
			}
			while (newPercent <= oldPercent && i < SpeedPercents.Length - 1);

			SetSpeedPercent(newPercent);
		}

		private void DecreaseSpeed()
		{
			if (!Config.ClockThrottle)
			{
				AddOnScreenMessage("Unable to change speed, please switch to clock throttle");
				return;
			}

			var oldPercent = Config.SpeedPercent;
			int newPercent;

			int i = SpeedPercents.Length - 1;
			do
			{
				i--;
				newPercent = SpeedPercents[i];
			}
			while (newPercent >= oldPercent && i > 0);

			SetSpeedPercent(newPercent);
		}

		private void SaveMovie()
		{
			if (MovieSession.Movie.IsActive())
			{
				MovieSession.Movie.Save();
				AddOnScreenMessage($"{MovieSession.Movie.Filename} saved.");
			}
		}

		private void HandleToggleLightAndLink()
		{
			if (MainStatusBar.Visible)
			{
				var hasDriveLight = Emulator.HasDriveLight() && Emulator.AsDriveLight().DriveLightEnabled;

				if (hasDriveLight)
				{
					if (!LedLightStatusLabel.Visible)
					{
						LedLightStatusLabel.Visible = true;
					}

					LedLightStatusLabel.Image = Emulator.AsDriveLight().DriveLightOn
						? _statusBarDiskLightOnImage
						: _statusBarDiskLightOffImage;
				}
				else
				{
					if (LedLightStatusLabel.Visible)
					{
						LedLightStatusLabel.Visible = false;
					}
				}

				if (Emulator.UsesLinkCable())
				{
					if (!LinkConnectStatusBarButton.Visible)
					{
						LinkConnectStatusBarButton.Visible = true;
					}

					LinkConnectStatusBarButton.Image = Emulator.AsLinkable().LinkConnected
						? _linkCableOn
						: _linkCableOff;

					LinkConnectStatusBarButton.ToolTipText = $"Link connection is currently {(Emulator.AsLinkable().LinkConnected ? "enabled" : "disabled")}";
				}
				else
				{
					if (LinkConnectStatusBarButton.Visible)
					{
						LinkConnectStatusBarButton.Visible = false;
					}
				}
			}
		}

		private void UpdateKeyPriorityIcon()
		{
			switch (Config.InputHotkeyOverrideOptions)
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
			Config.MoviePlaybackPokeMode ^= true;
			AddOnScreenMessage($"Movie Poke mode {(Config.MoviePlaybackPokeMode ? "enabled" : "disabled")}");
		}

		private void ToggleBackgroundInput()
		{
			Config.AcceptBackgroundInput ^= true;
			AddOnScreenMessage($"Background Input {(Config.AcceptBackgroundInput ? "enabled" : "disabled")}");
		}

		private void VsyncMessage()
		{
			AddOnScreenMessage($"Display Vsync set to {(Config.VSync ? "on" : "off")}");
		}

		private static bool StateErrorAskUser(string title, string message)
		{
			var result = MessageBox.Show(
				message,
				title,
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question);

			return result == DialogResult.Yes;
		}

		private void FdsInsertDiskMenuAdd(string name, string button, string msg)
		{
			FDSControlsMenuItem.DropDownItems.Add(name, null, delegate
			{
				if (Emulator.ControllerDefinition.BoolButtons.Contains(button))
				{
					if (MovieSession.Movie.Mode != MovieMode.Play)
					{
						ClickyVirtualPadController.Click(button);
						AddOnScreenMessage(msg);
					}
				}
			});
		}

		private const int WmDeviceChange = 0x0219;

		// Alt key hacks
		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case WmDeviceChange:
					GamePad.Initialize(this);
					GamePad360.Initialize();
					break;
			}

			// this is necessary to trap plain alt keypresses so that only our hotkey system gets them
			if (m.Msg == 0x0112) // WM_SYSCOMMAND
			{
				if (m.WParam.ToInt32() == 0xF100) // SC_KEYMENU
				{
					return;
				}
			}

			base.WndProc(ref m);
		}

		protected override bool ProcessDialogChar(char charCode)
		{
			// this is necessary to trap alt+char combinations so that only our hotkey system gets them
			return (ModifierKeys & Keys.Alt) != 0 || base.ProcessDialogChar(charCode);
		}

		private void UpdateCoreStatusBarButton()
		{
			if (Emulator.IsNull())
			{
				CoreNameStatusBarButton.Visible = false;
				return;
			}

			CoreNameStatusBarButton.Visible = true;
			var attributes = Emulator.Attributes();

			CoreNameStatusBarButton.Text = Emulator.DisplayName();
			CoreNameStatusBarButton.Image = Emulator.Icon();
			CoreNameStatusBarButton.ToolTipText = attributes.Ported ? "(ported) " : "";


			if (Emulator.SystemId == "ZXSpectrum")
			{
				var core = (Emulation.Cores.Computers.SinclairSpectrum.ZXSpectrum)Emulator;
				CoreNameStatusBarButton.ToolTipText = core.GetMachineType();
			}

			if (Emulator.SystemId == "AmstradCPC")
			{
				var core = (Emulation.Cores.Computers.AmstradCPC.AmstradCPC)Emulator;
				CoreNameStatusBarButton.ToolTipText = core.GetMachineType();
			}
		}

		private void ToggleKeyPriority()
		{
			Config.InputHotkeyOverrideOptions++;
			if (Config.InputHotkeyOverrideOptions > 2)
			{
				Config.InputHotkeyOverrideOptions = 0;
			}

			UpdateKeyPriorityIcon();
			switch (Config.InputHotkeyOverrideOptions)
			{
				case 0:
					AddOnScreenMessage("Key priority set to Both Hotkey and Input");
					break;
				case 1:
					AddOnScreenMessage("Key priority set to Input over Hotkey");
					break;
				case 2:
					AddOnScreenMessage("Key priority set to Input");
					break;
			}
		}

		#endregion

		#region Frame Loop

		private void StepRunLoop_Throttle()
		{
			SyncThrottle();
			_throttle.signal_frameAdvance = _runloopFrameAdvance;
			_throttle.signal_continuousFrameAdvancing = _runloopFrameProgress;

			_throttle.Step(true, -1);
		}

		public void FrameAdvance()
		{
			PressFrameAdvance = true;
			StepRunLoop_Core(true);
		}

		public void SeekFrameAdvance()
		{
			PressFrameAdvance = true;
			StepRunLoop_Core(true);
			PressFrameAdvance = false;
		}

		public bool IsLagFrame => Emulator.CanPollInput() && Emulator.AsInputPollable().IsLagFrame;

		private void StepRunLoop_Core(bool force = false)
		{
			var runFrame = false;
			_runloopFrameAdvance = false;
			var currentTimestamp = Stopwatch.GetTimestamp();

			double frameAdvanceTimestampDeltaMs = (double)(currentTimestamp - _frameAdvanceTimestamp) / Stopwatch.Frequency * 1000.0;
			bool frameProgressTimeElapsed = frameAdvanceTimestampDeltaMs >= Config.FrameProgressDelayMs;

			if (Config.SkipLagFrame && IsLagFrame && frameProgressTimeElapsed && Emulator.Frame > 0)
			{
				runFrame = true;
			}

			if (ClientControls["Frame Advance"] || PressFrameAdvance || HoldFrameAdvance)
			{
				_runloopFrameAdvance = true;

				// handle the initial trigger of a frame advance
				if (_frameAdvanceTimestamp == 0)
				{
					PauseEmulator();
					runFrame = true;
					_frameAdvanceTimestamp = currentTimestamp;
				}
				else
				{
					// handle the timed transition from countdown to FrameProgress
					if (frameProgressTimeElapsed)
					{
						runFrame = true;
						_runloopFrameProgress = true;
						UnpauseEmulator();
					}
				}
			}
			else
			{
				// handle release of frame advance: do we need to deactivate FrameProgress?
				if (_runloopFrameProgress)
				{
					_runloopFrameProgress = false;
					PauseEmulator();
				}

				_frameAdvanceTimestamp = 0;
			}

			if (!EmulatorPaused)
			{
				runFrame = true;
			}

			bool isRewinding = Rewind(ref runFrame, currentTimestamp, out var returnToRecording);

			float atten = 0;

			if (runFrame || force)
			{
				var isFastForwarding = ClientControls["Fast Forward"] || IsTurboing || InvisibleEmulation;
				var isFastForwardingOrRewinding = isFastForwarding || isRewinding || _unthrottled;

				if (isFastForwardingOrRewinding != _lastFastForwardingOrRewinding)
				{
					InitializeFpsData();
				}

				_lastFastForwardingOrRewinding = isFastForwardingOrRewinding;

				// client input-related duties
				GlobalWin.OSD.ClearGuiText();

				CheatList.Pulse();

				// zero 03-may-2014 - moved this before call to UpdateToolsBefore(), since it seems to clear the state which a lua event.framestart is going to want to alter
				ClickyVirtualPadController.FrameTick();
				Global.ButtonOverrideAdapter.FrameTick();

				if (Tools.Has<LuaConsole>() && !SuppressLua)
				{
					Tools.LuaConsole.LuaImp.CallFrameBeforeEvent();
				}

				if (IsTurboing)
				{
					Tools.FastUpdateBefore();
				}
				else
				{
					Tools.UpdateToolsBefore();
				}

				if (!InvisibleEmulation)
				{
					CaptureRewind(isRewinding);
				}

				// Set volume, if enabled
				if (Config.SoundEnabledNormal && !InvisibleEmulation)
				{
					atten = Config.SoundVolume / 100.0f;

					if (isFastForwardingOrRewinding)
					{
						if (Config.SoundEnabledRWFF)
						{
							atten *= Config.SoundVolumeRWFF / 100.0f;
						}
						else
						{
							atten = 0;
						}
					}

					// Mute if using Frame Advance/Frame Progress
					if (_runloopFrameAdvance && Config.MuteFrameAdvance)
					{
						atten = 0;
					}
				}

				MovieSession.HandleMovieOnFrameLoop();

				if (Config.AutosaveSaveRAM)
				{
					if (AutoFlushSaveRamIn-- <= 0)
					{
						FlushSaveRAM(true);
					}
				}
				// why not skip audio if the user doesn't want sound
				bool renderSound = (Config.SoundEnabled && !IsTurboing)
					|| (_currAviWriter?.UsesAudio ?? false);
				if (!renderSound)
				{
					atten = 0;
				}

				bool render = !InvisibleEmulation && (!_throttle.skipNextFrame || (_currAviWriter?.UsesVideo ?? false));
				bool newFrame = Emulator.FrameAdvance(Global.ControllerOutput, render, renderSound);

				MovieSession.HandleMovieAfterFrameLoop();

				if (returnToRecording)
				{
					MovieSession.Movie.SwitchToRecord();
				}

				if (isRewinding && !IsRewindSlave && MovieSession.Movie.IsRecording())
				{
					MovieSession.Movie.Truncate(Global.Emulator.Frame);
				}

				CheatList.Pulse();

				if (IsLagFrame && Config.AutofireLagFrames)
				{
					AutoFireController.IncrementStarts();
				}

				Global.AutofireStickyXORAdapter.IncrementLoops(IsLagFrame);

				PressFrameAdvance = false;

				if (Tools.Has<LuaConsole>() && !SuppressLua)
				{
					Tools.LuaConsole.LuaImp.CallFrameAfterEvent();
				}

				if (IsTurboing)
				{
					Tools.FastUpdateAfter(SuppressLua);
				}
				else
				{
					UpdateToolsAfter(SuppressLua);
				}

				if (!PauseAvi && newFrame && !InvisibleEmulation)
				{
					AvFrameAdvance();
				}

				if (newFrame)
				{
					_framesSinceLastFpsUpdate++;

					UpdateFpsDisplay(currentTimestamp, isRewinding, isFastForwarding);
				}

				if (Tools.IsLoaded<TAStudio>() &&
					Tools.TAStudio.LastPositionFrame == Emulator.Frame)
				{
					if (PauseOnFrame.HasValue &&
						PauseOnFrame.Value <= Tools.TAStudio.LastPositionFrame)
					{
						TasMovieRecord record = (MovieSession.Movie as TasMovie)[Emulator.Frame];
						if (!record.Lagged.HasValue && IsSeeking)
						{
							// haven't yet greenzoned the frame, hence it's after editing
							// then we want to pause here. taseditor fashion
							PauseEmulator();
						}
					}
				}

				if (IsSeeking && Emulator.Frame == PauseOnFrame.Value)
				{
					PauseEmulator();
					if (Tools.IsLoaded<TAStudio>())
					{
						Tools.TAStudio.StopSeeking();
					}
					PauseOnFrame = null;
				}
			}

			if (ClientControls["Rewind"] || PressRewind)
			{
				UpdateToolsAfter();
			}

			Sound.UpdateSound(atten);
		}

		private void UpdateFpsDisplay(long currentTimestamp, bool isRewinding, bool isFastForwarding)
		{
			double elapsedSeconds = (currentTimestamp - _timestampLastFpsUpdate) / (double)Stopwatch.Frequency;

			if (elapsedSeconds < 1.0 / _fpsUpdatesPerSecond)
			{
				return;
			}

			if (_lastFps == 0) // Initial calculation
			{
				_lastFps = (_framesSinceLastFpsUpdate - 1) / elapsedSeconds;
			}
			else
			{
				_lastFps = (_lastFps + (_framesSinceLastFpsUpdate * _fpsSmoothing)) / (1.0 + (elapsedSeconds * _fpsSmoothing));
			}

			_framesSinceLastFpsUpdate = 0;
			_timestampLastFpsUpdate = currentTimestamp;

			var fpsString = $"{_lastFps:0} fps";
			if (isRewinding)
			{
				fpsString += IsTurboing || isFastForwarding ?
					" <<<<" :
					" <<";
			}
			else if (isFastForwarding)
			{
				fpsString += IsTurboing ?
					" >>>>" :
					" >>";
			}

			GlobalWin.OSD.Fps = fpsString;

			// need to refresh window caption in this case
			if (Config.DispSpeedupFeatures == 0)
			{
				SetWindowText();
			}
		}

		private void InitializeFpsData()
		{
			_lastFps = 0;
			_timestampLastFpsUpdate = Stopwatch.GetTimestamp();
			_framesSinceLastFpsUpdate = 0;
		}

		#endregion

		#region AVI Stuff

		/// <summary>
		/// start AVI recording, unattended
		/// </summary>
		/// <param name="videoWriterName">match the short name of an <seealso cref="IVideoWriter"/></param>
		/// <param name="filename">filename to save to</param>
		private void RecordAv(string videoWriterName, string filename)
		{
			RecordAvBase(videoWriterName, filename, true);
		}

		/// <summary>
		/// start AV recording, asking user for filename and options
		/// </summary>
		private void RecordAv()
		{
			RecordAvBase(null, null, false);
		}

		/// <summary>
		/// start AV recording
		/// </summary>
		private void RecordAvBase(string videoWriterName, string filename, bool unattended)
		{
			if (_currAviWriter != null || OSTailoredCode.IsUnixHost) return;

			// select IVideoWriter to use
			IVideoWriter aw;

			if (string.IsNullOrEmpty(videoWriterName) && !string.IsNullOrEmpty(Config.VideoWriter))
			{
				videoWriterName = Config.VideoWriter;
			}

			_dumpaudiosync = Config.VideoWriterAudioSync;
			if (unattended && !string.IsNullOrEmpty(videoWriterName))
			{
				aw = VideoWriterInventory.GetVideoWriter(videoWriterName);
			}
			else
			{
				aw = VideoWriterChooserForm.DoVideoWriterChooserDlg(
					VideoWriterInventory.GetAllWriters(),
					this,
					Emulator,
					Config,
					out _avwriterResizew,
					out _avwriterResizeh,
					out _avwriterpad,
					ref _dumpaudiosync);
			}

			if (aw == null)
			{
				AddOnScreenMessage(
					unattended ? $"Couldn't start video writer \"{videoWriterName}\"" : "A/V capture canceled.");

				return;
			}

			try
			{
				bool usingAvi = aw is AviWriter; // SO GROSS!

				if (_dumpaudiosync)
				{
					aw = new VideoStretcher(aw);
				}
				else
				{
					aw = new AudioStretcher(aw);
				}

				aw.SetMovieParameters(Emulator.VsyncNumerator(), Emulator.VsyncDenominator());
				if (_avwriterResizew > 0 && _avwriterResizeh > 0)
				{
					aw.SetVideoParameters(_avwriterResizew, _avwriterResizeh);
				}
				else
				{
					aw.SetVideoParameters(_currentVideoProvider.BufferWidth, _currentVideoProvider.BufferHeight);
				}

				aw.SetAudioParameters(44100, 2, 16);

				// select codec token
				// do this before save dialog because ffmpeg won't know what extension it wants until it's been configured
				if (unattended && !string.IsNullOrEmpty(filename))
				{
					aw.SetDefaultVideoCodecToken();
				}
				else
				{
					// THIS IS REALLY SLOPPY!
					// PLEASE REDO ME TO NOT CARE WHICH AVWRITER IS USED!
					if (usingAvi && !string.IsNullOrEmpty(Config.AviCodecToken))
					{
						aw.SetDefaultVideoCodecToken();
					}

					var token = aw.AcquireVideoCodecToken(this);
					if (token == null)
					{
						AddOnScreenMessage("A/V capture canceled.");
						aw.Dispose();
						return;
					}

					aw.SetVideoCodecToken(token);
				}

				// select file to save to
				if (unattended && !string.IsNullOrEmpty(filename))
				{
					aw.OpenFile(filename);
				}
				else
				{
					string ext = aw.DesiredExtension();
					string pathForOpenFile;

					// handle directories first
					if (ext == "<directory>")
					{
						using var fbd = new FolderBrowserEx();
						if (fbd.ShowDialog() == DialogResult.Cancel)
						{
							aw.Dispose();
							return;
						}

						pathForOpenFile = fbd.SelectedPath;
					}
					else
					{
						using var sfd = new SaveFileDialog();
						if (Game != null)
						{
							sfd.FileName = $"{Game.Name.FilesystemSafeName()}.{ext}"; // don't use Path.ChangeExtension, it might wreck game names with dots in them
							sfd.InitialDirectory = Config.PathEntries.AvAbsolutePath();
						}
						else
						{
							sfd.FileName = "NULL";
							sfd.InitialDirectory = Config.PathEntries.AvAbsolutePath();
						}

						sfd.Filter = new FilesystemFilterSet(new FilesystemFilter(ext, new[] { ext })).ToString();

						var result = sfd.ShowHawkDialog();
						if (result == DialogResult.Cancel)
						{
							aw.Dispose();
							return;
						}

						pathForOpenFile = sfd.FileName;
					}

					aw.OpenFile(pathForOpenFile);
				}

				// commit the avi writing last, in case there were any errors earlier
				_currAviWriter = aw;
				AddOnScreenMessage("A/V capture started");
				AVIStatusLabel.Image = Properties.Resources.AVI;
				AVIStatusLabel.ToolTipText = "A/V capture in progress";
				AVIStatusLabel.Visible = true;
			}
			catch
			{
				AddOnScreenMessage("A/V capture failed!");
				aw.Dispose();
				throw;
			}

			if (_dumpaudiosync)
			{
				_currentSoundProvider.SetSyncMode(SyncSoundMode.Sync);
			}
			else
			{
				if (_currentSoundProvider.CanProvideAsync)
				{
					_currentSoundProvider.SetSyncMode(SyncSoundMode.Async);
					_aviSoundInputAsync = _currentSoundProvider;
				}
				else
				{
					_currentSoundProvider.SetSyncMode(SyncSoundMode.Sync);
					_aviSoundInputAsync = new SyncToAsyncProvider(_currentSoundProvider);
				}
			}

			_dumpProxy = new SimpleSyncSoundProvider();
			RewireSound();
		}

		private void AbortAv()
		{
			if (_currAviWriter == null)
			{
				_dumpProxy = null;
				RewireSound();
				return;
			}

			_currAviWriter.Dispose();
			_currAviWriter = null;
			AddOnScreenMessage("A/V capture aborted");
			AVIStatusLabel.Image = Properties.Resources.Blank;
			AVIStatusLabel.ToolTipText = "";
			AVIStatusLabel.Visible = false;
			_aviSoundInputAsync = null;
			_dumpProxy = null; // return to normal sound output
			RewireSound();
		}

		private void StopAv()
		{
			if (_currAviWriter == null)
			{
				_dumpProxy = null;
				RewireSound();
				return;
			}

			_currAviWriter.CloseFile();
			_currAviWriter.Dispose();
			_currAviWriter = null;
			AddOnScreenMessage("A/V capture stopped");
			AVIStatusLabel.Image = Properties.Resources.Blank;
			AVIStatusLabel.ToolTipText = "";
			AVIStatusLabel.Visible = false;
			_aviSoundInputAsync = null;
			_dumpProxy = null; // return to normal sound output
			RewireSound();
		}

		private void AvFrameAdvance()
		{
			if (_currAviWriter != null)
			{
				// TODO ZERO - this code is pretty jacked. we'll want to frugalize buffers better for speedier dumping, and we might want to rely on the GL layer for padding
				try
				{
					// is this the best time to handle this? or deeper inside?
					if (_argParser._currAviWriterFrameList != null)
					{
						if (!_argParser._currAviWriterFrameList.Contains(Emulator.Frame))
						{
							goto HANDLE_AUTODUMP;
						}
					}

					IVideoProvider output;
					IDisposable disposableOutput = null;
					if (_avwriterResizew > 0 && _avwriterResizeh > 0)
					{
						BitmapBuffer bbIn = null;
						Bitmap bmpIn = null;
						try
						{
							bbIn = Config.AviCaptureOsd
								? CaptureOSD()
								: new BitmapBuffer(_currentVideoProvider.BufferWidth, _currentVideoProvider.BufferHeight, _currentVideoProvider.GetVideoBuffer());

							bbIn.DiscardAlpha();

							var bmpOut = new Bitmap(_avwriterResizew, _avwriterResizeh, PixelFormat.Format32bppArgb);
							bmpIn = bbIn.ToSysdrawingBitmap();
							using (var g = Graphics.FromImage(bmpOut))
							{
								if (_avwriterpad)
								{
									g.Clear(Color.FromArgb(_currentVideoProvider.BackgroundColor));
									g.DrawImageUnscaled(bmpIn, (bmpOut.Width - bmpIn.Width) / 2, (bmpOut.Height - bmpIn.Height) / 2);
								}
								else
								{
									g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
									g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
									g.DrawImage(bmpIn, new Rectangle(0, 0, bmpOut.Width, bmpOut.Height));
								}
							}

							output = new BmpVideoProvider(bmpOut, _currentVideoProvider.VsyncNumerator, _currentVideoProvider.VsyncDenominator);
							disposableOutput = (IDisposable)output;
						}
						finally
						{
							bbIn?.Dispose();
							bmpIn?.Dispose();
						}
					}
					else
					{
						if (Config.AviCaptureOsd)
						{
							output = new BitmapBufferVideoProvider(CaptureOSD());
							disposableOutput = (IDisposable)output;
						}
						else
						{
							output = _currentVideoProvider;
						}
					}

					_currAviWriter.SetFrame(Emulator.Frame);

					short[] samp;
					int nsamp;
					if (_dumpaudiosync)
					{
						((VideoStretcher)_currAviWriter).DumpAV(output, _currentSoundProvider, out samp, out nsamp);
					}
					else
					{
						((AudioStretcher)_currAviWriter).DumpAV(output, _aviSoundInputAsync, out samp, out nsamp);
					}

					disposableOutput?.Dispose();

					_dumpProxy.PutSamples(samp, nsamp);
				}
				catch (Exception e)
				{
					MessageBox.Show($"Video dumping died:\n\n{e}");
					AbortAv();
				}

			HANDLE_AUTODUMP:
				if (_argParser._autoDumpLength > 0)
				{
					_argParser._autoDumpLength--;
					if (_argParser._autoDumpLength == 0) // finish
					{
						StopAv();
						if (_argParser._autoCloseOnDump)
						{
							_exitRequestPending = true;
						}
					}
				}
			}
		}

		private int? LoadArchiveChooser(HawkFile file)
		{
			using var ac = new ArchiveChooser(file);
			if (ac.ShowDialog(this) == DialogResult.OK)
			{
				return ac.SelectedMemberIndex;
			}

			return null;
		}

		#endregion

		#region Scheduled for refactor

		public string SaveStatePrefix()
		{
			var name = Game.Name.FilesystemSafeName();

			// Neshawk and Quicknes have incompatible savestates, store the name to keep them separate
			if (Emulator.SystemId == "NES")
			{
				name += $".{Emulator.Attributes().CoreName}";
			}

			// Gambatte and GBHawk have incompatible savestates, store the name to keep them separate
			if (Emulator.SystemId == "GB")
			{
				name += $".{Emulator.Attributes().CoreName}";
			}

			if (Emulator is Snes9x) // Keep snes9x savestate away from libsnes, we want to not be too tedious so bsnes names will just have the profile name not the core name
			{
				name += $".{Emulator.Attributes().CoreName}";
			}

			// Bsnes profiles have incompatible savestates so save the profile name
			if (Emulator is LibsnesCore bsnes)
			{
				name += $".{bsnes.CurrentProfile}";
			}

			if (Emulator.SystemId == "GBA")
			{
				name += $".{Emulator.Attributes().CoreName}";
			}

			if (MovieSession.Movie.IsActive())
			{
				name += $".{Path.GetFileNameWithoutExtension(MovieSession.Movie.Filename)}";
			}

			var pathEntry = Config.PathEntries.SaveStateAbsolutePath(Game.System);

			return Path.Combine(pathEntry, name);
		}

		private void ShowMessageCoreComm(string message)
		{
			MessageBox.Show(this, message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		private void ShowLoadError(object sender, RomLoader.RomErrorArgs e)
		{
			if (e.Type == RomLoader.LoadErrorType.MissingFirmware)
			{
				var result = MessageBox.Show(
					"You are missing the needed firmware files to load this Rom\n\nWould you like to open the firmware manager now and configure your firmwares?",
					e.Message,
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Error);
				if (result == DialogResult.Yes)
				{
					FirmwaresMenuItem_Click(null, e);
					if (e.Retry)
					{
						// Retry loading the ROM here. This leads to recursion, as the original call to LoadRom has not exited yet,
						// but unless the user tries and fails to set his firmware a lot of times, nothing should happen.
						// Refer to how RomLoader implemented its LoadRom method for a potential fix on this.
						LoadRom(e.RomPath, _currentLoadRomArgs);
					}
				}
			}
			else
			{
				string title = "load error";
				if (e.AttemptedCoreLoad != null)
				{
					title = $"{e.AttemptedCoreLoad} load error";
				}

				MessageBox.Show(this, e.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void NotifyCoreComm(string message)
		{
			AddOnScreenMessage(message);
		}

		private string ChoosePlatformForRom(RomGame rom)
		{
			using var platformChooser = new PlatformChooser(Config)
			{
				RomGame = rom
			};

			platformChooser.ShowDialog();
			return platformChooser.PlatformChoice;
		}

		public class LoadRomArgs
		{
			public bool? Deterministic { get; set; }
			public IOpenAdvanced OpenAdvanced { get; set; }
		}

		private LoadRomArgs _currentLoadRomArgs;

		public bool LoadRom(string path, LoadRomArgs args)
		{
			if (!LoadRomInternal(path, args))
				return false;

			// what's the meaning of the last rom path when opening an archive? based on the archive file location
			if (args.OpenAdvanced is OpenAdvanced_OpenRom)
			{
				var leftPart = path.Split('|')[0];
				Config.PathEntries.LastRomPath = Path.GetFullPath(Path.GetDirectoryName(leftPart) ?? "");
			}

			return true;
		}

		// Still needs a good bit of refactoring
		private bool LoadRomInternal(string path, LoadRomArgs args)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			if (args == null)
				throw new ArgumentNullException(nameof(args));

			path = EmuHawkUtil.ResolveShortcut(path);

			// if this is the first call to LoadRom (they will come in recursively) then stash the args
			bool firstCall = false;
			if (_currentLoadRomArgs == null)
			{
				firstCall = true;
				_currentLoadRomArgs = args;
			}
			else
			{
				args = _currentLoadRomArgs;
			}

			try
			{
				// movies should require deterministic emulation in ALL cases
				// if the core is managing its own DE through SyncSettings a 'deterministic' bool can be passed into the core's constructor
				// it is then up to the core itself to override its own local DeterministicEmulation setting
				bool deterministic = args.Deterministic ?? MovieSession.QueuedMovie != null;

				if (!Tools.AskSave())
				{
					return false;
				}

				var loader = new RomLoader
				{
					ChooseArchive = LoadArchiveChooser,
					ChoosePlatform = ChoosePlatformForRom,
					Deterministic = deterministic,
					MessageCallback = GlobalWin.OSD.AddMessage,
					OpenAdvanced = args.OpenAdvanced
				};
				Global.FirmwareManager.RecentlyServed.Clear();

				loader.OnLoadError += ShowLoadError;
				loader.OnLoadSettings += CoreSettings;
				loader.OnLoadSyncSettings += CoreSyncSettings;

				// this also happens in CloseGame(). But it needs to happen here since if we're restarting with the same core,
				// any settings changes that we made need to make it back to config before we try to instantiate that core with
				// the new settings objects
				CommitCoreSettingsToConfig(); // adelikat: I Think by reordering things, this isn't necessary anymore
				CloseGame();

				var nextComm = CreateCoreComm();

				IOpenAdvanced ioa = args.OpenAdvanced;
				var oaOpenrom = ioa as OpenAdvanced_OpenRom;
				var oaMame = ioa as OpenAdvanced_MAME;
				var oaRetro = ioa as OpenAdvanced_Libretro;
				var ioaRetro = ioa as IOpenAdvancedLibretro;

				// we need to inform LoadRom which Libretro core to use...
				if (ioaRetro != null)
				{
					// prepare a core specification
					// if it wasn't already specified, use the current default
					if (ioaRetro.CorePath == null)
					{
						ioaRetro.CorePath = Config.LibretroCore;
					}

					if (ioaRetro.CorePath == null)
					{
						throw new InvalidOperationException("Can't load a file via Libretro until a core is specified");
					}
				}

				if (oaOpenrom != null)
				{
					// path already has the right value, while ioa.Path is null (interestingly, these are swapped below)
					// I doubt null is meant to be assigned here, and it just prevents game load
					//path = ioa_openrom.Path;
				}

				var result = loader.LoadRom(path, nextComm, ioaRetro?.CorePath);

				// we need to replace the path in the OpenAdvanced with the canonical one the user chose.
				// It can't be done until loader.LoadRom happens (for CanonicalFullPath)
				// i'm not sure this needs to be more abstractly engineered yet until we have more OpenAdvanced examples
				if (oaRetro != null)
				{
					oaRetro.token.Path = loader.CanonicalFullPath;
				}

				if (oaOpenrom != null)
				{
					oaOpenrom.Path = loader.CanonicalFullPath;
				}

				if (oaMame != null)
				{
					oaMame.Path = loader.CanonicalFullPath;
				}

				if (result)
				{
					string openAdvancedArgs = $"*{OpenAdvancedSerializer.Serialize(ioa)}";
					Emulator = loader.LoadedEmulator;
					Global.Game = loader.Game;
					InputManager.SyncControls(Emulator, Config);

					if (oaOpenrom != null && Path.GetExtension(oaOpenrom.Path.Replace("|", "")).ToLowerInvariant() == ".xml" && !(Emulator is LibsnesCore))
					{
						// this is a multi-disk bundler file
						// determine the xml assets and create RomStatusDetails for all of them
						var xmlGame = XmlGame.Create(new HawkFile(oaOpenrom.Path));

						using var xSw = new StringWriter();

						for (int xg = 0; xg < xmlGame.Assets.Count; xg++)
						{
							var ext = Path.GetExtension(xmlGame.AssetFullPaths[xg])?.ToLowerInvariant();

							if (ext == ".cue" || ext == ".ccd" || ext == ".toc" || ext == ".mds")
							{
								xSw.WriteLine(Path.GetFileNameWithoutExtension(xmlGame.Assets[xg].Key));
								xSw.WriteLine("SHA1:N/A");
								xSw.WriteLine("MD5:N/A");
								xSw.WriteLine();
							}
							else
							{
								xSw.WriteLine(xmlGame.Assets[xg].Key);
								xSw.WriteLine($"SHA1:{xmlGame.Assets[xg].Value.HashSHA1()}");
								xSw.WriteLine($"MD5:{xmlGame.Assets[xg].Value.HashMD5()}");
								xSw.WriteLine();
							}
						}

						_defaultRomDetails = xSw.ToString();
						_multiDiskMode = true;
					}

					if (loader.LoadedEmulator is NES nes)
					{
						if (!string.IsNullOrWhiteSpace(nes.GameName))
						{
							Game.Name = nes.GameName;
						}

						Game.Status = nes.RomStatus;
					}
					else if (loader.LoadedEmulator is QuickNES qns)
					{
						if (!string.IsNullOrWhiteSpace(qns.BootGodName))
						{
							Game.Name = qns.BootGodName;
						}

						if (qns.BootGodStatus.HasValue)
						{
							Game.Status = qns.BootGodStatus.Value;
						}
					}

					var romDetails = Emulator.RomDetails();
					if (string.IsNullOrWhiteSpace(romDetails) && loader.Rom != null)
					{
						_defaultRomDetails = $"{loader.Game.Name}\r\nSHA1:{loader.Rom.RomData.HashSHA1()}\r\nMD5:{loader.Rom.RomData.HashMD5()}\r\n";
					}
					else if (string.IsNullOrWhiteSpace(romDetails) && loader.Rom == null)
					{
						// single disc game
						_defaultRomDetails = $"{loader.Game.Name}\r\nSHA1:N/A\r\nMD5:N/A\r\n";
					}

					if (Emulator.HasBoardInfo())
					{
						Console.WriteLine("Core reported BoardID: \"{0}\"", Emulator.AsBoardInfo().BoardName);
					}

					// restarts the lua console if a different rom is loaded.
					// im not really a fan of how this is done..
					if (Config.RecentRoms.Empty || Config.RecentRoms.MostRecent != openAdvancedArgs)
					{
						Tools.Restart<LuaConsole>();
					}

					Config.RecentRoms.Add(openAdvancedArgs);
					JumpLists.AddRecentItem(openAdvancedArgs, ioa.DisplayName);

					// Don't load Save Ram if a movie is being loaded
					if (!MovieSession.MovieIsQueued)
					{
						var movieIsActive = MovieSession.Movie.IsActive();
						if (File.Exists(Config.PathEntries.SaveRamAbsolutePath(loader.Game, movieIsActive)))
						{
							LoadSaveRam();
						}
						else if (Config.AutosaveSaveRAM && File.Exists(Config.PathEntries.SaveRamAbsolutePath(loader.Game, movieIsActive)))
						{
							AddOnScreenMessage("AutoSaveRAM found, but SaveRAM was not saved");
						}
					}

					Tools.Restart(Emulator);

					if (Config.LoadCheatFileByGame)
					{
						CheatList.SetDefaultFileName(Tools.GenerateDefaultCheatFilename());
						if (CheatList.AttemptToLoadCheatFile())
						{
							AddOnScreenMessage("Cheats file loaded");
						}
						else if (CheatList.Any())
						{
							CheatList.Clear();
						}
					}

					CurrentlyOpenRom = oaOpenrom?.Path ?? openAdvancedArgs;
					CurrentlyOpenRomArgs = args;
					OnRomChanged();
					DisplayManager.Blank();

					Rewinder.Initialize();

					Global.StickyXORAdapter.ClearStickies();
					Global.StickyXORAdapter.ClearStickyFloats();
					Global.AutofireStickyXORAdapter.ClearStickies();

					RewireSound();
					Tools.UpdateCheatRelatedTools(null, null);
					if (Config.AutoLoadLastSaveSlot && HasSlot(Config.SaveSlot))
					{
						LoadQuickSave($"QuickSave{Config.SaveSlot}");
					}

					if (FirmwareManager.RecentlyServed.Count > 0)
					{
						Console.WriteLine("Active Firmwares:");
						foreach (var f in FirmwareManager.RecentlyServed)
						{
							Console.WriteLine("  {0} : {1}", f.FirmwareId, f.Hash);
						}
					}

					ClientApi.OnRomLoaded(Emulator);
					return true;
				}
				else if (Emulator.IsNull())
				{
					// This shows up if there's a problem
					ClientApi.UpdateEmulatorAndVP(Emulator);
					OnRomChanged();
					return false;
				}
				else
				{
					// The ROM has been loaded by a recursive invocation of the LoadROM method.
					ClientApi.OnRomLoaded(Emulator);
					return true;
				}
			}
			finally
			{
				if (firstCall)
				{
					_currentLoadRomArgs = null;
				}
			}
		}

		private void OnRomChanged()
		{
			SetWindowText();
			HandlePlatformMenus();
			_stateSlots.ClearRedoList();
			UpdateStatusSlots();
			UpdateCoreStatusBarButton();
			UpdateDumpIcon();
			SetMainformMovieInfo();
		}

		private void CommitCoreSettingsToConfig()
		{
			// save settings object
			var t = Emulator.GetType();
			var settable = new SettingsAdapter(Emulator);

			if (settable.HasSettings)
			{
				Config.PutCoreSettings(settable.GetSettings(), t);
			}

			if (settable.HasSyncSettings && !MovieSession.Movie.IsActive())
			{
				// don't trample config with loaded-from-movie settings
				Config.PutCoreSyncSettings(settable.GetSyncSettings(), t);
			}
		}

		// whats the difference between these two methods??
		// its very tricky. rename to be more clear or combine them.
		// This gets called whenever a core related thing is changed.
		// Like reboot core.
		private void CloseGame(bool clearSram = false)
		{
			GameIsClosing = true;
			if (clearSram)
			{
				var path = Config.PathEntries.SaveRamAbsolutePath(Game, MovieSession.Movie.IsActive());
				if (File.Exists(path))
				{
					File.Delete(path);
					AddOnScreenMessage("SRAM cleared.");
				}
			}
			else if (Emulator.HasSaveRam() && Emulator.AsSaveRam().SaveRamModified)
			{
				if (!FlushSaveRAM())
				{
					var msgRes = MessageBox.Show("Failed flushing the game's Save RAM to your disk.\nClose without flushing Save RAM?",
							"Directory IO Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error);

					if (msgRes != DialogResult.Yes)
					{
						return;
					}
				}
			}

			StopAv();

			CommitCoreSettingsToConfig();
			if (MovieSession.Movie.IsActive()) // Note: this must be called after CommitCoreSettingsToConfig()
			{
				StopMovie();
			}

			Rewinder.Uninitialize();

			if (Tools.IsLoaded<TraceLogger>())
			{
				Tools.Get<TraceLogger>().Restart();
			}

			CheatList.SaveOnClose();
			Emulator.Dispose();
			Emulator = new NullEmulator();
			ClientApi.UpdateEmulatorAndVP(Emulator);
			Global.ActiveController = new Controller(NullController.Instance.Definition);
			Global.AutoFireController = _autofireNullControls;
			RewireSound();
			RebootStatusBarIcon.Visible = false;
			GameIsClosing = false;
		}

		public bool GameIsClosing { get; private set; } // Lets tools make better decisions when being called by CloseGame

		public void CloseRom(bool clearSram = false)
		{
			// This gets called after Close Game gets called.
			// Tested with NESHawk and SMB3 (U)
			if (Tools.AskSave())
			{
				CloseGame(clearSram);
				var coreComm = CreateCoreComm();
				Emulator = new NullEmulator();
				Global.Game = GameInfo.NullInstance;

				Tools.Restart(Emulator);
				RewireSound();
				ClearHolds();
				Tools.UpdateCheatRelatedTools(null, null);
				PauseOnFrame = null;
				CurrentlyOpenRom = null;
				CurrentlyOpenRomArgs = null;
				OnRomChanged();
			}
		}

		private void ProcessMovieImport(string fn, bool start)
		{
			var result = MovieImport.ImportFile(fn);

			if (result.Errors.Any())
			{
				MessageBox.Show(string.Join("\n", result.Errors), "Conversion error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			if (result.Warnings.Any())
			{
				AddOnScreenMessage(result.Warnings.First()); // For now, just show the first warning
			}

			AddOnScreenMessage($"{Path.GetFileName(fn)} imported as {result.Movie.Filename}");

			if (start)
			{
				StartNewMovie(result.Movie, false);
				Config.RecentMovies.Add(result.Movie.Filename);
			}
		}

		public void EnableRewind(bool enabled)
		{
			Rewinder.SuspendRewind = !enabled;
			AddOnScreenMessage($"Rewind {(enabled ? "enabled" : "suspended")}");
		}

		public void ClearRewindData()
		{
			Rewinder.Clear();
		}

		#endregion

		#region Tool Control API

		// TODO: move me
		public IControlMainform Master { get; private set; }

		private bool IsSlave => Master != null;

		private bool IsSavestateSlave => IsSlave && Master.WantsToControlSavestates;

		private bool IsRewindSlave => IsSlave && Master.WantsToControlRewind;

		public void RelinquishControl(IControlMainform master)
		{
			Master = master;
		}

		public void TakeBackControl()
		{
			Master = null;
		}

		private int SlotToInt(string slot)
		{
			return int.Parse(slot.Substring(slot.Length - 1, 1));
		}

		public void LoadState(string path, string userFriendlyStateName, bool fromLua = false, bool suppressOSD = false) // Move to client.common
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			if (IsSavestateSlave)
			{
				Master.LoadState();
				return;
			}

			// If from lua, disable counting rerecords
			bool wasCountingRerecords = MovieSession.Movie.IsCountingRerecords;

			if (fromLua)
			{
				MovieSession.Movie.IsCountingRerecords = false;
			}

			if (SavestateManager.LoadStateFile(path, userFriendlyStateName))
			{
				GlobalWin.OSD.ClearGuiText();
				ClientApi.OnStateLoaded(this, userFriendlyStateName);

				if (Tools.Has<LuaConsole>())
				{
					Tools.LuaConsole.LuaImp.CallLoadStateEvent(userFriendlyStateName);
				}

				SetMainformMovieInfo();
				Tools.UpdateToolsBefore(fromLua);
				UpdateToolsAfter(fromLua);
				UpdateToolsLoadstate();
				AutoFireController.ClearStarts();

				if (!IsRewindSlave && MovieSession.Movie.IsActive())
				{
					ClearRewindData();
				}

				if (!suppressOSD)
				{
					AddOnScreenMessage($"Loaded state: {userFriendlyStateName}");
				}
			}
			else
			{
				AddOnScreenMessage("Loadstate error!");
			}

			MovieSession.Movie.IsCountingRerecords = wasCountingRerecords;
		}

		public void LoadQuickSave(string quickSlotName, bool fromLua = false, bool suppressOSD = false)
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			ClientApi.OnBeforeQuickLoad(this, quickSlotName, out var handled);
			if (handled)
			{
				return;
			}

			if (IsSavestateSlave)
			{
				Master.LoadQuickSave(SlotToInt(quickSlotName));
				return;
			}

			var path = $"{SaveStatePrefix()}.{quickSlotName}.State";
			if (!File.Exists(path))
			{
				AddOnScreenMessage($"Unable to load {quickSlotName}.State");

				return;
			}

			LoadState(path, quickSlotName, fromLua, suppressOSD);
		}

		public void SaveState(string path, string userFriendlyStateName, bool fromLua = false, bool suppressOSD = false)
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			if (IsSavestateSlave)
			{
				Master.SaveState();
				return;
			}

			try
			{
				SavestateManager.SaveStateFile(path, userFriendlyStateName);

				ClientApi.OnStateSaved(this, userFriendlyStateName);

				if (!suppressOSD)
				{
					AddOnScreenMessage($"Saved state: {userFriendlyStateName}");
				}
			}
			catch (IOException)
			{
				AddOnScreenMessage($"Unable to save state {path}");
			}

			if (!fromLua)
			{
				UpdateStatusSlots();
			}
		}

		// TODO: should backup logic be stuffed in into Client.Common.SaveStateManager?
		public void SaveQuickSave(string quickSlotName, bool fromLua = false, bool suppressOSD = false)
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			ClientApi.OnBeforeQuickSave(this, quickSlotName, out var handled);
			if (handled)
			{
				return;
			}

			if (IsSavestateSlave)
			{
				Master.SaveQuickSave(SlotToInt(quickSlotName));
				return;
			}

			var path = $"{SaveStatePrefix()}.{quickSlotName}.State";

			var file = new FileInfo(path);
			if (file.Directory != null && !file.Directory.Exists)
			{
				file.Directory.Create();
			}

			// Make backup first
			if (Config.BackupSavestates)
			{
				Util.TryMoveBackupFile(path, $"{path}.bak");
			}

			SaveState(path, quickSlotName, fromLua, suppressOSD);

			if (Tools.Has<LuaConsole>())
			{
				Tools.LuaConsole.LuaImp.CallSaveStateEvent(quickSlotName);
			}
		}

		private void SaveStateAs()
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			// allow named state export for tastudio, since it's safe, unlike loading one
			// todo: make it not save laglog in that case
			if (Tools.IsLoaded<TAStudio>())
			{
				Tools.TAStudio.NamedStatePending = true;
			}

			if (IsSavestateSlave)
			{
				Master.SaveStateAs();
				return;
			}

			var path = Config.PathEntries.SaveStateAbsolutePath(Game.System);

			var file = new FileInfo(path);
			if (file.Directory != null && !file.Directory.Exists)
			{
				file.Directory.Create();
			}

			using var sfd = new SaveFileDialog
			{
				AddExtension = true,
				DefaultExt = "State",
				Filter = new FilesystemFilterSet(FilesystemFilter.EmuHawkSaveStates).ToString(),
				InitialDirectory = path,
				FileName = $"{SaveStatePrefix()}.QuickSave0.State"
			};

			var result = sfd.ShowHawkDialog();
			if (result == DialogResult.OK)
			{
				SaveState(sfd.FileName, sfd.FileName);
			}

			if (Tools.IsLoaded<TAStudio>())
			{
				Tools.TAStudio.NamedStatePending = false;
			}
		}

		private void LoadStateAs()
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			if (IsSavestateSlave)
			{
				Master.LoadStateAs();
				return;
			}

			using var ofd = new OpenFileDialog
			{
				InitialDirectory = Config.PathEntries.SaveStateAbsolutePath(Game.System),
				Filter = new FilesystemFilterSet(FilesystemFilter.EmuHawkSaveStates).ToString(),
				RestoreDirectory = true
			};

			var result = ofd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return;
			}

			if (!File.Exists(ofd.FileName))
			{
				return;
			}

			LoadState(ofd.FileName, Path.GetFileName(ofd.FileName));
		}

		private void SelectSlot(int slot)
		{
			if (Emulator.HasSavestates())
			{
				if (IsSavestateSlave)
				{
					var handled = Master.SelectSlot(slot);
					if (handled)
					{
						return;
					}
				}

				Config.SaveSlot = slot;
				SaveSlotSelectedMessage();
				UpdateStatusSlots();
			}
		}

		private void PreviousSlot()
		{
			if (Emulator.HasSavestates())
			{
				if (IsSavestateSlave)
				{
					var handled = Master.PreviousSlot();
					if (handled)
					{
						return;
					}
				}

				if (Config.SaveSlot == 0)
				{
					Config.SaveSlot = 9; // Wrap to end of slot list
				}
				else if (Config.SaveSlot > 9)
				{
					Config.SaveSlot = 9; // Meh, just in case
				}
				else
				{
					Config.SaveSlot--;
				}

				SaveSlotSelectedMessage();
				UpdateStatusSlots();
			}
		}

		private void NextSlot()
		{
			if (Emulator.HasSavestates())
			{
				if (IsSavestateSlave)
				{
					var handled = Master.NextSlot();
					if (handled)
					{
						return;
					}
				}

				if (Config.SaveSlot >= 9)
				{
					Config.SaveSlot = 0; // Wrap to beginning of slot list
				}
				else if (Config.SaveSlot < 0)
				{
					Config.SaveSlot = 0; // Meh, just in case
				}
				else
				{
					Config.SaveSlot++;
				}

				SaveSlotSelectedMessage();
				UpdateStatusSlots();
			}
		}

		private void ToggleReadOnly()
		{
			if (IsSlave && Master.WantsToControlReadOnly)
			{
				Master.ToggleReadOnly();
			}
			else
			{
				if (MovieSession.Movie.IsActive())
				{
					MovieSession.ReadOnly ^= true;
					AddOnScreenMessage(MovieSession.ReadOnly ? "Movie read-only mode" : "Movie read+write mode");
				}
				else
				{
					AddOnScreenMessage("No movie active");
				}
			}
		}

		private void StopMovie(bool saveChanges = true)
		{
			if (IsSlave && Master.WantsToControlStopMovie)
			{
				Master.StopMovie(!saveChanges);
			}
			else
			{
				MovieSession.StopMovie(saveChanges);
				SetMainformMovieInfo();
				UpdateStatusSlots();
			}
		}

		private void CaptureRewind(bool suppressCaptureRewind)
		{
			if (IsRewindSlave)
			{
				Master.CaptureRewind();
			}
			else if (!suppressCaptureRewind && Rewinder.RewindActive)
			{
				Rewinder.Capture();
			}
		}

		private bool Rewind(ref bool runFrame, long currentTimestamp, out bool returnToRecording)
		{
			var isRewinding = false;

			returnToRecording = false;

			if (IsRewindSlave)
			{
				if (ClientControls["Rewind"] || PressRewind)
				{
					if (_frameRewindTimestamp == 0)
					{
						isRewinding = true;
						_frameRewindTimestamp = currentTimestamp;
						_frameRewindWasPaused = EmulatorPaused;
					}
					else
					{
						double timestampDeltaMs = (double)(currentTimestamp - _frameRewindTimestamp) / Stopwatch.Frequency * 1000.0;
						isRewinding = timestampDeltaMs >= Config.FrameProgressDelayMs;

						// clear this flag once we get out of the progress stage
						if (isRewinding)
						{
							_frameRewindWasPaused = false;
						}

						// if we're freely running, there's no need for reverse frame progress semantics (that may be debatable though)
						if (!EmulatorPaused)
						{
							isRewinding = true;
						}

						if (_frameRewindWasPaused)
						{
							if (IsSeeking)
							{
								isRewinding = false;
							}
						}
					}

					if (isRewinding)
					{
						runFrame = Emulator.Frame > 1; // TODO: the master should be deciding this!
						Master.Rewind();
					}
				}
				else
				{
					_frameRewindTimestamp = 0;
				}

				return isRewinding;
			}

			if (Rewinder.RewindActive && (ClientControls["Rewind"] || PressRewind))
			{
				if (EmulatorPaused)
				{
					if (_frameRewindTimestamp == 0)
					{
						isRewinding = true;
						_frameRewindTimestamp = currentTimestamp;
					}
					else
					{
						double timestampDeltaMs = (double)(currentTimestamp - _frameRewindTimestamp) / Stopwatch.Frequency * 1000.0;
						isRewinding = timestampDeltaMs >= Config.FrameProgressDelayMs;
					}
				}
				else
				{
					isRewinding = true;
				}

				if (isRewinding)
				{
					runFrame = Rewinder.Rewind(1) && Emulator.Frame > 1;

					if (runFrame && MovieSession.Movie.IsRecording())
					{
						MovieSession.Movie.SwitchToPlay();
						returnToRecording = true;
					}
				}
			}
			else
			{
				_frameRewindTimestamp = 0;
			}

			return isRewinding;
		}

		#endregion
	}
}
