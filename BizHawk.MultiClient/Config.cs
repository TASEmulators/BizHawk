using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;

namespace BizHawk.MultiClient
{
	public class Config
	{
		public Config()
		{
		}

		// Directories
		public bool UseRecentForROMs = false;
		public string LastRomPath = ".";
		public string BasePath = ".";
		public string BaseROMPath = ".";

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
		//public string PathSNESFirmwares = Path.Combine(".", "Firmwares");

		public string BaseGBA = Path.Combine(".", "GBA");
		public string PathGBAROMs = ".";
		public string PathGBASavestates = Path.Combine(".", "State");
		public string PathGBASaveRAM = Path.Combine(".", "SaveRAM");
		public string PathGBAScreenshots = Path.Combine(".", "Screenshots");
		public string PathGBACheats = Path.Combine(".", "Cheats");

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

		public string BaseAtari2600 = Path.Combine(".", "Atari 2600");
		public string PathAtari2600ROMs = ".";
		public string PathAtari2600Savestates = Path.Combine(".", "State");
		public string PathAtari2600Screenshots = Path.Combine(".", "Screenshots");
		public string PathAtari2600Cheats = Path.Combine(".", "Cheats");

		public string BaseAtari7800 = Path.Combine(".", "Atari 7800");
		public string PathAtari7800ROMs = ".";
		public string PathAtari7800Savestates = Path.Combine(".", "State");
		public string PathAtari7800SaveRAM = Path.Combine(".", "SaveRAM");
		public string PathAtari7800Screenshots = Path.Combine(".", "Screenshots");
		public string PathAtari7800Cheats = Path.Combine(".", "Cheats");
		//public string PathAtari7800Firmwares = Path.Combine(".", "Firmwares");

		public string BaseC64 = Path.Combine(".", "C64");
		public string PathC64ROMs = ".";
		public string PathC64Savestates = Path.Combine(".", "State");
		public string PathC64Screenshots = Path.Combine(".", "Screenshots");
		public string PathC64Cheats = Path.Combine(".", "Cheats");
		//public string PathC64Firmwares = Path.Combine(".", "Firmwares");

		public string BasePSX = Path.Combine(".", "PSX");
		public string PathPSXROMs = ".";
		public string PathPSXSavestates = Path.Combine(".", "State");
		public string PathPSXSaveRAM = Path.Combine(".", "SaveRAM");
		public string PathPSXScreenshots = Path.Combine(".", "Screenshots");
		public string PathPSXCheats = Path.Combine(".", "Cheats");
		//public string PathPSXFirmwares = Path.Combine(".", "Firmwares");

		public string BaseCOL = Path.Combine(".", "Coleco");
		public string PathCOLROMs = ".";
		public string PathCOLSavestates = Path.Combine(".", "State");
		public string PathCOLScreenshots = Path.Combine(".", "Screenshots");
		public string PathCOLCheats = Path.Combine(".", "Cheats");

		public string BaseN64 = Path.Combine(".", "N64");
		public string PathN64ROMs = ".";
		public string PathN64Savestates = Path.Combine(".", "State");
		public string PathN64SaveRAM = Path.Combine(".", "SaveRAM");
		public string PathN64Screenshots = Path.Combine(".", "Screenshots");
		public string PathN64Cheats = Path.Combine(".", "Cheats");

		public string BaseSaturn = Path.Combine(".", "Saturn");
		public string PathSaturnROMs = ".";
		public string PathSaturnSavestates = Path.Combine(".", "State");
		public string PathSaturnSaveRAM = Path.Combine(".", "SaveRAM");
		public string PathSaturnScreenshots = Path.Combine(".", "Screenshots");
		public string PathSaturnCheats = Path.Combine(".", "Cheats");

		public string MoviesPath = Path.Combine(".", "Movies");
		public string MoviesBackupPath = Path.Combine(".", "Movies", "backup");
		public string LuaPath = Path.Combine(".", "Lua");
		public string WatchPath = ".";
		public string AVIPath = ".";
		public string LogPath = ".";
		public string FirmwaresPath = Path.Combine(".", "Firmware");

