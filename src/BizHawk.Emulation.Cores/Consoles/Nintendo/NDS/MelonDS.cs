using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	[PortedCore(CoreNames.MelonDS, "Arisotura", "0.9.3", "http://melonds.kuribo64.net/", singleInstance: true, isReleased: false)]
	public class NDS : WaterboxCore, ISaveRam,
		ISettable<NDS.Settings, NDS.SyncSettings>
	{
		private readonly LibMelonDS _core;
		private readonly List<byte[]> _roms;
		private readonly byte[] _bios7;
		private readonly byte[] _bios9;
		private readonly byte[] _fw;
		private readonly bool _userealbios;
		private readonly bool _skipfw;

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

			_userealbios = _syncSettings.UseRealBIOS;

			_bios7 = _userealbios
				? lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7"))
				: null;

			_bios9 = _userealbios
				? lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9"))
				: null;

			_fw = lp.Comm.CoreFileProvider.GetFirmware(new("NDS", "firmware"));

			_skipfw = _syncSettings.SkipFirmware || !_syncSettings.UseRealBIOS || _fw == null;

			LibMelonDS.LoadFlags flags = LibMelonDS.LoadFlags.NONE;

			if (_userealbios)
				flags |= LibMelonDS.LoadFlags.USE_REAL_BIOS;
			if (_skipfw)
				flags |= LibMelonDS.LoadFlags.SKIP_FIRMWARE;
			if (gbacartpresent)
				flags |= LibMelonDS.LoadFlags.GBA_CART_PRESENT;
			if (_settings.AccurateAudioBitrate)
				flags |= LibMelonDS.LoadFlags.ACCURATE_AUDIO_BITRATE;
			if (_syncSettings.FirmwareOverride || lp.DeterministicEmulationRequested)
				flags |= LibMelonDS.LoadFlags.FIRMWARE_OVERRIDE;

			var fwSettings = new LibMelonDS.FirmwareSettings();
			var name = Encoding.UTF8.GetBytes(_syncSettings.FirmwareUsername);
			fwSettings.FirmwareUsernameLength = name.Length;
			fwSettings.FirmwareLanguage = _syncSettings.FirmwareLanguage;
			fwSettings.FirmwareBirthdayMonth = _syncSettings.FirmwareBirthdayMonth;
			fwSettings.FirmwareBirthdayDay = _syncSettings.FirmwareBirthdayDay;
			fwSettings.FirmwareFavouriteColour = _syncSettings.FirmwareFavouriteColour;
			var message = Encoding.UTF8.GetBytes(_syncSettings.FirmwareMessage);
			fwSettings.FirmwareMessageLength = message.Length;

			_exe.AddReadonlyFile(_roms[0], "game.rom");
			if (gbacartpresent)
			{
				_exe.AddReadonlyFile(_roms[1], "gba.rom");
			}
			if (_syncSettings.UseRealBIOS)
			{
				_exe.AddReadonlyFile(_bios7, "bios7.rom");
				_exe.AddReadonlyFile(_bios9, "bios9.rom");
			}
			if (_fw != null)
			{
				_exe.AddReadonlyFile(_fw, "firmware.bin");
			}

			fixed (byte* namePtr = &name[0])
			fixed (byte* messagePtr = &message[0])
			{
				fwSettings.FirmwareUsername = (IntPtr)namePtr;
				fwSettings.FirmwareMessage = (IntPtr)messagePtr;
				if (!_core.Init(flags, fwSettings))
				{
					throw new InvalidOperationException("Init returned false!");
				}
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
			if (_fw != null)
			{
				_exe.RemoveReadonlyFile("firmware.bin");
			}

			PostInit();

			DeterministicEmulation = lp.DeterministicEmulationRequested || (!_syncSettings.UseRealTime);
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
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Y", "X", "L", "R", "LidOpen", "LidClose", "Touch", "Power"
			}
		}.AddXYPair("Touch {0}", AxisPairOrientation.RightAndUp, 0.RangeTo(255), 128, 0.RangeTo(191), 96)
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

			[DisplayName("Invert Screens")]
			[Description("Inverts the order of the screens.")]
			[DefaultValue(false)]
			public bool ScreenInvert { get; set; }

			[DisplayName("Rotation")]
			[Description("Adjusts the orientation of the screens")]
			[DefaultValue(ScreenRotationKind.Rotate0)]
			public ScreenRotationKind ScreenRotation { get; set; }

			[DisplayName("Screen Gap")]
			[DefaultValue(0)]
			public int ScreenGap { get; set; }

			[DisplayName("Accurate Audio Bitrate")]
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
			private static readonly DateTime minDate = new DateTime(2000, 1, 1);
			private static readonly DateTime maxDate = new DateTime(2099, 12, 31, 23, 59, 59);

			[JsonIgnore]
			private DateTime _initaltime;

			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.")]
			[DefaultValue(typeof(DateTime), "2000-01-01")]
			public DateTime InitialTime
			{
				get => _initaltime;
				set
				{
					if (DateTime.Compare(minDate, value) > 0)
					{
						_initaltime = minDate;
					}
					else if (DateTime.Compare(maxDate, value) < 0)
					{
						_initaltime = maxDate;
					}
					else
					{
						_initaltime = value;
					}

				}
			}

			[DisplayName("Use Real Time")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time. Ignored (set to false) when recording a movie.")]
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

			[JsonIgnore]
			private string _firmwareusername;

			[DisplayName("Firmware Username")]
			[Description("Username in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue("MelonDS")]
			public string FirmwareUsername
			{
				get => _firmwareusername;
				set => _firmwareusername = value.Substring(0, Math.Min(value.Length, 10));
			}

			public enum Language : int
			{
				Japanese,
				English,
				French,
				German,
				Italian,
				Spanish,
			}

			[DisplayName("Firmware Language")]
			[Description("Language in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue(Language.English)]
			public Language FirmwareLanguage { get; set; }

			public enum Month : int
			{
				January = 1,
				February,
				March,
				April,
				May,
				June,
				July,
				August,
				September,
				October,
				November,
				December,
			}

			[JsonIgnore]
			private Month _firmwarebirthdaymonth;

			[JsonIgnore]
			private int _firmwarebirthdayday;

			[DisplayName("Firmware Birthday Month")]
			[Description("Birthday month in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue(Month.March)]
			public Month FirmwareBirthdayMonth
			{
				get => _firmwarebirthdaymonth;
				set
				{
					FirmwareBirthdayDay = SanitizeBirthdayDay(FirmwareBirthdayDay, value);
					_firmwarebirthdaymonth = value;
				}
			}

			[DisplayName("Firmware Birthday Day")]
			[Description("Birthday day in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue(3)]
			public int FirmwareBirthdayDay
			{
				get => _firmwarebirthdayday;
				set => _firmwarebirthdayday = SanitizeBirthdayDay(value, FirmwareBirthdayMonth);
			}

			public enum Color : int
			{
				GreyishBlue,
				Brown,
				Red,
				LightPink,
				Orange,
				Yellow,
				Lime,
				LightGreen,
				DarkGreen,
				Turqoise,
				LightBlue,
				Blue,
				DarkBlue,
				DarkPurple,
				LightPurple,
				DarkPink,
			}

			[DisplayName("Firmware Favorite Color")]
			[Description("Favorite color in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue(Color.Red)]
			public Color FirmwareFavouriteColour { get; set; }

			[JsonIgnore]
			private string _firmwaremessage;

			[DisplayName("Firmware Message")]
			[Description("Message in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue("Melons Taste Great!")]
			public string FirmwareMessage
			{
				get => _firmwaremessage;
				set => _firmwaremessage = value.Substring(0, Math.Min(value.Length, 26));
			}

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

		private static int SanitizeBirthdayDay(int day, SyncSettings.Month fwMonth)
		{
			int maxdays;
			switch (fwMonth)
			{
				case SyncSettings.Month.February:
				{
					maxdays = 29;
					break;
				}
				case SyncSettings.Month.April:
				case SyncSettings.Month.June:
				case SyncSettings.Month.September:
				case SyncSettings.Month.November:
				{
					maxdays = 30;
					break;
				}
				default:
				{
					maxdays = 31;
					break;
				}
			}

			return Math.Max(1, Math.Min(day, maxdays));
		}

		private void Reset()
		{
			// hack around core clearing out rom/save/bios/firmware data on reset
			byte[] sav = CloneSaveRam();
			_exe.AddReadonlyFile(_roms[0], "game.rom");
			if (_userealbios)
			{
				_exe.AddReadonlyFile(_bios7, "bios7.rom");
				_exe.AddReadonlyFile(_bios9, "bios9.rom");
			}
			_core.Reset();
			_exe.RemoveReadonlyFile("game.rom");
			if (_userealbios)
			{
				_exe.RemoveReadonlyFile("bios7.rom");
				_exe.RemoveReadonlyFile("bios9.rom");
			}
			StoreSaveRam(sav);
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			if (_fw != null)
			{
				_exe.AddTransientFile(_fw, "firmware.bin");
			}
			if (controller.IsPressed("Power"))
			{
				Reset();
			}
			return new LibMelonDS.FrameInfo
			{
				Time = GetRtcTime(!DeterministicEmulation),
				Keys = GetButtons(controller),
				TouchX = (byte)controller.AxisValue("Touch X"),
				TouchY = (byte)controller.AxisValue("Touch Y"),
				MicInput = (short)controller.AxisValue("Mic Input"),
				GBALightSensor = (byte)controller.AxisValue("GBA Light Sensor"),
			};
		}

		protected override void FrameAdvancePost()
		{
			if (_fw != null)
			{
				byte[] fw = _exe.RemoveTransientFile("firmware.bin");
				Array.Copy(fw, _fw, fw.Length);
			}
		}

		protected override void SaveStateBinaryInternal(BinaryWriter writer)
		{
			if (_fw != null)
			{
				writer.Write(_fw.Length);
				writer.Write(_fw, 0, _fw.Length);
			}
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			if (_fw != null)
			{
				int len = reader.ReadInt32();
				if (len != _fw.Length)
				{
					throw new InvalidOperationException("Firmware buffer size mismatch!");
				}
				reader.Read(_fw, 0, len);
			}
		}
	}
}
