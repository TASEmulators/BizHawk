using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public partial class Intellivision : IEmulator, ISettable<Intellivision.IntvSettings, Intellivision.IntvSyncSettings>
	{
		public IntvSettings GetSettings()
		{
			return _settings.Clone();
		}

		public IntvSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(IntvSettings o)
		{
			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(IntvSyncSettings o)
		{
			bool ret = IntvSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private IntvSettings _settings = new IntvSettings();
		private IntvSyncSettings _syncSettings = new IntvSyncSettings();

		public class IntvSettings
		{
			public IntvSettings Clone()
			{
				return (IntvSettings)MemberwiseClone();
			}
		}

		public class IntvSyncSettings
		{
			public PeripheralOption Port1 = IntellivisionControllerDeck.DEFAULT_PERIPHERAL_OPTION;

			public PeripheralOption Port2 = IntellivisionControllerDeck.DEFAULT_PERIPHERAL_OPTION;

			public IntvSyncSettings Clone()
			{
				return (IntvSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(IntvSyncSettings x, IntvSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
