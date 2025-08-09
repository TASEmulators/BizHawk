using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	public partial class PPSSPP : ISettable<PPSSPP.Settings, PPSSPP.SyncSettings>
	{
		public PutSettingsDirtyBits PutSettings(object o) => PutSettingsDirtyBits.None;

		/// <summary>
		/// Settings
		/// </summary>
		private Settings _settings;
		public Settings GetSettings() => _settings.Clone();

		public PutSettingsDirtyBits PutSettings(Settings o)
		{
			var ret = Settings.NeedsReboot(_settings, o);
			_settings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class Settings
		{
			public Settings()
				=> SettingsUtil.SetDefaultValues(this);

			public Settings Clone()
				=> (Settings) MemberwiseClone();

			public static bool NeedsReboot(Settings x, Settings y)
				=> !DeepEquality.DeepEquals(x, y);
		}

		/// <summary>
		/// Sync Settings
		/// </summary>
		private SyncSettings _syncSettings;

		public SyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class SyncSettings
		{
			public SyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public SyncSettings Clone()
				=> (SyncSettings) MemberwiseClone();

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
