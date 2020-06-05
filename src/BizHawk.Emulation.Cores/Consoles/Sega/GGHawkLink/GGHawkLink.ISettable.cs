using System.ComponentModel;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.GGHawkLink
{
	public partial class GGHawkLink : IEmulator, IStatable, ISettable<GGHawkLink.GGLinkSettings, GGHawkLink.GGLinkSyncSettings>
	{
		public GGLinkSettings GetSettings()
		{
			return linkSettings.Clone();
		}

		public GGLinkSyncSettings GetSyncSettings()
		{
			return linkSyncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(GGLinkSettings o)
		{
			linkSettings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(GGLinkSyncSettings o)
		{
			bool ret = GGLinkSyncSettings.NeedsReboot(linkSyncSettings, o);
			linkSyncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private GGLinkSettings linkSettings = new GGLinkSettings();
		public GGLinkSyncSettings linkSyncSettings = new GGLinkSyncSettings();

		public class GGLinkSettings
		{
			public enum AudioSrc
			{
				Left,
				Right,
				Both
			}

			[DisplayName("Audio Selection")]
			[Description("Choose Audio Source. Both will produce Stereo sound.")]
			[DefaultValue(AudioSrc.Left)]
			public AudioSrc AudioSet { get; set; }

			public GGLinkSettings Clone()
			{
				return (GGLinkSettings)MemberwiseClone();
			}
		}

		public class GGLinkSyncSettings
		{

			[DisplayName("Use Existing SaveRAM")]
			[Description("When true, existing SaveRAM will be loaded at boot up")]
			[DefaultValue(true)]
			public bool Use_SRAM { get; set; }

			public GGLinkSyncSettings Clone()
			{
				return (GGLinkSyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(GGLinkSyncSettings x, GGLinkSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
