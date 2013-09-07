using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Collections;

namespace BizHawk.MultiClient
{
	public class Config
	{
		public Config()
		{
			config.NewControllerConfig.ConfigCheckAllControlDefaults(this);
		}

		public void ResolveDefaults()
		{
			PathEntries.ResolveWithDefaults();
			HotkeyBindings.ResolveWithDefaults();
		}

		//Path Settings ************************************/
		public bool UseRecentForROMs = false;
		public string LastRomPath = ".";
		public PathEntryCollection PathEntries = new PathEntryCollection();

		//BIOS Paths
		public Dictionary<string, string> FirmwareUserSpecifications = new Dictionary<string, string>(); //key: sysid+firmwareId; value: absolute path

		public string FFMpegPath = "%exe%/dll/ffmpeg.exe";

		//N64 Config Settings
		public string N64VidPlugin = "Rice";
		public int N64VideoSizeX = 320;
		public int N64VideoSizeY = 240;

		public N64RicePluginSettings RicePlugin = new N64RicePluginSettings();
		public N64GlidePluginSettings GlidePlugin = new N64GlidePluginSettings();
		public N64Glide64mk2PluginSettings Glide64mk2Plugin = new N64Glide64mk2PluginSettings();

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

		//Rewind settings
		public bool Rewind_UseDelta = true;
		public bool RewindEnabledSmall = true;
		public bool RewindEnabledMedium = true;
		public bool RewindEnabledLarge = false;
		public int RewindFrequencySmall = 1;
		public int RewindFrequencyMedium = 2;
		public int RewindFrequencyLarge = 60;
		public int Rewind_MediumStateSize = 262144; //256kb
		public int Rewind_LargeStateSize = 1048576; //1mb
		public int Rewind_BufferSize = 128; //in mb
		public bool Rewind_OnDisk = false;
		public bool Rewind_IsThreaded = false;

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

		public BindingCollection HotkeyBindings = new BindingCollection();

		//Analog Hotkey values
		public int Analog_LargeChange = 10;
		public int Analog_SmallChange = 1;

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

		public struct AnalogBind
		{
			/// <summary>the physical stick that we're bound to</summary>
			public string Value;
			/// <summary>sensitivity and flip</summary>
			public float Mult;
			/// <summary>portion of axis to ignore</summary>
			public float Deadzone;
			public AnalogBind(string Value, float Mult, float Deadzone)
			{
				this.Value = Value;
				this.Mult = Mult;
				this.Deadzone = Deadzone;
			}
		}

		// [ControllerType][ButtonName] => Physical Bind
		public Dictionary<string, Dictionary<string, string>> AllTrollers = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, Dictionary<string, string>> AllTrollersAutoFire = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, Dictionary<string, AnalogBind>> AllTrollersAnalog = new Dictionary<string, Dictionary<string, AnalogBind>>();

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

	#region Sub-classes TODO - it is about time to port these to separate files

	public class BindingCollection : IEnumerable<Binding>
	{
		public List<Binding> Bindings { get; private set; }

		public BindingCollection()
		{
			Bindings = new List<Binding>();
			Bindings.AddRange(DefaultValues);
		}

		public void Add(Binding b)
		{
			Bindings.Add(b);
		}

