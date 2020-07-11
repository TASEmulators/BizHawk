using System.ComponentModel;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public partial class O2Hawk : IEmulator, ISettable<O2Hawk.O2Settings, O2Hawk.O2SyncSettings>
	{
		public O2Settings GetSettings()
		{
			return _settings.Clone();
		}

		public O2SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(O2Settings o)
		{
			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(O2SyncSettings o)
		{
			bool ret = O2SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public O2Settings _settings = new O2Settings();
		public O2SyncSettings _syncSettings = new O2SyncSettings();

		public class O2Settings
		{
			[DisplayName("Display Characters")]
			[Description("When true, displays character.")]
			[DefaultValue(true)]
			public bool Show_Chars { get; set; }

			[DisplayName("Display Quad Characters")]
			[Description("When true, displays quad character.")]
			[DefaultValue(true)]
			public bool Show_Quads { get; set; }

			[DisplayName("Display Sprites")]
			[Description("When true, displays sprites.")]
			[DefaultValue(true)]
			public bool Show_Sprites { get; set; }

			public O2Settings Clone()
			{
				return (O2Settings)MemberwiseClone();
			}

			public O2Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public class O2SyncSettings
		{
			[DisplayName("Use G7400 Enhanemants")]
			[Description("When true, boots using G7400 BIOS and features")]
			[DefaultValue(true)]
			public bool G7400_Enable { get; set; }

			[DisplayName("Use Existing SaveRAM")]
			[Description("When true, existing SaveRAM will be loaded at boot up")]
			[DefaultValue(true)]
			public bool Use_SRAM { get; set; }

			public O2SyncSettings Clone()
			{
				return (O2SyncSettings)MemberwiseClone();
			}

			public O2SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(O2SyncSettings x, O2SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