		//BIOS Paths
		public string FilenamePCEBios = "[BIOS] Super CD-ROM System (Japan) (v3.0).pce";
		public string FilenameFDSBios = "disksys.rom";
		public string FilenameGBABIOS = "gbabios.rom";
		public string FilenameCOLBios = "ColecoBios.bin";
		public string FilenameINTVGROM = "grom.bin";
		public string FilenameA78NTSCBios = "7800NTSCBIOS.bin";
		public string FilenameA78PALBios = "7800PALBIOS.bin";
		public string FilenameA78HSCBios = "7800highscore.bin";
		public string FilenameINTVEROM = "erom.bin";
		public string FilenameSaturnBios = "Sega Saturn BIOS v1.01 (JAP).bin";

		public string FFMpegPath = "%exe%/dll/ffmpeg.exe";

		//N64 Config Settings
		public string N64VidPlugin = "Rice";
		public int N64VideoSizeX = 320;
		public int N64VideoSizeY = 240;

	
		//public int RiceFrameBufferSetting = 0;
		//public int RiceFrameBufferWriteBackControl = 0;
		//public int RiceRenderToTexture = 0;
		//public int RiceScreenUpdateSetting = 4;
		//public int RiceMipmapping = 2;
		//public int RiceFogMethod = 0;
		//public int RiceForceTextureFilter = 0;
		//public int RiceTextureEnhancement = 0;
		//public int RiceTextureEnhancementControl = 0;
		//public int RiceTextureQuality = 0;
		//public int RiceOpenGLDepthBufferSetting = 16;
		//public int RiceMultiSampling = 0;
		//public int RiceColorQuality = 0;
		//public int RiceOpenGLRenderSetting = 0;
		//public int RiceAnisotropicFiltering = 0;

		
		//public bool RiceNormalAlphaBlender = false; 
		//public bool RiceFastTextureLoading = false;
		//public bool RiceAccurateTextureMapping = true;
		//public bool RiceInN64Resolution = false;
		//public bool RiceSaveVRAM = false;
		//public bool RiceDoubleSizeForSmallTxtrBuf = false;
		//public bool RiceDefaultCombinerDisable = false;
		//public bool RiceEnableHacks = true;
		//public bool RiceWinFrameMode = false;
		//public bool RiceFullTMEMEmulation = false;
		//public bool RiceOpenGLVertexClipper = false;
		//public bool RiceEnableSSE = true;
		//public bool RiceEnableVertexShader = false;
		//public bool RiceSkipFrame = false;
		//public bool RiceTexRectOnly = false;
		//public bool RiceSmallTextureOnly = false;
		//public bool RiceLoadHiResCRCOnly = true;
		//public bool RiceLoadHiResTextures = false;
		//public bool RiceDumpTexturesToFiles = false;

		//public bool RiceUseDefaultHacks = true;
		//public bool RiceDisableTextureCRC = false;
		//public bool RiceDisableCulling = false;
		//public bool RiceIncTexRectEdge = false;
		//public bool RiceZHack = false;
		//public bool RiceTextureScaleHack = false;
		//public bool RicePrimaryDepthHack = false;
		//public bool RiceTexture1Hack = false;
		//public bool RiceFastLoadTile = false;
		//public bool RiceUseSmallerTexture = false;
		//public int RiceVIWidth = -1;
		//public int RiceVIHeight = -1;
		//public int RiceUseCIWidthAndRatio = 0;
		//public int RiceFullTMEM = 0;
		//public bool RiceTxtSizeMethod2 = false;
		//public bool RiceEnableTxtLOD = false;
		//public int RiceFastTextureCRC = 0;
		//public bool RiceEmulateClear = false;
		//public bool RiceForceScreenClear = false;
		//public int RiceAccurateTextureMappingHack = 0;
		//public int RiceNormalBlender = 0;
		//public bool RiceDisableBlender = false;
		//public bool RiceForceDepthBuffer = false;
		//public bool RiceDisableObjBG = false;
		//public int RiceFrameBufferOption = 0;
		//public int RiceRenderToTextureOption = 0;
		//public int RiceScreenUpdateSettingHack = 0;
		//public int RiceEnableHacksForGame = 0;

		public N64RicePluginSettings RicePlugin = new N64RicePluginSettings();
		public N64GlidePluginSettings GlidePlugin = new N64GlidePluginSettings();

		// General Client Settings
		public int Input_Hotkey_OverrideOptions = 0;
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

		public enum SaveStateTypeE { Default, Binary, Text };
		public SaveStateTypeE SaveStateType = SaveStateTypeE.Default;

