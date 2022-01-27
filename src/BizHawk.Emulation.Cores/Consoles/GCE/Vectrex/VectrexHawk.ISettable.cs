using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk : IEmulator, ISettable<object, VectrexHawk.VectrexSyncSettings>
	{
		public object GetSettings() => _settings;

		public VectrexSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(object o)
		{
			_settings = o;
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(VectrexSyncSettings o)
		{
			bool ret = VectrexSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private object _settings = new object();
		public VectrexSyncSettings _syncSettings = new VectrexSyncSettings();

		public class VectrexSyncSettings
		{
			[DisplayName("Controller 1")]
			[Description("Select Controller Type")]
			[DefaultValue(VectrexHawkControllerDeck.DEFAULT_PERIPHERAL_OPTION)]
			public ControllerType Port1 { get; set; } = VectrexHawkControllerDeck.DEFAULT_PERIPHERAL_OPTION;

			[DisplayName("Controller 2")]
			[Description("Select Controller Type")]
			[DefaultValue(VectrexHawkControllerDeck.DEFAULT_PERIPHERAL_OPTION)]
			public ControllerType Port2 { get; set; } = VectrexHawkControllerDeck.DEFAULT_PERIPHERAL_OPTION;

			public VectrexSyncSettings Clone()
			{
				return (VectrexSyncSettings)MemberwiseClone();
			}

			public VectrexSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(VectrexSyncSettings x, VectrexSyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}
		}
	}
}
