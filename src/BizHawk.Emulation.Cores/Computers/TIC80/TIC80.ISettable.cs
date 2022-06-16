using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.TIC80
{
	public partial class TIC80 : ISettable<TIC80.TIC80Settings, object>
	{
		private TIC80Settings _settings;

		public TIC80Settings GetSettings()
			=> _settings.Clone();

		public object GetSyncSettings()
			=> null;

		public PutSettingsDirtyBits PutSettings(TIC80Settings o)
		{
			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(object o)
			=> PutSettingsDirtyBits.None;

		public class TIC80Settings
		{
			[DisplayName("Crop")]
			[Description("")]
			[DefaultValue(false)]
			public bool Crop { get; set; }

			public TIC80Settings()
				=> SettingsUtil.SetDefaultValues(this);

			public TIC80Settings Clone()
				=> (TIC80Settings)MemberwiseClone();
		}
	}
}
