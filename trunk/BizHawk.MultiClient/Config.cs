using System.IO;
using System.Drawing;

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
			SNESController[0] = new SNESControllerTemplate(true);
			SNESController[1] = new SNESControllerTemplate(false);
			SNESController[2] = new SNESControllerTemplate(false);
			SNESController[3] = new SNESControllerTemplate(false);
			GBController[0] = new GBControllerTemplate(true);
			GBAutoController[0] = new GBControllerTemplate(true);
			TI83Controller[0] = new TI83ControllerTemplate(true);

			GenesisController[0] = new GenControllerTemplate(true);
			GenesisAutoController[0] = new GenControllerTemplate(false);

			Atari2600Controller[0] = new SingleButtonJoyStickTemplate(true);
			Atari2600Controller[1] = new SingleButtonJoyStickTemplate(false);
			Atari2600AutoController[0] = new SingleButtonJoyStickTemplate(false);
			Atari2600AutoController[1] = new SingleButtonJoyStickTemplate(false);
			Atari2600ConsoleButtons[0] = new Atari2600ConsoleButtonsTemplate(true);

			NESAutoController[0] = new NESControllerTemplate(false);
			NESAutoController[1] = new NESControllerTemplate(false);
			NESAutoController[2] = new NESControllerTemplate(false);
			NESAutoController[3] = new NESControllerTemplate(false);

			SMSAutoController[0] = new SMSControllerTemplate(false);
			SMSAutoController[1] = new SMSControllerTemplate(false);

			PCEAutoController[0] = new PCEControllerTemplate(false);
			PCEAutoController[1] = new PCEControllerTemplate(false);
			PCEAutoController[2] = new PCEControllerTemplate(false);
			PCEAutoController[3] = new PCEControllerTemplate(false);
			PCEAutoController[4] = new PCEControllerTemplate(false);

			SNESAutoController[0] = new SNESControllerTemplate(false);
			SNESAutoController[1] = new SNESControllerTemplate(false);
			SNESAutoController[2] = new SNESControllerTemplate(false);
			SNESAutoController[3] = new SNESControllerTemplate(false);

			ColecoController = new ColecoVisionControllerTemplate(true);

			C64Joysticks[0] = new SingleButtonJoyStickTemplate(true, true);
			C64Joysticks[1] = new SingleButtonJoyStickTemplate(false);
			C64AutoJoysticks[0] = new SingleButtonJoyStickTemplate(false);
			C64AutoJoysticks[1] = new SingleButtonJoyStickTemplate(false);

			NESConsoleButtons = new NESConsoleButtonTemplate();
			SNESConsoleButtons = new NESConsoleButtonTemplate();
			SMSConsoleButtons = new SMSConsoleButtonTemplate();
			GenesisConsoleButtons = new GenConsoleButtonTemplate();
		}

		// Directories
		public bool UseRecentForROMs = false;
		public string LastRomPath = ".";
		public string BasePath = ".";

		public string BaseINTV = Path.Combine(".", "Intellivision");
		public string PathINTVROMs = ".";
		public string PathINTVSavestates = Path.Combine(".", "State");
		public string PathINTVSaveRAM = Path.Combine(".", "SaveRAM");
		public string PathINTVScreenshots = Path.Combine(".", "Screenshots");
		public string PathINTVCheats = Path.Combine(".", "Cheats");

		public string BaseNES = Path.Combine(".", "NES");
		public string PathNESROMs = ".";
		public string PathNESSavestates = Path.Combine(".", "State");
		public string PathNESSaveRAM = Path.Combine(".", "SaveRAM");
		public string PathNESScreenshots = Path.Combine(".", "Screenshots");
		public string PathNESCheats = Path.Combine(".", "Cheats");
		public string PathNESPalette = Path.Combine(".", "Palettes");

		public string BaseSNES = Path.Combine(".", "SNES");
		public string PathSNESROMs = ".";
		public string PathSNESSavestates = Path.Combine(".", "State");
		public string PathSNESSaveRAM = Path.Combine(".", "SaveRAM");
		public string PathSNESScreenshots = Path.Combine(".", "Screenshots");
		public string PathSNESCheats = Path.Combine(".", "Cheats");
		public string PathSNESFirmwares = Path.Combine(".", "Firmwares");

		public string BaseSMS = Path.Combine(".", "SMS");
		public string PathSMSROMs = ".";
		public string PathSMSSavestates = Path.Combine(".", "State");
		public string PathSMSSaveRAM = Path.Combine(".", "SaveRAM");
		public string PathSMSScreenshots = Path.Combine(".", "Screenshots");
		public string PathSMSCheats = Path.Combine(".", "Cheats");

		public string BaseGG = Path.Combine(".", "Game Gear");
		public string PathGGROMs = ".";
		public string PathGGSavestates = Path.Combine(".", "State");
		public string PathGGSaveRAM = Path.Combine(".", "SaveRAM");
		public string PathGGScreenshots = Path.Combine(".", "Screenshots");
		public string PathGGCheats = Path.Combine(".", "Cheats");

		public string BaseSG = Path.Combine(".", "SG-1000");
		public string PathSGROMs = ".";
		public string PathSGSavestates = Path.Combine(".", "State");
		public string PathSGSaveRAM = Path.Combine(".", "SaveRAM");
		public string PathSGScreenshots = Path.Combine(".", "Screenshots");
		public string PathSGCheats = Path.Combine(".", "Cheats");

		public string BaseGenesis = Path.Combine(".", "Genesis");
		public string PathGenesisROMs = ".";
		public string PathGenesisSavestates = Path.Combine(".", "State");
		public string PathGenesisSaveRAM = Path.Combine(".", "SaveRAM");
		public string PathGenesisScreenshots = Path.Combine(".", "Screenshots");
		public string PathGenesisCheats = Path.Combine(".", "Cheats");

		public string BasePCE = Path.Combine(".", "PC Engine");
		public string PathPCEROMs = ".";
		public string PathPCESavestates = Path.Combine(".", "State");
		public string PathPCESaveRAM = Path.Combine(".", "SaveRAM");
		public string PathPCEScreenshots = Path.Combine(".", "Screenshots");
		public string PathPCECheats = Path.Combine(".", "Cheats");

		public string BaseGameboy = Path.Combine(".", "Gameboy");
		public string PathGBROMs = ".";
		public string PathGBSavestates = Path.Combine(".", "State");
		public string PathGBSaveRAM = Path.Combine(".", "SaveRAM");
		public string PathGBScreenshots = Path.Combine(".", "Screenshots");
		public string PathGBCheats = Path.Combine(".", "Cheats");
		public string PathGBPalettes = Path.Combine(".", "Palettes");

		public string BaseTI83 = Path.Combine(".", "TI83");
		public string PathTI83ROMs = ".";
		public string PathTI83Savestates = Path.Combine(".", "State");
		public string PathTI83SaveRAM = Path.Combine(".", "SaveRAM");
		public string PathTI83Screenshots = Path.Combine(".", "Screenshots");
		public string PathTI83Cheats = Path.Combine(".", "Cheats");

		public string BaseAtari = Path.Combine(".", "Atari");
		public string PathAtariROMs = ".";
		public string PathAtariSavestates = Path.Combine(".", "State");
		public string PathAtariSaveRAM = Path.Combine(".", "SaveRAM");
		public string PathAtariScreenshots = Path.Combine(".", "Screenshots");
		public string PathAtariCheats = Path.Combine(".", "Cheats");

		public string BaseC64 = Path.Combine(".", "C64");
		public string PathC64ROMs = ".";
		public string PathC64Savestates = Path.Combine(".", "State");
		public string PathC64Screenshots = Path.Combine(".", "Screenshots");
		public string PathC64Cheats = Path.Combine(".", "Cheats");
		public string PathC64Firmwares = Path.Combine(".", "Firmwares");

		public string BasePSX = Path.Combine(".", "PSX");
		public string PathPSXROMs = ".";
		public string PathPSXSavestates = Path.Combine(".", "State");
		public string PathPSXSaveRAM = Path.Combine(".", "SaveRAM");
		public string PathPSXScreenshots = Path.Combine(".", "Screenshots");
		public string PathPSXCheats = Path.Combine(".", "Cheats");
		public string PathPSXFirmwares = Path.Combine(".", "Firmwares");

		public string MoviesPath = Path.Combine(".", "Movies");
		public string MoviesBackupPath = Path.Combine(".", "Movies", "backup");
		public string LuaPath = Path.Combine(".", "Lua");
		public string WatchPath = ".";
		public string AVIPath = ".";
		public string LogPath = ".";

		//BIOS Paths
		public string PathPCEBios = Path.Combine(".", "PCECDBios.pce");
		public string PathINTVGROM = Path.Combine(".", "grom.bin");
		public string PathINTVEROM = Path.Combine(".", "erom.bin");
		public string PathFDSBios = Path.Combine(".", "disksys.rom");
		public string PathAtari7800NTSCBIOS = Path.Combine(".", "7800NTSCBIOS.bin");
		public string PathAtari7800PALBIOS = Path.Combine(".", "7800PALBIOS.bin");
		public string PathAtari7800HighScoreBIOS = Path.Combine(".", "7800highscore.bin");

		public string FFMpegPath = "%exe%/dll/ffmpeg.exe";

		// General Client Settings
		public bool StackOSDMessages = true;
		public int TargetZoomFactor = 2;
		public int TargetDisplayFilter = 0;
		public bool AutoLoadMostRecentRom = false;
		public RecentFiles RecentRoms = new RecentFiles(8);
		public bool PauseWhenMenuActivated = true;
		public bool SaveWindowPosition = true;
		public bool StartPaused = false;
		public int MainWndx = -1; //Negative numbers will be ignored
		public int MainWndy = -1;
		public bool RunInBackground = true;
		public bool AcceptBackgroundInput = false;
		public bool SingleInstanceMode = false;
		public bool AllowUD_LR = false;
		public bool ShowContextMenu = true;
		public bool EnableBackupMovies = true;
		public bool HotkeyConfigAutoTab = true;
		public bool InputConfigAutoTab = true;
		public bool ShowLogWindow = false;
		public bool BackupSavestates = true;
		public bool BackupSaveram = true;
		public bool AutoSavestates = false;
		public bool SaveScreenshotWithStates = true;
		public int AutofireOn = 1;
		public int AutofireOff = 1;
		public bool AutofireLagFrames = true;
		public int SaveSlot = 0; //currently selected savestate slot
		public bool AutoLoadLastSaveSlot = false;
		public bool WIN32_CONSOLE = true;
		public bool SkipLagFrame = false;
		public string MovieExtension = "bkm";
		public bool SupressAskSave = false;
		public bool AVI_CaptureOSD = false;
		public bool Screenshot_CaptureOSD = false;

		// Run-Control settings
		public int FrameProgressDelayMs = 500; //how long until a frame advance hold turns into a frame progress?
		public int FrameSkip = 4;
		public int SpeedPercent = 100;
		public int SpeedPercentAlternate = 400;
		public bool LimitFramerate = true;
		public bool AutoMinimizeSkipping = true;
		public bool DisplayVSync = false;
		public bool RewindEnabled = true;

		// Display options
		public int MessagesColor = -1;
		public int AlertMessageColor = -65536;
		public int LastInputColor = -23296;
		public int MovieInput = -8355712;
		public bool DisplayFPS = false;
		public int DispFPSx = 0;
		public int DispFPSy = 0;
		public int DispFPSanchor = 0;	//0 = UL, 1 = UR, 2 = DL, 3 = DR
		public bool DisplayFrameCounter = false;
		public int DispFrameCx = 0;
		public int DispFrameCy = 14;
		public int DispFrameanchor = 0;
		public bool DisplayLagCounter = false;
		public int DispLagx = 0;
		public int DispLagy = 42;
		public int DispLaganchor = 0;
		public bool DisplayInput = false;
		public int DispInpx = 0;
		public int DispInpy = 28;
		public int DispInpanchor = 0;
		public bool DisplayRerecordCount = false;
		public int DispRecx = 0;
		public int DispRecy = 56;
		public int DispRecanchor = 0;
		public int DispMultix = 36;
		public int DispMultiy = 0;
		public int DispMultianchor = 0;
		public bool DisplayGDI = false;
		public bool SuppressGui = false;
		public bool DisplayStatusBar = true;
		public int DispRamWatchx = 0;
		public int DispRamWatchy = 70;
		public bool DisplayRamWatch = false;
		public bool ShowMenuInFullscreen = false;
		public int DispMessagex = 3;
		public int DispMessagey = 0;
		public int DispMessageanchor = 2;
		public int DispAutoholdx = 0;
		public int DispAutoholdy = 0;
		public int DispAutoholdanchor = 1;
		public bool DispBlurry = false; // make display look ugly

		// Sound options
		public bool SoundEnabled = true;
		public bool MuteFrameAdvance = true;
		public int SoundVolume = 100; //Range 0-100
		public bool SoundThrottle = false;

		// Log Window
		public bool LogWindowSaveWindowPosition = true;
		public int LogWindowWndx = -1;
		public int LogWindowWndy = -1;
		public int LogWindowWidth = -1;
		public int LogWindowHeight = -1;

		// Lua Console
		public RecentFiles RecentLua = new RecentFiles(8);
		public RecentFiles RecentLuaSession = new RecentFiles(8);
		public bool AutoLoadLuaSession = false;
		public bool AutoLoadLuaConsole = false;
		public bool LuaConsoleSaveWindowPosition = true;
		public int LuaConsoleWndx = -1;   //Negative numbers will be ignored even with save window position set
		public int LuaConsoleWndy = -1;
		public int LuaConsoleWidth = -1;
		public int LuaConsoleHeight = -1;
		public bool DisableLuaScriptsOnLoad = false;

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
		public bool RamWatchShowDiffColumn = false;
		public bool RamWatchShowDomainColumn = true;
		public int RamWatchAddressWidth = -1;
		public int RamWatchValueWidth = -1;
		public int RamWatchPrevWidth = -1;
		public int RamWatchChangeWidth = -1;
		public int RamWatchDiffWidth = -1;
		public int RamWatchNotesWidth = -1;
		public int RamWatchDomainWidth = -1;

		public int RamWatchAddressIndex = 0;
		public int RamWatchValueIndex = 1;
		public int RamWatchPrevIndex = 2;
		public int RamWatchChangeIndex = 3;
		public int RamWatchDiffIndex = 4;
		public int RamWatchDomainIndex = 5;
		public int RamWatchNotesIndex = 6;
		public int RamWatchPrev_Type = 1;

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
		public bool AlwaysExcludeRamWatch = false;
		public int RamSearchAddressWidth = -1;
		public int RamSearchValueWidth = -1;
		public int RamSearchPrevWidth = -1;
		public int RamSearchChangesWidth = -1;
		public int RamSearchAddressIndex = 0;
		public int RamSearchValueIndex = 1;
		public int RamSearchPrevIndex = 2;
		public int RamSearchChangesIndex = 3;

		// HexEditor Settings
		public bool AutoLoadHexEditor = false;
		public bool HexEditorSaveWindowPosition = true;
		public int HexEditorWndx = -1;  //Negative numbers will be ignored even with save window position set
		public int HexEditorWndy = -1;
		public int HexEditorWidth = -1;
		public int HexEditorHeight = -1;
		public bool HexEditorBigEndian = false;
		public int HexEditorDataSize = 1;
		//Hex Editor Colors
		public Color HexBackgrndColor = Color.FromName("Control");
		public Color HexForegrndColor = Color.FromName("ControlText");
		public Color HexMenubarColor = Color.FromName("Control");
		public Color HexFreezeColor = Color.LightBlue;
		public Color HexHighlightColor = Color.Pink;
		public Color HexHighlightFreezeColor = Color.Violet;

		//Trace Logger Settings
		public bool TraceLoggerAutoLoad = false;
		public bool TraceLoggerSaveWindowPosition = true;
		public int TraceLoggerMaxLines = 100000;
		public int TraceLoggerWndx = -1;
		public int TraceLoggerWndy = -1;
		public int TraceLoggerWidth = -1;
		public int TraceLoggerHeight = -1;

		// Video dumping settings
		public string VideoWriter = "";
		public int JMDCompression = 3;
		public int JMDThreads = 3;
		public string FFmpegFormat = "";
		public string FFmpegCustomCommand = "-c:a foo -c:v bar -f baz";
		public string AVICodecToken = "";
		public int GifWriterFrameskip = 3;
		public int GifWriterDelay = -1;

		// NESPPU Settings
		public bool AutoLoadNESPPU = false;
		public bool NESPPUSaveWindowPosition = true;
		public int NESPPUWndx = -1;
		public int NESPPUWndy = -1;
		public int NESPPURefreshRate = 4;

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
		public int NESNameTableRefreshRate = 4;

		// NES Graphics settings
		public bool NESAllowMoreThanEightSprites = false;
		public bool NESClipLeftAndRight = false;
		public bool NESAutoLoadPalette = true;
		public bool NESDispBackground = true;
		public bool NESDispSprites = true;
		public int NESBackgroundColor = 0;
		public string NESPaletteFile = "";
		public int NESTopLine = 8;
		public int NESBottomLine = 231;

		// gb gpu view settings
		public bool AutoLoadGBGPUView = false;
		public bool GBGPUViewSaveWindowPosition = true;
		public int GBGPUViewWndx = -1;
		public int GBGPUViewWndy = -1;


		// SNES Graphics Debugger Dialog Settings
		public bool AutoLoadSNESGraphicsDebugger = false;
		public bool SNESGraphicsDebuggerSaveWindowPosition = true;
		public int SNESGraphicsDebuggerWndx = -1;
		public int SNESGraphicsDebuggerWndy = -1;
		public int SNESGraphicsDebuggerRefreshRate = 4;
		public bool SNESGraphicsUseUserBackdropColor = false;
		public int SNESGraphicsUserBackdropColor = -1;

		// SNES Graphics settings
		//bsnes allows the layers to be enabled for each priority level.
		//this may not be important for the bg (there are two priority levels)
		//but it may be useful for OBJ, so we might want to control them separately
		public bool SNES_ShowBG1_0 = true;
		public bool SNES_ShowBG2_0 = true;
		public bool SNES_ShowBG3_0 = true;
		public bool SNES_ShowBG4_0 = true;
		public bool SNES_ShowBG1_1 = true;
		public bool SNES_ShowBG2_1 = true;
		public bool SNES_ShowBG3_1 = true;
		public bool SNES_ShowBG4_1 = true;
		public bool SNES_ShowOBJ1 = true;
		public bool SNES_ShowOBJ2 = true;
		public bool SNES_ShowOBJ3 = true;
		public bool SNES_ShowOBJ4 = true;

		// PCE Graphics settings
		public bool PCEDispBG1 = true;
		public bool PCEDispOBJ1 = true;
		public bool PCEDispBG2 = true;
		public bool PCEDispOBJ2 = true;

		// PCE BG Viewer settings
		public bool PCEBGViewerSaveWIndowPosition = true;
		public bool PCEBGViewerAutoload = false;
		public int PCEBGViewerWndx = -1;
		public int PCEBGViewerWndy = -1;
		public int PCEBGViewerRefreshRate = 16;

		// SMS Graphics settings
		public bool SMSDispBG = true;
		public bool SMSDispOBJ = true;

		//GB Debugger settings
		public bool AutoloadGBDebugger = false;
		public bool GBDebuggerSaveWindowPosition = true;
		public bool GameBoySkipBIOS = true;

		// Cheats Dialog
		public bool Cheats_ValuesAsHex = true;
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
		public int CheatsCompareWidth = -1;
		public int CheatsDomainWidth = -1;
		public int CheatsOnWidth = -1;
		public int CheatsNameIndex = 0;
		public int CheatsAddressIndex = 1;
		public int CheatsValueIndex = 2;
		public int CheatsCompareIndex = 3;
		public int CheatsOnIndex = 4;
		public int CheatsDomainIndex = 5;

		// TAStudio Dialog
		public bool TAStudioSaveWindowPosition = true;
		public bool AutoloadTAStudio = false;
		public int TASWndx = -1;
		public int TASWndy = -1;
		public int TASWidth = -1;
		public int TASHeight = -1;
		public bool TASUpdatePads = true;

		// VirtualPad Dialog
		public bool VirtualPadSaveWindowPosition = true;
		public bool AutoloadVirtualPad = false;
		public bool VirtualPadSticky = true;
		public int VPadWndx = -1;
		public int VPadWndy = -1;

		// NES Game Genie Encoder/Decoder
		public bool NESGGAutoload = false;
		public bool NESGGSaveWindowPosition = true;
		public int NESGGWndx = -1;
		public int NESGGWndy = -1;

		//Movie Settings
		public RecentFiles RecentMovies = new RecentFiles(8);
		public bool AutoLoadMostRecentMovie = false;
		public bool BindSavestatesToMovies = true;
		public string DefaultAuthor = "default user";
		public bool UseDefaultAuthor = true;
		public bool DisplaySubtitles = true;

		//Play Movie Dialog
		public bool PlayMovie_IncludeSubdir = true;
		public bool PlayMovie_ShowStateFiles = false;
		public bool PlayMovie_MatchGameName = false;

		//TI83
		public bool TI83autoloadKeyPad = true;
		public bool TI83KeypadSaveWindowPosition = true;
		public int TI83KeyPadWndx = -1;
		public int TI83KeyPadWndy = -1;
		public bool TI83ToolTips = true;

		// Client Hotkey Bindings
		public string ToggleBackgroundInput = "";
		public string IncreaseSpeedBinding = "Equals";
		public string DecreaseSpeedBinding = "Minus";
		public string HardResetBinding = "";
		public string RebootCoreResetBinding = "Ctrl+R";
		public string FastForwardBinding = "Tab, X1 RightShoulder";
		public string RewindBinding = "Shift+R, X1 LeftShoulder";
		public string EmulatorPauseBinding = "Pause";
		public string FrameAdvanceBinding = "F";
		public string TurboBinding = "";
		public string MaxTurboBinding = "Shift+Tab";
		public string ScreenshotBinding = "F12";
		public string ToggleFullscreenBinding = "Alt+Return";
		public string QuickSave = "I";
		public string QuickLoad = "P";
		public string SelectSlot0 = "D0";
		public string SelectSlot1 = "D1";
		public string SelectSlot2 = "D2";
		public string SelectSlot3 = "D3";
		public string SelectSlot4 = "D4";
		public string SelectSlot5 = "D5";
		public string SelectSlot6 = "D6";
		public string SelectSlot7 = "D7";
		public string SelectSlot8 = "D8";
		public string SelectSlot9 = "D9";
		public string SaveSlot0 = "Shift+F10";
		public string SaveSlot1 = "Shift+F1";
		public string SaveSlot2 = "Shift+F2";
		public string SaveSlot3 = "Shift+F3";
		public string SaveSlot4 = "Shift+F4";
		public string SaveSlot5 = "Shift+F5";
		public string SaveSlot6 = "Shift+F6";
		public string SaveSlot7 = "Shift+F7";
		public string SaveSlot8 = "Shift+F8";
		public string SaveSlot9 = "Shift+F9";
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
		public string TASTudio = "";
		public string OpenROM = "Ctrl+O";
		public string CloseROM = "Ctrl+W";
		public string FrameCounterBinding = "";
		public string FPSBinding = "";
		public string LagCounterBinding = "";
		public string InputDisplayBinding = "";
		public string ReadOnlyToggleBinding = "Q";
		public string PlayMovieBinding = "";
		public string RecordMovieBinding = "";
		public string StopMovieBinding = "";
		public string PlayBeginningBinding = "";
		public string VolUpBinding = "";
		public string VolDownBinding = "";
		public string SoftResetBinding = "";
		public string ToggleMultiTrack = "";
		public string MTRecordAll = "";
		public string MTRecordNone = "";
		public string MTIncrementPlayer = "";
		public string MTDecrementPlayer = "";
		public string AVIRecordBinding = "";
		public string AVIStopBinding = "";
		public string ToggleMenuBinding = "";
		public string IncreaseWindowSize = "Alt+UpArrow";
		public string DecreaseWindowSize = "Alt+DownArrow";
		public string AutoholdBinding = "";
		public string AutoholdClear = "";
		public string ToggleSNESBG1Binding = "";
		public string ToggleSNESBG2Binding = "";
		public string ToggleSNESBG3Binding = "";
		public string ToggleSNESBG4Binding = "";
		public string ToggleSNESOBJ1Binding = "";
		public string ToggleSNESOBJ2Binding = "";
		public string ToggleSNESOBJ3Binding = "";
		public string ToggleSNESOBJ4Binding = "";
		public string SaveMovieBinding = "";

		// NES Sound settings
		public bool NESEnableSquare1 = true;
		public bool NESEnableSquare2 = true;
		public bool NESEnableTriangle = true;
		public bool NESEnableNoise = true;
		public bool NESEnableDMC = true;

		// SMS / GameGear Settings
		public bool SmsEnableFM = true;
		public bool SmsAllowOverlock = false;
		public bool SmsForceStereoSeparation = false;
		public bool SmsSpriteLimit = false;
		public bool GGShowClippedRegions = false;
		public bool GGHighlightActiveDisplayRegion = false;

		public SMSControllerTemplate[] SMSController = new SMSControllerTemplate[2];
		public SMSControllerTemplate[] SMSAutoController = new SMSControllerTemplate[2];
		public SMSConsoleButtonTemplate SMSConsoleButtons = new SMSConsoleButtonTemplate();

		// PCEngine Settings
		public bool PceSpriteLimit = false;
		public bool PceEqualizeVolume = false;
		public bool PceArcadeCardRewindHack = true;
		public PCEControllerTemplate[] PCEController = new PCEControllerTemplate[5];
		public PCEControllerTemplate[] PCEAutoController = new PCEControllerTemplate[5];

		// Genesis Settings
		public GenControllerTemplate[] GenesisController = new GenControllerTemplate[1];
		public GenControllerTemplate[] GenesisAutoController = new GenControllerTemplate[1];
		public GenConsoleButtonTemplate GenesisConsoleButtons = new GenConsoleButtonTemplate();

		//Atari 2600 Settings
		public SingleButtonJoyStickTemplate[] Atari2600Controller = new SingleButtonJoyStickTemplate[2];
		public SingleButtonJoyStickTemplate[] Atari2600AutoController = new SingleButtonJoyStickTemplate[2];
		public Atari2600ConsoleButtonsTemplate[] Atari2600ConsoleButtons = new Atari2600ConsoleButtonsTemplate[1];
		public bool Atari2600_BW = false;
		public bool Atari2600_LeftDifficulty = true;
		public bool Atari2600_RightDifficulty = true;

		//ColecoVision
		public ColecoVisionControllerTemplate ColecoController = new ColecoVisionControllerTemplate(true);

		//NES settings
		public NESControllerTemplate[] NESController = new NESControllerTemplate[4];
		public NESControllerTemplate[] NESAutoController = new NESControllerTemplate[4];
		public NESConsoleButtonTemplate NESConsoleButtons = new NESConsoleButtonTemplate();

		//SNES settings
		public SNESControllerTemplate[] SNESController = new SNESControllerTemplate[4];
		public SNESControllerTemplate[] SNESAutoController = new SNESControllerTemplate[4];
		public NESConsoleButtonTemplate SNESConsoleButtons = new NESConsoleButtonTemplate();

		//TI 83 settings
		public TI83ControllerTemplate[] TI83Controller = new TI83ControllerTemplate[1];

		//GB settings
		public GBControllerTemplate[] GBController = new GBControllerTemplate[1];
		public GBControllerTemplate[] GBAutoController = new GBControllerTemplate[1];
		public bool GB_ForceDMG = false;
		public bool GB_GBACGB = false;
		public bool GB_MulticartCompat = false;
		public string GB_PaletteFile = "";
		public bool GB_AsSGB = false;

		//Commodore 64 Settings
		public SingleButtonJoyStickTemplate[] C64Joysticks = new SingleButtonJoyStickTemplate[2];
		public SingleButtonJoyStickTemplate[] C64AutoJoysticks = new SingleButtonJoyStickTemplate[2]; 
		public C64KeyboardTemplate C64Keyboard = new C64KeyboardTemplate();
		public C64KeyboardTemplate C64AutofireKeyboard = new C64KeyboardTemplate();

		//GIF Animator Settings
		public int GifAnimatorNumFrames;
		public int GifAnimatorFrameSkip;
		public int GifAnimatorSpeed;
		public bool GifAnimatorReversable;

		//LuaWriter Settings
		public int LuaDefaultTextColor = -16777216;
		public bool LuaDefaultTextBold = false;
		public int LuaWriterBackColor = -1;

		public int LuaKeyWordColor = -16776961;
		public bool LuaKeyWordBold = false;
		public int LuaCommentColor = -16744448;
		public bool LuaCommentBold = false;
		public int LuaStringColor = -8355712;
		public bool LuaStringBold = false;
		public int LuaSymbolColor = -16777216;
		public bool LuaSymbolBold = false;
		public int LuaLibraryColor = -16711681;
		public bool LuaLibraryBold = false;
		public int LuaDecimalColor = -23296;
		public bool LuaDecimalBold = false;
		public float LuaWriterFontSize = 11;
		public string LuaWriterFont = "Courier New";
		public float LuaWriterZoom = 1;
		public bool LuaWriterStartEmpty = false;

		//Atari 2600 Settings
		public bool Atari2600_ShowBG = true;
		public bool Atari2600_ShowPlayer1 = true;
		public bool Atari2600_ShowPlayer2 = true;
		public bool Atari2600_ShowMissle1 = true;
		public bool Atari2600_ShowMissle2 = true;
		public bool Atari2600_ShowBall = true;
		public bool Atari2600_ShowPlayfield = true;
	}

	public class NESConsoleButtonTemplate
	{
		public string Reset = "";
		public string Power = "";
		//public string FDS_Eject = ""; //Not supporting this for now
		//public string FDS_Insert = ""; //Need to support inserting of disk 1,2,3,4,etc so not supporting this for now
		//public string VS_Coin_1 = ""; //Not supported yet
		//public string VS_Coin_2 = ""; //Not supported yet
	}

	public class SMSConsoleButtonTemplate
	{
		public string Reset = "C";
		public string Pause = "V, X1 Start";
	}

	public class GenConsoleButtonTemplate
	{
		public string Reset = "";
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
				Up = "UpArrow, X1 DpadUp, X1 LStickUp";
				Down = "DownArrow, X1 DpadDown, X1 LStickDown";
				Left = "LeftArrow, X1 DpadLeft, X1 LStickLeft";
				Right = "RightArrow, X1 DpadRight, X1 LStickRight";
				B1 = "Z, X1 A";
				B2 = "X, X1 B";
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
				Up = "UpArrow, X1 DpadUp, X1 LStickUp";
				Down = "DownArrow, X1 DpadDown, X1 LStickDown";
				Left = "LeftArrow, X1 DpadLeft, X1 LStickLeft";
				Right = "RightArrow, X1 DpadRight, X1 LStickRight";
				I = "Z, X1 B";
				II = "X, X1 A";
				Run = "C, X1 Start";
				Select = "V, X1 Back";
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

	public class SNESControllerTemplate
	{
		public string Up;
		public string Down;
		public string Left;
		public string Right;
		public string A;
		public string B;
		public string Start;
		public string Select;
		public string Y, X, L, R;
		public bool Enabled;
		public SNESControllerTemplate() { }
		public SNESControllerTemplate(bool defaults)
		{
			if (defaults)
			{
				Enabled = true;
				Up = "UpArrow, X1 DpadUp, X1 LStickUp";
				Down = "DownArrow, X1 DpadDown, X1 LStickDown";
				Left = "LeftArrow, X1 DpadLeft, X1 LStickLeft";
				Right = "RightArrow, X1 DpadRight, X1 LStickRight";
				A = "X, X1 B";
				B = "Z, X1 A";
				Y = "A, X1 X";
				X = "S, X1 Y";
				L = "W, X1 LeftTrigger";
				R = "E, X1 RightTrigger";
				Start = "Return, X1 Start";
				Select = "Space, X1 Back";
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
				X = Y = L = R = "";
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
		public string Select;
		public string Start;
		public bool Enabled;
		public NESControllerTemplate() { }
		public NESControllerTemplate(bool defaults)
		{
			if (defaults)
			{
				Enabled = true;
				Up = "UpArrow, X1 DpadUp, X1 LStickUp";
				Down = "DownArrow, X1 DpadDown, X1 LStickDown";
				Left = "LeftArrow, X1 DpadLeft, X1 LStickLeft";
				Right = "RightArrow, X1 DpadRight, X1 LStickRight";
				A = "X, X1 B";
				B = "Z, X1 A";
				Start = "Return, X1 Start";
				Select = "Space, X1 Back";
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

	public class GBControllerTemplate
	{
		public string Up;
		public string Down;
		public string Left;
		public string Right;
		public string A;
		public string B;
		public string Select;
		public string Start;
		public string Power = "";
		public bool Enabled;
		public GBControllerTemplate() { }
		public GBControllerTemplate(bool defaults)
		{
			if (defaults)
			{
				Enabled = true;
				Up = "UpArrow, X1 DpadUp, X1 LStickUp";
				Down = "DownArrow, X1 DpadDown, X1 LStickDown";
				Left = "LeftArrow, X1 DpadLeft, X1 LStickLeft";
				Right = "RightArrow, X1 DpadRight, X1 LStickRight";
				A = "X, X1 B";
				B = "Z, X1 A";
				Start = "Return, X1 Start";
				Select = "Space, X1 Back";
				Power = "";
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
				Power = "";
			}
		}
	}

	public class GenControllerTemplate
	{
		public string Up = "";
		public string Down = "";
		public string Left = "";
		public string Right = "";
		public string A = "";
		public string B = "";
		public string C = "";
		public string Start = "";
		public bool Enabled;

		public GenControllerTemplate() { }
		public GenControllerTemplate(bool defaults)
		{
			if (defaults)
			{
				Enabled = true;
				Up = "UpArrow, X1 DpadUp, X1 LStickUp";
				Down = "DownArrow, X1 DpadDown, X1 LStickDown";
				Left = "LeftArrow, X1 DpadLeft, X1 LStickLeft";
				Right = "RightArrow, X1 DpadRight, X1 LStickRight";
				A = "Z, X1 A";
				B = "X, X1 X";
				C = "C, X1 Y";
				Start = "Return, X1 Start";
			}
		}
	}

	public class SingleButtonJoyStickTemplate
	{
		public string Up = "";
		public string Down = "";
		public string Left = "";
		public string Right = "";
		public string Button = "";
		public bool Enabled;

		public SingleButtonJoyStickTemplate() { }
		public SingleButtonJoyStickTemplate(bool defaults, bool useNumpad = false)
		{
			if (defaults)
			{
				if (useNumpad)
				{
					Enabled = true;
					Up = "NumberPad8, X1 DpadUp, X1 LStickUp";
					Down = "NumberPad2, X1 DpadDown, X1 LStickDown";
					Left = "NumberPad4, X1 DpadLeft, X1 LStickLeft";
					Right = "NumberPad6, X1 DpadRight, X1 LStickRight";
					Button = "NumberPad8, X1 A";
				}
				else
				{
					Enabled = true;
					Up = "UpArrow, X1 DpadUp, X1 LStickUp";
					Down = "DownArrow, X1 DpadDown, X1 LStickDown";
					Left = "LeftArrow, X1 DpadLeft, X1 LStickLeft";
					Right = "RightArrow, X1 DpadRight, X1 LStickRight";
					Button = "Z, X1 A";
				}
			}
		}
	}

	public class Atari2600ConsoleButtonsTemplate
	{
		public string Reset = "";
		public string Select = "";
		public bool Enabled;

		public Atari2600ConsoleButtonsTemplate() { }
		public Atari2600ConsoleButtonsTemplate(bool defaults)
		{
			if (defaults)
			{
				Enabled = true;
				Reset = "";
				Select = "";
			}
		}
	}

	public class ColecoVisionControllerTemplate
	{
		public string Up = "";
		public string Down = "";
		public string Left = "";
		public string Right = "";
		public string L1 = "";
		public string L2 = "";
		public string R1 = "";
		public string R2 = "";

		public string _1 = "";
		public string _2 = "";
		public string _3 = "";
		public string _4 = "";
		public string _5 = "";
		public string _6 = "";
		public string _7 = "";
		public string _8 = "";
		public string _9 = "";
		public string Star = "";
		public string Pound = "";
		public bool Enabled;

		public ColecoVisionControllerTemplate() { }
		public ColecoVisionControllerTemplate(bool defaults)
		{
			if (defaults)
			{
				Enabled = true;
				Up = "UpArrow, X1 DpadUp, X1 LStickUp";
				Down = "DownArrow, X1 DpadDown, X1 LStickDown";
				Left = "LeftArrow, X1 DpadLeft, X1 LStickLeft";
				Right = "RightArrow, X1 DpadRight, X1 LStickRight";
				L1 = "Z, J1 B1";
				L2 = "X, J1 B2";
				R1 = "C, J1 B1";
				R2 = "V, J1 B2";
				_1 = "NumberPad1";
				_2 = "NumberPad2";
				_3 = "NumberPad3";
				_4 = "NumberPad4";
				_5 = "NumberPad5";
				_6 = "NumberPad6";
				_7 = "NumberPad7";
				_8 = "NumberPad8";
				_9 = "NumberPad9";
				Pound = "NumberPadPeriod";
				Star = "NumberPad0";
			}
		}
	}

	public class C64KeyboardTemplate
	{
		public string F1;
		public string F3;
		public string F5;
		public string F7;

		public string Left_Arrow;
		public string _1;
		public string _2;
		public string _4;
		public string _3;
		public string _5;
		public string _6;
		public string _7;
		public string _8;
		public string _9;
		public string _0;
		public string Plus;
		public string Minus;
		public string Pound;
		public string Clear_Home;
		public string Insert_Delete;

		public string Control;
		public string Q;
		public string W;
		public string E;
		public string R;
		public string T;
		public string Y;
		public string U;
		public string I;
		public string O;
		public string P;
		public string At;
		public string Asterisk;
		public string Up_Arrow;

		public string Run_Stop;
		public string A;
		public string S;
		public string D;
		public string F;
		public string G;
		public string H;
		public string J;
		public string K;
		public string L;
		public string Colon;
		public string Semicolon;
		public string Equal;
		public string Return;

		public string Commodore;
		public string Left_Shift;
		public string Z;
		public string X;
		public string C;
		public string V;
		public string B;
		public string N;
		public string M;
		public string Comma;
		public string Period;
		public string Slash;
		public string Right_Shift;
		public string Cursor_Up_Down;
		public string Cursor_Left_Right;

		public string Space;

		public bool enabled;

		public C64KeyboardTemplate() { }
		public C64KeyboardTemplate(bool defaults)
		{
			if (defaults)
			{
				F1 = "F1";
				F3 = "F3";
				F5 = "F5";
				F7 = "F7";

				Left_Arrow = "";
				_1 = "1";
				_2 = "2";
				_4 = "3";
				_3 = "4";
				_5 = "5";
				_6 = "6";
				_7 = "7";
				_8 = "8";
				_9 = "9";
				_0 = "0";
				Plus = "Equals";
				Minus = "Minus";
				Pound = "";
				Clear_Home = "";
				Insert_Delete = "";

				Control = "";
				Q = "Q";
				W = "W";
				E = "E";
				R = "R";
				T = "T";
				Y = "Y";
				U = "U";
				I = "I";
				O = "O";
				P = "P";
				At = "";
				Asterisk = "";
				Up_Arrow = "";

				Run_Stop = "";
				A = "A";
				S = "S";
				D = "D";
				F = "F";
				G = "G";
				H = "H";
				J = "J";
				K = "K";
				L = "L";
				Colon = "Semicolon";
				Semicolon = "Apostrophe";
				Equal = "";
				Return = "Return";

				Commodore = "";
				Left_Shift = "LeftShift";
				Z = "Z";
				X = "X";
				C = "C";
				V = "V";
				B = "B";
				N = "N";
				M = "M";
				Comma = "Comma";
				Period = "Period";
				Slash = "Slash";
				Right_Shift = "RightShift";
				Cursor_Up_Down = "";
				Cursor_Left_Right = "";

				Space = "Space";
			}
		}
	}

	public class TI83ControllerTemplate
	{
		public string _0;
		public string _1;
		public string _2;
		public string _3;
		public string _4;
		public string _5;
		public string _6;
		public string _7;
		public string _8;
		public string _9;
		public string DOT;
		public string ON;
		public string ENTER;
		public string DOWN;
		public string UP;
		public string LEFT;
		public string RIGHT;
		public string PLUS;
		public string MINUS;
		public string MULTIPLY;
		public string DIVIDE;
		public string CLEAR;
		public string EXP;
		public string DASH;
		public string PARACLOSE;
		public string TAN;
		public string VARS;
		public string PARAOPEN;
		public string COS;
		public string PRGM;
		public string STAT;
		public string SIN;
		public string MATRIX;
		public string X;
		public string STO;
		public string LN;
		public string LOG;
		public string SQUARED;
		public string NEG1;
		public string MATH;
		public string ALPHA;
		public string GRAPH;
		public string TRACE;
		public string ZOOM;
		public string WINDOW;
		public string Y;
		public string SECOND;
		public string MODE;
		public string DEL;
		public TI83ControllerTemplate() { }
		public bool Enabled;
		public string COMMA;
		public TI83ControllerTemplate(bool defaults)
		{
			if (defaults)
			{
				Enabled = true;
				_0 = "NumberPad0";      //0
				_1 = "NumberPad1";      //1
				_2 = "NumberPad2";      //2
				_3 = "NumberPad3";      //3
				_4 = "NumberPad4";      //4
				_5 = "NumberPad5";      //5
				_6 = "NumberPad6";      //6
				_7 = "NumberPad7";      //7
				_8 = "NumberPad8";      //8
				_9 = "NumberPad9";      //9
				DOT = "NumberPadPeriod";//10
				ON = "Space";           //11
				ENTER = "Return, NumberPadEnter";       //12
				UP = "UpArrow";         //13
				DOWN = "DownArrow";     //14
				LEFT = "LeftArrow";     //15
				RIGHT = "RightArrow";   //16
				PLUS = "NumberPadPlus"; //17
				MINUS = "NumberPadMinus";     //18
				MULTIPLY = "NumberPadStar";   //19
				DIVIDE = "NumberPadSlash";    //20
				CLEAR = "Escape";       //21
				EXP = "6";              //22
				DASH = "Minus";         //23
				PARACLOSE = "0";        //24
				PARAOPEN = "9";         //25
				TAN = "T";              //26
				VARS = "V";             //27
				COS = "C";              //28
				PRGM = "R";             //29
				STAT = "S";             //30
				MATRIX = "LeftBracket"; //31
				X = "X";                //32
				STO = "Insert";         //33
				LN = "L";               //34
				LOG = "O";              //35
				SQUARED = "2";          //36
				NEG1 = "1";             //37
				MATH = "M";             //38
				ALPHA = "A";            //39
				GRAPH = "G";            //40
				TRACE = "Home";         //41
				ZOOM = "Z";             //42
				WINDOW = "W";           //43
				Y = "Y";                //44
				SECOND = "Slash";       //45
				MODE = "BackSlash";     //46
				DEL = "Delete";         //47
				COMMA = "Comma";        //48
				SIN = "Period";         //49
			}
			else
			{
				Enabled = false;
				_0 = "";
				_1 = "";
				_2 = "";
				_3 = "";
				_4 = "";
				_5 = "";
				_6 = "";
				_7 = "";
				_8 = "";
				_9 = "";
				DOT = "";
				ON = "";
				ENTER = "";
				UP = "";
				DOWN = "";
				LEFT = "";
				RIGHT = "";
				PLUS = "";
				MINUS = "";
				MULTIPLY = "";
				DIVIDE = "";
				CLEAR = "";
				EXP = "";
				DASH = "";
				PARACLOSE = "";
				TAN = "";
				VARS = "";
				PARAOPEN = "";
				COS = "";
				PRGM = "";
				STAT = "";
				SIN = "";
				MATRIX = "";
				X = "";
				STO = "";
				LN = "";
				LOG = "";
				SQUARED = "";
				NEG1 = "";
				MATH = "";
				ALPHA = "";
				GRAPH = "";
				TRACE = "";
				ZOOM = "";
				WINDOW = "";
				Y = "";
				SECOND = "";
				MODE = "";
				DEL = "";
				COMMA = "";
			}
		}
	}
}