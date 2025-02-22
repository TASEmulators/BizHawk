using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public partial class DOSBox : ISettable<object, DOSBox.SyncSettings>
	{
		public enum ConfigurationPreset
		{
			[Display(Name = "Early 80s")]
			Early80s,
			[Display(Name = "Late 80s")]
			Late80s,
			[Display(Name = "Early 90s")]
			Early90s,
			[Display(Name = "Mid 90s")]
			Mid90s,
			[Display(Name = "Late 90s")]
			Late90s,
		}

		public object GetSettings() => null;
		public PutSettingsDirtyBits PutSettings(object o) => PutSettingsDirtyBits.None;

		private SyncSettings _syncSettings;
		public SyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class SyncSettings
		{
			[DisplayName("Configuration Preset")]
			[Description("Establishes a base configuration for DOSBox roughly corresponding to the selected era.")]
			[DefaultValue(ConfigurationPreset.Early90s)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public ConfigurationPreset ConfigurationPreset { get; set; }

			[DisplayName("Enable Joystick 1")]
			[Description("Determines whether a joystick will be plugged in the IBM PC Gameport 1")]
			[DefaultValue(true)]
			public bool EnableJoystick1 { get; set; }

			[DisplayName("Enable Joystick 2")]
			[Description("Determines whether a joystick will be plugged in the IBM PC Gameport 2")]
			[DefaultValue(true)]
			public bool EnableJoystick2 { get; set; }

			[DisplayName("Enable Mouse")]
			[Description("Determines whether a mouse will be plugged in")]
			[DefaultValue(true)]
			public bool EnableMouse { get; set; }

			public SyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public SyncSettings Clone()
				=> (SyncSettings)MemberwiseClone();

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
