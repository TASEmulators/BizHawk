using System;
using System.Collections.Generic;
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
        public const string EMUVERSION = "BizHawk v1.0.0";
		private Control renderTarget;
		private RetainedViewportPanel retainedPanel;
		public string CurrentlyOpenRom;

        //Movie variables
        public Movie InputLog  = new Movie("", MOVIEMODE.INACTIVE);
        public Movie UserMovie = new Movie("", MOVIEMODE.INACTIVE);
        public bool ReadOnly = true;    //Global Movie Read only setting

		//the currently selected savestate slot
		private int SaveSlot = 0;

		//runloop control
		bool exit;
		bool runloop_frameProgress;
		DateTime FrameAdvanceTimestamp = DateTime.MinValue;
		public bool EmulatorPaused;
		int runloop_fps;
		bool runloop_frameadvance;
		DateTime runloop_second;
        public string wasPressed = "";  //Last frame mnemonic
		
		Throttle throttle = new Throttle();
		int rewindCredits;

		//For handling automatic pausing when entering the menu
		private bool wasPaused = false;
		private bool didMenuPause = false;

		//tool dialogs
		public RamWatch RamWatch1 = new RamWatch();
		public RamSearch RamSearch1 = new RamSearch();
        public HexEditor HexEditor1 = new HexEditor();
        public NESNameTableViewer NESNameTableViewer1 = new NESNameTableViewer();
        public NESPPU NESPPU1 = new NESPPU();
        public NESDebugger NESDebug1 = new NESDebugger();
        public Cheats Cheats1 = new Cheats();
        public ToolBox ToolBox1 = new ToolBox();
        public TI83KeyPad TI83KeyPad1 = new TI83KeyPad();
        public TAStudio TAStudio1 = new TAStudio();

		public MainForm(string[] args)
		{
			InitializeComponent();

			//in order to allow late construction of this database, we hook up a delegate here to dearchive the data and provide it on demand
			//we could background thread this later instead if we wanted to be real clever
			NES.BootGodDB.GetDatabaseBytes = () => {
				using (HawkFile NesCartFile = new HawkFile("NesCarts.7z").BindFirst())
				    return Util.ReadAllBytes(NesCartFile.GetStream());
			};
			Global.MainForm = this;

			Database.LoadDatabase("gamedb.txt");

			SyncPresentationMode();

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
                UserMovie.StopMovie();
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
            Global.ActiveController = Global.NullControls;
			Global.Sound = new Sound(Handle, Global.DSound);
			Global.Sound.StartSound();

			//TODO - replace this with some kind of standard dictionary-yielding parser in a separate component
			string cmdRom = null;
			string cmdLoadState = null;
            string cmdMovie = null;
			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i].ToLower();
                if (arg.StartsWith("--load-slot="))
                    cmdLoadState = arg.Substring(arg.IndexOf('=') + 1);
                else if (arg.StartsWith("--movie="))
                    cmdMovie = arg.Substring(arg.IndexOf('=') + 1);
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
                Movie m = new Movie(cmdMovie, MOVIEMODE.PLAY);
                ReadOnly = true;
                StartNewMovie(m, false);
                CreateNewInputLog(false);
                UserMovie.StartPlayback();
                Global.Config.RecentMovies.Add(cmdMovie);
            }
            else if (Global.Config.AutoLoadMostRecentMovie && !Global.Config.RecentMovies.IsEmpty())
            {
                Movie m = new Movie(Global.Config.RecentMovies.GetRecentFileByPosition(0), MOVIEMODE.PLAY);
                ReadOnly = true;
                StartNewMovie(m, false);
                UserMovie.StartPlayback();
                CreateNewInputLog(false);
            }
            else
            {
                CreateNewInputLog(true);
            }

			if (cmdLoadState != null && Global.Game != null)
				LoadState("QuickSave" + cmdLoadState);

			if (Global.Config.AutoLoadRamWatch)
				LoadRamWatch();
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

			if (Global.Config.MainWndx >= 0 && Global.Config.MainWndy >= 0 && Global.Config.SaveWindowPosition)
				this.Location = new Point(Global.Config.MainWndx, Global.Config.MainWndy);

			if (Global.Config.StartPaused)
				PauseEmulator();
		}

        void CreateNewInputLog(bool active)
        {
            MOVIEMODE m;
            if (active)
                m = MOVIEMODE.RECORD;
            else
                m = MOVIEMODE.INACTIVE;
            InputLog = new Movie(PathManager.MakeAbsolutePath(Global.Config.MoviesPath, "") + "\\log.tas", m);
        }

		void SyncPresentationMode()
		{
			bool gdi = Global.Config.ForceGDI;
			if (Global.Direct3D == null)
			{
				gdi = Global.Config.ForceGDI = true;
			}

			if (renderTarget != null)
			{
				renderTarget.Dispose();
				Controls.Remove(renderTarget);
			}

			if (retainedPanel != null) retainedPanel.Dispose();
			if (Global.RenderPanel != null) Global.RenderPanel.Dispose();

			if (gdi)
				renderTarget = retainedPanel = new RetainedViewportPanel();
			else renderTarget = new ViewportPanel();
			Controls.Add(renderTarget);
			Controls.SetChildIndex(renderTarget, 0);

			renderTarget.Dock = DockStyle.Fill;
			renderTarget.BackColor = Color.Black;

			if (gdi)
			{
				Global.RenderPanel = new SysdrawingRenderPanel(retainedPanel);
				retainedPanel.ActivateThreaded();
			}
			else
			{
				Global.RenderPanel = new Direct3DRenderPanel(Global.Direct3D, renderTarget);
			}
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

        public void TogglePause()
        {
            EmulatorPaused ^= true;
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

        private void LoadMoviesFromRecent(string movie)
        {
            Movie m = new Movie(movie, MOVIEMODE.PLAY);
            ReadOnly = true;
            StartNewMovie(m, false);
            /*
            bool r = true; // LoadRom(rom);
            if (!r)
            {
                Global.Sound.StopSound();
                DialogResult result = MessageBox.Show("Could not open " + movie + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                if (result == DialogResult.Yes)
                    Global.Config.RecentMovies.Remove(movie);
                Global.Sound.StartSound();
            }
             */ //TODO: make StartNewMovie or Movie constructor 
        }

		public static ControllerDefinition ClientControlsDef = new ControllerDefinition
		{
			Name = "Emulator Frontend Controls",
			BoolButtons = { "Fast Forward", "Rewind", "Hard Reset", "Mode Flip", "Quick Save State", "Quick Load State", "Save Named State", "Load Named State", 
                "Emulator Pause", "Frame Advance", "Screenshot", "Toggle Fullscreen", "SelectSlot0", "SelectSlot1", "SelectSlot2", "SelectSlot3", "SelectSlot4",
                "SelectSlot5", "SelectSlot6", "SelectSlot7", "SelectSlot8", "SelectSlot9", "SaveSlot0", "SaveSlot1", "SaveSlot2", "SaveSlot3", "SaveSlot4",
                "SaveSlot5","SaveSlot6","SaveSlot7","SaveSlot8","SaveSlot9","LoadSlot0","LoadSlot1","LoadSlot2","LoadSlot3","LoadSlot4","LoadSlot5","LoadSlot6",
                "LoadSlot7","LoadSlot8","LoadSlot9", "ToolBox", "Previous Slot", "Next Slot", "Ram Watch", "Ram Search", "Ram Poke", "Hex Editor", 
                "Lua Console", "Cheats", "Open ROM", "Close ROM", "Display FPS", "Display FrameCounter", "Display LagCounter", "Display Input", "Toggle Read Only",
                "Play Movie", "Record Movie", "Stop Movie", "Play Beginning"}
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
            controls.BindMulti("ToolBox", Global.Config.ToolBox);
            controls.BindMulti("Save Named State", Global.Config.SaveNamedState);
            controls.BindMulti("Load Named State", Global.Config.LoadNamedState);
            controls.BindMulti("Previous Slot", Global.Config.PreviousSlot);
            controls.BindMulti("Next Slot", Global.Config.NextSlot);
            controls.BindMulti("Ram Watch", Global.Config.RamWatch);
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

			Global.ClientControls = controls;


            Global.NullControls = new Controller(NullEmulator.NullController);

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
			for (int i = 0; i < 5; i++)
			{
				pceControls.BindMulti("P"+(i+1)+" Up", Global.Config.PCEController[i].Up);
				pceControls.BindMulti("P"+(i+1)+" Down", Global.Config.PCEController[i].Down);
				pceControls.BindMulti("P"+(i+1)+" Left", Global.Config.PCEController[i].Left);
				pceControls.BindMulti("P"+(i+1)+" Right", Global.Config.PCEController[i].Right);

				pceControls.BindMulti("P"+(i+1)+" B2", Global.Config.PCEController[i].II);
				pceControls.BindMulti("P"+(i+1)+" B1", Global.Config.PCEController[i].I);
				pceControls.BindMulti("P"+(i+1)+" Select", Global.Config.PCEController[i].Select);
				pceControls.BindMulti("P"+(i+1)+" Run", Global.Config.PCEController[i].Run);
			}
			Global.PCEControls = pceControls;

            var nesControls = new Controller(NES.NESController);
			nesControls.BindMulti("Reset", Global.Config.NESReset);
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
			TI83Controls.BindMulti("0", Global.Config.TI83Controller[0]._0); //TODO numpad 4,8,6,2 (up/down/left/right) dont work in slimdx!! wtf!!
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
            TI83Controls.BindMulti("DEL", Global.Config.TI83Controller[0].COMMA);
            TI83Controls.BindMulti("DEL", Global.Config.TI83Controller[0].SIN);
			Global.TI83Controls = TI83Controls;
		}

        private static void FormDragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

        private bool IsValidMovieExtension(string ext)
        {
            switch (ext.ToUpper())
            {
                case ".TAS":   //Bizhawk
                case ".FM2":   //FCUEX
                case ".MC2":   //PCEjin
                case ".STATE": //Savestates
                    return true;
                default:
                    return false;
            }
        }

		private void FormDragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (IsValidMovieExtension(Path.GetExtension(filePaths[0])))
            {
                Movie m = new Movie(filePaths[0], MOVIEMODE.PLAY);
                StartNewMovie(m, false);
                
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

        private void HandlePlatformMenus()
        {
			string system = "";
			if (Global.Game != null)
				system = Global.Game.System;
            switch (system)
            {
                case "TI83":
                    tI83ToolStripMenuItem.Visible = true;
                    NESToolStripMenuItem.Visible = false;
                    break;
                case "NES":
                    NESToolStripMenuItem.Visible = true;
                    tI83ToolStripMenuItem.Visible = false;
                    break;
                default:
                    tI83ToolStripMenuItem.Visible = false;
                    NESToolStripMenuItem.Visible = false;
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
					break;
				case "GG":
                    Global.ActiveController = Global.SMSControls;
					break;
				case "PCE":
                    Global.ActiveController = Global.PCEControls;
					break;
				case "SGX":
                    Global.ActiveController = Global.PCEControls;
					break;
				case "GEN":
                    Global.ActiveController = Global.GenControls;
					break;
				case "TI83":
                    Global.ActiveController = Global.TI83Controls;
					break;
				case "NES":
                    Global.ActiveController = Global.NESControls;
					break;
				case "GB":
					break;
                default:
                    Global.ActiveController = Global.NullControls;
                    break;
			}
		    Global.Emulator.Controller = Global.ActiveController;
		    Global.ActiveController.MovieMode = false;
		}

		public bool LoadRom(string path)
		{
			using (var file = new HawkFile(path))
			{
				//if the provided file doesnt even exist, give up!
				if (!file.Exists) return false;

				//try binding normal rom extensions first
				if (!file.IsBound)
					file.BindSoleItemOf("SMS", "PCE", "SGX", "GG", "SG", "BIN", "SMD", "GB", "NES", "ROM");

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
						if (Global.Config.SmsEnableFM) game.AddOptions("UseFM");
						if (Global.Config.SmsAllowOverlock) game.AddOptions("AllowOverclock");
						if (Global.Config.SmsForceStereoSeparation) game.AddOptions("ForceStereo");
						break;
					case "GG":
						nextEmulator = new SMS { IsGameGear = true };
						if (Global.Config.SmsAllowOverlock) game.AddOptions("AllowOverclock");
						break;
					case "PCE":
						nextEmulator = new PCEngine(NecSystemType.TurboGrafx);
						break;
					case "SGX":
						nextEmulator = new PCEngine(NecSystemType.SuperGrafx);
						break;
					case "GEN":
						nextEmulator = new Genesis(false);//TODO
						break;
					case "TI83":
						nextEmulator = new TI83();
                        if (Global.Config.TI83autoloadKeyPad)
                            LoadTI83KeyPad();
						break;
					case "NES":
						nextEmulator = new NES();
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
				SyncControls();

				if (game.System == "NES")
				{
					Global.Game.Name = (Global.Emulator as NES).GameName;
				}

				Text = DisplayNameForSystem(game.System) + " - " + game.Name;
				ResetRewindBuffer();
				Global.Config.RecentRoms.Add(file.CanonicalFullPath);
				if (File.Exists(game.SaveRamPath))
					LoadSaveRam();

				if (game.System == "GB")
				{
					new BizHawk.Emulation.Consoles.Gameboy.Debugger(Global.Emulator as Gameboy).Show();
				}

                if (UserMovie.GetMovieMode() != MOVIEMODE.INACTIVE)
                {
                    InputLog.SetHeaderLine(MovieHeader.PLATFORM, Global.Emulator.SystemId);
                    CreateNewInputLog(true);
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
                RamWatch1.Restart();
				HexEditor1.Restart();
                NESPPU1.Restart();
                NESNameTableViewer1.Restart();
                NESDebug1.Restart();
                TI83KeyPad1.Restart();
                if (Global.Config.LoadCheatFileByGame)
                {
                    if (Cheats1.AttemptLoadCheatFile())
                        Global.RenderPanel.AddMessage("Cheats file loaded");
                }
                Cheats1.Restart();
				CurrentlyOpenRom = path;
                HandlePlatformMenus();
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
            Global.ActiveController = Global.NullControls;
            UserMovie.StopMovie();
            InputLog.StopMovie();
		}

		[System.Security.SuppressUnmanagedCodeSecurity, DllImport("User32.dll", CharSet = CharSet.Auto)]
		public static extern bool PeekMessage(out Message msg, IntPtr hWnd, UInt32 msgFilterMin, UInt32 msgFilterMax, UInt32 flags);

		public void CheckHotkeys()
        {
            if (Global.ClientControls["ToolBox"])
            {
                LoadToolBox();
                Global.ClientControls.UnpressButton("ToolBox");
            }

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
                    Global.ClientControls.UnpressButton("LoadSlot" + i.ToString());
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
                    Global.ClientControls.UnpressButton("SaveSlot" + i.ToString());
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

            if (Global.ClientControls["Save Named State"])
            {
                SaveStateAs();
                Global.ClientControls.UnpressButton("Save Named State");
            }

            if (Global.ClientControls["Load Named State"])
            {
                LoadStateAs();
                Global.ClientControls.UnpressButton("Load Named State");
            }

            if (Global.ClientControls["Previous Slot"])
            {
                PreviousSlot();
                Global.ClientControls.UnpressButton("Previous Slot");
            }

            if (Global.ClientControls["Next Slot"])
            {
                NextSlot();
                Global.ClientControls.UnpressButton("Next Slot");
            }

            if (Global.ClientControls["Ram Watch"])
            {
                LoadRamWatch();
                Global.ClientControls.UnpressButton("Ram Watch");
            }

            if (Global.ClientControls["Ram Search"])
            {
                LoadRamSearch();
                Global.ClientControls.UnpressButton("Ram Search");
            }

            if (Global.ClientControls["Ram Poke"])
            {
                RamPoke r = new RamPoke();
                r.Show();
                Global.ClientControls.UnpressButton("Ram Poke");
            }

            if (Global.ClientControls["Hex Editor"])
            {
                LoadHexEditor();
                Global.ClientControls.UnpressButton("Hex Editor");
            }

            if (Global.ClientControls["Lua Console"])
            {
                var window = new BizHawk.MultiClient.tools.LuaWindow();
                window.Show();
                Global.ClientControls.UnpressButton("Lua Console");
            }

            if (Global.ClientControls["Cheats"])
            {
                LoadCheatsWindow();
                Global.ClientControls.UnpressButton("Cheats");
            }

            if (Global.ClientControls["Open ROM"])
            {
                OpenROM();
                Global.ClientControls.UnpressButton("Open ROM");
            }

            if (Global.ClientControls["Close ROM"])
            {
                CloseROM();
                Global.ClientControls.UnpressButton("Close ROM");
            }
            //"Display LagCounter", "Display Input"}
            if (Global.ClientControls["Display FPS"])
            {
                ToggleFPS();
                Global.ClientControls.UnpressButton("Display FPS");
            }

            if (Global.ClientControls["Display FrameCounter"])
            {
                ToggleFrameCounter();
                Global.ClientControls.UnpressButton("Display FrameCounter");
            }

            if (Global.ClientControls["Display LagCounter"])
            {
                ToggleLagCounter();
                Global.ClientControls.UnpressButton("Display LagCounter");
            }

            if (Global.ClientControls["Display Input"])
            {
                ToggleInputDisplay();
                Global.ClientControls.UnpressButton("Display Input");
            }

            if (Global.ClientControls["Toggle Read Only"])
            {
                ToggleReadOnly();
                Global.ClientControls.UnpressButton("Toggle Read Only");
            }

            if (Global.ClientControls["Play Movie"])
            {
                PlayMovie();
                Global.ClientControls.UnpressButton("Play Movie");
            }

            if (Global.ClientControls["Record Movie"])
            {
                RecordMovie();
                Global.ClientControls.UnpressButton("Record Movie");
            }

            if (Global.ClientControls["Stop Movie"])
            {
                StopUserMovie();
                Global.ClientControls.UnpressButton("Stop Movie");
            }

            if (Global.ClientControls["Play Beginning"])
            {
                PlayMovieFromBeginning();
                Global.ClientControls.UnpressButton("Play Beginning");
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
                wasPressed = Global.ActiveController.GetControllersAsMnemonic();
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
				runloop_fps++;
				if ((DateTime.Now - runloop_second).TotalSeconds > 1)
				{
					runloop_second = DateTime.Now;
					Global.RenderPanel.FPS = runloop_fps;
					Global.RenderPanel.FPS = runloop_fps;
					runloop_fps = 0;
				}

				if(!suppressCaptureRewind && Global.Config.RewindEnabled) CaptureRewindState();
				if (!runloop_frameadvance) genSound = true;
				else if (!Global.Config.MuteFrameAdvance)
					genSound = true;
                
                if (UserMovie.GetMovieMode() == MOVIEMODE.PLAY)
                {
                    if (UserMovie.GetMovieLength() == Global.Emulator.Frame)
                    {
                        UserMovie.SetMovieFinished();
                        Global.ActiveController.MovieMode = false;
                    }
                    else 
                        Global.ActiveController.SetControllersAsMnemonic(UserMovie.GetInputFrame(Global.Emulator.Frame) + 1);
                }
                
                if (UserMovie.GetMovieMode() == MOVIEMODE.FINISHED)
                {
                    if (UserMovie.GetMovieLength() > Global.Emulator.Frame)
                    {
                        UserMovie.StartPlayback();
                        Global.ActiveController.SetControllersAsMnemonic(UserMovie.GetInputFrame(Global.Emulator.Frame) + 1);
                    }
                }
				Global.Emulator.FrameAdvance(!throttle.skipnextframe);
				RamWatch1.UpdateValues();
				RamSearch1.UpdateValues();
                HexEditor1.UpdateValues();
                NESNameTableViewer1.UpdateValues();
                NESPPU1.UpdateValues();

                if (UserMovie.GetMovieMode() == MOVIEMODE.RECORD)
                    UserMovie.GetMnemonic();
                else if (InputLog.GetMovieMode() ==  MOVIEMODE.RECORD)
                    InputLog.GetMnemonic();
			}

			if(genSound)
				Global.Sound.UpdateSound(Global.Emulator.SoundProvider);
			else
				Global.Sound.UpdateSound(new NullEmulator()); //generates silence

		}

        private void MakeScreenshot(string path)
        {
            var video = Global.Emulator.VideoProvider;
            var image = new Bitmap(video.BufferWidth, video.BufferHeight, PixelFormat.Format32bppArgb);

            var framebuf = video.GetVideoBuffer();
            for (int y = 0; y < video.BufferHeight; y++)
                for (int x = 0; x < video.BufferWidth; x++)
                    image.SetPixel(x, y, Color.FromArgb(framebuf[(y * video.BufferWidth) + x]));

            var f = new FileInfo(path);
            if (f.Directory.Exists == false)
                f.Directory.Create();

            Global.RenderPanel.AddMessage(f.Name + " saved.");

            image.Save(f.FullName, ImageFormat.Png);
        }

		private void TakeScreenshot()
		{
			MakeScreenshot(String.Format(Global.Game.ScreenshotPrefix + ".{0:yyyy-MM-dd HH.mm.ss}.png", DateTime.Now));
		}

        private void HandleMovieSaveState(StreamWriter writer)
        {
            if (UserMovie.GetMovieMode() != MOVIEMODE.INACTIVE)
            {
                UserMovie.DumpLogIntoSavestateText(writer);
            }
            else if (InputLog.GetMovieMode() != MOVIEMODE.INACTIVE)
                InputLog.DumpLogIntoSavestateText(writer);
        }

		private void SaveState(string name)
		{
			string path = Global.Game.SaveStatePrefix + "." + name + ".State";

			var file = new FileInfo(path);
			if (file.Directory.Exists == false)
				file.Directory.Create();

			var writer = new StreamWriter(path);
			Global.Emulator.SaveStateText(writer);
            HandleMovieSaveState(writer);
			writer.Close();
			Global.RenderPanel.AddMessage("Saved state: " + name);
            Global.RenderPanel.AddMessage("Saved state: " + name);
		}

        private void SaveStateAs()
        {
            var sfd = new SaveFileDialog();
            string path = Global.Game.SaveStatePrefix;
            sfd.InitialDirectory = path;
            sfd.FileName = "QuickSave0.State";
            var file = new FileInfo(path);
            if (file.Directory.Exists == false)
                file.Directory.Create();

            var result = sfd.ShowDialog();
            if (result != DialogResult.OK)
                return;

            var writer = new StreamWriter(sfd.FileName);

            Global.Emulator.SaveStateText(writer);
            HandleMovieSaveState(writer);
            writer.Close();
            Global.RenderPanel.AddMessage(sfd.FileName + " saved");
        }

        private void HandleMovieLoadState(StreamReader reader)
        {
            //Note, some of the situations in these IF's may be identical and could be combined but I intentionally separated it out for clarity
            if (UserMovie.GetMovieMode() == MOVIEMODE.RECORD)
            {
                if (ReadOnly)
                {
                    int x = UserMovie.CheckTimeLines(reader);
                    //if (x >= 0)
                    //    MessageBox.Show("Savestate input log does not match the movie at frame " + (x+1).ToString() + "!", "Timeline error", MessageBoxButtons.OK); //TODO: replace with a not annoying message once savestate logic is running smoothly
                    //else
                    {
                        UserMovie.WriteMovie();
                        UserMovie.StartPlayback();
                        Global.ActiveController.MovieMode = true;
                    }
                }
                else
                {
                    Global.ActiveController.MovieMode = false;
                    UserMovie.LoadLogFromSavestateText(reader);
                }
            }
            else if (UserMovie.GetMovieMode() == MOVIEMODE.PLAY)
            {
                if (ReadOnly)
                {
                    int x = UserMovie.CheckTimeLines(reader);
                    //if (x >= 0)
                    //    MessageBox.Show("Savestate input log does not match the movie at frame " + (x+1).ToString() + "!", "Timeline error", MessageBoxButtons.OK); //TODO: replace with a not annoying message once savestate logic is running smoothly
                }
                else
                {
                    UserMovie.StartNewRecording();
                    Global.ActiveController.MovieMode = false;
                    UserMovie.LoadLogFromSavestateText(reader);
                }
            }
            else if (UserMovie.GetMovieMode() == MOVIEMODE.FINISHED)
            {
                //TODO: have the input log kick in upon movie finished mode and stop upon movie resume
                if (ReadOnly)
                {
                    if (Global.Emulator.Frame > UserMovie.GetMovieLength())
                    {
                        Global.ActiveController.MovieMode = false;
                        //Post movie savestate
                        //There is no movie data to load, and the movie will stay in movie finished mode
                        //So do nothing
                    }
                    else
                    {
                        int x = UserMovie.CheckTimeLines(reader);
                        UserMovie.StartPlayback();
                        Global.ActiveController.MovieMode = true;
                        //if (x >= 0)
                        //    MessageBox.Show("Savestate input log does not match the movie at frame " + (x+1).ToString() + "!", "Timeline error", MessageBoxButtons.OK); //TODO: replace with a not annoying message once savestate logic is running smoothly
                    }
                }
                else
                {
                    if (Global.Emulator.Frame > UserMovie.GetMovieLength())
                    {
                        Global.ActiveController.MovieMode = false;
                        //Post movie savestate
                        //There is no movie data to load, and the movie will stay in movie finished mode
                        //So do nothing
                    }
                    else
                    {
                        UserMovie.StartNewRecording();
                        Global.ActiveController.MovieMode = false;
                        UserMovie.LoadLogFromSavestateText(reader);
                    }
                }
            }
            else
            {
                if (InputLog.GetMovieMode() == MOVIEMODE.RECORD)
                    InputLog.LoadLogFromSavestateText(reader);
            }
        }

		private void LoadState(string name)
		{
			string path = Global.Game.SaveStatePrefix + "." + name + ".State";
			if (File.Exists(path) == false)
				return;

			var reader = new StreamReader(path);
			Global.Emulator.LoadStateText(reader);
            HandleMovieLoadState(reader);
            reader.Close();
			Global.RenderPanel.AddMessage("Loaded state: " + name);
		}

        private void LoadStateAs()
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = Global.Game.SaveStatePrefix;
            ofd.Filter = "Save States (*.State)|*.State|All File|*.*";
            ofd.RestoreDirectory = true;

            Global.Sound.StopSound();
            var result = ofd.ShowDialog();
            Global.Sound.StartSound();

            if (result != DialogResult.OK)
                return;

            if (File.Exists(ofd.FileName) == false)
                return;

            var reader = new StreamReader(ofd.FileName);
            Global.Emulator.LoadStateText(reader);
            HandleMovieLoadState(reader);
            reader.Close();
            Global.RenderPanel.AddMessage(ofd.FileName + " loaded");
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

            pauseToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.EmulatorPauseBinding;
            powerToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.HardResetBinding;
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

			screenshotF12ToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.ScreenshotBinding;
            openROMToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.OpenROM;
            closeROMToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.CloseROM;
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
                statusStrip1.Visible = false;
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
                statusStrip1.Visible = true;
			}
		}

	
		private void viewToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			displayFPSToolStripMenuItem.Checked = Global.Config.DisplayFPS;
			displayFrameCounterToolStripMenuItem.Checked = Global.Config.DisplayFrameCounter;
			displayLagCounterToolStripMenuItem.Checked = Global.Config.DisplayLagCounter;
			displayInputToolStripMenuItem.Checked = Global.Config.DisplayInput;
            displayRerecordCountToolStripMenuItem.Checked = Global.Config.DisplayRerecordCount;

            displayFPSToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.FPSBinding;
            displayFrameCounterToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.FrameCounterBinding;
            displayLagCounterToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.LagCounterBinding;
            displayInputToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.InputDisplayBinding;

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

            switchToFullscreenToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.ToggleFullscreenBinding;
		}

		private void menuStrip1_MenuActivate(object sender, EventArgs e)
		{
            HandlePlatformMenus();
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
            runInBackgroundToolStripMenuItem.Checked = Global.Config.RunInBackground;
            pauseWhenMenuActivatedToolStripMenuItem.Checked = Global.Config.PauseWhenMenuActivated;
			saveWindowPositionToolStripMenuItem.Checked = Global.Config.SaveWindowPosition;
			startPausedToolStripMenuItem.Checked = Global.Config.StartPaused;
            enableRewindToolStripMenuItem.Checked = Global.Config.RewindEnabled;
			forceGDIPPresentationToolStripMenuItem.Checked = Global.Config.ForceGDI;
            acceptBackgroundInputToolStripMenuItem.Checked = Global.Config.AcceptBackgroundInput;
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
            //Hide platform specific menus until an appropriate ROM is loaded
            NESToolStripMenuItem.Visible = false;
            tI83ToolStripMenuItem.Visible = false;
		}

        private void OpenROM()
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = PathManager.GetRomsPath(Global.Emulator.SystemId);
            ofd.Filter = "Rom Files|*.NES;*.SMS;*.GG;*.SG;*.PCE;*.SGX;*.GB;*.BIN;*.SMD;*.ROM;*.ZIP;*.7z|NES|*.NES|Master System|*.SMS;*.GG;*.SG;*.ZIP;*.7z|PC Engine|*.PCE;*.SGX;*.ZIP;*.7z|Gameboy|*.GB;*.ZIP;*.7z|TI-83|*.rom|Archive Files|*.zip;*.7z|Savestate|*.state|All Files|*.*";
            ofd.RestoreDirectory = true;

            Global.Sound.StopSound();
            var result = ofd.ShowDialog();
            Global.Sound.StartSound();
            if (result != DialogResult.OK)
                return;
            var file = new FileInfo(ofd.FileName);
            Global.Config.LastRomPath = file.DirectoryName;
            LoadRom(file.FullName);
        }

        private void CloseROM()
        {
            CloseGame();
            Global.Emulator = new NullEmulator();
            Global.Game = null;
            RamSearch1.Restart();
            RamWatch1.Restart();
            HexEditor1.Restart();
            NESPPU1.Restart();
            NESNameTableViewer1.Restart();
            NESDebug1.Restart();
            TI83KeyPad1.Restart();
            Cheats1.Restart();
            Text = "BizHawk";
            HandlePlatformMenus();
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

        private void PreviousSlot()
        {
            if (SaveSlot == 0) SaveSlot = 9;       //Wrap to end of slot list
            else if (SaveSlot > 9) SaveSlot = 9;   //Meh, just in case
            else SaveSlot--;
            SaveSlotSelectedMessage();
        }

        private void NextSlot()
        {
            if (SaveSlot >= 9) SaveSlot = 1;       //Wrap to beginning of slot list
            else SaveSlot++;
            SaveSlotSelectedMessage();
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

        private void movieToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            if (UserMovie.GetMovieMode() == MOVIEMODE.INACTIVE)
            {
                stopMovieToolStripMenuItem.Enabled = false;
                playFromBeginningToolStripMenuItem.Enabled = false;
            }
            else
            {
                stopMovieToolStripMenuItem.Enabled = true;
                playFromBeginningToolStripMenuItem.Enabled = true;
            }

            readonlyToolStripMenuItem.Checked = ReadOnly;
            bindSavestatesToMoviesToolStripMenuItem.Checked = Global.Config.BindSavestatesToMovies;

            readonlyToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.ReadOnlyToggleBinding;
            recordMovieToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.RecordMovieBinding;
            playMovieToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.PlayMovieBinding;
            stopMovieToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.StopMovieBinding;
            playFromBeginningToolStripMenuItem.ShortcutKeyDisplayString = Global.Config.PlayBeginningBinding;
            
        }

        public void ToggleReadOnly()
        {
            ReadOnly ^= true;
            if (ReadOnly)
                Global.RenderPanel.AddMessage("Movie read-only mode");
            else
                Global.RenderPanel.AddMessage("Movie read+write mode");
        }

        private void readonlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleReadOnly();
        }

        public void StartNewMovie(Movie m, bool record)
        {
            
            UserMovie = m;
            InputLog.StopMovie();
            LoadRom(Global.MainForm.CurrentlyOpenRom);
            UserMovie.LoadMovie();
            Global.Config.RecentMovies.Add(m.GetFilePath());
            if (record)
            {
                UserMovie.StartNewRecording();
                ReadOnly = false;
            }
            else
                UserMovie.StartPlayback();
        }

        public Movie GetActiveMovie()
        {
            if (UserMovie.GetMovieMode() != MOVIEMODE.INACTIVE)
                return UserMovie;
            else if (InputLog.GetMovieMode() != MOVIEMODE.INACTIVE)
                return InputLog;
            else
                return null;
        }

        public bool MovieActive()
        {
            if (UserMovie.GetMovieMode() != MOVIEMODE.INACTIVE)
                return true;
            else if (InputLog.GetMovieMode() != MOVIEMODE.INACTIVE)
                return true;
            else
                return false;
        }

        private void PlayMovie()
        {
            PlayMovie p = new PlayMovie();
            DialogResult d = p.ShowDialog();
        }

        private void RecordMovie()
        {
            RecordMovie r = new RecordMovie();
            r.ShowDialog();
        }

        public void PlayMovieFromBeginning()
        {
            if (InputLog.GetMovieMode() != MOVIEMODE.INACTIVE)
            {
                LoadRom(CurrentlyOpenRom);
                UserMovie.StartPlayback();
            }
        }

        public void LoadRamWatch()
        {
            if (!RamWatch1.IsHandleCreated || RamWatch1.IsDisposed)
            {
                RamWatch1 = new RamWatch();
                if (Global.Config.AutoLoadRamWatch && Global.Config.RecentWatches.Length() > 0)
                    RamWatch1.LoadWatchFromRecent(Global.Config.RecentWatches.GetRecentFileByPosition(0));
                RamWatch1.Show();
            }
            else
                RamWatch1.Focus();
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            if (!Global.Config.RunInBackground)
                PauseEmulator();
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            if (!Global.Config.RunInBackground)
                UnpauseEmulator();
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

        private void tAStudioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadTAStudio();
        }
	}
}