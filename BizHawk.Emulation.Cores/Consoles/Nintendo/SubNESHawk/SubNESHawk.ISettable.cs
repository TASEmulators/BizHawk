using System;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Emulation.Cores.Nintendo.SubNESHawk
{
	public partial class SubNESHawk : IEmulator, IStatable, ISettable<SubNESHawk.SubNESHawkSettings, SubNESHawk.SubNESHawkSyncSettings>
	{
		public SubNESHawkSettings GetSettings()
		{
			return subnesSettings.Clone();
		}

		public SubNESHawkSyncSettings GetSyncSettings()
		{
			return subnesSyncSettings.Clone();
		}

		public bool PutSettings(SubNESHawkSettings o)
		{
			subnesSettings = o;
			return false;
		}

		public bool PutSyncSettings(SubNESHawkSyncSettings o)
		{
			bool ret = SubNESHawkSyncSettings.NeedsReboot(subnesSyncSettings, o);
			subnesSyncSettings = o;
			return ret;
		}

		private SubNESHawkSettings subnesSettings = new SubNESHawkSettings();
		public SubNESHawkSyncSettings subnesSyncSettings = new SubNESHawkSyncSettings();

		public class SubNESHawkSettings
		{
			public SubNESHawkSettings Clone()
			{
				return (SubNESHawkSettings)MemberwiseClone();
			}
		}

		public class SubNESHawkSyncSettings
		{
			public SubNESHawkSyncSettings Clone()
			{
				return (SubNESHawkSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(SubNESHawkSyncSettings x, SubNESHawkSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
