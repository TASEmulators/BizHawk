using BizHawk.Common;
using BizHawk.Emulation.Common;

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class NDS : ISettable<NDS.Settings, NDS.SyncSettings>
	{
		private Settings _settings;
		private SyncSettings _syncSettings;

		public enum ScreenLayoutKind
		{
			Vertical,
			Horizontal,
			Top,
			Bottom,
		}

		public enum ScreenRotationKind
		{
			Rotate0,
			Rotate90,
			Rotate180,
			Rotate270
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

			[JsonIgnore]
			private int _screengap;

			[DisplayName("Screen Gap")]
			[Description("Gap between the screens")]
			[DefaultValue(0)]
			public int ScreenGap
			{
				get => _screengap;
				set => _screengap = Math.Max(0, Math.Min(128, value));
			}

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

		private static readonly DateTime minDate = new DateTime(2000, 1, 1);
		private static readonly DateTime maxDate = new DateTime(2099, 12, 31, 23, 59, 59);

		public class SyncSettings
		{
			[JsonIgnore]
			private DateTime _initaltime;

			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.")]
			[DefaultValue(typeof(DateTime), "2000-01-01")]
			[TypeConverter(typeof(BizDateTimeConverter))]
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

			public enum StartUp : int
			{
				[Display(Name = "Auto Boot")]
				AutoBoot,
				[Display(Name = "Manual Boot")]
				ManualBoot,
			}

			[DisplayName("Firmware Start-Up")]
			[Description("The way firmware is booted. Auto Boot will go to the game immediately, while Manual Boot will go into the firmware menu. Only applicable if firmware override is in effect.")]
			[DefaultValue(StartUp.AutoBoot)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public StartUp FirmwareStartUp { get; set; }

			[JsonIgnore]
			private string _firmwareusername;

			[DisplayName("Firmware Username")]
			[Description("Username in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue("MelonDS")]
			public string FirmwareUsername
			{
				get => _firmwareusername;
				set => _firmwareusername = value.Substring(0, Math.Min(10, value.Length));
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
			[DefaultValue(Month.May)]
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
			[DefaultValue(15)]
			public int FirmwareBirthdayDay
			{
				get => _firmwarebirthdayday;
				set => _firmwarebirthdayday = SanitizeBirthdayDay(value, FirmwareBirthdayMonth);
			}

			public enum Color : int
			{
				[Display(Name = "Greyish Blue")]
				GreyishBlue,
				Brown,
				Red,
				[Display(Name = "Light Pink")]
				LightPink,
				Orange,
				Yellow,
				Lime,
				[Display(Name = "Light Green")]
				LightGreen,
				[Display(Name = "Dark Green")]
				DarkGreen,
				Turqoise,
				[Display(Name = "Light Blue")]
				LightBlue,
				Blue,
				[Display(Name = "Dark Blue")]
				DarkBlue,
				[Display(Name = "Dark Purple")]
				DarkPurple,
				[Display(Name = "Light Purple")]
				LightPurple,
				[Display(Name = "Dark Pink")]
				DarkPink,
			}

			[DisplayName("Firmware Favorite Color")]
			[Description("Favorite color in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue(Color.Red)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public Color FirmwareFavouriteColour { get; set; }

			[JsonIgnore]
			private string _firmwaremessage;

			[DisplayName("Firmware Message")]
			[Description("Message in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue("Melons Taste Great!")]
			public string FirmwareMessage
			{
				get => _firmwaremessage;
				set => _firmwaremessage = value.Substring(0, Math.Min(26, value.Length));
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
	}
}
