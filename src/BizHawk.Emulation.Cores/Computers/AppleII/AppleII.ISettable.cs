using System.ComponentModel;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : ISettable<AppleII.Settings, object>
	{
		private Settings _settings;

		public class Settings
		{
			[DefaultValue(false)]
			[Description("Choose a monochrome monitor.")]
			public bool Monochrome { get; set; }

			public Settings Clone() => (Settings)MemberwiseClone();
		}

		public Settings GetSettings() => _settings.Clone();

		public object GetSyncSettings() => null;

		public PutSettingsDirtyBits PutSettings(Settings o)
		{
			_settings = o;
			_machine.Video.IsMonochrome = _settings.Monochrome;

			SetCallbacks();

			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(object o) => PutSettingsDirtyBits.None;
	}
}