		// Run-Control settings
		public int FrameProgressDelayMs = 500; //how long until a frame advance hold turns into a frame progress?
		public int FrameSkip = 4;
		public int SpeedPercent = 100;
		public int SpeedPercentAlternate = 400;
		public bool ClockThrottle = true;
		public bool AutoMinimizeSkipping = true;
		public bool VSyncThrottle = false;
		public bool RewindEnabled = true;
		/// <summary>use vsync.  if VSyncThrottle = false, this will try to use vsync without throttling to it</summary>
		public bool VSync = false;

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
		public int DispMultix = 0;
		public int DispMultiy = 14;
		public int DispMultianchor = 1;
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
		public string SoundDevice = "";

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
		public bool RamSearchFastMode = false;

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
		public int NTSC_NESTopLine = 8;
		public int NTSC_NESBottomLine = 231;
		public int PAL_NESTopLine = 8;
		public int PAL_NESBottomLine = 231;

		// gb gpu view settings
		public bool AutoLoadGBGPUView = false;
		public bool GBGPUViewSaveWindowPosition = true;
		public int GBGPUViewWndx = -1;
		public int GBGPUViewWndy = -1;
		public Color GBGPUSpriteBack = Color.Lime;

		// SNES Graphics Debugger Dialog Settings
		public bool AutoLoadSNESGraphicsDebugger = false;
		public bool SNESGraphicsDebuggerSaveWindowPosition = true;
		public int SNESGraphicsDebuggerWndx = -1;
		public int SNESGraphicsDebuggerWndy = -1;
		public int SNESGraphicsDebuggerRefreshRate = 4;
		public bool SNESGraphicsUseUserBackdropColor = false;
		public int SNESGraphicsUserBackdropColor = -1;
		public string SNESPalette = "BizHawk";

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

		// SATURN GRAPHICS SETTINGS
		public bool SaturnUseGL = false;
		public int SaturnDispFactor = 1;
		public bool SaturnDispFree = false;
		public int SaturnGLW = 640;
		public int SaturnGLH = 480;

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

		// Coleco Settings
		public bool ColecoSkipBiosIntro = false;

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
		public int VPadWidth = -1;
		public int VPadHeight = -1;

		// NES Game Genie Encoder/Decoder
		public bool NESGGAutoload = false;
		public bool NESGGSaveWindowPosition = true;
		public int NESGGWndx = -1;
		public int NESGGWndy = -1;

		// SNES Game Genie Encoder/Decoder
		public bool SNESGGAutoload = false;
		public bool SNESGGSaveWindowPosition = true;
		public int SNESGGWndx = -1;
		public int SNESGGWndy = -1;

		// GB/GG Game Genie Encoder/Decoder
		public bool GBGGAutoload = false;
		public bool GBGGSaveWindowPosition = true;
		public int GBGGWndx = -1;
		public int GBGGWndy = -1;

		// GEN Game Genie Encoder/Decoder
		public bool GENGGAutoload = false;
		public bool GENGGSaveWindowPosition = true;
		public int GENGGWndx = -1;
		public int GENGGWndy = -1;

		//Movie Settings
		public RecentFiles RecentMovies = new RecentFiles(8);
		public bool AutoLoadMostRecentMovie = false;
		public bool BindSavestatesToMovies = true;
		public string DefaultAuthor = "default user";
		public bool UseDefaultAuthor = true;
		public bool DisplaySubtitles = true;
		public bool VBAStyleMovieLoadState = false;
		public bool MoviePlaybackPokeMode = false;

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
		public string AutoholdAutofireBinding = "";
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
		public string OpenVirtualPadBinding = "";
		public string MoviePlaybackPokeModeBinding = "";
		public string ClearFrameBinding = "";
		// NES Sound settings
		public int NESSquare1 = 376;
		public int NESSquare2 = 376;
		public int NESTriangle = 426;
		public int NESNoise = 247;
		public int NESDMC = 167;

		public const int NESSquare1Max = 376;
		public const int NESSquare2Max = 376;
		public const int NESTriangleMax = 426;
		public const int NESNoiseMax = 247;
		public const int NESDMCMax = 167;

		// [ControllerType][ControllerName] => Bind
		public Dictionary<string, Dictionary<string, string>> AllTrollers = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, Dictionary<string, string>> AllTrollersAutoFire = new Dictionary<string, Dictionary<string, string>>();

