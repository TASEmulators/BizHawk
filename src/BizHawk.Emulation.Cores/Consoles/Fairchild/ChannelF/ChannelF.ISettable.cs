using System.ComponentModel;

using BizHawk.API.ApiHawk;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : ISettable<ChannelF.ChannelFSettings, ChannelF.ChannelFSyncSettings>
	{
		internal ChannelFSettings Settings = new ChannelFSettings();
		internal ChannelFSyncSettings SyncSettings = new ChannelFSyncSettings();

		public ChannelFSettings GetSettings()
		{
			return Settings.Clone();
		}

		public ChannelFSyncSettings GetSyncSettings()
		{
			return SyncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(ChannelFSettings o)
		{
			Settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(ChannelFSyncSettings o)
		{
			bool ret = ChannelFSyncSettings.NeedsReboot(SyncSettings, o);
			SyncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public class ChannelFSettings
		{
			[DisplayName("Default Background Color")]
			[Description("The default BG color")]
			[DefaultValue(0)]
			public int BackgroundColor { get; set; }

			public ChannelFSettings Clone()
			{
				return (ChannelFSettings)MemberwiseClone();
			}

			public ChannelFSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public class ChannelFSyncSettings
		{
			[DisplayName("Deterministic Emulation")]
			[Description("If true, the core agrees to behave in a completely deterministic manner")]
			[DefaultValue(true)]
			public bool DeterministicEmulation { get; set; }

			public ChannelFSyncSettings Clone()
			{
				return (ChannelFSyncSettings) MemberwiseClone();
			}

			public ChannelFSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(ChannelFSyncSettings x, ChannelFSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
