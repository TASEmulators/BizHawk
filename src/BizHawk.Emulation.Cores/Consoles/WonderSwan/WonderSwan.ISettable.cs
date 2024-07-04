using System.ComponentModel;
using System.Drawing;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.WonderSwan
{
	partial class WonderSwan : ISettable<WonderSwan.Settings, WonderSwan.SyncSettings>
	{
		private Settings _settings;
		private SyncSettings _syncSettings;

		[CoreSettings]
		public class Settings
		{
			[DisplayName("Background Layer")]
			[Description("True to display the selected layer.")]
			[DefaultValue(true)]
			public bool EnableBG { get; set; }

			[DisplayName("Foreground Layer")]
			[Description("True to display the selected layer.")]
			[DefaultValue(true)]
			public bool EnableFG { get; set; }

			[DisplayName("Sprites Layer")]
			[Description("True to display the selected layer.")]
			[DefaultValue(true)]
			public bool EnableSprites { get; set; }

			[DisplayName("B&W Palette")]
			[Description("Colors to display in Wonderswan (not Color) mode")]
			public Color[] BWPalette { get; private set; }

			public BizSwan.Settings GetNativeSettings()
			{
				var ret = new BizSwan.Settings();
				if (EnableBG) ret.LayerMask |= BizSwan.LayerFlags.BG;
				if (EnableFG) ret.LayerMask |= BizSwan.LayerFlags.FG;
				if (EnableSprites) ret.LayerMask |= BizSwan.LayerFlags.Sprite;

				ret.BWPalette = new uint[16];
				for (int i = 0; i < 16; i++)
				{
					ret.BWPalette[i] = (uint)BWPalette[i].ToArgb() | 0xff000000;
				}

				// default color algorithm from WonderSwan
				// todo: we could give options like the gameboy cores have
				ret.ColorPalette = new uint[4096];
				for (int r = 0; r < 16; r++)
				{
					for (int g = 0; g < 16; g++)
					{
						for (int b = 0; b < 16; b++)
						{
							var neoR = (uint)r * 17;
							var neoG = (uint)g * 17;
							var neoB = (uint)b * 17;
							ret.ColorPalette[r << 8 | g << 4 | b] = 0xff000000 | neoR << 16 | neoG << 8 | neoB << 0;
						}
					}
				}

				return ret;
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
				BWPalette = new Color[16];
				for (int i = 0; i < 16; i++)
				{
					BWPalette[i] = Color.FromArgb(255, i * 17, i * 17, i * 17);
				}
			}

			public Settings Clone()
			{
				var ret = (Settings)MemberwiseClone();
				ret.BWPalette = (Color[])BWPalette.Clone();
				return ret;
			}
		}

		[CoreSettings]
		public class SyncSettings
		{
			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.  Only relevant when UseRealTime is false.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			[TypeConverter(typeof(BizDateTimeConverter))]
			public DateTime InitialTime { get; set; }

			[Description("Your birthdate.  Stored in EEPROM and used by some games.")]
			[DefaultValue(typeof(DateTime), "1968-05-13")]
			[TypeConverter(typeof(BizDateTimeConverter))]
			public DateTime BirthDate { get; set; }

			[Description("True to emulate a color system.")]
			[DefaultValue(true)]
			public bool Color { get; set; }

			[DisplayName("Use RealTime")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time.  Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			[Description("Your gender.  Stored in EEPROM and used by some games.")]
			[DefaultValue(BizSwan.Gender.Female)]
			public BizSwan.Gender Gender { get; set; }

			[Description("Language to play games in.  Most games ignore this.")]
			[DefaultValue(BizSwan.Language.Japanese)]
			public BizSwan.Language Language { get; set; }

			[DisplayName("Blood Type")]
			[Description("Your blood type.  Stored in EEPROM and used by some games.")]
			[DefaultValue(BizSwan.Bloodtype.AB)]
			public BizSwan.Bloodtype BloodType { get; set; }

			[Description("Your name.  Stored in EEPROM and used by some games.  Maximum of 16 characters")]
			[DefaultValue("Lady Ashelia")]
			public string Name { get; set; }

			public BizSwan.SyncSettings GetNativeSettings()
			{
				var ret = new BizSwan.SyncSettings
				{
					color = Color,
					userealtime = UseRealTime,
					sex = Gender,
					language = Language,
					blood = BloodType
				};
				ret.SetName(Name);
				ret.bday = (uint)BirthDate.Day;
				ret.bmonth = (uint)BirthDate.Month;
				ret.byear = (uint)BirthDate.Year;
				ret.initialtime = (ulong)((InitialTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
				return ret;
			}

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}

		public Settings GetSettings() => _settings.Clone();

		public SyncSettings GetSyncSettings() => _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(Settings o)
		{
			_settings = o;
			var native = _settings.GetNativeSettings();
			BizSwan.bizswan_putsettings(Core, ref native);
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			bool ret = SyncSettings.NeedsReboot(o, _syncSettings);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

	}
}
