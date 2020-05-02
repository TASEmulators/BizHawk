using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Cores;

namespace BizHawk.Client.Common
{
	public class Config
	{
		public static string ControlDefaultPath => Path.Combine(PathUtils.ExeDirectoryPath, "defctrl.json");
		
		public static string DefaultIniPath { get; private set; } = Path.Combine(PathUtils.ExeDirectoryPath, "config.ini");

		// Shenanigans
		public static void SetDefaultIniPath(string newDefaultIniPath)
		{
			DefaultIniPath = newDefaultIniPath;
		}

		public Config()
		{
			if (AllTrollers.Count == 0 && AllTrollersAutoFire.Count == 0 && AllTrollersAnalog.Count == 0)
			{
				var cd = ConfigService.Load<DefaultControls>(ControlDefaultPath);
				AllTrollers = cd.AllTrollers;
				AllTrollersAutoFire = cd.AllTrollersAutoFire;
				AllTrollersAnalog = cd.AllTrollersAnalog;
			}
		}

		public void ResolveDefaults()
		{
			PathEntries.ResolveWithDefaults();
			HotkeyBindings.ResolveWithDefaults();
			PathEntries.RefreshTempPath();
		}

		// Core preference for generic file extension, key: file extension, value: a systemID or empty if no preference
		public Dictionary<string, string> PreferredPlatformsForExtensions { get; set; } = new Dictionary<string, string>
		{
			[".bin"] =  "",
			[".rom"] =  "",
			[".iso"] =  "",
			[".img"] = "",
			[".cue"] =  ""
		};

		public PathEntryCollection PathEntries { get; set; } = new PathEntryCollection();

		// BIOS Paths
		// key: sysId+firmwareId; value: absolute path
		public Dictionary<string, string> FirmwareUserSpecifications { get; set; } = new Dictionary<string, string>();

		// General Client Settings
		public int InputHotkeyOverrideOptions { get; set; }
		public bool StackOSDMessages { get; set; } = true;

		public ZoomFactors TargetZoomFactors { get; set; } = new ZoomFactors();

		// choose between 0 and 256
		public int TargetScanlineFilterIntensity { get; set; } = 128;
		public int TargetDisplayFilter { get; set; }
		public int DispFinalFilter { get; set; } = 0; // None
		public string DispUserFilterPath { get; set; } = "";
		public RecentFiles RecentRoms { get; set; } = new RecentFiles(10);
		public bool PauseWhenMenuActivated { get; set; } = true;
		public bool SaveWindowPosition { get; set; } = true;
		public bool StartPaused { get; set; }
		public bool StartFullscreen { get; set; }
		public int MainWndx { get; set; } = -1; // Negative numbers will be ignored
		public int MainWndy { get; set; } = -1;
		public bool RunInBackground { get; set; } = true;
		public bool AcceptBackgroundInput { get; set; }
		public bool AcceptBackgroundInputControllerOnly { get; set; }
		public bool HandleAlternateKeyboardLayouts { get; set; }
		public bool SingleInstanceMode { get; set; }
		public bool AllowUdlr { get; set; }
		public bool ForbidUdlr { get; set; }
		public bool ShowContextMenu { get; set; } = true;
		public bool EnableBackupMovies { get; set; } = true;
		public bool MoviesOnDisk { get; set; }
		public bool MoviesInAwe { get; set; }
		public bool HotkeyConfigAutoTab { get; set; } = true;
		public bool InputConfigAutoTab { get; set; } = true;
		public bool BackupSavestates { get; set; } = true;
		public bool SaveScreenshotWithStates { get; set; } = true;
		public int BigScreenshotSize { get; set; } = 128 * 1024;
		public bool NoLowResLargeScreenshotWithStates { get; set; }
		public int AutofireOn { get; set; } = 1;
		public int AutofireOff { get; set; } = 1;
		public bool AutofireLagFrames { get; set; } = true;
		public int SaveSlot { get; set; } // currently selected savestate slot
		public bool AutoLoadLastSaveSlot { get; set; }
		public bool SkipLagFrame { get; set; }
		public bool SuppressAskSave { get; set; }
		public bool AviCaptureOsd { get; set; }
		public bool ScreenshotCaptureOsd { get; set; }
		public bool FirstBoot { get; set; } = true;
		public bool UpdateAutoCheckEnabled { get; set; }
		public DateTime? UpdateLastCheckTimeUtc { get; set; }
		public string UpdateLatestVersion { get; set; } = "";
		public string UpdateIgnoreVersion { get; set; } = "";
		public bool SkipOutdatedOsCheck { get; set; }

