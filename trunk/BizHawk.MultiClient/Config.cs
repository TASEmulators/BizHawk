namespace BizHawk.MultiClient
{
    public class Config
    {
        // General Client Settings
        public bool SoundEnabled = true;
        public int TargetZoomFactor = 2;
        public string LastRomPath = ".";
        public bool AutoLoadMostRecentRom = false;    //TODO: eventually make a class or struct for all the auto-loads, which will include recent roms, movies, etc, as well as autoloading any modeless dialog
        public RecentFiles RecentRoms = new RecentFiles(8);

        // RamWatch Settings
        public bool AutoLoadRamWatch = false;
        public RecentFiles RecentWatches = new RecentFiles(8);
        public int RamWatchWndx = -1;   //Negative numbers will be ignored even with save window position set
        public int RamWatchWndy = -1;
        public int RamWatchWidth = -1;  //Negative numbers will be ignored
        public int RamWatchHeight = -1; 

        // Client Hotkey Bindings
        public string HardResetBinding = "LeftShift+Tab";
        public string FastForwardBinding = "J1 B6";
        public string RewindBinding = "J1 B5";
        public string EmulatorPauseBinding = "LeftControl+Space";
        public string FrameAdvanceBinding = "F";
        public string ScreenshotBinding = "F12";

        // SMS / GameGear Settings
        public string SmsReset = "Tab";
        public string SmsPause = "J1 B10, Space";
        public string SmsP1Up = "J1 Up, UpArrow";
        public string SmsP1Left = "J1 Left, LeftArrow";
        public string SmsP1Right = "J1 Right, RightArrow";
        public string SmsP1Down = "J1 Down, DownArrow";
        public string SmsP1B1 = "J1 B1, Z";
        public string SmsP1B2 = "J1 B2, X";
        public string SmsP2Up = "J2 Up";
        public string SmsP2Left = "J2 Left";
        public string SmsP2Right = "J2 Right";
        public string SmsP2Down = "J2 Down";
        public string SmsP2B1 = "J2 B1";
        public string SmsP2B2 = "J2 B2";

        // PCEngine Settings
        public string PCEUp = "J1 Up, UpArrow";
        public string PCEDown = "J1 Down, DownArrow";
        public string PCELeft = "J1 Left, LeftArrow";
        public string PCERight = "J1 Right, RightArrow";
        public string PCEBII = "J1 B1, Z";
        public string PCEBI = "J1 B2, X";
        public string PCESelect = "J1 B9, Space";
        public string PCERun = "J1 B10, Return";

        // Genesis Settings
        public string GenP1Up = "J1 Up, UpArrow";
        public string GenP1Down = "J1 Down, DownArrow";
        public string GenP1Left = "J1 Left, LeftArrow";
        public string GenP1Right = "J1 Right, RightArrow";
        public string GenP1A = "J1 B1, Z";
        public string GenP1B = "J1 B2, X";
        public string GenP1C = "J1 B9, C";
        public string GenP1Start = "J1 B10, Return";
    }
}