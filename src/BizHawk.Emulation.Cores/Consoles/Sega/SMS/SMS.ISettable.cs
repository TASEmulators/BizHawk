using System.ComponentModel;
using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS : ISettable<SMS.SmsSettings, SMS.SmsSyncSettings>
	{
		public SmsSettings GetSettings() => Settings.Clone();

		public SmsSyncSettings GetSyncSettings() => SyncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(SmsSettings o)
		{
			bool ret = SmsSettings.RebootNeeded(Settings, o);
			Settings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(SmsSyncSettings o)
		{
			bool ret = SmsSyncSettings.RebootNeeded(SyncSettings, o);
			SyncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		internal SmsSettings Settings { get; private set; }
		internal SmsSyncSettings SyncSettings { get; private set; }

		public class SmsSettings
		{
			// Game settings
			public bool ForceStereoSeparation { get; set; }
			public bool SpriteLimit { get; set; }

			[DisplayName("Fix 3D")]
			[Description("SMS only")]
			public bool Fix3D { get; set; } = true;

			[DisplayName("Display Overscan")]
			[Description("Not applicable to Game Gear")]
			public bool DisplayOverscan { get; set; }

			[DisplayName("Show Clipped Regions")]
			[Description("Game Gear only")]
			public bool ShowClippedRegions { get; set; }

			[DisplayName("Highlight Active Display Region")]
			[Description("Game Gear only")]
			public bool HighlightActiveDisplayRegion { get; set; }

			// graphics settings
			[DisplayName("Display Background")]
			public bool DispBG { get; set; } = true;

			[DisplayName("Display Objects")]
			public bool DispOBJ { get; set; } = true;

			public SmsSettings Clone() => (SmsSettings)MemberwiseClone();

			public static bool RebootNeeded(SmsSettings x, SmsSettings y) => false;
		}

		public class SmsSyncSettings
		{
			[DisplayName("Enable FM")]
			[Description("SMS only")]
			public bool EnableFm { get; set; } = true;

			[DisplayName("Allow Overclock")]
			[Description("SMS only")]
			public bool AllowOverClock { get; set; }

			[DisplayName("Use BIOS")]
			[Description("Must be Enabled for TAS")]
			public bool UseBios { get; set; } = true;

			[DisplayName("Region")]
			public Regions ConsoleRegion { get; set; } = Regions.Auto;

			[DisplayName("Display Type")]
			public DisplayTypes DisplayType { get; set; } = DisplayTypes.Auto;

			[DisplayName("Use keyboard")]
			[Description("Enables keyboard for non-GG systems")]
			public bool UseKeyboard { get; set; } = false;

			[DefaultValue(SMSControllerTypes.Standard)]
			[DisplayName("Port 1 Device")]
			[Description("The type of controller plugged into the first controller port")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SMSControllerTypes Port1 { get; set; } = SMSControllerTypes.Standard;

			[DefaultValue(SMSControllerTypes.Standard)]
			[DisplayName("Port 2 Device")]
			[Description("The type of controller plugged into the second controller port")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SMSControllerTypes Port2 { get; set; } = SMSControllerTypes.Standard;

			public SmsSyncSettings Clone() => (SmsSyncSettings)MemberwiseClone();

			public static bool RebootNeeded(SmsSyncSettings x, SmsSyncSettings y)
			{
				return
					x.EnableFm != y.EnableFm
					|| x.AllowOverClock != y.AllowOverClock
					|| x.UseBios != y.UseBios
					|| x.ConsoleRegion != y.ConsoleRegion
					|| x.DisplayType != y.DisplayType
					|| x.UseKeyboard != y.UseKeyboard
					|| x.Port1 != y.Port1
					|| x.Port2 != y.Port2;
			}

			public enum ControllerTypes
			{
				Standard,
				Paddle,
				LightPhaser,
				SportsPad,
				Keyboard
			}

			public enum Regions
			{
				Export,
				Japan,
				Korea,
				Auto
			}

			public enum DisplayTypes
			{
				Ntsc, Pal, Auto
			}
		}
	}
}