		/// <summary>
		/// Makes a .bak file before any saveram-writing operation (could be extended to make timestamped backups)
		/// </summary>
		public bool BackupSaveram { get; set; } = true;

		/// <summary>
		/// Whether to make AutoSave files at periodic intervals
		/// </summary>
		public bool AutosaveSaveRAM { get; set; }

		/// <summary>
		/// Intervals at which to make AutoSave files
		/// </summary>
		public int FlushSaveRamFrames { get; set; }

		/// <remarks>Don't rename this without changing <c>BizHawk.Client.EmuHawk.Program.CurrentDomain_AssemblyResolve</c></remarks>
		public ELuaEngine LuaEngine { get; set; } = ELuaEngine.LuaPlusLuaInterface;

		public bool TurboSeek { get; set; }

		public MovieEndAction MovieEndAction { get; set; } = MovieEndAction.Finish;

		public ClientProfile SelectedProfile { get; set; } = ClientProfile.Unknown;

		// N64
		public bool N64UseCircularAnalogConstraint { get; set; } = true;

		// Run-Control settings
		public int FrameProgressDelayMs { get; set; } = 500; // how long until a frame advance hold turns into a frame progress?
		public int FrameSkip { get; set; } = 4;
		public int SpeedPercent { get; set; } = 100;
		public int SpeedPercentAlternate { get; set; } = 400;
		public bool ClockThrottle { get; set; }= true;
		public bool AutoMinimizeSkipping { get; set; } = true;
		public bool VSyncThrottle { get; set; } = false;

		public RewindConfig Rewind { get; set; } = new RewindConfig();

		// Savestate settings
		public SaveStateTypeE SaveStateType { get; set; } = SaveStateTypeE.Binary;
		public const int DefaultSaveStateCompressionLevelNormal = 1;
		public int SaveStateCompressionLevelNormal { get; set; } = DefaultSaveStateCompressionLevelNormal;
		public const int DefaultSaveStateCompressionLevelRewind = 0; // this isn't actually used yet 
		public int SaveStateCompressionLevelRewind { get; set; } = DefaultSaveStateCompressionLevelRewind; // this isn't actually used yet 
		public int MovieCompressionLevel { get; set; } = 2;

		/// <summary>
		/// Use vsync when presenting all 3d accelerated windows.
		/// For the main window, if VSyncThrottle = false, this will try to use vsync without throttling to it
		/// </summary>
		public bool VSync { get; set; }

		/// <summary>
		/// Tries to use an alternate vsync mechanism, for video cards that just can't do it right
		/// </summary>
		public bool DispAlternateVsync { get; set; }

		// Display options
		public bool DisplayFps { get; set; }
		public bool DisplayFrameCounter { get; set; }
		public bool DisplayLagCounter { get; set; }
		public bool DisplayInput { get; set; }
		public bool DisplayRerecordCount { get; set; }
		public bool DisplayMessages { get; set; } = true;

		public bool DispFixAspectRatio { get; set; } = true;
		public bool DispFixScaleInteger { get; set; }
		public bool DispFullscreenHacks { get; set; }
		public bool DispAutoPrescale { get; set; }
		public int DispSpeedupFeatures { get; set; } = 2;

