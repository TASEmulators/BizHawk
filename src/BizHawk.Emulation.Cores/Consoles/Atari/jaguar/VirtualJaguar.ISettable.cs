using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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

		[CoreSettings]
		public class VirtualJaguarSettings
		{
			[DisplayName("Trace M68K (CPU)")]
			[Description("Used for the tracelogger")]
			[DefaultValue(true)]
			public bool TraceCPU { get; set; }

			[DisplayName("Trace TOM (GPU)")]
			[Description("Used for the tracelogger")]
			[DefaultValue(false)]
			public bool TraceGPU { get; set; }

			[DisplayName("Trace JERRY (DSP)")]
			[Description("Used for the tracelogger")]
			[DefaultValue(false)]
			public bool TraceDSP { get; set; }

			public VirtualJaguarSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public VirtualJaguarSettings Clone()
				=> (VirtualJaguarSettings)MemberwiseClone();
		}

		[CoreSettings]
		public class VirtualJaguarSyncSettings
		{
			[DisplayName("Player 1 Connected")]
			[Description("")]
			[DefaultValue(true)]
			public bool P1Active { get; set; }

			[DisplayName("Player 2 Connected")]
			[Description("")]
			[DefaultValue(false)]
			public bool P2Active { get; set; }

			[DisplayName("Skip BIOS")]
			[Description("Ignored (set to true) for Jaguar CD")]
			[DefaultValue(false)]
			public bool SkipBIOS { get; set; }

			public enum BiosRevisions
			{
				[Display(Name = "K Series")]
				KSeries = 0,
				[Display(Name = "M Series")]
				MSeries = 1,
			}

			[DisplayName("BIOS Revision")]
			[Description("")]
			[DefaultValue(BiosRevisions.KSeries)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public BiosRevisions BiosRevision { get; set; }

			[DisplayName("NTSC")]
			[Description("Set this to false to emulate a PAL console")]
			[DefaultValue(true)]
			public bool NTSC { get; set; }

			[DisplayName("Use Fast Blitter")]
			[Description("If true, a faster, less compatible blitter is used")]
			[DefaultValue(false)]
			public bool UseFastBlitter { get; set; }

			[DisplayName("Use Memory Track")]
			[Description("Allows for SaveRAM creation with Jaguar CD games. Does nothing for non-CD games.")]
			[DefaultValue(true)]
			public bool UseMemoryTrack { get; set; }

			public VirtualJaguarSyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public VirtualJaguarSyncSettings Clone()
				=> (VirtualJaguarSyncSettings)MemberwiseClone();

			public static bool NeedsReboot(VirtualJaguarSyncSettings x, VirtualJaguarSyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
