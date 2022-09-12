using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	public partial class VirtualJaguar : ISettable<object, VirtualJaguar.VirtualJaguarSyncSettings>
	{
		private VirtualJaguarSyncSettings _syncSettings;

		public object GetSettings()
			=> null;

		public PutSettingsDirtyBits PutSettings(object o)
			=> PutSettingsDirtyBits.None;

		public VirtualJaguarSyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSyncSettings(VirtualJaguarSyncSettings o)
		{
			var ret = VirtualJaguarSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public class VirtualJaguarSyncSettings
		{
			[DisplayName("Player 1 Controller Connected")]
			[Description("")]
			[DefaultValue(true)]
			public bool P1Active { get; set; }

			[DisplayName("Player 2 Controller Connected")]
			[Description("")]
			[DefaultValue(false)]
			public bool P2Active { get; set; }

			[DisplayName("NTSC")]
			[Description("")]
			[DefaultValue(true)]
			public bool NTSC { get; set; }

			[DisplayName("Skip BIOS")]
			[Description("BIOS file must still be present")]
			[DefaultValue(true)]
			public bool SkipBIOS { get; set; }

			[DisplayName("Use Fast Blitter")]
			[Description("")]
			[DefaultValue(true)]
			public bool UseFastBlitter { get; set; }

			public VirtualJaguarSyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public VirtualJaguarSyncSettings Clone()
				=> (VirtualJaguarSyncSettings)MemberwiseClone();

			public static bool NeedsReboot(VirtualJaguarSyncSettings x, VirtualJaguarSyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
