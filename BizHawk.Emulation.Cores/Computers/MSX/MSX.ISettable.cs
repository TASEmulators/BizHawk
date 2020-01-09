using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX : ISettable<MSX.MSXSettings, MSX.MSXSyncSettings>
	{
		public MSXSettings GetSettings()
		{
			return Settings.Clone();
		}

		public MSXSyncSettings GetSyncSettings()
		{
			return SyncSettings.Clone();
		}

		public bool PutSettings(MSXSettings o)
		{
			bool ret = MSXSettings.RebootNeeded(Settings, o);
			Settings = o;
			return ret;
		}

		public bool PutSyncSettings(MSXSyncSettings o)
		{
			bool ret = MSXSyncSettings.RebootNeeded(SyncSettings, o);
			SyncSettings = o;
			return ret;
		}

		internal MSXSettings Settings { get; private set; }
		internal MSXSyncSettings SyncSettings { get; private set; }

		public class MSXSettings
		{
			// graphics settings
			public bool DispBG = true;
			public bool DispOBJ = true;

			public MSXSettings Clone()
			{
				return (MSXSettings)MemberwiseClone();
			}

			public static bool RebootNeeded(MSXSettings x, MSXSettings y)
			{
				return false;
			}
		}

		public class MSXSyncSettings
		{
			public MSXSyncSettings Clone()
			{
				return (MSXSyncSettings)MemberwiseClone();
			}

			public static bool RebootNeeded(MSXSyncSettings x, MSXSyncSettings y)
			{
				return false;
			}
		}
	}
}