		// SMS / GameGear Settings
		public bool SmsEnableFM = true;
		public bool SmsAllowOverlock = false;
		public bool SmsForceStereoSeparation = false;
		public bool SmsSpriteLimit = false;
		public bool GGShowClippedRegions = false;
		public bool GGHighlightActiveDisplayRegion = false;

		// PCEngine Settings
		public bool PceSpriteLimit = false;
		public bool PceEqualizeVolume = false;
		public bool PceArcadeCardRewindHack = true;

		// Genesis Settings

		//Atari 2600 Settings
		public bool Atari2600_BW = false;
		public bool Atari2600_LeftDifficulty = true;
		public bool Atari2600_RightDifficulty = true;

		//Atari 7800 Settings

		//ColecoVision

		//Intellivision

		//NES settings

		//SNES settings
		public string SNESProfile = "Compatibility";
		public bool SNESUseRingBuffer = true;
		public bool SNESAlwaysDoubleSize = false;

		//N64 settings

		//TI 83 settings

		//GB settings
		public bool GB_ForceDMG = false;
		public bool GB_GBACGB = false;
		public bool GB_MulticartCompat = false;
		public string GB_PaletteFile = "";
		public bool GB_AsSGB = false;
		public Emulation.Consoles.GB.GBColors.ColorType CGBColors = Emulation.Consoles.GB.GBColors.ColorType.gambatte;

		//Dual Gb

		//GBA settings

		//Saturn

		//Commodore 64 Settings

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

	public enum PLUGINTYPE { RICE, GLIDE }; 

	public interface iPluginSettings
	{
		PLUGINTYPE PluginType { get; }
		Dictionary<string, object> GetPluginSettings();
	}

	public class N64RicePluginSettings : iPluginSettings
	{
		public PLUGINTYPE PluginType
		{
			get { return PLUGINTYPE.RICE; }
		}

		public void FillPerGameHacks(GameInfo game)
		{
			if (Global.Config.RicePlugin.UseDefaultHacks)
			{
				DisableTextureCRC = game.GetBool("RiceDisableTextureCRC", false);
				DisableCulling = game.GetBool("RiceDisableCulling", false);
				IncTexRectEdge = game.GetBool("RiceIncTexRectEdge", false);
				ZHack = game.GetBool("RiceZHack", false);
				TextureScaleHack = game.GetBool("RiceTextureScaleHack", false);
				PrimaryDepthHack = game.GetBool("RicePrimaryDepthHack", false);
				Texture1Hack = game.GetBool("RiceTexture1Hack", false);
				FastLoadTile = game.GetBool("RiceFastLoadTile", false);
				UseSmallerTexture = game.GetBool("RiceUseSmallerTexture", false);
				VIWidth = game.GetInt("RiceVIWidth", -1);
				VIHeight = game.GetInt("RiceVIHeight", -1);
				UseCIWidthAndRatio = game.GetInt("RiceUseCIWidthAndRatio", 0);
				FullTMEM = game.GetInt("RiceFullTMEM", 0);
				TxtSizeMethod2 = game.GetBool("RiceTxtSizeMethod2", false);
				EnableTxtLOD = game.GetBool("RiceEnableTxtLOD", false);
				FastTextureCRC = game.GetInt("RiceFastTextureCRC", 0);
				EmulateClear = game.GetBool("RiceEmulateClear", false);
				ForceScreenClear = game.GetBool("RiceForceScreenClear", false);
				AccurateTextureMappingHack = game.GetInt("RiceAccurateTextureMappingHack", 0);
				NormalBlender = game.GetInt("RiceNormalBlender", 0);
				DisableBlender = game.GetBool("RiceDisableBlender", false);
				ForceDepthBuffer = game.GetBool("RiceForceDepthBuffer", false);
				DisableObjBG = game.GetBool("RiceDisableObjBG", false);
				FrameBufferOption = game.GetInt("RiceFrameBufferOption", 0);
				RenderToTextureOption = game.GetInt("RiceRenderToTextureOption", 0);
				ScreenUpdateSettingHack = game.GetInt("RiceScreenUpdateSettingHack", 0);
				EnableHacksForGame = game.GetInt("RiceEnableHacksForGame", 0);
			}
		}

