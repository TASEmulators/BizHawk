using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	[PortedCore(CoreNames.MelonDS, "Arisotura", "0.9.3", "http://melonds.kuribo64.net/", singleInstance: true, isReleased: false)]
	public class NDS : WaterboxCore, ISaveRam,
		ISettable<NDS.Settings, NDS.SyncSettings>
	{
		private readonly LibMelonDS _core;
		private readonly List<byte[]> _roms;

		[CoreConstructor("NDS")]
		public unsafe NDS(CoreLoadParameters<Settings, SyncSettings> lp)
			: base(lp.Comm, new Configuration
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
			_roms = lp.Roms.Select(r => r.RomData).ToList();

			if (_roms.Count > 2)
			{
				throw new InvalidOperationException("Wrong number of ROMs!");
			}

			bool gbacartpresent = _roms.Count == 2;

			_core = PreInit<LibMelonDS>(new WaterboxOptions
			{
				Filename = "melonDS.wbx",
				SbrkHeapSizeKB = 2 * 1024,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 4,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 512 * 1024,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			_syncSettings = lp.SyncSettings ?? new SyncSettings();
			_settings = lp.Settings ?? new Settings();

			byte[] bios7 = _syncSettings.UseRealBIOS
				? lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7"))
				: null;

			byte[] bios9 = _syncSettings.UseRealBIOS
				? lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9"))
				: null;

			byte[] fw = lp.Comm.CoreFileProvider.GetFirmware(new("NDS", "firmware"));

			LibMelonDS.LoadFlags flags = LibMelonDS.LoadFlags.NONE;

			if (_syncSettings.UseRealBIOS)
				flags |= LibMelonDS.LoadFlags.USE_REAL_BIOS;
			if (_syncSettings.SkipFirmware || !_syncSettings.UseRealBIOS || fw == null)
				flags |= LibMelonDS.LoadFlags.SKIP_FIRMWARE;
			if (gbacartpresent)
				flags |= LibMelonDS.LoadFlags.GBA_CART_PRESENT;
			if (_settings.AccurateAudioBitrate)
				flags |= LibMelonDS.LoadFlags.ACCURATE_AUDIO_BITRATE;
			if (_syncSettings.FirmwareOverride || lp.DeterministicEmulationRequested)
				flags |= LibMelonDS.LoadFlags.FIRMWARE_OVERRIDE;

			var fwSettings = new LibMelonDS.FirmwareSettings();
			fwSettings.FirmwareUsername = new byte[64];
			fwSettings.FirmwareLanguage = 0;
			fwSettings.FirmwareBirthdayMonth = 0;
			fwSettings.FirmwareBirthdayDay = 0;
			fwSettings.FirmwareFavouriteColour = 0;
			fwSettings.FirmwareMessage = new byte[1024];

			_exe.AddReadonlyFile(_roms[0], "game.rom");
			if (gbacartpresent)
			{
				_exe.AddReadonlyFile(_roms[1], "gba.rom");
			}
			if (_syncSettings.UseRealBIOS)
			{
				_exe.AddReadonlyFile(bios7, "bios7.rom");
				_exe.AddReadonlyFile(bios9, "bios9.rom");
			}
			if (fw != null)
			{
				_exe.AddReadonlyFile(fw, "firmware.bin");
			}

			if (!_core.Init(flags, fwSettings))
			{
				throw new InvalidOperationException("Init returned false!");
			}

			_exe.RemoveReadonlyFile("game.rom");
			if (gbacartpresent)
			{
				_exe.RemoveReadonlyFile("gba.rom");
			}
			if (_syncSettings.UseRealBIOS)
			{
				_exe.RemoveReadonlyFile("bios7.rom");
				_exe.RemoveReadonlyFile("bios9.rom");
			}
			if (fw != null)
			{
				_exe.RemoveReadonlyFile("firmware.bin");
			}

			PostInit();

			DeterministicEmulation = lp.DeterministicEmulationRequested || (!_syncSettings.UseRealTime && _syncSettings.FirmwareOverride);
			InitializeRtc(_syncSettings.InitialTime);

			_resampler = new SpeexResampler(SpeexResampler.Quality.QUALITY_DEFAULT, 32768, 44100, 32768, 44100, null, this);
			_serviceProvider.Register<ISoundProvider>(_resampler);
		}

		private SpeexResampler _resampler;
		public override void Dispose()
		{
			base.Dispose();
			if (_resampler != null)
			{
				_resampler.Dispose();
				_resampler = null;
			}
		}

		public override ControllerDefinition ControllerDefinition => NDSController;

		public static readonly ControllerDefinition NDSController = new ControllerDefinition
		{
			Name = "NDS Controller",
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Y", "X", "L", "R", "Lid Open", "Lid Close", "Touch", "Power"
			}
		}.AddXYPair("Touch{0}", AxisPairOrientation.RightAndUp, 0.RangeTo(255), 128, 0.RangeTo(191), 96)
			.AddAxis("Mic Input", 0.RangeTo(2047), 0)
				.AddAxis("GBA Light Sensor", 0.RangeTo(10), 0);
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
			if (c.IsPressed("Touch"))
				b |= LibMelonDS.Buttons.TOUCH;

			return b;
		}

		public new bool SaveRamModified => _core.SaveRamIsDirty();

		public new byte[] CloneSaveRam()
		{
			int length = _core.GetSaveRamLength();

			if (length > 0)
			{
				byte[] ret = new byte[length];
				_core.GetSaveRam(ret);
				return ret;
			}

			return new byte[0];
		}

		public new void StoreSaveRam(byte[] data)
		{
			if (_core.PutSaveRam(data, (uint)data.Length))
			{
				throw new InvalidOperationException("SaveRAM size mismatch!");
			}
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

			[Description("If true, the audio bitrate will be set to 10. Otherwise, it will be set to 16.")]
			[DefaultValue(true)]
			public bool AccurateAudioBitrate { get; set; }

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

			[DisplayName("Use Real BIOS")]
			[Description("If true, real BIOS files will be used.")]
			[DefaultValue(false)]
			public bool UseRealBIOS { get; set; }

			[DisplayName("Skip Firmware")]
			[Description("If true, initial firmware boot will be skipped. Forced true if firmware cannot be booted (no real bios or missing firmware).")]
			[DefaultValue(false)]
			public bool SkipFirmware { get; set; }

			[DisplayName("Firmware Override")]
			[Description("If true, the firmware settings will be overriden by provided settings. Forced true when recording a movie.")]
			[DefaultValue(false)]
			public bool FirmwareOverride { get; set; }

			public string FirmwareUsername { get; set; }
			public int FirmwareLanguage { get; set; }
			public int FirmwareBirthdayMonth { get; set; }
			public int FirmwareBirthdayDay { get; set; }
			public int FirmwareFavouriteColour { get; set; }
			public string FirmwareMessage { get; set; }


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
			if (controller.IsPressed("Power"))
			{
				byte[] sav = CloneSaveRam();
				_core.Reset();
				StoreSaveRam(sav);
			}
			return new LibMelonDS.FrameInfo
			{
				Time = GetRtcTime(!DeterministicEmulation),
				Keys = GetButtons(controller),
				TouchX = (byte)controller.AxisValue("TouchX"),
				TouchY = (byte)controller.AxisValue("TouchY"),
				MicInput = (short)controller.AxisValue("Mic Input"),
				GBALightSensor = (byte)controller.AxisValue("GBA Light Sensor"),
			};
		}
	}
}
