using System.ComponentModel;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.G7400Hawk
{
	public partial class G7400Hawk : IEmulator, ISettable<G7400Hawk.G7400Settings, G7400Hawk.G7400SyncSettings>
	{
		public G7400Settings GetSettings()
		{
			return _settings.Clone();
		}

		public G7400SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(G7400Settings o)
		{
			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(G7400SyncSettings o)
		{
			bool ret = G7400SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public G7400Settings _settings = new G7400Settings();
		public G7400SyncSettings _syncSettings = new G7400SyncSettings();

		public class G7400Settings
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

			public G7400Settings Clone()
			{
				return (G7400Settings)MemberwiseClone();
			}

			public G7400Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public class G7400SyncSettings
		{
			[DisplayName("Use Existing SaveRAM")]
			[Description("When true, existing SaveRAM will be loaded at boot up")]
			[DefaultValue(true)]
			public bool Use_SRAM { get; set; }

			public G7400SyncSettings Clone()
			{
				return (G7400SyncSettings)MemberwiseClone();
			}

			public G7400SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(G7400SyncSettings x, G7400SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
