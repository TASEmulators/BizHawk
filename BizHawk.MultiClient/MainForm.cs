 using System;
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

namespace BizHawk.MultiClient
{
    public partial class MainForm : Form
    {
        private Control renderTarget;
		private RetainedViewportPanel retainedPanel;
        private string CurrentlyOpenRom;
        private int SaveSlot = 0;   //Saveslot sytem
        private bool wasPaused = false; //For handling automatic pausing when entering the menu
        private int FrameAdvanceDelay = 0;
        private bool EmulatorPaused = false;
        RamWatch RamWatch1 = new RamWatch();
        RamSearch RamSearch1 = new RamSearch();

        public MainForm(string[] args)
        {
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
                   ConfigService.Save("config.ini", Global.Config);
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

            Application.Idle += Application_Idle;

			//TODO - replace this with some kind of standard dictionary-yielding parser in a separate component
			string cmdRom = null;
			string cmdLoadState = null;
			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i].ToLower();
				if (arg.StartsWith("--load-slot="))
					cmdLoadState = arg.Substring(arg.IndexOf('=')+1);
				else
					cmdRom = arg;
			}

			if(cmdRom != null) //Commandline should always override auto-load
				LoadRom(cmdRom);
            else if (Global.Config.AutoLoadMostRecentRom && !Global.Config.RecentRoms.IsEmpty())
                LoadRomFromRecent(Global.Config.RecentRoms.GetRecentFileByPosition(0));

			if(cmdLoadState != null)
				LoadState("QuickSave" + cmdLoadState);