		public MessagePosition Fps { get; set; } = DefaultMessagePositions.Fps.Clone();
		public MessagePosition FrameCounter { get; set; } = DefaultMessagePositions.FrameCounter.Clone();
		public MessagePosition LagCounter { get; set; } = DefaultMessagePositions.LagCounter.Clone();
		public MessagePosition InputDisplay { get; set; } = DefaultMessagePositions.InputDisplay.Clone();
		public MessagePosition ReRecordCounter { get; set; } = DefaultMessagePositions.ReRecordCounter.Clone();
		public MessagePosition MultitrackRecorder { get; set; } = DefaultMessagePositions.MultitrackRecorder.Clone();
		public MessagePosition Messages { get; set; } = DefaultMessagePositions.Messages.Clone();
		public MessagePosition Autohold { get; set; } = DefaultMessagePositions.Autohold.Clone();
		public MessagePosition RamWatches { get; set; } = DefaultMessagePositions.RamWatches.Clone();

		public int MessagesColor { get; set; } = DefaultMessagePositions.MessagesColor;
		public int AlertMessageColor { get; set; } = DefaultMessagePositions.AlertMessageColor;
		public int LastInputColor { get; set; } = DefaultMessagePositions.LastInputColor;
		public int MovieInput { get; set; } = DefaultMessagePositions.MovieInput;
		
		public int DispPrescale { get; set; } = 1;

		private static bool DetectDirectX()
		{
			if (OSTailoredCode.IsUnixHost) return false;
			var p = OSTailoredCode.LinkedLibManager.LoadOrNull("d3dx9_43.dll");
			if (p == null) return false;
			OSTailoredCode.LinkedLibManager.FreeByPtr(p.Value);
			return true;
		}

		/// <remarks>warning: we don't even want to deal with changing this at runtime. but we want it changed here for config purposes. so don't check this variable. check in GlobalWin or something like that.</remarks>
		public EDispMethod DispMethod { get; set; } = DetectDirectX() ? EDispMethod.SlimDX9 : EDispMethod.OpenGL;

		public int DispChromeFrameWindowed { get; set; } = 2;
		public bool DispChromeStatusBarWindowed { get; set; } = true;
		public bool DispChromeCaptionWindowed { get; set; } = true;
		public bool DispChromeMenuWindowed { get; set; } = true;
		public bool DispChromeStatusBarFullscreen { get; set; }
		public bool DispChromeMenuFullscreen { get; set; }
		public bool DispChromeFullscreenAutohideMouse { get; set; } = true;
		public bool DispChromeAllowDoubleClickFullscreen { get; set; } = true;

		public EDispManagerAR DispManagerAR { get; set; } = EDispManagerAR.System;

		// these are misnomers. they're actually a fixed size (fixme on major release)
		public int DispCustomUserARWidth { get; set; } = -1;
		public int DispCustomUserARHeight { get; set; } = -1;

		// these are more like the actual AR ratio (i.e. 4:3) (fixme on major release)
		public float DispCustomUserArx { get; set; } = -1;
		public float DispCustomUserAry { get; set; } = -1;

		//these default to 0 because by default we crop nothing
		public int DispCropLeft { get; set; } = 0;
		public int DispCropTop { get; set; } = 0;
		public int DispCropRight { get; set; } = 0;
		public int DispCropBottom { get; set; } = 0;

		// Sound options
		public ESoundOutputMethod SoundOutputMethod { get; set; } = DetectDirectX() ? ESoundOutputMethod.DirectSound : ESoundOutputMethod.OpenAL;
		public bool SoundEnabled { get; set; } = true;
		public bool SoundEnabledNormal { get; set; } = true;
		public bool SoundEnabledRWFF { get; set; } = true;
		public bool MuteFrameAdvance { get; set; } = true;
		public int SoundVolume { get; set; } = 100; // Range 0-100
		public int SoundVolumeRWFF { get; set; } = 50; // Range 0-100
		public bool SoundThrottle { get; set; }
		public string SoundDevice { get; set; } = "";
		public int SoundBufferSizeMs { get; set; } = 100;

		// Lua
		public RecentFiles RecentLua { get; set; } = new RecentFiles(8);
		public RecentFiles RecentLuaSession { get; set; } = new RecentFiles(8);
		public bool DisableLuaScriptsOnLoad { get; set; }
		public bool RunLuaDuringTurbo { get; set; } = true;

