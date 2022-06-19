using System;
using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64
{
	public partial class Ares64 : ISettable<Ares64.Ares64Settings, Ares64.Ares64SyncSettings>
	{
		private Ares64Settings _settings;
		private Ares64SyncSettings _syncSettings;

		public Ares64Settings GetSettings() => _settings.Clone();

		public Ares64SyncSettings GetSyncSettings() => _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(Ares64Settings o)
		{
			var ret = Ares64Settings.NeedsReboot(_settings, o);
			_settings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(Ares64SyncSettings o)
		{
			var ret = Ares64SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public class Ares64Settings
		{
			[DisplayName("Deinterlacer")]
			[Description("Weave looks good for still images, but creates artifacts for moving images.\n" +
				"Bob looks good for moving images, but makes the image bob up and down.")]
			[DefaultValue(LibAres64.DeinterlacerType.Weave)]
			public LibAres64.DeinterlacerType Deinterlacer { get; set; }

			public Ares64Settings() => SettingsUtil.SetDefaultValues(this);

			public Ares64Settings Clone() => MemberwiseClone() as Ares64Settings;

			public static bool NeedsReboot(Ares64Settings x, Ares64Settings y) => !DeepEquality.DeepEquals(x, y);
		}

		public class Ares64SyncSettings
		{
			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			[TypeConverter(typeof(BizDateTimeConverter))]
			public DateTime InitialTime { get; set; }

			[DisplayName("Use Real Time")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time. Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			[DisplayName("Player 1 Controller")]
			[Description("")]
			[DefaultValue(LibAres64.ControllerType.Mempak)]
			public LibAres64.ControllerType P1Controller { get; set; }

			[DisplayName("Player 2 Controller")]
			[Description("")]
			[DefaultValue(LibAres64.ControllerType.Unplugged)]
			public LibAres64.ControllerType P2Controller { get; set; }

			[DisplayName("Player 3 Controller")]
			[Description("")]
			[DefaultValue(LibAres64.ControllerType.Unplugged)]
			public LibAres64.ControllerType P3Controller { get; set; }

			[DisplayName("Player 4 Controller")]
			[Description("")]
			[DefaultValue(LibAres64.ControllerType.Unplugged)]
			public LibAres64.ControllerType P4Controller { get; set; }

			[DisplayName("Restrict Analog Range")]
			[Description("Restricts analog range to account for physical limitations.")]
			[DefaultValue(false)]
			public bool RestrictAnalogRange { get; set; }

			public Ares64SyncSettings() => SettingsUtil.SetDefaultValues(this);

			public Ares64SyncSettings Clone() => MemberwiseClone() as Ares64SyncSettings;

			public static bool NeedsReboot(Ares64SyncSettings x, Ares64SyncSettings y) => !DeepEquality.DeepEquals(x, y);
		}
	}
}
