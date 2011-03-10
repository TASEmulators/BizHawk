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
	public partial class MainForm : Form
	{
		private Control renderTarget;
		private RetainedViewportPanel retainedPanel;
		private string CurrentlyOpenRom;

        //TODO: adelikat: can this be the official file extension?
        public Movie InputLog = new Movie("log.tas", MOVIEMODE.RECORD);   //This movie is always recording while user is playing

		//the currently selected savestate slot
		private int SaveSlot = 0;

		//runloop control
		bool exit;
		bool runloop_frameProgress;
		DateTime FrameAdvanceTimestamp = DateTime.MinValue;
		public bool EmulatorPaused;
		bool runloop_frameadvance;
		Throttle throttle = new Throttle();
		int rewindCredits;

		//For handling automatic pausing when entering the menu
		private bool wasPaused = false;
		private bool didMenuPause = false;

		//tool dialogs
		public RamWatch RamWatch1 = new RamWatch();
		public RamSearch RamSearch1 = new RamSearch();
        public HexEditor HexEditor1 = new HexEditor();
        public NESPPU NESPPU1 = new NESPPU();

		public MainForm(string[] args)
		{
			//in order to allow late construction of this database, we hook up a delegate here to dearchive the data and provide it on demand
			//we could background thread this later instead if we wanted to be real clever
			NES.BootGodDB.GetDatabaseBytes = () => {
				using (HawkFile NesCartFile = new HawkFile("NesCarts.7z").BindFirst())
				    return Util.ReadAllBytes(NesCartFile.GetStream());
			};

			Global.MainForm = this;
			Global.Config = ConfigService.Load<Config>("config.ini");

			if (Global.Direct3D != null)
				renderTarget = new ViewportPanel();
			else renderTarget = retainedPanel = new RetainedViewportPanel();

			renderTarget.Dock = DockStyle.Fill;
			renderTarget.BackColor = Color.Black;
			Controls.Add(renderTarget);

			InitializeComponent();

			Database.LoadDatabase("gamedb.txt");

			if (Global.Direct3D != null)
			{
				Global.RenderPanel = new Direct3DRenderPanel(Global.Direct3D, renderTarget);
			}
			else
			{
				Global.RenderPanel = new SysdrawingRenderPanel(retainedPanel);
			}

			Load += (o, e) =>
			{
				AllowDrop = true;
				DragEnter += FormDragEnter;
				DragDrop += FormDragDrop;
			};

		    Closing += (o, e) =>
		    {
                CloseGame();
                InputLog.StopMovie();
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

			InitControls();
			Global.Emulator = new NullEmulator();
			Global.Sound = new Sound(Handle, Global.DSound);
			Global.Sound.StartSound();

			//TODO - replace this with some kind of standard dictionary-yielding parser in a separate component
			string cmdRom = null;
			string cmdLoadState = null;
			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i].ToLower();
				if (arg.StartsWith("--load-slot="))
					cmdLoadState = arg.Substring(arg.IndexOf('=') + 1);
				else
					cmdRom = arg;
			}

			if (cmdRom != null) //Commandline should always override auto-load
				LoadRom(cmdRom);
			else if (Global.Config.AutoLoadMostRecentRom && !Global.Config.RecentRoms.IsEmpty())
				LoadRomFromRecent(Global.Config.RecentRoms.GetRecentFileByPosition(0));

			if (cmdLoadState != null)
				LoadState("QuickSave" + cmdLoadState);

			if (Global.Config.AutoLoadRamWatch)
				LoadRamWatch();
			if (Global.Config.AutoLoadRamSearch)
				LoadRamSearch();
            if (Global.Config.AutoLoadHexEditor)
                LoadHexEditor();
            if (Global.Config.AutoLoadNESPPU && Global.Emulator is NES)
                LoadNESPPU();

			if (Global.Config.MainWndx >= 0 && Global.Config.MainWndy >= 0 && Global.Config.SaveWindowPosition)
				this.Location = new Point(Global.Config.MainWndx, Global.Config.MainWndy);

			if (Global.Config.StartPaused)
				PauseEmulator();
		}

		void SetSpeedPercent(int value)
		{
			Global.Config.SpeedPercent = value;
			throttle.SetSpeedPercent(value);
            Global.RenderPanel.AddMessage(value + "% Speed");
		}

		protected override void OnClosed(EventArgs e)
		{
			exit = true;
			base.OnClosed(e);
		}

		public void ProgramRunLoop()
		{
			for (; ; )
			{
				
                Input.Update();
				CheckHotkeys();
                
				StepRunLoop_Core();
				if(!IsNullEmulator())
					StepRunLoop_Throttle();
				
				Render();

				CheckMessages();
				if (exit)
					break;
				Thread.Sleep(0);
			}
		}

		void CheckMessages()
		{
			Application.DoEvents();
			if (ActiveForm != null)
				ScreenSaver.ResetTimerPeriodically();
		}

		private void PauseEmulator()
		{
			EmulatorPaused = true;
		}

		private void UnpauseEmulator()
		{
			EmulatorPaused = false;
		}

		private void LoadRomFromRecent(string rom)
		{
			bool r = LoadRom(rom);
			if (!r)
			{
				Global.Sound.StopSound();
				DialogResult result = MessageBox.Show("Could not open " + rom + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
				if (result == DialogResult.Yes)
					Global.Config.RecentRoms.Remove(rom);
				Global.Sound.StartSound();
			}
		}

		public static ControllerDefinition ClientControlsDef = new ControllerDefinition
		{
			Name = "Emulator Frontend Controls",
			BoolButtons = { "Fast Forward", "Rewind", "Hard Reset", "Mode Flip", "Quick Save State", "Quick Load State", "Save Named State", "Load Named State", 
                "Emulator Pause", "Frame Advance", "Screenshot", "Toggle Fullscreen", "SelectSlot0", "SelectSlot1", "SelectSlot2", "SelectSlot3", "SelectSlot4",
                "SelectSlot5", "SelectSlot6", "SelectSlot7", "SelectSlot8", "SelectSlot9", "SaveSlot0", "SaveSlot1", "SaveSlot2", "SaveSlot3", "SaveSlot4",
                "SaveSlot5","SaveSlot6","SaveSlot7","SaveSlot8","SaveSlot9","LoadSlot0","LoadSlot1","LoadSlot2","LoadSlot3","LoadSlot4","LoadSlot5","LoadSlot6",
                "LoadSlot7","LoadSlot8","LoadSlot9"}
		};

		private void InitControls()
		{
			Input.Initialize();
			var controls = new Controller(ClientControlsDef);
			controls.BindMulti("Fast Forward", Global.Config.FastForwardBinding);
			controls.BindMulti("Rewind", Global.Config.RewindBinding);
			controls.BindMulti("Hard Reset", Global.Config.HardResetBinding);
			controls.BindMulti("Emulator Pause", Global.Config.EmulatorPauseBinding);
			controls.BindMulti("Frame Advance", Global.Config.FrameAdvanceBinding);
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
			Global.ClientControls = controls;

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

			var pceControls = new Controller(PCEngine.PCEngineController);
			for (int i = 0; i < 1 /*TODO*/; i++)
			{
				pceControls.BindMulti("Up", Global.Config.PCEController[i].Up);
				pceControls.BindMulti("Down", Global.Config.PCEController[i].Down);
				pceControls.BindMulti("Left", Global.Config.PCEController[i].Left);
				pceControls.BindMulti("Right", Global.Config.PCEController[i].Right);

				pceControls.BindMulti("II", Global.Config.PCEController[i].II);
				pceControls.BindMulti("I", Global.Config.PCEController[i].I);
				pceControls.BindMulti("Select", Global.Config.PCEController[i].Select);
				pceControls.BindMulti("Run", Global.Config.PCEController[i].Run);
			}
			Global.PCEControls = pceControls;

            var nesControls = new Controller(NES.NESController);
            for (int i = 0; i < 1 /*TODO*/; i++)
            {
                nesControls.BindMulti("Up", Global.Config.NESController[i].Up);
                nesControls.BindMulti("Down", Global.Config.NESController[i].Down);
                nesControls.BindMulti("Left", Global.Config.NESController[i].Left);
                nesControls.BindMulti("Right", Global.Config.NESController[i].Right);
                nesControls.BindMulti("A", Global.Config.NESController[i].A);
                nesControls.BindMulti("B", Global.Config.NESController[i].B);
                nesControls.BindMulti("Select", Global.Config.NESController[i].Select);
                nesControls.BindMulti("Start", Global.Config.NESController[i].Start);
            }
            Global.NESControls = nesControls;

			var genControls = new Controller(Genesis.GenesisController);
			genControls.BindMulti("P1 Up", Global.Config.GenP1Up);
			genControls.BindMulti("P1 Left", Global.Config.GenP1Left);
			genControls.BindMulti("P1 Right", Global.Config.GenP1Right);
			genControls.BindMulti("P1 Down", Global.Config.GenP1Down);
			genControls.BindMulti("P1 A", Global.Config.GenP1A);
			genControls.BindMulti("P1 B", Global.Config.GenP1B);
			genControls.BindMulti("P1 C", Global.Config.GenP1C);
			genControls.BindMulti("P1 Start", Global.Config.GenP1Start);
			Global.GenControls = genControls;

			var TI83Controls = new Controller(TI83.TI83Controller);
			TI83Controls.BindMulti("0", "D0"); //numpad 4,8,6,2 (up/down/left/right) dont work in slimdx!! wtf!!
			TI83Controls.BindMulti("1", "D1");
			TI83Controls.BindMulti("2", "D2");
			TI83Controls.BindMulti("3", "D3");
			TI83Controls.BindMulti("4", "D4");
			TI83Controls.BindMulti("5", "D5");
			TI83Controls.BindMulti("6", "D6");
			TI83Controls.BindMulti("7", "D7");
			TI83Controls.BindMulti("8", "D8");
			TI83Controls.BindMulti("9", "D9");
			TI83Controls.BindMulti("ON", "Space");
			TI83Controls.BindMulti("ENTER", "NumberPadEnter");
			TI83Controls.BindMulti("DOWN", "DownArrow");
			TI83Controls.BindMulti("LEFT", "LeftArrow");
			TI83Controls.BindMulti("RIGHT", "RightArrow");
			TI83Controls.BindMulti("UP", "UpArrow");
			TI83Controls.BindMulti("PLUS", "NumberPadPlus");
			TI83Controls.BindMulti("MINUS", "NumberPadMinus");
			TI83Controls.BindMulti("MULTIPLY", "NumberPadStar");
			TI83Controls.BindMulti("DIVIDE", "NumberPadSlash");
			TI83Controls.BindMulti("CLEAR", "Escape");
			TI83Controls.BindMulti("DOT", "NumberPadPeriod");
			Global.TI83Controls = TI83Controls;


		}

		private static void FormDragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void FormDragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
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
			switch (system)
			{
				case "SG": return "SG-1000";
				case "SMS": return "Sega Master System";
				case "GG": return "Game Gear";
				case "PCE": return "TurboGrafx-16";
				case "SGX": return "SuperGrafx";
				case "GEN": return "Genesis";
				case "TI83": return "TI-83";
				case "NES": return "NES";
				case "GB": return "Game Boy";
			}
			return "";
		}

        private void HandlePlatformMenus(string system)
        {
            switch (system)
            {
                case "NES":
                    NESToolStripMenuItem.Visible = true;
                    break;
                default:
                    NESToolStripMenuItem.Visible = false;
                    break;
            }
        }

		private bool LoadRom(string path)
		{
			using (var file = new HawkFile(path))
			{
				//if the provided file doesnt even exist, give up!
				if (!file.Exists) return false;

				//try binding normal rom extensions first
				if (!file.IsBound)
					file.BindSoleItemOf("SMS", "PCE", "SGX", "GG", "SG", "BIN", "SMD", "GB", "NES");

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

				var game = new RomGame(file);
				IEmulator nextEmulator = null;

				switch (game.System)
				{
					case "SG":
					case "SMS":
						nextEmulator = new SMS();
						nextEmulator.Controller = Global.SMSControls;
						if (Global.Config.SmsEnableFM) game.AddOptions("UseFM");
						if (Global.Config.SmsAllowOverlock) game.AddOptions("AllowOverclock");
						if (Global.Config.SmsForceStereoSeparation) game.AddOptions("ForceStereo");
						break;
					case "GG":
						nextEmulator = new SMS { IsGameGear = true };
						nextEmulator.Controller = Global.SMSControls;
						if (Global.Config.SmsAllowOverlock) game.AddOptions("AllowOverclock");
						break;
					case "PCE":
						nextEmulator = new PCEngine(NecSystemType.TurboGrafx);
						nextEmulator.Controller = Global.PCEControls;
						break;
					case "SGX":
						nextEmulator = new PCEngine(NecSystemType.SuperGrafx);
						nextEmulator.Controller = Global.PCEControls;
						break;
					case "GEN":
						nextEmulator = new Genesis(false);//TODO
						nextEmulator.Controller = Global.GenControls;
						break;
					case "TI83":
						nextEmulator = new TI83();
						nextEmulator.Controller = Global.TI83Controls;
						break;
					case "NES":
						nextEmulator = new NES();
						nextEmulator.Controller = Global.NESControls;
						break;
					case "GB":
						nextEmulator = new Gameboy();
						break;
				}

				if (nextEmulator == null)
				{
					throw new Exception();
				}

				try
				{
					nextEmulator.LoadGame(game);
				}
				catch(Exception ex)
				{
					MessageBox.Show("Exception during loadgame:\n\n" + ex.ToString());
					return false;
				}

				CloseGame();
				Global.Emulator = nextEmulator;
				Global.Game = game;

				if (game.System == "NES")
				{
					Global.Game.Name = (Global.Emulator as NES).GameName;
				}

				Text = DisplayNameForSystem(game.System) + " - " + game.Name;
				ResetRewindBuffer();
				Global.Config.RecentRoms.Add(file.CanonicalName);
				if (File.Exists(game.SaveRamPath))
					LoadSaveRam();

				if (game.System == "GB")
				{
					new BizHawk.Emulation.Consoles.Gameboy.Debugger(Global.Emulator as Gameboy).Show();
				}
		

				if (InputLog.GetMovieMode() == MOVIEMODE.RECORD)
					InputLog.StartNewRecording(); //TODO: Uncomment and check for a user movie selected?
				else if (InputLog.GetMovieMode() == MOVIEMODE.PLAY)
				{
					InputLog.LoadMovie();   //TODO: Debug
					InputLog.StartPlayback(); //TODO: Debug
				}

				//setup the throttle based on platform's specifications
				//(one day later for some systems we will need to modify it at runtime as the display mode changes)
				{
					object o = Global.Emulator.Query(EmulatorQuery.VsyncRate);
					if (o is double)
						throttle.SetCoreFps((double)o);
					else throttle.SetCoreFps(60);
					SetSpeedPercent(Global.Config.SpeedPercent);
				}
				RamSearch1.Restart();
				HexEditor1.Restart();
                NESPPU1.Restart();
				CurrentlyOpenRom = path;
                HandlePlatformMenus(Global.Game.System);
				return true;
			}
		}

		private void LoadSaveRam()
		{
			using (var reader = new BinaryReader(new FileStream(Global.Game.SaveRamPath, FileMode.Open, FileAccess.Read)))
				reader.Read(Global.Emulator.SaveRam, 0, Global.Emulator.SaveRam.Length);
		}

		private void CloseGame()
		{
			if (Global.Emulator.SaveRamModified)
			{
				string path = Global.Game.SaveRamPath;

				var f = new FileInfo(path);
				if (f.Directory.Exists == false)
					f.Directory.Create();

				var writer = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write));
				int len = Util.SaveRamBytesUsed(Global.Emulator.SaveRam);
				writer.Write(Global.Emulator.SaveRam, 0, len);
				writer.Close();
			}
			Global.Emulator = new NullEmulator();
		}

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("User32.dll", CharSet = CharSet.Auto)]
		public static extern bool PeekMessage(out Message msg, IntPtr hWnd, UInt32 msgFilterMin, UInt32 msgFilterMax, UInt32 flags);

		public void CheckHotkeys()
		{
			if (Global.ClientControls["Quick Save State"])
			{
				if (!IsNullEmulator())
					SaveState("QuickSave" + SaveSlot.ToString());
				Global.ClientControls.UnpressButton("Quick Save State");
			}

			if (Global.ClientControls["Quick Load State"])
			{
				if (!IsNullEmulator())
					LoadState("QuickSave" + SaveSlot.ToString());
				Global.ClientControls.UnpressButton("Quick Load State");
			}

			//the pause hotkey is ignored when we are frame advancing
			if (!Global.ClientControls.IsPressed("Frame Advance"))
			{
				if (Global.ClientControls["Emulator Pause"])
				{
					Global.ClientControls.UnpressButton("Emulator Pause");
					if (EmulatorPaused)
						UnpauseEmulator();
					else
						PauseEmulator();
				}
			}

			if (Global.ClientControls["Hard Reset"])
			{
				Global.ClientControls.UnpressButton("Hard Reset");
				LoadRom(CurrentlyOpenRom);
			}

			if (Global.ClientControls["Screenshot"])
			{
				Global.ClientControls.UnpressButton("Screenshot");
				TakeScreenshot();
			}

			for (int i = 0; i < 10; i++)
			{
				if (Global.ClientControls["SaveSlot" + i.ToString()])
				{
					if (!IsNullEmulator())
						SaveState("QuickSave" + i.ToString());
					Global.ClientControls.UnpressButton("SaveSlot" + i.ToString());
				}
			}
			for (int i = 0; i < 10; i++)
			{
				if (Global.ClientControls["LoadSlot" + i.ToString()])
				{
					if (!IsNullEmulator())
						LoadState("QuickSave" + i.ToString());
					Global.ClientControls.UnpressButton("LoadSlot" + i.ToString());
				}
			}
			for (int i = 0; i < 10; i++)
			{
				if (Global.ClientControls["SelectSlot" + i.ToString()])
				{
					SaveSlot = i;
					SaveSlotSelectedMessage();
					Global.ClientControls.UnpressButton("SelectSlot" + i.ToString());
				}
			}

			if (Global.ClientControls["Toggle Fullscreen"])
			{
				Global.ClientControls.UnpressButton("Toggle Fullscreen");
				ToggleFullscreen();
			}
		}

		void StepRunLoop_Throttle()
		{
			throttle.signal_fastForward = Global.ClientControls["Fast Forward"];
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

			if (Global.ClientControls["Frame Advance"])
			{
				//handle the initial trigger of a frame advance
				if (FrameAdvanceTimestamp == DateTime.MinValue)
				{
					if (!EmulatorPaused) PauseEmulator();
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

			if (Global.Config.RewindEnabled && Global.ClientControls["Rewind"])
			{
				rewindCredits += Global.Config.SpeedPercent;
				int rewindTodo = rewindCredits / 100;
				if (rewindTodo >= 1)
				{
					rewindCredits -= 100 * rewindTodo;
					Rewind(rewindTodo);
					suppressCaptureRewind = true;
					runFrame = true;
				}
				else
					runFrame = false;
			}
			else rewindCredits = 0;

			bool genSound = false;
			if (runFrame)
			{
				if(!suppressCaptureRewind && Global.Config.RewindEnabled) CaptureRewindState();
				if (!runloop_frameadvance) genSound = true;
				else if (!Global.Config.MuteFrameAdvance)
					genSound = true;
                if (InputLog.GetMovieMode() == MOVIEMODE.PLAY)
                    Global.Emulator.SetControllersAsMnemonic(InputLog.GetInputFrame(Global.Emulator.Frame)+ 1);
				Global.Emulator.FrameAdvance(!throttle.skipnextframe);
				RamWatch1.UpdateValues();
				RamSearch1.UpdateValues();
                HexEditor1.UpdateValues();
                NESPPU1.UpdateValues();
                if (InputLog.GetMovieMode() ==  MOVIEMODE.RECORD)
                    InputLog.GetMnemonic();
			}

			if(genSound)
				Global.Sound.UpdateSound(Global.Emulator.SoundProvider);
			else
				Global.Sound.UpdateSound(new NullEmulator()); //generates silence

		}

		private void TakeScreenshot()
		{
			var video = Global.Emulator.VideoProvider;
			var image = new Bitmap(video.BufferWidth, video.BufferHeight, PixelFormat.Format32bppArgb);

			for (int y = 0; y < video.BufferHeight; y++)
				for (int x = 0; x < video.BufferWidth; x++)
					image.SetPixel(x, y, Color.FromArgb(video.GetVideoBuffer()[(y * video.BufferWidth) + x]));

			var f = new FileInfo(String.Format(Global.Game.ScreenshotPrefix + ".{0:yyyy-MM-dd HH.mm.ss}.png", DateTime.Now));
			if (f.Directory.Exists == false)
				f.Directory.Create();

			Global.RenderPanel.AddMessage(f.Name + " saved.");

			image.Save(f.FullName, ImageFormat.Png);
		}

		private void SaveState(string name)
		{
			string path = Global.Game.SaveStatePrefix + "." + name + ".State";

			var file = new FileInfo(path);
			if (file.Directory.Exists == false)
				file.Directory.Create();

			var writer = new StreamWriter(path);
			Global.Emulator.SaveStateText(writer);
			writer.Close();
			Global.RenderPanel.AddMessage("Saved state: " + name);
		}

		private void LoadState(string name)
		{
			string path = Global.Game.SaveStatePrefix + "." + name + ".State";
			if (File.Exists(path) == false)
				return;

			var reader = new StreamReader(path);
			Global.Emulator.LoadStateText(reader);
			reader.Close();
			Global.RenderPanel.AddMessage("Loaded state: " + name);
		}

	
		private void SaveSlotSelectedMessage()
		{
			Global.RenderPanel.AddMessage("Slot " + SaveSlot + " selected.");
		}

		private void emulationToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			powerToolStripMenuItem.Enabled = !IsNullEmulator();
			resetToolStripMenuItem.Enabled = Global.Emulator.ControllerDefinition.BoolButtons.Contains("Reset");

			enableFMChipToolStripMenuItem.Checked = Global.Config.SmsEnableFM;
			overclockWhenKnownSafeToolStripMenuItem.Checked = Global.Config.SmsAllowOverlock;
			forceStereoSeparationToolStripMenuItem.Checked = Global.Config.SmsForceStereoSeparation;
			pauseToolStripMenuItem.Checked = EmulatorPaused;
			if (didMenuPause) pauseToolStripMenuItem.Checked = wasPaused;
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

		private void fileToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (IsNullEmulator())
			{
				closeROMToolStripMenuItem.Enabled = false;
				screenshotF12ToolStripMenuItem.Enabled = false;
				saveToCurrentSlotToolStripMenuItem.Enabled = false;
				loadCurrentSlotToolStripMenuItem.Enabled = false;
				loadNamedStateToolStripMenuItem.Enabled = false;
				saveNamedStateToolStripMenuItem.Enabled = false;
				savestate1toolStripMenuItem.Enabled = false;
				savestate2toolStripMenuItem.Enabled = false;
				savestate3toolStripMenuItem.Enabled = false;
				savestate4toolStripMenuItem.Enabled = false;
				savestate5toolStripMenuItem.Enabled = false;
				savestate6toolStripMenuItem.Enabled = false;
				savestate7toolStripMenuItem.Enabled = false;
				savestate8toolStripMenuItem.Enabled = false;
				savestate9toolStripMenuItem.Enabled = false;
				savestate0toolStripMenuItem.Enabled = false;
				loadstate1toolStripMenuItem.Enabled = false;
				loadstate2toolStripMenuItem.Enabled = false;
				loadstate3toolStripMenuItem.Enabled = false;
				loadstate4toolStripMenuItem.Enabled = false;
				loadstate5toolStripMenuItem.Enabled = false;
				loadstate6toolStripMenuItem.Enabled = false;
				loadstate7toolStripMenuItem.Enabled = false;
				loadstate8toolStripMenuItem.Enabled = false;
				loadstate9toolStripMenuItem.Enabled = false;
				loadstate0toolStripMenuItem.Enabled = false;

			}
			else
			{
				closeROMToolStripMenuItem.Enabled = true;
				screenshotF12ToolStripMenuItem.Enabled = true;
				saveToCurrentSlotToolStripMenuItem.Enabled = true;
				loadCurrentSlotToolStripMenuItem.Enabled = true;
				loadNamedStateToolStripMenuItem.Enabled = true;
				saveNamedStateToolStripMenuItem.Enabled = true;
				savestate1toolStripMenuItem.Enabled = true;
				savestate2toolStripMenuItem.Enabled = true;
				savestate3toolStripMenuItem.Enabled = true;
				savestate4toolStripMenuItem.Enabled = true;
				savestate5toolStripMenuItem.Enabled = true;
				savestate6toolStripMenuItem.Enabled = true;
				savestate7toolStripMenuItem.Enabled = true;
				savestate8toolStripMenuItem.Enabled = true;
				savestate9toolStripMenuItem.Enabled = true;
				savestate0toolStripMenuItem.Enabled = true;
				loadstate1toolStripMenuItem.Enabled = true;
				loadstate2toolStripMenuItem.Enabled = true;
				loadstate3toolStripMenuItem.Enabled = true;
				loadstate4toolStripMenuItem.Enabled = true;
				loadstate5toolStripMenuItem.Enabled = true;
				loadstate6toolStripMenuItem.Enabled = true;
				loadstate7toolStripMenuItem.Enabled = true;
				loadstate8toolStripMenuItem.Enabled = true;
				loadstate9toolStripMenuItem.Enabled = true;
				loadstate0toolStripMenuItem.Enabled = true;
			}

			selectSlot10ToolStripMenuItem.Checked = false;
			selectSlot1ToolStripMenuItem.Checked = false;
			selectSlot2ToolStripMenuItem.Checked = false;
			selectSlot3ToolStripMenuItem.Checked = false;
			selectSlot4ToolStripMenuItem.Checked = false;
			selectSlot5ToolStripMenuItem.Checked = false;
			selectSlot6ToolStripMenuItem.Checked = false;
			selectSlot7ToolStripMenuItem.Checked = false;
			selectSlot8ToolStripMenuItem.Checked = false;
			selectSlot9ToolStripMenuItem.Checked = false;

			selectSlot1ToolStripMenuItem.Checked = false;
			switch (SaveSlot)
			{
				case 0:
					selectSlot10ToolStripMenuItem.Checked = true;
					break;
				case 1:
					selectSlot1ToolStripMenuItem.Checked = true;
					break;
				case 2:
					selectSlot2ToolStripMenuItem.Checked = true;
					break;
				case 3:
					selectSlot3ToolStripMenuItem.Checked = true;
					break;
				case 4:
					selectSlot4ToolStripMenuItem.Checked = true;
					break;
				case 5:
					selectSlot5ToolStripMenuItem.Checked = true;
					break;
				case 6:
					selectSlot6ToolStripMenuItem.Checked = true;
					break;
				case 7:
					selectSlot7ToolStripMenuItem.Checked = true;
					break;
				case 8:
					selectSlot8ToolStripMenuItem.Checked = true;
					break;
				case 9:
					selectSlot9ToolStripMenuItem.Checked = true;
					break;
				default:
					break;
			}

			if (Global.Config.AutoLoadMostRecentRom == true)
				autoloadMostRecentToolStripMenuItem.Checked = true;
			else
				autoloadMostRecentToolStripMenuItem.Checked = false;
		}

		private void recentROMToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			//Clear out recent Roms list
			//repopulate it with an up to date list
			recentROMToolStripMenuItem.DropDownItems.Clear();

			if (Global.Config.RecentRoms.IsEmpty())
			{
				recentROMToolStripMenuItem.DropDownItems.Add("None");
			}
			else
			{
				for (int x = 0; x < Global.Config.RecentRoms.Length(); x++)
				{
					string path = Global.Config.RecentRoms.GetRecentFileByPosition(x);
					var item = new ToolStripMenuItem();
					item.Text = path;
					item.Click += (o, ev) => LoadRomFromRecent(path);
					recentROMToolStripMenuItem.DropDownItems.Add(item);
				}
			}

			recentROMToolStripMenuItem.DropDownItems.Add("-");

			var clearitem = new ToolStripMenuItem();
			clearitem.Text = "&Clear";
			clearitem.Click += (o, ev) => Global.Config.RecentRoms.Clear();
			recentROMToolStripMenuItem.DropDownItems.Add(clearitem);

			var auto = new ToolStripMenuItem();
			auto.Text = "&Autoload Most Recent";
			auto.Click += (o, ev) => UpdateAutoLoadRecentRom();
			if (Global.Config.AutoLoadMostRecentRom == true)
				auto.Checked = true;
			else
				auto.Checked = false;
			recentROMToolStripMenuItem.DropDownItems.Add(auto);
		}

		private void LoadRamWatch()
		{
			if (!RamWatch1.IsHandleCreated || RamWatch1.IsDisposed)
			{
				RamWatch1 = new RamWatch();
				if (Global.Config.AutoLoadRamWatch)
					RamWatch1.LoadWatchFromRecent(Global.Config.RecentWatches.GetRecentFileByPosition(0));
				RamWatch1.Show();
			}
			else
				RamWatch1.Focus();
		}

		private void LoadRamSearch()
		{
			if (!RamSearch1.IsHandleCreated || RamSearch1.IsDisposed)
			{
				RamSearch1 = new RamSearch();
				RamSearch1.Show();
			}
			else
				RamSearch1.Focus();
		}

        private void LoadHexEditor()
        {
            if (!HexEditor1.IsHandleCreated || HexEditor1.IsDisposed)
            {
                HexEditor1 = new HexEditor();
                HexEditor1.Show();
            }
            else
                HexEditor1.Focus();
        }

        private void LoadNESPPU()
        {
            if (!NESPPU1.IsHandleCreated || NESPPU1.IsDisposed)
            {
                NESPPU1 = new NESPPU();
                NESPPU1.Show();
            }
            else
                NESPPU1.Focus();
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

			Global.RenderPanel.Render(Global.Emulator.VideoProvider);
		}

		private void FrameBufferResized()
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
				MainMenuStrip.Visible = false;
				PerformLayout();
				Global.RenderPanel.Resized = true;
				InFullscreen = true;
			}
			else
			{
				FormBorderStyle = FormBorderStyle.FixedSingle;
				WindowState = FormWindowState.Normal;
				MainMenuStrip.Visible = true;
				Location = WindowedLocation;
				PerformLayout();
				FrameBufferResized();
				InFullscreen = false;
			}
		}

	
		private void viewToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			displayFPSToolStripMenuItem.Checked = Global.Config.DisplayFPS;
			displayFrameCounterToolStripMenuItem.Checked = Global.Config.DisplayFrameCounter;
			displayLagCounterToolStripMenuItem.Checked = Global.Config.DisplayLagCounter;
			displayInputToolStripMenuItem.Checked = Global.Config.DisplayInput;

			x1MenuItem.Checked = false;
			x2MenuItem.Checked = false;
			x3MenuItem.Checked = false;
			x4MenuItem.Checked = false;
			x5MenuItem.Checked = false;
			switch (Global.Config.TargetZoomFactor)
			{
				case 1: x1MenuItem.Checked = true; break;
				case 2: x2MenuItem.Checked = true; break;
				case 3: x3MenuItem.Checked = true; break;
				case 4: x4MenuItem.Checked = true; break;
				case 5: x5MenuItem.Checked = true; break;
				case 10: mzMenuItem.Checked = true; break;
			}
		}

		private void menuStrip1_MenuActivate(object sender, EventArgs e)
		{
            HandlePlatformMenus(Global.Game.System);
            if (Global.Config.PauseWhenMenuActivated)
			{
				if (EmulatorPaused)
					wasPaused = true;
				else
					wasPaused = false;
				didMenuPause = true;
				PauseEmulator();
			}
		}

		private void menuStrip1_MenuDeactivate(object sender, EventArgs e)
		{
			if (!wasPaused)
			{
				UnpauseEmulator();
			}
		}

		private void gUIToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			pauseWhenMenuActivatedToolStripMenuItem.Checked = Global.Config.PauseWhenMenuActivated;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.SaveWindowPosition;
			startPausedToolStripMenuItem.Checked = Global.Config.StartPaused;
            enableRewindToolStripMenuItem.Checked = Global.Config.RewindEnabled;
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
            //Hide platform specific menus until an appropriate ROM is loaded
            NESToolStripMenuItem.Visible = false;
		}

		private void frameSkipToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			miDisplayVsync.Checked = Global.Config.LimitFramerate;
			miAutoMinimizeSkipping.Checked = Global.Config.AutoMinimizeSkipping;
			miLimitFramerate.Checked = Global.Config.LimitFramerate;
			miDisplayVsync.Checked = Global.Config.DisplayVSync;
			miFrameskip0.Checked = Global.Config.FrameSkip == 0;
			miFrameskip1.Checked = Global.Config.FrameSkip == 1;
			miFrameskip2.Checked = Global.Config.FrameSkip == 2;
			miFrameskip3.Checked = Global.Config.FrameSkip == 3;
			miFrameskip4.Checked = Global.Config.FrameSkip == 4;
			miFrameskip5.Checked = Global.Config.FrameSkip == 5;
			miFrameskip6.Checked = Global.Config.FrameSkip == 6;
			miFrameskip7.Checked = Global.Config.FrameSkip == 7;
			miFrameskip8.Checked = Global.Config.FrameSkip == 8;
			miFrameskip9.Checked = Global.Config.FrameSkip == 9;
			miSpeed100.Checked = Global.Config.SpeedPercent == 100;
			miSpeed150.Checked = Global.Config.SpeedPercent == 150;
			miSpeed200.Checked = Global.Config.SpeedPercent == 200;
			miSpeed75.Checked = Global.Config.SpeedPercent == 75;
			miSpeed50.Checked = Global.Config.SpeedPercent == 50;
			miAutoMinimizeSkipping.Enabled = !miFrameskip0.Checked;
			if (!miAutoMinimizeSkipping.Enabled) miAutoMinimizeSkipping.Checked = true;
		}

        private void saveConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveConfig();
            Global.RenderPanel.AddMessage("Saved settings");
        }

        private void loadConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config = ConfigService.Load<Config>("config.ini");
            Global.RenderPanel.AddMessage("Saved loaded");
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
            if (!RamWatch1.IsDisposed)
                RamWatch1.SaveConfigSettings();
            if (!RamSearch1.IsDisposed)
                RamSearch1.SaveConfigSettings();
            if (!HexEditor1.IsDisposed)
                HexEditor1.SaveConfigSettings();
            ConfigService.Save("config.ini", Global.Config);
        }

        private void replayInputLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InputLog.StopMovie();
            InputLog.StartPlayback(); 
            LoadRom(CurrentlyOpenRom);
        }

        private void pPUViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadNESPPU();
        }

        private void enableRewindToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.RewindEnabled ^= true;
        }

        private void hexEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadHexEditor();
        }
	}
}