		// Watch Settings
		public RecentFiles RecentWatches { get; set; } = new RecentFiles(8);
		public PreviousType RamWatchDefinePrevious { get; set; } = PreviousType.LastFrame;
		public bool DisplayRamWatch { get; set; }

		// Video dumping settings
		public string VideoWriter { get; set; } = "";
		public int JmdCompression { get; set; } = 3;
		public int JmdThreads { get; set; } = 3;
		public string FFmpegFormat { get; set; } = "";
		public string FFmpegCustomCommand { get; set; } = "-c:a foo -c:v bar -f baz";
		public string AviCodecToken { get; set; } = "";
		public int GifWriterFrameskip { get; set; } = 3;
		public int GifWriterDelay { get; set; } = -1;
		public bool VideoWriterAudioSync { get; set; } = true;

		// Emulation core settings
		public Dictionary<string, object> CoreSettings { get; set; } = new Dictionary<string, object>();
		public Dictionary<string, object> CoreSyncSettings { get; set; } = new Dictionary<string, object>();

		public Dictionary<string, ToolDialogSettings> CommonToolSettings { get; set; } = new Dictionary<string, ToolDialogSettings>();
		public Dictionary<string, Dictionary<string, object>> CustomToolSettings { get; set; } = new Dictionary<string, Dictionary<string, object>>();

		// Cheats
		public bool DisableCheatsOnLoad { get; set; }
		public bool LoadCheatFileByGame { get; set; } = true;
		public bool CheatsAutoSaveOnClose { get; set; } = true;
		public RecentFiles RecentCheats { get; set; } = new RecentFiles(8);

		// TAStudio
		public TasStateManagerSettings DefaultTasStateManagerSettings { get; set; } = new TasStateManagerSettings();

		// Macro Tool
		public RecentFiles RecentMacros { get; set; } = new RecentFiles(8);

		// Movie Settings
		public RecentFiles RecentMovies { get; set; } = new RecentFiles(8);
		public string DefaultAuthor { get; set; } = "default user";
		public bool UseDefaultAuthor { get; set; } = true;
		public bool DisplaySubtitles { get; set; } = true;
		public bool VBAStyleMovieLoadState { get; set; }
		public bool MoviePlaybackPokeMode { get; set; }

		// Play Movie Dialog
		public bool PlayMovieIncludeSubDir { get; set; }
		public bool PlayMovieMatchHash { get; set; } = true;

		// TI83
		public bool Ti83AutoloadKeyPad { get; set; } = true;

		public BindingCollection HotkeyBindings { get; set; } = new BindingCollection();

		// Analog Hotkey values
		public int AnalogLargeChange { get; set; } = 10;
		public int AnalogSmallChange { get; set; } = 1;

		// [ControllerType][ButtonName] => Physical Bind
		public Dictionary<string, Dictionary<string, string>> AllTrollers { get; set; } = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, Dictionary<string, string>> AllTrollersAutoFire { get; set; } = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, Dictionary<string, AnalogBind>> AllTrollersAnalog { get; set; } = new Dictionary<string, Dictionary<string, AnalogBind>>();

		// Core Pick
		// as this setting spans multiple cores and doesn't actually affect the behavior of any core,
		// it hasn't been absorbed into the new system
		public bool GbAsSgb { get; set; }
		public bool SgbUseBsnes { get; set; }
		public bool CoreForcingViaGameDb { get; set; } = true;
		public string LibretroCore { get; set; }

		public Dictionary<string, string> PreferredCores = new Dictionary<string, string>
		{
			["NES"] = CoreNames.QuickNes,
			["SNES"] = CoreNames.Snes9X,
			["GB"] = CoreNames.Gambatte
		};

		// ReSharper disable once UnusedMember.Global
		public string LastWrittenFrom { get; set; } = VersionInfo.MainVersion;

		// ReSharper disable once UnusedMember.Global
		public string LastWrittenFromDetailed { get; set; } = VersionInfo.GetEmuVersion();

		public EHostInputMethod HostInputMethod { get; set; } = EHostInputMethod.OpenTK;
	}
}