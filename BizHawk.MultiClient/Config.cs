namespace BizHawk.MultiClient
{    
    public class Config
    {
        public Config()
        {
            SMSController[0] = new SMSControllerTemplate(true);
            SMSController[1] = new SMSControllerTemplate(false);
            PCEController[0] = new PCEControllerTemplate(true);
            PCEController[1] = new PCEControllerTemplate(false);
            PCEController[2] = new PCEControllerTemplate(false);
            PCEController[3] = new PCEControllerTemplate(false);
            PCEController[4] = new PCEControllerTemplate(false);
            NESController[0] = new NESControllerTemplate(true);
            NESController[1] = new NESControllerTemplate(false);
            NESController[2] = new NESControllerTemplate(false);
            NESController[3] = new NESControllerTemplate(false);
        }

        // General Client Settings
        public int TargetZoomFactor = 2;
        public string LastRomPath = ".";
        public bool AutoLoadMostRecentRom = false;    //TODO: eventually make a class or struct for all the auto-loads, which will include recent roms, movies, etc, as well as autoloading any modeless dialog
        public RecentFiles RecentRoms = new RecentFiles(8);
        public bool PauseWhenMenuActivated = true;
        public bool SaveWindowPosition = true;
        public bool StartPaused = false;
        public int MainWndx = -1; //Negative numbers will be ignored
        public int MainWndy = -1;

		// Run-Control settings
		public int FrameProgressDelayMs = 500; //how long until a frame advance hold turns into a frame progress?
		public int FrameSkip = 0;
		public int SpeedPercent = 100;
		public bool LimitFramerate = true;
		public bool AutoMinimizeSkipping = true;
		public bool DisplayVSync = false;
		public bool RewindEnabled = true;

        // Display options
        public bool DisplayFPS = false;
        public int DispFPSx = 0;
        public int DispFPSy = 0;
        public bool DisplayFrameCounter = false;
        public int DispFrameCx = 0;
        public int DispFrameCy = 12;
        public bool DisplayLagCounter = false;
        public int DispLagx = 0;
        public int DispLagy = 36;
        public bool DisplayInput = false;
        public int DispInpx = 0;
        public int DispInpy = 24;
		public bool ForceGDI = false;

        // Sound options
        public bool SoundEnabled = true;
        public bool MuteFrameAdvance = true;

        // Lua Console
        public RecentFiles RecentLua = new RecentFiles(8);
        public bool AutoLoadLua = false;
        public bool LuaConsoleSaveWindowPosition = true;
        public int LuaConsoleWndx = -1;   //Negative numbers will be ignored even with save window position set
        public int LuaConsoleWndy = -1;
        public int LuaConsoleWidth = -1;
        public int LuaConsoleHeight = -1;

        // RamWatch Settings
        public bool AutoLoadRamWatch = false;
        public RecentFiles RecentWatches = new RecentFiles(8);
        public bool RamWatchSaveWindowPosition = true;
        public int RamWatchWndx = -1;   //Negative numbers will be ignored even with save window position set
        public int RamWatchWndy = -1;
        public int RamWatchWidth = -1;  
        public int RamWatchHeight = -1;
        public bool RamWatchShowChangeColumn = true;
        public bool RamWatchShowPrevColumn = false;
        public bool RamWatchShowChangeFromPrev = true;
        public int RamWatchAddressWidth = -1;
        public int RamWatchValueWidth = -1;
        public int RamWatchPrevWidth = -1;
        public int RamWatchChangeWidth = -1;
        public int RamWatchNotesWidth = -1;

        // RamSearch Settings
        public bool AutoLoadRamSearch = false;
        public bool RamSearchSaveWindowPosition = true;
        public RecentFiles RecentSearches = new RecentFiles(8);
        public int RamSearchWndx = -1;   //Negative numbers will be ignored even with save window position set
        public int RamSearchWndy = -1;
        public int RamSearchWidth = -1;  //Negative numbers will be ignored
        public int RamSearchHeight = -1;
        public int RamSearchPreviousAs = 0;
        public bool RamSearchPreviewMode = true;
        public bool AlwaysExludeRamWatch = false;

        // HexEditor Settings
        public bool AutoLoadHexEditor = false;
        public bool HexEditorSaveWindowPosition = true;
        public int HexEditorWndx = -1;  //Negative numbers will be ignored even with save window position set
        public int HexEditorWndy = -1;
        public int HexEditorWidth = -1;
        public int HexEditorHeight = -1;

        // NESPPU Settings
        public bool AutoLoadNESPPU = false;
        public bool NESPPUSaveWindowPosition = true;
        public int NESPPUWndx = -1;
        public int NESPPUWndy = -1;

        // NESDebuger Settings
        public bool AutoLoadNESDebugger = false;
        public bool NESDebuggerSaveWindowPosition = true;
        public int NESDebuggerWndx = -1;
        public int NESDebuggerWndy = -1;
        public int NESDebuggerWidth = -1;
        public int NESDebuggerHeight = -1;

        // NESNameTableViewer Settings
        public bool AutoLoadNESNameTable = false;
        public bool NESNameTableSaveWindowPosition = true;
        public int NESNameTableWndx = -1;
        public int NESNameTableWndy = -1;

        // Cheats Dialog
        public bool AutoLoadCheats = false;
        public bool CheatsSaveWindowPosition = true;
        public bool DisableCheatsOnLoad = false;
        public bool LoadCheatFileByGame = true;
        public bool CheatsAutoSaveOnClose = true;
        public RecentFiles RecentCheats = new RecentFiles(8);
        public int CheatsWndx = -1;
        public int CheatsWndy = -1;
        public int CheatsWidth = -1;
        public int CheatsHeight = -1;
        public int CheatsNameWidth = -1;
        public int CheatsAddressWidth = -1;
        public int CheatsValueWidth = -1;
        public int CheatsDomainWidth = -1;
        public int CheatsOnWidth = -1;
        
        // NES Game Genie Encoder/Decoder
        public bool NESGGAutoload = false;
        public bool NESGGSaveWindowPosition = true;
        public int NESGGWndx = -1;
        public int NESGGWndy = -1;

        //Movie Settings
        public RecentFiles RecentMovies = new RecentFiles(8);

        // Client Hotkey Bindings
        public string HardResetBinding = "LeftShift+Tab"; //TODO: This needs to be Ctrl+R but how?
        public string FastForwardBinding = "J1 B6, Tab";
        public string RewindBinding = "J1 B5, LeftShift+R, RightShift+R";
        public string EmulatorPauseBinding = "Pause";
        public string FrameAdvanceBinding = "F";
        public string ScreenshotBinding = "F12";
        public string ToggleFullscreenBinding = "LeftAlt+Return, RightAlt+Return";
        public string QuickSave = "I";
        public string QuickLoad = "P";
        public string SelectSlot0 = "0";
        public string SelectSlot1 = "1";
        public string SelectSlot2 = "2";
        public string SelectSlot3 = "3";
        public string SelectSlot4 = "4";
        public string SelectSlot5 = "5";
        public string SelectSlot6 = "6";
        public string SelectSlot7 = "7";
        public string SelectSlot8 = "8";
        public string SelectSlot9 = "9";
        public string SaveSlot0 = "LeftShift+F10";
        public string SaveSlot1 = "LeftShift+F1";
        public string SaveSlot2 = "LeftShift+F2";
        public string SaveSlot3 = "LeftShift+F3";
        public string SaveSlot4 = "LeftShift+F4";
        public string SaveSlot5 = "LeftShift+F5";
        public string SaveSlot6 = "LeftShift+F6";
        public string SaveSlot7 = "LeftShift+F7";
        public string SaveSlot8 = "LeftShift+F8";
        public string SaveSlot9 = "LeftShift+F9";
        public string LoadSlot0 = "F10";
        public string LoadSlot1 = "F1";
        public string LoadSlot2 = "F2";
        public string LoadSlot3 = "F3";
        public string LoadSlot4 = "F4";
        public string LoadSlot5 = "F5";
        public string LoadSlot6 = "F6";
        public string LoadSlot7 = "F7";
        public string LoadSlot8 = "F8";
        public string LoadSlot9 = "F9";
        public string ToolBox = "T";
        public string SaveNamedState = "";
        public string LoadNamedState = "";
        public string PreviousSlot = "";
        public string NextSlot = "";
        public string RamWatch = "";
        public string RamSearch = "";
        public string RamPoke = "";
        public string HexEditor = "";
        public string LuaConsole = "";
        public string Cheats = "";
        public string OpenROM = "";
        public string CloseROM = "";
        public string FrameCounterBinding = "";
        public string FPSBinding = "";
        public string LagCounterBinding = "";
        public string InputDisplayBinding = "";
        
        // SMS / GameGear Settings
        public bool SmsEnableFM = true;
        public bool SmsAllowOverlock = false;
        public bool SmsForceStereoSeparation = false;

        public string SmsReset = "Tab";
        public string SmsPause = "J1 B10, Space";
        public SMSControllerTemplate[] SMSController = new SMSControllerTemplate[2];

        // PCEngine Settings
        public PCEControllerTemplate[] PCEController = new PCEControllerTemplate[5];

        // Genesis Settings
        public string GenP1Up = "J1 Up, UpArrow";
        public string GenP1Down = "J1 Down, DownArrow";
        public string GenP1Left = "J1 Left, LeftArrow";
        public string GenP1Right = "J1 Right, RightArrow";
        public string GenP1A = "J1 B1, Z";
        public string GenP1B = "J1 B2, X";
        public string GenP1C = "J1 B9, C";
        public string GenP1Start = "J1 B10, Return";

        //GameBoy Settings
        public NESControllerTemplate GameBoyController = new NESControllerTemplate(true);

		public string NESReset = "Backspace";
        public NESControllerTemplate[] NESController = new NESControllerTemplate[4];
    }

    public class SMSControllerTemplate
    {
        public string Up;
        public string Down;
        public string Left;
        public string Right;
        public string B1;
        public string B2;
        public bool Enabled;
        public SMSControllerTemplate() { }
        public SMSControllerTemplate(bool defaults)
        {
            if (defaults)
            {
                Enabled = true;
                Up = "J1 Up, UpArrow";
                Down = "J1 Down, DownArrow";
                Left = "J1 Left, LeftArrow";
                Right = "J1 Right, RightArrow";
                B1 = "J1 B1, Z";
                B2 = "J1 B2, X";
            }
            else
            {
                Enabled = false;
                Up = "";
                Down = "";
                Right = "";
                Left = "";
                B1 = "";
                B2 = "";                
            }                        
        }
    }

    public class PCEControllerTemplate
    {
        public string Up;
        public string Down;
        public string Left;
        public string Right;
        public string I;
        public string II;
        public string Run;
        public string Select;
        public bool Enabled;
        public PCEControllerTemplate() { }
        public PCEControllerTemplate(bool defaults)
        {
            if (defaults)
            {
                Enabled = true;
                Up = "J1 Up, UpArrow";
                Down = "J1 Down, DownArrow";
                Left = "J1 Left, LeftArrow";
                Right = "J1 Right, RightArrow";
                I = "J1 B1, Z";
                II = "J1 B2, X";
                Run = "J1 B10, C";
                Select = "J1 B9, V";
            }
            else
            {
                Enabled = false;
                Up = "";
                Down = "";
                Right = "";
                Left = "";
                I = "";
                II = "";
                Run = "";
                Select = "";
            }
        }
    }

    public class NESControllerTemplate
    {
        public string Up;
        public string Down;
        public string Left;
        public string Right;
        public string A;
        public string B;
        public string Start;
        public string Select;
        public bool Enabled;
        public NESControllerTemplate() { }
        public NESControllerTemplate(bool defaults)
        {
            if (defaults)
            {
                Enabled = true;
                Up = "J1 Up, UpArrow";
                Down = "J1 Down, DownArrow";
                Left = "J1 Left, LeftArrow";
                Right = "J1 Right, RightArrow";
                A = "J1 B1, X";
                B = "J1 B2, Z";
                Start = "J1 B10, Return";
                Select = "J1 B9, Space";
            }
            else
            {
                Enabled = false;
                Up = "";
                Down = "";
                Right = "";
                Left = "";
                A = "";
                B = "";
                Start = "";
                Select = "";
            }
        }
    }
}