		public Dictionary<string, object> GetPluginSettings()
		{
			//TODO: deal witn the game depedent settings
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			System.Reflection.FieldInfo[] members = Global.Config.RicePlugin.GetType().GetFields();
			foreach (System.Reflection.FieldInfo member in members)
			{
				object field = Global.Config.RicePlugin.GetType().GetField(member.Name).GetValue(Global.Config.RicePlugin);
				dictionary.Add(member.Name, field);
			}

			return dictionary;
		}

		public int FrameBufferSetting = 0;
		public int FrameBufferWriteBackControl = 0;
		public int RenderToTexture = 0;
		public int ScreenUpdateSetting = 4;
		public int Mipmapping = 2;
		public int FogMethod = 0;
		public int ForceTextureFilter = 0;
		public int TextureEnhancement = 0;
		public int TextureEnhancementControl = 0;
		public int TextureQuality = 0;
		public int OpenGLDepthBufferSetting = 16;
		public int MultiSampling = 0;
		public int ColorQuality = 0;
		public int OpenGLRenderSetting = 0;
		public int AnisotropicFiltering = 0;


		public bool NormalAlphaBlender = false;
		public bool FastTextureLoading = false;
		public bool AccurateTextureMapping = true;
		public bool InN64Resolution = false;
		public bool SaveVRAM = false;
		public bool DoubleSizeForSmallTxtrBuf = false;
		public bool DefaultCombinerDisable = false;
		public bool EnableHacks = true;
		public bool WinFrameMode = false;
		public bool FullTMEMEmulation = false;
		public bool OpenGLVertexClipper = false;
		public bool EnableSSE = true;
		public bool EnableVertexShader = false;
		public bool SkipFrame = false;
		public bool TexRectOnly = false;
		public bool SmallTextureOnly = false;
		public bool LoadHiResCRCOnly = true;
		public bool LoadHiResTextures = false;
		public bool DumpTexturesToFiles = false;

		public bool UseDefaultHacks = true;
		public bool DisableTextureCRC = false;
		public bool DisableCulling = false;
		public bool IncTexRectEdge = false;
		public bool ZHack = false;
		public bool TextureScaleHack = false;
		public bool PrimaryDepthHack = false;
		public bool Texture1Hack = false;
		public bool FastLoadTile = false;
		public bool UseSmallerTexture = false;
		public int VIWidth = -1;
		public int VIHeight = -1;
		public int UseCIWidthAndRatio = 0;
		public int FullTMEM = 0;
		public bool TxtSizeMethod2 = false;
		public bool EnableTxtLOD = false;
		public int FastTextureCRC = 0;
		public bool EmulateClear = false;
		public bool ForceScreenClear = false;
		public int AccurateTextureMappingHack = 0;
		public int NormalBlender = 0;
		public bool DisableBlender = false;
		public bool ForceDepthBuffer = false;
		public bool DisableObjBG = false;
		public int FrameBufferOption = 0;
		public int RenderToTextureOption = 0;
		public int ScreenUpdateSettingHack = 0;
		public int EnableHacksForGame = 0;
	}

	public class N64GlidePluginSettings : iPluginSettings
	{
		public PLUGINTYPE PluginType
		{
			get { return PLUGINTYPE.GLIDE; }
		}

