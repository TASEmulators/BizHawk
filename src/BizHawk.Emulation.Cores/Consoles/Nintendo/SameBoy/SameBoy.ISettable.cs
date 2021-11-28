using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy : ISettable<object, Sameboy.SameboySyncSettings>
	{
		private SameboySyncSettings _syncSettings;

		public object GetSettings() => null;

		public PutSettingsDirtyBits PutSettings(object o) => PutSettingsDirtyBits.None;

		public SameboySyncSettings GetSyncSettings() => _syncSettings.Clone();

		public PutSettingsDirtyBits PutSyncSettings(SameboySyncSettings o)
		{
			bool ret = SameboySyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public class SameboySyncSettings
		{
			[DisplayName("Use official BIOS")]
			[Description("When false, SameBoy's internal bios is used. The official bios should be used for TASing.")]
			[DefaultValue(false)]
			public bool EnableBIOS { get; set; }

			public enum ConsoleModeType
			{
				Auto,
				GB,
				GBC,
				GBA
			}

			[DisplayName("Console Mode")]
			[Description("Pick which console to run, 'Auto' chooses from ROM header; 'GB', 'GBC', and 'GBA' chooses the respective system.")]
			[DefaultValue(ConsoleModeType.Auto)]
			public ConsoleModeType ConsoleMode { get; set; }

			[DisplayName("Use Real Time")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time. Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			public SameboySyncSettings() => SettingsUtil.SetDefaultValues(this);

			public SameboySyncSettings Clone() => MemberwiseClone() as SameboySyncSettings;

			public static bool NeedsReboot(SameboySyncSettings x, SameboySyncSettings y) => !DeepEquality.DeepEquals(x, y);
		}
	}
}
