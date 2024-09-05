using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	public partial class ChannelF : ISettable<object, ChannelF.ChannelFSyncSettings>
	{
		private ChannelFSyncSettings _syncSettings;

		public object GetSettings()
			=> null;

		public ChannelFSyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(object o)
			=> PutSettingsDirtyBits.None;

		public PutSettingsDirtyBits PutSyncSettings(ChannelFSyncSettings o)
		{
			var ret = ChannelFSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class ChannelFSyncSettings
		{
			[DisplayName("Deterministic Emulation")]
			[Description("If true, the core agrees to behave in a completely deterministic manner")]
			[DefaultValue(true)]
			public bool DeterministicEmulation { get; set; }
			[DisplayName("Region")]
			[Description("NTSC or PAL - Affects the CPU clock speed and refresh rate")]
			[DefaultValue(RegionType.NTSC)]
			public RegionType Region { get; set; }
			[DisplayName("Version")]
			[Description("Channel F II has a very slightly different BIOS to Channel F and a slightly slower CPU in the PAL version compared to v1")]
			[DefaultValue(ConsoleVersion.ChannelF)]
			public ConsoleVersion Version { get; set; }

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

		public enum RegionType
		{
			NTSC,
			PAL
		}

		public enum ConsoleVersion
		{
			ChannelF,
			ChannelF_II
		}
	}
}
