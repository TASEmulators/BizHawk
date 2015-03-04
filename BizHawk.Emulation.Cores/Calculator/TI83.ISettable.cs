using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Calculators
{
	public partial class TI83 : ISettable<TI83.TI83Settings, object>
	{
		private TI83Settings Settings;

		public TI83Settings GetSettings()
		{
			return Settings.Clone();
		}

		public bool PutSettings(TI83Settings o)
		{
			Settings = o;
			return false;
		}

		public object GetSyncSettings()
		{
			return null;
		}

		public bool PutSyncSettings(object o)
		{
			return false;
		}

		public class TI83Settings
		{
			public uint BGColor = 0x889778;
			public uint ForeColor = 0x36412D;

			public TI83Settings()
			{

			}

			public TI83Settings Clone()
			{
				return (TI83Settings)MemberwiseClone();
			}
		}
	}
}
