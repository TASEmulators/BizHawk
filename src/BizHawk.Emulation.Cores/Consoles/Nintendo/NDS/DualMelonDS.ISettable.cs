using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class DualNDS : ISettable<DualNDS.DualNDSSettings, DualNDS.DualNDSSyncSettings>
	{
		public class DualNDSSettings
		{
			public NDS.NDSSettings L;
			public NDS.NDSSettings R;

			public DualNDSSettings Clone()
			{
				return new DualNDSSettings(L.Clone(), R.Clone());
			}

			public DualNDSSettings()
			{
				L = new NDS.NDSSettings();
				R = new NDS.NDSSettings();
			}

			public DualNDSSettings(NDS.NDSSettings L, NDS.NDSSettings R)
			{
				this.L = L;
				this.R = R;
			}
		}

		public class DualNDSSyncSettings
		{
			public NDS.NDSSyncSettings L;
			public NDS.NDSSyncSettings R;

			public DualNDSSyncSettings Clone()
			{
				return new DualNDSSyncSettings(L.Clone(), R.Clone());
			}

			public DualNDSSyncSettings()
			{
				L = new NDS.NDSSyncSettings();
				R = new NDS.NDSSyncSettings();
			}

			public DualNDSSyncSettings(NDS.NDSSyncSettings L, NDS.NDSSyncSettings R)
			{
				this.L = L;
				this.R = R;
			}
		}

		public DualNDSSettings GetSettings()
		{
			return new DualNDSSettings
			(
				L.GetSettings(),
				R.GetSettings()
			);
		}

		public DualNDSSyncSettings GetSyncSettings()
		{
			return new DualNDSSyncSettings
			(
				L.GetSyncSettings(),
				R.GetSyncSettings()
			);
		}

		public PutSettingsDirtyBits PutSettings(DualNDSSettings o)
		{
			return (PutSettingsDirtyBits)((int)L.PutSettings(o.L) | (int)R.PutSettings(o.R));
		}

		public PutSettingsDirtyBits PutSyncSettings(DualNDSSyncSettings o)
		{
			return (PutSettingsDirtyBits)((int)L.PutSyncSettings(o.L) | (int)R.PutSyncSettings(o.R));
		}
	}
}
