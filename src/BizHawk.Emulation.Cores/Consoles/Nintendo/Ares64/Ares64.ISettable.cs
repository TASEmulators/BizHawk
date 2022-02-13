using System.ComponentModel;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64
{
	public partial class Ares64 : ISettable<object, Ares64.Ares64SyncSettings>
	{
		private Ares64SyncSettings _syncSettings;

		public object GetSettings() => null;

		public Ares64SyncSettings GetSyncSettings() => _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(object o) => PutSettingsDirtyBits.None;

		public PutSettingsDirtyBits PutSyncSettings(Ares64SyncSettings o)
		{
			var ret = Ares64SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public class Ares64SyncSettings
		{
			[DisplayName("Player 1 Controller")]
			[Description("")]
			[DefaultValue(LibAres64.ControllerType.Mempak)]
			public LibAres64.ControllerType P1Controller { get; set; }

			[DisplayName("Player 2 Controller")]
			[Description("")]
			[DefaultValue(LibAres64.ControllerType.Unplugged)]
			public LibAres64.ControllerType P2Controller { get; set; }

			[DisplayName("Player 3 Controller")]
			[Description("")]
			[DefaultValue(LibAres64.ControllerType.Unplugged)]
			public LibAres64.ControllerType P3Controller { get; set; }

			[DisplayName("Player 4 Controller")]
			[Description("")]
			[DefaultValue(LibAres64.ControllerType.Unplugged)]
			public LibAres64.ControllerType P4Controller { get; set; }

			[DisplayName("Restrict Analog Range")]
			[Description("Restricts analog range to account for physical limitations.")]
			[DefaultValue(false)]
			public bool RestrictAnalogRange { get; set; }

			public Ares64SyncSettings() => SettingsUtil.SetDefaultValues(this);

			public Ares64SyncSettings Clone() => MemberwiseClone() as Ares64SyncSettings;

			public static bool NeedsReboot(Ares64SyncSettings x, Ares64SyncSettings y) => !DeepEquality.DeepEquals(x, y);
		}
	}
}
