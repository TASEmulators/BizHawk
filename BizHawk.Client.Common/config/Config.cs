using System;
using System.Collections.Generic;
using System.Drawing;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class Config
	{
		public static string ControlDefaultPath
		{
			get { return PathManager.MakeProgramRelativePath("defctrl.json"); }
		}

		public void ConfigCheckAllControlDefaults()
		{
			if (AllTrollers.Count == 0 && AllTrollersAutoFire.Count == 0 && AllTrollersAnalog.Count == 0)
			{
				var cd = ConfigService.Load<ControlDefaults>(ControlDefaultPath);
				AllTrollers = cd.AllTrollers;
				AllTrollersAutoFire = cd.AllTrollersAutoFire;
				AllTrollersAnalog = cd.AllTrollersAnalog;
			}
		}

		public Config()
		{
			ConfigCheckAllControlDefaults();
		}

		public void ResolveDefaults()
		{
			PathEntries.ResolveWithDefaults();
			HotkeyBindings.ResolveWithDefaults();
		}

		// Path Settings ************************************/
		public bool UseRecentForROMs = false;
		public string LastRomPath = ".";
		public PathEntryCollection PathEntries = new PathEntryCollection();

		// BIOS Paths
		public Dictionary<string, string> FirmwareUserSpecifications = new Dictionary<string, string>(); // key: sysid+firmwareId; value: absolute path

		public string FFMpegPath = "%exe%/dll/ffmpeg.exe";

		// General Client Settings
		public int Input_Hotkey_OverrideOptions = 0;
		public bool StackOSDMessages = true;
		public int TargetZoomFactor = 2;
		public int TargetScanlineFilterIntensity = 0; // choose between 0 and 256
		public RecentFiles RecentRoms = new RecentFiles(8);
		public bool PauseWhenMenuActivated = true;
		public bool SaveWindowPosition = true;
		public bool StartPaused = false;
		public int MainWndx = -1; // Negative numbers will be ignored
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
		public int FrameProgressDelayMs = 500; // how long until a frame advance hold turns into a frame progress?
		public int FrameSkip = 4;
		public int SpeedPercent = 100;
		public int SpeedPercentAlternate = 400;
		public bool ClockThrottle = true;
		public bool AutoMinimizeSkipping = true;
		public bool VSyncThrottle = false;

		// Rewind settings
		public bool Rewind_UseDelta = true;
		public bool RewindEnabledSmall = true;
		public bool RewindEnabledMedium = false;
		public bool RewindEnabledLarge = false;
		public int RewindFrequencySmall = 1;
		public int RewindFrequencyMedium = 4;
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
		public int DispFPSanchor = 0;	// 0 = UL, 1 = UR, 2 = DL, 3 = DR
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
		public int SoundVolume = 100; // Range 0-100
		public bool SoundThrottle = false;
		public string SoundDevice = "";

		// Log Window
		public bool LogWindowSaveWindowPosition = true;
		public int LogWindowWndx = -1;
		public int LogWindowWndy = -1;
		public int LogWindowWidth = -1;
		public int LogWindowHeight = -1;

		// Lua Console
		public ToolDialogSettings LuaSettings = new ToolDialogSettings();
		public RecentFiles RecentLua = new RecentFiles(8);
		public RecentFiles RecentLuaSession = new RecentFiles(8);
		public bool AutoLoadLuaConsole = false;
		public bool DisableLuaScriptsOnLoad = false;

		// RamWatch Settings
		public ToolDialogSettings RamWatchSettings = new ToolDialogSettings();
		public RecentFiles RecentWatches = new RecentFiles(8);
		public bool RamWatchShowChangeColumn = true;
		public bool RamWatchShowPrevColumn = false;
		public bool RamWatchShowDiffColumn = false;
		public bool RamWatchShowDomainColumn = true;

		public Dictionary<string, int> RamWatchColumnWidths = new Dictionary<string, int>
			{
			{ "AddressColumn", -1 },
			{ "ValueColumn", -1 },
			{ "PrevColumn", -1 },
			{ "ChangesColumn", -1 },
			{ "DiffColumn", -1 },
			{ "DomainColumn", -1 },
			{ "NotesColumn",-1 },
		};

		public Dictionary<string, int> RamWatchColumnIndexes = new Dictionary<string, int>
			{
			{ "AddressColumn", 0 },
			{ "ValueColumn", 1 },
			{ "PrevColumn", 2 },
			{ "ChangesColumn", 3 },
			{ "DiffColumn", 4 },
			{ "DomainColumn", 5 },
			{ "NotesColumn", 6 },
		};

		public Watch.PreviousType RamWatchDefinePrevious = Watch.PreviousType.LastFrame;

		// RamSearch Settings
		public ToolDialogSettings RamSearchSettings = new ToolDialogSettings();
		public int RamSearchPrev_Type = 1;
		public RecentFiles RecentSearches = new RecentFiles(8);
		public int RamSearchPreviousAs = 0;
		public bool RamSearchPreviewMode = true;
		public bool RamSearchAlwaysExcludeRamWatch = false;
		public int RamSearchAddressWidth = -1;
		public int RamSearchValueWidth = -1;
		public int RamSearchPrevWidth = -1;
		public int RamSearchChangesWidth = -1;
		public int RamSearchAddressIndex = 0;
		public int RamSearchValueIndex = 1;
		public int RamSearchPrevIndex = 2;
		public int RamSearchChangesIndex = 3;
		public bool RamSearchFastMode = false;

		public Dictionary<string, int> RamSearchColumnWidths = new Dictionary<string, int>
		{
			{ "AddressColumn", -1 },
			{ "ValueColumn", -1 },
			{ "PrevColumn", -1 },
			{ "ChangesColumn", -1 },
			{ "DiffColumn", -1 },
		};

		public Dictionary<string, int> RamSearchColumnIndexes = new Dictionary<string, int>
			{
			{ "AddressColumn", 0 },
			{ "ValueColumn", 1 },
			{ "PrevColumn", 2 },
			{ "ChangesColumn", 3 },
			{ "DiffColumn", 4 },
		};

		public bool RamSearchShowPrevColumn = true;
		public bool RamSearchShowChangeColumn = true;
		public bool RamSearchShowDiffColumn = false;

		// HexEditor Settings
		public ToolDialogSettings HexEditorSettings = new ToolDialogSettings();
		public bool AutoLoadHexEditor = false;
		public bool HexEditorBigEndian = false;
		public int HexEditorDataSize = 1;

		// Hex Editor Colors
		public Color HexBackgrndColor = Color.FromName("Control");
		public Color HexForegrndColor = Color.FromName("ControlText");
		public Color HexMenubarColor = Color.FromName("Control");
		public Color HexFreezeColor = Color.LightBlue;
		public Color HexHighlightColor = Color.Pink;
		public Color HexHighlightFreezeColor = Color.Violet;

		// Trace Logger Settings
		public ToolDialogSettings TraceLoggerSettings = new ToolDialogSettings();
		public bool TraceLoggerAutoLoad = false;
		public int TraceLoggerMaxLines = 100000;

		// Video dumping settings
		public string VideoWriter = "";
		public int JMDCompression = 3;
		public int JMDThreads = 3;
		public string FFmpegFormat = "";
		public string FFmpegCustomCommand = "-c:a foo -c:v bar -f baz";
		public string AVICodecToken = "";
		public int GifWriterFrameskip = 3;
		public int GifWriterDelay = -1;

		#region emulation core settings

		public Dictionary<string, object> CoreSettings = new Dictionary<string, object>();
		public Dictionary<string, object> CoreSyncSettings = new Dictionary<string, object>();

		public object GetCoreSettings<T>()
			where T : IEmulator
		{
			return GetCoreSettings(typeof(T));
		}

		public object GetCoreSettings(Type t)
		{
			object ret;
			CoreSettings.TryGetValue(t.ToString(), out ret);
			return ret;
		}

		public void PutCoreSettings<T>(object o)
			where T : IEmulator
		{
			PutCoreSettings(o, typeof(T));
		}

		public void PutCoreSettings(object o, Type t)
		{
			if (o != null)
			{
				CoreSettings[t.ToString()] = o;
			}
			else
			{
				CoreSettings.Remove(t.ToString());
			}
		}

		public object GetCoreSyncSettings<T>()
			where T : IEmulator
		{
			return GetCoreSyncSettings(typeof(T));
		}

		public object GetCoreSyncSettings(Type t)
		{
			object ret;
			CoreSyncSettings.TryGetValue(t.ToString(), out ret);
			return ret;
		}

		public void PutCoreSyncSettings<T>(object o)
			where T : IEmulator
		{
			PutCoreSyncSettings(o, typeof(T));
		}

		public void PutCoreSyncSettings(object o, Type t)
		{
			if (o != null)
			{
				CoreSyncSettings[t.ToString()] = o;
			}
			else
			{
				CoreSyncSettings.Remove(t.ToString());
			}
		}

		#endregion

		// NESPPU Settings
		public ToolDialogSettings NesPPUSettings = new ToolDialogSettings();
		public bool AutoLoadNESPPU = false;
		public int NESPPURefreshRate = 4;

		// NESDebuger Settings
		public bool AutoLoadNESDebugger = false;
		public bool NESDebuggerSaveWindowPosition = true;
		public int NESDebuggerWndx = -1;
		public int NESDebuggerWndy = -1;
		public int NESDebuggerWidth = -1;
		public int NESDebuggerHeight = -1;

		// NES NameTableViewer Settings
		public ToolDialogSettings NesNameTableSettings = new ToolDialogSettings();
		public bool AutoLoadNESNameTable = false;
		public int NESNameTableRefreshRate = 4;

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

		// PCE BG Viewer settings
		public ToolDialogSettings PceBgViewerSettings = new ToolDialogSettings();
		public bool PCEBGViewerAutoload = false;
		public int PCEBGViewerRefreshRate = 16;

		#region Cheats Dialog

		public ToolDialogSettings CheatsSettings = new ToolDialogSettings();
		public bool Cheats_ValuesAsHex = true;
		public bool DisableCheatsOnLoad = false;
		public bool LoadCheatFileByGame = true;
		public bool CheatsAutoSaveOnClose = true;
		public RecentFiles RecentCheats = new RecentFiles(8);
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

		public Dictionary<string, int> CheatsColumnWidths = new Dictionary<string, int>
			{
			{ "NamesColumn", -1 },
			{ "AddressColumn", -1 },
			{ "ValueColumn", -1 },
			{ "CompareColumn", -1 },
			{ "OnColumn", -1 },
			{ "DomainColumn", -1 },
			{ "SizeColumn", -1 },
			{ "EndianColumn", -1 },
			{ "DisplayTypeColumn", -1 },
		};

		public Dictionary<string, int> CheatsColumnIndices = new Dictionary<string, int>
			{
			{ "NamesColumn", 0 },
			{ "AddressColumn", 1 },
			{ "ValueColumn", 2 },
			{ "CompareColumn", 3 },
			{ "OnColumn", 4 },
			{ "DomainColumn", 5 },
			{ "SizeColumn", 6 },
			{ "EndianColumn", 7 },
			{ "DisplayTypeColumn", 8 },
		};

		public Dictionary<string, bool> CheatsColumnShow = new Dictionary<string, bool>
			{
			{ "NamesColumn", true },
			{ "AddressColumn", true },
			{ "ValueColumn", true },
			{ "CompareColumn", true },
			{ "OnColumn", false },
			{ "DomainColumn", true },
			{ "SizeColumn", true },
			{ "EndianColumn", false },
			{ "DisplayTypeColumn", false },
		};

		#endregion

		// TAStudio Dialog
		public bool TAStudioSaveWindowPosition = true;
		public bool AutoloadTAStudio = false;
		public bool AutoloadTAStudioProject = false;
		public bool TAStudioTopMost = false;
		public int TASWndx = -1;
		public int TASWndy = -1;
		public int TASWidth = -1;
		public int TASHeight = -1;
		public bool TAStudioDrawInput = true;
		public RecentFiles RecentTas = new RecentFiles(8);

		// VirtualPad Dialog
		public ToolDialogSettings VirtualPadSettings = new ToolDialogSettings();
		public bool VirtualPadsUpdatePads = true;
		public bool AutoloadVirtualPad = false;
		public bool VirtualPadSticky = true;

		// NES Game Genie Encoder/Decoder
		public ToolDialogSettings NesGGSettings = new ToolDialogSettings();
		public bool NESGGAutoload = false;

		// SNES Game Genie Encoder/Decoder
		public bool SNESGGAutoload = false;
		public bool SNESGGSaveWindowPosition = true;
		public int SNESGGWndx = -1;
		public int SNESGGWndy = -1;

		// GB/GG Game Genie Encoder/Decoder
		public ToolDialogSettings GbGGSettings = new ToolDialogSettings();
		public bool GBGGAutoload = false;
		

		// GEN Game Genie Encoder/Decoder
		public ToolDialogSettings GenGGSettings = new ToolDialogSettings();
		public bool GenGGAutoload = false;

		// Movie Settings
		public RecentFiles RecentMovies = new RecentFiles(8);
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

		// Analog Hotkey values
		public int Analog_LargeChange = 10;
		public int Analog_SmallChange = 1;

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

		// Core Pick
		// as this setting spans multiple cores and doesn't actually affect the behavior of any core,
		// it hasn't been absorbed into the new system
		public bool GB_AsSGB = false;
		public bool NES_InQuickNES = false;

		// LuaWriter Settings
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
		public bool LuaShowLineNumbers = false;
	}

	// These are used in the defctrl.json or wherever
	public class ControlDefaults
	{
		public Dictionary<string, Dictionary<string, string>> AllTrollers = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, Dictionary<string, string>> AllTrollersAutoFire = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, Dictionary<string, Config.AnalogBind>> AllTrollersAnalog = new Dictionary<string, Dictionary<string, Config.AnalogBind>>();
	}
}