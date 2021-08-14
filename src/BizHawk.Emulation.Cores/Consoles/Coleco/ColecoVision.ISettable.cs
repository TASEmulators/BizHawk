using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision : IEmulator, ISettable<ColecoVision.ColecoSettings, ColecoVision.ColecoSyncSettings>
	{
		public ColecoSettings GetSettings()
		{
			return _settings.Clone();
		}

		public ColecoSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(ColecoSettings o)
		{
			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(ColecoSyncSettings o)
		{
			bool ret = o.SkipBiosIntro != _syncSettings.SkipBiosIntro;
			ret |= ColecoSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public class ColecoSettings
		{
			public ColecoSettings Clone()
			{
				return (ColecoSettings)MemberwiseClone();
			}
		}

		private ColecoSettings _settings = new ColecoSettings();
		private ColecoSyncSettings _syncSettings = new ColecoSyncSettings();

		public class ColecoSyncSettings
		{
			public bool SkipBiosIntro { get; set; }
			public bool UseSGM { get; set; }

			public PeripheralOption Port1 = ColecoVisionControllerDeck.DEFAULT_PERIPHERAL_OPTION;

			public PeripheralOption Port2 = ColecoVisionControllerDeck.DEFAULT_PERIPHERAL_OPTION;

			public ColecoSyncSettings Clone()
			{
				return (ColecoSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(ColecoSyncSettings x, ColecoSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
