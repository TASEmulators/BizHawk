using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision : ISettable<object, ColecoVision.ColecoSyncSettings>
	{
		public object GetSettings()
		{
			return null;
		}

		public ColecoSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public bool PutSettings(object o)
		{
			return false;
		}

		public bool PutSyncSettings(ColecoSyncSettings o)
		{
			bool ret = o.SkipBiosIntro != _syncSettings.SkipBiosIntro;
			_syncSettings = o;
			return ret;
		}

		private ColecoSyncSettings _syncSettings;

		public class ColecoSyncSettings
		{
			public bool SkipBiosIntro { get; set; }

			public ColecoSyncSettings Clone()
			{
				return (ColecoSyncSettings)MemberwiseClone();
			}
		}
	}
}
