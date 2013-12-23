using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Atari.Atari2600;
using BizHawk.Emulation.Cores.Atari.Atari7800;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Computers.Commodore64;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Intellivision;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBA;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.Genesis;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.Sega.Saturn;
using BizHawk.Emulation.Cores.Sony.PSP;
using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm : Form
	{
		#region Constructors and Initialization, and Tear down

		private void MainForm_Load(object sender, EventArgs e)
		{
			Text = "BizHawk" + (VersionInfo.INTERIM ? " (interim) " : String.Empty);

			// Hide Status bar icons
			PlayRecordStatusButton.Visible = false;
			AVIStatusLabel.Visible = false;
			SetPauseStatusbarIcon();
			ToolHelpers.UpdateCheatRelatedTools(null, null);
			RebootStatusBarIcon.Visible = false;
			Global.CheatList.Changed += ToolHelpers.UpdateCheatRelatedTools;
		}

		static MainForm()
		{
			// If this isnt here, then our assemblyresolving hacks wont work due to the check for MainForm.INTERIM
			// its.. weird. dont ask.
		}

		public MainForm(string[] args)
		{
			GlobalWin.MainForm = this;
			Global.FirmwareManager = new FirmwareManager();
			Global.MovieSession = new MovieSession
			{
				Movie = new Movie(),
				MessageCallback = GlobalWin.OSD.AddMessage,
				AskYesNoCallback = StateErrorAskUser
			};
			_mainWait = new AutoResetEvent(false);
			Icon = Properties.Resources.logo;
			InitializeComponent();
			Global.Game = GameInfo.GetNullGame();
			if (Global.Config.ShowLogWindow)
			{
				ShowConsole();
				DisplayLogWindowMenuItem.Checked = true;
			}

			_throttle = new Throttle();

			FFMpeg.FFMpegPath = PathManager.MakeProgramRelativePath(Global.Config.FFMpegPath);

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
					return Util.ReadAllBytes(NesCartFile.GetStream());
				}
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
				if (GlobalWin.Sound != null)
				{
					GlobalWin.Sound.StopSound();
				}
			};

			ResizeEnd += (o, e) =>
			{
				if (GlobalWin.RenderPanel != null)
				{
					GlobalWin.RenderPanel.Resized = true;
				}

				if (GlobalWin.Sound != null)
				{
					GlobalWin.Sound.StartSound();
				}
			};

			Input.Initialize();
			InitControls();
			Global.CoreComm = new CoreComm(ShowMessageCoreComm);
			CoreFileProvider.SyncCoreCommInputSignals();
			Global.Emulator = new NullEmulator(Global.CoreComm);
			Global.ActiveController = Global.NullControls;
			Global.AutoFireController = Global.AutofireNullControls;
			Global.AutofireStickyXORAdapter.SetOnOffPatternFromConfig();
#if WINDOWS
			GlobalWin.Sound = new Sound(Handle, GlobalWin.DSound);
#else
			Global.Sound = new Sound();
