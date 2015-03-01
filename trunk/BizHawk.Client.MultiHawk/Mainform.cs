using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Bizware.BizwareGL;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Client.Common;
using BizHawk.Client.MultiHawk.ToolExtensions;

namespace BizHawk.Client.MultiHawk
{
	public partial class Mainform : Form
	{
		static Mainform()
		{
			// If this isnt here, then our assemblyresolving hacks wont work due to the check for MainForm.INTERIM
			// its.. weird. dont ask.
		}

		public Mainform(string[] args)
		{
			InitializeComponent();
			_throttle = new Throttle();
			_inputManager = new InputManager(this);
			Global.Config = ConfigService.Load<Config>(PathManager.DefaultIniPath);
			Global.Config.TargetZoomFactor = 1; // TODO: hardcode to 1 for now but eventually let user configure this
			Global.Config.DispFixAspectRatio = false;
			Global.Config.ResolveDefaults();
			GlobalWin.MainForm = this;

			Global.ControllerInputCoalescer = new ControllerInputCoalescer();
			Global.FirmwareManager = new FirmwareManager();
			Global.MovieSession = new MovieSession
			{
				Movie = MovieService.DefaultInstance,
				MovieControllerAdapter = MovieService.DefaultInstance.LogGeneratorInstance().MovieControllerAdapter,
				MessageCallback = AddMessage,

				// TODO
				//AskYesNoCallback = StateErrorAskUser,
				PauseCallback = PauseEmulator,
				ModeChangedCallback = SetMainformMovieInfo
			};

			new AutoResetEvent(false);
			// TODO
			//Icon = Properties.Resources.logo;
			Global.Game = GameInfo.NullInstance;

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

			Database.LoadDatabase(Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "gamedb.txt"));

			Input.Initialize(this.Handle);
			InitControls();

			// TODO
			//CoreFileProvider.SyncCoreCommInputSignals();

			Global.ActiveController = new Controller(NullEmulator.NullController);
			Global.AutoFireController = Global.AutofireNullControls;
			Global.AutofireStickyXORAdapter.SetOnOffPatternFromConfig();

			Closing += (o, e) =>
			{
				foreach (var ew in EmulatorWindows.ToList())
				{
					ew.ShutDown();
				}

				SaveConfig();
			};
		}

		private void Mainform_Load(object sender, EventArgs e)
		{
			SetMainformMovieInfo();

			if (Global.Config.RecentRoms.AutoLoad)
			{
				LoadRomFromRecent(Global.Config.RecentRoms.MostRecent);
			}
		}

		public List<EmulatorWindow> EmulatorWindows = new List<EmulatorWindow>();

		private bool _exit;

		protected override void OnClosed(EventArgs e)
		{
			_exit = true;
			base.OnClosed(e);
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

			ConfigService.Save(PathManager.DefaultIniPath, Global.Config);
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
			Global.AutofireNullControls = new AutofireController(NullEmulator.NullController, Global.Emulator);
		}

		private void OpenRomMenuItem_Click(object sender, EventArgs e)
		{
			var ofd = new OpenFileDialog
			{
				InitialDirectory = PathManager.GetExeDirectoryAbsolute()
			};

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

			var result = ofd.ShowDialog();
			if (result != DialogResult.OK)
			{
				return;
			}

			LoadRom(ofd.FileName);
		}

		private readonly InputManager _inputManager;

