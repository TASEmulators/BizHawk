using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Common.IOExtensions;

using BizHawk.Client.Common;
using BizHawk.Bizware.BizwareGL;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.N64;

using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Client.EmuHawk.CoreExtensions;
using BizHawk.Client.ApiHawk;
using BizHawk.Emulation.Common.Base_Implementations;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using BizHawk.Emulation.Cores.Consoles.SNK;
using BizHawk.Emulation.Cores.Consoles.Sega.PicoDrive;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Atari.A7800Hawk;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm : Form
	{
		#region Constructors and Initialization, and Tear down

		private void MainForm_Load(object sender, EventArgs e)
		{
			SetWindowText();

			// Hide Status bar icons and general statusbar prep
			MainStatusBar.Padding = new Padding(MainStatusBar.Padding.Left, MainStatusBar.Padding.Top, MainStatusBar.Padding.Left, MainStatusBar.Padding.Bottom); // Workaround to remove extra padding on right
			PlayRecordStatusButton.Visible = false;
			AVIStatusLabel.Visible = false;
			SetPauseStatusbarIcon();
			ToolFormBase.UpdateCheatRelatedTools(null, null);
			RebootStatusBarIcon.Visible = false;
			UpdateNotification.Visible = false;
			_statusBarDiskLightOnImage = Properties.Resources.LightOn;
			_statusBarDiskLightOffImage = Properties.Resources.LightOff;
			_linkCableOn = Properties.Resources.connect_16x16;
			_linkCableOff = Properties.Resources.noconnect_16x16;
			UpdateCoreStatusBarButton();
			if (Global.Config.FirstBoot)
			{
				ProfileFirstBootLabel.Visible = true;
			}

			HandleToggleLightAndLink();
			SetStatusBar();

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
			// If this isnt here, then our assemblyresolving hacks wont work due to the check for MainForm.INTERIM
			// its.. weird. dont ask.
		}

		private CoreComm CreateCoreComm()
		{
			CoreComm ret = new CoreComm(ShowMessageCoreComm, NotifyCoreComm)
			{
				ReleaseGLContext = o => GlobalWin.GLManager.ReleaseGLContext(o),
				RequestGLContext = (major, minor, forward) => GlobalWin.GLManager.CreateGLContext(major, minor, forward),
				ActivateGLContext = gl => GlobalWin.GLManager.Activate((GLManager.ContextRef)gl),
				DeactivateGLContext = () => GlobalWin.GLManager.Deactivate()
			};
			return ret;
		}

		public MainForm(string[] args)
		{
			GlobalWin.MainForm = this;
			Global.Rewinder = new Rewinder
			{
				MessageCallback = GlobalWin.OSD.AddMessage
			};

			Global.ControllerInputCoalescer = new ControllerInputCoalescer();
			Global.FirmwareManager = new FirmwareManager();
			Global.MovieSession = new MovieSession
			{
				Movie = MovieService.DefaultInstance,
				MovieControllerAdapter = MovieService.DefaultInstance.LogGeneratorInstance().MovieControllerAdapter,
				MessageCallback = GlobalWin.OSD.AddMessage,
				AskYesNoCallback = StateErrorAskUser,
				PauseCallback = PauseEmulator,
				ModeChangedCallback = SetMainformMovieInfo
			};

			Icon = Properties.Resources.logo;
			InitializeComponent();
			Global.Game = GameInfo.NullInstance;
			if (Global.Config.ShowLogWindow)
			{
				LogConsole.ShowConsole();
				DisplayLogWindowMenuItem.Checked = true;
			}

			_throttle = new Throttle();

			Global.CheatList = new CheatCollection();
			Global.CheatList.Changed += ToolFormBase.UpdateCheatRelatedTools;

			UpdateStatusSlots();
			UpdateKeyPriorityIcon();

			// In order to allow late construction of this database, we hook up a delegate here to dearchive the data and provide it on demand
			// we could background thread this later instead if we wanted to be real clever
			NES.BootGodDB.GetDatabaseBytes = () =>
			{
				string xmlPath = Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "NesCarts.xml");
				string x7zPath = Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "NesCarts.7z");
				bool loadXml = File.Exists(xmlPath);
				using (var nesCartFile = new HawkFile(loadXml ? xmlPath : x7zPath))
				{
					if (!loadXml)
					{
						nesCartFile.BindFirst();
					}

					return nesCartFile
						.GetStream()
						.ReadAllBytes();
				}
			};

			argParse.parseArguments(args);

			Database.LoadDatabase(Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "gamedb.txt"));

			// TODO GL - a lot of disorganized wiring-up here
			CGC.CGCBinPath = Path.Combine(PathManager.GetDllDirectory(), "cgc.exe");
			PresentationPanel = new PresentationPanel();
			PresentationPanel.GraphicsControl.MainWindow = true;
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
				if (GlobalWin.Tools.AskSave())
				{
					// zero 03-nov-2015 - close game after other steps. tools might need to unhook themselves from a core.
					Global.MovieSession.Movie.Stop();
					GlobalWin.Tools.Close();
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
				if (GlobalWin.Sound != null)
				{
					GlobalWin.Sound.StopSound();
				}
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

				if (GlobalWin.Sound != null)
				{
					GlobalWin.Sound.StartSound();
				}
			};

			Input.Initialize();
			InitControls();

			var comm = CreateCoreComm();
			CoreFileProvider.SyncCoreCommInputSignals(comm);
			Emulator = new NullEmulator(comm, Global.Config.GetCoreSettings<NullEmulator>());
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
				if (Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.DirectSound)
				{
					message = "Couldn't initialize DirectSound! Things may go poorly for you. Try changing your sound driver to 44.1khz instead of 48khz in mmsys.cpl.";
				}

				MessageBox.Show(message, "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

				Global.Config.SoundOutputMethod = Config.ESoundOutputMethod.Dummy;
				GlobalWin.Sound = new Sound(Handle);
			}

			GlobalWin.Sound.StartSound();
			InputManager.RewireInputChain();
			GlobalWin.Tools = new ToolManager(this);
			RewireSound();

			// Workaround for windows, location is -32000 when minimized, if they close it during this time, that's what gets saved
			if (Global.Config.MainWndx == -32000)
			{
				Global.Config.MainWndx = 0;
			}

			if (Global.Config.MainWndy == -32000)
			{
				Global.Config.MainWndy = 0;
			}

			if (Global.Config.MainWndx != -1 && Global.Config.MainWndy != -1 && Global.Config.SaveWindowPosition)
			{
				Location = new Point(Global.Config.MainWndx, Global.Config.MainWndy);
			}

			if (argParse.socket_ip != null)
			{
				Global.Config.controller_ip = argParse.socket_ip;
			}

			if (argParse.socket_port != null)
			{
				Global.Config.controller_port = argParse.socket_port;
			}

			if (argParse.socket_ip_p2 != null)
			{
				Global.Config.controller_ip_p2 = argParse.socket_ip_p2;
			}

			if (argParse.socket_port_p2 != null)
			{
				Global.Config.controller_port_p2 = argParse.socket_port_p2;
			}

			if (argParse.use_two_controllers)
			{
				Global.Config.use_two_controllers = true;
			}

			if (argParse.run_id != null)
			{
				Global.Config.run_id = argParse.run_id;
			}

			if (argParse.cmdRom != null)
			{
				// Commandline should always override auto-load
				var ioa = OpenAdvancedSerializer.ParseWithLegacy(argParse.cmdRom);
				LoadRom(argParse.cmdRom, new LoadRomArgs { OpenAdvanced = ioa });
				if (Global.Game == null)
				{
					MessageBox.Show("Failed to load " + argParse.cmdRom + " specified on commandline");
				}
			}
			else if (Global.Config.RecentRoms.AutoLoad && !Global.Config.RecentRoms.Empty)
			{
				LoadRomFromRecent(Global.Config.RecentRoms.MostRecent);
			}

			if (argParse.cmdMovie != null)
			{
				_supressSyncSettingsWarning = true; // We dont' want to be nagged if we are attempting to automate
				if (Global.Game == null)
				{
					OpenRom();
				}

				// If user picked a game, then do the commandline logic
				if (!Global.Game.IsNullInstance)
				{
					var movie = MovieService.Get(argParse.cmdMovie);
					Global.MovieSession.ReadOnly = true;

					// if user is dumping and didnt supply dump length, make it as long as the loaded movie
					if (argParse._autoDumpLength == 0)
					{
						argParse._autoDumpLength = movie.InputLogLength;
					}

					// Copy pasta from drag & drop
					if (MovieImport.IsValidMovieExtension(Path.GetExtension(argParse.cmdMovie)))
					{
						string errorMsg;
						string warningMsg;
						var imported = MovieImport.ImportFile(argParse.cmdMovie, out errorMsg, out warningMsg);
						if (!string.IsNullOrEmpty(errorMsg))
						{
							MessageBox.Show(errorMsg, "Conversion error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
						else
						{
							// fix movie extension to something palatable for these purposes. 
							// for instance, something which doesnt clobber movies you already may have had.
							// i'm evenly torn between this, and a file in %TEMP%, but since we dont really have a way to clean up this tempfile, i choose this:
							StartNewMovie(imported, false);
						}

						GlobalWin.OSD.AddMessage(warningMsg);
					}
					else
					{
						StartNewMovie(movie, false);
						Global.Config.RecentMovies.Add(argParse.cmdMovie);
					}

					_supressSyncSettingsWarning = false;
				}
			}
			else if (Global.Config.RecentMovies.AutoLoad && !Global.Config.RecentMovies.Empty)
			{
				if (Global.Game.IsNullInstance)
				{
					OpenRom();
				}

				// If user picked a game, then do the autoload logic
				if (!Global.Game.IsNullInstance)
				{
					if (File.Exists(Global.Config.RecentMovies.MostRecent))
					{
						StartNewMovie(MovieService.Get(Global.Config.RecentMovies.MostRecent), false);
					}
					else
					{
						Global.Config.RecentMovies.HandleLoadError(Global.Config.RecentMovies.MostRecent);
					}
				}
			}

			if (argParse.startFullscreen || Global.Config.StartFullscreen)
			{
				_needsFullscreenOnLoad = true;
			}

			if (!Global.Game.IsNullInstance)
			{
				if (argParse.cmdLoadState != null)
				{
					LoadState(argParse.cmdLoadState, Path.GetFileName(argParse.cmdLoadState));
				}
				else if (argParse.cmdLoadSlot != null)
				{
					LoadQuickSave("QuickSave" + argParse.cmdLoadSlot);
				}
				else if (Global.Config.AutoLoadLastSaveSlot)
				{
					LoadQuickSave("QuickSave" + Global.Config.SaveSlot);
				}
			}

			//start Lua Console if requested in the command line arguments
			if (argParse.luaConsole)
			{
				GlobalWin.Tools.Load<LuaConsole>();
			}
			//load Lua Script if requested in the command line arguments
			if (argParse.luaScript != null)
			{
				GlobalWin.Tools.LuaConsole.LoadLuaFile(argParse.luaScript);
			}

			GlobalWin.Tools.AutoLoad();

			if (Global.Config.RecentWatches.AutoLoad)
			{
				GlobalWin.Tools.LoadRamWatch(!Global.Config.DisplayRamWatch);
			}

			if (Global.Config.RecentCheats.AutoLoad)
			{
				GlobalWin.Tools.Load<Cheats>();
			}

			SetStatusBar();

			if (Global.Config.StartPaused)
			{
				PauseEmulator();
			}
		
			// start dumping, if appropriate
			if (argParse.cmdDumpType != null && argParse.cmdDumpName != null)
			{
				RecordAv(argParse.cmdDumpType, argParse.cmdDumpName);
			}

			SetMainformMovieInfo();

			SynchChrome();

			PresentationPanel.Control.Paint += (o, e) =>
			{
				// I would like to trigger a repaint here, but this isnt done yet
			};
		}

		private readonly bool _supressSyncSettingsWarning;

		public int ProgramRunLoop()
		{
			CheckMessages(); // can someone leave a note about why this is needed?
			LogConsole.PositionConsole();

			// needs to be done late, after the log console snaps on top
			// fullscreen should snap on top even harder!
			if (_needsFullscreenOnLoad)
			{
				_needsFullscreenOnLoad = false;
				ToggleFullscreen();
			}

			// incantation required to get the program reliably on top of the console window
			// we might want it in ToggleFullscreen later, but here, it needs to happen regardless
			BringToFront();
			Activate();
			BringToFront();

			InitializeFpsData();

			for (;;)
			{
				Input.Instance.Update();

				// handle events and dispatch as a hotkey action, or a hotkey button, or an input button
				ProcessInput();
				Global.ClientControls.LatchFromPhysical(_hotkeyCoalescer);

				Global.ActiveController.LatchFromPhysical(Global.ControllerInputCoalescer);

				Global.ActiveController.ApplyAxisConstraints(
					(Emulator is N64 && Global.Config.N64UseCircularAnalogConstraint) ? "Natural Circle" : null);

				Global.ActiveController.OR_FromLogical(Global.ClickyVirtualPadController);
				Global.AutoFireController.LatchFromPhysical(Global.ControllerInputCoalescer);

				if (Global.ClientControls["Autohold"])
				{
					Global.StickyXORAdapter.MassToggleStickyState(Global.ActiveController.PressedButtons);
					Global.AutofireStickyXORAdapter.MassToggleStickyState(Global.AutoFireController.PressedButtons);
				}
				else if (Global.ClientControls["Autofire"])
				{
					Global.AutofireStickyXORAdapter.MassToggleStickyState(Global.ActiveController.PressedButtons);
				}

				// autohold/autofire must not be affected by the following inputs
				Global.ActiveController.Overrides(Global.LuaAndAdaptor);

				if (GlobalWin.Tools.Has<LuaConsole>() && !SuppressLua)
				{
					GlobalWin.Tools.LuaConsole.ResumeScripts(false);
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

				if (_exit)
				{
					break;
				}

				if (Global.Config.DispSpeedupFeatures != 0)
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
			if (GlobalWin.DisplayManager != null)
			{
				GlobalWin.DisplayManager.Dispose();
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
			get
			{
				return _emulatorPaused;
			}

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

			public bool Paused { get; private set; }
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

		// runloop won't exec lua
		public bool SuppressLua { get; set; }

		public long MouseWheelTracker { get; private set; }

		private int? _pauseOnFrame;
		public int? PauseOnFrame // If set, upon completion of this frame, the client wil pause
		{
			get
			{
				return _pauseOnFrame;
			}

			set
			{
				_pauseOnFrame = value;
				SetPauseStatusbarIcon();

				if (value == null) // TODO: make an Event handler instead, but the logic here is that after turbo seeking, tools will want to do a real update when the emulator finally pauses
				{
					bool skipScripts = !(Global.Config.TurboSeek && !Global.Config.RunLuaDuringTurbo && !SuppressLua);
					GlobalWin.Tools.UpdateToolsBefore(skipScripts);
					GlobalWin.Tools.UpdateToolsAfter(skipScripts);
				}
			}
		}

		public bool IsSeeking => PauseOnFrame.HasValue;

		private bool IsTurboSeeking => PauseOnFrame.HasValue && Global.Config.TurboSeek;

		private bool IsTurboing => Global.ClientControls["Turbo"] || IsTurboSeeking;

		#endregion

		#region Public Methods

		public void ClearHolds()
		{
			Global.StickyXORAdapter.ClearStickies();
			Global.AutofireStickyXORAdapter.ClearStickies();

			if (GlobalWin.Tools.Has<VirtualpadTool>())
			{
				GlobalWin.Tools.VirtualPad.ClearVirtualPadHolds();
			}
		}

		public void FlagNeedsReboot()
		{
			RebootStatusBarIcon.Visible = true;
			GlobalWin.OSD.AddMessage("Core reboot needed for this setting");
		}

		/// <summary>
		/// Controls whether the app generates input events. should be turned off for most modal dialogs
		/// </summary>
		public bool AllowInput(bool yieldAlt)
		{
			// the main form gets input
			if (ActiveForm == this)
			{
				return true;
			}

			// even more special logic for TAStudio:
			// TODO - implement by event filter in TAStudio
			var maybeTAStudio = ActiveForm as TAStudio;
			if (maybeTAStudio != null)
			{
				if (yieldAlt)
				{
					return false;
				}

				if (maybeTAStudio.IsInMenuLoop)
				{
					return false;
				}
			}

			// modals that need to capture input for binding purposes get input, of course
			if (ActiveForm is HotkeyConfig
				|| ActiveForm is ControllerConfig
				|| ActiveForm is TAStudio
				|| ActiveForm is VirtualpadTool)
			{
				return true;
			}

			// if no form is active on this process, then the background input setting applies
			if (ActiveForm == null && Global.Config.AcceptBackgroundInput)
			{
				return true;
			}

			return false;
		}

		// TODO: make this an actual property, set it when loading a Rom, and pass it dialogs, etc
		// This is a quick hack to reduce the dependency on Global.Emulator
		private IEmulator Emulator
		{
			get
			{
				return Global.Emulator;
			}

			set
			{
				Global.Emulator = value;
				_currentVideoProvider = Global.Emulator.AsVideoProviderOrDefault();
				_currentSoundProvider = Global.Emulator.AsSoundProviderOrDefault();
			}
		}

		private IVideoProvider _currentVideoProvider = NullVideo.Instance;

		private ISoundProvider _currentSoundProvider = new NullSound(44100 / 60); // Reasonable default until we have a core instance

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
			ControllerInputCoalescer conInput = (ControllerInputCoalescer)Global.ControllerInputCoalescer;

			for (;;)
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
				var triggers = Global.ClientControls.SearchBindings(ie.LogicalButton.ToString());
				if (triggers.Count == 0)
				{
					// Maybe it is a system alt-key which hasnt been overridden
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

					// ordinarily, an alt release with nothing else would move focus to the menubar. but that is sort of useless, and hard to implement exactly right.
				}

				// zero 09-sep-2012 - all input is eligible for controller input. not sure why the above was done. 
				// maybe because it doesnt make sense to me to bind hotkeys and controller inputs to the same keystrokes

				// adelikat 02-dec-2012 - implemented options for how to handle controller vs hotkey conflicts. This is primarily motivated by computer emulation and thus controller being nearly the entire keyboard
				bool handled;
				switch (Global.Config.Input_Hotkey_OverrideOptions)
				{
					default:
					case 0: // Both allowed
						conInput.Receive(ie);

						handled = false;
						if (ie.EventType == Input.InputEventType.Press)
						{
							handled = triggers.Aggregate(handled, (current, trigger) => current | CheckHotkey(trigger));
						}

						// hotkeys which arent handled as actions get coalesced as pollable virtual client buttons
						if (!handled)
						{
							_hotkeyCoalescer.Receive(ie);
						}

						break;
					case 1: // Input overrides Hokeys
						conInput.Receive(ie);
						if (!Global.ActiveController.HasBinding(ie.LogicalButton.ToString()))
						{
							handled = false;
							if (ie.EventType == Input.InputEventType.Press)
							{
								handled = triggers.Aggregate(handled, (current, trigger) => current | CheckHotkey(trigger));
							}

							// hotkeys which arent handled as actions get coalesced as pollable virtual client buttons
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
							handled = triggers.Aggregate(handled, (current, trigger) => current | CheckHotkey(trigger));
						}

						// hotkeys which arent handled as actions get coalesced as pollable virtual client buttons
						if (!handled)
						{
							_hotkeyCoalescer.Receive(ie);

							// Check for hotkeys that may not be handled through Checkhotkey() method, reject controller input mapped to these
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
					var p = GlobalWin.DisplayManager.UntransformPoint(new Point((int)o.Item2, 0));
					float x = p.X / (float)_currentVideoProvider.BufferWidth;
					return new Tuple<string, float>("WMouse X", (x * 20000) - 10000);
				}

				if (o.Item1 == "WMouse Y")
				{
					var p = GlobalWin.DisplayManager.UntransformPoint(new Point(0, (int)o.Item2));
					float y = p.Y / (float)_currentVideoProvider.BufferHeight;
					return new Tuple<string, float>("WMouse Y", (y * 20000) - 10000);
				}

				return o;
			}));
		}

		public void RebootCore()
		{
			var ioa = OpenAdvancedSerializer.ParseWithLegacy(_currentlyOpenRomPoopForAdvancedLoaderPleaseRefactorMe);
			if (ioa is OpenAdvanced_LibretroNoGame)
			{
				LoadRom("", CurrentlyOpenRomArgs);
			}
			else
			{
				LoadRom(ioa.SimplePath, CurrentlyOpenRomArgs);
			}
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

		public void TogglePause()
		{
			EmulatorPaused ^= true;
			SetPauseStatusbarIcon();

			// TODO: have tastudio set a pause status change callback, or take control over pause
			if (GlobalWin.Tools.Has<TAStudio>())
			{
				GlobalWin.Tools.UpdateValues<TAStudio>();
			}
		}

		public byte[] CurrentFrameBuffer(bool captureOSD)
		{
			using (var bb = captureOSD ? CaptureOSD() : MakeScreenshotImage())
			{
				using (var img = bb.ToSysdrawingBitmap())
				{
					ImageConverter converter = new ImageConverter();
					return (byte[])converter.ConvertTo(img, typeof(byte[]));
				}
			}
		}

		public void TakeScreenshotToClipboard()
		{
			using (var bb = Global.Config.Screenshot_CaptureOSD ? CaptureOSD() : MakeScreenshotImage())
			{
				using (var img = bb.ToSysdrawingBitmap())
				{
					Clipboard.SetImage(img);
				}
			}

			GlobalWin.OSD.AddMessage("Screenshot (raw) saved to clipboard.");
		}

		private void TakeScreenshotClientToClipboard()
		{
			using (var bb = GlobalWin.DisplayManager.RenderOffscreen(_currentVideoProvider, Global.Config.Screenshot_CaptureOSD))
			{
				using (var img = bb.ToSysdrawingBitmap())
				{
					Clipboard.SetImage(img);
				}
			}

			GlobalWin.OSD.AddMessage("Screenshot (client) saved to clipboard.");
		}

		public void TakeScreenshot()
		{
			string fmt = "{0}.{1:yyyy-MM-dd HH.mm.ss}{2}.png";
			string prefix = PathManager.ScreenshotPrefix(Global.Game);
			var ts = DateTime.Now;

			string fnameBare = string.Format(fmt, prefix, ts, "");
			string fname = string.Format(fmt, prefix, ts, " (0)");

			// if the (0) filename exists, do nothing. we'll bump up the number later
			// if the bare filename exists, move it to (0)
			// otherwise, no related filename exists, and we can proceed with the bare filename
			if (File.Exists(fname))
			{
			}
			else if (File.Exists(fnameBare))
			{
				File.Move(fnameBare, fname);
			}
			else
			{
				fname = fnameBare;
			}

			int seq = 0;
			while (File.Exists(fname))
			{
				var sequence = $" ({seq++})";
				fname = string.Format(fmt, prefix, ts, sequence);
			}

			TakeScreenshot(fname);
		}

		public void TakeScreenshot(string path)
		{
			var fi = new FileInfo(path);
			if (fi.Directory != null && !fi.Directory.Exists)
			{
				fi.Directory.Create();
			}

			using (var bb = Global.Config.Screenshot_CaptureOSD ? CaptureOSD() : MakeScreenshotImage())
			{
				using (var img = bb.ToSysdrawingBitmap())
				{
					img.Save(fi.FullName, ImageFormat.Png);
				}
			}

			/*
			using (var fs = new FileStream(path + "_test.bmp", FileMode.OpenOrCreate, FileAccess.Write))
				QuickBmpFile.Save(Emulator.VideoProvider(), fs, r.Next(50, 500), r.Next(50, 500));
			*/
			GlobalWin.OSD.AddMessage(fi.Name + " saved.");
		}

		public void FrameBufferResized()
		{
			// run this entire thing exactly twice, since the first resize may adjust the menu stacking
			for (int i = 0; i < 2; i++)
			{
				int zoom = Global.Config.TargetZoomFactors[Emulator.SystemId];
				var area = Screen.FromControl(this).WorkingArea;

				int borderWidth = Size.Width - PresentationPanel.Control.Size.Width;
				int borderHeight = Size.Height - PresentationPanel.Control.Size.Height;

				// start at target zoom and work way down until we find acceptable zoom
				Size lastComputedSize = new Size(1, 1);
				for (; zoom >= 1; zoom--)
				{
					lastComputedSize = GlobalWin.DisplayManager.CalculateClientSize(_currentVideoProvider, zoom);
					if (lastComputedSize.Width + borderWidth < area.Width
						&& lastComputedSize.Height + borderHeight < area.Height)
					{
						break;
					}
				}

				Console.WriteLine("Selecting display size " + lastComputedSize);

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
				MainMenuStrip.Visible = Global.Config.DispChrome_MenuFullscreen && !argParse._chromeless;
				MainStatusBar.Visible = Global.Config.DispChrome_StatusBarFullscreen && !argParse._chromeless;
			}
			else
			{
				MainStatusBar.Visible = Global.Config.DispChrome_StatusBarWindowed && !argParse._chromeless;
				MainMenuStrip.Visible = Global.Config.DispChrome_MenuWindowed && !argParse._chromeless;
				MaximizeBox = MinimizeBox = Global.Config.DispChrome_CaptionWindowed && !argParse._chromeless;
				if (Global.Config.DispChrome_FrameWindowed == 0 || argParse._chromeless)
				{
					FormBorderStyle = FormBorderStyle.None;
				}
				else if (Global.Config.DispChrome_FrameWindowed == 1)
				{
					FormBorderStyle = FormBorderStyle.SizableToolWindow;
				}
				else if (Global.Config.DispChrome_FrameWindowed == 2)
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
#if WINDOWS
				// Work around an AMD driver bug in >= vista:
				// It seems windows will activate opengl fullscreen mode when a GL control is occupying the exact space of a screen (0,0 and dimensions=screensize)
				// AMD cards manifest a problem under these circumstances, flickering other monitors. 
				// It isnt clear whether nvidia cards are failing to employ this optimization, or just not flickering.
				// (this could be determined with more work; other side affects of the fullscreen mode include: corrupted taskbar, no modal boxes on top of GL control, no screenshots)
				// At any rate, we can solve this by adding a 1px black border around the GL control
				// Please note: It is important to do this before resizing things, otherwise momentarily a GL control without WS_BORDER will be at the magic dimensions and cause the flakeout
				if (Global.Config.DispFullscreenHacks && Global.Config.DispMethod == Config.EDispMethod.OpenGL)
				{
					//ATTENTION: this causes the statusbar to not work well, since the backcolor is now set to black instead of SystemColors.Control.
					//It seems that some statusbar elements composite with the backcolor. 
					//Maybe we could add another control under the statusbar. with a different backcolor
					Padding = new Padding(1);
					BackColor = Color.Black;

					// FUTURE WORK:
					// re-add this padding back into the display manager (so the image will get cut off a little but, but a few more resolutions will fully fit into the screen)
				}
#endif

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

#if WINDOWS
				// do this even if DispFullscreenHacks arent enabled, to restore it in case it changed underneath us or something
				Padding = new Padding(0);
				// it's important that we set the form color back to this, because the statusbar icons blend onto the mainform, not onto the statusbar--
				// so we need the statusbar and mainform backdrop color to match
				BackColor = SystemColors.Control;
#endif

				_inFullscreen = false;

				SynchChrome();
				Location = _windowedLocation;
				ResumeLayout();

				FrameBufferResized();
			}
		}

		private void OpenLuaConsole()
		{
#if WINDOWS
			GlobalWin.Tools.Load<LuaConsole>();
#else
			MessageBox.Show("Sorry, Lua is not supported on this platform.", "Lua not supported", MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
		}

		public void NotifyLogWindowClosing()
		{
			DisplayLogWindowMenuItem.Checked = false;
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
			string ttype = ":(none)";
			if (Global.Config.SoundThrottle)
			{
				ttype = ":Sound";
			}

			if (Global.Config.VSyncThrottle)
			{
				ttype = $":Vsync{(Global.Config.VSync ? "[ena]" : "[dis]")}";
			}

			if (Global.Config.ClockThrottle)
			{
				ttype = ":Clock";
			}

			string xtype = _unthrottled ? "Unthrottled" : "Throttled";
			string msg = $"{xtype}{ttype} ";

			GlobalWin.OSD.AddMessage(msg);
		}

		public void FrameSkipMessage()
		{
			GlobalWin.OSD.AddMessage("Frameskipping set to " + Global.Config.FrameSkip);
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

		private LibsnesCore AsSNES => Emulator as LibsnesCore;

		private void SNES_ToggleBg(int layer)
		{
			if (!(Emulator is LibsnesCore) && !(Emulator is Snes9x))
			{
				return;
			}

			if (layer < 1 || layer > 4)
			{
				return;
			}

			bool result = false;
			if (Emulator is LibsnesCore)
			{
				var s = ((LibsnesCore)Emulator).GetSettings();
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

				((LibsnesCore)Emulator).PutSettings(s);
			}
			else if (Emulator is Snes9x)
			{
				var s = ((Snes9x)Emulator).GetSettings();
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

				((Snes9x)Emulator).PutSettings(s);
			}

			GlobalWin.OSD.AddMessage($"BG {layer} Layer " + (result ? "On" : "Off"));
		}

		private void SNES_ToggleObj(int layer)
		{
			if (!(Emulator is LibsnesCore) && !(Emulator is Snes9x))
			{
				return;
			}

			if (layer < 1 || layer > 4)
			{
				return;
			}

			bool result = false;
			if (Emulator is LibsnesCore)
			{
				var s = ((LibsnesCore)Emulator).GetSettings();
				switch (layer)
				{
					case 1:
						result = s.ShowOBJ_0 ^= true;
						break;
					case 2:
						result = s.ShowOBJ_1 ^= true;
						break;
					case 3:
						result = s.ShowOBJ_2 ^= true;
						break;
					case 4:
						result = s.ShowOBJ_3 ^= true;
						break;
				}

				((LibsnesCore)Emulator).PutSettings(s);
				GlobalWin.OSD.AddMessage($"Obj {layer} Layer " + (result ? "On" : "Off"));
			}
			else if (Emulator is Snes9x)
			{
				var s = ((Snes9x)Emulator).GetSettings();
				switch (layer)
				{
					case 1:
						result = s.ShowSprites0 ^= true;
						break;
					case 2:
						result = s.ShowSprites1 ^= true;
						break;
					case 3:
						result = s.ShowSprites2 ^= true;
						break;
					case 4:
						result = s.ShowSprites3 ^= true;
						break;
				}

				((Snes9x)Emulator).PutSettings(s);
				GlobalWin.OSD.AddMessage($"Sprite {layer} Layer " + (result ? "On" : "Off"));
			}
		}

		public bool RunLibretroCoreChooser()
		{
			var ofd = new OpenFileDialog();

			if (Global.Config.LibretroCore != null)
			{
				ofd.FileName = Path.GetFileName(Global.Config.LibretroCore);
				ofd.InitialDirectory = Path.GetDirectoryName(Global.Config.LibretroCore);
			}
			else
			{
				ofd.InitialDirectory = PathManager.GetPathType("Libretro", "Cores");
				if (!Directory.Exists(ofd.InitialDirectory))
				{
					Directory.CreateDirectory(ofd.InitialDirectory);
				}
			}

			ofd.RestoreDirectory = true;
			ofd.Filter = "Libretro Cores (*.dll)|*.dll";

			if (ofd.ShowDialog() == DialogResult.Cancel)
			{
				return false;
			}

			Global.Config.LibretroCore = ofd.FileName;

			return true;
		}

		#endregion

		#region Private variables

		private Size _lastVideoSize = new Size(-1, -1), _lastVirtualSize = new Size(-1, -1);
		private readonly SaveSlotManager _stateSlots = new SaveSlotManager();

		// AVI/WAV state
		private IVideoWriter _currAviWriter;

		private AutofireController _autofireNullControls;

		// Sound refator TODO: we can enforce async mode here with a property that gets/sets this but does an async check
		private ISoundProvider _aviSoundInputAsync; // Note: This sound provider must be in async mode!

		private SimpleSyncSoundProvider _dumpProxy; // an audio proxy used for dumping
		private bool _dumpaudiosync; // set true to for experimental AV dumping
		private int _avwriterResizew;
		private int _avwriterResizeh;
		private bool _avwriterpad;

		private bool _exit;
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

		private Cursor _blankCursor;
		private bool _cursorHidden;
		private bool _inFullscreen;
		private Point _windowedLocation;
		private bool _needsFullscreenOnLoad;

		private int _lastOpenRomFilter;

		private ArgParser argParse = new ArgParser();
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

		//countdown for saveram autoflushing
		public int AutoFlushSaveRamIn { get; set; }
		#endregion

		#region Private methods

		private void SetStatusBar()
		{
			if (!_inFullscreen)
			{
				MainStatusBar.Visible = Global.Config.DispChrome_StatusBarWindowed;
				PerformLayout();
				FrameBufferResized();
			}
		}

		private void SetWindowText()
		{
			string str = "";

			if (_inResizeLoop)
			{
				var size = PresentationPanel.NativeSize;
				float ar = (float)size.Width / size.Height;
				str += $"({size.Width}x{size.Height})={ar} - ";
			}

			// we need to display FPS somewhere, in this case
			if (Global.Config.DispSpeedupFeatures == 0)
			{
				str += $"({_lastFps:0} fps) - ";
			}

			if (!string.IsNullOrEmpty(VersionInfo.CustomBuildString))
			{
				str += VersionInfo.CustomBuildString + " ";
			}

			str += Emulator.IsNull() ? "BizHawk" : Global.SystemInfo.DisplayName;

			if (Global.Config.run_id != null)
			{
				str += " " + Global.Config.run_id;
			}

			if (VersionInfo.DeveloperBuild)
			{
				str += " (interim)";
			}

			if (!Emulator.IsNull())
			{
				str += " - " + Global.Game.Name;

				if (Global.MovieSession.Movie.IsActive)
				{
					str += " - " + Path.GetFileName(Global.MovieSession.Movie.Filename);
				}
			}

			if (!Global.Config.DispChrome_CaptionWindowed || argParse._chromeless)
			{
				str = "";
			}

			Text = str;
		}

		private void ClearAutohold()
		{
			ClearHolds();
			GlobalWin.OSD.AddMessage("Autohold keys cleared");
		}

		private static void UpdateToolsLoadstate()
		{
			if (GlobalWin.Tools.Has<SNESGraphicsDebugger>())
			{
				GlobalWin.Tools.SNESGraphicsDebugger.UpdateToolsLoadstate();
			}
		}

		private void UpdateToolsAfter(bool fromLua = false)
		{
			GlobalWin.Tools.UpdateToolsAfter(fromLua);
			HandleToggleLightAndLink();
		}

		public void UpdateDumpIcon()
		{
			DumpStatusButton.Image = Properties.Resources.Blank;
			DumpStatusButton.ToolTipText = "";

			if (Emulator.IsNull())
			{
				return;
			}
			else if (Global.Game == null)
			{
				return;
			}

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

			if (!string.IsNullOrEmpty(Emulator.CoreComm.RomStatusAnnotation))
			{
				annotation = Emulator.CoreComm.RomStatusAnnotation;
			}

			DumpStatusButton.ToolTipText = annotation;
		}

		private void LoadSaveRam()
		{
			if (Emulator.HasSaveRam())
			{
				try // zero says: this is sort of sketchy... but this is no time for rearchitecting
				{
					if (Global.Config.AutosaveSaveRAM)
					{
						var saveram = new FileInfo(PathManager.SaveRamPath(Global.Game));
						var autosave = new FileInfo(PathManager.AutoSaveRamPath(Global.Game));
						if (autosave.Exists && autosave.LastWriteTime > saveram.LastWriteTime)
						{
							GlobalWin.OSD.AddMessage("AutoSaveRAM is newer than last saved SaveRAM");
						}
					}

					byte[] sram;

					// GBA meteor core might not know how big the saveram ought to be, so just send it the whole file
					// GBA vba-next core will try to eat anything, regardless of size
					if (Emulator is VBANext || Emulator is MGBAHawk || Emulator is NeoGeoPort)
					{
						sram = File.ReadAllBytes(PathManager.SaveRamPath(Global.Game));
					}
					else
					{
						var oldram = Emulator.AsSaveRam().CloneSaveRam();
						if (oldram == null)
						{
							// we're eating this one now. The possible negative consequence is that a user could lose
							// their saveram and not know why
							// MessageBox.Show("Error: tried to load saveram, but core would not accept it?");
							return;
						}

						// why do we silently truncate\pad here instead of warning\erroring?
						sram = new byte[oldram.Length];
						using (var reader = new BinaryReader(
								new FileStream(PathManager.SaveRamPath(Global.Game), FileMode.Open, FileAccess.Read)))
						{
							reader.Read(sram, 0, sram.Length);
						}
					}

					Emulator.AsSaveRam().StoreSaveRam(sram);
					AutoFlushSaveRamIn = Global.Config.FlushSaveRamFrames;
				}
				catch (IOException)
				{
					GlobalWin.OSD.AddMessage("An error occurred while loading Sram");
				}
			}
		}		

		public void FlushSaveRAM(bool autosave = false)
		{
			if (Emulator.HasSaveRam())
			{
				string path;
				if (autosave)
				{
					path = PathManager.AutoSaveRamPath(Global.Game);
					AutoFlushSaveRamIn = Global.Config.FlushSaveRamFrames;
				}
				else
				{
					path = PathManager.SaveRamPath(Global.Game);
				}
				var file = new FileInfo(path);
				var newPath = path + ".new";
				var newFile = new FileInfo(newPath);
				var backupPath = path + ".bak";
				var backupFile = new FileInfo(backupPath);
				if (file.Directory != null && !file.Directory.Exists)
				{
					file.Directory.Create();
				}

				var writer = new BinaryWriter(new FileStream(newPath, FileMode.Create, FileAccess.Write));
				var saveram = Emulator.AsSaveRam().CloneSaveRam();

				if (saveram != null)
				{
					writer.Write(saveram, 0, saveram.Length);
				}
				writer.Close();

				if (file.Exists)
				{
					if (Global.Config.BackupSaveram)
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
		}

		private void RewireSound()
		{
			if (_dumpProxy != null)
			{
				// we're video dumping, so async mode only and use the DumpProxy.
				// note that the avi dumper has already rewired the emulator itself in this case.
				GlobalWin.Sound.SetInputPin(_dumpProxy);
			}
			else
			{
				bool useAsyncMode = _currentSoundProvider.CanProvideAsync && !Global.Config.SoundThrottle;
				_currentSoundProvider.SetSyncMode(useAsyncMode ? SyncSoundMode.Async : SyncSoundMode.Sync);
				GlobalWin.Sound.SetInputPin(_currentSoundProvider);
			}
		}

		private void HandlePlatformMenus()
		{
			var system = "";
			if (!Global.Game.IsNullInstance)
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
			GenesisSubMenu.Visible = false;
			wonderSwanToolStripMenuItem.Visible = false;
			AppleSubMenu.Visible = false;
			C64SubMenu.Visible = false;
			IntvSubMenu.Visible = false;
			virtualBoyToolStripMenuItem.Visible = false;
			sNESToolStripMenuItem.Visible = false;
			neoGeoPocketToolStripMenuItem.Visible = false;
			pCFXToolStripMenuItem.Visible = false;

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
					if (Emulator is A7800Hawk)
					{
						A7800SubMenu.Visible = true;
					}
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
					DGBSubMenu.Visible = true;
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

			Global.ClientControls = controls;
			_autofireNullControls = new AutofireController(NullController.Instance.Definition, Emulator);
		}

		private void LoadMoviesFromRecent(string path)
		{
			if (File.Exists(path))
			{
				var movie = MovieService.Get(path);
				Global.MovieSession.ReadOnly = true;
				StartNewMovie(movie, false);
			}
			else
			{
				Global.Config.RecentMovies.HandleLoadError(path);
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
				Global.Config.RecentRoms.HandleLoadError(romPath, rom);
			}
		}

		private void SetPauseStatusbarIcon()
		{
			if (IsTurboSeeking)
			{
				PauseStatusButton.Image = Properties.Resources.Lightning;
				PauseStatusButton.Visible = true;
				PauseStatusButton.ToolTipText = "Emulator is turbo seeking to frame " + PauseOnFrame.Value + " click to stop seek";
			}
			else if (PauseOnFrame.HasValue)
			{
				PauseStatusButton.Image = Properties.Resources.YellowRight;
				PauseStatusButton.Visible = true;
				PauseStatusButton.ToolTipText = "Emulator is playing to frame " + PauseOnFrame.Value + " click to stop seek";
			}
			else if (EmulatorPaused)
			{
				PauseStatusButton.Image = Properties.Resources.Pause;
				PauseStatusButton.Visible = true;
				PauseStatusButton.ToolTipText = "Emulator Paused";
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
			var rewind = Global.Rewinder.RewindActive && (Global.ClientControls["Rewind"] || PressRewind);
			var fastForward = Global.ClientControls["Fast Forward"] || FastForward;
			var turbo = IsTurboing;

			int speedPercent = fastForward ? Global.Config.SpeedPercentAlternate : Global.Config.SpeedPercent;

			if (rewind)
			{
				speedPercent = Math.Max(speedPercent * Global.Config.RewindSpeedMultiplier / Global.Rewinder.RewindFrequency, 5);
			}

			Global.DisableSecondaryThrottling = _unthrottled || turbo || fastForward || rewind;

			// realtime throttle is never going to be so exact that using a double here is wrong
			_throttle.SetCoreFps(Emulator.VsyncRate());
			_throttle.signal_paused = EmulatorPaused;
			_throttle.signal_unthrottle = _unthrottled || turbo;

			// zero 26-mar-2016 - vsync and vsync throttle here both is odd, but see comments elsewhere about triple buffering
			_throttle.signal_overrideSecondaryThrottle = (fastForward || rewind) && (Global.Config.SoundThrottle || Global.Config.VSyncThrottle || Global.Config.VSync);
			_throttle.SetSpeedPercent(speedPercent);
		}

		private void SetSpeedPercentAlternate(int value)
		{
			Global.Config.SpeedPercentAlternate = value;
			SyncThrottle();
			GlobalWin.OSD.AddMessage("Alternate Speed: " + value + "%");
		}

		private void SetSpeedPercent(int value)
		{
			Global.Config.SpeedPercent = value;
			SyncThrottle();
			GlobalWin.OSD.AddMessage("Speed: " + value + "%");
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
				if (_blankCursor == null)
				{
					var ms = new MemoryStream(Properties.Resources.BlankCursor);
					_blankCursor = new Cursor(ms);
				}

				PresentationPanel.Control.Cursor = _blankCursor;
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

		private BitmapBuffer MakeScreenshotImage()
		{
			return GlobalWin.DisplayManager.RenderVideoProvider(_currentVideoProvider);
		}

		private void SaveSlotSelectedMessage()
		{
			int slot = Global.Config.SaveSlot;
			string emptypart = _stateSlots.HasSlot(slot) ? "" : " (empty)";
			string message = $"Slot {slot}{emptypart} selected.";
			GlobalWin.OSD.AddMessage(message);
		}

		private void Render()
		{
			if (Global.Config.DispSpeedupFeatures == 0)
			{
				return;
			}

			var video = _currentVideoProvider;
			Size currVideoSize = new Size(video.BufferWidth, video.BufferHeight);
			Size currVirtualSize = new Size(video.VirtualWidth, video.VirtualHeight);

			bool resizeFramebuffer = false;
			if (currVideoSize != _lastVideoSize || currVirtualSize != _lastVirtualSize)
				resizeFramebuffer = true;

			bool isZero = false;
			if (currVideoSize.Width == 0 || currVideoSize.Height == 0 || currVirtualSize.Width == 0 || currVirtualSize.Height == 0)
				isZero = true;
			
			//don't resize if the new size is 0 somehow; we'll wait until we have a sensible size
			if(isZero)
				resizeFramebuffer = false;

			if(resizeFramebuffer)
			{
				_lastVideoSize = currVideoSize;
				_lastVirtualSize = currVirtualSize;
				FrameBufferResized();
			}

			//rendering flakes out egregiously if we have a zero size
			//can we fix it later not to?
			if(isZero)
				GlobalWin.DisplayManager.Blank();
			else
				GlobalWin.DisplayManager.UpdateSource(video);
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
			typeof(ToolStrip).InvokeMember("ProcessMnemonicInternal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Instance, null, MainformMenu, new object[] { c });
		}

		public static string FormatFilter(params string[] args)
		{
			var sb = new StringBuilder();
			if (args.Length % 2 != 0)
			{
				throw new ArgumentException();
			}

			var num = args.Length / 2;
			for (int i = 0; i < num; i++)
			{
				sb.AppendFormat("{0} ({1})|{1}", args[i * 2], args[(i * 2) + 1]);
				if (i != num - 1)
				{
					sb.Append('|');
				}
			}

			var str = sb.ToString().Replace("%ARCH%", "*.zip;*.rar;*.7z;*.gz");
			str = str.Replace(";", "; ");
			return str;
		}

		public static string RomFilter
		{
			get
			{
				if (VersionInfo.DeveloperBuild)
				{
					return FormatFilter(
						"Rom Files", "*.nes;*.fds;*.unf;*.sms;*.gg;*.sg;*.pce;*.sgx;*.bin;*.smd;*.rom;*.a26;*.a78;*.lnx;*.m3u;*.cue;*.ccd;*.exe;*.gb;*.gbc;*.gba;*.gen;*.md;*.32x;*.col;*.int;*.smc;*.sfc;*.prg;*.d64;*.g64;*.crt;*.tap;*.sgb;*.xml;*.z64;*.v64;*.n64;*.ws;*.wsc;*.dsk;*.do;*.po;*.vb;*.ngp;*.ngc;*.psf;*.minipsf;*.nsf;%ARCH%",
						"Music Files", "*.psf;*.minipsf;*.sid;*.nsf",
						"Disc Images", "*.cue;*.ccd;*.m3u",
						"NES", "*.nes;*.fds;*.unf;*.nsf;%ARCH%",
						"Super NES", "*.smc;*.sfc;*.xml;%ARCH%",
						"Master System", "*.sms;*.gg;*.sg;%ARCH%",
						"PC Engine", "*.pce;*.sgx;*.cue;*.ccd;%ARCH%",
						"TI-83", "*.rom;%ARCH%",
						"Archive Files", "%ARCH%",
						"Savestate", "*.state",
						"Atari 2600", "*.a26;*.bin;%ARCH%",
						"Atari 7800", "*.a78;*.bin;%ARCH%",
						"Atari Lynx", "*.lnx;%ARCH%",
						"Genesis", "*.gen;*.smd;*.bin;*.md;*.32x;*.cue;*.ccd;%ARCH%",
						"Gameboy", "*.gb;*.gbc;*.sgb;%ARCH%",
						"Gameboy Advance", "*.gba;%ARCH%",
						"Colecovision", "*.col;%ARCH%",
						"Intellivision", "*.int;*.bin;*.rom;%ARCH%",
						"PlayStation", "*.cue;*.ccd;*.m3u",
						"PSX Executables (experimental)", "*.exe",
						"PSF Playstation Sound File", "*.psf;*.minipsf",
						"Commodore 64", "*.prg; *.d64, *.g64; *.crt; *.tap;%ARCH%",
						"SID Commodore 64 Music File", "*.sid;%ARCH%",
						"Nintendo 64", "*.z64;*.v64;*.n64",
						"WonderSwan", "*.ws;*.wsc;%ARCH%",
						"Apple II", "*.dsk;*.do;*.po;%ARCH%",
						"Virtual Boy", "*.vb;%ARCH%",
						"Neo Geo Pocket", "*.ngp;*.ngc;%ARCH%",
						"All Files", "*.*");
				}

				return FormatFilter(
					"Rom Files", "*.nes;*.fds;*.unf;*.sms;*.gg;*.sg;*.gb;*.gbc;*.gba;*.pce;*.sgx;*.bin;*.smd;*.gen;*.md;*.32x;*.smc;*.sfc;*.a26;*.a78;*.lnx;*.col;*.int;*.rom;*.m3u;*.cue;*.ccd;*.sgb;*.z64;*.v64;*.n64;*.ws;*.wsc;*.xml;*.dsk;*.do;*.po;*.psf;*.ngp;*.ngc;*.prg;*.d64;*.g64;*.minipsf;*.nsf;%ARCH%",
					"Disc Images", "*.cue;*.ccd;*.m3u",
					"NES", "*.nes;*.fds;*.unf;*.nsf;%ARCH%",
					"Super NES", "*.smc;*.sfc;*.xml;%ARCH%",
					"PlayStation", "*.cue;*.ccd;*.m3u",
					"PSF Playstation Sound File", "*.psf;*.minipsf",
					"Nintendo 64", "*.z64;*.v64;*.n64",
					"Gameboy", "*.gb;*.gbc;*.sgb;%ARCH%",
					"Gameboy Advance", "*.gba;%ARCH%",
					"Master System", "*.sms;*.gg;*.sg;%ARCH%",
					"PC Engine", "*.pce;*.sgx;*.cue;*.ccd;%ARCH%",
					"Atari 2600", "*.a26;%ARCH%",
					"Atari 7800", "*.a78;%ARCH%",
					"Atari Lynx", "*.lnx;%ARCH%",
					"Colecovision", "*.col;%ARCH%",
					"Intellivision", "*.int;*.bin;*.rom;%ARCH%",
					"TI-83", "*.rom;%ARCH%",
					"Archive Files", "%ARCH%",
					"Savestate", "*.state",
					"Genesis", "*.gen;*.md;*.smd;*.32x;*.bin;*.cue;*.ccd;%ARCH%",
					"WonderSwan", "*.ws;*.wsc;%ARCH%",
					"Apple II", "*.dsk;*.do;*.po;%ARCH%",
					"Virtual Boy", "*.vb;%ARCH%",
					"Neo Geo Pocket", "*.ngp;*.ngc;%ARCH%",
					"Commodore 64", "*.prg; *.d64, *.g64; *.crt; *.tap;%ARCH%",
					"All Files", "*.*");
			}
		}

		private void OpenRom()
		{
			var ofd = new OpenFileDialog
			{
				InitialDirectory = PathManager.GetRomsPath(Emulator.SystemId),
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
			Global.Config.LastRomPath = file.DirectoryName;
			_lastOpenRomFilter = ofd.FilterIndex;

			var lra = new LoadRomArgs { OpenAdvanced = new OpenAdvanced_OpenRom { Path = file.FullName } };
			LoadRom(file.FullName, lra);
		}

		private void CoreSyncSettings(object sender, RomLoader.SettingsLoadArgs e)
		{
			if (Global.MovieSession.QueuedMovie != null)
			{
				if (!string.IsNullOrWhiteSpace(Global.MovieSession.QueuedMovie.SyncSettingsJson))
				{
					e.Settings = ConfigService.LoadWithType(Global.MovieSession.QueuedMovie.SyncSettingsJson);
				}
				else
				{
					e.Settings = Global.Config.GetCoreSyncSettings(e.Core);

					// adelikat: only show this nag if the core actually has sync settings, not all cores do
					if (e.Settings != null && !_supressSyncSettingsWarning)
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
				e.Settings = Global.Config.GetCoreSyncSettings(e.Core);
			}
		}

		private static void CoreSettings(object sender, RomLoader.SettingsLoadArgs e)
		{
			e.Settings = Global.Config.GetCoreSettings(e.Core);
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

		/// <summary>
		/// send core sync settings to emu, setting reboot flag if needed
		/// </summary>
		public void PutCoreSyncSettings(object o)
		{
			var settable = new SettingsAdapter(Emulator);
			if (Global.MovieSession.Movie.IsActive)
			{
				GlobalWin.OSD.AddMessage("Attempt to change sync-relevant settings while recording BLOCKED.");
			}
			else if (settable.HasSyncSettings && settable.PutSyncSettings(o))
			{
				FlagNeedsReboot();
			}
		}

		private void SaveConfig(string path = "")
		{
			if (Global.Config.SaveWindowPosition)
			{
				if (Global.Config.MainWndx != -32000) // When minimized location is -32000, don't save this into the config file!
				{
					Global.Config.MainWndx = Location.X;
				}

				if (Global.Config.MainWndy != -32000)
				{
					Global.Config.MainWndy = Location.Y;
				}
			}
			else
			{
				Global.Config.MainWndx = -1;
				Global.Config.MainWndy = -1;
			}

			if (Global.Config.ShowLogWindow)
			{
				LogConsole.SaveConfigSettings();
			}

			if (string.IsNullOrEmpty(path))
			{
				path = PathManager.DefaultIniPath;
			}

			ConfigService.Save(path, Global.Config);
		}

		private static void ToggleFps()
		{
			Global.Config.DisplayFPS ^= true;
		}

		private static void ToggleFrameCounter()
		{
			Global.Config.DisplayFrameCounter ^= true;
		}

		private static void ToggleLagCounter()
		{
			Global.Config.DisplayLagCounter ^= true;
		}

		private static void ToggleInputDisplay()
		{
			Global.Config.DisplayInput ^= true;
		}

		private static void ToggleSound()
		{
			Global.Config.SoundEnabled ^= true;
			GlobalWin.Sound.StopSound();
			GlobalWin.Sound.StartSound();
		}

		private static void VolumeUp()
		{
			Global.Config.SoundVolume += 10;
			if (Global.Config.SoundVolume > 100)
			{
				Global.Config.SoundVolume = 100;
			}

			GlobalWin.OSD.AddMessage("Volume " + Global.Config.SoundVolume);
		}

		private static void VolumeDown()
		{
			Global.Config.SoundVolume -= 10;
			if (Global.Config.SoundVolume < 0)
			{
				Global.Config.SoundVolume = 0;
			}

			GlobalWin.OSD.AddMessage("Volume " + Global.Config.SoundVolume);
		}

		private void SoftReset()
		{
			// is it enough to run this for one frame? maybe..
			if (Emulator.ControllerDefinition.BoolButtons.Contains("Reset"))
			{
				if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
				{
					Global.ClickyVirtualPadController.Click("Reset");
					GlobalWin.OSD.AddMessage("Reset button pressed.");
				}
			}
		}

		private void HardReset()
		{
			// is it enough to run this for one frame? maybe..
			if (Emulator.ControllerDefinition.BoolButtons.Contains("Power"))
			{
				if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
				{
					Global.ClickyVirtualPadController.Click("Power");
					GlobalWin.OSD.AddMessage("Power button pressed.");
				}
			}
		}

		private void UpdateStatusSlots()
		{
			_stateSlots.Update();

			Slot0StatusButton.ForeColor = _stateSlots.HasSlot(0) ? Global.Config.SaveSlot == 0 ? SystemColors.HighlightText : SystemColors.WindowText : SystemColors.GrayText;
			Slot1StatusButton.ForeColor = _stateSlots.HasSlot(1) ? Global.Config.SaveSlot == 1 ? SystemColors.HighlightText : SystemColors.WindowText : SystemColors.GrayText;
			Slot2StatusButton.ForeColor = _stateSlots.HasSlot(2) ? Global.Config.SaveSlot == 2 ? SystemColors.HighlightText : SystemColors.WindowText : SystemColors.GrayText;
			Slot3StatusButton.ForeColor = _stateSlots.HasSlot(3) ? Global.Config.SaveSlot == 3 ? SystemColors.HighlightText : SystemColors.WindowText : SystemColors.GrayText;
			Slot4StatusButton.ForeColor = _stateSlots.HasSlot(4) ? Global.Config.SaveSlot == 4 ? SystemColors.HighlightText : SystemColors.WindowText : SystemColors.GrayText;
			Slot5StatusButton.ForeColor = _stateSlots.HasSlot(5) ? Global.Config.SaveSlot == 5 ? SystemColors.HighlightText : SystemColors.WindowText : SystemColors.GrayText;
			Slot6StatusButton.ForeColor = _stateSlots.HasSlot(6) ? Global.Config.SaveSlot == 6 ? SystemColors.HighlightText : SystemColors.WindowText : SystemColors.GrayText;
			Slot7StatusButton.ForeColor = _stateSlots.HasSlot(7) ? Global.Config.SaveSlot == 7 ? SystemColors.HighlightText : SystemColors.WindowText : SystemColors.GrayText;
			Slot8StatusButton.ForeColor = _stateSlots.HasSlot(8) ? Global.Config.SaveSlot == 8 ? SystemColors.HighlightText : SystemColors.WindowText : SystemColors.GrayText;
			Slot9StatusButton.ForeColor = _stateSlots.HasSlot(9) ? Global.Config.SaveSlot == 9 ? SystemColors.HighlightText : SystemColors.WindowText : SystemColors.GrayText;

			Slot0StatusButton.BackColor = Global.Config.SaveSlot == 0 ? SystemColors.Highlight : SystemColors.Control;
			Slot1StatusButton.BackColor = Global.Config.SaveSlot == 1 ? SystemColors.Highlight : SystemColors.Control;
			Slot2StatusButton.BackColor = Global.Config.SaveSlot == 2 ? SystemColors.Highlight : SystemColors.Control;
			Slot3StatusButton.BackColor = Global.Config.SaveSlot == 3 ? SystemColors.Highlight : SystemColors.Control;
			Slot4StatusButton.BackColor = Global.Config.SaveSlot == 4 ? SystemColors.Highlight : SystemColors.Control;
			Slot5StatusButton.BackColor = Global.Config.SaveSlot == 5 ? SystemColors.Highlight : SystemColors.Control;
			Slot6StatusButton.BackColor = Global.Config.SaveSlot == 6 ? SystemColors.Highlight : SystemColors.Control;
			Slot7StatusButton.BackColor = Global.Config.SaveSlot == 7 ? SystemColors.Highlight : SystemColors.Control;
			Slot8StatusButton.BackColor = Global.Config.SaveSlot == 8 ? SystemColors.Highlight : SystemColors.Control;
			Slot9StatusButton.BackColor = Global.Config.SaveSlot == 9 ? SystemColors.Highlight : SystemColors.Control;

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
			var bb = GlobalWin.DisplayManager.RenderOffscreen(_currentVideoProvider, true);
			bb.DiscardAlpha();
			return bb;
		}

		private void IncreaseWindowSize()
		{
			switch (Global.Config.TargetZoomFactors[Emulator.SystemId])
			{
				case 1:
					Global.Config.TargetZoomFactors[Emulator.SystemId] = 2;
					break;
				case 2:
					Global.Config.TargetZoomFactors[Emulator.SystemId] = 3;
					break;
				case 3:
					Global.Config.TargetZoomFactors[Emulator.SystemId] = 4;
					break;
				case 4:
					Global.Config.TargetZoomFactors[Emulator.SystemId] = 5;
					break;
				case 5:
					Global.Config.TargetZoomFactors[Emulator.SystemId] = 10;
					break;
				case 10:
					return;
			}

			GlobalWin.OSD.AddMessage("Screensize set to " + Global.Config.TargetZoomFactors[Emulator.SystemId] + "x");
			FrameBufferResized();
		}

		private void DecreaseWindowSize()
		{
			switch (Global.Config.TargetZoomFactors[Emulator.SystemId])
			{
				case 1:
					return;
				case 2:
					Global.Config.TargetZoomFactors[Emulator.SystemId] = 1;
					break;
				case 3:
					Global.Config.TargetZoomFactors[Emulator.SystemId] = 2;
					break;
				case 4:
					Global.Config.TargetZoomFactors[Emulator.SystemId] = 3;
					break;
				case 5:
					Global.Config.TargetZoomFactors[Emulator.SystemId] = 4;
					break;
				case 10:
					Global.Config.TargetZoomFactors[Emulator.SystemId] = 5;
					return;
			}

			GlobalWin.OSD.AddMessage("Screensize set to " + Global.Config.TargetZoomFactors[Emulator.SystemId] + "x");
			FrameBufferResized();
		}

		private void IncreaseSpeed()
		{
			if (!Global.Config.ClockThrottle)
			{
				GlobalWin.OSD.AddMessage("Unable to change speed, please switch to clock throttle");
				return;
			}

			var oldp = Global.Config.SpeedPercent;
			int newp;

			if (oldp < 3)
			{
				newp = 3;
			}
			else if (oldp < 6)
			{
				newp = 6;
			}
			else if (oldp < 12)
			{
				newp = 12;
			}
			else if (oldp < 25)
			{
				newp = 25;
			}
			else if (oldp < 50)
			{
				newp = 50;
			}
			else if (oldp < 75)
			{
				newp = 75;
			}
			else if (oldp < 100)
			{
				newp = 100;
			}
			else if (oldp < 150)
			{
				newp = 150;
			}
			else if (oldp < 200)
			{
				newp = 200;
			}
			else if (oldp < 300)
			{
				newp = 300;
			}
			else if (oldp < 400)
			{
				newp = 400;
			}
			else if (oldp < 800)
			{
				newp = 800;
			}
			else if (oldp < 1600)
			{
				newp = 1600;
			}
			else if (oldp < 3200)
			{
				newp = 3200;
			}
			else
			{
				newp = 6400;
			}

			SetSpeedPercent(newp);
		}

		private void DecreaseSpeed()
		{
			if (!Global.Config.ClockThrottle)
			{
				GlobalWin.OSD.AddMessage("Unable to change speed, please switch to clock throttle");
				return;
			}

			var oldp = Global.Config.SpeedPercent;
			int newp;

			if (oldp > 3200)
			{
				newp = 3200;
			}
			else if (oldp > 1600)
			{
				newp = 1600;
			}
			else if (oldp > 800)
			{
				newp = 800;
			}
			else if (oldp > 400)
			{
				newp = 400;
			}
			else if (oldp > 300)
			{
				newp = 300;
			}
			else if (oldp > 200)
			{
				newp = 200;
			}
			else if (oldp > 150)
			{
				newp = 150;
			}
			else if (oldp > 100)
			{
				newp = 100;
			}
			else if (oldp > 75)
			{
				newp = 75;
			}
			else if (oldp > 50)
			{
				newp = 50;
			}
			else if (oldp > 25)
			{
				newp = 25;
			}
			else if (oldp > 12)
			{
				newp = 12;
			}
			else if (oldp > 6)
			{
				newp = 6;
			}
			else if (oldp > 3)
			{
				newp = 3;
			}
			else
			{
				newp = 1;
			}

			SetSpeedPercent(newp);
		}

		private static void SaveMovie()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.Save();
				GlobalWin.OSD.AddMessage(Global.MovieSession.Movie.Filename + " saved.");
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

		private static void ToggleModePokeMode()
		{
			Global.Config.MoviePlaybackPokeMode ^= true;
			GlobalWin.OSD.AddMessage(Global.Config.MoviePlaybackPokeMode ? "Movie Poke mode enabled" : "Movie Poke mode disabled");
		}

		private static void ToggleBackgroundInput()
		{
			Global.Config.AcceptBackgroundInput ^= true;
			GlobalWin.OSD.AddMessage(Global.Config.AcceptBackgroundInput
										 ? "Background Input enabled"
										 : "Background Input disabled");
		}

		private static void VsyncMessage()
		{
			GlobalWin.OSD.AddMessage(
				"Display Vsync set to " + (Global.Config.VSync ? "on" : "off"));
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
					if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
					{
						Global.ClickyVirtualPadController.Click(button);
						GlobalWin.OSD.AddMessage(msg);
					}
				}
			});
		}

		private const int WmDevicechange = 0x0219;

		// Alt key hacks
		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case WmDevicechange:
					GamePad.Initialize();
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
		}

		private void ToggleKeyPriority()
		{
			Global.Config.Input_Hotkey_OverrideOptions++;
			if (Global.Config.Input_Hotkey_OverrideOptions > 2)
			{
				Global.Config.Input_Hotkey_OverrideOptions = 0;
			}

			UpdateKeyPriorityIcon();
			switch (Global.Config.Input_Hotkey_OverrideOptions)
			{
				case 0:
					GlobalWin.OSD.AddMessage("Key priority set to Both Hotkey and Input");
					break;
				case 1:
					GlobalWin.OSD.AddMessage("Key priority set to Input over Hotkey");
					break;
				case 2:
					GlobalWin.OSD.AddMessage("Key priority set to Input");
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

		public bool IsLagFrame
		{
			get
			{
				if (Emulator.CanPollInput())
				{
					return Emulator.AsInputPollable().IsLagFrame;
				}

				return false;
			}
		}

		private void StepRunLoop_Core(bool force = false)
		{
			var runFrame = false;
			_runloopFrameAdvance = false;
			var currentTimestamp = Stopwatch.GetTimestamp();

			double frameAdvanceTimestampDeltaMs = (double)(currentTimestamp - _frameAdvanceTimestamp) / Stopwatch.Frequency * 1000.0;
			bool frameProgressTimeElapsed = frameAdvanceTimestampDeltaMs >= Global.Config.FrameProgressDelayMs;

			if (Global.Config.SkipLagFrame && IsLagFrame && frameProgressTimeElapsed && Emulator.Frame > 0)
			{
				runFrame = true;
			}

			if (Global.ClientControls["Frame Advance"] || PressFrameAdvance || HoldFrameAdvance)
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

			bool returnToRecording;
			bool isRewinding = Rewind(ref runFrame, currentTimestamp, out returnToRecording);

			float atten = 0;

			if (runFrame || force)
			{
				var isFastForwarding = Global.ClientControls["Fast Forward"] || IsTurboing;
				var isFastForwardingOrRewinding = isFastForwarding || isRewinding || _unthrottled;

				if (isFastForwardingOrRewinding != _lastFastForwardingOrRewinding)
				{
					InitializeFpsData();
				}

				_lastFastForwardingOrRewinding = isFastForwardingOrRewinding;

				// client input-related duties
				GlobalWin.OSD.ClearGUIText();

				Global.CheatList.Pulse();

				// zero 03-may-2014 - moved this before call to UpdateToolsBefore(), since it seems to clear the state which a lua event.framestart is going to want to alter
				Global.ClickyVirtualPadController.FrameTick();
				Global.LuaAndAdaptor.FrameTick();

				if (GlobalWin.Tools.Has<LuaConsole>() && !SuppressLua)
				{
					GlobalWin.Tools.LuaConsole.LuaImp.CallFrameBeforeEvent();
				}

				if (IsTurboing)
				{
					GlobalWin.Tools.FastUpdateBefore();
				}
				else
				{
					GlobalWin.Tools.UpdateToolsBefore();
				}

				_framesSinceLastFpsUpdate++;

				UpdateFpsDisplay(currentTimestamp, isRewinding, isFastForwarding);

				CaptureRewind(isRewinding);

				// Set volume, if enabled
				if (Global.Config.SoundEnabledNormal)
				{
					atten = Global.Config.SoundVolume / 100.0f;

					if (isFastForwardingOrRewinding)
					{
						if (Global.Config.SoundEnabledRWFF)
						{
							atten *= Global.Config.SoundVolumeRWFF / 100.0f;
						}
						else
						{
							atten = 0;
						}
					}

					// Mute if using Frame Advance/Frame Progress
					if (_runloopFrameAdvance && Global.Config.MuteFrameAdvance)
					{
						atten = 0;
					}
				}

				Global.MovieSession.HandleMovieOnFrameLoop();

				if (Global.Config.AutosaveSaveRAM)
				{
					if (AutoFlushSaveRamIn-- <= 0)
					{
						FlushSaveRAM(true);
					}
				}
				// why not skip audio if the user doesnt want sound
				bool renderSound = (Global.Config.SoundEnabled && !IsTurboing) || (_currAviWriter?.UsesAudio ?? false);
				if (!renderSound)
				{
					atten = 0;
				}

				bool render = !_throttle.skipNextFrame || (_currAviWriter?.UsesVideo ?? false);
				Emulator.FrameAdvance(Global.ControllerOutput, render, renderSound);

				Global.MovieSession.HandleMovieAfterFrameLoop();

				if (returnToRecording)
				{
					Global.MovieSession.Movie.SwitchToRecord();
				}

				if (isRewinding && !IsRewindSlave && Global.MovieSession.Movie.IsRecording)
				{
					Global.MovieSession.Movie.Truncate(Global.Emulator.Frame);
				}

				Global.CheatList.Pulse();

				if (!PauseAvi)
				{
					AvFrameAdvance();
				}

				if (IsLagFrame && Global.Config.AutofireLagFrames)
				{
					Global.AutoFireController.IncrementStarts();
				}

				Global.AutofireStickyXORAdapter.IncrementLoops(IsLagFrame);

				PressFrameAdvance = false;

				if (GlobalWin.Tools.Has<LuaConsole>() && !SuppressLua)
				{
					GlobalWin.Tools.LuaConsole.LuaImp.CallFrameAfterEvent();
				}

				if (IsTurboing)
				{
					GlobalWin.Tools.FastUpdateAfter();
				}
				else
				{
					UpdateToolsAfter(SuppressLua);
				}

				if (GlobalWin.Tools.IsLoaded<TAStudio>() &&
					GlobalWin.Tools.TAStudio.LastPositionFrame == Emulator.Frame)
				{
					if (PauseOnFrame.HasValue &&
						PauseOnFrame.Value <= GlobalWin.Tools.TAStudio.LastPositionFrame)
					{
						TasMovieRecord record = (Global.MovieSession.Movie as TasMovie)[Emulator.Frame];
						if (!record.Lagged.HasValue && IsSeeking)
						{
							// haven't yet greenzoned the frame, hence it's after editing
							// then we want to pause here. taseditor fasion
							PauseEmulator();
						}
					}
				}

				if (IsSeeking && Emulator.Frame == PauseOnFrame.Value)
				{
					PauseEmulator();
					PauseOnFrame = null;
					if (GlobalWin.Tools.IsLoaded<TAStudio>())
					{
						GlobalWin.Tools.TAStudio.StopSeeking();
					}
				}
			}

			if (Global.ClientControls["Rewind"] || PressRewind)
			{
				UpdateToolsAfter();
			}

			GlobalWin.Sound.UpdateSound(atten);
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

			GlobalWin.OSD.FPS = fpsString;

			// need to refresh window caption in this case
			if (Global.Config.DispSpeedupFeatures == 0)
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
		/// <param name="videowritername">match the short name of an <seealso cref="IVideoWriter"/></param>
		/// <param name="filename">filename to save to</param>
		private void RecordAv(string videowritername, string filename)
		{
			RecordAvBase(videowritername, filename, true);
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
		private void RecordAvBase(string videowritername, string filename, bool unattended)
		{
			if (_currAviWriter != null)
			{
				return;
			}

			// select IVideoWriter to use
			IVideoWriter aw;

			if (string.IsNullOrEmpty(videowritername) && !string.IsNullOrEmpty(Global.Config.VideoWriter))
			{
				videowritername = Global.Config.VideoWriter;
			}

			_dumpaudiosync = Global.Config.VideoWriterAudioSync;
			if (unattended && !string.IsNullOrEmpty(videowritername))
			{
				aw = VideoWriterInventory.GetVideoWriter(videowritername);
			}
			else
			{
				aw = VideoWriterChooserForm.DoVideoWriterChoserDlg(VideoWriterInventory.GetAllWriters(), this,
					out _avwriterResizew, out _avwriterResizeh, out _avwriterpad, ref _dumpaudiosync);
			}

			if (aw == null)
			{
				GlobalWin.OSD.AddMessage(
					unattended ? $"Couldn't start video writer \"{videowritername}\"" : "A/V capture canceled.");

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
					if (usingAvi && !string.IsNullOrEmpty(Global.Config.AVICodecToken))
					{
						aw.SetDefaultVideoCodecToken();
					}

					var token = aw.AcquireVideoCodecToken(this);
					if (token == null)
					{
						GlobalWin.OSD.AddMessage("A/V capture canceled.");
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
						var fbd = new FolderBrowserEx();
						if (fbd.ShowDialog() == DialogResult.Cancel)
						{
							aw.Dispose();
							return;
						}

						pathForOpenFile = fbd.SelectedPath;
					}
					else
					{
						var sfd = new SaveFileDialog();
						if (Global.Game != null)
						{
							sfd.FileName = PathManager.FilesystemSafeName(Global.Game) + "." + ext; // dont use Path.ChangeExtension, it might wreck game names with dots in them
							sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.AvPathFragment, null);
						}
						else
						{
							sfd.FileName = "NULL";
							sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.AvPathFragment, null);
						}

						sfd.Filter = string.Format("{0} (*.{0})|*.{0}|All Files|*.*", ext);

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
				GlobalWin.OSD.AddMessage("A/V capture started");
				AVIStatusLabel.Image = Properties.Resources.AVI;
				AVIStatusLabel.ToolTipText = "A/V capture in progress";
				AVIStatusLabel.Visible = true;
			}
			catch
			{
				GlobalWin.OSD.AddMessage("A/V capture failed!");
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
			GlobalWin.OSD.AddMessage("A/V capture aborted");
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
			GlobalWin.OSD.AddMessage("A/V capture stopped");
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
					if (argParse._currAviWriterFrameList != null)
					{
						if (!argParse._currAviWriterFrameList.Contains(Emulator.Frame))
						{
							goto HANDLE_AUTODUMP;
						}
					}

					IVideoProvider output;
					IDisposable disposableOutput = null;
					if (_avwriterResizew > 0 && _avwriterResizeh > 0)
					{
						BitmapBuffer bbin = null;
						Bitmap bmpin = null;
						try
						{
							bbin = Global.Config.AVI_CaptureOSD
								? CaptureOSD()
								: new BitmapBuffer(_currentVideoProvider.BufferWidth, _currentVideoProvider.BufferHeight, _currentVideoProvider.GetVideoBuffer());

							bbin.DiscardAlpha();

							var bmpout = new Bitmap(_avwriterResizew, _avwriterResizeh, PixelFormat.Format32bppArgb);
							bmpin = bbin.ToSysdrawingBitmap();
							using (var g = Graphics.FromImage(bmpout))
							{
								if (_avwriterpad)
								{
									g.Clear(Color.FromArgb(_currentVideoProvider.BackgroundColor));
									g.DrawImageUnscaled(bmpin, (bmpout.Width - bmpin.Width) / 2, (bmpout.Height - bmpin.Height) / 2);
								}
								else
								{
									g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
									g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
									g.DrawImage(bmpin, new Rectangle(0, 0, bmpout.Width, bmpout.Height));
								}
							}

							output = new BmpVideoProvider(bmpout, _currentVideoProvider.VsyncNumerator, _currentVideoProvider.VsyncDenominator);
							disposableOutput = (IDisposable)output;
						}
						finally
						{
							bbin?.Dispose();
							bmpin?.Dispose();
						}
					}
					else
					{
						if (Global.Config.AVI_CaptureOSD)
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
					MessageBox.Show("Video dumping died:\n\n" + e);
					AbortAv();
				}

				HANDLE_AUTODUMP:
				if (argParse._autoDumpLength > 0)
				{
					argParse._autoDumpLength--;
					if (argParse._autoDumpLength == 0) // finish
					{
						StopAv();
						if (argParse._autoCloseOnDump)
						{
							_exit = true;
						}
					}
				}
			}
		}

		private int? LoadArhiveChooser(HawkFile file)
		{
			var ac = new ArchiveChooser(file);
			if (ac.ShowDialog(this) == DialogResult.OK)
			{
				return ac.SelectedMemberIndex;
			}

			return null;
		}

		#endregion

		#region Scheduled for refactor

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
					title = e.AttemptedCoreLoad + " load error";
				}

				MessageBox.Show(this, e.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void NotifyCoreComm(string message)
		{
			GlobalWin.OSD.AddMessage(message);
		}

		private string ChoosePlatformForRom(RomGame rom)
		{
			var platformChooser = new PlatformChooser
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

		// Still needs a good bit of refactoring
		public bool LoadRom(string path, LoadRomArgs args)
		{
			path = HawkFile.Util_ResolveLink(path);

			// default args
			if (args == null)
			{
				args = new LoadRomArgs();
			}

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
				// If deterministic emulation is passed in, respect that value regardless, else determine a good value (currently that simply means movies require deterministic emulaton)
				bool deterministic = args.Deterministic ?? Global.MovieSession.QueuedMovie != null;

				if (!GlobalWin.Tools.AskSave())
				{
					return false;
				}

				bool asLibretro = args.OpenAdvanced is OpenAdvanced_Libretro || args.OpenAdvanced is OpenAdvanced_LibretroNoGame;

				var loader = new RomLoader
				{
					ChooseArchive = LoadArhiveChooser,
					ChoosePlatform = ChoosePlatformForRom,
					Deterministic = deterministic,
					MessageCallback = GlobalWin.OSD.AddMessage,
					AsLibretro = asLibretro
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

				// we need to inform LoadRom which Libretro core to use...
				IOpenAdvanced ioa = args.OpenAdvanced;
				if (ioa is IOpenAdvancedLibretro)
				{
					var ioaretro = (IOpenAdvancedLibretro)ioa;

					// prepare a core specification
					// if it wasnt already specified, use the current default
					if (ioaretro.CorePath == null)
					{
						ioaretro.CorePath = Global.Config.LibretroCore;
					}

					nextComm.LaunchLibretroCore = ioaretro.CorePath;
					if (nextComm.LaunchLibretroCore == null)
					{
						throw new InvalidOperationException("Can't load a file via Libretro until a core is specified");
					}
				}

				if (ioa is OpenAdvanced_OpenRom)
				{
					var ioa_openrom = (OpenAdvanced_OpenRom)ioa;
					path = ioa_openrom.Path;
				}

				CoreFileProvider.SyncCoreCommInputSignals(nextComm);
				var result = loader.LoadRom(path, nextComm);

				// we need to replace the path in the OpenAdvanced with the canonical one the user chose.
				// It can't be done until loder.LoadRom happens (for CanonicalFullPath)
				// i'm not sure this needs to be more abstractly engineered yet until we have more OpenAdvanced examples
				if (ioa is OpenAdvanced_Libretro)
				{
					var oaretro = ioa as OpenAdvanced_Libretro;
					oaretro.token.Path = loader.CanonicalFullPath;
				}

				if (ioa is OpenAdvanced_OpenRom)
				{
					((OpenAdvanced_OpenRom)ioa).Path = loader.CanonicalFullPath;
				}

				if (result)
				{

					string loaderName = "*" + OpenAdvancedSerializer.Serialize(ioa);
					Emulator = loader.LoadedEmulator;
					Global.Game = loader.Game;
					CoreFileProvider.SyncCoreCommInputSignals(nextComm);
					InputManager.SyncControls();

					if (Emulator is TI83 && Global.Config.TI83autoloadKeyPad)
					{
						GlobalWin.Tools.Load<TI83KeyPad>();
					}

					if (loader.LoadedEmulator is NES)
					{
						var nes = (NES)loader.LoadedEmulator;
						if (!string.IsNullOrWhiteSpace(nes.GameName))
						{
							Global.Game.Name = nes.GameName;
						}

						Global.Game.Status = nes.RomStatus;
					}
					else if (loader.LoadedEmulator is QuickNES)
					{
						var qns = (QuickNES)loader.LoadedEmulator;
						if (!string.IsNullOrWhiteSpace(qns.BootGodName))
						{
							Global.Game.Name = qns.BootGodName;
						}

						if (qns.BootGodStatus.HasValue)
						{
							Global.Game.Status = qns.BootGodStatus.Value;
						}
					}

					if (Emulator.CoreComm.RomStatusDetails == null && loader.Rom != null)
					{
						Emulator.CoreComm.RomStatusDetails = $"{loader.Game.Name}\r\nSHA1:{loader.Rom.RomData.HashSHA1()}\r\nMD5:{loader.Rom.RomData.HashMD5()}\r\n";
					}

					if (Emulator.HasBoardInfo())
					{
						Console.WriteLine("Core reported BoardID: \"{0}\"", Emulator.AsBoardInfo().BoardName);
					}

					// restarts the lua console if a different rom is loaded.
					// im not really a fan of how this is done..
					if (Global.Config.RecentRoms.Empty || Global.Config.RecentRoms.MostRecent != loaderName)
					{
						GlobalWin.Tools.Restart<LuaConsole>();
					}

					Global.Config.RecentRoms.Add(loaderName);
					JumpLists.AddRecentItem(loaderName, ioa.DisplayName);

					// Don't load Save Ram if a movie is being loaded
					if (!Global.MovieSession.MovieIsQueued)
					{
						if (File.Exists(PathManager.SaveRamPath(loader.Game)))
						{
							LoadSaveRam();
						}
						else if (Global.Config.AutosaveSaveRAM && File.Exists(PathManager.AutoSaveRamPath(loader.Game)))
						{
							GlobalWin.OSD.AddMessage("AutoSaveRAM found, but SaveRAM was not saved");
						}
					}

					GlobalWin.Tools.Restart();

					if (Global.Config.LoadCheatFileByGame)
					{
						Global.CheatList.SetDefaultFileName(ToolManager.GenerateDefaultCheatFilename());
						if (Global.CheatList.AttemptToLoadCheatFile())
						{
							GlobalWin.OSD.AddMessage("Cheats file loaded");
						}
						else if (Global.CheatList.Any())
						{
							Global.CheatList.Clear();
						}
					}

					SetWindowText();
					_currentlyOpenRomPoopForAdvancedLoaderPleaseRefactorMe = loaderName;
					CurrentlyOpenRom = loaderName.Replace("*OpenRom*", ""); // POOP
					HandlePlatformMenus();
					_stateSlots.Clear();
					UpdateCoreStatusBarButton();
					UpdateDumpIcon();
					SetMainformMovieInfo();
					CurrentlyOpenRomArgs = args;
					GlobalWin.DisplayManager.Blank();

					Global.Rewinder.Initialize();

					Global.StickyXORAdapter.ClearStickies();
					Global.StickyXORAdapter.ClearStickyFloats();
					Global.AutofireStickyXORAdapter.ClearStickies();

					RewireSound();
					ToolFormBase.UpdateCheatRelatedTools(null, null);
					if (Global.Config.AutoLoadLastSaveSlot && _stateSlots.HasSlot(Global.Config.SaveSlot))
					{
						LoadQuickSave("QuickSave" + Global.Config.SaveSlot);
					}

					if (Global.FirmwareManager.RecentlyServed.Count > 0)
					{
						Console.WriteLine("Active Firmwares:");
						foreach (var f in Global.FirmwareManager.RecentlyServed)
						{
							Console.WriteLine("  {0} : {1}", f.FirmwareId, f.Hash);
						}
					}

					ClientApi.OnRomLoaded();
					return true;
				}
				else
				{
					// This shows up if there's a problem
					// TODO: put all these in a single method or something

					// The ROM has been loaded by a recursive invocation of the LoadROM method.
					if (!(Emulator is NullEmulator))
					{
						ClientApi.OnRomLoaded();
						return true;
					}

					HandlePlatformMenus();
					_stateSlots.Clear();
					UpdateStatusSlots();
					UpdateCoreStatusBarButton();
					UpdateDumpIcon();
					SetMainformMovieInfo();
					SetWindowText();
					return false;
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

		private string _currentlyOpenRomPoopForAdvancedLoaderPleaseRefactorMe = "";

		private void CommitCoreSettingsToConfig()
		{
			// save settings object
			var t = Emulator.GetType();
			var settable = new SettingsAdapter(Emulator);

			if (settable.HasSettings)
			{
				Global.Config.PutCoreSettings(settable.GetSettings(), t);
			}

			if (settable.HasSyncSettings && !Global.MovieSession.Movie.IsActive)
			{
				// don't trample config with loaded-from-movie settings
				Global.Config.PutCoreSyncSettings(settable.GetSyncSettings(), t);
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
				var path = PathManager.SaveRamPath(Global.Game);
				if (File.Exists(path))
				{
					File.Delete(path);
					GlobalWin.OSD.AddMessage("SRAM cleared.");
				}
			}
			else if (Emulator.HasSaveRam() && Emulator.AsSaveRam().SaveRamModified)
			{
				FlushSaveRAM();
			}

			StopAv();

			CommitCoreSettingsToConfig();
			if (Global.MovieSession.Movie.IsActive) // Note: this must be called after CommitCoreSettingsToConfig()
			{
				StopMovie();
			}

			Global.Rewinder.Uninitialize();

			if (GlobalWin.Tools.IsLoaded<TraceLogger>())
			{
				GlobalWin.Tools.Get<TraceLogger>().Restart();
			}

			Global.CheatList.SaveOnClose();
			Emulator.Dispose();
			var coreComm = CreateCoreComm();
			CoreFileProvider.SyncCoreCommInputSignals(coreComm);
			Emulator = new NullEmulator(coreComm, Global.Config.GetCoreSettings<NullEmulator>());
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
			if (GlobalWin.Tools.AskSave())
			{
				CloseGame(clearSram);
				var coreComm = CreateCoreComm();
				CoreFileProvider.SyncCoreCommInputSignals(coreComm);
				Emulator = new NullEmulator(coreComm, Global.Config.GetCoreSettings<NullEmulator>());
				Global.Game = GameInfo.NullInstance;

				GlobalWin.Tools.Restart();
				RewireSound();
				Text = "BizHawk" + (VersionInfo.DeveloperBuild ? " (interim) " : "");
				HandlePlatformMenus();
				_stateSlots.Clear();
				UpdateDumpIcon();
				UpdateCoreStatusBarButton();
				ClearHolds();
				PauseOnFrame = null;
				ToolFormBase.UpdateCheatRelatedTools(null, null);
				UpdateStatusSlots();
				CurrentlyOpenRom = null;
				CurrentlyOpenRomArgs = null;
			}
		}

		private static void ShowConversionError(string errorMsg)
		{
			MessageBox.Show(errorMsg, "Conversion error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private static void ProcessMovieImport(string fn)
		{
			MovieImport.ProcessMovieImport(fn, ShowConversionError, GlobalWin.OSD.AddMessage);
		}

		public void EnableRewind(bool enabled)
		{
			Global.Rewinder.SuspendRewind = !enabled;
			GlobalWin.OSD.AddMessage("Rewind " + (enabled ? "enabled" : "suspended"));
		}

		public void ClearRewindData()
		{
			Global.Rewinder.Clear();
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

		public void LoadState(string path, string userFriendlyStateName, bool fromLua = false, bool supressOSD = false) // Move to client.common
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
			bool wasCountingRerecords = Global.MovieSession.Movie.IsCountingRerecords;

			if (fromLua)
			{
				Global.MovieSession.Movie.IsCountingRerecords = false;
			}

			if (SavestateManager.LoadStateFile(path, userFriendlyStateName))
			{
				GlobalWin.OSD.ClearGUIText();
				ClientApi.OnStateLoaded(this, userFriendlyStateName);

				if (GlobalWin.Tools.Has<LuaConsole>())
				{
					GlobalWin.Tools.LuaConsole.LuaImp.CallLoadStateEvent(userFriendlyStateName);
				}

				SetMainformMovieInfo();
				GlobalWin.Tools.UpdateToolsBefore(fromLua);
				UpdateToolsAfter(fromLua);
				UpdateToolsLoadstate();
				Global.AutoFireController.ClearStarts();

				if (!IsRewindSlave && Global.MovieSession.Movie.IsActive)
				{
					ClearRewindData();
				}

				if (!supressOSD)
				{
					GlobalWin.OSD.AddMessage("Loaded state: " + userFriendlyStateName);
				}
			}
			else
			{
				GlobalWin.OSD.AddMessage("Loadstate error!");
			}

			Global.MovieSession.Movie.IsCountingRerecords = wasCountingRerecords;
		}

		public void LoadQuickSave(string quickSlotName, bool fromLua = false, bool supressOSD = false)
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			bool handled;
			ClientApi.OnBeforeQuickLoad(this, quickSlotName, out handled);
			if (handled)
			{
				return;
			}

			if (IsSavestateSlave)
			{
				Master.LoadQuickSave(SlotToInt(quickSlotName));
				return;
			}

			var path = PathManager.SaveStatePrefix(Global.Game) + "." + quickSlotName + ".State";
			if (!File.Exists(path))
			{
				GlobalWin.OSD.AddMessage("Unable to load " + quickSlotName + ".State");

				return;
			}

			LoadState(path, quickSlotName, fromLua, supressOSD);
		}

		public void SaveState(string path, string userFriendlyStateName, bool fromLua)
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

				GlobalWin.OSD.AddMessage("Saved state: " + userFriendlyStateName);
			}
			catch (IOException)
			{
				GlobalWin.OSD.AddMessage("Unable to save state " + path);
			}

			if (!fromLua)
			{
				UpdateStatusSlots();
			}
		}

		// TODO: should backup logic be stuffed in into Client.Common.SaveStateManager?
		public void SaveQuickSave(string quickSlotName)
		{
			if (!Emulator.HasSavestates())
			{
				return;
			}

			bool handled;
			ClientApi.OnBeforeQuickSave(this, quickSlotName, out handled);
			if (handled)
			{
				return;
			}

			if (IsSavestateSlave)
			{
				Master.SaveQuickSave(SlotToInt(quickSlotName));
				return;
			}

			var path = PathManager.SaveStatePrefix(Global.Game) + "." + quickSlotName + ".State";

			var file = new FileInfo(path);
			if (file.Directory != null && !file.Directory.Exists)
			{
				file.Directory.Create();
			}

			// Make backup first
			if (Global.Config.BackupSavestates)
			{
				Util.TryMoveBackupFile(path, path + ".bak");
			}

			SaveState(path, quickSlotName, false);

			if (GlobalWin.Tools.Has<LuaConsole>())
			{
				GlobalWin.Tools.LuaConsole.LuaImp.CallSaveStateEvent(quickSlotName);
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
			if (GlobalWin.Tools.IsLoaded<TAStudio>())
			{
				GlobalWin.Tools.TAStudio.NamedStatePending = true;
			}

			if (IsSavestateSlave)
			{
				Master.SaveStateAs();
				return;
			}

			var path = PathManager.GetSaveStatePath(Global.Game);

			var file = new FileInfo(path);
			if (file.Directory != null && !file.Directory.Exists)
			{
				file.Directory.Create();
			}

			var sfd = new SaveFileDialog
			{
				AddExtension = true,
				DefaultExt = "State",
				Filter = "Save States (*.State)|*.State|All Files|*.*",
				InitialDirectory = path,
				FileName = PathManager.SaveStatePrefix(Global.Game) + "." + "QuickSave0.State"
			};

			var result = sfd.ShowHawkDialog();
			if (result == DialogResult.OK)
			{
				SaveState(sfd.FileName, sfd.FileName, false);
			}

			if (GlobalWin.Tools.IsLoaded<TAStudio>())
			{
				GlobalWin.Tools.TAStudio.NamedStatePending = false;
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

			var ofd = new OpenFileDialog
			{
				InitialDirectory = PathManager.GetSaveStatePath(Global.Game),
				Filter = "Save States (*.State)|*.State|All Files|*.*",
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
					Master.SelectSlot(slot);
					return;
				}

				Global.Config.SaveSlot = slot;
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
					Master.PreviousSlot();
					return;
				}

				if (Global.Config.SaveSlot == 0)
				{
					Global.Config.SaveSlot = 9; // Wrap to end of slot list
				}
				else if (Global.Config.SaveSlot > 9)
				{
					Global.Config.SaveSlot = 9; // Meh, just in case
				}
				else
				{
					Global.Config.SaveSlot--;
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
					Master.NextSlot();
					return;
				}

				if (Global.Config.SaveSlot >= 9)
				{
					Global.Config.SaveSlot = 0; // Wrap to beginning of slot list
				}
				else if (Global.Config.SaveSlot < 0)
				{
					Global.Config.SaveSlot = 0; // Meh, just in case
				}
				else
				{
					Global.Config.SaveSlot++;
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
				if (Global.MovieSession.Movie.IsActive)
				{
					Global.MovieSession.ReadOnly ^= true;
					GlobalWin.OSD.AddMessage(Global.MovieSession.ReadOnly ? "Movie read-only mode" : "Movie read+write mode");
				}
				else
				{
					GlobalWin.OSD.AddMessage("No movie active");
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
				Global.MovieSession.StopMovie(saveChanges);
				SetMainformMovieInfo();
				UpdateStatusSlots();
			}
		}

		private void GBAcoresettingsToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Gameboy Advance Settings");
		}

		private void CaptureRewind(bool suppressCaptureRewind)
		{
			if (IsRewindSlave)
			{
				Master.CaptureRewind();
			}
			else if (!suppressCaptureRewind && Global.Rewinder.RewindActive)
			{
				Global.Rewinder.Capture();
			}
		}

		private void preferencesToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "VirtualBoy Settings");
		}

		private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Snes9x Settings");
		}

		private void preferencesToolStripMenuItem2_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "NeoPop Settings");
		}

		private void preferencesToolStripMenuItem3_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "PC-FX Settings");
		}

		private bool Rewind(ref bool runFrame, long currentTimestamp, out bool returnToRecording)
		{
			var isRewinding = false;

			returnToRecording = false;

			if (IsRewindSlave)
			{
				if (Global.ClientControls["Rewind"] || PressRewind)
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
						isRewinding = timestampDeltaMs >= Global.Config.FrameProgressDelayMs;

						// clear this flag once we get out of the progress stage
						if (isRewinding)
						{
							_frameRewindWasPaused = false;
						}

						// if we're freely running, there's no need for reverse frame progress semantics (that may be debateable though)
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
						runFrame = true; // TODO: the master should be deciding this!
						Master.Rewind();
					}
				}
				else
				{
					_frameRewindTimestamp = 0;
				}

				return isRewinding;
			}

			if (Global.Rewinder.RewindActive && (Global.ClientControls["Rewind"] || PressRewind))
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
						isRewinding = timestampDeltaMs >= Global.Config.FrameProgressDelayMs;
					}
				}
				else
				{
					isRewinding = true;
				}

				if (isRewinding)
				{
					runFrame = Global.Rewinder.Rewind(1);

					if (runFrame && Global.MovieSession.Movie.IsRecording)
					{
						Global.MovieSession.Movie.SwitchToPlay();
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
