using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Consoles._3DO
{
	public partial class Opera : ISettable<object, Opera.SyncSettings>
	{
		public enum ControllerType 
		{
			[Display(Name = "None")]
			None = 0,
			[Display(Name = "Gamepad")]
			Gamepad = 1,
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
			[DisplayName("Controller 1 Type")]
			[Description("Sets the type of controller connected to the console's port 1.")]
			[DefaultValue(ControllerType.Gamepad)]
			[TypeConverter(typeof(ControllerType))]
			public ControllerType Controller1Type { get; set; }

			[DisplayName("Controller 2 Type")]
			[Description("Sets the type of controller connected to the console's port 2.")]
			[DefaultValue(ControllerType.None)]
			[TypeConverter(typeof(ControllerType))]
			public ControllerType Controller2Type { get; set; }

			public SyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public SyncSettings Clone()
				=> (SyncSettings)MemberwiseClone();

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
