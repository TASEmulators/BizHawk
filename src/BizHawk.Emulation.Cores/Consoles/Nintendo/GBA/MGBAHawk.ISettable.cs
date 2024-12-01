using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public partial class MGBAHawk : ISettable<MGBAHawk.Settings, MGBAHawk.SyncSettings>
	{
		public Settings GetSettings()
			=> _settings.Clone();

		public PutSettingsDirtyBits PutSettings(Settings o)
		{
			LibmGBA.Layers mask = 0;
			if (o.DisplayBG0) mask |= LibmGBA.Layers.BG0;
			if (o.DisplayBG1) mask |= LibmGBA.Layers.BG1;
			if (o.DisplayBG2) mask |= LibmGBA.Layers.BG2;
			if (o.DisplayBG3) mask |= LibmGBA.Layers.BG3;
			if (o.DisplayOBJ) mask |= LibmGBA.Layers.OBJ;
			LibmGBA.BizSetLayerMask(Core, mask);

			LibmGBA.Sounds smask = 0;
			if (o.PlayCh0) smask |= LibmGBA.Sounds.CH0;
			if (o.PlayCh1) smask |= LibmGBA.Sounds.CH1;
			if (o.PlayCh2) smask |= LibmGBA.Sounds.CH2;
			if (o.PlayCh3) smask |= LibmGBA.Sounds.CH3;
			if (o.PlayChA) smask |= LibmGBA.Sounds.CHA;
			if (o.PlayChB) smask |= LibmGBA.Sounds.CHB;
			LibmGBA.BizSetSoundMask(Core, smask);

			var palette = new int[65536];
			var c = o.ColorType switch
			{
				Settings.ColorTypes.SameBoy => GBColors.ColorType.sameboy,
				Settings.ColorTypes.Gambatte => GBColors.ColorType.gambatte,
				Settings.ColorTypes.Vivid => GBColors.ColorType.vivid,
				Settings.ColorTypes.VbaVivid => GBColors.ColorType.vbavivid,
				Settings.ColorTypes.VbaGbNew => GBColors.ColorType.vbagbnew,
				Settings.ColorTypes.VbaGbOld => GBColors.ColorType.vbabgbold,
				Settings.ColorTypes.BizhawkGba => GBColors.ColorType.gba,
				_ => GBColors.ColorType.vivid,
			};
			GBColors.GetLut(c, palette, agb: true);
			for (var i = 32768; i < 65536; i++)
				palette[i] = palette[i - 32768];
			LibmGBA.BizSetPalette(Core, palette);

			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		private Settings _settings;

		[CoreSettings]
		public class Settings
		{
			[DisplayName("Display BG Layer 0")]
			[DefaultValue(true)]
			public bool DisplayBG0 { get; set; }

			[DisplayName("Display BG Layer 1")]
			[DefaultValue(true)]
			public bool DisplayBG1 { get; set; }

			[DisplayName("Display BG Layer 2")]
			[DefaultValue(true)]
			public bool DisplayBG2 { get; set; }

			[DisplayName("Display BG Layer 3")]
			[DefaultValue(true)]
			public bool DisplayBG3 { get; set; }

			[DisplayName("Display Sprite Layer")]
			[DefaultValue(true)]
			public bool DisplayOBJ { get; set; }

			[DisplayName("Play Square 1")]
			[DefaultValue(true)]
			public bool PlayCh0 { get; set; }

			[DisplayName("Play Square 2")]
			[DefaultValue(true)]
			public bool PlayCh1 { get; set; }

			[DisplayName("Play Wave")]
			[DefaultValue(true)]
			public bool PlayCh2 { get; set; }

			[DisplayName("Play Noise")]
			[DefaultValue(true)]
			public bool PlayCh3 { get; set; }

			[DisplayName("Play Direct Sound A")]
			[DefaultValue(true)]
			public bool PlayChA { get; set; }

			[DisplayName("Play Direct Sound B")]
			[DefaultValue(true)]
			public bool PlayChB { get; set; }

			public enum ColorTypes
			{
				[Display(Name = "SameBoy GBA")]
				SameBoy,
				[Display(Name = "Gambatte CGB")]
				Gambatte,
				[Display(Name = "Vivid")]
				Vivid,
				[Display(Name = "VBA Vivid")]
				VbaVivid,
				[Display(Name = "VBA GB")]
				VbaGbNew,
				[Display(Name = "VBA GB (Old)")]
				VbaGbOld,
				[Display(Name = "Bizhawk GBA")]
				BizhawkGba
			}

			[DisplayName("Color Type")]
			[DefaultValue(ColorTypes.Vivid)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public ColorTypes ColorType { get; set; }

			public Settings()
				=> SettingsUtil.SetDefaultValues(this);

			public Settings Clone()
				=> (Settings)MemberwiseClone();
		}

		public SyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			bool ret = SyncSettings.NeedsReboot(o, _syncSettings);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private SyncSettings _syncSettings;

		[CoreSettings]
		public class SyncSettings
		{
			[DisplayName("Skip BIOS")]
			[Description("Skips the BIOS intro.  Not applicable when a BIOS is not provided.")]
			[DefaultValue(true)]
			public bool SkipBios { get; set; }

			[DisplayName("RTC Use Real Time")]
			[Description("Causes the internal clock to reflect your system clock.  Only relevant when a game has an RTC chip.  Forced to false for movie recording.")]
			[DefaultValue(true)]
			public bool RTCUseRealTime { get; set; }

			[DisplayName("RTC Initial Time")]
			[Description("The initial time of emulation.  Only relevant when a game has an RTC chip and \"RTC Use Real Time\" is false.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			[TypeConverter(typeof(BizDateTimeConverter))]
			public DateTime RTCInitialTime { get; set; }

			[DisplayName("Save Type")]
			[Description("Save type used in games.")]
			[DefaultValue(LibmGBA.SaveType.Autodetect)]
			public LibmGBA.SaveType OverrideSaveType { get; set; }
			public enum HardwareSelection : int
			{
				Autodetect = 0,
				True = 1,
				False = 2,
			}

			[DisplayName("RTC")]
			[Description("")]
			[DefaultValue(HardwareSelection.Autodetect)]
			public HardwareSelection OverrideRtc { get; set; }

			[DisplayName("Rumble")]
			[Description("")]
			[DefaultValue(HardwareSelection.Autodetect)]
			public HardwareSelection OverrideRumble { get; set; }

			[DisplayName("Light Sensor")]
			[Description("")]
			[DefaultValue(HardwareSelection.Autodetect)]
			public HardwareSelection OverrideLightSensor { get; set; }

			[DisplayName("Gyro")]
			[Description("")]
			[DefaultValue(HardwareSelection.Autodetect)]
			public HardwareSelection OverrideGyro { get; set; }

			[DisplayName("Tilt")]
			[Description("")]
			[DefaultValue(HardwareSelection.Autodetect)]
			public HardwareSelection OverrideTilt { get; set; }

			[DisplayName("GB Player Detection")]
			[Description("")]
			[DefaultValue(false)]
			public bool OverrideGbPlayerDetect { get; set; }

			[DisplayName("VBA Bug Compatibility Mode")]
			[Description("Enables a compatibility mode for buggy Pokemon romhacks which rely on VBA bugs. Generally you don't need to enable yourself as Pokemon romhack detection will enable this itself.")]
			[DefaultValue(false)]
			public bool OverrideVbaBugCompat { get; set; }

			[DisplayName("Detect Pokemon Romhacks")]
			[Description("Detects Pokemon romhacks and enables compatibility options due to their generally buggy nature. Will override other override settings.")]
			[DefaultValue(true)]
			public bool OverridePokemonRomhackDetect { get; set; }

			public SyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public SyncSettings Clone()
				=> (SyncSettings)MemberwiseClone();

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