		public void FillPerGameHacks(GameInfo game)
		{
			if (Global.Config.GlidePlugin.UseDefaultHacks)
			{
				alt_tex_size = Global.Game.GetBool("Glide_alt_tex_size", false);
				buff_clear = Global.Game.GetBool("Glide_buff_clear", true);
				decrease_fillrect_edge = Global.Game.GetBool("Glide_decrease_fillrect_edge", false);
				detect_cpu_write = Global.Game.GetBool("Glide_detect_cpu_write", false);
				fb_clear = Global.Game.GetBool("Glide_fb_clear", false);
				fb_hires = Global.Game.GetBool("Glide_fb_clear", true);
				fb_read_alpha = Global.Game.GetBool("Glide_fb_read_alpha", false);
				fb_smart = Global.Game.GetBool("Glide_fb_smart", false);
				fillcolor_fix = Global.Game.GetBool("Glide_fillcolor_fix", false);
				fog = Global.Game.GetBool("Glide_fog", true);
				force_depth_compare = Global.Game.GetBool("Glide_force_depth_compare", false);
				force_microcheck = Global.Game.GetBool("Glide_force_microcheck", false);
				fb_hires_buf_clear = Global.Game.GetBool("Glide_fb_hires_buf_clear", true);
				fb_ignore_aux_copy = Global.Game.GetBool("Glide_fb_ignore_aux_copy", false);
				fb_ignore_previous = Global.Game.GetBool("Glide_fb_ignore_previous", false);
				increase_primdepth = Global.Game.GetBool("Glide_increase_primdepth", false);
				increase_texrect_edge = Global.Game.GetBool("Glide_increase_texrect_edge", false);
				fb_optimize_texrect = Global.Game.GetBool("Glide_fb_optimize_texrect", true);
				fb_optimize_write = Global.Game.GetBool("Glide_fb_optimize_write", false);
				PPL = Global.Game.GetBool("Glide_PPL", false);
				soft_depth_compare = Global.Game.GetBool("Glide_soft_depth_compare", false);
				use_sts1_only = Global.Game.GetBool("Glide_use_sts1_only", false);
				wrap_big_tex = Global.Game.GetBool("Glide_wrap_big_tex", false);

				depth_bias = Global.Game.GetInt("Glide_depth_bias", 20);
				filtering = Global.Game.GetInt("Glide_filtering", 1);
				fix_tex_coord = Global.Game.GetInt("Glide_fix_tex_coord", 0);
				lodmode = Global.Game.GetInt("Glide_lodmode", 0);
				
				stipple_mode = Global.Game.GetInt("Glide_stipple_mode", 2);
				stipple_pattern = Global.Game.GetInt("Glide_stipple_pattern", 1041204192);
				swapmode = Global.Game.GetInt("Glide_swapmode", 1);
				enable_hacks_for_game = Global.Game.GetInt("Glide_enable_hacks_for_game", 0);
			}
		}

		public Dictionary<string, object> GetPluginSettings()
		{
			//TODO: deal witn the game depedent settings
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			System.Reflection.FieldInfo[] members = Global.Config.GlidePlugin.GetType().GetFields();
			foreach (System.Reflection.FieldInfo member in members)
			{
				object field = Global.Config.GlidePlugin.GetType().GetField(member.Name).GetValue(Global.Config.GlidePlugin);
				dictionary.Add(member.Name, field);
			}

			return dictionary;
		}

		public int wfmode = 1;
		public bool wireframe = false;
		public int card_id = 0;
		public bool flame_corona = false;
		public int ucode = 2;
		public bool autodetect_ucode = true;
		public bool motionblur = false;
		public bool fb_read_always = false;
		public bool unk_as_red = false;
		public bool filter_cache = false;
		public bool fast_crc = false;
		public bool disable_auxbuf = false;
		public bool fbo = false;
		public bool noglsl = true;
		public bool noditheredalpha = true;
		public int tex_filter = 0;
		public bool fb_render = false;
		public bool wrap_big_tex = false;
		public bool use_sts1_only = false;
		public bool soft_depth_compare = false;
		public bool PPL = false;
		public bool fb_optimize_write = false;
		public bool fb_optimize_texrect = true;
		public bool increase_texrect_edge = false;
		public bool increase_primdepth = false;
		public bool fb_ignore_previous = false;
		public bool fb_ignore_aux_copy = false;
		public bool fb_hires_buf_clear = true;
		public bool force_microcheck = false;
		public bool force_depth_compare = false;
		public bool fog = true;
		public bool fillcolor_fix = false;
		public bool fb_smart = false;
		public bool fb_read_alpha = false;
		public bool fb_get_info = false;
		public bool fb_hires = true;
		public bool fb_clear = false;
		public bool detect_cpu_write = false;
		public bool decrease_fillrect_edge = false;
		public bool buff_clear = true;
		public bool alt_tex_size = false;
		public bool UseDefaultHacks = true;
		public int enable_hacks_for_game = 0;
		public int swapmode = 1;
		public int stipple_pattern = 1041204192;
		public int stipple_mode = 2;
		public int scale_y = 100000;
		public int scale_x = 100000;
		public int offset_y = 0;
		public int offset_x = 0;
		public int lodmode = 0;
		public int fix_tex_coord = 0;
		public int filtering = 1;
		public int depth_bias = 20;
	}
}