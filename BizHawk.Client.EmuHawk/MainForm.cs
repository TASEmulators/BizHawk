using System;
using System.Collections.Generic;
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
using BizHawk.Emulation.Cores.Atari.Atari2600;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.DiscSystem;
using BizHawk.Emulation.Cores.Nintendo.N64;

using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Client.EmuHawk.CoreExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm : Form
	{
		#region Constructors and Initialization, and Tear down

		private void MainForm_Load(object sender, EventArgs e)
		{
			SetWindowText();

			Global.CheatList.Changed += ToolHelpers.UpdateCheatRelatedTools;

			// Hide Status bar icons and general statusbar prep
			MainStatusBar.Padding = new Padding(MainStatusBar.Padding.Left, MainStatusBar.Padding.Top, MainStatusBar.Padding.Left, MainStatusBar.Padding.Bottom); // Workaround to remove extra padding on right
			PlayRecordStatusButton.Visible = false;
			AVIStatusLabel.Visible = false;
			SetPauseStatusbarIcon();
			ToolHelpers.UpdateCheatRelatedTools(null, null);
			RebootStatusBarIcon.Visible = false;
			UpdateNotification.Visible = false;
			StatusBarDiskLightOnImage = Properties.Resources.LightOn;
			StatusBarDiskLightOffImage = Properties.Resources.LightOff;
			LinkCableOn = Properties.Resources.connect_16x16;
			LinkCableOff = Properties.Resources.noconnect_16x16;
			UpdateCoreStatusBarButton();
			if (Global.Config.FirstBoot == true)
			{
				ProfileFirstBootLabel.Visible = true;
			}

			HandleToggleLightAndLink();

			// New version notification
			UpdateChecker.CheckComplete += (s2, e2) =>
			{
				if (IsDisposed) return;
				this.BeginInvoke(() => { UpdateNotification.Visible = UpdateChecker.IsNewVersionAvailable; });
			};
			UpdateChecker.BeginCheck(); // Won't actually check unless enabled by user
		}

		static MainForm()
		{
			// If this isnt here, then our assemblyresolving hacks wont work due to the check for MainForm.INTERIM
			// its.. weird. dont ask.
		}

		CoreComm CreateCoreComm()
		{
			CoreComm ret = new CoreComm(ShowMessageCoreComm, NotifyCoreComm);
			ret.RequestGLContext = () => GlobalWin.GLManager.CreateGLContext();
			ret.ActivateGLContext = (gl) => GlobalWin.GLManager.Activate((GLManager.ContextRef)gl);
			ret.DeactivateGLContext = () => GlobalWin.GLManager.Deactivate();
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

			new AutoResetEvent(false);
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
			Global.CheatList.Changed += ToolHelpers.UpdateCheatRelatedTools;

			UpdateStatusSlots();
			UpdateKeyPriorityIcon();

			// In order to allow late construction of this database, we hook up a delegate here to dearchive the data and provide it on demand
			// we could background thread this later instead if we wanted to be real clever
			NES.BootGodDB.GetDatabaseBytes = () =>
			{
				using (var NesCartFile =
						new HawkFile(Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "NesCarts.7z")).BindFirst())
				{
					return NesCartFile
						.GetStream()
						.ReadAllBytes();
				}
			};

			// TODO - replace this with some kind of standard dictionary-yielding parser in a separate component
			string cmdRom = null;
			string cmdLoadState = null;
			string cmdMovie = null;
			string cmdDumpType = null;
			string cmdDumpName = null;
			bool startFullscreen = false;
			for (int i = 0; i < args.Length; i++)
			{
				// For some reason sometimes visual studio will pass this to us on the commandline. it makes no sense.
				if (args[i] == ">")
				{
					i++;
					var stdout = args[i];
					Console.SetOut(new StreamWriter(stdout));
					continue;
				}

				var arg = args[i].ToLower();
				if (arg.StartsWith("--load-slot="))
				{
					cmdLoadState = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--movie="))
				{
					cmdMovie = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--dump-type="))
				{
					cmdDumpType = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--dump-name="))
				{
					cmdDumpName = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--dump-length="))
				{
					int.TryParse(arg.Substring(arg.IndexOf('=') + 1), out _autoDumpLength);
				}
				else if (arg.StartsWith("--dump-close"))
				{
					_autoCloseOnDump = true;
				}
				else if (arg.StartsWith("--chromeless"))
				{
					_chromeless = true;
				}
				else if (arg.StartsWith("--fullscreen"))
				{
					startFullscreen = true;
				}
				else
				{
					cmdRom = arg;
				}
			}

			Database.LoadDatabase(Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "gamedb.txt"));

			//TODO GL - a lot of disorganized wiring-up here
			GlobalWin.PresentationPanel = new PresentationPanel();
			GlobalWin.DisplayManager = new DisplayManager(GlobalWin.PresentationPanel);
			Controls.Add(GlobalWin.PresentationPanel);
			Controls.SetChildIndex(GlobalWin.PresentationPanel, 0);

			//TODO GL - move these event handlers somewhere less obnoxious line in the On* overrides
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
					Global.CheatList.SaveOnClose();
					CloseGame();
					Global.MovieSession.Movie.Stop();
					GlobalWin.Tools.Close();
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

				if (GlobalWin.PresentationPanel != null)
				{
					GlobalWin.PresentationPanel.Resized = true;
				}

				if (GlobalWin.Sound != null)
				{
					GlobalWin.Sound.StartSound();
				}
			};

			Input.Initialize();
			InitControls();
			Global.CoreComm = CreateCoreComm();
			CoreFileProvider.SyncCoreCommInputSignals();
			Global.Emulator = new NullEmulator(Global.CoreComm, Global.Config.GetCoreSettings<NullEmulator>());
			Global.ActiveController = Global.NullControls;
			Global.AutoFireController = Global.AutofireNullControls;
			Global.AutofireStickyXORAdapter.SetOnOffPatternFromConfig();
#if WINDOWS
			GlobalWin.Sound = new Sound(Handle, GlobalWin.DSound);
#else
			Global.Sound = new Sound();
#endif
			GlobalWin.Sound.StartSound();
			InputManager.RewireInputChain();
			GlobalWin.Tools = new ToolManager();
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

			if (cmdRom != null)
			{
				// Commandline should always override auto-load
				LoadRom(cmdRom);
				if (Global.Game == null)
				{
					MessageBox.Show("Failed to load " + cmdRom + " specified on commandline");
				}
			}
			else if (Global.Config.RecentRoms.AutoLoad && !Global.Config.RecentRoms.Empty)
			{
				LoadRomFromRecent(Global.Config.RecentRoms.MostRecent);
			}

			if (cmdMovie != null)
			{
				if (Global.Game == null)
				{
					OpenRom();
				}
				else
				{
					var movie = MovieService.Get(cmdMovie);
					Global.MovieSession.ReadOnly = true;

					// if user is dumping and didnt supply dump length, make it as long as the loaded movie
					if (_autoDumpLength == 0)
					{
						_autoDumpLength = movie.InputLogLength;
					}

					StartNewMovie(movie, false);
					Global.Config.RecentMovies.Add(cmdMovie);
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

			if (startFullscreen || Global.Config.StartFullscreen)
			{
				ToggleFullscreen();
			}

			if (cmdLoadState != null && !Global.Game.IsNullInstance)
			{
				LoadQuickSave("QuickSave" + cmdLoadState);
			}
			else if (Global.Config.AutoLoadLastSaveSlot && !Global.Game.IsNullInstance)
			{
				LoadQuickSave("QuickSave" + Global.Config.SaveSlot);
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

			// start dumping, if appropriate
			if (cmdDumpType != null && cmdDumpName != null)
			{
				RecordAv(cmdDumpType, cmdDumpName);
			}

			SetMainformMovieInfo();

			SynchChrome();

			//TODO POOP
			GlobalWin.PresentationPanel.Control.Paint += (o, e) =>
			{
				GlobalWin.DisplayManager.NeedsToPaint = true;
			};
		}

		public void ProgramRunLoop()
		{
			CheckMessages();
			LogConsole.PositionConsole();

			for (; ; )
			{
				Input.Instance.Update();

				// handle events and dispatch as a hotkey action, or a hotkey button, or an input button
				ProcessInput();
				Global.ClientControls.LatchFromPhysical(GlobalWin.HotkeyCoalescer);

				Global.ActiveController.LatchFromPhysical(Global.ControllerInputCoalescer);

				Global.ActiveController.ApplyAxisConstraints(
					(Global.Emulator is N64 && Global.Config.N64UseCircularAnalogConstraint) ? "Natural Circle" : null);

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

				if (GlobalWin.Tools.Has<LuaConsole>())
				{
					GlobalWin.Tools.LuaConsole.ResumeScripts(false);
				}

				if (Global.Config.DisplayInput) // Input display wants to update even while paused
				{
					GlobalWin.DisplayManager.NeedsToPaint = true;
				}

				StepRunLoop_Core();
				StepRunLoop_Throttle();

				if (GlobalWin.DisplayManager.NeedsToPaint)
				{
					Render();
				}

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

				Thread.Sleep(0);
			}

			Shutdown();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			//NOTE: this gets called twice sometimes. once by using() in Program.cs and once from winforms internals when the form is closed...
		
			if (GlobalWin.DisplayManager != null)
			{
				GlobalWin.DisplayManager.Dispose();
				GlobalWin.DisplayManager = null;
			}

			if (disposing && (components != null))
			{
				components.Dispose();
			}

			base.Dispose(disposing);
		}

		#endregion`

		#region Properties

		public string CurrentlyOpenRom;
		public bool PauseAVI = false;
		public bool PressFrameAdvance = false;
		public bool PressRewind = false;
		public bool FastForward = false;
		public bool TurboFastForward = false;
		public bool RestoreReadWriteOnStop = false;
		public bool UpdateFrame = false;
		public bool EmulatorPaused { get; private set; }

		private int? _pauseOnFrame;
		public int? PauseOnFrame // If set, upon completion of this frame, the client wil pause
		{
			get { return _pauseOnFrame; }
			set
			{
				_pauseOnFrame = value;
				SetPauseStatusbarIcon();

				if (value == null) // TODO: make an Event handler instead, but the logic here is that after turbo seeking, tools will want to do a real update when the emulator finally pauses
				{
					GlobalWin.Tools.UpdateToolsBefore();
					GlobalWin.Tools.UpdateToolsAfter();
				}
			}
		}

		public bool IsSeeking
		{
			get { return PauseOnFrame.HasValue; }
		}

		public bool IsTurboSeeking
		{
			get
			{
				return PauseOnFrame.HasValue && Global.Config.TurboSeek;
			}
		}

		public bool IsTurboing
		{
			get
			{
				return Global.ClientControls["Turbo"] || IsTurboSeeking;
			}
		}

		// TODO: SystemInfo should be able to do this
		// Because we don't have enough places where we list SystemID's
		public Dictionary<string, string> SupportedPlatforms
		{
			get
			{
				var released = new Dictionary<string, string>
				{
					{ "A26", "Atari 2600" },
					{ "A78", "Atari 7800" },
					{ "Lynx", "Atari Lynx" },

					{ "NES", "Nintendo Entertainment System/Famicom" },
					{ "SNES", "Super Nintendo" },
					{ "N64", "Nintendo 64" },

					{ "GB", "Game Boy" },
					{ "GBC", "Game Boy Color" },
					{ "GBA", "Gameboy Advance" },

					{ "PSX", "Playstation" },

					{ "SMS", "Sega Master System" },
					{ "GG", "Sega Game Gear" },
					{ "SG", "SG-1000" },
					{ "GEN", "Sega Genesis/Megadrive" },
					{ "SAT", "Sega Saturn" },

					{ "PCE", "PC Engine/TurboGrafx 16" },

					{ "Coleco", "Colecovision" },
					{ "TI83", "TI-83 Calculator" },

					{ "WSWAN", "WonderSwan" },
				};

				if (VersionInfo.DeveloperBuild)
				{
					released.Add("C64", "Commodore 64");
				}

				return released;
			}
		}

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
		public bool AllowInput
		{
			get
			{
				// the main form gets input
				if (ActiveForm == this)
				{
					return true;
				}

				// modals that need to capture input for binding purposes get input, of course
				if (ActiveForm is HotkeyConfig ||
					ActiveForm is ControllerConfig ||
					ActiveForm is TAStudio ||
					ActiveForm is VirtualpadTool)
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
		}



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

		public void ProcessInput()
		{
			ControllerInputCoalescer conInput = Global.ControllerInputCoalescer as ControllerInputCoalescer;

			for (; ; )
			{

				// loop through all available events
				var ie = Input.Instance.DequeueEvent();
				if (ie == null) { break; }

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

				// adelikat 02-dec-2012 - implemented options for how to handle controller vs hotkey conflicts.  This is primarily motivated by computer emulation and thus controller being nearly the entire keyboard
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
							GlobalWin.HotkeyCoalescer.Receive(ie);
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
								GlobalWin.HotkeyCoalescer.Receive(ie);
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
							GlobalWin.HotkeyCoalescer.Receive(ie);
							conInput.Receive(ie);
						}

						break;
				}

			} // foreach event

			// also handle floats
			conInput.AcceptNewFloats(Input.Instance.GetFloats().Select(o =>
			{
				var video = Global.Emulator.VideoProvider();
				// hackish
				if (o.Item1 == "WMouse X")
				{
					var P = GlobalWin.DisplayManager.UntransformPoint(new Point((int)o.Item2, 0));
					float x = P.X / (float)video.BufferWidth;
					return new Tuple<string, float>("WMouse X", x * 20000 - 10000);
				}
					
				if (o.Item1 == "WMouse Y")
				{
					var P = GlobalWin.DisplayManager.UntransformPoint(new Point(0, (int)o.Item2));
					float y = P.Y / (float)video.BufferHeight;
					return new Tuple<string, float>("WMouse Y", y * 20000 - 10000);
				}
					
				return o;
			}));
		}

		public void RebootCore()
		{
			LoadRom(CurrentlyOpenRom);
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

		public void TakeScreenshotToClipboard()
		{
			using (var bb = Global.Config.Screenshot_CaptureOSD ? CaptureOSD() : MakeScreenshotImage())
			{
				using(var img = bb.ToSysdrawingBitmap())
					Clipboard.SetImage(img);
			}

			GlobalWin.OSD.AddMessage("Screenshot (raw) saved to clipboard.");
		}

		public void TakeScreenshot()
		{
			TakeScreenshot(
				String.Format(PathManager.ScreenshotPrefix(Global.Game) + ".{0:yyyy-MM-dd HH.mm.ss}.png", DateTime.Now)
			);
		}

		public void TakeScreenshot(string path)
		{
			var fi = new FileInfo(path);
			if (fi.Directory != null && fi.Directory.Exists == false)
			{
				fi.Directory.Create();
			}

			using (var bb = Global.Config.Screenshot_CaptureOSD ? CaptureOSD() : MakeScreenshotImage())
			{
				using(var img = bb.ToSysdrawingBitmap())
					img.Save(fi.FullName, ImageFormat.Png);
			}

			GlobalWin.OSD.AddMessage(fi.Name + " saved.");
		}

		public void FrameBufferResized()
		{
			// run this entire thing exactly twice, since the first resize may adjust the menu stacking
			for (int i = 0; i < 2; i++)
			{
				var video = Global.Emulator.VideoProvider();
				int zoom = Global.Config.TargetZoomFactor;
				var area = Screen.FromControl(this).WorkingArea;

				int borderWidth = Size.Width - GlobalWin.PresentationPanel.Control.Size.Width;
				int borderHeight = Size.Height - GlobalWin.PresentationPanel.Control.Size.Height;

				// start at target zoom and work way down until we find acceptable zoom
				Size lastComputedSize = new Size(1, 1);
				for (; zoom >= 1; zoom--)
				{
					lastComputedSize = GlobalWin.DisplayManager.CalculateClientSize(video, zoom);
					if ((((lastComputedSize.Width) + borderWidth) < area.Width)
						&& (((lastComputedSize.Height) + borderHeight) < area.Height))
					{
						break;
					}
				}
				Console.WriteLine("Selecting display size " + lastComputedSize.ToString());

				// Change size
				Size = new Size((lastComputedSize.Width) + borderWidth, ((lastComputedSize.Height) + borderHeight));
				PerformLayout();
				GlobalWin.PresentationPanel.Resized = true;

				// Is window off the screen at this size?
				if (area.Contains(Bounds) == false)
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

		public bool IsInFullscreen
		{
			get { return _inFullscreen; }
		}

		public void SynchChrome()
		{
			//PANTS

			if (_inFullscreen)
			{
				//TODO - maybe apply a hack tracked during fullscreen here to override it
				FormBorderStyle = FormBorderStyle.None;
				MainMenuStrip.Visible = Global.Config.DispChrome_MenuFullscreen && !_chromeless;
				MainStatusBar.Visible = Global.Config.DispChrome_StatusBarFullscreen && !_chromeless;
			}
			else
			{
				MainStatusBar.Visible = Global.Config.DispChrome_StatusBarWindowed && !_chromeless;
				MainMenuStrip.Visible = Global.Config.DispChrome_MenuWindowed && !_chromeless;
				MaximizeBox = MinimizeBox = Global.Config.DispChrome_CaptionWindowed && !_chromeless;
				if (Global.Config.DispChrome_FrameWindowed == 0 || _chromeless)
					FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
				else if (Global.Config.DispChrome_FrameWindowed == 1)
					FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
				else if (Global.Config.DispChrome_FrameWindowed == 2)
					FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
			}

		}

		public void ToggleFullscreen(bool allowSuppress=false)
		{
			//prohibit this operation if the current controls include LMouse
			if (allowSuppress)
			{
				if (Global.ActiveController.HasBinding("WMouse L"))
					return;
			}

			if (_inFullscreen == false)
			{
				SuspendLayout();
				#if WINDOWS
					//Work around an AMD driver bug in >= vista:
					//It seems windows will activate opengl fullscreen mode when a GL control is occupying the exact space of a screen (0,0 and dimensions=screensize)
					//AMD cards manifest a problem under these circumstances, flickering other monitors. 
					//It isnt clear whether nvidia cards are failing to employ this optimization, or just not flickering.
					//(this could be determined with more work; other side affects of the fullscreen mode include: corrupted taskbar, no modal boxes on top of GL control, no screenshots)
					//At any rate, we can solve this by adding a 1px black border around the GL control
					//Please note: It is important to do this before resizing things, otherwise momentarily a GL control without WS_BORDER will be at the magic dimensions and cause the flakeout
					if (Global.Config.DispFullscreenHacks)
					{
						//ATTENTION: this causes the statusbar to not work well, since the backcolor is now set to black instead of SystemColors.Control.
						//It seems that some statusbar elements composite with the backcolor. 
						//Maybe we could add another control under the statusbar. with a different backcolor
						Padding = new Padding(1);
						BackColor = Color.Black;
					}
				#endif

				_windowedLocation = Location;

				_inFullscreen = true;
				SynchChrome();
				WindowState = FormWindowState.Maximized; //be sure to do this after setting the chrome, otherwise it wont work fully
				ResumeLayout();

				GlobalWin.PresentationPanel.Resized = true;
			}
			else
			{
				SuspendLayout();

				WindowState = FormWindowState.Normal;

				#if WINDOWS
					//do this even if DispFullscreenHacks arent enabled, to restore it in case it changed underneath us or something
					Padding = new Padding(0);
					//it's important that we set the form color back to this, because the statusbar icons blend onto the mainform, not onto the statusbar--
					//so we need the statusbar and mainform backdrop color to match
					BackColor = SystemColors.Control; 
				#endif

				_inFullscreen = false;
					
				SynchChrome();
				Location = _windowedLocation;
				ResumeLayout();

				FrameBufferResized();
			}
		}

		public void OpenLuaConsole()
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
				CheatStatusButton.ToolTipText = string.Empty;
				CheatStatusButton.Image = Properties.Resources.Blank;
				CheatStatusButton.Visible = false;
			}
		}

		private static LibsnesCore AsSNES { get { return Global.Emulator as LibsnesCore; } }

		// TODO: Clean Me!

		public void SNES_ToggleBG1(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore)) return;
			var s = AsSNES.GetSettings();
			if (setto.HasValue)
			{
				s.ShowBG1_1 = s.ShowBG1_0 = setto.Value;
			}
			else
			{
				s.ShowBG1_1 = s.ShowBG1_0 ^= true;
			}

			AsSNES.PutSettings(s);
			GlobalWin.OSD.AddMessage(s.ShowBG1_1 ? "BG 1 Layer On" : "BG 1 Layer Off");
		}

		public void SNES_ToggleBG2(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore)) return;
			var s = AsSNES.GetSettings();
			if (setto.HasValue)
			{
				s.ShowBG2_1 = s.ShowBG2_0 = setto.Value;
			}
			else
			{
				s.ShowBG2_1 = s.ShowBG2_0 ^= true;
			}

			AsSNES.PutSettings(s);
			GlobalWin.OSD.AddMessage(s.ShowBG2_1 ? "BG 2 Layer On" : "BG 2 Layer Off");
		}

		public void SNES_ToggleBG3(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore)) return;
			var s = AsSNES.GetSettings();
			if (setto.HasValue)
			{
				s.ShowBG3_1 = s.ShowBG3_0 = setto.Value;
			}
			else
			{
				s.ShowBG3_1 = s.ShowBG3_0 ^= true;
			}

			AsSNES.PutSettings(s);
			GlobalWin.OSD.AddMessage(s.ShowBG3_1 ? "BG 3 Layer On" : "BG 3 Layer Off");
		}

		public void SNES_ToggleBG4(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore)) return;
			var s = AsSNES.GetSettings();
			if (setto.HasValue)
			{
				s.ShowBG4_1 = s.ShowBG4_0 = setto.Value;
			}
			else
			{
				s.ShowBG4_1 = s.ShowBG4_0 ^= true;
			}

			AsSNES.PutSettings(s);
			GlobalWin.OSD.AddMessage(s.ShowBG4_1 ? "BG 4 Layer On" : "BG 4 Layer Off");
		}

		public void SNES_ToggleObj1(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore)) return;
			var s = AsSNES.GetSettings();
			if (setto.HasValue)
			{
				s.ShowOBJ_0 = setto.Value;
			}
			else
			{
				s.ShowOBJ_0 ^= true;
			}

			AsSNES.PutSettings(s);
			GlobalWin.OSD.AddMessage(s.ShowOBJ_0 ? "OBJ 1 Layer On" : "OBJ 1 Layer Off");
		}

		public void SNES_ToggleObj2(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore)) return;
			var s = AsSNES.GetSettings();
			if (setto.HasValue)
			{
				s.ShowOBJ_1 = setto.Value;
			}
			else
			{
				s.ShowOBJ_1 ^= true;
			}
			AsSNES.PutSettings(s);
			GlobalWin.OSD.AddMessage(s.ShowOBJ_1 ? "OBJ 2 Layer On" : "OBJ 2 Layer Off");
		}

		public void SNES_ToggleOBJ3(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore)) return;
			var s = AsSNES.GetSettings();
			if (setto.HasValue)
			{
				s.ShowOBJ_2 = setto.Value;
			}
			else
			{
				s.ShowOBJ_2 ^= true;
			}

			AsSNES.PutSettings(s);
			GlobalWin.OSD.AddMessage(s.ShowOBJ_2 ? "OBJ 3 Layer On" : "OBJ 3 Layer Off");
		}

		public void SNES_ToggleOBJ4(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore)) return;
			var s = AsSNES.GetSettings();
			if (setto.HasValue)
			{
				s.ShowOBJ_3 = setto.Value;
			}
			else
			{
				s.ShowOBJ_3 ^= true;
			}

			AsSNES.PutSettings(s);
			GlobalWin.OSD.AddMessage(s.ShowOBJ_3 ? "OBJ 4 Layer On" : "OBJ 4 Layer Off");
		}

		#endregion

		#region Private variables

		private Size _lastVideoSize = new Size(-1, -1), _lastVirtualSize = new Size(-1, -1);
		private readonly SaveSlotManager _stateSlots = new SaveSlotManager();

		// AVI/WAV state
		private IVideoWriter _currAviWriter;
		private ISoundProvider _aviSoundInput;
		private MetaspuSoundProvider _dumpProxy; // an audio proxy used for dumping
		private bool _dumpaudiosync; // set true to for experimental AV dumping
		private int _avwriterResizew;
		private int _avwriterResizeh;
		private bool _avwriterpad;

		private bool _exit;
		private bool _exitRequestPending;
		private bool _runloopFrameProgress;
		private DateTime _frameAdvanceTimestamp = DateTime.MinValue;
		private int _runloopFps;
		private int _runloopLastFps;
		private bool _runloopFrameadvance;
		private DateTime _runloopSecond;
		private bool _runloopLastFf;
		private bool _inResizeLoop;

		private readonly Throttle _throttle;
		private bool _unthrottled;

		// For handling automatic pausing when entering the menu
		private bool _wasPaused;
		private bool _didMenuPause;

		private bool _inFullscreen;
		private Point _windowedLocation;

		private int _autoDumpLength;
		private readonly bool _autoCloseOnDump;
		private int _lastOpenRomFilter;

		//chrome is never shown, even in windowed mode
		private readonly bool _chromeless;

		// Resources
		Bitmap StatusBarDiskLightOnImage, StatusBarDiskLightOffImage;
		Bitmap LinkCableOn, LinkCableOff;

		#endregion

		#region Private methods

		private static string DisplayNameForSystem(string system)
		{
			var str = Global.SystemInfo.DisplayName;

			if (VersionInfo.DeveloperBuild)
			{
				str += " (interim)";
			}

			return str;
		}

		private void SetWindowText()
		{
			string str = string.Empty;

			if (_inResizeLoop)
			{
				var size = GlobalWin.PresentationPanel.NativeSize;
				str = str + string.Format("({0}x{1}) - ", size.Width, size.Height);
			}

			if (Global.Emulator.IsNull())
			{
				str = str + "BizHawk" + (VersionInfo.DeveloperBuild ? " (interim) " : string.Empty);
			}
			else
			{
				str = str + Global.SystemInfo.DisplayName;

				if (VersionInfo.DeveloperBuild)
				{
					str += " (interim)";
				}

				if (Global.MovieSession.Movie.IsActive)
				{
					str = str + " - " + Global.Game.Name + " - " + Path.GetFileName(Global.MovieSession.Movie.Filename);
				}
				else
				{
					str = str + " - " + Global.Game.Name;
				}
			}

			if (!Global.Config.DispChrome_CaptionWindowed || _chromeless)
				str = "";

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
			DumpStatusButton.ToolTipText = string.Empty;

			if (Global.Emulator == null)
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

			if (!string.IsNullOrEmpty(Global.Emulator.CoreComm.RomStatusAnnotation))
			{
				annotation = Global.Emulator.CoreComm.RomStatusAnnotation;
			}

			DumpStatusButton.ToolTipText = annotation;
		}

		private static void LoadSaveRam()
		{
			if (Global.Emulator.HasSaveRam())
			{
				try // zero says: this is sort of sketchy... but this is no time for rearchitecting
				{
					byte[] sram;

					// GBA meteor core might not know how big the saveram ought to be, so just send it the whole file
					// GBA vba-next core will try to eat anything, regardless of size
					if (Global.Emulator is GBA || Global.Emulator is VBANext)
					{
						sram = File.ReadAllBytes(PathManager.SaveRamPath(Global.Game));
					}
					else
					{
						var oldram = Global.Emulator.AsSaveRam().CloneSaveRam();
						if (oldram == null)
						{
							// we're eating this one now.  the possible negative consequence is that a user could lose
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

					Global.Emulator.AsSaveRam().StoreSaveRam(sram);
				}
				catch (IOException)
				{
					GlobalWin.OSD.AddMessage("An error occurred while loading Sram");
				}
			}
		}

		private static void SaveRam()
		{
			if (Global.Emulator.HasSaveRam())
			{
				var path = PathManager.SaveRamPath(Global.Game);
				var f = new FileInfo(path);
				if (f.Directory != null && f.Directory.Exists == false)
				{
					f.Directory.Create();
				}

				// Make backup first
				if (Global.Config.BackupSaveram && f.Exists)
				{
					var backup = path + ".bak";
					var backupFile = new FileInfo(backup);
					if (backupFile.Exists)
					{
						backupFile.Delete();
					}

					f.CopyTo(backup);
				}

				var writer = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write));
				var saveram = Global.Emulator.AsSaveRam().CloneSaveRam();

				writer.Write(saveram, 0, saveram.Length);
				writer.Close();
			}
		}

		private void SelectSlot(int num)
		{
			if (Global.Emulator.HasSavestates())
			{
				Global.Config.SaveSlot = num;
				SaveSlotSelectedMessage();
				UpdateStatusSlots();
			}
		}

		private void RewireSound()
		{
			if (_dumpProxy != null)
			{
				// we're video dumping, so async mode only and use the DumpProxy.
				// note that the avi dumper has already rewired the emulator itself in this case.
				GlobalWin.Sound.SetAsyncInputPin(_dumpProxy);
			}
			else if (Global.Config.SoundThrottle)
			{
				// for sound throttle, use sync mode
				Global.Emulator.EndAsyncSound();
				GlobalWin.Sound.SetSyncInputPin(Global.Emulator.SyncSoundProvider);
			}
			else
			{
				// for vsync\clock throttle modes, use async
				GlobalWin.Sound.SetAsyncInputPin(
					!Global.Emulator.StartAsyncSound()
						? new MetaspuAsync(Global.Emulator.SyncSoundProvider, ESynchMethod.ESynchMethod_V)
						: Global.Emulator.SoundProvider);
			}
		}

		private void HandlePlatformMenus()
		{
			var system = string.Empty;
			if (!Global.Game.IsNullInstance)
			{
				//New Code
				//We use SystemID as that has the system we are playing on.
				system = Global.Emulator.SystemId;
				//Old Code below.
				//system = Global.Game.System;

			}

			TI83SubMenu.Visible = false;
			NESSubMenu.Visible = false;
			PCESubMenu.Visible = false;
			SMSSubMenu.Visible = false;
			GBSubMenu.Visible = false;
			GBASubMenu.Visible = false;
			AtariSubMenu.Visible = false;
			SNESSubMenu.Visible = false;
			PSXSubMenu.Visible = false;
			ColecoSubMenu.Visible = false;
			N64SubMenu.Visible = false;
			SaturnSubMenu.Visible = false;
			DGBSubMenu.Visible = false;
			GenesisSubMenu.Visible = false;
			wonderSwanToolStripMenuItem.Visible = false;

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
				case "PSX":
					PSXSubMenu.Visible = true;
					break;
				case "SNES":
				case "SGB":
					// TODO: fix SNES9x here
					if (Global.Emulator is LibsnesCore)
					{
						if ((Global.Emulator as LibsnesCore).IsSGB)
						{
							SNESSubMenu.Text = "&SGB";
						}
						else
						{
							SNESSubMenu.Text = "&SNES";
						}
						SNESSubMenu.Visible = true;
					}
					else
					{
						SNESSubMenu.Visible = false;
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
			}
		}

		private static void InitControls()
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
			Global.NullControls = new Controller(NullEmulator.NullController);
			Global.AutofireNullControls = new AutofireController(NullEmulator.NullController);

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
			if (!LoadRom(rom))
			{
				Global.Config.RecentRoms.HandleLoadError(rom);
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
				PauseStatusButton.ToolTipText = string.Empty;
			}
		}


		private void SyncThrottle()
		{
			//TODO - did we change 'unthrottled' nomenclature to turbo? is turbo defined as 'temporarily disable throttle entirely'?

			var rewind = Global.Rewinder.RewindActive && (Global.ClientControls["Rewind"] || PressRewind);
			var fastForward = Global.ClientControls["Fast Forward"] || FastForward;
			var superFastForward = IsTurboing;

			int speedPercent = fastForward ? Global.Config.SpeedPercentAlternate : Global.Config.SpeedPercent;

			if (rewind)
			{
				speedPercent = Math.Max(speedPercent / Global.Rewinder.RewindFrequency, 5);
			}

			Global.DisableSecondaryThrottling = _unthrottled || fastForward || superFastForward || rewind;

			// realtime throttle is never going to be so exact that using a double here is wrong
			_throttle.SetCoreFps(Global.Emulator.CoreComm.VsyncRate);
			_throttle.signal_paused = EmulatorPaused;
			_throttle.signal_unthrottle = _unthrottled || superFastForward;
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

		private static unsafe BitmapBuffer MakeScreenshotImage()
		{
			var bb = new BitmapBuffer(Global.Emulator.VideoProvider().BufferWidth, Global.Emulator.VideoProvider().BufferHeight, Global.Emulator.VideoProvider().GetVideoBuffer());
			bb.DiscardAlpha();
			return bb;
		}

		private void SaveStateAs()
		{
			if (!Global.Emulator.HasSavestates())
			{
				return;
			}

			var path = PathManager.GetSaveStatePath(Global.Game);

			var file = new FileInfo(path);
			if (file.Directory != null && file.Directory.Exists == false)
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
		}

		private void LoadStateAs()
		{
			if (!Global.Emulator.HasSavestates())
			{
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

			if (File.Exists(ofd.FileName) == false)
			{
				return;
			}

			LoadState(ofd.FileName, Path.GetFileName(ofd.FileName));
		}

		private static void SaveSlotSelectedMessage()
		{
			GlobalWin.OSD.AddMessage("Slot " + Global.Config.SaveSlot + " selected.");
		}

		private void Render()
		{
			//private Size _lastVideoSize = new Size(-1, -1), _lastVirtualSize = new Size(-1, -1);
			var video = Global.Emulator.VideoProvider();
			//bool change = false;
			Size currVideoSize = new Size(video.BufferWidth,video.BufferHeight);
			Size currVirtualSize = new Size(video.VirtualWidth,video.VirtualWidth);
			if (currVideoSize != _lastVideoSize || currVirtualSize != _lastVirtualSize)
			{
				_lastVideoSize = currVideoSize;
				_lastVirtualSize = currVirtualSize;
				FrameBufferResized();
			}

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

		private static string FormatFilter(params string[] args)
		{
			var sb = new StringBuilder();
			if (args.Length % 2 != 0)
			{
				throw new ArgumentException();
			}

			var num = args.Length / 2;
			for (int i = 0; i < num; i++)
			{
				sb.AppendFormat("{0} ({1})|{1}", args[i * 2], args[i * 2 + 1]);
				if (i != num - 1)
				{
					sb.Append('|');
				}
			}

			var str = sb.ToString().Replace("%ARCH%", "*.zip;*.rar;*.7z;*.gz");
			str = str.Replace(";", "; ");
			return str;
		}

		private void OpenRom()
		{
			var ofd = new OpenFileDialog { InitialDirectory = PathManager.GetRomsPath(Global.Emulator.SystemId) };

			// adelikat: ugly design for this, I know
			if (VersionInfo.DeveloperBuild)
			{
				ofd.Filter = FormatFilter(
					"Rom Files", "*.nes;*.fds;*.sms;*.gg;*.sg;*.pce;*.sgx;*.bin;*.smd;*.rom;*.a26;*.a78;*.lnx;*.m3u;*.cue;*.ccd;*.exe;*.gb;*.gbc;*.gba;*.gen;*.md;*.col;.int;*.smc;*.sfc;*.prg;*.d64;*.g64;*.crt;*.sgb;*.xml;*.z64;*.v64;*.n64;*.ws;*.wsc;%ARCH%",
					"Music Files", "*.psf;*.sid",
					"Disc Images", "*.cue;*.ccd;*.m3u",
					"NES", "*.nes;*.fds;%ARCH%",
					"Super NES", "*.smc;*.sfc;*.xml;%ARCH%",
					"Master System", "*.sms;*.gg;*.sg;%ARCH%",
					"PC Engine", "*.pce;*.sgx;*.cue;*.ccd;%ARCH%",
					"TI-83", "*.rom;%ARCH%",
					"Archive Files", "%ARCH%",
					"Savestate", "*.state",
					"Atari 2600", "*.a26;*.bin;%ARCH%",
					"Atari 7800", "*.a78;*.bin;%ARCH%",
					"Atari Lynx", "*.lnx;%ARCH%",
					"Genesis", "*.gen;*.smd;*.bin;*.md;*.cue;*.ccd;%ARCH%",
					"Gameboy", "*.gb;*.gbc;*.sgb;%ARCH%",
					"Gameboy Advance", "*.gba;%ARCH%",
					"Colecovision", "*.col;%ARCH%",
					"Intellivision (very experimental)", "*.int;*.bin;*.rom;%ARCH%",
					"PSX Executables (experimental)", "*.exe",
					"PSF Playstation Sound File (not supported)", "*.psf",
					"Commodore 64 (experimental)", "*.prg; *.d64, *.g64; *.crt;%ARCH%",
					"SID Commodore 64 Music File", "*.sid;%ARCH%",
					"Nintendo 64", "*.z64;*.v64;*.n64",
					"WonderSwan", "*.ws;*.wsc;%ARCH%",
					"All Files", "*.*");
			}
			else
			{
				ofd.Filter = FormatFilter(
					"Rom Files", "*.nes;*.fds;*.sms;*.gg;*.sg;*.gb;*.gbc;*.gba;*.pce;*.sgx;*.bin;*.smd;*.gen;*.md;*.smc;*.sfc;*.a26;*.a78;*.lnx;*.col;*.rom;*.cue;*.ccd;*.sgb;*.z64;*.v64;*.n64;*.ws;*.wsc;*.xml;%ARCH%",
					"Disc Images", "*.cue;*.ccd;*.m3u",
					"NES", "*.nes;*.fds;%ARCH%",
					"Super NES", "*.smc;*.sfc;*.xml;%ARCH%",
					"Nintendo 64", "*.z64;*.v64;*.n64",
					"Gameboy", "*.gb;*.gbc;*.sgb;%ARCH%",
					"Gameboy Advance", "*.gba;%ARCH%",
					"Master System", "*.sms;*.gg;*.sg;%ARCH%",
					"PC Engine", "*.pce;*.sgx;*.cue;*.ccd;%ARCH%",
					"Atari 2600", "*.a26;%ARCH%",
					"Atari 7800", "*.a78;%ARCH%",
					"Atari Lynx", "*.lnx;%ARCH%",
					"Colecovision", "*.col;%ARCH%",
					"TI-83", "*.rom;%ARCH%",
					"Archive Files", "%ARCH%",
					"Savestate", "*.state",
					"Genesis", "*.gen;*.md;*.smd;*.bin;*.cue;*.ccd;%ARCH%",
					"WonderSwan", "*.ws;*.wsc;%ARCH%",
					"All Files", "*.*");
			}

			ofd.RestoreDirectory = false;
			ofd.FilterIndex = _lastOpenRomFilter;

			var result = ofd.ShowHawkDialog();
			if (result != DialogResult.OK)
			{
				return;
			}

			var file = new FileInfo(ofd.FileName);
			Global.Config.LastRomPath = file.DirectoryName;
			_lastOpenRomFilter = ofd.FilterIndex;
			LoadRom(file.FullName);
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
					MessageBox.Show(
						"No sync settings found, using currently configured settings for this core.",
						"No sync settings found",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning
						);

					e.Settings = Global.Config.GetCoreSyncSettings(e.Core);
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
			var settable = new SettingsAdapter(Global.Emulator);
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
			var settable = new SettingsAdapter(Global.Emulator);
			if (Global.MovieSession.Movie.IsActive)
			{
				GlobalWin.OSD.AddMessage("Attempt to change sync-relevant settings while recording BLOCKED.");
			}
            else if (settable.HasSyncSettings && settable.PutSyncSettings(o))
			{
				FlagNeedsReboot();
			}
		}

		private void SaveConfig()
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

			ConfigService.Save(PathManager.DefaultIniPath, Global.Config);
		}

		private void PreviousSlot()
		{
			if (Global.Emulator.HasSavestates())
			{
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
			if (Global.Emulator.HasSavestates())
			{
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

		private static void ToggleFPS()
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

		private static void VolumeUp()
		{
			Global.Config.SoundVolume += 10;
			if (Global.Config.SoundVolume > 100)
			{
				Global.Config.SoundVolume = 100;
			}

			GlobalWin.Sound.ChangeVolume(Global.Config.SoundVolume);
			GlobalWin.OSD.AddMessage("Volume " + Global.Config.SoundVolume);
		}

		public static void ToggleSound()
		{
			Global.Config.SoundEnabled ^= true;
			GlobalWin.Sound.UpdateSoundSettings();
			GlobalWin.Sound.StartSound();
		}

		private static void VolumeDown()
		{
			Global.Config.SoundVolume -= 10;
			if (Global.Config.SoundVolume < 0)
			{
				Global.Config.SoundVolume = 0;
			}

			GlobalWin.Sound.ChangeVolume(Global.Config.SoundVolume);
			GlobalWin.OSD.AddMessage("Volume " + Global.Config.SoundVolume);
		}

		private static void SoftReset()
		{
			// is it enough to run this for one frame? maybe..
			if (Global.Emulator.ControllerDefinition.BoolButtons.Contains("Reset"))
			{
				if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
				{
					Global.ClickyVirtualPadController.Click("Reset");
					GlobalWin.OSD.AddMessage("Reset button pressed.");
				}
			}
		}

		private static void HardReset()
		{
			// is it enough to run this for one frame? maybe..
			if (Global.Emulator.ControllerDefinition.BoolButtons.Contains("Power"))
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
				Global.Emulator.HasSavestates();
		}

		private BitmapBuffer CaptureOSD()
		{
			var bb = GlobalWin.DisplayManager.RenderOffscreen(Global.Emulator.VideoProvider(), true);
			bb.Normalize(true);
			return bb;
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

			GlobalWin.OSD.AddMessage("Screensize set to " + Global.Config.TargetZoomFactor + "x");
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

			GlobalWin.OSD.AddMessage("Screensize set to " + Global.Config.TargetZoomFactor + "x");
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
				var hasDriveLight = Global.Emulator.HasDriveLight() && Global.Emulator.AsDriveLight().DriveLightEnabled;

				if (hasDriveLight)
				{
					if (!LedLightStatusLabel.Visible)
					{
						LedLightStatusLabel.Visible = true;
					}

					LedLightStatusLabel.Image = Global.Emulator.AsDriveLight().DriveLightOn
						? StatusBarDiskLightOnImage
						: StatusBarDiskLightOffImage;
				}
				else
				{
					if (LedLightStatusLabel.Visible)
					{
						LedLightStatusLabel.Visible = false;
					}
				}

				if (Global.Emulator.CoreComm.UsesLinkCable)
				{
					if (!LinkConnectStatusBarButton.Visible)
					{
						LinkConnectStatusBarButton.Visible = true;
					}

					LinkConnectStatusBarButton.Image = Global.Emulator.CoreComm.LinkConnected
						? LinkCableOn
						: LinkCableOff;
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

		private static void LimitFrameRateMessage()
		{
			GlobalWin.OSD.AddMessage(Global.Config.ClockThrottle ? "Framerate limiting on" : "Framerate limiting off");
		}

		private static void VsyncMessage()
		{
			GlobalWin.OSD.AddMessage(
				"Display Vsync set to " + (Global.Config.VSyncThrottle ? "on" : "off")
			);
		}

		private static bool StateErrorAskUser(string title, string message)
		{
			var result = MessageBox.Show(
				message,
				title,
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question
			);

			return result == DialogResult.Yes;
		}

		private void FdsInsertDiskMenuAdd(string name, string button, string msg)
		{
			FDSControlsMenuItem.DropDownItems.Add(name, null, delegate
			{
				if (Global.Emulator.ControllerDefinition.BoolButtons.Contains(button))
				{
					if (!Global.MovieSession.Movie.IsPlaying || Global.MovieSession.Movie.IsFinished)
					{
						Global.ClickyVirtualPadController.Click(button);
						GlobalWin.OSD.AddMessage(msg);
					}
				}
			});
		}

		public void LoadState(string path, string userFriendlyStateName, bool fromLua = false) // Move to client.common
		{
			if (!Global.Emulator.HasSavestates())
			{
				return;
			}

			// If from lua, disable counting rerecords
			bool wasCountingRerecords = Global.MovieSession.Movie.IsCountingRerecords;

			if (fromLua)
				Global.MovieSession.Movie.IsCountingRerecords = false;

			GlobalWin.DisplayManager.NeedsToPaint = true;

			if (SavestateManager.LoadStateFile(path, userFriendlyStateName))
			{
				SetMainformMovieInfo();
				GlobalWin.OSD.ClearGUIText();
				GlobalWin.Tools.UpdateToolsBefore(fromLua);
				UpdateToolsAfter(fromLua);
				UpdateToolsLoadstate();
				Global.AutoFireController.ClearStarts();
				GlobalWin.OSD.AddMessage("Loaded state: " + userFriendlyStateName);

				if (GlobalWin.Tools.Has<LuaConsole>())
				{
					GlobalWin.Tools.LuaConsole.LuaImp.CallLoadStateEvent(userFriendlyStateName);
				}
			}
			else
			{
				GlobalWin.OSD.AddMessage("Loadstate error!");
			}

			Global.MovieSession.Movie.IsCountingRerecords = wasCountingRerecords;
		}

		public void LoadQuickSave(string quickSlotName, bool fromLua = false)
		{
			if (!Global.Emulator.HasSavestates())
			{
				return;
			}

			var path = PathManager.SaveStatePrefix(Global.Game) + "." + quickSlotName + ".State";
			if (File.Exists(path) == false)
			{
				GlobalWin.OSD.AddMessage("Unable to load " + quickSlotName + ".State");
				return;
			}

			LoadState(path, quickSlotName, fromLua);
		}

		public void SaveState(string path, string userFriendlyStateName, bool fromLua)
		{
			if (!Global.Emulator.HasSavestates())
			{
				return;
			}

			try
			{
				SavestateManager.SaveStateFile(path, userFriendlyStateName);

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

		// Alt key hacks
		protected override void WndProc(ref Message m)
		{
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
			if (Global.Emulator.IsNull())
			{
				CoreNameStatusBarButton.Visible = false;
				return;
			}

			CoreNameStatusBarButton.Visible = true;
			var attributes = Global.Emulator.Attributes();

			CoreNameStatusBarButton.Text = Global.Emulator.DisplayName();
			CoreNameStatusBarButton.Image = Global.Emulator.Icon();
			CoreNameStatusBarButton.ToolTipText = attributes.Ported ? "(ported) " : string.Empty;
		}

		#endregion

		#region Frame Loop

		private void StepRunLoop_Throttle()
		{
			SyncThrottle();
			_throttle.signal_frameAdvance = _runloopFrameadvance;
			_throttle.signal_continuousframeAdvancing = _runloopFrameProgress;

			_throttle.Step(true, -1);
		}

		public void FrameAdvance()
		{
			PressFrameAdvance = true;
			StepRunLoop_Core(true);
		}

		public bool IsLagFrame
		{
			get
			{
				if (Global.Emulator.CanPollInput())
				{
					return Global.Emulator.AsInputPollable().IsLagFrame;
				}

				return false;
			}
		}

		private void StepRunLoop_Core(bool force = false)
		{
			var runFrame = false;
			_runloopFrameadvance = false;
			var now = DateTime.Now;
			var suppressCaptureRewind = false;

			double frameAdvanceTimestampDelta = (now - _frameAdvanceTimestamp).TotalMilliseconds;
			bool frameProgressTimeElapsed = Global.Config.FrameProgressDelayMs < frameAdvanceTimestampDelta;

			if (Global.Config.SkipLagFrame && IsLagFrame && frameProgressTimeElapsed && Global.Emulator.Frame > 0)
			{
				runFrame = true;
			}

			if (Global.ClientControls["Frame Advance"] || PressFrameAdvance)
			{
				// handle the initial trigger of a frame advance
				if (_frameAdvanceTimestamp == DateTime.MinValue)
				{
					PauseEmulator();
					runFrame = true;
					_runloopFrameadvance = true;
					_frameAdvanceTimestamp = now;
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

				_frameAdvanceTimestamp = DateTime.MinValue;
			}

			if (!EmulatorPaused)
			{
				runFrame = true;
			}

			bool isRewinding = suppressCaptureRewind = Rewind(ref runFrame);

			if (UpdateFrame)
			{
				runFrame = true;
			}

			var genSound = false;
			var coreskipaudio = false;
			if (runFrame || force)
			{
				var isFastForwarding = Global.ClientControls["Fast Forward"] || IsTurboing;
				var updateFpsString = _runloopLastFf != isFastForwarding;
				_runloopLastFf = isFastForwarding;

				// client input-related duties
				GlobalWin.OSD.ClearGUIText();

				Global.CheatList.Pulse();

				//zero 03-may-2014 - moved this before call to UpdateToolsBefore(), since it seems to clear the state which a lua event.framestart is going to want to alter
				Global.ClickyVirtualPadController.FrameTick();
				Global.LuaAndAdaptor.FrameTick();

				if (GlobalWin.Tools.Has<LuaConsole>())
				{
					GlobalWin.Tools.LuaConsole.LuaImp.CallFrameBeforeEvent();
				}

				if (!IsTurboing)
				{
					GlobalWin.Tools.UpdateToolsBefore();
				}
				else
				{
					GlobalWin.Tools.FastUpdateBefore();
				}

				_runloopFps++;

				if ((DateTime.Now - _runloopSecond).TotalSeconds > 1)
				{
					_runloopLastFps = _runloopFps;
					_runloopSecond = DateTime.Now;
					_runloopFps = 0;
					updateFpsString = true;
				}

				if (updateFpsString)
				{
					var fps_string = _runloopLastFps + " fps";
					if (isRewinding)
					{
						if (IsTurboing || isFastForwarding)
						{
							fps_string += " <<<<";
						}
						else
						{
							fps_string += " <<";
						}
					}
					else if (IsTurboing)
					{
						fps_string += " >>>>";
					}
					else if (isFastForwarding)
					{
						fps_string += " >>";
					}

					GlobalWin.OSD.FPS = fps_string;
				}

				CaptureRewind(suppressCaptureRewind);

				if (!_runloopFrameadvance)
				{
					genSound = true;
				}
				else if (!Global.Config.MuteFrameAdvance)
				{
					genSound = true;
				}

				Global.MovieSession.HandleMovieOnFrameLoop();

				coreskipaudio = IsTurboing && _currAviWriter == null;

				{
					bool render = !_throttle.skipnextframe || _currAviWriter != null;
					bool renderSound = !coreskipaudio;
					Global.Emulator.FrameAdvance(render, renderSound);
				}

				GlobalWin.DisplayManager.NeedsToPaint = true;
				Global.CheatList.Pulse();

				if (!PauseAVI)
				{
					AvFrameAdvance();
				}

				if (IsLagFrame && Global.Config.AutofireLagFrames)
				{
					Global.AutoFireController.IncrementStarts();
				}

				PressFrameAdvance = false;

				if (GlobalWin.Tools.Has<LuaConsole>())
				{
					GlobalWin.Tools.LuaConsole.LuaImp.CallFrameAfterEvent();
				}

				if (!IsTurboing)
				{
					UpdateToolsAfter();
				}
				else
				{
					GlobalWin.Tools.FastUpdateAfter();
				}

				if (IsSeeking && Global.Emulator.Frame == PauseOnFrame.Value)
				{
					PauseEmulator();
					PauseOnFrame = null;
				}
			}

			if (Global.ClientControls["Rewind"] || PressRewind)
			{
				UpdateToolsAfter();
				PressRewind = false;
			}

			if (UpdateFrame)
			{
				UpdateFrame = false;
			}

			if (genSound && !coreskipaudio)
			{
				GlobalWin.Sound.UpdateSound();
			}
			else
			{
				GlobalWin.Sound.UpdateSilence();
			}
		}

		#endregion

		#region AVI Stuff

		/// <summary>
		/// start avi recording, unattended
		/// </summary>
		/// <param name="videowritername">match the short name of an ivideowriter</param>
		/// <param name="filename">filename to save to</param>
		private void RecordAv(string videowritername, string filename)
		{
			_RecordAv(videowritername, filename, true);
		}

		/// <summary>
		/// start avi recording, asking user for filename and options
		/// </summary>
		private void RecordAv()
		{
			_RecordAv(null, null, false);
		}

		/// <summary>
		/// start AV recording
		/// </summary>
		private void _RecordAv(string videowritername, string filename, bool unattended)
		{
			if (_currAviWriter != null)
			{
				return;
			}

			// select IVideoWriter to use
			IVideoWriter aw = null;

			if (unattended)
			{
				aw = VideoWriterInventory.GetVideoWriter(videowritername);

			}
			else
			{
				aw = VideoWriterChooserForm.DoVideoWriterChoserDlg(VideoWriterInventory.GetAllWriters(), this,
					out _avwriterResizew, out _avwriterResizeh, out _avwriterpad, out _dumpaudiosync);
			}

			if (aw == null)
			{
				GlobalWin.OSD.AddMessage(
					unattended ? string.Format("Couldn't start video writer \"{0}\"", videowritername) : "A/V capture canceled.");

				return;
			}

			try
			{
				if (_dumpaudiosync)
				{
					aw = new VideoStretcher(aw);
				}
				else
				{
					aw = new AudioStretcher(aw);
				}

				aw.SetMovieParameters(Global.Emulator.CoreComm.VsyncNum, Global.Emulator.CoreComm.VsyncDen);
				if (_avwriterResizew > 0 && _avwriterResizeh > 0)
				{
					aw.SetVideoParameters(_avwriterResizew, _avwriterResizeh);
				}
				else
				{
					aw.SetVideoParameters(Global.Emulator.VideoProvider().BufferWidth, Global.Emulator.VideoProvider().BufferHeight);
				}

				aw.SetAudioParameters(44100, 2, 16);

				// select codec token
				// do this before save dialog because ffmpeg won't know what extension it wants until it's been configured
				if (unattended)
				{
					aw.SetDefaultVideoCodecToken();
				}
				else
				{
					var token = aw.AcquireVideoCodecToken(GlobalWin.MainForm);
					if (token == null)
					{
						GlobalWin.OSD.AddMessage("A/V capture canceled.");
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
					string ext = aw.DesiredExtension();
					string pathForOpenFile;

					//handle directories first
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
							sfd.FileName = PathManager.FilesystemSafeName(Global.Game) + "." + ext; //dont use Path.ChangeExtension, it might wreck game names with dots in them
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
				Global.Emulator.EndAsyncSound();
			}
			else
			{
				_aviSoundInput = !Global.Emulator.StartAsyncSound()
					? new MetaspuAsync(Global.Emulator.SyncSoundProvider, ESynchMethod.ESynchMethod_V)
					: Global.Emulator.SoundProvider;
			}
			_dumpProxy = new MetaspuSoundProvider(ESynchMethod.ESynchMethod_V);
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
			AVIStatusLabel.ToolTipText = string.Empty;
			AVIStatusLabel.Visible = false;
			_aviSoundInput = null;
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
			AVIStatusLabel.ToolTipText = string.Empty;
			AVIStatusLabel.Visible = false;
			_aviSoundInput = null;
			_dumpProxy = null; // return to normal sound output
			RewireSound();
		}

		private void AvFrameAdvance()
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			if (_currAviWriter != null)
			{
				//TODO ZERO - this code is pretty jacked. we'll want to frugalize buffers better for speedier dumping, and we might want to rely on the GL layer for padding
				try
				{
					IVideoProvider output;
					IDisposable disposableOutput = null;
					if (_avwriterResizew > 0 && _avwriterResizeh > 0)
					{
						BitmapBuffer bbin = null;
						Bitmap bmpin = null;
						Bitmap bmpout = null;
						try
						{
							if (Global.Config.AVI_CaptureOSD)
							{
								bbin = CaptureOSD();
							}
							else
							{
								bbin = new BitmapBuffer(Global.Emulator.VideoProvider().BufferWidth, Global.Emulator.VideoProvider().BufferHeight, Global.Emulator.VideoProvider().GetVideoBuffer());
							}


							bmpout = new Bitmap(_avwriterResizew, _avwriterResizeh, PixelFormat.Format32bppArgb);
							bmpin = bbin.ToSysdrawingBitmap();
							using (var g = Graphics.FromImage(bmpout))
							{
								if (_avwriterpad)
								{
									g.Clear(Color.FromArgb(Global.Emulator.VideoProvider().BackgroundColor));
									g.DrawImageUnscaled(bmpin, (bmpout.Width - bmpin.Width) / 2, (bmpout.Height - bmpin.Height) / 2);
								}
								else
								{
									g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
									g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
									g.DrawImage(bmpin, new Rectangle(0, 0, bmpout.Width, bmpout.Height));
								}
							}

							output = new BmpVideoProvider(bmpout);
							disposableOutput = (IDisposable)output;
						}
						finally
						{
							if (bbin != null) bbin.Dispose();
							if (bmpin != null) bmpin.Dispose();
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
							output = Global.Emulator.VideoProvider();
					}

					_currAviWriter.SetFrame(Global.Emulator.Frame);

					short[] samp;
					int nsamp;
					if (_dumpaudiosync)
					{
						(_currAviWriter as VideoStretcher).DumpAV(output, Global.Emulator.SyncSoundProvider, out samp, out nsamp);
					}
					else
					{
						(_currAviWriter as AudioStretcher).DumpAV(output, _aviSoundInput, out samp, out nsamp);
					}

					if (disposableOutput != null)
					{
						disposableOutput.Dispose();
					}

					_dumpProxy.buffer.enqueue_samples(samp, nsamp);
				}
				catch (Exception e)
				{
					MessageBox.Show("Video dumping died:\n\n" + e);
					AbortAv();
				}

				if (_autoDumpLength > 0)
				{
					_autoDumpLength--;
					if (_autoDumpLength == 0) // finish
					{
						StopAv();
						if (_autoCloseOnDump)
						{
							_exit = true;
						}
					}
				}

				GlobalWin.DisplayManager.NeedsToPaint = true;
			}
		}

		private int? LoadArhiveChooser(HawkFile file)
		{
			var ac = new ArchiveChooser(file);
			if (ac.ShowDialog(this) == DialogResult.OK)
			{
				return ac.SelectedMemberIndex;
			}
			else
			{
				return null;
			}
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
					FirmwaresMenuItem_Click(null, null);
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

		// Still needs a good bit of refactoring
		public bool LoadRom(string path, bool? deterministicemulation = null)
		{
			// If deterministic emulation is passed in, respect that value regardless, else determine a good value (currently that simply means movies require deterministic emulaton)
			bool deterministic = deterministicemulation.HasValue ?
				deterministicemulation.Value :
				Global.MovieSession.QueuedMovie != null;
				//Global.MovieSession.Movie.IsActive;
			
			if (!GlobalWin.Tools.AskSave())
			{
				return false;
			}

			var loader = new RomLoader
				{
					ChooseArchive = LoadArhiveChooser,
					ChoosePlatform = ChoosePlatformForRom,
					Deterministic = deterministic,
					MessageCallback = GlobalWin.OSD.AddMessage
				};

			loader.OnLoadError += ShowLoadError;
			loader.OnLoadSettings += CoreSettings;
			loader.OnLoadSyncSettings += CoreSyncSettings;

			// this also happens in CloseGame().  but it needs to happen here since if we're restarting with the same core,
			// any settings changes that we made need to make it back to config before we try to instantiate that core with
			// the new settings objects
			CommitCoreSettingsToConfig(); // adelikat: I Think by reordering things, this isn't necessary anymore
			CloseGame();
			
			var nextComm = CreateCoreComm();
			CoreFileProvider.SyncCoreCommInputSignals(nextComm);
			var result = loader.LoadRom(path, nextComm);

			if (result)
			{
				if (loader.LoadedEmulator is TI83 && Global.Config.TI83autoloadKeyPad)
				{
					GlobalWin.Tools.Load<TI83KeyPad>();
				}

				Global.Emulator = loader.LoadedEmulator;
				Global.CoreComm = nextComm;
				Global.Game = loader.Game;
				CoreFileProvider.SyncCoreCommInputSignals();
				InputManager.SyncControls();

				if (loader.LoadedEmulator is NES)
				{
					var nes = loader.LoadedEmulator as NES;
					if (!string.IsNullOrWhiteSpace(nes.GameName))
					{
						Global.Game.Name = nes.GameName;
					}

					Global.Game.Status = nes.RomStatus;
				}
				else if (loader.LoadedEmulator is QuickNES)
				{
					var qns = loader.LoadedEmulator as QuickNES;
					if (!string.IsNullOrWhiteSpace(qns.BootGodName))
					{
						Global.Game.Name = qns.BootGodName;
					}
					if (qns.BootGodStatus.HasValue)
					{
						Global.Game.Status = qns.BootGodStatus.Value;
					}
				}

				Global.Rewinder.ResetRewindBuffer();

				if (Global.Emulator.CoreComm.RomStatusDetails == null && loader.Rom != null)
				{
					Global.Emulator.CoreComm.RomStatusDetails = string.Format(
						"{0}\r\nSHA1:{1}\r\nMD5:{2}\r\n",
						loader.Game.Name,
						loader.Rom.RomData.HashSHA1(),
						loader.Rom.RomData.HashMD5());
				}

				if (Global.Emulator.BoardName != null)
				{
					Console.WriteLine("Core reported BoardID: \"{0}\"", Global.Emulator.BoardName);
				}

				// restarts the lua console if a different rom is loaded.
				// im not really a fan of how this is done..
				if (Global.Config.RecentRoms.Empty || Global.Config.RecentRoms.MostRecent != loader.CanonicalFullPath)
				{
					GlobalWin.Tools.Restart<LuaConsole>();
				}

				Global.Config.RecentRoms.Add(loader.CanonicalFullPath);
				JumpLists.AddRecentItem(loader.CanonicalFullPath);

				// Don't load Save Ram if a movie is being loaded
				if (!Global.MovieSession.MovieIsQueued && File.Exists(PathManager.SaveRamPath(loader.Game)))
				{
					LoadSaveRam();
				}

				GlobalWin.Tools.Restart();

				if (Global.Config.LoadCheatFileByGame)
				{
					if (Global.CheatList.AttemptToLoadCheatFile())
					{
						GlobalWin.OSD.AddMessage("Cheats file loaded");
					}
				}

				SetWindowText();
				CurrentlyOpenRom = loader.CanonicalFullPath;
				HandlePlatformMenus();
				_stateSlots.Clear();
				UpdateCoreStatusBarButton();
				UpdateDumpIcon();
				SetMainformMovieInfo();

				Global.Rewinder.CaptureRewindState();

				Global.StickyXORAdapter.ClearStickies();
				Global.StickyXORAdapter.ClearStickyFloats();
				Global.AutofireStickyXORAdapter.ClearStickies();

				RewireSound();
				ToolHelpers.UpdateCheatRelatedTools(null, null);
				if (Global.Config.AutoLoadLastSaveSlot && _stateSlots.HasSlot(Global.Config.SaveSlot))
				{
					LoadQuickSave("QuickSave" + Global.Config.SaveSlot);
				}
				return true;
			}
			else
			{
				//This shows up if there's a problem                
				// TODO: put all these in a single method or something
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

		// TODO: should backup logic be stuffed in into Client.Common.SaveStateManager?
		public void SaveQuickSave(string quickSlotName)
		{
			if (!Global.Emulator.HasSavestates())
			{
				return;
			}

			var path = PathManager.SaveStatePrefix(Global.Game) + "." + quickSlotName + ".State";

			var file = new FileInfo(path);
			if (file.Directory != null && file.Directory.Exists == false)
			{
				file.Directory.Create();
			}


			// Make backup first
			if (Global.Config.BackupSavestates && file.Exists)
			{
				var backup = path + ".bak";
				var backupFile = new FileInfo(backup);
				if (backupFile.Exists)
				{
					backupFile.Delete();
				}

				File.Move(path, backup);
			}

			SaveState(path, quickSlotName, false);

			if (GlobalWin.Tools.Has<LuaConsole>())
			{
				GlobalWin.Tools.LuaConsole.LuaImp.CallSaveStateEvent(quickSlotName);
			}
		}

		private static void CommitCoreSettingsToConfig()
		{
			// save settings object
			var t = Global.Emulator.GetType();
			var settable = new SettingsAdapter(Global.Emulator);

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
			else if (Global.Emulator.HasSaveRam() && Global.Emulator.AsSaveRam().SaveRamModified)
			{
				SaveRam();
			}

			StopAv();

			CommitCoreSettingsToConfig();
			if (Global.MovieSession.Movie.IsActive) // Note: this must be called after CommitCoreSettingsToConfig()
			{
				StopMovie(true);
			}
			Global.Emulator.Dispose();
			Global.CoreComm = CreateCoreComm();
			CoreFileProvider.SyncCoreCommInputSignals();
			Global.Emulator = new NullEmulator(Global.CoreComm, Global.Config.GetCoreSettings<NullEmulator>());
			Global.ActiveController = Global.NullControls;
			Global.AutoFireController = Global.AutofireNullControls;
			RewireSound();
			RebootStatusBarIcon.Visible = false;
			GameIsClosing = false;
		}

		public bool GameIsClosing { get; set; } // Let's tools make better decisions when being called by CloseGame

		public void CloseRom(bool clearSram = false)
		{
			//This gets called after Close Game gets called.
			//Tested with NESHawk and SMB3 (U)
			if (GlobalWin.Tools.AskSave())
			{
				CloseGame(clearSram);
				Global.CoreComm = CreateCoreComm();
				CoreFileProvider.SyncCoreCommInputSignals();
				Global.Emulator = new NullEmulator(Global.CoreComm, Global.Config.GetCoreSettings<NullEmulator>());
				Global.Game = GameInfo.NullInstance;

				GlobalWin.Tools.Restart();
				RewireSound();
				Global.Rewinder.ResetRewindBuffer();
				Text = "BizHawk" + (VersionInfo.DeveloperBuild ? " (interim) " : string.Empty);
				HandlePlatformMenus();
				_stateSlots.Clear();
				UpdateDumpIcon();
				UpdateCoreStatusBarButton();
				ClearHolds();
				PauseOnFrame = null;
				ToolHelpers.UpdateCheatRelatedTools(null, null);
				UpdateStatusSlots();
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
			if (enabled)
			{
				Global.Rewinder.RewindActive = true;
				GlobalWin.OSD.AddMessage("Rewind enabled");
			}
			else
			{
				Global.Rewinder.RewindActive = false;
				GlobalWin.OSD.AddMessage("Rewind suspended");
			}
		}

		public void ClearRewindData()
		{
			Global.Rewinder.ResetRewindBuffer();
		}

		#endregion

		#region Tool Control API

		// TODO: move me
		public IControlMainform master { get; private set; }
		public void RelinquishControl(IControlMainform master)
		{
			this.master = master;
		}

		private void ToggleReadOnly()
		{
			if (IsSlave && master.WantsToControlReadOnly)
			{
				master.ToggleReadOnly();
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

		public void StopMovie(bool saveChanges = true)
		{
			if (IsSlave && master.WantsToControlStopMovie)
			{
				master.StopMovie(!saveChanges);
			}
			else
			{
				Global.MovieSession.StopMovie(saveChanges);
				SetMainformMovieInfo();
				UpdateStatusSlots();
			}
		}

		private bool IsSlave
		{
			get { return master != null; }
		}

		public void TakeBackControl()
		{
			master = null;
		}

		private void GBAcoresettingsToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			GenericCoreConfig.DoDialog(this, "Gameboy Advance Settings");
		}


		private void CaptureRewind(bool suppressCaptureRewind)
		{
			if (IsSlave && master.WantsToControlRewind)
			{
				master.CaptureRewind();
			}
			else if (!suppressCaptureRewind && Global.Rewinder.RewindActive)
			{
				Global.Rewinder.CaptureRewindState();
			}
		}

		private bool Rewind(ref bool runFrame)
		{
			if (IsSlave && master.WantsToControlRewind)
			{
				if (Global.ClientControls["Rewind"] || PressRewind)
				{
					runFrame = false; // TODO: the master should be deciding this!
					return master.Rewind();
				}
			}
			
			var isRewinding = false;
			if (Global.Rewinder.RewindActive && (Global.ClientControls["Rewind"] || PressRewind)
				&& !Global.MovieSession.Movie.IsRecording) // Rewind isn't "bulletproof" and can desync a recording movie!
			{
				Global.Rewinder.Rewind(1);

				runFrame = Global.Rewinder.Count != 0;
				isRewinding = true;
			}

			return isRewinding;
		}

		#endregion

		private void LinkConnectStatusBarButton_Click(object sender, EventArgs e)
		{
			// TODO: it would be cool if clicking this toggled the state
			if (Global.Emulator.CoreComm.LinkConnected == true)
			{
				//Disconnect
				//This Value:  cablediscosignal_new  Changes to False, The Core will disconnect

			}
			else if (Global.Emulator.CoreComm.LinkConnected == false)
			{
				//Reconnect

			}
		}

		private void barcodeReaderToolStripMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<BarcodeEntry>();
		}

		private void FeaturesMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.Tools.Load<CoreFeatureAnalysis>();
		}

		private void HelpSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			FeaturesMenuItem.Visible = VersionInfo.DeveloperBuild;
		}
	}
}