		public IEnumerator<Binding> GetEnumerator()
		{
			return Bindings.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Binding this[string index]
		{
			get
			{
				return Bindings.FirstOrDefault(x => x.DisplayName == index) ?? new Binding();
			}
		}

		public void ResolveWithDefaults()
		{
			//Add missing entries
			foreach (Binding default_binding in DefaultValues)
			{
				var binding = Bindings.FirstOrDefault(x => x.DisplayName == default_binding.DisplayName);
				if (binding == null)
				{
					Bindings.Add(default_binding);
				}
			}

			List<Binding> entriesToRemove = new List<Binding>();

			//Remove entries that no longer exist in defaults
			foreach (Binding entry in Bindings)
			{
				var binding = DefaultValues.FirstOrDefault(x => x.DisplayName == entry.DisplayName);
				if (binding == null)
				{
					entriesToRemove.Add(entry);
				}
			}

			foreach (Binding entry in entriesToRemove)
			{
				Bindings.Remove(entry);
			}
		}

		public static List<Binding> DefaultValues
		{
			get
			{
				return new List<Binding>()
		        {
			        //General
			        new Binding() { DisplayName = "Frame Advance", Bindings = "F", TabGroup = "General", DefaultBinding = "F", Ordinal = 0 },
			        new Binding() { DisplayName = "Rewind", Bindings = "Shift+R, X1 LeftShoulder", TabGroup = "General", DefaultBinding = "Shift+R, X1 LeftShoulder", Ordinal = 1 },
			        new Binding() { DisplayName = "Pause", Bindings = "Pause", TabGroup = "General", DefaultBinding = "Pause", Ordinal = 2 },
			        new Binding() { DisplayName = "Fast Forward", Bindings = "Tab, X1 RightShoulder", TabGroup = "General", DefaultBinding = "Tab, X1 RightShoulder", Ordinal = 3 },
			        new Binding() { DisplayName = "Turbo", Bindings = "Shift+Tab", TabGroup = "General", DefaultBinding = "Shift+Tab", Ordinal = 4 },
			        new Binding() { DisplayName = "Toggle Throttle", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 5 },
			        new Binding() { DisplayName = "Soft Reset", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 6 },
			        new Binding() { DisplayName = "Hard Reset", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 7 },
			        new Binding() { DisplayName = "Quick Load", Bindings = "P", TabGroup = "General", DefaultBinding = "P", Ordinal = 8 },
			        new Binding() { DisplayName = "Quick Save", Bindings = "I", TabGroup = "General", DefaultBinding = "I", Ordinal = 9 },
			        new Binding() { DisplayName = "Autohold", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 10 },
			        new Binding() { DisplayName = "Clear Autohold", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 11 },
			        new Binding() { DisplayName = "Screenshot", Bindings = "F12", TabGroup = "General", DefaultBinding = "F12", Ordinal = 12 },
			        new Binding() { DisplayName = "Full Screen", Bindings = "Alt+Return", TabGroup = "General", DefaultBinding = "Alt+Return", Ordinal = 13 },
			        new Binding() { DisplayName = "Open ROM", Bindings = "Ctrl+O", TabGroup = "General", DefaultBinding = "Ctrl+O", Ordinal = 14 },
			        new Binding() { DisplayName = "Close ROM", Bindings = "Ctrl+W", TabGroup = "General", DefaultBinding = "Ctrl+W", Ordinal = 15 },
			        new Binding() { DisplayName = "Display FPS", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 16 },
			        new Binding() { DisplayName = "Frame Counter", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 17 },
			        new Binding() { DisplayName = "Lag Counter", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 18 },
			        new Binding() { DisplayName = "Input Display", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 19 },
			        new Binding() { DisplayName = "Toggle BG Input", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 20 },
			        new Binding() { DisplayName = "Toggle Menu", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 21 },
			        new Binding() { DisplayName = "Volume Up", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 22 },
			        new Binding() { DisplayName = "Volume Down", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 23 },
			        new Binding() { DisplayName = "Record A/V", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 24 },
			        new Binding() { DisplayName = "Stop A/V", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 25 },
			        new Binding() { DisplayName = "Larger Window", Bindings = "Alt+UpArrow", TabGroup = "General", DefaultBinding = "Alt+UpArrow", Ordinal = 26 },
			        new Binding() { DisplayName = "Smaller Window", Bindings = "Alt+DownArrow", TabGroup = "General", DefaultBinding = "Alt+DownArrow", Ordinal = 27 },
			        new Binding() { DisplayName = "Increase Speed", Bindings = "Equals", TabGroup = "General", DefaultBinding = "Equals", Ordinal = 28 },
			        new Binding() { DisplayName = "Decrease Speed", Bindings = "Minus", TabGroup = "General", DefaultBinding = "Minus", Ordinal = 29 },
			        new Binding() { DisplayName = "Reboot Core", Bindings = "Ctrl+R", TabGroup = "General", DefaultBinding = "Ctrl+R", Ordinal = 30 },
			        new Binding() { DisplayName = "Autofire", Bindings = "", TabGroup = "General", DefaultBinding = "", Ordinal = 31 },

			        //Save States
			        new Binding() { DisplayName = "Save State 0", Bindings = "Shift+F10", TabGroup = "Save States", DefaultBinding = "Shift+F10", Ordinal = 1 },
			        new Binding() { DisplayName = "Save State 1", Bindings = "Shift+F1", TabGroup = "Save States", DefaultBinding = "Shift+F1", Ordinal = 2 },
			        new Binding() { DisplayName = "Save State 2", Bindings = "Shift+F2", TabGroup = "Save States", DefaultBinding = "Shift+F2", Ordinal = 3 },
			        new Binding() { DisplayName = "Save State 3", Bindings = "Shift+F3", TabGroup = "Save States", DefaultBinding = "Shift+F3", Ordinal = 4 },
			        new Binding() { DisplayName = "Save State 4", Bindings = "Shift+F4", TabGroup = "Save States", DefaultBinding = "Shift+F4", Ordinal = 5 },
			        new Binding() { DisplayName = "Save State 5", Bindings = "Shift+F5", TabGroup = "Save States", DefaultBinding = "Shift+F5", Ordinal = 6 },
			        new Binding() { DisplayName = "Save State 6", Bindings = "Shift+F6", TabGroup = "Save States", DefaultBinding = "Shift+F6", Ordinal = 7 },
			        new Binding() { DisplayName = "Save State 7", Bindings = "Shift+F7", TabGroup = "Save States", DefaultBinding = "Shift+F7", Ordinal = 8 },
			        new Binding() { DisplayName = "Save State 8", Bindings = "Shift+F8", TabGroup = "Save States", DefaultBinding = "Shift+F8", Ordinal = 9 },
			        new Binding() { DisplayName = "Save State 9", Bindings = "Shift+F9", TabGroup = "Save States", DefaultBinding = "Shift+F9", Ordinal = 10 },
			        new Binding() { DisplayName = "Load State 0", Bindings = "F10", TabGroup = "Save States", DefaultBinding = "F10", Ordinal = 11 },
			        new Binding() { DisplayName = "Load State 1", Bindings = "F1", TabGroup = "Save States", DefaultBinding = "F1", Ordinal = 12 },
			        new Binding() { DisplayName = "Load State 2", Bindings = "F2", TabGroup = "Save States", DefaultBinding = "F2", Ordinal = 13 },
			        new Binding() { DisplayName = "Load State 3", Bindings = "F3", TabGroup = "Save States", DefaultBinding = "F3", Ordinal = 14 },
			        new Binding() { DisplayName = "Load State 4", Bindings = "F4", TabGroup = "Save States", DefaultBinding = "F4", Ordinal = 15 },
			        new Binding() { DisplayName = "Load State 5", Bindings = "F5", TabGroup = "Save States", DefaultBinding = "F5", Ordinal = 16 },
			        new Binding() { DisplayName = "Load State 6", Bindings = "F6", TabGroup = "Save States", DefaultBinding = "F6", Ordinal = 17 },
			        new Binding() { DisplayName = "Load State 7", Bindings = "F7", TabGroup = "Save States", DefaultBinding = "F7", Ordinal = 18 },
			        new Binding() { DisplayName = "Load State 8", Bindings = "F8", TabGroup = "Save States", DefaultBinding = "F8", Ordinal = 19 },
			        new Binding() { DisplayName = "Load State 9", Bindings = "F9", TabGroup = "Save States", DefaultBinding = "F9", Ordinal = 20 },
			        new Binding() { DisplayName = "Select State 0", Bindings = "D0", TabGroup = "Save States", DefaultBinding = "D0", Ordinal = 21 },
			        new Binding() { DisplayName = "Select State 1", Bindings = "D1", TabGroup = "Save States", DefaultBinding = "D1", Ordinal = 22 },
			        new Binding() { DisplayName = "Select State 2", Bindings = "D2", TabGroup = "Save States", DefaultBinding = "D2", Ordinal = 23 },
			        new Binding() { DisplayName = "Select State 3", Bindings = "D3", TabGroup = "Save States", DefaultBinding = "D3", Ordinal = 24 },
			        new Binding() { DisplayName = "Select State 4", Bindings = "D4", TabGroup = "Save States", DefaultBinding = "D4", Ordinal = 25 },
			        new Binding() { DisplayName = "Select State 5", Bindings = "D5", TabGroup = "Save States", DefaultBinding = "D5", Ordinal = 26 },
			        new Binding() { DisplayName = "Select State 6", Bindings = "D6", TabGroup = "Save States", DefaultBinding = "D6", Ordinal = 27 },
			        new Binding() { DisplayName = "Select State 7", Bindings = "D7", TabGroup = "Save States", DefaultBinding = "D7", Ordinal = 28 },
			        new Binding() { DisplayName = "Select State 8", Bindings = "D8", TabGroup = "Save States", DefaultBinding = "D8", Ordinal = 29 },
			        new Binding() { DisplayName = "Select State 9", Bindings = "D9", TabGroup = "Save States", DefaultBinding = "D9", Ordinal = 30 },
			        new Binding() { DisplayName = "Save Named State", Bindings = "", TabGroup = "Save States", DefaultBinding = "", Ordinal = 31 },
			        new Binding() { DisplayName = "Load Named State", Bindings = "", TabGroup = "Save States", DefaultBinding = "", Ordinal = 32 },
			        new Binding() { DisplayName = "Previous Slot", Bindings = "", TabGroup = "Save States", DefaultBinding = "", Ordinal = 33 },
			        new Binding() { DisplayName = "Next Slot", Bindings = "", TabGroup = "Save States", DefaultBinding = "", Ordinal = 34 },

			        //Movie
			        new Binding() { DisplayName = "Toggle read-only", Bindings = "Q", TabGroup = "Movie", DefaultBinding = "Q", Ordinal = 0 },
			        new Binding() { DisplayName = "Play Movie", Bindings = "", TabGroup = "Movie", DefaultBinding = "", Ordinal = 1 },
			        new Binding() { DisplayName = "Record Movie", Bindings = "", TabGroup = "Movie", DefaultBinding = "", Ordinal = 2 },
			        new Binding() { DisplayName = "Stop Movie", Bindings = "", TabGroup = "Movie", DefaultBinding = "", Ordinal = 3 },
			        new Binding() { DisplayName = "Play from beginning", Bindings = "", TabGroup = "Movie", DefaultBinding = "", Ordinal = 4 },
			        new Binding() { DisplayName = "Save Movie", Bindings = "", TabGroup = "Movie", DefaultBinding = "", Ordinal = 5 },
			        new Binding() { DisplayName = "Toggle MultiTrack", Bindings = "", TabGroup = "Movie", DefaultBinding = "", Ordinal = 6 },
			        new Binding() { DisplayName = "MT Select All", Bindings = "", TabGroup = "Movie", DefaultBinding = "", Ordinal = 7 },
			        new Binding() { DisplayName = "MT Select None", Bindings = "", TabGroup = "Movie", DefaultBinding = "", Ordinal = 8 },
			        new Binding() { DisplayName = "MT Increment Player", Bindings = "", TabGroup = "Movie", DefaultBinding = "", Ordinal = 9 },
			        new Binding() { DisplayName = "MT Decrement Player", Bindings = "", TabGroup = "Movie", DefaultBinding = "", Ordinal = 10 },
			        new Binding() { DisplayName = "Movie Poke", Bindings = "", TabGroup = "Movie", DefaultBinding = "", Ordinal = 11 },
			        new Binding() { DisplayName = "Scrub Input", Bindings = "", TabGroup = "Movie", DefaultBinding = "", Ordinal = 12 },

			        //Tools
			        new Binding() { DisplayName = "Ram Watch", Bindings = "", TabGroup = "Tools", DefaultBinding = "", Ordinal = 0 },
			        new Binding() { DisplayName = "Ram Search", Bindings = "", TabGroup = "Tools", DefaultBinding = "", Ordinal = 1 },
			        new Binding() { DisplayName = "Ram Poke", Bindings = "", TabGroup = "Tools", DefaultBinding = "", Ordinal = 2 },
			        new Binding() { DisplayName = "Hex Editor", Bindings = "", TabGroup = "Tools", DefaultBinding = "", Ordinal = 3 },
                    new Binding() { DisplayName = "Trace Logger", Bindings = "", TabGroup = "Tools", DefaultBinding = "", Ordinal = 4 },
			        new Binding() { DisplayName = "Lua Console", Bindings = "", TabGroup = "Tools", DefaultBinding = "", Ordinal = 5 },
			        new Binding() { DisplayName = "Cheats", Bindings = "", TabGroup = "Tools", DefaultBinding = "", Ordinal = 6 },
			        new Binding() { DisplayName = "TAStudio", Bindings = "", TabGroup = "Tools", DefaultBinding = "", Ordinal = 7 },
			        new Binding() { DisplayName = "ToolBox", Bindings = "T", TabGroup = "Tools", DefaultBinding = "", Ordinal = 8 },
			        new Binding() { DisplayName = "Virtual Pad", Bindings = "", TabGroup = "Tools", DefaultBinding = "", Ordinal = 9 },

			        //SNES
			        new Binding() { DisplayName = "Toggle BG 1", Bindings = "", TabGroup = "SNES", DefaultBinding = "", Ordinal = 0 },
			        new Binding() { DisplayName = "Toggle BG 2", Bindings = "", TabGroup = "SNES", DefaultBinding = "", Ordinal = 1 },
			        new Binding() { DisplayName = "Toggle BG 3", Bindings = "", TabGroup = "SNES", DefaultBinding = "", Ordinal = 2 },
			        new Binding() { DisplayName = "Toggle BG 4", Bindings = "", TabGroup = "SNES", DefaultBinding = "", Ordinal = 3 },
			        new Binding() { DisplayName = "Toggle OBJ 1", Bindings = "", TabGroup = "SNES", DefaultBinding = "", Ordinal = 4 },
			        new Binding() { DisplayName = "Toggle OBJ 2", Bindings = "", TabGroup = "SNES", DefaultBinding = "", Ordinal = 5 },
			        new Binding() { DisplayName = "Toggle OBJ 3", Bindings = "", TabGroup = "SNES", DefaultBinding = "", Ordinal = 6 },
			        new Binding() { DisplayName = "Toggle OBJ 4", Bindings = "", TabGroup = "SNES", DefaultBinding = "", Ordinal = 7 },

			        //Analog
			        new Binding() { DisplayName = "Y Up Small", Bindings = "", TabGroup = "Analog", DefaultBinding = "", Ordinal = 0 },
			        new Binding() { DisplayName = "Y Up Large", Bindings = "", TabGroup = "Analog", DefaultBinding = "", Ordinal = 1 },
			        new Binding() { DisplayName = "Y Down Small", Bindings = "", TabGroup = "Analog", DefaultBinding = "", Ordinal = 2 },
			        new Binding() { DisplayName = "Y Down Large", Bindings = "", TabGroup = "Analog", DefaultBinding = "", Ordinal = 3 },
			        new Binding() { DisplayName = "X Up Small", Bindings = "", TabGroup = "Analog", DefaultBinding = "", Ordinal = 4 },
			        new Binding() { DisplayName = "X Up Large", Bindings = "", TabGroup = "Analog", DefaultBinding = "", Ordinal = 5 },
			        new Binding() { DisplayName = "X Down Small", Bindings = "", TabGroup = "Analog", DefaultBinding = "", Ordinal = 6 },
			        new Binding() { DisplayName = "X Down Large", Bindings = "", TabGroup = "Analog", DefaultBinding = "", Ordinal = 7 },
			
		        };
			}
		}
	}