#endif
			GlobalWin.Sound.StartSound();
			RewireInputChain();
			GlobalWin.Tools = new ToolManager();
			RewireSound();

			// TODO - replace this with some kind of standard dictionary-yielding parser in a separate component
			string cmdRom = null;
			string cmdLoadState = null;
			string cmdMovie = null;
			string cmdDumpType = null;
			string cmdDumpName = null;

			if (Global.Config.MainWndx >= 0 && Global.Config.MainWndy >= 0 && Global.Config.SaveWindowPosition)
			{
				Location = new Point(Global.Config.MainWndx, Global.Config.MainWndy);
			}

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
					autoCloseOnDump = true;
				}
				else if (arg.StartsWith("--fullscreen"))
				{
					ToggleFullscreen();
				}
				else
				{
					cmdRom = arg;
				}
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
				LoadRomFromRecent(Global.Config.RecentRoms[0]);
			}

			if (cmdMovie != null)
			{
				if (Global.Game == null)
				{
					OpenRom();
				}
				else
				{
					var movie = new Movie(cmdMovie);
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
				if (Global.Game == null)
				{
					OpenRom();
				}
				else
				{
					StartNewMovie(new Movie(Global.Config.RecentMovies[0]), false);
				}
			}

			if (cmdLoadState != null && Global.Game != null)
			{
				LoadState("QuickSave" + cmdLoadState);
			}
			else if (Global.Config.AutoLoadLastSaveSlot && Global.Game != null)
			{
				LoadState("QuickSave" + Global.Config.SaveSlot);
			}

			if (Global.Config.RecentWatches.AutoLoad)
			{
				GlobalWin.Tools.LoadRamWatch(!Global.Config.DisplayRamWatch);
			}

			if (Global.Config.RecentSearches.AutoLoad)
			{
				GlobalWin.Tools.Load<RamSearch>();
			}

			if (Global.Config.AutoLoadHexEditor)
			{
				GlobalWin.Tools.Load<HexEditor>();
			}

			if (Global.Config.RecentCheats.AutoLoad)
			{
				GlobalWin.Tools.Load<Cheats>();
			}

			if (Global.Config.AutoLoadNESPPU && Global.Emulator is NES)
			{
				GlobalWin.Tools.Load<NESPPU>();
			}

			if (Global.Config.AutoLoadNESNameTable && Global.Emulator is NES)
			{
				GlobalWin.Tools.Load<NESNameTableViewer>();
			}

			if (Global.Config.AutoLoadNESDebugger && Global.Emulator is NES)
			{
				GlobalWin.Tools.Load<NESDebugger>();
			}

			if (Global.Config.NESGGAutoload && Global.Emulator is NES)
			{
				GlobalWin.Tools.LoadGameGenieEc();
			}

			if (Global.Config.AutoLoadGBGPUView && Global.Emulator is Gameboy)
			{
				GlobalWin.Tools.Load<GBGPUView>();
			}

			if (Global.Config.AutoloadTAStudio)
			{
				LoadTAStudio();
			}

			if (Global.Config.AutoloadVirtualPad)
			{
				GlobalWin.Tools.Load<VirtualPadForm>();
			}

			if (Global.Config.AutoLoadLuaConsole)
			{
				OpenLuaConsole();
			}

			if (Global.Config.PCEBGViewerAutoload && Global.Emulator is PCEngine)
			{
				GlobalWin.Tools.Load<PCEBGViewer>();
			}

			if (Global.Config.AutoLoadSNESGraphicsDebugger && Global.Emulator is LibsnesCore)
			{
				GlobalWin.Tools.Load<SNESGraphicsDebugger>();
			}

			if (Global.Config.TraceLoggerAutoLoad)
			{
				GlobalWin.Tools.LoadTraceLogger();
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

			if (!VersionInfo.INTERIM)
			{
				NESDebuggerMenuItem.Enabled = false;
			}

			// start dumping, if appropriate
			if (cmdDumpType != null && cmdDumpName != null)
			{
				RecordAVI(cmdDumpType, cmdDumpName);
			}

			UpdateStatusSlots();

			_renderTarget.Paint += (o, e) =>
			{
				GlobalWin.DisplayManager.NeedsToPaint = true;
			};
		}

		public void ProgramRunLoop()
		{
			CheckMessages();
			LogConsole.PositionConsole();

			for (;;)
			{
				Input.Instance.Update();

				// handle events and dispatch as a hotkey action, or a hotkey button, or an input button
				ProcessInput();
				Global.ClientControls.LatchFromPhysical(GlobalWin.HotkeyCoalescer);
				Global.ActiveController.LatchFromPhysical(GlobalWin.ControllerInputCoalescer);

				Global.ActiveController.OR_FromLogical(Global.ClickyVirtualPadController);
				Global.AutoFireController.LatchFromPhysical(GlobalWin.ControllerInputCoalescer);

				if (Global.ClientControls["Autohold"])
				{
					Global.StickyXORAdapter.MassToggleStickyState(Global.ActiveController.PressedButtons);
					Global.AutofireStickyXORAdapter.MassToggleStickyState(Global.AutoFireController.PressedButtons);
				}
				else if (Global.ClientControls["Autofire"])
				{
					Global.AutofireStickyXORAdapter.MassToggleStickyState(Global.ActiveController.PressedButtons);
				}

				if (GlobalWin.Tools.Has<LuaConsole>())
				{
					GlobalWin.Tools.LuaConsole.ResumeScripts(false);
				}

				StepRunLoop_Core();
				StepRunLoop_Throttle();

				if (GlobalWin.DisplayManager.NeedsToPaint)
				{
					Render();
				}

				CheckMessages();
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
			if (GlobalWin.DisplayManager != null)
			{
				GlobalWin.DisplayManager.Dispose();
			}

			GlobalWin.DisplayManager = null;

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
		public bool RewindActive = true;
		public bool EmulatorPaused { get; private set; }

		#endregion

		#region Public Methods

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
					ActiveForm is VirtualPadForm)
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

		public void ProcessInput()
		{
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
						GlobalWin.ControllerInputCoalescer.Receive(ie);

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
						GlobalWin.ControllerInputCoalescer.Receive(ie);
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
							GlobalWin.ControllerInputCoalescer.Receive(ie);
						}

						break;
				}

			} // foreach event

			// also handle floats
			GlobalWin.ControllerInputCoalescer.AcceptNewFloats(Input.Instance.GetFloats());
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
		}

		public string GenerateDefaultCheatFilename()
		{
			var pathEntry = Global.Config.PathEntries[Global.Game.System, "Cheats"];
			
			if (pathEntry == null)
			{
				pathEntry = Global.Config.PathEntries[Global.Game.System, "Base"];
			}
			
			var path = PathManager.MakeAbsolutePath(pathEntry.Path, Global.Game.System);

			var f = new FileInfo(path);
			if (f.Directory != null && f.Directory.Exists == false)
			{
				f.Directory.Create();
			}

			return Path.Combine(path, PathManager.FilesystemSafeName(Global.Game) + ".cht");
		}

		public void TakeScreenshotToClipboard()
		{
			using (var img = Global.Config.Screenshot_CaptureOSD ? CaptureOSD() : MakeScreenshotImage())
			{
				Clipboard.SetImage(img);
			}

			GlobalWin.OSD.AddMessage("Screenshot saved to clipboard.");
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

			using (var img = Global.Config.Screenshot_CaptureOSD ? CaptureOSD() : MakeScreenshotImage())
			{
				img.Save(fi.FullName, ImageFormat.Png);
			}

			GlobalWin.OSD.AddMessage(fi.Name + " saved.");
		}

		public void FrameBufferResized()
		{
			// run this entire thing exactly twice, since the first resize may adjust the menu stacking
			for (int i = 0; i < 2; i++)
			{
				var video = Global.Emulator.VideoProvider;
				int zoom = Global.Config.TargetZoomFactor;
				var area = Screen.FromControl(this).WorkingArea;

				int borderWidth = Size.Width - _renderTarget.Size.Width;
				int borderHeight = Size.Height - _renderTarget.Size.Height;

				// start at target zoom and work way down until we find acceptable zoom
				for (; zoom >= 1; zoom--)
				{
					if ((((video.BufferWidth * zoom) + borderWidth) < area.Width)
					    && (((video.BufferHeight * zoom) + borderHeight) < area.Height))
					{
						break;
					}
				}

				// Change size
				Size = new Size((video.BufferWidth * zoom) + borderWidth, ((video.BufferHeight * zoom) + borderHeight));
				PerformLayout();
				GlobalWin.RenderPanel.Resized = true;

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

		public void ToggleFullscreen()
		{
			if (_inFullscreen == false)
			{
				_windowed_location = Location;
				FormBorderStyle = FormBorderStyle.None;
				WindowState = FormWindowState.Maximized;

				MainMenuStrip.Visible = Global.Config.ShowMenuInFullscreen;

				MainStatusBar.Visible = false;
				PerformLayout();
				GlobalWin.RenderPanel.Resized = true;
				_inFullscreen = true;
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
				_inFullscreen = false;
			}
		}

		public void LoadTAStudio()
		{
			GlobalWin.Tools.Load<TAStudio>();
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
			LogWindowAsConsoleMenuItem.Enabled = true;
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
				CheatStatusButton.ToolTipText = String.Empty;
				CheatStatusButton.Image = Properties.Resources.Blank;
				CheatStatusButton.Visible = false;
			}
		}

		public void SNES_ToggleBG1(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore))
			{
				return;
			}

			if (setto.HasValue)
			{
				Global.Config.SNES_ShowBG1_1 = Global.Config.SNES_ShowBG1_0 = setto.Value;
			}
			else
			{
				Global.Config.SNES_ShowBG1_1 = Global.Config.SNES_ShowBG1_0 ^= true;
			}

			CoreFileProvider.SyncCoreCommInputSignals();
			GlobalWin.OSD.AddMessage(Global.Config.SNES_ShowBG1_1 ? "BG 1 Layer On" : "BG 1 Layer Off");
		}

		public void SNES_ToggleBG2(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore))
			{
				return;
			}

			if (setto.HasValue)
			{
				Global.Config.SNES_ShowBG2_1 = Global.Config.SNES_ShowBG2_0 = setto.Value;
			}
			else
			{
				Global.Config.SNES_ShowBG2_1 = Global.Config.SNES_ShowBG2_0 ^= true;
			}

			CoreFileProvider.SyncCoreCommInputSignals();
			GlobalWin.OSD.AddMessage(Global.Config.SNES_ShowBG2_1 ? "BG 2 Layer On" : "BG 2 Layer Off");
		}

		public void SNES_ToggleBG3(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore))
			{
				return;
			}

			if (setto.HasValue)
			{
				Global.Config.SNES_ShowBG3_1 = Global.Config.SNES_ShowBG3_0 = setto.Value;
			}
			else
			{
				Global.Config.SNES_ShowBG3_1 = Global.Config.SNES_ShowBG3_0 ^= true;
			}

			CoreFileProvider.SyncCoreCommInputSignals();
			GlobalWin.OSD.AddMessage(Global.Config.SNES_ShowBG3_1 ? "BG 3 Layer On" : "BG 3 Layer Off");
		}

		public void SNES_ToggleBG4(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore))
			{
				return;
			}

			if (setto.HasValue)
			{
				Global.Config.SNES_ShowBG4_1 = Global.Config.SNES_ShowBG4_0 = setto.Value;
			}
			else
			{
				Global.Config.SNES_ShowBG4_1 = Global.Config.SNES_ShowBG4_0 ^= true;
			}

			CoreFileProvider.SyncCoreCommInputSignals();
			GlobalWin.OSD.AddMessage(Global.Config.SNES_ShowBG4_1 ? "BG 4 Layer On" : "BG 4 Layer Off");
		}

		public void SNES_ToggleOBJ1(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore))
			{
				return;
			}

			if (setto.HasValue)
			{
				Global.Config.SNES_ShowOBJ1 = setto.Value;
			}
			else
			{
				Global.Config.SNES_ShowOBJ1 ^= true;
			}

			CoreFileProvider.SyncCoreCommInputSignals();
			GlobalWin.OSD.AddMessage(Global.Config.SNES_ShowOBJ1 ? "OBJ 1 Layer On" : "OBJ 1 Layer Off");
		}

		public void SNES_ToggleOBJ2(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore))
			{
				return;
			}

			if (setto.HasValue)
			{
				Global.Config.SNES_ShowOBJ2 = setto.Value;
			}
			else
			{
				Global.Config.SNES_ShowOBJ2 ^= true;
			}
			CoreFileProvider.SyncCoreCommInputSignals();
			GlobalWin.OSD.AddMessage(Global.Config.SNES_ShowOBJ2 ? "OBJ 2 Layer On" : "OBJ 2 Layer Off");
		}

		public void SNES_ToggleOBJ3(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore))
			{
				return;
			}

			if (setto.HasValue)
			{
				Global.Config.SNES_ShowOBJ3 = setto.Value;
			}
			else
			{
				Global.Config.SNES_ShowOBJ3 ^= true;
			}

			CoreFileProvider.SyncCoreCommInputSignals();
			GlobalWin.OSD.AddMessage(Global.Config.SNES_ShowOBJ3 ? "OBJ 3 Layer On" : "OBJ 3 Layer Off");
		}

		public void SNES_ToggleOBJ4(bool? setto = null)
		{
			if (!(Global.Emulator is LibsnesCore))
			{
				return;
			}

			if (setto.HasValue)
			{
				Global.Config.SNES_ShowOBJ4 = setto.Value;
			}
			else
			{
				Global.Config.SNES_ShowOBJ4 ^= true;
			}

			CoreFileProvider.SyncCoreCommInputSignals();
			GlobalWin.OSD.AddMessage(Global.Config.SNES_ShowOBJ4 ? "OBJ 4 Layer On" : "OBJ 4 Layer Off");
		}

		#endregion

		#region Private variables

		private int _lastWidth = -1;
		private int _lastHeight = -1;
		private Control _renderTarget;
		private RetainedViewportPanel _retainedPanel;
		private readonly SaveSlotManager _stateSlots = new SaveSlotManager();
		private readonly Dictionary<string, string> _snesPrepared = new Dictionary<string, string>();

		// AVI/WAV state
		private IVideoWriter _currAviWriter;
		private ISoundProvider _aviSoundInput;
		private MetaspuSoundProvider _dumpProxy; // an audio proxy used for dumping
		private long _soundRemainder; // audio timekeeping for video dumping
		private int _avwriterResizew;
		private int _avwriterResizeh;
		private EventWaitHandle _mainWait;
		private bool _exit;
		private bool _runloopFrameProgress;
		private DateTime _frameAdvanceTimestamp = DateTime.MinValue;
		private int _runloopFps;
		private int _runloopLastFps;
		private bool _runloopFrameadvance;
		private DateTime _runloopSecond;
		private bool _runloopLastFf;

		private readonly Throttle _throttle;
		private bool _unthrottled;

		// For handling automatic pausing when entering the menu
		private bool _wasPaused;
		private bool _didMenuPause;

		private bool _inFullscreen;
		private Point _windowed_location;

		private int _autoDumpLength;
		private readonly bool autoCloseOnDump;
		private int _lastOpenRomFilter;

		// workaround for possible memory leak in SysdrawingRenderPanel
		private RetainedViewportPanel captureosd_rvp;
		private SysdrawingRenderPanel captureosd_srp;

		#endregion

		#region Private methods

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
			HandleToggleLight();
		}

		private static void ClearAutohold()
		{
			Global.StickyXORAdapter.ClearStickies();
			Global.AutofireStickyXORAdapter.ClearStickies();

			if (GlobalWin.Tools.Has<VirtualPadForm>())
			{
				GlobalWin.Tools.VirtualPad.ClearVirtualPadHolds();
			}

			GlobalWin.OSD.AddMessage("Autohold keys cleared");
		}

		private bool CheckHotkey(string trigger)
		{
			// todo - could have these in a table somehow ?
			switch (trigger)
			{
				default:
					return false;
				case "Pause": TogglePause(); break;
				case "Toggle Throttle":
					_unthrottled ^= true;
					GlobalWin.OSD.AddMessage("Unthrottled: " + _unthrottled);
					break;
				case "Soft Reset": SoftReset(); break;
				case "Hard Reset": HardReset(); break;
				case "Quick Load": LoadState("QuickSave" + Global.Config.SaveSlot); break;
				case "Quick Save": SaveState("QuickSave" + Global.Config.SaveSlot); break;
				case "Clear Autohold": ClearAutohold(); break;
				case "Screenshot": TakeScreenshot(); break;
				case "Full Screen": ToggleFullscreen(); break;
				case "Open ROM": OpenRom(); break;
				case "Close ROM": CloseRom(); break;
				case "Display FPS": ToggleFPS(); break;
				case "Frame Counter": ToggleFrameCounter(); break;
				case "Lag Counter": ToggleLagCounter(); break;
				case "Input Display": ToggleInputDisplay(); break;
				case "Toggle BG Input": ToggleBackgroundInput(); break;
				case "Toggle Menu": MainMenuStrip.Visible ^= true; break;
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
							GlobalWin.OSD.AddMessage("Multi-track can not be used in Full Movie Loadstates mode");
						}
						else
						{
							Global.MovieSession.MultiTrack.IsActive = !Global.MovieSession.MultiTrack.IsActive;
							if (Global.MovieSession.MultiTrack.IsActive)
							{
								GlobalWin.OSD.AddMessage("MultiTrack Enabled");
								GlobalWin.OSD.MT = "Recording None";
							}
							else
							{
								GlobalWin.OSD.AddMessage("MultiTrack Disabled");
							}
							Global.MovieSession.MultiTrack.RecordAll = false;
							Global.MovieSession.MultiTrack.CurrentPlayer = 0;
						}
					}
					else
					{
						GlobalWin.OSD.AddMessage("MultiTrack cannot be enabled while not recording.");
					}
					GlobalWin.DisplayManager.NeedsToPaint = true;
					break;
				case "MT Select All":
					Global.MovieSession.MultiTrack.CurrentPlayer = 0;
					Global.MovieSession.MultiTrack.RecordAll = true;
					GlobalWin.OSD.MT = "Recording All";
					GlobalWin.DisplayManager.NeedsToPaint = true;
					break;
				case "MT Select None":
					Global.MovieSession.MultiTrack.CurrentPlayer = 0;
					Global.MovieSession.MultiTrack.RecordAll = false;
					GlobalWin.OSD.MT = "Recording None";
					GlobalWin.DisplayManager.NeedsToPaint = true;
					break;
				case "MT Increment Player":
					Global.MovieSession.MultiTrack.CurrentPlayer++;
					Global.MovieSession.MultiTrack.RecordAll = false;
					if (Global.MovieSession.MultiTrack.CurrentPlayer > 5) // TODO: Replace with console's maximum or current maximum players??!
					{
						Global.MovieSession.MultiTrack.CurrentPlayer = 1;
					}
					GlobalWin.OSD.MT = "Recording Player " + Global.MovieSession.MultiTrack.CurrentPlayer;
					GlobalWin.DisplayManager.NeedsToPaint = true;
					break;
				case "MT Decrement Player":
					Global.MovieSession.MultiTrack.CurrentPlayer--;
					Global.MovieSession.MultiTrack.RecordAll = false;
					if (Global.MovieSession.MultiTrack.CurrentPlayer < 1)
					{
						Global.MovieSession.MultiTrack.CurrentPlayer = 5; // TODO: Replace with console's maximum or current maximum players??!
					}
					GlobalWin.OSD.MT = "Recording Player " + Global.MovieSession.MultiTrack.CurrentPlayer;
					GlobalWin.DisplayManager.NeedsToPaint = true;
					break;
				case "Movie Poke": ToggleModePokeMode(); break;

				case "Ram Watch": GlobalWin.Tools.LoadRamWatch(true); break;
				case "Ram Search": GlobalWin.Tools.Load<RamSearch>(); break;
				case "Hex Editor": GlobalWin.Tools.Load<HexEditor>(); break;
				case "Trace Logger": GlobalWin.Tools.LoadTraceLogger(); break;
				case "Lua Console": OpenLuaConsole(); break;
				case "Cheats": GlobalWin.Tools.Load<Cheats>(); break;
				case "TAStudio": LoadTAStudio(); break;
				case "ToolBox": GlobalWin.Tools.Load<ToolBox>(); break;
				case "Virtual Pad": GlobalWin.Tools.Load<VirtualPadForm>(); break;

				case "Do Search": GlobalWin.Tools.RamSearch.DoSearch(); break;
				case "New Search": GlobalWin.Tools.RamSearch.NewSearch(); break;
				case "Previous Compare To": GlobalWin.Tools.RamSearch.NextCompareTo(reverse: true); break;
				case "Next Compare To": GlobalWin.Tools.RamSearch.NextCompareTo(); break;
				case "Previous Operator": GlobalWin.Tools.RamSearch.NextOperator(reverse: true); break;
				case "Next Operator": GlobalWin.Tools.RamSearch.NextOperator(); break;

				case "Toggle BG 1": SNES_ToggleBG1(); break;
				case "Toggle BG 2": SNES_ToggleBG2(); break;
				case "Toggle BG 3": SNES_ToggleBG3(); break;
				case "Toggle BG 4": SNES_ToggleBG4(); break;
				case "Toggle OBJ 1": SNES_ToggleOBJ1(); break;
				case "Toggle OBJ 2": SNES_ToggleOBJ2(); break;
				case "Toggle OBJ 3": SNES_ToggleOBJ3(); break;
				case "Toggle OBJ 4": SNES_ToggleOBJ4(); break;


				case "Y Up Small": GlobalWin.Tools.VirtualPad.BumpAnalogValue(null, Global.Config.Analog_SmallChange); break;
				case "Y Up Large": GlobalWin.Tools.VirtualPad.BumpAnalogValue(null, Global.Config.Analog_LargeChange); break;
				case "Y Down Small": GlobalWin.Tools.VirtualPad.BumpAnalogValue(null, -(Global.Config.Analog_SmallChange)); break;
				case "Y Down Large": GlobalWin.Tools.VirtualPad.BumpAnalogValue(null, -(Global.Config.Analog_LargeChange)); break;

				case "X Up Small": GlobalWin.Tools.VirtualPad.BumpAnalogValue(Global.Config.Analog_SmallChange, null); break;
				case "X Up Large": GlobalWin.Tools.VirtualPad.BumpAnalogValue(Global.Config.Analog_LargeChange, null); break;
				case "X Down Small": GlobalWin.Tools.VirtualPad.BumpAnalogValue(-(Global.Config.Analog_SmallChange), null); break;
				case "X Down Large": GlobalWin.Tools.VirtualPad.BumpAnalogValue(-(Global.Config.Analog_LargeChange), null); break;
			}

			return true;
		}

		private void UpdateDumpIcon()
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
			try // zero says: this is sort of sketchy... but this is no time for rearchitecting
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
					using (var reader = new BinaryReader(
							new FileStream(PathManager.SaveRamPath(Global.Game), FileMode.Open, FileAccess.Read)))
					{
						reader.Read(sram, 0, sram.Length);
					}
				}

				Global.Emulator.StoreSaveRam(sram);
			}
			catch (IOException)
			{
				
			}
		}

		private static void SaveRam()
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
			var saveram = Global.Emulator.ReadSaveRam();

			writer.Write(saveram, 0, saveram.Length);
			writer.Close();
		}

		private void SelectSlot(int num)
		{
			Global.Config.SaveSlot = num;
			SaveSlotSelectedMessage();
			UpdateStatusSlots();
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
				if (!Global.Emulator.StartAsyncSound())
				{
					// if the core doesn't support async mode, use a standard vecna wrapper
					GlobalWin.Sound.SetAsyncInputPin(new MetaspuAsync(Global.Emulator.SyncSoundProvider, ESynchMethod.ESynchMethod_V));
				}
				else
				{
					GlobalWin.Sound.SetAsyncInputPin(Global.Emulator.SoundProvider);
				}
			}
		}

		private static Controller BindToDefinition(ControllerDefinition def, IDictionary<string, Dictionary<string, string>> allbinds, IDictionary<string, Dictionary<string, Config.AnalogBind>> analogbinds)
		{
			var ret = new Controller(def);
			Dictionary<string, string> binds;
			if (allbinds.TryGetValue(def.Name, out binds))
			{
				foreach (var cbutton in def.BoolButtons)
				{
					string bind;
					if (binds.TryGetValue(cbutton, out bind))
					{
						ret.BindMulti(cbutton, bind);
					}
				}
			}

			Dictionary<string, Config.AnalogBind> abinds;
			if (analogbinds.TryGetValue(def.Name, out abinds))
			{
				foreach (var cbutton in def.FloatControls)
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

		private static AutofireController BindToDefinitionAF(ControllerDefinition def, IDictionary<string, Dictionary<string, string>> allbinds)
		{
			var ret = new AutofireController(def);
			Dictionary<string, string> binds;
			if (allbinds.TryGetValue(def.Name, out binds))
			{
				foreach (var cbutton in def.BoolButtons)
				{
					string bind;
					if (binds.TryGetValue(cbutton, out bind))
					{
						ret.BindMulti(cbutton, bind);
					}
				}
			}

			return ret;
		}

		private void SaturnSetPrefs(Yabause e = null)
		{
			if (e == null)
			{
				e = Global.Emulator as Yabause;
			}

			if (Global.Config.SaturnUseGL != e.GLMode)
			{
				// theoretically possible; not coded. meh.
				FlagNeedsReboot();
				return;
			}

			if (e.GLMode && Global.Config.SaturnUseGL)
			{
				if (Global.Config.SaturnDispFree)
				{
					e.SetGLRes(0, Global.Config.SaturnGLW, Global.Config.SaturnGLH);
				}
				else
				{
					e.SetGLRes(Global.Config.SaturnDispFactor, 0, 0);
				}
			}
		}

		private void HandlePlatformMenus()
		{
			var system = String.Empty;

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
					{
						SNESSubMenu.Text = "&SGB";
					}
					else
					{
						SNESSubMenu.Text = "&SNES";
					}

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

		private static string DisplayNameForSystem(string system)
		{
			var str = String.Empty;
			switch (system)
			{
				case "INTV": str = "Intellivision"; break;
				case "SG": str = "SG-1000"; break;
				case "SMS": str = "Sega Master System"; break;
				case "GG": str = "Game Gear"; break;
				case "PCECD": str = "TurboGrafx-16 (CD)"; break;
				case "PCE": str = "TurboGrafx-16"; break;
				case "SGX": str = "SuperGrafx"; break;
				case "GEN": str = "Genesis"; break;
				case "TI83": str = "TI-83"; break;
				case "NES": str = "NES"; break;
				case "SNES": str = "SNES"; break;
				case "GB": str = "Game Boy"; break;
				case "GBC": str = "Game Boy Color"; break;
				case "A26": str = "Atari 2600"; break;
				case "A78": str = "Atari 7800"; break;
				case "C64": str = "Commodore 64"; break;
				case "Coleco": str = "ColecoVision"; break;
				case "GBA": str = "Game Boy Advance"; break;
				case "N64": str = "Nintendo 64"; break;
				case "SAT": str = "Saturn"; break;
				case "DGB": str = "Game Boy Link"; break;
			}

			if (VersionInfo.INTERIM)
			{
				str += " (interim)";
			}

			return str;
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

		private static bool IsValidMovieExtension(string ext)
		{
			if (ext.ToUpper() == "." + Global.Config.MovieExtension)
			{
				return true;
			}
			else if (ext.ToUpper() == ".TAS" || ext.ToUpper() == ".BKM")
			{
				return true;
			}

			return false;
		}

		private void LoadMoviesFromRecent(string path)
		{
			var movie = new Movie(path);

			if (!movie.Loaded)
			{
				ToolHelpers.HandleLoadError(Global.Config.RecentMovies, path);
			}
			else
			{
				Global.MovieSession.ReadOnly = true;
				StartNewMovie(movie, false);
			}
		}

		private void LoadRomFromRecent(string rom)
		{
			if (!LoadRom(rom))
			{
				ToolHelpers.HandleLoadError(Global.Config.RecentRoms, rom);
			}
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

		private void Cheats_Restart()
		{
			// If Cheat tool is loaded, restarting will restart the list too anyway
			if (!GlobalWin.Tools.Has<Cheats>())
			{
				Global.CheatList.NewList(GenerateDefaultCheatFilename());
			}
		}

		// Contains a mapping: profilename->exepath ; or null if the exe wasnt available
		private string SNES_Prepare(string profile)
		{
			SNES_Check(profile);
			if (_snesPrepared[profile] == null)
			{
				throw new InvalidOperationException("Couldn't locate the executable for SNES emulation for profile: " + profile + ". Please make sure you're using a fresh dearchive of a BizHawk distribution.");
			}

			return _snesPrepared[profile];
		}

		private void SNES_Check(string profile)
		{
			if (_snesPrepared.ContainsKey(profile))
			{
				return;
			}

			const string bits = "32";

			// disabled til it works
			// if (Win32.Is64BitOperatingSystem)
			// bits = "64";
			var exename = "libsneshawk-" + bits + "-" + profile.ToLower() + ".exe";
			var thisDir = PathManager.GetExeDirectoryAbsolute();
			var exePath = Path.Combine(thisDir, exename);

			if (!File.Exists(exePath))
			{
				exePath = Path.Combine(Path.Combine(thisDir, "dll"), exename);
			}

			if (!File.Exists(exePath))
			{
				exePath = null;
			}

			_snesPrepared[profile] = exePath;
		}

		private void SyncPresentationMode()
		{
			GlobalWin.DisplayManager.Suspend();

#if WINDOWS
			bool gdi = Global.Config.DisplayGDI || GlobalWin.Direct3D == null;
#endif
			if (_renderTarget != null)
			{
				_renderTarget.Dispose();
				Controls.Remove(_renderTarget);
			}

			if (_retainedPanel != null)
			{
				_retainedPanel.Dispose();
			}

			if (GlobalWin.RenderPanel != null)
			{
				GlobalWin.RenderPanel.Dispose();
			}

#if WINDOWS
			if (gdi)
#endif
				_renderTarget = _retainedPanel = new RetainedViewportPanel();
#if WINDOWS
			else _renderTarget = new ViewportPanel();
#endif
			Controls.Add(_renderTarget);
			Controls.SetChildIndex(_renderTarget, 0);

			_renderTarget.Dock = DockStyle.Fill;
			_renderTarget.BackColor = Color.Black;

#if WINDOWS
			if (gdi)
			{
#endif
				GlobalWin.RenderPanel = new SysdrawingRenderPanel(_retainedPanel);
				_retainedPanel.ActivateThreaded();
#if WINDOWS
			}
			else
			{
				try
				{
					var d3dPanel = new Direct3DRenderPanel(GlobalWin.Direct3D, _renderTarget);
					d3dPanel.CreateDevice();
					GlobalWin.RenderPanel = d3dPanel;
				}
				catch
				{
					Program.DisplayDirect3DError();
					GlobalWin.Direct3D.Dispose();
					GlobalWin.Direct3D = null;
					SyncPresentationMode();
				}
			}
#endif

			GlobalWin.DisplayManager.Resume();
		}

		private void SyncThrottle()
		{
			bool fastforward = Global.ClientControls["Fast Forward"] || FastForward;
			bool superfastforward = Global.ClientControls["Turbo"];
			Global.ForceNoThrottle = _unthrottled || fastforward;

			// realtime throttle is never going to be so exact that using a double here is wrong
			_throttle.SetCoreFps(Global.Emulator.CoreComm.VsyncRate);
			_throttle.signal_paused = EmulatorPaused || Global.Emulator is NullEmulator;
			_throttle.signal_unthrottle = _unthrottled || superfastforward;
			_throttle.SetSpeedPercent(fastforward ? Global.Config.SpeedPercentAlternate : Global.Config.SpeedPercent);
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

		private static unsafe Image MakeScreenshotImage()
		{
			var video = Global.Emulator.VideoProvider;
			var image = new Bitmap(video.BufferWidth, video.BufferHeight, PixelFormat.Format32bppArgb);

			// TODO - replace with BitmapBuffer
			var framebuf = video.GetVideoBuffer();
			var bmpdata = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int* ptr = (int*)bmpdata.Scan0.ToPointer();
			int stride = bmpdata.Stride / 4;
			for (int y = 0; y < video.BufferHeight; y++)
			{
				for (int x = 0; x < video.BufferWidth; x++)
				{
					int col = framebuf[(y * video.BufferWidth) + x];

					if (Global.Emulator is TI83)
					{
						if (col == 0)
						{
							col = Color.Black.ToArgb();
						}
						else
						{
							col = Color.White.ToArgb();
						}
					}

					// make opaque
					col |= unchecked((int)0xff000000);

					ptr[(y * stride) + x] = col;
				}
			}

			image.UnlockBits(bmpdata);
			return image;
		}

		private void SaveStateAs()
		{
			if (Global.Emulator is NullEmulator)
			{
				return;
			}

			var sfd = new SaveFileDialog();
			var path = PathManager.GetSaveStatePath(Global.Game);
			sfd.InitialDirectory = path;
			sfd.FileName = PathManager.SaveStatePrefix(Global.Game) + "." + "QuickSave0.State";
			var file = new FileInfo(path);
			if (file.Directory != null && file.Directory.Exists == false)
			{
				file.Directory.Create();
			}

			var result = sfd.ShowHawkDialog();
			if (result == DialogResult.OK)
			{
				SaveStateFile(sfd.FileName, sfd.FileName, false);
			}
		}

		private void LoadStateAs()
		{
			if (Global.Emulator is NullEmulator)
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

			LoadStateFile(ofd.FileName, Path.GetFileName(ofd.FileName));
		}

		private static void SaveSlotSelectedMessage()
		{
			GlobalWin.OSD.AddMessage("Slot " + Global.Config.SaveSlot + " selected.");
		}

		private static VideoPluginSettings N64GenerateVideoSettings(GameInfo game, bool hasmovie)
		{
			var pluginToUse = String.Empty;

			if (hasmovie && Global.MovieSession.Movie.Header[HeaderKeys.PLATFORM] == "N64" && Global.MovieSession.Movie.Header.ContainsKey(HeaderKeys.VIDEOPLUGIN))
			{
				pluginToUse = Global.MovieSession.Movie.Header[HeaderKeys.VIDEOPLUGIN];
			}

			if (pluginToUse == string.Empty || (pluginToUse != "Rice" && pluginToUse != "Glide64"))
			{
				pluginToUse = Global.Config.N64VidPlugin;
			}

			var video_settings = new VideoPluginSettings(pluginToUse, Global.Config.N64VideoSizeX, Global.Config.N64VideoSizeY);

			if (pluginToUse == "Rice")
			{
				Global.Config.RicePlugin.FillPerGameHacks(game);
				video_settings.Parameters = Global.Config.RicePlugin.GetPluginSettings();
			}
			else if (pluginToUse == "Glide64")
			{
				Global.Config.GlidePlugin.FillPerGameHacks(game);
				video_settings.Parameters = Global.Config.GlidePlugin.GetPluginSettings();
			}
			else if (pluginToUse == "Glide64mk2")
			{
				Global.Config.Glide64mk2Plugin.FillPerGameHacks(game);
				video_settings.Parameters = Global.Config.Glide64mk2Plugin.GetPluginSettings();
			}

			if (hasmovie && Global.MovieSession.Movie.Header[HeaderKeys.PLATFORM] == "N64" && Global.MovieSession.Movie.Header.ContainsKey(HeaderKeys.VIDEOPLUGIN))
			{
				var settings = new List<string>(video_settings.Parameters.Keys);
				foreach (var setting in settings)
				{
					if (Global.MovieSession.Movie.Header.ContainsKey(setting))
					{
						var Value = Global.MovieSession.Movie.Header[setting];
						if (video_settings.Parameters[setting] is bool)
						{
							try
							{
								video_settings.Parameters[setting] = bool.Parse(Value);
							}
							catch { }
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

		private void Render()
		{
			var video = Global.Emulator.VideoProvider;
			if (video.BufferHeight != _lastHeight || video.BufferWidth != _lastWidth)
			{
				_lastWidth = video.BufferWidth;
				_lastHeight = video.BufferHeight;
				FrameBufferResized();
			}

			GlobalWin.DisplayManager.UpdateSource(Global.Emulator.VideoProvider);
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

			var str = sb.ToString().Replace("%ARCH%", "*.zip;*.rar;*.7z");
			str = str.Replace(";", "; ");
			return str;
		}

		private void OpenRom()
		{
			var ofd = new OpenFileDialog { InitialDirectory = PathManager.GetRomsPath(Global.Emulator.SystemId) };

			// adelikat: ugly design for this, I know
			if (VersionInfo.INTERIM)
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

		/// <summary>
		/// send core settings to emu, setting reboot flag if needed
		/// </summary>
		/// <param name="o"></param>
		private void PutCoreSettings(object o)
		{
			if (Global.Emulator.PutSettings(o))
				FlagNeedsReboot();
		}

		/// <summary>
		/// send core sync settings to emu, setting reboot flag if needed
		/// </summary>
		/// <param name="o"></param>
		private void PutCoreSyncSettings(object o)
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				GlobalWin.OSD.AddMessage("Attempt to change sync-relevant setings while recording BLOCKED.");
			}
			else
			{
				if (Global.Emulator.PutSyncSettings(o))
					FlagNeedsReboot();
			}
		}

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

			if (Global.Config.ShowLogWindow)
			{
				LogConsole.SaveConfigSettings();
			}

			ConfigService.Save(PathManager.DefaultIniPath, Global.Config);
		}

		private void PreviousSlot()
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

		private void NextSlot()
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

		private static void ToggleReadOnly()
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

			Slot1StatusButton.ForeColor = _stateSlots.HasSlot(1) ? Color.Black : Color.Gray;
			Slot2StatusButton.ForeColor = _stateSlots.HasSlot(2) ? Color.Black : Color.Gray;
			Slot3StatusButton.ForeColor = _stateSlots.HasSlot(3) ? Color.Black : Color.Gray;
			Slot4StatusButton.ForeColor = _stateSlots.HasSlot(4) ? Color.Black : Color.Gray;
			Slot5StatusButton.ForeColor = _stateSlots.HasSlot(5) ? Color.Black : Color.Gray;
			Slot6StatusButton.ForeColor = _stateSlots.HasSlot(6) ? Color.Black : Color.Gray;
			Slot7StatusButton.ForeColor = _stateSlots.HasSlot(7) ? Color.Black : Color.Gray;
			Slot8StatusButton.ForeColor = _stateSlots.HasSlot(8) ? Color.Black : Color.Gray;
			Slot9StatusButton.ForeColor = _stateSlots.HasSlot(9) ? Color.Black : Color.Gray;
			Slot0StatusButton.ForeColor = _stateSlots.HasSlot(0) ? Color.Black : Color.Gray;

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

		private Bitmap CaptureOSD() // sort of like MakeScreenShot(), but with OSD and LUA captured as well.  slow and bad.
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

			GlobalWin.DisplayManager.UpdateSourceEx(Global.Emulator.VideoProvider, captureosd_srp);
			return (Bitmap)captureosd_rvp.GetBitmap().Clone();
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

		private static void SaveMovie()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				Global.MovieSession.Movie.Save();
				GlobalWin.OSD.AddMessage(Global.MovieSession.Movie.Filename + " saved.");
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

		#endregion

		#region Frame Loop

		private void StepRunLoop_Throttle()
		{
			SyncThrottle();
			_throttle.signal_frameAdvance = _runloopFrameadvance;
			_throttle.signal_continuousframeAdvancing = _runloopFrameProgress;

			_throttle.Step(true, -1);
		}

		private void StepRunLoop_Core()
		{
			bool runFrame = false;
			_runloopFrameadvance = false;
			DateTime now = DateTime.Now;
			bool suppressCaptureRewind = false;

			double frameAdvanceTimestampDelta = (now - this._frameAdvanceTimestamp).TotalMilliseconds;
			bool frameProgressTimeElapsed = Global.Config.FrameProgressDelayMs < frameAdvanceTimestampDelta;

			if (Global.Config.SkipLagFrame && Global.Emulator.IsLagFrame && frameProgressTimeElapsed)
			{
				runFrame = true;
			}

			if (Global.ClientControls["Frame Advance"] || PressFrameAdvance)
			{
				//handle the initial trigger of a frame advance
				if (this._frameAdvanceTimestamp == DateTime.MinValue)
				{
					PauseEmulator();
					runFrame = true;
					_runloopFrameadvance = true;
					this._frameAdvanceTimestamp = now;
				}
				else
				{
					//handle the timed transition from countdown to FrameProgress
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
				//handle release of frame advance: do we need to deactivate FrameProgress?
				if (_runloopFrameProgress)
				{
					_runloopFrameProgress = false;
					PauseEmulator();
				}
				this._frameAdvanceTimestamp = DateTime.MinValue;
			}

			if (!EmulatorPaused)
			{
				runFrame = true;
			}

			bool ReturnToRecording = Global.MovieSession.Movie.IsRecording;
			if (RewindActive && (Global.ClientControls["Rewind"] || PressRewind))
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
				bool ff = Global.ClientControls["Fast Forward"] || Global.ClientControls["Turbo"];
				bool fff = Global.ClientControls["Turbo"];
				bool updateFpsString = (_runloopLastFf != ff);
				_runloopLastFf = ff;

				//client input-related duties
				GlobalWin.OSD.ClearGUIText();

				if (!fff)
				{
					GlobalWin.Tools.UpdateToolsBefore();
				}

				Global.ClickyVirtualPadController.FrameTick();

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
					string fps_string = _runloopLastFps + " fps";
					if (fff)
					{
						fps_string += " >>>>";
					}
					else if (ff)
					{
						fps_string += " >>";
					}
					GlobalWin.OSD.FPS = fps_string;
				}

				if (!suppressCaptureRewind && RewindActive) CaptureRewindState();

				if (!_runloopFrameadvance) genSound = true;
				else if (!Global.Config.MuteFrameAdvance)
					genSound = true;

				Global.MovieSession.HandleMovieOnFrameLoop();

				coreskipaudio = Global.ClientControls["Turbo"] && _currAviWriter == null;
				//=======================================
				Global.CheatList.Pulse();
				Global.Emulator.FrameAdvance(!_throttle.skipnextframe || _currAviWriter != null, !coreskipaudio);
				GlobalWin.DisplayManager.NeedsToPaint = true;
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
				GlobalWin.Sound.UpdateSound();
			}
			else
				GlobalWin.Sound.UpdateSilence();
		}

		#endregion

		#region AVI Stuff

		/// <summary>
		/// start avi recording, unattended
		/// </summary>
		/// <param name="videowritername">match the short name of an ivideowriter</param>
		/// <param name="filename">filename to save to</param>
		private void RecordAVI(string videowritername, string filename)
		{
			_RecordAVI(videowritername, filename, true);
		}

		/// <summary>
		/// start avi recording, asking user for filename and options
		/// </summary>
		private void RecordAVI()
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
			if (_currAviWriter != null) return;

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
				aw = VideoWriterChooserForm.DoVideoWriterChoserDlg(video_writers, GlobalWin.MainForm, out _avwriterResizew, out _avwriterResizeh);
			}

			foreach (var w in video_writers)
			{
				if (w != aw)
					w.Dispose();
			}

			if (aw == null)
			{
				if (unattended)
					GlobalWin.OSD.AddMessage(string.Format("Couldn't start video writer \"{0}\"", videowritername));
				else
					GlobalWin.OSD.AddMessage("A/V capture canceled.");
				return;
			}

			try
			{
				aw.SetMovieParameters(Global.Emulator.CoreComm.VsyncNum, Global.Emulator.CoreComm.VsyncDen);
				if (_avwriterResizew > 0 && _avwriterResizeh > 0)
					aw.SetVideoParameters(_avwriterResizew, _avwriterResizeh);
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
					var sfd = new SaveFileDialog();
					if (!(Global.Emulator is NullEmulator))
					{
						sfd.FileName = PathManager.FilesystemSafeName(Global.Game);
						sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.AvPathFragment, null);
					}
					else
					{
						sfd.FileName = "NULL";
						sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.AvPathFragment, null);
					}
					sfd.Filter = String.Format("{0} (*.{0})|*.{0}|All Files|*.*", aw.DesiredExtension());

					var result = sfd.ShowHawkDialog();
					if (result == DialogResult.Cancel)
					{
						aw.Dispose();
						return;
					}
					aw.OpenFile(sfd.FileName);
				}

				//commit the avi writing last, in case there were any errors earlier
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

			// do sound rewire.  the plan is to eventually have AVI writing support syncsound input, but it doesn't for the moment
			if (!Global.Emulator.StartAsyncSound())
				_aviSoundInput = new MetaspuAsync(Global.Emulator.SyncSoundProvider, ESynchMethod.ESynchMethod_V);
			else
				_aviSoundInput = Global.Emulator.SoundProvider;
			_dumpProxy = new MetaspuSoundProvider(ESynchMethod.ESynchMethod_V);
			_soundRemainder = 0;
			RewireSound();
		}

		private void AbortAVI()
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
			_soundRemainder = 0;
			RewireSound();
		}

		private void StopAVI()
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
			_soundRemainder = 0;
			RewireSound();
		}

		private void AVIFrameAdvance()
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;
			if (_currAviWriter != null)
			{
				long nsampnum = 44100 * (long)Global.Emulator.CoreComm.VsyncDen + _soundRemainder;
				long nsamp = nsampnum / Global.Emulator.CoreComm.VsyncNum;
				// exactly remember fractional parts of an audio sample
				_soundRemainder = nsampnum % Global.Emulator.CoreComm.VsyncNum;

				short[] temp = new short[nsamp * 2];
				_aviSoundInput.GetSamples(temp);
				_dumpProxy.buffer.enqueue_samples(temp, (int)nsamp);

				try
				{
					IVideoProvider output;
					if (_avwriterResizew > 0 && _avwriterResizeh > 0)
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
						Bitmap bmpout = new Bitmap(_avwriterResizew, _avwriterResizeh, PixelFormat.Format32bppArgb);
						using (Graphics g = Graphics.FromImage(bmpout))
							g.DrawImage(bmpin, new Rectangle(0, 0, bmpout.Width, bmpout.Height));
						bmpin.Dispose();
						output = new BmpVideoProvder(bmpout);
					}
					else
					{
						if (Global.Config.AVI_CaptureOSD)
							output = new BmpVideoProvder(CaptureOSD());
						else
							output = Global.Emulator.VideoProvider;
					}

					_currAviWriter.AddFrame(output);
					if (output is BmpVideoProvder)
						(output as BmpVideoProvder).Dispose();

					_currAviWriter.AddSamples(temp);
				}
				catch (Exception e)
				{
					MessageBox.Show("Video dumping died:\n\n" + e);
					AbortAVI();
				}

				if (_autoDumpLength > 0)
				{
					_autoDumpLength--;
					if (_autoDumpLength == 0) // finish
					{
						StopAVI();
						if (autoCloseOnDump)
						{
							_exit = true;
						}
					}
				}
				GlobalWin.DisplayManager.NeedsToPaint = true;
			}
		}

		#endregion

		#region Scheduled for refactor

		private void ShowMessageCoreComm(string message)
		{
			MessageBox.Show(this, message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		private static void RewireInputChain() // Move to Client.Common
		{
			GlobalWin.ControllerInputCoalescer = new ControllerInputCoalescer { Type = Global.ActiveController.Type };

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

			// connect the movie session before MovieOutputHardpoint if it is doing anything
			// otherwise connect the MovieInputSourceAdapter to it, effectively bypassing the movie session
			if (Global.MovieSession.Movie != null)
			{
				Global.MovieOutputHardpoint.Source = Global.MovieSession.MovieControllerAdapter;
			}
			else
			{
				Global.MovieOutputHardpoint.Source = Global.MovieInputSourceAdapter;
			}
		}

		private static void SyncControls() // Move to client.comon
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

		public bool LoadRom(string path, bool deterministicemulation = false, bool hasmovie = false) // Move to client.common
		{
			if (path == null)
			{
				return false;
			}

			using (var file = new HawkFile())
			{
				var romExtensions = new[] { "SMS", "SMC", "SFC", "PCE", "SGX", "GG", "SG", "BIN", "GEN", "MD", "SMD", "GB", "NES", "FDS", "ROM", "INT", "GBC", "UNF", "A78", "CRT", "COL", "XML", "Z64", "V64", "N64" };

				// lets not use this unless we need to
				// file.NonArchiveExtensions = romExtensions;
				file.Open(path);

				// if the provided file doesnt even exist, give up!
				if (!file.Exists)
				{
					return false;
				}

				// try binding normal rom extensions first
				if (!file.IsBound)
				{
					file.BindSoleItemOf(romExtensions);
				}

				// if we have an archive and need to bind something, then pop the dialog
				if (file.IsArchive && !file.IsBound)
				{
					var ac = new ArchiveChooser(file);
					if (ac.ShowDialog(this) == DialogResult.OK)
					{
						file.BindArchiveMember(ac.SelectedMemberIndex);
					}
					else
					{
						return false;
					}
				}

				IEmulator nextEmulator = null;
				RomGame rom = null;
				GameInfo game = null;
				var nextComm = new CoreComm(ShowMessageCoreComm);
				CoreFileProvider.SyncCoreCommInputSignals(nextComm);

				try
				{
					var ext = file.Extension.ToLower();
					if (ext == ".iso" || ext == ".cue")
					{
						var disc = ext == ".iso" ? Disc.FromIsoPath(path) : Disc.FromCuePath(path, new CueBinPrefs());
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
								case DiscType.MegaCD:
									game.System = "GEN";
									break;
								case DiscType.TurboCD:
								case DiscType.UnknownCDFS:
								case DiscType.UnknownFormat:
								default: // PCECD was bizhawk's first CD core,
									// and during that time, all CDs were blindly sent to it
									// so this prevents regressions
									game.System = "PCECD";
									break;
							}
						}

						switch (game.System)
						{
							case "GEN":
								{
									var genesis = new GPGX(
										nextComm, null, disc, "GEN", true, GPGX.ControlType.Normal);
									nextEmulator = genesis;
								}
								break;
							case "SAT":
								{
									var saturn = new Yabause(nextComm, disc, Global.Config.SaturnUseGL);
									nextEmulator = saturn;
									SaturnSetPrefs(saturn);
								}
								break;
							case "PSP":
								{
									var psp = new PSP(nextComm, file.Name);
									nextEmulator = psp;
								}
								break;
							case "PSX":
								{
									var psx = new Octoshock(nextComm);
									nextEmulator = psx;
									psx.LoadCuePath(file.CanonicalFullPath);
									nextEmulator.CoreComm.RomStatusDetails = "PSX etc.";
								}
								break;
							case "PCE":
							case "PCECD":
								{
									var biosPath = Global.FirmwareManager.Request("PCECD", "Bios");
									if (File.Exists(biosPath) == false)
									{
										MessageBox.Show("PCE-CD System Card not found. Please check the BIOS path in Config->Paths->PC Engine.");
										return false;
									}

									rom = new RomGame(new HawkFile(biosPath));

									if (rom.GameInfo.Status == RomStatus.BadDump)
									{
										MessageBox.Show(
											"The PCE-CD System Card you have selected is known to be a bad dump. This may cause problems playing PCE-CD games.\n\n"
											+ "It is recommended that you find a good dump of the system card. Sorry to be the bearer of bad news!");
									}
									else if (rom.GameInfo.NotInDatabase)
									{
										MessageBox.Show(
											"The PCE-CD System Card you have selected is not recognized in our database. That might mean it's a bad dump, or isn't the correct rom.");
									}
									else if (rom.GameInfo["BIOS"] == false)
									{
										MessageBox.Show(
											"The PCE-CD System Card you have selected is not a BIOS image. You may have selected the wrong rom.");
									}

									if (rom.GameInfo["SuperSysCard"])
									{
										game.AddOption("SuperSysCard");
									}

									if (game["NeedSuperSysCard"] && game["SuperSysCard"] == false)
									{
										MessageBox.Show(
											"This game requires a version 3.0 System card and won't run with the system card you've selected. Try selecting a 3.0 System Card in Config->Paths->PC Engine.");
									}

									game.FirmwareHash = Util.BytesToHexString(System.Security.Cryptography.SHA1.Create().ComputeHash(rom.RomData));
									nextEmulator = new PCEngine(nextComm, game, disc, rom.RomData, Global.Config.GetCoreSettings<PCEngine>());
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

									var gbl = new GambatteLink(nextComm, L, XMLG.Assets["LeftRom"], R, XMLG.Assets["RightRom"],
										Global.Config.GetCoreSettings<GambatteLink>(),
										Global.Config.GetCoreSyncSettings<GambatteLink>());
									nextEmulator = gbl;

									// other stuff todo
									break;

								default:
									return false;
							}
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.ToString(), "XMLGame Load Error");
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

						switch (game.System)
						{
							case "SNES":
								{
									game.System = "SNES";
									nextComm.SNES_ExePath = SNES_Prepare(Global.Config.SNESProfile);

									// need to get rid of this hack at some point
									((CoreFileProvider)nextComm.CoreFileProvider).SubfileDirectory = Path.GetDirectoryName(path.Replace("|", string.Empty)); //Dirty hack to get around archive filenames (since we are just getting the directory path, it is safe to mangle the filename

									var snes = new LibsnesCore(nextComm);
									nextEmulator = snes;
									byte[] romData = isXml ? null : rom.FileData;
									byte[] xmlData = isXml ? rom.FileData : null;
									snes.Load(game, romData, deterministicemulation, xmlData);
								}
								break;
							case "SMS":
							case "SG":
								if (Global.Config.SmsEnableFM)
								{
									game.AddOption("UseFM");
								}

								if (Global.Config.SmsAllowOverlock)
								{
									game.AddOption("AllowOverclock");
								}

								if (Global.Config.SmsForceStereoSeparation)
								{
									game.AddOption("ForceStereo");
								}

								if (Global.Config.SmsSpriteLimit)
								{
									game.AddOption("SpriteLimit");
								}

								nextEmulator = new SMS(nextComm, game, rom.RomData);
								break;
							case "GG":
								if (Global.Config.SmsAllowOverlock)
								{
									game.AddOption("AllowOverclock");
								}

								if (Global.Config.SmsSpriteLimit)
								{
									game.AddOption("SpriteLimit");
								}

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
								nextEmulator = new PCEngine(nextComm, game, rom.RomData, Global.Config.GetCoreSettings<PCEngine>());
								break;
							case "GEN":
								{
									// nextEmulator = new Genesis(nextComm, game, rom.RomData);
									nextEmulator = new GPGX(nextComm, rom.RomData, null, "GEN", true, GPGX.ControlType.Normal);
									break;
								}
							case "TI83":
								nextEmulator = new TI83(nextComm, game, rom.RomData);
								if (Global.Config.TI83autoloadKeyPad)
								{
									GlobalWin.Tools.Load<TI83KeyPad>();
								}
								break;
							case "NES":
									nextEmulator = new NES(nextComm, game, rom.FileData,
										Global.Config.GetCoreSettings<NES>(),
										Global.MovieSession.Movie.Header.BoardProperties);
								break;
							case "GB":
							case "GBC":
								if (!Global.Config.GB_AsSGB)
								{
									var gb = new Gameboy(nextComm, game, rom.FileData,
										Global.Config.GetCoreSettings<Gameboy>(),
										Global.Config.GetCoreSyncSettings<Gameboy>());
									nextEmulator = gb;
								}
								else
								{
									try
									{
										game.System = "SNES";
										game.AddOption("SGB");
										nextComm.SNES_ExePath = SNES_Prepare(Global.Config.SNESProfile);
										var snes = new LibsnesCore(nextComm);
										nextEmulator = snes;
										snes.Load(game, rom.FileData, deterministicemulation, null);
									}
									catch
									{
										// failed to load SGB bios.  to avoid catch-22, disable SGB mode
										ShowMessageCoreComm("Failed to load a GB rom in SGB mode.  Disabling SGB Mode.");
										Global.Config.GB_AsSGB = false;
										throw;
									}
								}
								break;
							case "Coleco":
								{
									var c = new ColecoVision(nextComm, game, rom.RomData, Global.Config.ColecoSkipBiosIntro);
									nextEmulator = c;
								}
								break;
							case "INTV":
								{
									var intv = new Intellivision(nextComm, game, rom.RomData);
									nextEmulator = intv;
								}
								break;
							case "A78":
								var gamedbpath = Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "EMU7800.csv");
								var a78 = new Atari7800(nextComm, game, rom.RomData, gamedbpath);
								nextEmulator = a78;
								break;
							case "C64":
								C64 c64 = new C64(nextComm, game, rom.RomData, rom.Extension);
								c64.HardReset();
								nextEmulator = c64;
								break;
							case "GBA":
								if (VersionInfo.INTERIM)
								{
									GBA gba = new GBA(nextComm);
									// var gba = new GarboDev.GbaManager(nextComm);
									gba.Load(rom.RomData);
									nextEmulator = gba;
								}
								break;
							case "N64":
								Global.Game = game;
								var video_settings = N64GenerateVideoSettings(game, hasmovie);
								int SaveType = 0;
								if (game.OptionValue("SaveType") == "EEPROM_16K")
								{
									SaveType = 1;
								}
								nextEmulator = new N64(nextComm, game, rom.RomData, video_settings, SaveType);
								break;

							case "DEBUG":
								if (VersionInfo.INTERIM)
								{
									nextEmulator = LibRetroEmulator.CreateDebug(nextComm, rom.RomData);
								}
								break;
						}
					}

					if (nextEmulator == null)
					{
						throw new Exception("No core could load the rom.");
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("Exception during loadgame:\n\n" + ex);
					return false;
				}

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
					var nes = Global.Emulator as NES;
					if (nes != null && nes.GameName != null)
					{
						Global.Game.Name = nes.GameName;
					}

					Global.Game.Status = nes.RomStatus;
				}

				Text = DisplayNameForSystem(game.System) + " - " + game.Name;
				ResetRewindBuffer();

				if (Global.Emulator.CoreComm.RomStatusDetails == null && rom != null)
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

				// restarts the lua console if a different rom is loaded.
				// im not really a fan of how this is done..
				if (Global.Config.RecentRoms.Empty || Global.Config.RecentRoms[0] != file.CanonicalFullPath)
				{
					GlobalWin.Tools.Restart<LuaConsole>();
				}

				Global.Config.RecentRoms.Add(file.CanonicalFullPath);
				JumpLists.AddRecentItem(file.CanonicalFullPath);
				if (File.Exists(PathManager.SaveRamPath(game)))
				{
					LoadSaveRam();
				}

				if (Global.Config.AutoSavestates)
				{
					LoadState("Auto");
				}

				GlobalWin.Tools.Restart();
				Cheats_Restart();

				if (Global.Config.LoadCheatFileByGame)
				{
					if (Global.CheatList.AttemptToLoadCheatFile())
					{
						GlobalWin.OSD.AddMessage("Cheats file loaded");
					}
				}

				CurrentlyOpenRom = file.CanonicalFullPath;
				HandlePlatformMenus();
				_stateSlots.Clear();
				UpdateStatusSlots();
				UpdateDumpIcon();

				CaptureRewindState();

				Global.StickyXORAdapter.ClearStickies();
				Global.StickyXORAdapter.ClearStickyFloats();
				Global.AutofireStickyXORAdapter.ClearStickies();

				RewireSound();
				ToolHelpers.UpdateCheatRelatedTools(null, null);
				return true;
			}
		}

		public void SaveState(string name) // Move to client.common
		{
			if (Global.Emulator is NullEmulator)
			{
				return;
			}

			var path = PathManager.SaveStatePrefix(Global.Game) + "." + name + ".State";

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
				file.CopyTo(backup);
			}

			SaveStateFile(path, name, false);

			if (GlobalWin.Tools.Has<LuaConsole>())
			{
				GlobalWin.Tools.LuaConsole.LuaImp.CallSaveStateEvent(name);
			}
		}

		public void SaveStateFile(string filename, string name, bool fromLua)  // Move to client.common
		{
			SavestateManager.SaveStateFile(filename, name);

			GlobalWin.OSD.AddMessage("Saved state: " + name);

			if (!fromLua)
			{
				UpdateStatusSlots();
			}
		}

		public void LoadStateFile(string path, string name, bool fromLua = false) // Move to client.commo
		{
			GlobalWin.DisplayManager.NeedsToPaint = true;

			if (SavestateManager.LoadStateFile(path, name))
			{
				SetMainformMovieInfo();
				GlobalWin.OSD.ClearGUIText();
				GlobalWin.Tools.UpdateToolsBefore(fromLua);
				UpdateToolsAfter(fromLua);
				UpdateToolsLoadstate();
				GlobalWin.OSD.AddMessage("Loaded state: " + name);

				if (GlobalWin.Tools.Has<LuaConsole>())
				{
					GlobalWin.Tools.LuaConsole.LuaImp.CallLoadStateEvent(name);
				}
			}
			else
			{
				GlobalWin.OSD.AddMessage("Loadstate error!");
			}
		}

		public void LoadState(string name, bool fromLua = false) // Move to client.commo
		{
			if (Global.Emulator is NullEmulator)
			{
				return;
			}

			var path = PathManager.SaveStatePrefix(Global.Game) + "." + name + ".State";
			if (File.Exists(path) == false)
			{
				GlobalWin.OSD.AddMessage("Unable to load " + name + ".State");
				return;
			}

			LoadStateFile(path, name, fromLua);
		}

		// whats the difference between these two methods??
		// its very tricky. rename to be more clear or combine them.
		private void CloseGame(bool clearSram = false)
		{
			if (Global.Config.AutoSavestates && Global.Emulator is NullEmulator == false)
			{
				SaveState("Auto");
			}

			if (clearSram)
			{
				var path = PathManager.SaveRamPath(Global.Game);
				if (File.Exists(path))
				{
					File.Delete(path);
					GlobalWin.OSD.AddMessage("SRAM cleared.");
				}
			}
			else if (Global.Emulator.SaveRamModified)
			{
				SaveRam();
			}

			StopAVI();

			{
				// save settings object
				Type t = Global.Emulator.GetType();
				Global.Config.PutCoreSettings(Global.Emulator.GetSettings(), t);
				// don't trample config with loaded-from-movie settings
				if (!Global.MovieSession.Movie.IsActive)
					Global.Config.PutCoreSyncSettings(Global.Emulator.GetSyncSettings(), t);
			}

			Global.Emulator.Dispose();
			Global.CoreComm = new CoreComm(ShowMessageCoreComm);
			CoreFileProvider.SyncCoreCommInputSignals();
			Global.Emulator = new NullEmulator(Global.CoreComm);
			Global.ActiveController = Global.NullControls;
			Global.AutoFireController = Global.AutofireNullControls;

			// adelikat: TODO: Ugly hack! But I don't know a way around this yet.
			if (!(Global.MovieSession.Movie is TasMovie))
			{
				Global.MovieSession.Movie.Stop();
			}

			RebootStatusBarIcon.Visible = false;
		}

		public void CloseRom(bool clearSram = false)
		{
			if (GlobalWin.Tools.AskSave())
			{
				CloseGame(clearSram);
				Global.CoreComm = new CoreComm(ShowMessageCoreComm);
				CoreFileProvider.SyncCoreCommInputSignals();
				Global.Emulator = new NullEmulator(Global.CoreComm);
				Global.Game = GameInfo.GetNullGame();

				GlobalWin.Tools.Restart();

				RewireSound();
				ResetRewindBuffer();
				Cheats_Restart();
				Text = "BizHawk" + (VersionInfo.INTERIM ? " (interim) " : String.Empty);
				HandlePlatformMenus();
				_stateSlots.Clear();
				UpdateDumpIcon();
				ToolHelpers.UpdateCheatRelatedTools(null, null);
			}
		}

		private void SwapBackupSavestate(string path) // Move inside Saveslot Manager
		{
			// Takes the .state and .bak files and swaps them
			var state = new FileInfo(path);
			var backup = new FileInfo(path + ".bak");
			var temp = new FileInfo(path + ".bak.tmp");

			if (!state.Exists || !backup.Exists)
			{
				return;
			}

			if (temp.Exists)
			{
				temp.Delete();
			}

			backup.CopyTo(path + ".bak.tmp");
			backup.Delete();
			state.CopyTo(path + ".bak");
			state.Delete();
			temp.CopyTo(path);
			temp.Delete();

			_stateSlots.ToggleRedo(Global.Config.SaveSlot);
		}

		private static void ProcessMovieImport(string fn) // Nothing Winform Specific here, move to Movie import
		{
			var d = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null);
			string errorMsg;
			string warningMsg;
			var m = MovieImport.ImportFile(fn, out errorMsg, out warningMsg);
			
			if (!String.IsNullOrWhiteSpace(errorMsg))
			{
				MessageBox.Show(errorMsg, "Conversion error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			if (!String.IsNullOrWhiteSpace(warningMsg))
			{
				GlobalWin.OSD.AddMessage(warningMsg);
			}
			else
			{
				GlobalWin.OSD.AddMessage(Path.GetFileName(fn) + " imported as " + "Movies\\" +
				                         Path.GetFileName(fn) + "." + Global.Config.MovieExtension);
			}

			if (!Directory.Exists(d))
			{
				Directory.CreateDirectory(d);
			}

			var outPath = Path.Combine(d, Path.GetFileName(fn) + "." + Global.Config.MovieExtension);
			m.SaveAs(outPath);
		}

		#endregion
	}
}
