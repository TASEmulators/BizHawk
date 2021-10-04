using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;

using System.ComponentModel;
using System.IO;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	[PortedCore(CoreNames.MelonDS, "Arisotura", "0.9.3", "http://melonds.kuribo64.net/", singleInstance: true, isReleased: false)]
	public class NDS : WaterboxCore, ISaveRam,
		ISettable<NDS.Settings, NDS.SyncSettings>
	{

		private readonly LibMelonDS _core;
		private readonly bool _dsi;
		private readonly bool _skipfw;

		private byte[] bios7;
		private byte[] bios9;
		private byte[] fw;
		private byte[] dsrom;
		private byte[] sd;
		private byte[] gbarom;
		private byte[] gbasram;
		private byte[] bios7i;
		private byte[] bios9i;
		private byte[] nand;

		public delegate void FileOpenCallback(string file);
		public delegate void FileCloseCallback(string file);

		[CoreConstructor("NDS")]
		public NDS(byte[] rom, CoreComm comm, Settings settings, SyncSettings syncSettings, bool deterministic)
			: base(comm, new Configuration
			{
				DefaultWidth = 256,
				DefaultHeight = 384,
				MaxWidth = 256,
				MaxHeight = 384,
				MaxSamples = 1024,
				DefaultFpsNumerator = 33513982,
				DefaultFpsDenominator = 560190,
				SystemId = "NDS"
			})
		{
			_coreFileOpenCallback = FileOpenCb;
			_coreFileCloseCallback = FileCloseCb;

			_core = PreInit<LibMelonDS>(new WaterboxOptions
			{
				Filename = "melonds.wbx",
				SbrkHeapSizeKB = 192,
				InvisibleHeapSizeKB = 12,
				SealedHeapSizeKB = 9 * 1024,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 1024,
				SkipCoreConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { _coreFileOpenCallback, _coreFileCloseCallback });

			_syncSettings = syncSettings ?? new SyncSettings();
			_settings = settings ?? new Settings();

			_dsi = _syncSettings.UseDSi;

			fw = _dsi
				? comm.CoreFileProvider.GetFirmware(new("NDS", "DSi Firmware"))
				: comm.CoreFileProvider.GetFirmware(new("NDS", "DS Firmware"));

			_skipfw = _syncSettings.DirectBoot || !_syncSettings.UseRealDSBIOS || fw == null;

			bios7 = _syncSettings.UseRealDSBIOS
				? comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "BIOS7"))
				: null;

			bios9 = _syncSettings.UseRealDSBIOS
				? comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "BIOS9"))
				: null;

			Array.Copy(rom, dsrom, rom.Length);

			sd = _syncSettings.SDCardEnable
				? comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "SD Card Image"))
				: null;

			gbarom = _syncSettings.GBACartPresent && !_dsi
				? comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "GBA ROM"))
				: null;

			gbasram = _syncSettings.GBACartPresent && !_dsi
				? comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "GBA SaveRAM"))
				: null;

			bios7i = _dsi
				? comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "BIOS7i"))
				: null;

			bios9i = _dsi
				? comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "BIOS9i"))
				: null;

			nand = _dsi
				? comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "DSi NAND"))
				: null;

			LibMelonDS.LoadFlags flags = LibMelonDS.LoadFlags.NONE;

			if (_dsi)
			{
				flags |= LibMelonDS.LoadFlags.USE_DSI;
			}

			if (_syncSettings.UseRealDSBIOS)
			{
				flags |= LibMelonDS.LoadFlags.USE_REAL_DS_BIOS;
			}

			if (_skipfw)
			{
				flags |= LibMelonDS.LoadFlags.SKIP_FIRMWARE;
			}

			if (_syncSettings.SDCardEnable)
			{
				flags |= LibMelonDS.LoadFlags.SD_CARD_ENABLE;
			}

			if (_syncSettings.GBACartPresent)
			{
				flags |= LibMelonDS.LoadFlags.GBA_CART_PRESENT;
			}

			if (_syncSettings.AccurateAudioBitrate)
			{
				flags |= LibMelonDS.LoadFlags.ACCURATE_AUDIO_BITRATE;
			}

			if (_syncSettings.FirmwareOverride || deterministic)
			{
				flags |= LibMelonDS.LoadFlags.FIRMWARE_OVERRIDE;
			}

			_core.SetFileOpenCallback(_coreFileOpenCallback);
			_core.SetFileCloseCallback(_coreFileCloseCallback);

			if (!_core.Init(flags))
			{
				throw new InvalidOperationException("Core rejected the rom!");
			}

			PostInit();

			DeterministicEmulation = deterministic || !_syncSettings.UseRealTime;
			InitializeRtc(_syncSettings.InitialTime);
		}

		public override ControllerDefinition ControllerDefinition => NDSController;

		public static readonly ControllerDefinition NDSController = new ControllerDefinition
		{
			Name = "NDS Controller",
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Y", "X", "L", "R", "LidOpen", "LidClose", "Power", "Touch"
			}
		}.AddXYPair("Touch{0}", AxisPairOrientation.RightAndUp, 0.RangeTo(255), 128, 0.RangeTo(191), 96)
			.AddAxis("GBA Light Sensor", 0.RangeTo(255), 0);
		private LibMelonDS.Buttons GetButtons(IController c)
		{
			LibMelonDS.Buttons b = 0;
			if (c.IsPressed("Up"))
				b |= LibMelonDS.Buttons.UP;
			if (c.IsPressed("Down"))
				b |= LibMelonDS.Buttons.DOWN;
			if (c.IsPressed("Left"))
				b |= LibMelonDS.Buttons.LEFT;
			if (c.IsPressed("Right"))
				b |= LibMelonDS.Buttons.RIGHT;
			if (c.IsPressed("Start"))
				b |= LibMelonDS.Buttons.START;
			if (c.IsPressed("Select"))
				b |= LibMelonDS.Buttons.SELECT;
			if (c.IsPressed("B"))
				b |= LibMelonDS.Buttons.B;
			if (c.IsPressed("A"))
				b |= LibMelonDS.Buttons.A;
			if (c.IsPressed("Y"))
				b |= LibMelonDS.Buttons.Y;
			if (c.IsPressed("X"))
				b |= LibMelonDS.Buttons.X;
			if (c.IsPressed("L"))
				b |= LibMelonDS.Buttons.L;
			if (c.IsPressed("R"))
				b |= LibMelonDS.Buttons.R;
			if (c.IsPressed("LidOpen"))
				b |= LibMelonDS.Buttons.LIDOPEN;
			if (c.IsPressed("LidClose"))
				b |= LibMelonDS.Buttons.LIDCLOSE;
			if (c.IsPressed("Power"))
				b |= LibMelonDS.Buttons.POWER;
			if (c.IsPressed("Touch"))
				b |= LibMelonDS.Buttons.TOUCH;

			return b;
		}

		public new bool SaveRamModified => _core.HasSaveRam();

		public new byte[] CloneSaveRam()
		{
			_exe.AddTransientFile(new byte[0], "save.ram");
			_core.GetSaveRam();
			return _exe.RemoveTransientFile("save.ram");
		}

		public new void StoreSaveRam(byte[] data)
		{
			_exe.AddReadonlyFile(data, "save.ram");
			_core.PutSaveRam();
			_exe.RemoveReadonlyFile("save.ram");
		}

		private Settings _settings;
		private SyncSettings _syncSettings;

		public enum ScreenLayoutKind
		{
			Vertical, Horizontal, Top, Bottom
		}

		public enum ScreenRotationKind
		{
			Rotate0, Rotate90, Rotate180, Rotate270
		}

		public class Settings
		{
			[DisplayName("Screen Layout")]
			[Description("Adjusts the layout of the screens")]
			[DefaultValue(ScreenLayoutKind.Vertical)]
			public ScreenLayoutKind ScreenLayout { get; set; }

			[DisplayName("Rotation")]
			[Description("Adjusts the orientation of the screens")]
			[DefaultValue(ScreenRotationKind.Rotate0)]
			public ScreenRotationKind ScreenRotation { get; set; }

			[DisplayName("Screen Gap")]
			[DefaultValue(0)]
			public int ScreenGap { get; set; }

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}

			public static bool NeedsReboot(Settings x, Settings y)
			{
				return false;
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public class SyncSettings
		{
			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.")]
			[DefaultValue(typeof(DateTime), "2000-01-01")]
			public DateTime InitialTime { get; set; }

			[DisplayName("Use RealTime")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time.  Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			[Description("If true, real DS BIOS files will be used.")]
			[DefaultValue(false)]
			public bool UseRealDSBIOS { get; set; }

			[Description("If true, initial firmware boot will be skipped. Forced true if firmware cannot be booted.")]
			[DefaultValue(false)]
			public bool DirectBoot { get; set; }

			[Description("If true, the firmware settings will be overriden by provided setting. Forced true when recording a movie.")]
			[DefaultValue(false)]
			public bool FirmwareOverride { get; set; }

			[Description("")]
			[DefaultValue(false)]
			public bool SDCardEnable { get; set; }

			[Description("If true, a GBA cart will be loaded. Ignored in DSi mode.")]
			[DefaultValue(false)]
			public bool GBACartPresent { get; set; }

			[Description("If true, the audio bitrate will be set to 10. Otherwise, it will be set to 16.")]
			[DefaultValue(true)]
			public bool AccurateAudioBitrate { get; set; }

			[Description("If true, a DSi will be emulated instead of a DS. Highly experimental.")]
			[DefaultValue(false)]
			public bool UseDSi { get; set; }

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public Settings GetSettings()
		{
			return _settings.Clone();
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(Settings o)
		{
			var ret = Settings.NeedsReboot(_settings, o);
			_settings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			return new LibMelonDS.FrameInfo
			{
				Time = GetRtcTime(!DeterministicEmulation),
				Keys = GetButtons(controller),
				TouchX = (byte)controller.AxisValue("TouchX"),
				TouchY = (byte)controller.AxisValue("TouchY"),
				GBALightSensor = (byte)controller.AxisValue("GBA Light Sensor"),
				SkipFw = _skipfw,
			};
		}

		protected override unsafe void FrameAdvancePost()
		{
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_core.SetFileOpenCallback(_coreFileOpenCallback);
			_core.SetFileCloseCallback(_coreFileCloseCallback);
		}

		public bool IsDSiMode() => _dsi;

		private readonly FileOpenCallback _coreFileOpenCallback;
		private readonly FileCloseCallback _coreFileCloseCallback;

		private void FileOpenCb(string file)
		{
			
		}

		public void FileCloseCb(string file)
		{
			
		}
	}
}
