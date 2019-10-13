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
using BizHawk.Client.EmuHawk;
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
			GLManager.CreateInstance(GlobalWin.IGL_GL);

			InitializeComponent();
			_throttle = new BizHawk.Client.EmuHawk.Throttle();
			_inputManager = new InputManager(this);
			Global.Config = ConfigService.Load<Config>(PathManager.DefaultIniPath);
			Global.Config.DispFixAspectRatio = false; // TODO: don't hardcode this
			Global.Config.ResolveDefaults();
			GlobalWin.MainForm = this;

			Global.ControllerInputCoalescer = new ControllerInputCoalescer();
			Global.FirmwareManager = new FirmwareManager();
			Global.MovieSession = new MovieSession
			{
				Movie = MovieService.DefaultInstance,
				MovieControllerAdapter = MovieService.DefaultInstance.LogGeneratorInstance().MovieControllerAdapter,
				MessageCallback = AddMessage,

				AskYesNoCallback = StateErrorAskUser,
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
				string xmlPath = Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "NesCarts.xml");
				string x7zPath = Path.Combine(PathManager.GetExeDirectoryAbsolute(), "gamedb", "NesCarts.7z");
				bool loadXml = File.Exists(xmlPath);
				using (var NesCartFile = new HawkFile(loadXml ? xmlPath : x7zPath))
				{
					if (!loadXml) { NesCartFile.BindFirst(); }
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

			Global.ActiveController = new Controller(NullController.Instance.Definition);
			Global.AutoFireController = AutofireNullControls;
			Global.AutofireStickyXORAdapter.SetOnOffPatternFromConfig();

			Closing += (o, e) =>
			{
				Global.MovieSession.Movie.Stop();

				foreach (var ew in EmulatorWindows)
				{
					ew.ShutDown();
				}

				SaveConfig();
			};

			if (Global.Config.MainWndx != -1 && Global.Config.MainWndy != -1 && Global.Config.SaveWindowPosition)
			{
				Location = new Point(Global.Config.MainWndx, Global.Config.MainWndy);
			}
		}

		// TODO: make this an actual property, set it when loading a Rom, and pass it dialogs, etc
		// This is a quick hack to reduce the dependency on Global.Emulator
		public IEmulator Emulator
		{
			get { return Global.Emulator; }
			set { Global.Emulator = value; }
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

		private void Mainform_Load(object sender, EventArgs e)
		{
			SetMainformMovieInfo();

			if (Global.Config.RecentRomSessions.AutoLoad)
			{
				LoadRomSessionFromRecent(Global.Config.RecentRomSessions.MostRecent);

				if (Global.Config.RecentMovies.AutoLoad && !Global.Config.RecentMovies.Empty)
				{
					LoadMoviesFromRecent(Global.Config.RecentMovies.MostRecent);
				}
			}

			if (Global.Config.SaveWindowPosition && Global.Config.MainWidth > 0 && Global.Config.MainHeight > 0)
			{
				this.Size = new Size(Global.Config.MainWidth, Global.Config.MainHeight);
				
			}
		}

		public EmulatorWindowList EmulatorWindows = new EmulatorWindowList();
		private AutofireController AutofireNullControls;
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

			Global.Config.MainWidth = this.Width;
			Global.Config.MainHeight = this.Height;

			ConfigService.Save(PathManager.DefaultIniPath, Global.Config);
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
			AutofireNullControls = new AutofireController(NullController.Instance.Definition, Emulator);
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
					"PSF Playstation Sound File", "*.psf;*.minipsf",
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
					Emulator = ew.Emulator;
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
			nextComm.CoreFileProvider = new CoreFileProvider(s => MessageBox.Show(s));

			var result = loader.LoadRom(path, nextComm);

			if (result)
			{
				var ew = new EmulatorWindow(this)
				{
					TopLevel = false,
					Text = Path.GetFileNameWithoutExtension(StripArchivePath(path)),
					Emulator = loader.LoadedEmulator,
					GL = new Bizware.BizwareGL.Drivers.OpenTK.IGL_TK(2,0,false),
					//GL = new Bizware.BizwareGL.Drivers.SlimDX.IGL_SlimDX9(),
					GLManager = GLManager.Instance,
					Game = loader.Game,
					CurrentRomPath = loader.CanonicalFullPath
				};

				nextComm.ReleaseGLContext = (o) => GlobalWin.GLManager.ReleaseGLContext(o);
				nextComm.RequestGLContext = (major, minor, forward) => GlobalWin.GLManager.CreateGLContext(major, minor, forward);
				nextComm.ActivateGLContext = (gl) => GlobalWin.GLManager.Activate((GLManager.ContextRef)gl);
				nextComm.DeactivateGLContext = () => GlobalWin.GLManager.Deactivate();

				ew.CoreComm = nextComm;
				ew.Init();

				if (EmulatorWindows.Any())
				{
					// Attempt to open the window is a smart location
					var last = EmulatorWindows.Last();

					int x = last.Location.X + last.Width + 5;
					int y = last.Location.Y;
					if (x + (last.Width / 2) > Width) // If it will go too far off screen
					{
						y += last.Height + 5;
						x = EmulatorWindows.First().Location.X;
					}

					ew.Location = new Point(x, y);
				}
				

				EmulatorWindows.Add(ew);

				WorkspacePanel.Controls.Add(ew);
				ew.Show();

				Global.Config.RecentRoms.Add(loader.CanonicalFullPath);

				if (EmulatorWindows.Count == 1)
				{
					Emulator = ew.Emulator;
					ViewSubMenu.Enabled = true;
				}

				_inputManager.SyncControls();

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

				// modals that need to capture input for binding purposes get input, of course
				if (ActiveForm is BizHawk.Client.EmuHawk.HotkeyConfig
					|| ActiveForm is BizHawk.Client.EmuHawk.ControllerConfig
					//|| ActiveForm is TAStudio
					//|| ActiveForm is VirtualpadTool
				)
				{
					return true;
				}

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
			var ac = new BizHawk.Client.EmuHawk.ArchiveChooser(file);
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
				BizHawk.Client.EmuHawk.ScreenSaver.ResetTimerPeriodically();
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
				//	(Emulator is N64 && Global.Config.N64UseCircularAnalogConstraint) ? "Natural Circle" : null);

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

		public void LoadQuickSave(string quickSlotName)
		{
			try
			{
				foreach (var window in EmulatorWindows)
				{
					window.LoadQuickSave(quickSlotName);
				}
			}
			catch
			{
				MessageBox.Show("Could not load " + quickSlotName);
			}
		}

		public void SaveQuickSave(string quickSlotName)
		{
			try
			{
				foreach (var window in EmulatorWindows)
				{
					window.SaveQuickSave(quickSlotName);
				}
			}
			catch
			{
				MessageBox.Show("Could not save " + quickSlotName);
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
			UpdateAfterFrameChanged();
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
		private readonly BizHawk.Client.EmuHawk.Throttle _throttle;
		//private bool _unthrottled; // TODO
		private bool _runloopFrameAdvance;
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

			Global.DisableSecondaryThrottling = /*_unthrottled || TODO */ turbo || fastForward;

			// realtime throttle is never going to be so exact that using a double here is wrong
			_throttle.SetCoreFps(EmulatorWindows.Master.Emulator.VsyncRate());
			_throttle.signal_paused = EmulatorPaused;
			_throttle.signal_unthrottle = /*_unthrottled || TODO */ turbo;
			_throttle.signal_overrideSecondaryThrottle = fastForward && (Global.Config.SoundThrottle || Global.Config.VSyncThrottle || Global.Config.VSync);
			_throttle.SetSpeedPercent(speedPercent);
		}

		private void StepRunLoop_Throttle()
		{
			SyncThrottle();
			_throttle.signal_frameAdvance = _runloopFrameAdvance;
			_throttle.signal_continuousFrameAdvancing = _runloopFrameProgress;

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
			_runloopFrameAdvance = false;
			var currentTimestamp = Stopwatch.GetTimestamp();
			double frameAdvanceTimestampDeltaMs = (double)(currentTimestamp - _frameAdvanceTimestamp) / Stopwatch.Frequency * 1000.0;
			bool frameProgressTimeElapsed = frameAdvanceTimestampDeltaMs >= Global.Config.FrameProgressDelayMs;

			if (Global.ClientControls["Frame Advance"])
			{
				// handle the initial trigger of a frame advance
				if (_frameAdvanceTimestamp == 0)
				{
					PauseEmulator();
					runFrame = true;
					_runloopFrameAdvance = true;
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
				string frame = EmulatorWindows.Master.Emulator.Frame.ToString();

				if (Global.MovieSession.Movie.IsActive)
				{
					if (Global.MovieSession.Movie.IsFinished)
					{
						frame += $" / {Global.MovieSession.Movie.FrameCount} (finished)";
					}
					else if (Global.MovieSession.Movie.IsPlaying)
					{
						frame += $" / {Global.MovieSession.Movie.FrameCount}";
					}
				}

				FameStatusBarLabel.Text = frame;
			}
		}

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveSessionMenuItem.Enabled = !string.IsNullOrWhiteSpace(EmulatorWindows.SessionName);
			SaveSessionAsMenuItem.Enabled = EmulatorWindows.Any();
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

			StopMovieMenuItem.Enabled =
				RestartMovieMenuItem.Enabled =
				Global.MovieSession.Movie.IsActive;
		}

		private void RecordMovieMenuItem_Click(object sender, EventArgs e)
		{
			new RecordMovie(Emulator).ShowDialog();
			UpdateMainText();
			UpdateAfterFrameChanged();
		}

		private void PlayMovieMenuItem_Click(object sender, EventArgs e)
		{
			new PlayMovie().ShowDialog();
			UpdateMainText();
			UpdateAfterFrameChanged();
		}

		private void StopMovieMenuItem_Click(object sender, EventArgs e)
		{
			Global.MovieSession.StopMovie(true);
			SetMainformMovieInfo();
			UpdateMainText();
			UpdateAfterFrameChanged();
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
				Global.MovieSession.QueueNewMovie(movie, record, Emulator);
			}
			catch (MoviePlatformMismatchException ex)
			{
				MessageBox.Show(this, ex.Message, "Movie/Platform Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			RebootCoresMenuItem_Click(null, null);

			Emulator = EmulatorWindows.Master.Emulator;

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

			if (Global.MovieSession.PreviousGBA_UsemGBA.HasValue)
			{
				Global.Config.GBA_UsemGBA = Global.MovieSession.PreviousGBA_UsemGBA.Value;
				Global.MovieSession.PreviousGBA_UsemGBA = null;
			}

			Global.Config.RecentMovies.Add(movie.Filename);

			if (EmulatorWindows.Master.Emulator.HasSavestates() && movie.StartsFromSavestate)
			{
				if (movie.TextSavestate != null)
				{
					EmulatorWindows.Master.Emulator.AsStatable().LoadStateText(new StringReader(movie.TextSavestate));
				}
				else
				{
					EmulatorWindows.Master.Emulator.AsStatable().LoadStateBinary(new BinaryReader(new MemoryStream(movie.BinarySavestate, false)));
				}

				foreach (var ew in EmulatorWindows)
				{
					ew.Emulator.ResetCounters();
				}
			}

			Global.MovieSession.RunQueuedMovie(record);

			SetMainformMovieInfo();
			UpdateAfterFrameChanged();
			return true;
		}

		private void hotkeyConfigToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (new BizHawk.Client.EmuHawk.HotkeyConfig().ShowDialog() == DialogResult.OK)
			{
				InitControls();
				_inputManager.SyncControls();
			}
		}

		private void controllerConfigToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var controller = new BizHawk.Client.EmuHawk.ControllerConfig(EmulatorWindows.Master.Emulator.ControllerDefinition);
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

			if (ew.Emulator == Emulator)
			{
				if (EmulatorWindows.Any())
				{
					Emulator = EmulatorWindows.Master.Emulator;
				}
				else
				{
					Emulator = null;
					ViewSubMenu.Enabled = false;
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

		private void SaveSessionMenuItem_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(EmulatorWindows.SessionName))
			{
				File.WriteAllText(EmulatorWindows.SessionName, EmulatorWindows.SessionJson);
				AddMessage("Session saved.");
			}
		}

		private void SaveSessionAsMenuItem_Click(object sender, EventArgs e)
		{
			if (EmulatorWindows.Any())
			{
				var file = GetSaveFileFromUser();
				if (file != null)
				{
					EmulatorWindows.SessionName = file.FullName;
					Global.Config.RecentRomSessions.Add(file.FullName);
					SaveSessionMenuItem_Click(sender, e);
					UpdateMainText();
				}
			}
		}

		private FileInfo GetSaveFileFromUser()
		{
			var sfd = new SaveFileDialog();
			if (!string.IsNullOrWhiteSpace(EmulatorWindows.SessionName))
			{
				sfd.FileName = Path.GetFileNameWithoutExtension(EmulatorWindows.SessionName);
				sfd.InitialDirectory = Path.GetDirectoryName(EmulatorWindows.SessionName);
			}
			else if (EmulatorWindows.Master != null)
			{
				sfd.FileName = PathManager.FilesystemSafeName(EmulatorWindows.Master.Game);
				sfd.InitialDirectory = PathManager.GetRomsPath("Global");
			}
			else
			{
				sfd.FileName = "NULL";
				sfd.InitialDirectory = PathManager.GetRomsPath("Global");
			}

			sfd.Filter = "Rom Session Files (*.romses)|*.romses|All Files|*.*";
			sfd.RestoreDirectory = true;
			var result = sfd.ShowDialog();
			if (result != DialogResult.OK)
			{
				return null;
			}

			return new FileInfo(sfd.FileName);
		}

		private void OpenSessionMenuItem_Click(object sender, EventArgs e)
		{
			var file = GetFileFromUser("Rom Session Files (*.romses)|*.romses|All Files|*.*");
			if (file != null)
			{
				NewSessionMenuItem_Click(null, null);
				var json = File.ReadAllText(file.FullName);
				EmulatorWindows.SessionName = file.FullName;
				LoadRomSession(EmulatorWindowList.FromJson(json));
				Global.Config.RecentRomSessions.Add(file.FullName);
				UpdateMainText();
			}
		}

		private void UpdateMainText()
		{
			string text = "MultiHawk";

			if (!string.IsNullOrWhiteSpace(EmulatorWindows.SessionName))
			{
				text += " - " + Path.GetFileNameWithoutExtension(EmulatorWindows.SessionName);
			}

			if (Global.MovieSession.Movie.IsActive)
			{
				text += " - " + Path.GetFileNameWithoutExtension(Global.MovieSession.Movie.Filename);
			}

			Text = text;
		}

		private static FileInfo GetFileFromUser(string filter)
		{
			var ofd = new OpenFileDialog
			{
				InitialDirectory = PathManager.GetRomsPath("Global"),
				Filter = filter,
				RestoreDirectory = true
			};

			if (!Directory.Exists(ofd.InitialDirectory))
			{
				Directory.CreateDirectory(ofd.InitialDirectory);
			}

			var result = ofd.ShowDialog();
			return result == DialogResult.OK ? new FileInfo(ofd.FileName) : null;
		}

		private void CloseAllWindows()
		{
			foreach (var ew in EmulatorWindows)
			{
				ew.Close();
			}

			EmulatorWindows.Clear();
		}

		private void NewSessionMenuItem_Click(object sender, EventArgs e)
		{
			foreach (var ew in EmulatorWindows)
			{
				ew.Close();
			}

			EmulatorWindows.Clear();
			UpdateMainText();
		}

		private void LoadRomSession(IEnumerable<EmulatorWindowList.RomSessionEntry> entries)
		{
			foreach (var entry in entries)
			{
				LoadRom(entry.RomName);
				EmulatorWindows.Last().Location = new Point(entry.Wndx, entry.Wndy);
				UpdateMainText();
			}
		}

		private void LoadRomSessionFromRecent(string path)
		{
			var file = new FileInfo(path);
			if (file.Exists)
			{
				NewSessionMenuItem_Click(null, null);
				var json = File.ReadAllText(file.FullName);
				EmulatorWindows.SessionName = file.FullName;
				LoadRomSession(EmulatorWindowList.FromJson(json));
				Global.Config.RecentRomSessions.Add(file.FullName);
				UpdateMainText();
			}
			else
			{
				Global.Config.RecentRomSessions.HandleLoadError(path);
			}
		}

		private void RecentSessionSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSessionSubMenu.DropDownItems.Clear();
			RecentSessionSubMenu.DropDownItems.AddRange(
				Global.Config.RecentRomSessions.RecentMenu(LoadRomSessionFromRecent, autoload: true));
		}

		private void RecentRomSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentRomSubMenu.DropDownItems.Clear();
			RecentRomSubMenu.DropDownItems.AddRange(
				Global.Config.RecentRoms.RecentMenu(LoadRomFromRecent, autoload: false));
		}

		private void ViewSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			_1xMenuItem.Checked = Global.Config.TargetZoomFactors[Emulator.SystemId] == 1;
			_2xMenuItem.Checked = Global.Config.TargetZoomFactors[Emulator.SystemId] == 2;
			_3xMenuItem.Checked = Global.Config.TargetZoomFactors[Emulator.SystemId] == 3;
			_4xMenuItem.Checked = Global.Config.TargetZoomFactors[Emulator.SystemId] == 4;
		}

		private void _1xMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TargetZoomFactors[Emulator.SystemId] = 1;
			ReRenderAllWindows();
		}

		private void _2xMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TargetZoomFactors[Emulator.SystemId] = 2;
			ReRenderAllWindows();
		}

		private void _3xMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TargetZoomFactors[Emulator.SystemId] = 3;
			ReRenderAllWindows();
		}

		private void _4xMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TargetZoomFactors[Emulator.SystemId] = 4;
			ReRenderAllWindows();
		}

		private void ReRenderAllWindows()
		{
			foreach (var ew in EmulatorWindows)
			{
				ew.FrameBufferResized();
				ew.Render();
			}
		}

		private void LoadLastMovieMenuItem_Click(object sender, EventArgs e)
		{
			LoadMoviesFromRecent(Global.Config.RecentMovies.MostRecent);
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			LoadLastMovieContextMenuItem.Visible = !Global.Config.RecentMovies.Empty;
			PlayMovieContextMenuItem.Visible =
				RecordMovieContextMenuItem.Visible =
				!Global.MovieSession.Movie.IsActive;

			StopMovieContextMenuItem.Visible =
				RestartMovieContextMenuItem.Visible =
				Global.MovieSession.Movie.IsActive;

		}

		private void RecentMovieSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentMovieSubMenu.DropDownItems.Clear();
			RecentMovieSubMenu.DropDownItems.AddRange(
				Global.Config.RecentMovies.RecentMenu(LoadMoviesFromRecent, autoload: true));
		}

		private void RestartMovieMenuItem_Click(object sender, EventArgs e)
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				StartNewMovie(Global.MovieSession.Movie, false);
				AddMessage("Replaying movie file in read-only mode");
			}
		}
	}
}
