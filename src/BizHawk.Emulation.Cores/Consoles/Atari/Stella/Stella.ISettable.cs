using System.ComponentModel;

using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	public partial class Stella : ISettable<object, Stella.A2600SyncSettings>
	{
		private A2600SyncSettings _syncSettings;

		public object GetSettings()
			=> null;

		public A2600SyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(object o)
			=> PutSettingsDirtyBits.None;

		public PutSettingsDirtyBits PutSyncSettings(A2600SyncSettings o)
		{
			var ret = A2600SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class A2600SyncSettings
		{
			[DefaultValue(Atari2600ControllerTypes.Joystick)]
			[DisplayName("Port 1 Device")]
			[Description("The type of controller plugged into the first controller port")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public Atari2600ControllerTypes Port1 { get; set; }

			[DefaultValue(Atari2600ControllerTypes.Joystick)]
			[DisplayName("Port 2 Device")]
			[Description("The type of controller plugged into the second controller port")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public Atari2600ControllerTypes Port2 { get; set; }

			public CInterface.InitSettings GetNativeSettings(GameInfo game)
			{
				return new CInterface.InitSettings
				{
					dummy = 1
				};
			}

			public A2600SyncSettings Clone()
				=> (A2600SyncSettings)MemberwiseClone();

			public A2600SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(A2600SyncSettings x, A2600SyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
