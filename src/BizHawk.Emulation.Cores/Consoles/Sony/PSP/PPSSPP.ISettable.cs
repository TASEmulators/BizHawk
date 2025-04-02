using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	public partial class PPSSPP : ISettable<PPSSPP.Settings, PPSSPP.SyncSettings>
	{
		private Settings _settings;
		private SyncSettings _syncSettings;
		public Settings GetSettings()
			=> _settings.Clone();

		public SyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(Settings o)
		{
			_settings = o;
			_core.PPSSPP_ReloadConfig(_context);
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class Settings
		{
			public Settings Clone()
				=> (Settings)MemberwiseClone();

			public Settings()
				=> SettingsUtil.SetDefaultValues(this);
		}

		[CoreSettings]
		public class SyncSettings
		{
			public SyncSettings Clone()
				=> (SyncSettings)MemberwiseClone();

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);

			public SyncSettings()
				=> SettingsUtil.SetDefaultValues(this);
		}
	}
}
