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

        private bool EmulatorPaused = false;

        public MainForm(string[] args)
        {
            Global.MainForm = this;
            Global.Config = ConfigService.Load<Config>("config.ini");

            InitializeComponent();

			if (Global.Direct3D != null)
				renderTarget = new ViewportPanel();
			else renderTarget = retainedPanel = new RetainedViewportPanel();

            renderTarget.Dock = DockStyle.Fill;
            renderTarget.BackColor = Color.Black;
            Controls.Add(renderTarget);
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

            if (args.Length != 0)
                LoadRom(args[0]);
            else if (Global.Config.AutoLoadMostRecentRom && !Global.Config.RecentRoms.IsEmpty())
                LoadRomFromRecent(Global.Config.RecentRoms.GetRecentFileByPosition(0));

            if (Global.Config.AutoLoadRamWatch)
                LoadRamWatch();
        }

        private void LoadRomFromRecent(string rom)
        {
            bool r = LoadRom(rom);
            if (!r)
            {
                DialogResult result = MessageBox.Show("Could not open " + rom + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                if (result == DialogResult.Yes)
                    Global.Config.RecentRoms.Remove(rom);
            }
        }

        public static ControllerDefinition ClientControlsDef = new ControllerDefinition
        {
            Name = "Emulator Frontend Controls",
            BoolButtons = { "Fast Forward", "Rewind", "Hard Reset", "Mode Flip", "Quick Save State", "Quick Load State", "Save Named State", "Load Named State", "Emulator Pause", "Frame Advance", "Screenshot" }
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
                    break;
                case "GG":
                    Global.Emulator = new SMS { IsGameGear = true };
                    Global.Emulator.Controller = Global.SMSControls;
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

        public void GameTick()
        {
            Input.Update();
            if (ActiveForm != null)
                ScreenSaver.ResetTimerPeriodically();

            if (/*Global.Config.RewindEnabled && */Global.ClientControls["Rewind"])
            {
                Rewind();
                return;
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
                EmulatorPaused = !EmulatorPaused;
                if (EmulatorPaused) Global.Sound.StopSound();
                else Global.Sound.StartSound();
            }

            if (EmulatorPaused == false || Global.ClientControls["Frame Advance"])
            {
                CaptureRewindState();
                Global.Emulator.FrameAdvance(true);
                if (EmulatorPaused)
                    Global.ClientControls.UnpressButton("Frame Advance");
            }
            Global.Sound.UpdateSound(Global.Emulator.SoundProvider);
            Global.RenderPanel.Render(Global.Emulator.VideoProvider);
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
            {
                Global.Sound.StartSound();
                EmulatorPaused = false;
                pauseToolStripMenuItem.Checked = false;
            }
            else
            {
                Global.Sound.StopSound();
                EmulatorPaused = true;
                pauseToolStripMenuItem.Checked = true;
            }
        }

        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void controllersToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void hotkeysToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void displayFPSToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void displayFrameCounterToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void displayInputToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void displayLagCounterToolStripMenuItem_Click(object sender, EventArgs e)
        {

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
            else if (SaveSlot > 9) SaveSlot = 9;  //Meh, just in case
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
            if (IsNullEmulator())
            {
                powerToolStripMenuItem.Enabled = false;
            }
            else
            {
                powerToolStripMenuItem.Enabled = true;
            }

            if (Global.Emulator.ControllerDefinition.BoolButtons.Contains("Reset"))
                resetToolStripMenuItem.Enabled = true;
            else
                resetToolStripMenuItem.Enabled = false;
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
                    recentROMToolStripMenuItem.DropDownItems.Add(item); //TODO: truncate this to a nice size
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

        private void LoadRamWatch() //TODO: accept a filename parameter and feed it to ram watch for loading
        {
            RamWatch RamWatch1 = new RamWatch();
            if (Global.Config.AutoLoadRamWatch)
                RamWatch1.LoadWatchFromRecent(Global.Config.RecentWatches.GetRecentFileByPosition(0));
            RamWatch1.Show();
        }

        private void RAMWatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadRamWatch();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}