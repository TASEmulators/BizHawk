using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Jaguar
{
	public partial class VirtualJaguar : ISettable<VirtualJaguar.VirtualJaguarSettings, VirtualJaguar.VirtualJaguarSyncSettings>
	{
		private VirtualJaguarSettings _settings;
		private VirtualJaguarSyncSettings _syncSettings;

		public VirtualJaguarSettings GetSettings()
			=> _settings.Clone();

		public PutSettingsDirtyBits PutSettings(VirtualJaguarSettings o)
		{
			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		public VirtualJaguarSyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSyncSettings(VirtualJaguarSyncSettings o)
		{
			var ret = VirtualJaguarSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public class VirtualJaguarSettings
		{
			[DisplayName("Trace M68K (CPU)")]
			[Description("")]
			[DefaultValue(true)]
			public bool TraceCPU { get; set; }

			[DisplayName("Trace TOM (GPU)")]
			[Description("")]
			[DefaultValue(false)]
			public bool TraceGPU { get; set; }

			[DisplayName("Trace JERRY (DSP)")]
			[Description("")]
			[DefaultValue(false)]
			public bool TraceDSP { get; set; }

			public VirtualJaguarSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public VirtualJaguarSettings Clone()
				=> (VirtualJaguarSettings)MemberwiseClone();
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
			[Description("BIOS file must still be present. Ignored (set to true) for Jaguar CD")]
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
