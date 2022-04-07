using BizHawk.Common;
using BizHawk.Emulation.Common;

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class NDS : ISettable<NDS.NDSSettings, NDS.NDSSyncSettings>
	{
		private NDSSettings _settings;
		private NDSSyncSettings _syncSettings;

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
			Rotate270,
		}

		public class NDSSettings
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

			public enum AudioBitrateType : int
			{
				Auto,
				Ten,
				Sixteen,
			}

			[DisplayName("Audio Bitrate")]
			[Description("Auto will set the audio bitrate most accurate to the console (10 for DS, 16 for DSi).")]
			[DefaultValue(AudioBitrateType.Auto)]
			public AudioBitrateType AudioBitrate { get; set; }

			public NDSSettings Clone() => MemberwiseClone() as NDSSettings;

			public static bool NeedsScreenResize(NDSSettings x, NDSSettings y)
			{
				bool ret = false;
				ret |= x.ScreenLayout != y.ScreenLayout;
				ret |= x.ScreenGap != y.ScreenGap;
				ret |= x.ScreenRotation != y.ScreenRotation;
				return ret;
			}

			public NDSSettings() => SettingsUtil.SetDefaultValues(this);
		}

		private static readonly DateTime minDate = new(2000, 1, 1);
		private static readonly DateTime maxDate = new(2099, 12, 31, 23, 59, 59);

		public class NDSSyncSettings
		{
			[DisplayName("Threaded 3D Rendering")]
			[Description("Offloads 3D rendering to a separate thread")]
			[DefaultValue(true)]
			public bool ThreadedRendering { get; set; }

			[JsonIgnore]
			private DateTime _initaltime;

			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			[TypeConverter(typeof(BizDateTimeConverter))]
			public DateTime InitialTime
			{
				get => _initaltime;
				set => _initaltime = value < minDate ? minDate : (value > maxDate ? maxDate : value);
			}

			[DisplayName("Use Real Time")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time. Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			[DisplayName("DSi Mode")]
			[Description("If true, DSi mode will be used.")]
			[DefaultValue(false)]
			public bool UseDSi { get; set; }

			[DisplayName("Use Real BIOS")]
			[Description("If true, real BIOS files will be used. Forced true for DSi.")]
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
			[DefaultValue("melonDS")]
			public string FirmwareUsername
			{
				get => _firmwareusername;
				set => _firmwareusername = value.Length != 0 ? value.Substring(0, Math.Min(10, value.Length)) : _firmwareusername.Substring(0, 1);
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
			[DefaultValue(Month.November)]
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

			public NDSSyncSettings Clone() => MemberwiseClone() as NDSSyncSettings;

			public static bool NeedsReboot(NDSSyncSettings x, NDSSyncSettings y) => !DeepEquality.DeepEquals(x, y);

			public NDSSyncSettings() => SettingsUtil.SetDefaultValues(this);
		}

		public NDSSettings GetSettings() => _settings.Clone();

		public NDSSyncSettings GetSyncSettings() => _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(NDSSettings o)
		{
			var ret = NDSSettings.NeedsScreenResize(_settings, o);
			_settings = o;
			return ret ? PutSettingsDirtyBits.ScreenLayoutChanged : PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(NDSSyncSettings o)
		{
			var ret = NDSSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private static int SanitizeBirthdayDay(int day, NDSSyncSettings.Month fwMonth)
		{
			int maxdays;
			switch (fwMonth)
			{
				case NDSSyncSettings.Month.February:
					{
						maxdays = 29;
						break;
					}
				case NDSSyncSettings.Month.April:
				case NDSSyncSettings.Month.June:
				case NDSSyncSettings.Month.September:
				case NDSSyncSettings.Month.November:
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
