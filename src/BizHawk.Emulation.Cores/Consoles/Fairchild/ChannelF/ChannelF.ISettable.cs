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


		public enum ViewPort
		{
			/// <summary>
			/// View the entire screen minus flyback areas
			/// </summary>
			AllVisible,
			/// <summary>
			/// Trimmed viewport for a more centred display
			/// </summary>
			Trimmed
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

		[CoreSettings]
		public class ChannelFSyncSettings
		{
			[DisplayName("Region")]
			[Description("NTSC or PAL - Affects the CPU clock speed and refresh rate")]
			[DefaultValue(RegionType.NTSC)]
			public RegionType Region { get; set; }

			[DisplayName("Version")]
			[Description("Channel F II has a very slightly different BIOS to Channel F and a slightly slower CPU in the PAL version compared to v1")]
			[DefaultValue(ConsoleVersion.ChannelF)]
			public ConsoleVersion Version { get; set; }

			[DisplayName("Viewport")]
			[Description("Visable screen area (cropping options)")]
			[DefaultValue(ViewPort.AllVisible)]
			public ViewPort Viewport { get; set; }

			public ChannelFSyncSettings Clone()
				=> (ChannelFSyncSettings)MemberwiseClone();

			public ChannelFSyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public static bool NeedsReboot(ChannelFSyncSettings x, ChannelFSyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
