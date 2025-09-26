using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators.TI83
{
	public class TI83Common : ISettable<TI83Common.TI83CommonSettings, object>
	{
		[CLSCompliant(false)]
		protected TI83CommonSettings _settings;

		[CLSCompliant(false)]
		public TI83CommonSettings GetSettings() => _settings.Clone();

		[CLSCompliant(false)]
		public PutSettingsDirtyBits PutSettings(TI83CommonSettings o)
		{
			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		public object GetSyncSettings() => null;

		public PutSettingsDirtyBits PutSyncSettings(object o) => PutSettingsDirtyBits.None;

		[CLSCompliant(false)]
		public class TI83CommonSettings
		{
			public uint BGColor { get; set; } = 0x889778;
			public uint ForeColor { get; set; } = 0x36412D;

			public TI83CommonSettings Clone() => (TI83CommonSettings)MemberwiseClone();
		}
	}
}
