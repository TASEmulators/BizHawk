using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.GGHawkLink
{
	public partial class GGHawkLink : IEmulator, IStatable, ISettable<GGHawkLink.GGLinkSettings, GGHawkLink.GGLinkSyncSettings>
	{
		public GGLinkSettings GetSettings()
			=> linkSettings.Clone();

		public GGLinkSyncSettings GetSyncSettings()
			=> linkSyncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(GGLinkSettings o)
		{
			linkSettings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(GGLinkSyncSettings o)
		{
			var ret = GGLinkSyncSettings.NeedsReboot(linkSyncSettings, o);
			linkSyncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private GGLinkSettings linkSettings = new();
		public GGLinkSyncSettings linkSyncSettings = new();

		[CoreSettings]
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

			public GGLinkSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public GGLinkSettings Clone()
				=> (GGLinkSettings)MemberwiseClone();
		}

		[CoreSettings]
		public class GGLinkSyncSettings
		{
			[DisplayName("Use Existing SaveRAM")]
			[Description("When true, existing SaveRAM will be loaded at boot up")]
			[DefaultValue(true)]
			public bool Use_SRAM { get; set; }

			public GGLinkSyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public GGLinkSyncSettings Clone()
				=> (GGLinkSyncSettings)MemberwiseClone();

			public static bool NeedsReboot(GGLinkSyncSettings x, GGLinkSyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