		private bool ReloadRom(EmulatorWindow ew)
		{
			bool deterministic = ew.Emulator.DeterministicEmulation;
			string path = ew.CurrentRomPath;

			var loader = new RomLoader
			{
				ChooseArchive = LoadArhiveChooser,
				ChoosePlatform = ChoosePlatformForRom,
				Deterministic = deterministic,
				MessageCallback = AddMessage
			};


			loader.OnLoadError += ShowLoadError;
			loader.OnLoadSettings += CoreSettings;
			loader.OnLoadSyncSettings += CoreSyncSettings;

			var nextComm = new CoreComm(ShowMessageCoreComm, AddMessage);

			var result = loader.LoadRom(path, nextComm);

			if (result)
			{
				ew.SaveRam();
				ew.Emulator.Dispose();
				ew.Emulator = loader.LoadedEmulator;
				ew.CoreComm = nextComm;

				_inputManager.SyncControls();

				if (EmulatorWindows.First() == ew)
				{
					Global.Emulator = ew.Emulator;
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		private string StripArchivePath(string path)
		{
			if (path.Contains("|"))
			{
				return path.Split('|').Last();
			}

			return path;
		}

		private bool LoadRom(string path)
		{
			bool deterministic = true;

			var loader = new RomLoader
			{
				ChooseArchive = LoadArhiveChooser,
				ChoosePlatform = ChoosePlatformForRom,
				Deterministic = deterministic,
				MessageCallback = AddMessage
			};


			loader.OnLoadError += ShowLoadError;
			loader.OnLoadSettings += CoreSettings;
			loader.OnLoadSyncSettings += CoreSyncSettings;

			var nextComm = new CoreComm(ShowMessageCoreComm, AddMessage);

			var result = loader.LoadRom(path, nextComm);

			if (result)
			{
				var ew = new EmulatorWindow(this)
				{
					TopLevel = false,
					Text = Path.GetFileNameWithoutExtension(StripArchivePath(path)),
					Emulator = loader.LoadedEmulator,

					GL = new Bizware.BizwareGL.Drivers.OpenTK.IGL_TK(),
					GLManager = new GLManager(),
					Game = loader.Game,
					CurrentRomPath = path
				};

				nextComm.RequestGLContext = () => ew.GLManager.CreateGLContext();
				nextComm.ActivateGLContext = (gl) => ew.GLManager.Activate((GLManager.ContextRef)ew.GL);
				nextComm.DeactivateGLContext = () => ew.GLManager.Deactivate();

				ew.CoreComm = nextComm;
				ew.Init();

				EmulatorWindows.Add(ew);

				_inputManager.SyncControls();

				WorkspacePanel.Controls.Add(ew);
				ew.Show();

				Global.Config.RecentRoms.Add(path);

				if (EmulatorWindows.Count == 1)
				{
					Global.Emulator = ew.Emulator;
				}

				return true;
			}
			else
			{
				return false;
			}
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

				// TODO
				// modals that need to capture input for binding purposes get input, of course
				if (ActiveForm is HotkeyConfig
					|| ActiveForm is ControllerConfig
					//|| ActiveForm is TAStudio
					//|| ActiveForm is VirtualpadTool
				)
				{
					return true;
				}

				//// if no form is active on this process, then the background input setting applies
				//if (ActiveForm == null && Global.Config.AcceptBackgroundInput)
				//{
				//	return true;
				//}

				return false;
			}
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

		public void AddMessage(string message)
		{
			StatusBarMessageLabel.Text = message;
		}

		private string ChoosePlatformForRom(RomGame rom)
		{
			// TODO
			return null;
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

		private void ShowLoadError(object sender, RomLoader.RomErrorArgs e)
		{
			string title = "load error";
			if (e.AttemptedCoreLoad != null)
			{
				title = e.AttemptedCoreLoad + " load error";
			}

			MessageBox.Show(this, e.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private static void CoreSettings(object sender, RomLoader.SettingsLoadArgs e)
		{
			e.Settings = Global.Config.GetCoreSettings(e.Core);
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

		public void FlagNeedsReboot()
		{
			// TODO
		}

		private void ShowMessageCoreComm(string message)
		{
			MessageBox.Show(this, message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		private static void CheckMessages()
		{
			Application.DoEvents();
			if (ActiveForm != null)
			{
				ScreenSaver.ResetTimerPeriodically();
			}
		}

		// sends an alt+mnemonic combination
		private void SendAltKeyChar(char c)
		{
			typeof(ToolStrip).InvokeMember("ProcessMnemonicInternal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Instance, null, MainformMenu, new object[] { c });
		}

		// sends a simulation of a plain alt key keystroke
		private void SendPlainAltKey(int lparam)
		{
			var m = new Message { WParam = new IntPtr(0xF100), LParam = new IntPtr(lparam), Msg = 0x0112, HWnd = Handle };
			base.WndProc(ref m);
		}

		public void ProcessInput()
		{
			ControllerInputCoalescer conInput = Global.ControllerInputCoalescer as ControllerInputCoalescer;

			for (; ; )
			{

				// loop through all available events
				var ie = Input.Instance.DequeueEvent();
				if (ie == null) { break; }

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
				}

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
			}
		}

		public void ProgramRunLoop()
		{			
			CheckMessages();

			for (; ; )
			{
				Input.Instance.Update();

				// handle events and dispatch as a hotkey action, or a hotkey button, or an input button
				ProcessInput();
				Global.ClientControls.LatchFromPhysical(GlobalWin.HotkeyCoalescer);

				Global.ActiveController.LatchFromPhysical(Global.ControllerInputCoalescer);

				// TODO
				//Global.ActiveController.ApplyAxisConstraints(
				//	(Global.Emulator is N64 && Global.Config.N64UseCircularAnalogConstraint) ? "Natural Circle" : null);

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

				if (EmulatorWindows.Any())
				{
					StepRunLoop_Core();
					StepRunLoop_Throttle();

					foreach (var window in EmulatorWindows)
					{
						window.Render();
					}
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

		private void Shutdown()
		{
			//TODO
			//if (_currAviWriter != null)
			//{
			//	_currAviWriter.CloseFile();
			//	_currAviWriter = null;
			//}
		}

		public void LoadQuickSave(string quickSlotName, bool fromLua = false)
		{
			foreach (var window in EmulatorWindows)
			{
				window.LoadQuickSave(quickSlotName);
			}
		}

		public void SaveQuickSave(string quickSlotName)
		{
			foreach (var window in EmulatorWindows)
			{
				window.SaveQuickSave(quickSlotName);
			}
		}

		private void SelectSlot(int num)
		{
			Global.Config.SaveSlot = num;
			UpdateStatusSlots();
		}

		private void UpdateStatusSlots()
		{
			// TODO
			SaveSlotSelectedMessage();
		}

		private void SaveSlotSelectedMessage()
		{
			AddMessage("Slot " + Global.Config.SaveSlot + " selected.");
		}

		public void TogglePause()
		{
			EmulatorPaused ^= true;
			//SetPauseStatusbarIcon(); // TODO
		}
		private bool CheckHotkey(string trigger)
		{
			switch (trigger)
			{
				default:
					return false;

				case "Pause":
					TogglePause();
					break;
				case "Reboot Core":
					RebootCoresMenuItem_Click(null, null);
					break;
				case "Quick Load":
					LoadQuickSave("QuickSave" + Global.Config.SaveSlot);
					break;
				case "Quick Save":
					SaveQuickSave("QuickSave" + Global.Config.SaveSlot);
					break;

				// Save States
				case "Save State 0":
					SaveQuickSave("QuickSave0");
					Global.Config.SaveSlot = 0;
					UpdateStatusSlots();
					break;
				case "Save State 1":
					SaveQuickSave("QuickSave1");
					Global.Config.SaveSlot = 1;
					UpdateStatusSlots();
					break;
				case "Save State 2":
					SaveQuickSave("QuickSave2");
					Global.Config.SaveSlot = 2;
					UpdateStatusSlots();
					break;
				case "Save State 3":
					SaveQuickSave("QuickSave3");
					Global.Config.SaveSlot = 3;
					UpdateStatusSlots();
					break;
				case "Save State 4":
					SaveQuickSave("QuickSave4");
					Global.Config.SaveSlot = 4;
					UpdateStatusSlots();
					break;
				case "Save State 5":
					SaveQuickSave("QuickSave5");
					Global.Config.SaveSlot = 5;
					UpdateStatusSlots();
					break;
				case "Save State 6":
					SaveQuickSave("QuickSave6");
					Global.Config.SaveSlot = 6;
					UpdateStatusSlots();
					break;
				case "Save State 7":
					SaveQuickSave("QuickSave7");
					Global.Config.SaveSlot = 7;
					UpdateStatusSlots();
					break;
				case "Save State 8":
					SaveQuickSave("QuickSave8");
					Global.Config.SaveSlot = 8;
					UpdateStatusSlots();
					break;
				case "Save State 9":
					SaveQuickSave("QuickSave9");
					Global.Config.SaveSlot = 9;
					//UpdateStatusSlots();
					break;
				case "Load State 0":
					LoadQuickSave("QuickSave0");
					Global.Config.SaveSlot = 0;
					UpdateStatusSlots();
					break;
				case "Load State 1":
					LoadQuickSave("QuickSave1");
					Global.Config.SaveSlot = 1;
					UpdateStatusSlots();
					break;
				case "Load State 2":
					LoadQuickSave("QuickSave2");
					Global.Config.SaveSlot = 2;
					UpdateStatusSlots();
					break;
				case "Load State 3":
					LoadQuickSave("QuickSave3");
					Global.Config.SaveSlot = 3;
					UpdateStatusSlots();
					break;
				case "Load State 4":
					LoadQuickSave("QuickSave4");
					Global.Config.SaveSlot = 4;
					UpdateStatusSlots();
					break;
				case "Load State 5":
					LoadQuickSave("QuickSave5");
					Global.Config.SaveSlot = 5;
					UpdateStatusSlots();
					break;
				case "Load State 6":
					LoadQuickSave("QuickSave6");
					Global.Config.SaveSlot = 6;
					UpdateStatusSlots();
					break;
				case "Load State 7":
					LoadQuickSave("QuickSave7");
					Global.Config.SaveSlot = 7;
					break;
				case "Load State 8":
					LoadQuickSave("QuickSave8");
					Global.Config.SaveSlot = 8;
					UpdateStatusSlots();
					break;
				case "Load State 9":
					LoadQuickSave("QuickSave9");
					Global.Config.SaveSlot = 9;
					UpdateStatusSlots();
					break;

				case "Select State 0":
					SelectSlot(0);
					break;
				case "Select State 1":
					SelectSlot(1);
					break;
				case "Select State 2":
					SelectSlot(2);
					break;
				case "Select State 3":
					SelectSlot(3);
					break;
				case "Select State 4":
					SelectSlot(4);
					break;
				case "Select State 5":
					SelectSlot(5);
					break;
				case "Select State 6":
					SelectSlot(6);
					break;
				case "Select State 7":
					SelectSlot(7);
					break;
				case "Select State 8":
					SelectSlot(8);
					break;
				case "Select State 9":
					SelectSlot(9);
					break;

				// Movie
				case "Stop Movie":
					StopMovieMenuItem_Click(null, null);
					break;
				case "Toggle read-only":
					ToggleReadonlyMenuItem_Click(null, null);
					break;
				// TODO
				//case "Play from beginning":
				//	RestartMovie();
				//	break;
				// TODO
				//case "Save Movie":
				//	SaveMovie();
				//	break;
			}

			return true;
		}

		public bool PressFrameAdvance = false;
		public bool PressRewind = false;
		public bool FastForward = false;
		public bool TurboFastForward = false;
		public bool EmulatorPaused = true;
		private readonly Throttle _throttle;
		private bool _unthrottled;
		private bool _runloopFrameadvance;
		private bool _runloopFrameProgress;
		private long _frameAdvanceTimestamp;
		private bool _runloopLastFf;
		private int _runloopFps;
		private long _runloopSecond;
		private int _runloopLastFps;

		public bool IsTurboing
		{
			get
			{
				return Global.ClientControls["Turbo"];
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

			var fastForward = Global.ClientControls["Fast Forward"] || FastForward;
			var turbo = IsTurboing;

			int speedPercent = fastForward ? Global.Config.SpeedPercentAlternate : Global.Config.SpeedPercent;

			Global.DisableSecondaryThrottling = _unthrottled || turbo || fastForward;

			// realtime throttle is never going to be so exact that using a double here is wrong
			_throttle.SetCoreFps(Global.Emulator.CoreComm.VsyncRate);
			_throttle.signal_paused = EmulatorPaused;
			_throttle.signal_unthrottle = _unthrottled || turbo;
			_throttle.signal_overrideSecondaryThrottle = fastForward && (Global.Config.SoundThrottle || Global.Config.VSyncThrottle || Global.Config.VSync);
			_throttle.SetSpeedPercent(speedPercent);
		}

		private void StepRunLoop_Throttle()
		{
			SyncThrottle();
			_throttle.signal_frameAdvance = _runloopFrameadvance;
			_throttle.signal_continuousframeAdvancing = _runloopFrameProgress;

			_throttle.Step(true, -1);
		}

		public void PauseEmulator()
		{
			EmulatorPaused = true;
			//SetPauseStatusbarIcon(); // TODO
		}

		public void UnpauseEmulator()
		{
			EmulatorPaused = false;
			//SetPauseStatusbarIcon(); // TODO
		}

		private void StepRunLoop_Core()
		{
			var runFrame = false;
			_runloopFrameadvance = false;
			var currentTimestamp = Stopwatch.GetTimestamp();
			var suppressCaptureRewind = false;
			double frameAdvanceTimestampDeltaMs = (double)(currentTimestamp - _frameAdvanceTimestamp) / Stopwatch.Frequency * 1000.0;
			bool frameProgressTimeElapsed = frameAdvanceTimestampDeltaMs >= Global.Config.FrameProgressDelayMs;

			if (Global.ClientControls["Frame Advance"])
			{
				// handle the initial trigger of a frame advance
				if (_frameAdvanceTimestamp == 0)
				{
					PauseEmulator();
					runFrame = true;
					_runloopFrameadvance = true;
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

			if (runFrame)
			{
				var isFastForwarding = Global.ClientControls["Fast Forward"] || IsTurboing;
				var updateFpsString = _runloopLastFf != isFastForwarding;
				_runloopLastFf = isFastForwarding;
				_runloopFps++;

				if ((double)(currentTimestamp - _runloopSecond) / Stopwatch.Frequency >= 1.0)
				{
					_runloopLastFps = _runloopFps;
					_runloopSecond = currentTimestamp;
					_runloopFps = 0;
					updateFpsString = true;
				}

				if (updateFpsString)
				{
					var fps_string = _runloopLastFps + " fps";
					if (IsTurboing)
					{
						fps_string += " >>>>";
					}
					else if (isFastForwarding)
					{
						fps_string += " >>";
					}

					//GlobalWin.OSD.FPS = fps_string; // TODO
				}

				Global.MovieSession.HandleMovieOnFrameLoop();

				foreach (var window in EmulatorWindows)
				{
					window.FrameAdvance();
				}

				PressFrameAdvance = false;

				UpdateAfterFrameChanged();
			}
		}

		private void UpdateAfterFrameChanged()
		{
			if (EmulatorWindows.Any())
			{
				FameStatusBarLabel.Text = EmulatorWindows.First().Emulator.Frame.ToString();
			}
		}

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentRomSubMenu.DropDownItems.Clear();
			RecentRomSubMenu.DropDownItems.AddRange(
				Global.Config.RecentRoms.RecentMenu(LoadRomFromRecent, true));
		}

		private void LoadRomFromRecent(string rom)
		{
			if (!LoadRom(rom))
			{
				Global.Config.RecentRoms.HandleLoadError(rom);
			}
		}

		private void MovieSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			PlayMovieMenuItem.Enabled =
				RecordMovieMenuItem.Enabled =
				EmulatorWindows.Any();

			StopMovieMenuItem.Enabled = Global.MovieSession.Movie.IsActive;
		}

		private void RecordMovieMenuItem_Click(object sender, EventArgs e)
		{
			new RecordMovie().ShowDialog();
		}

		private void PlayMovieMenuItem_Click(object sender, EventArgs e)
		{
			new PlayMovie().ShowDialog();
		}

		private void StopMovieMenuItem_Click(object sender, EventArgs e)
		{
			Global.MovieSession.StopMovie(true);
			SetMainformMovieInfo();
			//UpdateStatusSlots(); // TODO
		}

		private void ToggleReadonlyMenuItem_Click(object sender, EventArgs e)
		{
			Global.MovieSession.ReadOnly ^= true;
			if (Global.MovieSession.ReadOnly)
			{
				AddMessage("Movie is now Read-only");
			}
			else
			{
				AddMessage("Movie is now read+write");
			}
		}

		public void SetMainformMovieInfo()
		{
			if (Global.MovieSession.Movie.IsPlaying)
			{
				PlayRecordStatusButton.Image = Properties.Resources.Play;
				PlayRecordStatusButton.ToolTipText = "Movie is in playback mode";
				PlayRecordStatusButton.Visible = true;
			}
			else if (Global.MovieSession.Movie.IsRecording)
			{
				PlayRecordStatusButton.Image = Properties.Resources.RecordHS;
				PlayRecordStatusButton.ToolTipText = "Movie is in record mode";
				PlayRecordStatusButton.Visible = true;
			}
			else if (!Global.MovieSession.Movie.IsActive)
			{
				PlayRecordStatusButton.Image = Properties.Resources.Blank;
				PlayRecordStatusButton.ToolTipText = "No movie is active";
				PlayRecordStatusButton.Visible = false;
			}
		}

		public bool StartNewMovie(IMovie movie, bool record)
		{
			if (movie.IsActive)
			{
				movie.Save();
			}

			try
			{
				Global.MovieSession.QueueNewMovie(movie, record, Global.Emulator);
			}
			catch (MoviePlatformMismatchException ex)
			{
				MessageBox.Show(this, ex.Message, "Movie/Platform Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			RebootCoresMenuItem_Click(null, null);

			Global.Emulator = EmulatorWindows.First().Emulator;

			if (Global.MovieSession.PreviousNES_InQuickNES.HasValue)
			{
				Global.Config.NES_InQuickNES = Global.MovieSession.PreviousNES_InQuickNES.Value;
				Global.MovieSession.PreviousNES_InQuickNES = null;
			}

			if (Global.MovieSession.PreviousSNES_InSnes9x.HasValue)
			{
				Global.Config.SNES_InSnes9x = Global.MovieSession.PreviousSNES_InSnes9x.Value;
				Global.MovieSession.PreviousSNES_InSnes9x = null;
			}

			Global.Config.RecentMovies.Add(movie.Filename);

			if (Global.Emulator.HasSavestates() && movie.StartsFromSavestate)
			{
				if (movie.TextSavestate != null)
				{
					Global.Emulator.AsStatable().LoadStateText(new StringReader(movie.TextSavestate));
				}
				else
				{
					Global.Emulator.AsStatable().LoadStateBinary(new BinaryReader(new MemoryStream(movie.BinarySavestate, false)));
				}
				if (movie.SavestateFramebuffer != null)
				{
					var b1 = movie.SavestateFramebuffer;
					var b2 = Global.Emulator.VideoProvider().GetVideoBuffer();
					int len = Math.Min(b1.Length, b2.Length);
					for (int i = 0; i < len; i++)
					{
						b2[i] = b1[i];
					}
				}
				Global.Emulator.ResetCounters();
			}

			Global.MovieSession.RunQueuedMovie(record);

			SetMainformMovieInfo();

			return true;
		}

		private void hotkeyConfigToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (new HotkeyConfig().ShowDialog() == DialogResult.OK)
			{
				InitControls();
				_inputManager.SyncControls();
			}
		}

		private void controllerConfigToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var controller = new ControllerConfig(Global.Emulator.ControllerDefinition);
			if (controller.ShowDialog() == DialogResult.OK)
			{
				InitControls();
				_inputManager.SyncControls();
			}
		}

		public void EmulatorWindowClosed(EmulatorWindow ew)
		{
			EmulatorWindows.Remove(ew);
			WorkspacePanel.Controls.Remove(ew);

			if (ew.Emulator == Global.Emulator)
			{
				if (EmulatorWindows.Any())
				{
					Global.Emulator = EmulatorWindows.First().Emulator;
				}
				else
				{
					Global.Emulator = null;
				}
			}
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void saveConfigToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveConfig();
			AddMessage("Saved settings");
		}

		private void RebootCoresMenuItem_Click(object sender, EventArgs e)
		{
			foreach (var ew in EmulatorWindows)
			{
				ReloadRom(ew);
			}

			AddMessage("Rebooted all cores");
		}
	}
}
