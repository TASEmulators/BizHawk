using BizHawk.Common;
using BizHawk.Emulation.Common;

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text;
using BizHawk.Common.CollectionExtensions;
using Newtonsoft.Json;

// ReSharper disable SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public partial class NDS : ISettable<NDS.NDSSettings, NDS.NDSSyncSettings>
	{
		private NDSSettings _settings;
		private NDSSyncSettings _syncSettings;

		private readonly NDSSyncSettings _activeSyncSettings;
		private readonly LibMelonDS.ConfigCallbackInterface _configCallbackInterface;

		private bool GetBooleanSettingCallback(LibMelonDS.ConfigEntry configEntry) => configEntry switch
		{
			LibMelonDS.ConfigEntry.ExternalBIOSEnable => _activeSyncSettings.UseRealBIOS,
			LibMelonDS.ConfigEntry.DLDI_Enable => false, // TODO
			LibMelonDS.ConfigEntry.DLDI_ReadOnly => false, // TODO
			LibMelonDS.ConfigEntry.DLDI_FolderSync => false, // TODO
			LibMelonDS.ConfigEntry.DSiSD_Enable => false, // TODO
			LibMelonDS.ConfigEntry.DSiSD_ReadOnly => false, // TODO
			LibMelonDS.ConfigEntry.DSiSD_FolderSync => false, // TODO
			LibMelonDS.ConfigEntry.Firm_OverrideSettings => _activeSyncSettings.FirmwareOverride,
			LibMelonDS.ConfigEntry.DSi_FullBIOSBoot => false, // TODO
			LibMelonDS.ConfigEntry.UseRealTime => false, // RTC callback overrides this anyways, really this is so gmtime_r is used over localtime_r
			LibMelonDS.ConfigEntry.FixedBootTime => true, // this just means use TimeAtBoot (which we always want at Unix epoch)
			_ => throw new InvalidOperationException()
		};

		private int GetIntegerSettingCallback(LibMelonDS.ConfigEntry configEntry) => configEntry switch
		{
			LibMelonDS.ConfigEntry.DLDI_ImageSize => 0, // TODO
			LibMelonDS.ConfigEntry.DSiSD_ImageSize => 0, // TODO
			LibMelonDS.ConfigEntry.Firm_Language => (int)_activeSyncSettings.FirmwareLanguage,
			LibMelonDS.ConfigEntry.Firm_BirthdayMonth => (int)_activeSyncSettings.FirmwareBirthdayMonth,
			LibMelonDS.ConfigEntry.Firm_BirthdayDay => _activeSyncSettings.FirmwareBirthdayDay,
			LibMelonDS.ConfigEntry.Firm_Color => (int)_activeSyncSettings.FirmwareFavouriteColour,
			LibMelonDS.ConfigEntry.AudioBitDepth => (int)_settings.AudioBitDepth,
			LibMelonDS.ConfigEntry.TimeAtBoot => 0,
			_ => throw new InvalidOperationException()
		};

		private void GetStringSettingCallback(LibMelonDS.ConfigEntry configEntry, IntPtr buffer, int bufferSize)
		{
			var ret = configEntry switch
			{
				LibMelonDS.ConfigEntry.BIOS9Path => _configEntryToPath.GetValueOrDefault(configEntry),
				LibMelonDS.ConfigEntry.BIOS7Path => _configEntryToPath.GetValueOrDefault(configEntry),
				LibMelonDS.ConfigEntry.FirmwarePath => _configEntryToPath.GetValueOrDefault(configEntry),
				LibMelonDS.ConfigEntry.DSi_BIOS9Path => _configEntryToPath.GetValueOrDefault(configEntry),
				LibMelonDS.ConfigEntry.DSi_BIOS7Path => _configEntryToPath.GetValueOrDefault(configEntry),
				LibMelonDS.ConfigEntry.DSi_FirmwarePath => _configEntryToPath.GetValueOrDefault(configEntry),
				LibMelonDS.ConfigEntry.DSi_NANDPath => _configEntryToPath.GetValueOrDefault(configEntry),
				LibMelonDS.ConfigEntry.DLDI_ImagePath => "dldi.bin",
				LibMelonDS.ConfigEntry.DLDI_FolderPath => "dldi",
				LibMelonDS.ConfigEntry.DSiSD_ImagePath => "sd.bin",
				LibMelonDS.ConfigEntry.DSiSD_FolderPath => "sd",
				LibMelonDS.ConfigEntry.Firm_Username => _activeSyncSettings.FirmwareUsername,
				LibMelonDS.ConfigEntry.Firm_Message => _activeSyncSettings.FirmwareMessage,
				LibMelonDS.ConfigEntry.WifiSettingsPath => "wfcsettings.bin",
				_ => throw new InvalidOperationException()
			};

			if (string.IsNullOrEmpty(ret))
			{
				Marshal.WriteByte(buffer, 0, 0);
				return;
			}

			var bytes = Encoding.UTF8.GetBytes(ret);
			var numToCopy = Math.Min(bytes.Length, bufferSize - 1);
			Marshal.Copy(bytes, 0, buffer, numToCopy);
			Marshal.WriteByte(buffer, numToCopy, 0);
		}

		private void GetArraySettingCallback(LibMelonDS.ConfigEntry configEntry, IntPtr buffer)
		{
			if (configEntry != LibMelonDS.ConfigEntry.Firm_MAC)
			{
				throw new InvalidOperationException();
			}

			// TODO make MAC configurable
			Marshal.WriteByte(buffer, 0, 0x00);
			Marshal.WriteByte(buffer, 1, 0x09);
			Marshal.WriteByte(buffer, 2, 0xBF);
			Marshal.WriteByte(buffer, 3, 0x0E);
			Marshal.WriteByte(buffer, 4, 0x49);
			Marshal.WriteByte(buffer, 5, 0x16);
		}

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

			public enum AudioBitDepthType : int
			{
				Auto,
				Ten,
				Sixteen,
			}

			[DisplayName("Audio Bit Depth")]
			[Description("Auto will set the audio bit depth most accurate to the console (10 for DS, 16 for DSi).")]
			[DefaultValue(AudioBitDepthType.Auto)]
			public AudioBitDepthType AudioBitDepth { get; set; }

			[DisplayName("Alt Lag")]
			[Description("If true, touch screen polling and ARM7 key polling will be considered for lag frames. Otherwise, only ARM9 key polling will be considered.")]
			[DefaultValue(false)]
			public bool ConsiderAltLag { get; set; }

			[DisplayName("Trace ARM7 Thumb")]
			[Description("")]
			[DefaultValue(false)]
			public bool TraceArm7Thumb { get; set; }

			[DisplayName("Trace ARM7 ARM")]
			[Description("")]
			[DefaultValue(false)]
			public bool TraceArm7Arm { get; set; }

			[DisplayName("Trace ARM9 Thumb")]
			[Description("")]
			[DefaultValue(true)]
			public bool TraceArm9Thumb { get; set; }

			[DisplayName("Trace ARM9 ARM")]
			[Description("")]
			[DefaultValue(true)]
			public bool TraceArm9Arm { get; set; }

			public LibMelonDS.TraceMask GetTraceMask()
			{
				var ret = LibMelonDS.TraceMask.NONE;
				if (TraceArm7Thumb)
					ret |= LibMelonDS.TraceMask.ARM7_THUMB;
				if (TraceArm7Arm)
					ret |= LibMelonDS.TraceMask.ARM7_ARM;
				if (TraceArm9Thumb)
					ret |= LibMelonDS.TraceMask.ARM9_THUMB;
				if (TraceArm9Arm)
					ret |= LibMelonDS.TraceMask.ARM9_ARM;
				return ret;
			}

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
			public enum ThreeDeeRendererType : int
			{
				Software,
				//OpenGL_Classic,
				//OpenGL_Compute,
			}

			[DisplayName("3D Renderer")]
			[Description("Renderer used for 3D. OpenGL Classic requires at least OpenGL 3.2, OpenGL Compute requires at least OpenGL 4.3. Forced to Software when recording a movie.")]
			[DefaultValue(ThreeDeeRendererType.Software)]
			public ThreeDeeRendererType ThreeDeeRenderer { get; set; }

			[DisplayName("Threaded 3D Rendering")]
			[Description("Offloads 3D rendering to a separate thread. Only used for the software 3D renderer.")]
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
			[Description("If true, DSi mode will be used. Forced true if a DSiWare rom is detected.")]
			[DefaultValue(false)]
			public bool UseDSi { get; set; }

			[DisplayName("Use Real BIOS")]
			[Description("If true, real BIOS files will be used. Forced true for DSi.")]
			[DefaultValue(false)]
			public bool UseRealBIOS { get; set; }

			[DisplayName("Skip Firmware")]
			[Description("If true, initial firmware boot will be skipped. Forced true if firmware cannot be booted (no real bios or missing firmware).")]
			[DefaultValue(true)]
			public bool SkipFirmware { get; set; }

			[DisplayName("Firmware Override")]
			[Description("If true, the firmware settings will be overriden by provided settings. Forced true when recording a movie.")]
			[DefaultValue(true)]
			public bool FirmwareOverride { get; set; }

			[DisplayName("Clear NAND")]
			[Description("If true, the DSi NAND will have all its titles cleared. Forced true when recording a movie.")]
			[DefaultValue(true)]
			public bool ClearNAND { get; set; }

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