	public class Binding
	{
		//TODO: how about a delegate, that would be called by the mainform? Thereby putting all the action logic in one place
		public string DisplayName;
		public string Bindings;
		public string DefaultBinding;
		public string TabGroup;
		public int Ordinal = 0;
		public Binding() { }
	}

	public class PathEntryCollection : IEnumerable<PathEntry>
	{
		public List<PathEntry> Paths { get; private set; }

		public PathEntryCollection()
		{
			Paths = new List<PathEntry>();
			Paths.AddRange(DefaultValues);
		}

		public void Add(PathEntry p)
		{
			Paths.Add(p);
		}

		public IEnumerator<PathEntry> GetEnumerator()
		{
			return Paths.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public PathEntry this[string system, string type]
		{
			get
			{
				return Paths.FirstOrDefault(x => x.HasSystem(system) && x.Type == type);
			}
		}

		public void ResolveWithDefaults()
		{
			//Add missing entries
			foreach (PathEntry defaultpath in DefaultValues)
			{
				var path = Paths.FirstOrDefault(x => x.System == defaultpath.System && x.Type == defaultpath.Type);
				if (path == null)
				{
					Paths.Add(defaultpath);
				}
			}

			List<PathEntry> entriesToRemove = new List<PathEntry>();

			//Remove entries that no longer exist in defaults
			foreach (PathEntry pathEntry in Paths)
			{
				var path = DefaultValues.FirstOrDefault(x => x.System == pathEntry.System && x.Type == pathEntry.Type);
				if (path == null)
				{
					entriesToRemove.Add(pathEntry);
				}
			}

			foreach (PathEntry entry in entriesToRemove)
			{
				Paths.Remove(entry);
			}

			//Add missing displaynames
			var missingDisplayPaths = Paths.Where(x => x.SystemDisplayName == null).ToList();
			foreach (PathEntry path in missingDisplayPaths)
			{
				path.SystemDisplayName = DefaultValues.FirstOrDefault(x => x.System == path.System).SystemDisplayName;
				
			}
		}

		//Some frequently requested paths, made into a property for convenience
		public string WatchPath { get { return Global.Config.PathEntries["Global", "Watch (.wch)"].Path; } }
		public string MoviesPath { get { return Global.Config.PathEntries["Global", "Movies"].Path; } }
		public string LuaPath { get { return Global.Config.PathEntries["Global", "Lua"].Path; } }
		public string LogPath { get { return Global.Config.PathEntries["Global", "Debug Logs"].Path; } }
		public string FirmwaresPath { get { return Global.Config.PathEntries["Global", "Firmware"].Path; } }
		public string AVPath { get { return Global.Config.PathEntries["Global", "A/V Dumps"].Path; } }
		public string GlobalBase { get { return Global.Config.PathEntries["Global", "Base"].Path; } }

		public static List<PathEntry> DefaultValues
		{
			get
			{
				return new List<PathEntry>()
				{
					new PathEntry() { System = "Global_NULL", SystemDisplayName="Global", Type = "Movies", Path = Path.Combine(".", "Movies"), Ordinal = 0 },
					new PathEntry() { System = "Global_NULL", SystemDisplayName="Global", Type = "Movie backups", Path = Path.Combine(".", "Movies", "backup"), Ordinal = 1 },
					new PathEntry() { System = "Global_NULL", SystemDisplayName="Global", Type = "Lua", Path = Path.Combine(".", "Lua"), Ordinal = 2 },
					new PathEntry() { System = "Global_NULL", SystemDisplayName="Global", Type = "Watch (.wch)", Path = ".", Ordinal = 3 },
					new PathEntry() { System = "Global_NULL", SystemDisplayName="Global", Type = "A/V Dumps", Path = ".", Ordinal = 4 },
					new PathEntry() { System = "Global_NULL", SystemDisplayName="Global", Type = "Debug Logs", Path = ".", Ordinal = 5 },
					new PathEntry() { System = "Global_NULL", SystemDisplayName="Global", Type = "Firmware", Path = Path.Combine(".", "Firmware"), Ordinal = 6 },
					new PathEntry() { System = "Global_NULL", SystemDisplayName="Global", Type = "Base ROM", Path = ".", Ordinal = 6 },
					new PathEntry() { System = "Global_NULL", SystemDisplayName="Global", Type = "Base", Path = ".", Ordinal = 6 },

					new PathEntry() { System = "INTV", SystemDisplayName="Intellivision", Type = "Base", Path = Path.Combine(".", "Intellivision"), Ordinal = 0 },
					new PathEntry() { System = "INTV", SystemDisplayName="Intellivision", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "INTV", SystemDisplayName="Intellivision", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "INTV", SystemDisplayName="Intellivision", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "INTV", SystemDisplayName="Intellivision", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "INTV", SystemDisplayName="Intellivision", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
					new PathEntry() { System = "INTV", SystemDisplayName="Intellivision", Type = "Palettes", Path = Path.Combine(".", "Palettes"),  Ordinal = 6 },

					new PathEntry() { System = "NES", SystemDisplayName="NES", Type = "Base", Path = Path.Combine(".", "NES"), Ordinal = 0 },
					new PathEntry() { System = "NES", SystemDisplayName="NES", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "NES", SystemDisplayName="NES", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "NES", SystemDisplayName="NES", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "NES", SystemDisplayName="NES", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "NES", SystemDisplayName="NES", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
					new PathEntry() { System = "NES", SystemDisplayName="NES", Type = "Palettes", Path = Path.Combine(".", "Palettes"),  Ordinal = 6 },

					new PathEntry() { System = "SNES_SGB", SystemDisplayName="SNES", Type = "Base", Path= Path.Combine(".", "SNES"), Ordinal = 0 },
					new PathEntry() { System = "SNES_SGB", SystemDisplayName="SNES", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "SNES_SGB", SystemDisplayName="SNES", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "SNES_SGB", SystemDisplayName="SNES", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "SNES_SGB", SystemDisplayName="SNES", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "SNES_SGB", SystemDisplayName="SNES", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "GBA", SystemDisplayName="GBA", Type = "Base", Path= Path.Combine(".", "GBA"), Ordinal = 0 },
					new PathEntry() { System = "GBA", SystemDisplayName="GBA", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "GBA", SystemDisplayName="GBA", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "GBA", SystemDisplayName="GBA", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "GBA", SystemDisplayName="GBA", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "GBA", SystemDisplayName="GBA", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "SMS", SystemDisplayName="SMS", Type = "Base", Path= Path.Combine(".", "SMS"), Ordinal = 0 },
					new PathEntry() { System = "SMS", SystemDisplayName="SMS", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "SMS", SystemDisplayName="SMS", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "SMS", SystemDisplayName="SMS", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "SMS", SystemDisplayName="SMS", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "SMS", SystemDisplayName="SMS", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "GG", SystemDisplayName="GG", Type = "Base", Path= Path.Combine(".", "Game Gear"), Ordinal = 0 },
					new PathEntry() { System = "GG", SystemDisplayName="GG", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "GG", SystemDisplayName="GG", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "GG", SystemDisplayName="GG", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "GG", SystemDisplayName="GG", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "GG", SystemDisplayName="GG", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "SG", SystemDisplayName="SG", Type = "Base", Path= Path.Combine(".", "SG-1000"), Ordinal = 0 },
					new PathEntry() { System = "SG", SystemDisplayName="SG", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "SG", SystemDisplayName="SG", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "SG", SystemDisplayName="SG", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "SG", SystemDisplayName="SG", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "SG", SystemDisplayName="SG", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "GEN", SystemDisplayName="Genesis", Type = "Base", Path= Path.Combine(".", "Genesis"), Ordinal = 0 },
					new PathEntry() { System = "GEN", SystemDisplayName="Genesis", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "GEN", SystemDisplayName="Genesis", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "GEN", SystemDisplayName="Genesis", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "GEN", SystemDisplayName="Genesis", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "GEN", SystemDisplayName="Genesis", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "PCE_PCECD_SGX", SystemDisplayName="PC Engine", Type = "Base", Path= Path.Combine(".", "PC Engine"), Ordinal = 0 },
					new PathEntry() { System = "PCE_PCECD_SGX", SystemDisplayName="PC Engine", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "PCE_PCECD_SGX", SystemDisplayName="PC Engine", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "PCE_PCECD_SGX", SystemDisplayName="PC Engine", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "PCE_PCECD_SGX", SystemDisplayName="PC Engine", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "PCE_PCECD_SGX", SystemDisplayName="PC Engine", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "GB_GBC", SystemDisplayName="Gameboy", Type = "Base", Path= Path.Combine(".", "Gameboy"), Ordinal = 0 },
					new PathEntry() { System = "GB_GBC", SystemDisplayName="Gameboy", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "GB_GBC", SystemDisplayName="Gameboy", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "GB_GBC", SystemDisplayName="Gameboy", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "GB_GBC", SystemDisplayName="Gameboy", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "GB_GBC", SystemDisplayName="Gameboy", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
					new PathEntry() { System = "GB_GBC", SystemDisplayName="Gameboy", Type = "Palettes", Path = Path.Combine(".", "Palettes"),  Ordinal = 6 },

					new PathEntry() { System = "DGB", SystemDisplayName="Dual Gameboy", Type = "Base", Path= Path.Combine(".", "Dual Gameboy"), Ordinal = 0 },
					new PathEntry() { System = "DGB", SystemDisplayName="Dual Gameboy", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "DGB", SystemDisplayName="Dual Gameboy", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "DGB", SystemDisplayName="Dual Gameboy", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "DGB", SystemDisplayName="Dual Gameboy", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "DGB", SystemDisplayName="Dual Gameboy", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
					new PathEntry() { System = "DGB", SystemDisplayName="Dual Gameboy", Type = "Palettes", Path = Path.Combine(".", "Palettes"),  Ordinal = 6 },

					new PathEntry() { System = "TI83", SystemDisplayName="TI83", Type = "Base", Path= Path.Combine(".", "TI83"), Ordinal = 0 },
					new PathEntry() { System = "TI83", SystemDisplayName="TI83", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "TI83", SystemDisplayName="TI83", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "TI83", SystemDisplayName="TI83", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "TI83", SystemDisplayName="TI83", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "TI83", SystemDisplayName="TI83", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "A26", SystemDisplayName="Atari 2600", Type = "Base", Path= Path.Combine(".", "Atari 2600"), Ordinal = 0 },
					new PathEntry() { System = "A26", SystemDisplayName="Atari 2600", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "A26", SystemDisplayName="Atari 2600", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "A26", SystemDisplayName="Atari 2600", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "A26", SystemDisplayName="Atari 2600", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "A78", SystemDisplayName="Atari 7800", Type = "Base", Path= Path.Combine(".", "Atari 7800"), Ordinal = 0 },
					new PathEntry() { System = "A78", SystemDisplayName="Atari 7800", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "A78", SystemDisplayName="Atari 7800", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "A78", SystemDisplayName="Atari 7800", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "A78", SystemDisplayName="Atari 7800", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "A78", SystemDisplayName="Atari 7800", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "C64", SystemDisplayName="Commodore 64", Type = "Base", Path= Path.Combine(".", "C64"), Ordinal = 0 },
					new PathEntry() { System = "C64", SystemDisplayName="Commodore 64", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "C64", SystemDisplayName="Commodore 64", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "C64", SystemDisplayName="Commodore 64", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "C64", SystemDisplayName="Commodore 64", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "PSX", SystemDisplayName="Playstation", Type = "Base", Path= Path.Combine(".", "PSX"), Ordinal = 0 },
					new PathEntry() { System = "PSX", SystemDisplayName="Playstation", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "PSX", SystemDisplayName="Playstation", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "PSX", SystemDisplayName="Playstation", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "PSX", SystemDisplayName="Playstation", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "PSX", SystemDisplayName="Playstation", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "Coleco", SystemDisplayName = "Coleco", Type = "Base", Path= Path.Combine(".", "Coleco"), Ordinal = 0 },
					new PathEntry() { System = "Coleco", SystemDisplayName = "Coleco", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "Coleco", SystemDisplayName = "Coleco", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "Coleco", SystemDisplayName = "Coleco", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "Coleco", SystemDisplayName = "Coleco", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "N64", SystemDisplayName = "N64", Type = "Base", Path= Path.Combine(".", "N64"), Ordinal = 0 },
					new PathEntry() { System = "N64", SystemDisplayName = "N64", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "N64", SystemDisplayName = "N64", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "N64", SystemDisplayName = "N64", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "N64", SystemDisplayName = "N64", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "N64", SystemDisplayName = "N64", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },

					new PathEntry() { System = "SAT", SystemDisplayName = "Saturn", Type = "Base", Path= Path.Combine(".", "Saturn"), Ordinal = 0 },
					new PathEntry() { System = "SAT", SystemDisplayName = "Saturn", Type = "ROM", Path = ".", Ordinal = 1 },
					new PathEntry() { System = "SAT", SystemDisplayName = "Saturn", Type = "Savestates",  Path= Path.Combine(".", "State"), Ordinal = 2 },
					new PathEntry() { System = "SAT", SystemDisplayName = "Saturn", Type = "Save RAM", Path = Path.Combine(".", "SaveRAM"), Ordinal = 3 },
					new PathEntry() { System = "SAT", SystemDisplayName = "Saturn", Type = "Screenshots", Path = Path.Combine(".", "Screenshots"), Ordinal = 4 },
					new PathEntry() { System = "SAT", SystemDisplayName = "Saturn", Type = "Cheats", Path = Path.Combine(".", "Cheats"), Ordinal = 5 },
				};
			}
		}
	}

	public class PathEntry
	{
		public string SystemDisplayName;
		public string Type;
		public string Path;
		public string System;
		public int Ordinal;
		public PathEntry() { }
         public bool HasSystem(string systemID)
        {
            string[] ids = System.Split('_');
            return ids.Contains(systemID);
        }
	}

	public enum PLUGINTYPE { RICE, GLIDE, GLIDE64MK2 };

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

	public class N64Glide64mk2PluginSettings : iPluginSettings
	{
		public PLUGINTYPE PluginType
		{
			get { return PLUGINTYPE.GLIDE64MK2; }
		}

		public void FillPerGameHacks(GameInfo game)
		{
			if (Global.Config.Glide64mk2Plugin.UseDefaultHacks)
			{
				use_sts1_only = Global.Game.GetBool("Glide64mk2_use_sts1_only", false);
				optimize_texrect = Global.Game.GetBool("Glide64mk2_optimize_texrect", true);
				increase_texrect_edge = Global.Game.GetBool("Glide64mk2_increase_texrect_edge", false);
				ignore_aux_copy = Global.Game.GetBool("Glide64mk2_ignore_aux_copy", false);
				hires_buf_clear = Global.Game.GetBool("Glide64mk2_hires_buf_clear", true);
				force_microcheck = Global.Game.GetBool("Glide64mk2_force_microcheck", false);
				fog = Global.Game.GetBool("Glide64mk2_fog", true);
				fb_smart = Global.Game.GetBool("Glide64mk2_fb_smart", false);
				fb_read_alpha = Global.Game.GetBool("Glide64mk2_fb_read_alpha", false);
				fb_hires = Global.Game.GetBool("Glide64mk2_fb_hires", true);
				detect_cpu_write = Global.Game.GetBool("Glide64mk2_detect_cpu_write", false);
				decrease_fillrect_edge = Global.Game.GetBool("Glide64mk2_decrease_fillrect_edge", false);
				buff_clear = Global.Game.GetBool("Glide64mk2_buff_clear", true);
				alt_tex_size = Global.Game.GetBool("Glide64mk2_alt_tex_size", true);
				swapmode = Global.Game.GetInt("Glide64mk2_swapmode", 1);
				stipple_pattern = Global.Game.GetInt("Glide64mk2_stipple_pattern", 1041204192);
				stipple_mode = Global.Game.GetInt("Glide64mk2_stipple_mode", 2);
				lodmode = Global.Game.GetInt("Glide64mk2_lodmode", 0);
				filtering = Global.Game.GetInt("Glide64mk2_filtering", 0);
				correct_viewport = Global.Game.GetBool("Glide64mk2_correct_viewport", false);
				force_calc_sphere = Global.Game.GetBool("Glide64mk2_force_calc_sphere", false);
				pal230 = Global.Game.GetBool("Glide64mk2_pal230", false);
				texture_correction = Global.Game.GetBool("Glide64mk2_texture_correction", true);
				n64_z_scale = Global.Game.GetBool("Glide64mk2_n64_z_scale", false);
				old_style_adither = Global.Game.GetBool("Glide64mk2_old_style_adither", false);
				zmode_compare_less = Global.Game.GetBool("Glide64mk2_zmode_compare_less", false);
				adjust_aspect = Global.Game.GetBool("Glide64mk2_adjust_aspect", true);
				clip_zmax = Global.Game.GetBool("Glide64mk2_clip_zmax", true);
				clip_zmin = Global.Game.GetBool("Glide64mk2_clip_zmin", false);
				force_quad3d = Global.Game.GetBool("Glide64mk2_force_quad3d", false);
				useless_is_useless = Global.Game.GetBool("Glide64mk2_useless_is_useless", false);
				fb_read_always = Global.Game.GetBool("Glide64mk2_fb_read_always", false);
				aspectmode = Global.Game.GetInt("Glide64mk2_aspectmode", 0);
				fb_crc_mode = Global.Game.GetInt("Glide64mk2_fb_crc_mode", 1);
				enable_hacks_for_game = Global.Game.GetInt("Glide64mk2_enable_hacks_for_game", 0);
				read_back_to_screen = Global.Game.GetInt("Glide64mk2_read_back_to_screen", 0);
				fast_crc = Global.Game.GetBool("Glide64mk2_fast_crc", true);
			}
		}

		public Dictionary<string, object> GetPluginSettings()
		{
			//TODO: deal witn the game depedent settings
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			System.Reflection.FieldInfo[] members = Global.Config.Glide64mk2Plugin.GetType().GetFields();
			foreach (System.Reflection.FieldInfo member in members)
			{
				object field = Global.Config.Glide64mk2Plugin.GetType().GetField(member.Name).GetValue(Global.Config.Glide64mk2Plugin);
				dictionary.Add(member.Name, field);
			}

			return dictionary;
		}

		public bool wrpFBO = true;
		public int card_id = 0;
		public bool use_sts1_only = false;
		public bool optimize_texrect = true;
		public bool increase_texrect_edge = false;
		public bool ignore_aux_copy = false;
		public bool hires_buf_clear = true;
		public bool force_microcheck = false;
		public bool fog = true;
		public bool fb_smart = false;
		public bool fb_read_alpha = false;
		public bool fb_hires = true;
		public bool detect_cpu_write = false;
		public bool decrease_fillrect_edge = false;
		public bool buff_clear = true;
		public bool alt_tex_size = false;
		public int swapmode = 1;
		public int stipple_pattern = 1041204192;
		public int stipple_mode = 2;
		public int lodmode = 0;
		public int filtering = 0;
		public bool wrpAnisotropic = false;
		public bool correct_viewport = false;
		public bool force_calc_sphere = false;
		public bool pal230 = false;
		public bool texture_correction = true;
		public bool n64_z_scale = false;
		public bool old_style_adither = false;
		public bool zmode_compare_less = false;
		public bool adjust_aspect = true;
		public bool clip_zmax = true;
		public bool clip_zmin = false;
		public bool force_quad3d = false;
		public bool useless_is_useless = false;
		public bool fb_read_always = false;
		public bool fb_get_info = false;
		public bool fb_render = true;
		public int aspectmode = 0;
		public int fb_crc_mode = 1;
		public bool fast_crc = true;
		public bool UseDefaultHacks = true;
		public int enable_hacks_for_game = 0;
		public int read_back_to_screen = 0;
	}

	#endregion
}