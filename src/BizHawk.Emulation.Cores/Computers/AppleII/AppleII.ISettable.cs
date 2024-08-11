using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : ISettable<AppleII.Settings, AppleII.SyncSettings>
	{
		private Settings _settings;
		private SyncSettings _syncSettings;

		[CoreSettings]
		public class Settings
		{
			[DisplayName("Monochrome")]
			[DefaultValue(false)]
			[Description("Choose a monochrome monitor.")]
			public bool Monochrome { get; set; }

			public Settings()
				=> SettingsUtil.SetDefaultValues(this);

			public Settings Clone()
				=> (Settings)MemberwiseClone();
		}

		[CoreSettings]
		public class SyncSettings
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

			public SyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public SyncSettings Clone()
				=> (SyncSettings)MemberwiseClone();

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}

		public Settings GetSettings()
			=> _settings.Clone();

		public SyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(Settings o)
		{
			_settings = o;
			_machine.Video.IsMonochrome = _settings.Monochrome;

			SetCallbacks();

			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}
	}
}
