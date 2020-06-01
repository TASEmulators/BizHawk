using System.ComponentModel;

using BizHawk.API.ApiHawk;
using BizHawk.Common;
using BizHawk.Emulation.Common;

using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk : IEmulator, ISettable<VectrexHawk.VectrexSettings, VectrexHawk.VectrexSyncSettings>
	{
		public VectrexSettings GetSettings()
		{
			return _settings.Clone();
		}

		public VectrexSyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(VectrexSettings o)
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

		private VectrexSettings _settings = new VectrexSettings();
		public VectrexSyncSettings _syncSettings = new VectrexSyncSettings();

		public class VectrexSettings
		{

			public VectrexSettings Clone()
			{
				return (VectrexSettings)MemberwiseClone();
			}

			public VectrexSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public class VectrexSyncSettings
		{
			[JsonIgnore]
			public string Port1 = VectrexHawkControllerDeck.DefaultControllerName;
			public string Port2 = VectrexHawkControllerDeck.DefaultControllerName;

			public enum ControllerType
			{
				Digital,
				Analog
			}

			[JsonIgnore]
			private ControllerType _VectrexController1;
			private ControllerType _VectrexController2;

			[DisplayName("Controller 1")]
			[Description("Select Controller Type")]
			[DefaultValue(ControllerType.Digital)]
			public ControllerType VectrexController1
			{
				get => _VectrexController1;
				set
				{
					if (value == ControllerType.Digital) { Port1 = VectrexHawkControllerDeck.DefaultControllerName; }
					else { Port1 = "Vectrex Analog Controller"; }

					_VectrexController1 = value;
				}
			}

			[DisplayName("Controller 2")]
			[Description("Select Controller Type")]
			[DefaultValue(ControllerType.Digital)]
			public ControllerType VectrexController2
			{
				get => _VectrexController2;
				set
				{
					if (value == ControllerType.Digital) { Port2 = VectrexHawkControllerDeck.DefaultControllerName; }
					else { Port2 = "Vectrex Analog Controller"; }

					_VectrexController2 = value;
				}
			}

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
