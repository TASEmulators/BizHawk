using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.TIC80
{
	public partial class TIC80 : ISettable<TIC80.TIC80Settings, TIC80.TIC80SyncSettings>
	{
		private TIC80Settings _settings;
		private TIC80SyncSettings _syncSettings;

		public TIC80Settings GetSettings()
			=> _settings.Clone();

		public TIC80SyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(TIC80Settings o)
		{
			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(TIC80SyncSettings o)
		{
			var ret = TIC80SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class TIC80Settings
		{
			[DisplayName("Crop")]
			[Description("")]
			[DefaultValue(true)]
			public bool Crop { get; set; }

			public TIC80Settings()
				=> SettingsUtil.SetDefaultValues(this);

			public TIC80Settings Clone()
				=> (TIC80Settings)MemberwiseClone();
		}

		[CoreSettings]
		public class TIC80SyncSettings
		{
			[DisplayName("Gamepad 1 Enable")]
			[Description("Ignored if game does not support gamepads.")]
			[DefaultValue(true)]
			public bool Gamepad1 { get; set; }

			[DisplayName("Gamepad 2 Enable")]
			[Description("Ignored if game does not support gamepads.")]
			[DefaultValue(false)]
			public bool Gamepad2 { get; set; }

			[DisplayName("Gamepad 3 Enable")]
			[Description("Ignored if game does not support gamepads.")]
			[DefaultValue(false)]
			public bool Gamepad3 { get; set; }

			[DisplayName("Gamepad 4 Enable")]
			[Description("Ignored if game does not support gamepads.")]
			[DefaultValue(false)]
			public bool Gamepad4 { get; set; }

			[DisplayName("Mouse Enable")]
			[Description("Ignored if game does not support the mouse.")]
			[DefaultValue(true)]
			public bool Mouse { get; set; }

			[DisplayName("Keyboard Enable")]
			[Description("Ignored if game does not support the keyboard.")]
			[DefaultValue(true)]
			public bool Keyboard { get; set; }

			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			[TypeConverter(typeof(BizDateTimeConverter))]
			public DateTime InitialTime { get; set; }

			[DisplayName("Use Real Time")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time. Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			public TIC80SyncSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public TIC80SyncSettings Clone()
				=> (TIC80SyncSettings)MemberwiseClone();

			public static bool NeedsReboot(TIC80SyncSettings x, TIC80SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