            if (Global.Config.AutoLoadRamWatch)
                LoadRamWatch();
            if (Global.Config.AutoLoadRamSearch)
                LoadRamSearch();
        }

        private void PauseEmulator()
        {
            EmulatorPaused = true;
            Global.Sound.StopSound();
        }

        private void UnpauseEmulator()
        {
            EmulatorPaused = false;
            Global.Sound.StartSound();
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
                "SelectSlot5", "SelectSlot6", "SelectSlot7", "SelectSlot8", "SelectSlot9"}
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
            Global.ClientControls = controls;

            var smsControls = new Controller(SMS.SmsController);
            smsControls.BindMulti("Reset", Global.Config.SmsReset);
            smsControls.BindMulti("Pause", Global.Config.SmsPause);

            smsControls.BindMulti("P1 Up", Global.Config.SmsP1Up);
            smsControls.BindMulti("P1 Left", Global.Config.SmsP1Left);
            smsControls.BindMulti("P1 Right", Global.Config.SmsP1Right);
            smsControls.BindMulti("P1 Down", Global.Config.SmsP1Down);
            smsControls.BindMulti("P1 B1", Global.Config.SmsP1B1);
            smsControls.BindMulti("P1 B2", Global.Config.SmsP1B2);

            smsControls.BindMulti("P2 Up", Global.Config.SmsP2Up);
            smsControls.BindMulti("P2 Left", Global.Config.SmsP2Left);
            smsControls.BindMulti("P2 Right", Global.Config.SmsP2Right);
            smsControls.BindMulti("P2 Down", Global.Config.SmsP2Down);
            smsControls.BindMulti("P2 B1", Global.Config.SmsP2B1);
            smsControls.BindMulti("P2 B2", Global.Config.SmsP2B2);
            Global.SMSControls = smsControls;

            var pceControls = new Controller(PCEngine.PCEngineController);
            pceControls.BindMulti("Up", Global.Config.PCEUp);
            pceControls.BindMulti("Down", Global.Config.PCEDown);
            pceControls.BindMulti("Left", Global.Config.PCELeft);
            pceControls.BindMulti("Right", Global.Config.PCERight);

            pceControls.BindMulti("II", Global.Config.PCEBII);
            pceControls.BindMulti("I", Global.Config.PCEBI);
            pceControls.BindMulti("Select", Global.Config.PCESelect);
            pceControls.BindMulti("Run", Global.Config.PCERun);
            Global.PCEControls = pceControls;

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
            string[] filePaths = (string[]) e.Data.GetData(DataFormats.FileDrop);
            LoadRom(filePaths[0]);
        }

        bool IsNullEmulator()
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
                case "SG":  return "SG-1000";
                case "SMS": return "Sega Master System";
                case "GG":  return "Game Gear";
                case "PCE": return "TurboGrafx-16";
                case "SGX": return "SuperGrafx";
                case "GEN": return "Genesis";
                case "TI83": return "TI-83";
				case "GB": return "Game Boy";
            }
            return "";
        }

        private bool LoadRom(string path)
        {
            var file = new FileInfo(path);
            if (file.Exists == false) return false;

            CloseGame();

            var game = new RomGame(path);
            Global.Game = game;

            switch(game.System)
            {
                case "SG":
                case "SMS": 
                    Global.Emulator = new SMS();
                    Global.Emulator.Controller = Global.SMSControls;
                    if (Global.Config.SmsEnableFM) game.AddOptions("UseFM");
                    if (Global.Config.SmsAllowOverlock) game.AddOptions("AllowOverclock");
                    if (Global.Config.SmsForceStereoSeparation) game.AddOptions("ForceStereo");
                    break;
                case "GG":
                    Global.Emulator = new SMS { IsGameGear = true };
                    Global.Emulator.Controller = Global.SMSControls;
                    if (Global.Config.SmsAllowOverlock) game.AddOptions("AllowOverclock");
                    break;
                case "PCE":
                    Global.Emulator = new PCEngine(NecSystemType.TurboGrafx);
                    Global.Emulator.Controller = Global.PCEControls;
                    break;
                case "SGX":
                    Global.Emulator = new PCEngine(NecSystemType.SuperGrafx);
                    Global.Emulator.Controller = Global.PCEControls;
                    break;
                case "GEN":
                    Global.Emulator = new Genesis(false);//TODO
                    Global.Emulator.Controller = Global.GenControls;
                    break;
				case "TI83":
					Global.Emulator = new TI83();
					Global.Emulator.Controller = Global.TI83Controls;
					break;
				case "GB":
            		Global.Emulator = new Gameboy();
            		break;
            }

            Global.Emulator.LoadGame(game);
            Text = DisplayNameForSystem(game.System) + " - " + game.Name;
            ResetRewindBuffer();
            Global.Config.RecentRoms.Add(file.FullName);
            if (File.Exists(game.SaveRamPath))
                LoadSaveRam();

			if (game.System == "GB")
			{
				new BizHawk.Emulation.Consoles.Gameboy.Debugger(Global.Emulator as Gameboy).Show();
			}

            CurrentlyOpenRom = path;
        	return true;
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
        }

        [System.Security.SuppressUnmanagedCodeSecurity, DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool PeekMessage(out Message msg, IntPtr hWnd, UInt32 msgFilterMin, UInt32 msgFilterMax, UInt32 flags);

        /// <summary>
        /// This functions calls Emulator.FrameAdvance(true) and handles any updates that need to happen on a per frame basis
        /// </summary>
        public void DoFrameAdvance()
        {
            Global.Emulator.FrameAdvance(true); //TODO: Do these things need to happen on (false) as well? Think about it
            RamWatch1.UpdateValues();
        }

        public void GameTick()
        {
            Input.Update();
            if (ActiveForm != null)
                ScreenSaver.ResetTimerPeriodically();

            if (EmulatorPaused == false)
            {
                CaptureRewindState();
                DoFrameAdvance();
            }

            if (!Global.ClientControls.IsPressed("Frame Advance"))
                FrameAdvanceDelay = 60;


            if (Global.ClientControls["Frame Advance"] && FrameAdvanceDelay > 0)
            {
                if (FrameAdvanceDelay == 60)
                {
                    if (EmulatorPaused == false)
                        PauseEmulator();
                    DoFrameAdvance();
                    FrameAdvanceDelay--;
                }
                else
                {
                    if (FrameAdvanceDelay > 0)
                        FrameAdvanceDelay--;
                    if (FrameAdvanceDelay < 0)
                        FrameAdvanceDelay = 0;
                }
            }

            if (Global.ClientControls["Frame Advance"] && FrameAdvanceDelay == 0)
            {
                DoFrameAdvance();
            }

            if (/*Global.Config.RewindEnabled && */Global.ClientControls["Rewind"])
            {
                PauseEmulator();
                Rewind(Global.ClientControls["Fast Forward"] ? 3 : 1);
                return;
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

            if (Global.ClientControls["SelectSlot0"])
            {
                SaveSlot = 0;
                SaveSlotSelectedMessage();
                Global.ClientControls.UnpressButton("SelectSlot0");
            }
            if (Global.ClientControls["SelectSlot1"])
            {
                SaveSlot = 1;
                SaveSlotSelectedMessage();
                Global.ClientControls.UnpressButton("SelectSlot1");
            }
            if (Global.ClientControls["SelectSlot2"])
            {
                SaveSlot = 2;
                SaveSlotSelectedMessage();
                Global.ClientControls.UnpressButton("SelectSlot2");
            }
            if (Global.ClientControls["SelectSlot3"])
            {
                SaveSlot = 3;
                SaveSlotSelectedMessage();
                Global.ClientControls.UnpressButton("SelectSlot3");
            }
            if (Global.ClientControls["SelectSlot4"])
            {
                SaveSlot = 4;
                SaveSlotSelectedMessage();
                Global.ClientControls.UnpressButton("SelectSlot4");
            }
            if (Global.ClientControls["SelectSlot5"])
            {
                SaveSlot = 5;
                SaveSlotSelectedMessage();
                Global.ClientControls.UnpressButton("SelectSlot5");
            }
            if (Global.ClientControls["SelectSlot6"])
            {
                SaveSlot = 6;
                SaveSlotSelectedMessage();
                Global.ClientControls.UnpressButton("SelectSlot6");
            }
            if (Global.ClientControls["SelectSlot7"])
            {
                SaveSlot = 7;
                SaveSlotSelectedMessage();
                Global.ClientControls.UnpressButton("SelectSlot7");
            }
            if (Global.ClientControls["SelectSlot8"])
            {
                SaveSlot = 8;
                SaveSlotSelectedMessage();
                Global.ClientControls.UnpressButton("SelectSlot8");
            }
            if (Global.ClientControls["SelectSlot9"])
            {
                SaveSlot = 9;
                SaveSlotSelectedMessage();
                Global.ClientControls.UnpressButton("SelectSlot9");
            }

            if (Global.ClientControls["Hard Reset"])
            {
                Global.ClientControls.UnpressButton("Hard Reset");
                LoadRom(CurrentlyOpenRom);
            }

            if (Global.ClientControls["Fast Forward"])
            {
                Global.Emulator.FrameAdvance(false);
                Global.Emulator.FrameAdvance(false);
                Global.Emulator.FrameAdvance(false);
            }

            if (Global.ClientControls["Screenshot"])
            {
                Global.ClientControls.UnpressButton("Screenshot");
                TakeScreenshot();
            }

            if (Global.ClientControls["Emulator Pause"])
            {
                Global.ClientControls.UnpressButton("Emulator Pause");
                if (EmulatorPaused)
                    UnpauseEmulator();
                else
                    PauseEmulator();
            }

            if (Global.ClientControls["Toggle Fullscreen"])
            {
                Global.ClientControls.UnpressButton("Toggle Fullscreen");
                ToggleFullscreen();
            }

            Global.Sound.UpdateSound(Global.Emulator.SoundProvider);
            Render();
        }

        private bool wasMaximized = false;

        private void Application_Idle(object sender, EventArgs e)
        {
            if (wasMaximized != (WindowState == FormWindowState.Maximized))
            {
                wasMaximized = (WindowState == FormWindowState.Maximized);
                Global.RenderPanel.Resized = true;
            }

            Message msg;
            while (!PeekMessage(out msg, IntPtr.Zero, 0, 0, 0))
            {
                GameTick();
            }
        }

        private void TakeScreenshot()
        {
            var video = Global.Emulator.VideoProvider;
            var image = new Bitmap(video.BufferWidth, video.BufferHeight, PixelFormat.Format32bppArgb);

            for (int y=0; y<video.BufferHeight; y++)
                for (int x=0; x<video.BufferWidth; x++)
                    image.SetPixel(x, y, Color.FromArgb(video.GetVideoBuffer()[(y*video.BufferWidth)+x]));

            var f = new FileInfo(String.Format(Global.Game.ScreenshotPrefix+".{0:yyyy-MM-dd HH.mm.ss}.png",DateTime.Now));
            if (f.Directory.Exists == false)
                f.Directory.Create();

            Global.RenderPanel.AddMessage(f.Name+" saved.");

            image.Save(f.FullName, ImageFormat.Png);
        }

        private void SaveState(string name)
        {
            string path = Global.Game.SaveStatePrefix+"."+name+".State";

            var file = new FileInfo(path);
            if (file.Directory.Exists == false)
                file.Directory.Create();

            var writer = new StreamWriter(path);
            Global.Emulator.SaveStateText(writer);
            writer.Close();
            Global.RenderPanel.AddMessage("Saved state: "+name);
        }

        private void LoadState(string name)
        {
            string path = Global.Game.SaveStatePrefix + "." + name + ".State";
            if (File.Exists(path) == false)
                return;

            var reader = new StreamReader(path);
            Global.Emulator.LoadStateText(reader);
            reader.Close();
            Global.RenderPanel.AddMessage("Loaded state: "+name);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (RamWatch1.AskSave())
                Close();
        }

        private void openROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = Global.Config.LastRomPath;
            ofd.Filter = "Rom Files|*.SMS;*.GG;*.SG;*.PCE;*.SGX;*.GB;*.BIN;*.SMD;*.ZIP;*.7z|Master System|*.SMS;*.GG;*.SG;*.ZIP;*.7z|PC Engine|*.PCE;*.SGX;*.ZIP;*.7z|Gameboy|*.GB;*.ZIP;*.7z|Archive Files|*.zip;*.7z|All Files|*.*";
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

        private void savestate1toolStripMenuItem_Click(object sender, EventArgs e)  { SaveState("QuickSave1"); }
        private void savestate2toolStripMenuItem_Click(object sender, EventArgs e)  { SaveState("QuickSave2"); }
        private void savestate3toolStripMenuItem_Click(object sender, EventArgs e)  { SaveState("QuickSave3"); }
        private void savestate4toolStripMenuItem_Click(object sender, EventArgs e)  { SaveState("QuickSave4"); }
        private void savestate5toolStripMenuItem_Click(object sender, EventArgs e)  { SaveState("QuickSave5"); }
        private void savestate6toolStripMenuItem_Click(object sender, EventArgs e)  { SaveState("QuickSave6"); }
        private void savestate7toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave7"); }
        private void savestate8toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave8"); }
        private void savestate9toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave9"); }
        private void savestate0toolStripMenuItem_Click(object sender, EventArgs e) { SaveState("QuickSave0"); }

        private void loadstate1toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave1"); }
        private void loadstate2toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave2"); }
        private void loadstate3toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave3"); }
        private void loadstate4toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave4"); }
        private void loadstate5toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave5"); }
        private void loadstate6toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave6"); }
        private void loadstate7toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave7"); }
        private void loadstate8toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave8"); }
        private void loadstate9toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave9"); }
        private void loadstate0toolStripMenuItem_Click(object sender, EventArgs e) { LoadState("QuickSave0"); }

        private void saveStateToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Global.Sound.StopSound();
            Application.Idle -= Application_Idle;

            var frm = new NameStateForm();
            frm.ShowDialog(this);

            if (frm.OK)
                SaveState(frm.Result);

            Global.Sound.StartSound();
            Application.Idle += Application_Idle;
        }

        private void powerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadRom(CurrentlyOpenRom);
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Global.Emulator.ControllerDefinition.BoolButtons.Contains("Reset"))
                Global.Emulator.Controller.ForceButton("Reset");
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (EmulatorPaused == true)
                UnpauseEmulator();
            else
                PauseEmulator();
        }

        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void controllersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InputConfig i = new InputConfig();
            i.ShowDialog();
        }

        private void hotkeysToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void displayFPSToolStripMenuItem_Click(object sender, EventArgs e)
        {
           Global.Config.DisplayFPS ^= true;
        }

        private void displayFrameCounterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.DisplayFrameCounter ^= true;
        }

        private void displayInputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.DisplayInput ^= true;
        }

        private void displayLagCounterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.DisplayLagCounter ^= true;
        }

        private void screenshotF12ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TakeScreenshot();
        }

        private void SaveSlotSelectedMessage()
        {
            Global.RenderPanel.AddMessage("Slot " + SaveSlot + " selected.");
        }

        private void selectSlot1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSlot = 1;
            SaveSlotSelectedMessage();
        }

        private void selectSlot2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSlot = 2;
            SaveSlotSelectedMessage();
        }

        private void selectSlot3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSlot = 3;
            SaveSlotSelectedMessage();
        }

        private void selectSlot4ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSlot = 4;
            SaveSlotSelectedMessage();
        }

        private void selectSlot5ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSlot = 5;
            SaveSlotSelectedMessage();
        }

        private void selectSlot6ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSlot = 6;
            SaveSlotSelectedMessage();
        }

        private void selectSlot7ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSlot = 7;
            SaveSlotSelectedMessage();
        }

        private void selectSlot8ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSlot = 8;
            SaveSlotSelectedMessage();
        }

        private void selectSlot9ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSlot = 9;
            SaveSlotSelectedMessage();
        }

        private void selectSlot10ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveSlot = 0;
            SaveSlotSelectedMessage();
        }

        private void previousSlotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SaveSlot == 0) SaveSlot = 9;       //Wrap to end of slot list
            else if (SaveSlot > 9) SaveSlot = 9;   //Meh, just in case
            else SaveSlot--;
            SaveSlotSelectedMessage();
        }

        private void nextSlotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SaveSlot >= 9) SaveSlot = 1;       //Wrap to beginning of slot list
            else SaveSlot++;
            SaveSlotSelectedMessage();
        }

        private void saveToCurrentSlotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveState("QuickSave" + SaveSlot.ToString());
        }

        private void loadCurrentSlotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadState("QuickSave" + SaveSlot.ToString());
        }

        private void closeROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseGame();
            Global.Emulator = new NullEmulator();
            Text = "BizHawk";
        }

        private void emulationToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            powerToolStripMenuItem.Enabled = !IsNullEmulator();
            resetToolStripMenuItem.Enabled = Global.Emulator.ControllerDefinition.BoolButtons.Contains("Reset");

            enableFMChipToolStripMenuItem.Checked = Global.Config.SmsEnableFM;
            overclockWhenKnownSafeToolStripMenuItem.Checked = Global.Config.SmsAllowOverlock;
            forceStereoSeparationToolStripMenuItem.Checked = Global.Config.SmsForceStereoSeparation;
            pauseToolStripMenuItem.Checked = EmulatorPaused;
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.RecentRoms.Clear();
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

        private void autoloadMostRecentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateAutoLoadRecentRom();
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
            RamWatch1 = new RamWatch();
            if (Global.Config.AutoLoadRamWatch)
                RamWatch1.LoadWatchFromRecent(Global.Config.RecentWatches.GetRecentFileByPosition(0));
            RamWatch1.Show();
        }

        private void LoadRamSearch()
        {
            RamSearch1 = new RamSearch();
            RamSearch1.Show();
        }

        private void RAMWatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadRamWatch();
        }

        private void rAMSearchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadRamSearch();
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
            Size = new Size((video.BufferWidth*zoom) + borderWidth, (video.BufferHeight*zoom + borderHeight));
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
            } else {
                FormBorderStyle = FormBorderStyle.FixedSingle;
                WindowState = FormWindowState.Normal;
                MainMenuStrip.Visible = true;
                Location = WindowedLocation;
                PerformLayout();
                FrameBufferResized();
                InFullscreen = false;
            }
        }

        private void zoomMenuItem_Click(object sender, EventArgs e)
        {
            if (sender == x1MenuItem) Global.Config.TargetZoomFactor = 1;
            if (sender == x2MenuItem) Global.Config.TargetZoomFactor = 2;
            if (sender == x3MenuItem) Global.Config.TargetZoomFactor = 3;
            if (sender == x4MenuItem) Global.Config.TargetZoomFactor = 4;
            if (sender == x5MenuItem) Global.Config.TargetZoomFactor = 5;
            if (sender == mzMenuItem) Global.Config.TargetZoomFactor = 10;

            x1MenuItem.Checked = Global.Config.TargetZoomFactor == 1;
            x2MenuItem.Checked = Global.Config.TargetZoomFactor == 2;
            x3MenuItem.Checked = Global.Config.TargetZoomFactor == 3;
            x4MenuItem.Checked = Global.Config.TargetZoomFactor == 4;
            x5MenuItem.Checked = Global.Config.TargetZoomFactor == 5;
            mzMenuItem.Checked = Global.Config.TargetZoomFactor == 10;
            
            FrameBufferResized();
        }

        private void enableFMChipToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.SmsEnableFM ^= true;
        }

        private void overclockWhenKnownSafeToolStripMenuItem_Click(object sender, EventArgs e)
        {
           Global.Config.SmsAllowOverlock ^= true;
        }

        private void forceStereoSeparationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.SmsForceStereoSeparation ^= true;
        }

        private void recordMovieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RecordMovie r = new RecordMovie();
            r.ShowDialog();
        }

        private void playMovieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PlayMovie p = new PlayMovie();
            p.ShowDialog();
        }

        private void stopMovieToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void playFromBeginningToolStripMenuItem_Click(object sender, EventArgs e)
        {

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
            if (Global.Config.PauseWhenMenuActivated)
            {
                if (EmulatorPaused)
                    wasPaused = true;
                else
                    wasPaused = false;
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
        }

        private void pauseWhenMenuActivatedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Global.Config.PauseWhenMenuActivated ^= true;
        }

        private void soundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SoundConfig s = new SoundConfig();
            s.ShowDialog();
        }
    }
}